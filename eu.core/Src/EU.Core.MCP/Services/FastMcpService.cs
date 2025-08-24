using EU.Core.MCP.Attributes;
using EU.Core.MCP.Interfaces;
using EU.Core.MCP.Models;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;

namespace EU.Core.MCP.Services;

/// <summary>
/// FastMCP 风格的服务实现 - 支持完整的 MCP 协议
/// </summary>
public class FastMcpService : IMcpService
{
    private readonly ILogger<FastMcpService> _logger;
    private readonly Dictionary<string, MethodInfo> _toolMethods;
    private readonly Dictionary<string, MethodInfo> _resourceMethods;
    private readonly Dictionary<string, MethodInfo> _promptMethods;
    private readonly object _serviceInstance;

    // 添加通知回调支持
    public event Func<McpNotification, Task>? OnNotification;

    public FastMcpService(ILogger<FastMcpService> logger)
    {
        _logger = logger;
        _toolMethods = new Dictionary<string, MethodInfo>();
        _resourceMethods = new Dictionary<string, MethodInfo>();
        _promptMethods = new Dictionary<string, MethodInfo>();
        
        // 创建服务实例
        _serviceInstance = Activator.CreateInstance(typeof(FastMcpTools))!;
        
        // 自动发现工具、资源和提示
        DiscoverMcpMethods();
    }

