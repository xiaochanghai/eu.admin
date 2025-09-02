using EU.Core.MCP.Models;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;
using System.ClientModel;

namespace MCPClient
{
    /// <summary>
    /// 表示一个用于与 AI 聊天模型交互的客户端封装类。
    /// 负责初始化聊天客户端并维护对话上下文。
    /// </summary>
    public class ChatAIClient
    {
        /// <summary>
        /// 封装后的 AI 聊天客户端接口，支持函数调用等功能。
        /// </summary>
        private IChatClient ChatClient;

        /// <summary>
        /// 存储当前会话中的所有聊天消息记录。
        /// </summary>
        private IList<ChatMessage> Messages;

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

        /// <summary>
        /// 初始化一个新的 <see cref="ChatAIClient"/> 实例。
        /// 构造函数中自动完成聊天客户端的初始化配置。
        /// </summary>
        public ChatAIClient()
        {
            InitIChatClient();
        }

        /// <summary>
        /// 初始化内部使用的 AI 聊天客户端实例。
        /// 配置 API 凭证、服务端点，并构建具备函数调用能力的客户端。
        /// 同时初始化系统消息作为对话起点。
        /// </summary>
        private void InitIChatClient()
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
            ChatClient = new ChatClientBuilder(openaiClient)
                .UseFunctionInvocation()
                .Build();

            // 初始化对话历史，包含一条系统提示信息
            Messages =
            [
                // 添加系统角色消息
                new(ChatRole.System, "您是一位乐于助人的助手，帮助我们测试MCP服务器功能，优先使用中文回答！"),
            ];
        }

        /// <summary>
        /// 异步处理用户的自然语言查询，并与 AI 模型进行交互，支持 MCP 工具调用。
        /// </summary>
        /// <param name="query">用户的自然语言查询内容</param>
        /// <param name="tools">可用的 MCP 工具列表，用于扩展 AI 的外部能力</param>
        /// <returns>AI 返回的最终文本响应结果</returns>
        public async Task<string> ProcessQueryAsync(string query, IList<McpClientTool> tools)
        {
            // 如果消息历史为空，则初始化系统提示消息
            if (Messages.Count == 0)
            {
                Messages =
                [
                    new(ChatRole.System, "您是一位乐于助人的助手，帮助我们测试MCP服务器功能，优先使用中文回答！")
                ];
            }

            // 添加用户输入的消息到对话历史
            Messages.Add(new(ChatRole.User, query));

            // 设置请求选项，注入可用工具
            var options = new ChatOptions
            {
                Tools = [.. tools]
            };

            // 调用 AI 客户端获取响应
            Console.ForegroundColor = ConsoleColor.Green;

            List<ChatResponseUpdate> updates = [];
            await foreach (ChatResponseUpdate update in ChatClient.GetStreamingResponseAsync(Messages, options))
            {

                Task task = new Task(() =>
                {

                    if (!string.IsNullOrEmpty(update.Text))
                        Console.Write(update);
                });
                task.Start();
                updates.Add(update);
            }
            Console.ForegroundColor = ConsoleColor.Yellow;

            //var response = await ChatClient.GetResponseAsync(Messages, options);

            // 将 AI 响应加入对话历史
            //Messages.AddMessages(response);

            //// 输出调用的工具信息
            //OutputToolUsageInfo(response);

            // 返回模型生成的文本响应
            return "";
        }

        public async IAsyncEnumerable<McpStreamEvent> CallStreamAsync(string query, IList<McpClientTool> tools,
         [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {

            // 如果消息历史为空，则初始化系统提示消息
            if (Messages.Count == 0)
            {
                Messages =
                [
                    new(ChatRole.System, "您是一位乐于助人的助手，帮助我们测试MCP服务器功能，优先使用中文回答！")
                ];
            }

            // 添加用户输入的消息到对话历史
            Messages.Add(new(ChatRole.User, query));

            // 设置请求选项，注入可用工具
            var options = new ChatOptions
            {
                Tools = [.. tools]
            };

            // 调用 AI 客户端获取响应
            Console.ForegroundColor = ConsoleColor.Green;

            List<ChatResponseUpdate> updates = [];
            await foreach (ChatResponseUpdate update in ChatClient.GetStreamingResponseAsync(Messages, options))
            {
                updates.Add(update);
                if (!string.IsNullOrEmpty(update.Text))
                    yield return new McpStreamEvent
                    {
                        EventType = "tool_started",
                        Data = update.Text,
                        Id = Guid.NewGuid().ToString()
                    };
            }
            Console.ForegroundColor = ConsoleColor.Yellow;

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
                Data = $"执行完成",
                Id = Guid.NewGuid().ToString()
            };
        }

        /// <summary>
        /// 辅助方法：输出 AI 在响应中调用的工具信息到控制台。
        /// </summary>
        /// <param name="response">来自 AI 的完整响应对象</param>
        private void OutputToolUsageInfo(ChatResponse response)
        {
            // 获取所有 Tool 角色的消息
            var toolUseMessages = response.Messages.Where(m => m.Role == ChatRole.Tool).ToList();

            // 判断是否调用了工具
            // 获取响应中所有角色为 Tool 的消息（即 AI 调用了哪些工具）
            var toolUseMessage = response.Messages.Where(m => m.Role == ChatRole.Tool);

            // 判断第一条消息的内容是否多于一个（通常第一个消息是用户问题，第二个是调用函数）
            if (response.Messages[0].Contents.Count > 1)
            {
                // 尝试从第一条消息的第二个内容项提取出函数调用信息
                var functionCall = (FunctionCallContent)response.Messages[0].Contents[1];

                // 设置控制台输出颜色为绿色，用于突出显示工具调用信息
                Console.ForegroundColor = ConsoleColor.Green;

                string arguments = "";

                // 如果函数调用包含参数，则拼接参数信息
                if (functionCall.Arguments != null)
                {
                    foreach (var arg in functionCall.Arguments)
                    {
                        arguments += $"{arg.Key}:{arg.Value};";
                    }

                    // 输出调用的方法名及参数信息
                    Console.WriteLine($"调用方法名:{functionCall.Name};参数信息：{arguments}");

                    // 遍历所有 Tool 消息，输出每个工具调用的结果
                    foreach (var message in toolUseMessage)
                    {
                        // 提取工具调用后的执行结果
                        var functionResultContent = (FunctionResultContent)message.Contents[0];
                        Console.WriteLine($"调用工具结果：{functionResultContent.Result}");
                    }

                    // 恢复控制台默认颜色（白色）
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                {
                    // 如果没有参数
                    Console.WriteLine("工具参数为空");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("本次没有调用工具");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
    }
}