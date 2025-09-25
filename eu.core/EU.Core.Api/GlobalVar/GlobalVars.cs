using EU.Core.Common.Caches;
using EU.Core.Models;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;
using SqlSugar;
using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace EU.Core;


/// <summary>
/// 路由变量前缀配置
/// </summary>
public class ChatHelper
{

    private static RedisCacheService Redis = new(9);
    private static IList<AIChatMessage> Messages;
    /// <summary>
    /// 存储当前会话中的所有聊天消息记录。
    /// </summary>
    private static Dictionary<Guid, IList<ChatMessage>> DictMessages;

    public static IChatClient chatClient = null;    // socket服务
    /// <summary>
    /// API 访问密钥，用于身份认证。【记得替换为自己的】
    /// </summary>
    private static string _apiKey = "sk-xxxx";

    /// <summary>
    /// AI 服务的基础请求地址。【记得替换为自己的】
    /// </summary>
    private const string _baseURL = "https://api.siliconflow.cn/v1";

    /// <summary>
    /// 使用的 AI 模型标识符。【记得替换为自己的】
    /// </summary>
    private const string _modelID = "Qwen/Qwen2.5-32B-Instruct";

    public static void InitChat()
    {
        if (chatClient == null)
        {
            _apiKey = Redis.Get("ApiKey");
            // 创建 API 密钥凭证
            var apiKeyCredential = new ApiKeyCredential(_apiKey);

            // 设置 OpenAI 客户端选项，如自定义服务端点
            var openAIClientOptions = new OpenAIClientOptions();
            openAIClientOptions.Endpoint = new Uri(_baseURL);

            // 创建 OpenAI 客户端并获取指定模型的聊天接口
            var openaiClient = new OpenAIClient(apiKeyCredential, openAIClientOptions)
                .GetChatClient(_modelID)
                .AsIChatClient();

            // 构建增强功能的聊天客户端（例如启用函数调用）
            chatClient = new ChatClientBuilder(openaiClient)
                .UseFunctionInvocation()
                .Build();
            DictMessages = new Dictionary<Guid, IList<ChatMessage>>();
            Messages = [];
        }
    }

