using EU.Core;
using EU.Core.MCP.Models;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;
using System.ClientModel;

namespace EU.Core
{

    /// <summary>
    /// 路由变量前缀配置
    /// </summary>
    public class ChatHelper
    {
        /// <summary>
        /// 存储当前会话中的所有聊天消息记录。
        /// </summary>
        private static IList<ChatMessage> Messages;

        private static Dictionary<Guid, IList<ChatMessage>> DictMessages;

        public static IChatClient chatClient = null;    // socket服务
        /// <summary>
        /// API 访问密钥，用于身份认证。【记得替换为自己的】
        /// </summary>
        private const string _apiKey = "sk-couagngrgouclgbtiqvwpzlhshfjvmmkhlkhbnyjluemltdv";

        /// <summary>
        /// AI 服务的基础请求地址。【记得替换为自己的】
        /// </summary>
        private const string _baseURL = "https://api.siliconflow.cn/v1";

        /// <summary>
        /// 使用的 AI 模型标识符。【记得替换为自己的】
        /// </summary>
        private const string _modelID = "moonshotai/Kimi-K2-Instruct";

        public static void InitChat()
        {
            if (chatClient == null)
            {
                // 创建 API 密钥凭证
                ApiKeyCredential apiKeyCredential = new ApiKeyCredential(_apiKey);

                // 设置 OpenAI 客户端选项，如自定义服务端点
                OpenAIClientOptions openAIClientOptions = new OpenAIClientOptions();
                openAIClientOptions.Endpoint = new Uri(_baseURL);

                // 创建 OpenAI 客户端并获取指定模型的聊天接口
                var openaiClient = new OpenAIClient(apiKeyCredential, openAIClientOptions)
                    .GetChatClient(_modelID)
                    .AsIChatClient();

                // 构建增强功能的聊天客户端（例如启用函数调用）
                chatClient = new ChatClientBuilder(openaiClient)
                    .UseFunctionInvocation()
                    .Build();

                // 初始化对话历史，包含一条系统提示信息
                Messages =
                    [
                    // 添加系统角色消息
                    new(ChatRole.System, "您是一位乐于助人的助手，帮助我们测试MCP服务器功能，优先使用中文回答！")
                    ];
                DictMessages = new Dictionary<Guid, IList<ChatMessage>>();
            }
        }

        public static async IAsyncEnumerable<McpStreamEvent> CallStreamAsync(Guid chatId, string query, IList<McpClientTool> tools,
 [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var id = Utility.GetGUID();

            if (!DictMessages.ContainsKey(chatId))
            {
                // 添加系统角色消息
                DictMessages.Add(chatId, [
                    new(ChatRole.System, "You are an intelligent and helpful AI assistant. Please:\r\n1. Provide clear and concise responses\r\n2. If you're not sure about something, please say so\r\n3. When appropriate, provide examples to illustrate your points\r\n4. If a user messages you in a specific language, respond in that language\r\n5. Format responses using markdown when helpful\r\n6. Use mermaid to generate diagrams \r\n<system_prompt>\r\nYou will select appropriate tools and call them to solve user queries\r\n\r\n**CRITICAL CONSTRAINT: You MUST call only ONE tool per response. Never call multiple tools simultaneously.**\r\n</system_prompt>\r\nLanguage: zh")
                    ]);
            }
            var messages = DictMessages[chatId];

            // 添加用户输入的消息到对话历史
            messages.Add(new(ChatRole.User, query));

            // 设置请求选项，注入可用工具
            var options = new ChatOptions
            {
                Tools = [.. tools]
            };

            // 调用 AI 客户端获取响应
            Console.ForegroundColor = ConsoleColor.Green;

            List<ChatResponseUpdate> updates = [];
            await foreach (ChatResponseUpdate update in chatClient.GetStreamingResponseAsync(messages, options))
            {
                updates.Add(update);
                if (!string.IsNullOrEmpty(update.Text))
                    yield return new McpStreamEvent
                    {
                        EventType = "tool_started",
                        Data = update.Text,
                        Id = id
                    };
            }
            Console.ForegroundColor = ConsoleColor.Yellow;

            var content = string.Join("", updates.Where(x => x.Text.IsNotEmptyOrNull()).Select(x => x.Text).ToList());
            messages.Add(new(ChatRole.System, content));
            DictMessages[chatId] = messages;

            //var response = await ChatClient.GetResponseAsync(Messages, options);

            // 将 AI 响应加入对话历史
            //Messages.AddMessages(response);

            //// 输出调用的工具信息
            //OutputToolUsageInfo(response);

            // 返回模型生成的文本响应 


            //for (int i = 0; i < 10; i++)
            //{
            //    Thread.Sleep(1000); // 暂停1000毫秒（1秒）
            //    yield return new McpStreamEvent
            //    {
            //        EventType = "tool_result",
            //        Data = $"测试id: " + Guid.NewGuid(),
            //        Id = Guid.NewGuid().ToString()
            //    };
            //}

            yield return new McpStreamEvent
            {
                EventType = "tool_completed",
                Data = $"[DONE]",
                Id = id
            };
        }

    }


}