    private void DiscoverMcpMethods()
    {
        var type = typeof(FastMcpTools);
        
        // 发现工具方法
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            var toolAttr = method.GetCustomAttribute<McpToolAttribute>();
            if (toolAttr != null)
            {
                var name = string.IsNullOrEmpty(toolAttr.Name) ? method.Name : toolAttr.Name;
                _toolMethods[name] = method;
                _logger.LogInformation("发现工具: {ToolName}", name);
            }
            
            var resourceAttr = method.GetCustomAttribute<McpResourceAttribute>();
            if (resourceAttr != null)
            {
                var uri = resourceAttr.Uri;
                _resourceMethods[uri] = method;
                _logger.LogInformation("发现资源: {Uri}", uri);
            }
            
            var promptAttr = method.GetCustomAttribute<McpPromptAttribute>();
            if (promptAttr != null)
            {
                var name = string.IsNullOrEmpty(promptAttr.Name) ? method.Name : promptAttr.Name;
                _promptMethods[name] = method;
                _logger.LogInformation("发现提示: {PromptName}", name);
            }
        }
    }

    public async Task<List<McpTool>> GetToolsAsync()
    {
        var tools = new List<McpTool>();
        
        foreach (var kvp in _toolMethods)
        {
            var method = kvp.Value;
            var attr = method.GetCustomAttribute<McpToolAttribute>()!;
            
            tools.Add(new McpTool
            {
                Name = kvp.Key,
                Description = attr.Description,
                InputSchema = attr.InputSchema ?? GenerateInputSchema(method)
            });
        }
        
        return tools;
    }

    public async Task<List<McpResource>> GetResourcesAsync()
    {
        var resources = new List<McpResource>();
        
        foreach (var kvp in _resourceMethods)
        {
            var method = kvp.Value;
            var attr = method.GetCustomAttribute<McpResourceAttribute>()!;
            
            resources.Add(new McpResource
            {
                Uri = kvp.Key,
                Name = attr.Name,
                Description = attr.Description,
                MimeType = attr.MimeType
            });
        }
        
        return resources;
    }

    public async Task<object> CallToolAsync(string toolName, object parameters)
    {
        if (_toolMethods.TryGetValue(toolName, out var method))
        {
            try
            {
                var context = new McpContext();
                var args = PrepareMethodArguments(method, parameters, context);
                
                var result = method.Invoke(_serviceInstance, args);
                
                if (result is Task task)
                {
                    await task;
                    var resultProperty = task.GetType().GetProperty("Result");
                    return resultProperty?.GetValue(task) ?? new { };
                }
                
                return result ?? new { };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "工具 {ToolName} 执行失败", toolName);
                throw;
            }
        }
        
        throw new ArgumentException($"未知的工具: {toolName}");
    }

    public async Task<object> CallToolWithProgressAsync(string toolName, object parameters, 
        Action<string> progressCallback, CancellationToken cancellationToken = default)
    {
        progressCallback($"开始执行工具: {toolName}");
        
        var result = await CallToolAsync(toolName, parameters);
        
        progressCallback("工具执行完成");
        return result;
    }

    public async Task<object> GetResourceAsync(string uri)
    {
        if (_resourceMethods.TryGetValue(uri, out var method))
        {
            try
            {
                var context = new McpContext();
                var args = PrepareMethodArguments(method, new { }, context);
                
                var result = method.Invoke(_serviceInstance, args);
                
                if (result is Task task)
                {
                    await task;
                    var resultProperty = task.GetType().GetProperty("Result");
                    return resultProperty?.GetValue(task) ?? new { };
                }
                
                return result ?? new { };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "资源 {Uri} 访问失败", uri);
                throw;
            }
        }
        
        throw new ArgumentException($"无法处理的资源URI: {uri}");
    }

    public async Task<McpResponse> HandleRequestAsync(McpRequest request)
    {
        try
        {
            switch (request.Method)
            {
                case "initialize":
                    return await HandleInitializeRequest(request);


                case "notifications/initialized":
                    return await HandleNotificationRequest(request);

                case "tools/list":
                    var tools = await GetToolsAsync();
                    return new McpResponse { Id = request.Id, result = new { tools } };

                case "tools/call":
                    var callParams = JsonSerializer.Deserialize<McpToolsCallParams>(JsonSerializer.Serialize(request.Params));
                    if (callParams != null)
                    {
                        var result = await CallToolAsync(callParams.Name, callParams.Arguments ?? new { });
                        var content = new McpContent { Type = "text", Text = JsonSerializer.Serialize(result) };
                        return new McpResponse { Id = request.Id, result = new { content = new[] { content }, isError = false } };
                    }
                    break;

                case "resources/list":
                    var resources = await GetResourcesAsync();
                    return new McpResponse { Id = request.Id, result = new { resources } };

                case "resources/read":
                    var readParams = JsonSerializer.Deserialize<McpResourcesReadParams>(JsonSerializer.Serialize(request.Params));
                    if (readParams != null)
                    {
                        var resourceData = await GetResourceAsync(readParams.Uri);
                        var content = new McpContent { Type = "text", Text = JsonSerializer.Serialize(resourceData) };
                        return new McpResponse { Id = request.Id, result = new { contents = new[] { content }, mimeType = "application/json" } };
                    }
                    break;

                case "prompts/list":
                    var prompts = await GetPromptsAsync();
                    return new McpResponse { Id = request.Id, result = new { prompts } };

                default:
                    return new McpResponse
                    {
                        Id = request.Id,
                        Error = new McpError
                        {
                            Code = -32601,
                            Message = $"未知的方法: {request.Method}"
                        }
                    };
            }

            return new McpResponse
            {
                Id = request.Id,
                Error = new McpError
                {
                    Code = -32602,
                    Message = "无效的参数"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理MCP请求失败: {Method}", request.Method);
            return new McpResponse
            {
                Id = request.Id,
                Error = new McpError
                {
                    Code = -32603,
                    Message = ex.Message
                }
            };
        }
    }

    /// <summary>
    /// 处理初始化请求
    /// </summary>
    private async Task<McpResponse> HandleInitializeRequest(McpRequest request)
    {

        var result1 = new McpInitializeResult
        {
            ProtocolVersion = McpProtocolVersion.V2024_11_05,
            Capabilities = new McpServerCapabilities
            {
                Roots = new McpRootsCapability { ListChanged = true },
                Sampling = new { },
                Tools = new { },
                Resources = new { },
                Prompts = new { }
            },
            ServerInfo = new McpServerInfo
            {
                Name = "EU.Core FastMCP Server",
                Version = "1.0.0"
            }
        };
         

        return new McpResponse { Id = request.Id, result = result1 };

        var initParams = JsonSerializer.Deserialize<McpInitializeParams>(JsonSerializer.Serialize(request.Params));
        
        if (initParams != null)
        {
            var result = new McpInitializeResult
            {
                ProtocolVersion = McpProtocolVersion.V2024_11_05,
                Capabilities = new McpServerCapabilities
                {
                    Roots = new McpRootsCapability { ListChanged = true },
                    Sampling = new { },
                    Tools = new { },
                    Resources = new { },
                    Prompts = new { }
                },
                ServerInfo = new McpServerInfo
                {
                    Name = "EU.Core FastMCP Server",
                    Version = "1.0.0"
                }
            }; 
            
            return new McpResponse { Id = request.Id, result = result };
        }
        
        return new McpResponse
        {
            Id = request.Id,
            Error = new McpError
            {
                Code = -32602,
                Message = "无效的初始化参数"
            }
        };
    }

    private async Task<McpResponse> HandleNotificationRequest(McpRequest request)
    {

        var result1 = new McpInitializeResult
        {
            ProtocolVersion = McpProtocolVersion.V2024_11_05,
            Capabilities = new McpServerCapabilities
            {
                Roots = new McpRootsCapability { ListChanged = true },
                Sampling = new { },
                Tools = new { },
                Resources = new { },
                Prompts = new { }
            },
            ServerInfo = new McpServerInfo
            {
                Name = "EU.Core FastMCP Server",
                Version = "1.0.0"
            }
        };


        return new McpResponse { Id = request.Id, result = result1 };

        var initParams = JsonSerializer.Deserialize<McpInitializeParams>(JsonSerializer.Serialize(request.Params));

        if (initParams != null)
        {
            var result = new McpInitializeResult
            {
                ProtocolVersion = McpProtocolVersion.V2024_11_05,
                Capabilities = new McpServerCapabilities
                {
                    Roots = new McpRootsCapability { ListChanged = true },
                    Sampling = new { },
                    Tools = new { },
                    Resources = new { },
                    Prompts = new { }
                },
                ServerInfo = new McpServerInfo
                {
                    Name = "EU.Core FastMCP Server",
                    Version = "1.0.0"
                }
            };

            return new McpResponse { Id = request.Id, result = result };
        }

        return new McpResponse
        {
            Id = request.Id,
            Error = new McpError
            {
                Code = -32602,
                Message = "无效的初始化参数"
            }
        };
    }

    /// <summary>
    /// 发送通知
    /// </summary>
    private async Task SendNotificationAsync(McpNotification notification)
    {
        if (OnNotification != null)
        {
            try
            {
                await OnNotification(notification);
                _logger.LogDebug("发送通知: {Method}", notification.Method);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送通知失败: {Method}", notification.Method);
            }
        }
    }

    public async IAsyncEnumerable<McpStreamEvent> CallToolStreamAsync(string toolName, object parameters, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 发送开始事件
        yield return new McpStreamEvent
        {
            EventType = "tool_start",
            Data = new { toolName, parameters },
            Id = Guid.NewGuid().ToString()
        };

        //try
        //{
            var context = new McpContext();
            
            // 模拟流式执行过程
            for (int i = 0; i < 5; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await Task.Delay(500, cancellationToken);
                
                await context.ReportProgressAsync($"执行步骤 {i + 1}/5", (i + 1) * 20.0);

                yield return new McpStreamEvent
                {
                    EventType = "tool_progress",
                    Data = new { step = i + 1, total = 5, message = $"执行步骤 {i + 1}/5" },
                    Id = Guid.NewGuid().ToString()
                };
            }

            // 执行实际工具调用
            var result = await CallToolAsync(toolName, parameters);

            // 发送完成事件
            yield return new McpStreamEvent
            {
                EventType = "tool_complete",
                Data = new { result, message = "工具执行完成" },
                Id = Guid.NewGuid().ToString()
            };
        //}
        //catch (Exception ex)
        //{
        //    yield return new McpStreamEvent
        //    {
        //        EventType = "tool_error",
        //        Data = new { error = ex.Message, stackTrace = ex.StackTrace },
        //        Id = Guid.NewGuid().ToString()
        //    };
        //}
    }

    private async Task<List<object>> GetPromptsAsync()
    {
        var prompts = new List<object>();
        
        foreach (var kvp in _promptMethods)
        {
            var method = kvp.Value;
            var attr = method.GetCustomAttribute<McpPromptAttribute>()!;
            
            prompts.Add(new
            {
                name = kvp.Key,
                description = attr.Description
            });
        }
        
        return prompts;
    }

    private object[] PrepareMethodArguments(MethodInfo method, object parameters, McpContext context)
    {
        var parameters2 = method.GetParameters();
        var args = new object[parameters2.Length];
        
        for (int i = 0; i < parameters2.Length; i++)
        {
            var param = parameters2[i];
            
            if (param.ParameterType == typeof(McpContext))
            {
                args[i] = context;
            }
            else if (param.ParameterType == typeof(object))
            {
                args[i] = parameters;
            }
            else
            {
                // 尝试将参数转换为正确的类型
                try
                {
                    args[i] = JsonSerializer.Deserialize(JsonSerializer.Serialize(parameters), param.ParameterType) ?? 
                               Activator.CreateInstance(param.ParameterType)!;
                }
                catch
                {
                    args[i] = Activator.CreateInstance(param.ParameterType)!;
                }
            }
        }
        
        return args;
    }

    private object GenerateInputSchema(MethodInfo method)
    {
        var parameters = method.GetParameters();
        var properties = new Dictionary<string, object>();
        
        foreach (var param in parameters)
        {
            if (param.ParameterType != typeof(McpContext))
            {
                properties[param.Name!] = new
                {
                    type = GetJsonSchemaType(param.ParameterType),
                    description = $"参数: {param.Name}"
                };
            }
        }
        
        return new
        {
            type = "object",
            properties = properties
        };
    }

    private string GetJsonSchemaType(Type type)
    {
        if (type == typeof(string)) return "string";
        if (type == typeof(int) || type == typeof(long)) return "integer";
        if (type == typeof(double) || type == typeof(decimal)) return "number";
        if (type == typeof(bool)) return "boolean";
        if (type.IsArray || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) return "array";
        return "object";
    }
}

/// <summary>
/// FastMCP 工具实现类 - 使用装饰器定义工具
/// </summary>
public class FastMcpTools
{
    [McpTool("query_database", "查询数据库数据")]
    public async Task<object> QueryDatabaseAsync(string table, string[] fields, string where, int limit, McpContext ctx)
    {
        await ctx.InfoAsync($"开始查询数据库表: {table}");
        await ctx.ReportProgressAsync("连接数据库", 10);
        
        await Task.Delay(200);
        await ctx.ReportProgressAsync("执行查询", 50);
        
        await Task.Delay(200);
        await ctx.ReportProgressAsync("处理结果", 90);
        
        await ctx.InfoAsync("查询完成", new { table, fields, where, limit });
        
        return new
        {
            success = true,
            data = new[]
            {
                new { id = 1, name = "示例数据1", email = "user1@example.com" },
                new { id = 2, name = "示例数据2", email = "user2@example.com" }
            },
            total = 2,
            query = new { table, fields, where, limit }
        };
    }

    [McpTool("file_operations", "文件操作（读取、写入、删除）")]
    public async Task<object> FileOperationsAsync(string operation, string path, string? content, McpContext ctx)
    {
        await ctx.InfoAsync($"开始文件操作: {operation} {path}");
        
        switch (operation.ToLower())
        {
            case "read":
                await ctx.ReportProgressAsync("读取文件", 50);
                await Task.Delay(300);
                return new { success = true, content = $"文件 {path} 的内容", operation };
                
            case "write":
                await ctx.ReportProgressAsync("写入文件", 50);
                await Task.Delay(300);
                return new { success = true, message = $"文件 {path} 写入成功", operation, content };
                
            case "delete":
                await ctx.ReportProgressAsync("删除文件", 50);
                await Task.Delay(300);
                return new { success = true, message = $"文件 {path} 删除成功", operation };
                
            default:
                throw new ArgumentException($"不支持的文件操作: {operation}");
        }
    }

    [McpTool("data_analysis", "数据分析工具")]
    public async Task<object> DataAnalysisAsync(string dataSource, string analysisType, object parameters, McpContext ctx)
    {
        await ctx.InfoAsync($"开始数据分析: {dataSource} - {analysisType}");
        
        await ctx.ReportProgressAsync("加载数据", 20);
        await Task.Delay(200);
        
        await ctx.ReportProgressAsync("预处理数据", 40);
        await Task.Delay(200);
        
        await ctx.ReportProgressAsync("执行分析", 70);
        await Task.Delay(300);
        
        await ctx.ReportProgressAsync("生成报告", 90);
        await Task.Delay(100);
        
        var summary = await ctx.SampleAsync($"请总结 {dataSource} 的 {analysisType} 分析结果");
        
        return new
        {
            success = true,
            dataSource,
            analysisType,
            summary,
            insights = new[] { "洞察1: 数据趋势上升", "洞察2: 存在异常值", "洞察3: 相关性较强" },
            parameters
        };
    }

    [McpTool("code_generation", "代码生成工具")]
    public async Task<object> CodeGenerationAsync(string language, string template, object parameters, McpContext ctx)
    {
        await ctx.InfoAsync($"开始生成 {language} 代码");
        
        await ctx.ReportProgressAsync("分析模板", 25);
        await Task.Delay(200);
        
        await ctx.ReportProgressAsync("生成代码", 60);
        await Task.Delay(400);
        
        await ctx.ReportProgressAsync("代码优化", 85);
        await Task.Delay(200);
        
        await ctx.ReportProgressAsync("生成完成", 100);
        
        var generatedCode = await ctx.SampleAsync($"请生成 {language} 代码，模板: {template}");
        
        return new
        {
            success = true,
            language,
            template,
            code = generatedCode,
            parameters
        };
    }

    [McpResource("database://tables", "数据库表列表", "获取所有可用的数据库表")]
    public async Task<object> GetDatabaseTablesAsync(McpContext ctx)
    {
        await ctx.InfoAsync("获取数据库表列表");
        
        return new
        {
            tables = new[]
            {
                new { name = "Users", description = "用户表", columns = 5 },
                new { name = "Orders", description = "订单表", columns = 8 },
                new { name = "Products", description = "产品表", columns = 6 }
            }
        };
    }

    [McpResource("file://templates", "文件模板", "获取可用的文件模板")]
    public async Task<object> GetFileTemplatesAsync(McpContext ctx)
    {
        await ctx.InfoAsync("获取文件模板列表");
        
        return new
        {
            templates = new[]
            {
                new { name = "basic", description = "基础模板", content = "基础内容" },
                new { name = "advanced", description = "高级模板", content = "高级内容" }
            }
        };
    }

    [McpPrompt("summarize_request", "生成摘要请求提示")]
    public string SummarizeRequest(string text)
    {
        return $"请总结以下文本:\n\n{text}";
    }
} 