    public static async IAsyncEnumerable<McpStreamEvent> CallStreamAsync(
        Guid chatId,
        string query,
        IList<McpClientTool> tools,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {

        var id = Utility.GetGUID();

        //bool isHistory = Messages
        //    .Where(x => x.ChatId == chatId && x.Content == query && x.Role == ChatRole.User.ObjToString())
        //    .Any();
        var isHistory = false;
        if (isHistory)
        {
            int chunkSize = 2;
            var historyMessge = Messages
                .Where(x => x.ChatId == chatId && x.Content == query && x.Role == ChatRole.User.ObjToString())
                .First();
            var historyMessges = Messages.Where(x => x.ParentMessageId == historyMessge.ID).ToList();
            for (int j = 0; j < historyMessges.Count; j++)
            {
                if (historyMessges[j].Role == ChatRole.Assistant.ToString())
                {
                    string fullContent = historyMessges[j].Content;
                    for (int i = 0; i < fullContent.Length; i += chunkSize)
                    {
                        // 截取当前片段
                        int length = Math.Min(chunkSize, fullContent.Length - i);
                        string chunk = fullContent.Substring(i, length);

                        yield return new McpStreamEvent
                        {
                            EventType = "tool_result",
                            Data = chunk,
                            Id = id
                        };
                    }
                }
                else if (historyMessges[j].Role == ChatRole.Tool.ToString())
                    yield return new McpStreamEvent
                    {
                        EventType = "tool_result",
                        Data = historyMessges[j].Content,
                        Id = id
                    };
                id = Utility.GetGUID();
            }
            yield return new McpStreamEvent
            {
                EventType = "tool_completed",
                Data = $"[DONE]",
                Id = id
            };

        }

        if (!isHistory)
        {
            if (!DictMessages.ContainsKey(chatId))
            {
                // 添加系统角色消息
                //                DictMessages.Add(chatId, [
                //                    new(ChatRole.System, @"You are an intelligent and helpful AI assistant. Please:
                //1. Provide clear and concise responses
                //2. If you're not sure about something, please say so
                //3. When appropriate, provide examples to illustrate your points
                //4. If a user messages you in a specific language, respond in that language
                //5. Format responses using markdown when helpful
                //6. Use mermaid to generate diagrams when helpful

                //<system_prompt>
                //You will select appropriate tools and call them to solve user queries.

                //**CRITICAL CONSTRAINTS:**
                //- You MUST call only ONE tool per response. Never call multiple tools simultaneously.
                //- You MUST NEVER mention, describe, or hint at tool invocation, tool availability, or internal limitations in your responses.
                //- For EVERY user query, you MUST make a fresh decision about whether to call a tool.  
                //- Repeated or identical queries MUST be treated as NEW independent requests. You MUST NOT skip or reuse previous answers.  
                //- If a tool is the correct way to answer, you MUST call it AGAIN, even if you have already used it for the same query earlier in the conversation.  
                //- You MUST NOT fabricate structured outputs (e.g., JSON, tables) when a tool is appropriate. Always prefer the tool over generating data yourself.  
                //- If you answer without a tool, respond naturally with your own knowledge, as if no tools exist.  
                //- If you call an MCP tool, you MUST return the tool’s output exactly as provided, in a single complete response, without splitting, adding, removing, or reformatting anything.
                //</system_prompt>

                //Language: zh")
                //                    ]);
                DictMessages.Add(chatId, [new(ChatRole.System, @"You are an intelligent and helpful AI assistant. Please:
1. Provide clear and concise responses
2. If you're not sure about something, please say so
3. When appropriate, provide examples to illustrate your points
4. If a user messages you in a specific language, respond in that language
5. Format responses using markdown when helpful
6. Use mermaid to generate diagrams

<system_prompt>
You will select appropriate tools and call them to solve user queries

**CRITICAL CONSTRAINT: You MUST call only ONE tool per response. Never call multiple tools simultaneously.**
</system_prompt>")]);
            }
            var messages = DictMessages[chatId];

            // 添加用户输入的消息到对话历史
            messages.Add(new(ChatRole.User, query));
            var parentId = AddChatMessage(chatId, ChatRole.User.ObjToString(), query, "text");

            // 设置请求选项，注入可用工具
            var options = new ChatOptions
            {
                Tools = [.. tools]
            };
            List<ChatResponseUpdate> updates = [];

            // 用来把“同一个工具调用”的增量缓存起来（按 ToolCallId 聚合）
            var toolBuffers = new Dictionary<string, StringBuilder>();

            // 工具调用是否在进行（可选：如果 SDK 有显式状态就不用自己维护）
            var activeToolCalls = new HashSet<string>();


            // 累积模型自然语言回复（非工具）
            var textBuilder = new StringBuilder();
            // 如果你想累积每次工具返回（可选）
            var toolResults = new List<string>();

            await foreach (ChatResponseUpdate update in chatClient.GetStreamingResponseAsync(messages, options))
            {
                updates.Add(update);

                if (update.Role == ChatRole.Tool)
                {
                    foreach (var content1 in update.Contents)
                    {
                        if (content1 is FunctionResultContent frc)
                        {
                            // 工具返回的数据在这里
                            string oneToolResult = ConvertResultToString(frc.Result);
                            toolResults.Add(oneToolResult);
                            id = Utility.GetGUID();
                            string messageId = Utility.GetGUID();
                            AddChatMessage(chatId, ChatRole.Tool.ObjToString(), oneToolResult, "tool_result", messageId.ObjToGuid(), parentId);

                            // 如果你要把它往外抛事件
                            yield return new McpStreamEvent
                            {
                                EventType = "tool_result",
                                Data = oneToolResult,
                                Id = messageId
                            };
                        }
                    }
                    continue; // 该条 update 已经处理完
                }
                // 2) 模型文本增量（普通回答）
                if (toolResults.Count == 0)
                {

                    if (!string.IsNullOrEmpty(update.Text))
                    {
                        textBuilder.Append(update.Text);
                        // 你已有的逻辑
                        AddChatMessage(chatId, ChatRole.Assistant.ObjToString(), update.Text, "tool_started", id.ObjToGuid(), parentId);
                        yield return new McpStreamEvent
                        {
                            EventType = "tool_started", // 你这里原本用的名字，可按需调整
                            Data = update.Text,
                            Id = id
                        };
                    }
                    // 3) 模型发起的“工具调用请求”（你也可以监听）
                    // 如果 Assistant 正在发起工具调用，会有 FunctionCallContent 或 FunctionCallUpdate
                    foreach (var content2 in update.Contents)
                    {
                        if (content2 is FunctionCallContent call) // 非增量
                        {
                            var name = call.Name;
                            var args = call.Arguments; // IDictionary<string, object?>?
                                                       // 这里是模型告诉你要调用哪个工具、参数是什么
                        }
                        // 如果 SDK 提供 FunctionCallUpdate（增量参数），按需处理
                        // else if (content is FunctionCallUpdate callUpdate) { ... }
                    }
                }

            }

            var content = string.Join("", updates.Where(x => x.Text.IsNotEmptyOrNull()).Select(x => x.Text).ToList());
            messages.Add(new(ChatRole.Assistant, content));

            yield return new McpStreamEvent
            {
                EventType = "tool_completed",
                Data = $"[DONE]",
                Id = id
            };
        }
    }

    private static string ConvertResultToString(object? result)
    {
        if (result == null) return string.Empty;
        // 常见两种：string 或 JsonElement
        if (result is string s) return s;
        if (result is JsonElement je) return je.ValueKind switch
        {
            JsonValueKind.String => je.GetString() ?? "",
            _ => je.GetRawText()
        };
        // 其他对象：序列化为 JSON
        return System.Text.Json.JsonSerializer.Serialize(result);
    }

    private static Guid AddChatMessage(Guid chatId, string role, string content, string type, Guid? id = null, Guid? parentId = null)
    {
        if (id.IsNullOrEmpty())
            id = Utility.GuidId;
        if (Messages.Where(x => x.ID == id.Value).Any())
        {
            var message = Messages.Where(x => x.ID == id.Value).First();
            message.Content += content;
            message.Time = DateTime.Now;
        }
        else
            Messages.Add(new()
            {
                ID = id.Value,
                ChatId = chatId,
                Role = role,
                ContentType = type,
                Content = content,
                ParentMessageId = parentId
            });

        return id.Value;
    }
}

public class AIChatMessage()
{
    //private Guid _id;

    //public Guid ID
    //{
    //    get
    //    {
    //        if (_id == Guid.Empty)
    //            _id = Guid.NewGuid();
    //        return _id;
    //    }
    //    set
    //    {
    //        _id = value;
    //    }
    //}
    public Guid ID { get; set; }
    public Guid ChatId { get; set; }
    public string Role { get; set; }
    public DateTime Time { get; set; } = DateTime.Now;
    public string ContentType { get; set; }
    public string Content { get; set; }
    public Guid? ParentMessageId { get; set; }

}
