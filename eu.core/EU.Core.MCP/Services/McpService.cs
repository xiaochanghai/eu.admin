using EU.Core.MCP.Interfaces;
using EU.Core.MCP.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EU.Core.MCP.Services;

/// <summary>
/// MCP 服务实现 - 支持流式操作
/// </summary>
public class McpService : IMcpService
{
    private readonly ILogger<McpService> _logger;
    private readonly Dictionary<string, Func<object, Task<object>>> _toolHandlers;
    private readonly Dictionary<string, Func<string, Task<object>>> _resourceHandlers;

    public McpService(ILogger<McpService> logger)
    {
        _logger = logger;
        _toolHandlers = new Dictionary<string, Func<object, Task<object>>>();
        _resourceHandlers = new Dictionary<string, Func<string, Task<object>>>();

        InitializeToolHandlers();
        InitializeResourceHandlers();
    }

    private void InitializeToolHandlers()
    {
        // 注册数据库查询工具
        _toolHandlers["query_database"] = async (parameters) =>
        {
            var param = JsonSerializer.Deserialize<DatabaseQueryParams>(JsonSerializer.Serialize(parameters));
            return await QueryDatabaseAsync(param);
        };

        // 注册文件操作工具
        _toolHandlers["file_operations"] = async (parameters) =>
        {
            var param = JsonSerializer.Deserialize<FileOperationParams>(JsonSerializer.Serialize(parameters));
            return await HandleFileOperationsAsync(param);
        };

        // 注册业务逻辑工具
        _toolHandlers["business_logic"] = async (parameters) =>
        {
            var param = JsonSerializer.Deserialize<BusinessLogicParams>(JsonSerializer.Serialize(parameters));
            return await ExecuteBusinessLogicAsync(param);
        };
    }

    private void InitializeResourceHandlers()
    {
        // 注册数据库资源处理器
        _resourceHandlers["database://"] = async (uri) =>
        {
            return await GetDatabaseResourceAsync(uri);
        };

        // 注册文件资源处理器
        _resourceHandlers["file://"] = async (uri) =>
        {
            return await GetFileResourceAsync(uri);
        };
    }

    public async Task<List<McpTool>> GetToolsAsync()
    {
        return new List<McpTool>
        {
            new McpTool
            {
                Name = "query_database",
                Description = "查询数据库数据",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        table = new { type = "string", description = "表名" },
                        fields = new { type = "array", items = new { type = "string" } },
                        where = new { type = "string", description = "查询条件" },
                        limit = new { type = "integer", description = "限制条数" }
                    }
                }
            },
            new McpTool
            {
                Name = "file_operations",
                Description = "文件操作（读取、写入、删除）",
                InputSchema = new
                {
                    type = "object",
                    //properties = new
                    //{
                    //    operation = new { type = "string", enum = new[] { "read", "write", "delete" } },
                    //    path = new { type = "string", description = "文件路径" },
                    //    content = new { type = "string", description = "文件内容（写入时）" }
                    //}
                }
            },
            new McpTool
            {
                Name = "business_logic",
                Description = "执行业务逻辑",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        module = new { type = "string", description = "业务模块" },
                        action = new { type = "string", description = "操作类型" },
                        data = new { type = "object", description = "业务数据" }
                    }
                }
            }
        };
    }

    public async Task<List<McpResource>> GetResourcesAsync()
    {
        return new List<McpResource>
        {
            new McpResource
            {
                Uri = "database://tables",
                Name = "数据库表列表",
                Description = "获取所有可用的数据库表",
                MimeType = "application/json"
            },
            new McpResource
            {
                Uri = "file://templates",
                Name = "文件模板",
                Description = "获取可用的文件模板",
                MimeType = "application/json"
            }
        };
    }

    public async Task<object> CallToolAsync(string toolName, object parameters)
    {
        if (_toolHandlers.TryGetValue(toolName, out var handler))
        {
            try
            {
                return await handler(parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "工具 {ToolName} 执行失败", toolName);
                throw;
            }
        }

        throw new ArgumentException($"未知的工具: {toolName}");
    }

    public async Task<object> GetResourceAsync(string uri)
    {
        foreach (var handler in _resourceHandlers)
        {
            if (uri.StartsWith(handler.Key))
            {
                return await handler.Value(uri);
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
                case "tools/list":
                    var tools = await GetToolsAsync();
                    return new McpResponse { Id = request.Id, result = tools };

                case "tools/call":
                    var callParams = JsonSerializer.Deserialize<ToolCallParams>(JsonSerializer.Serialize(request.Params));
                    if (callParams != null)
                    {
                        var result = await CallToolAsync(callParams.Name, callParams.Arguments);
                        return new McpResponse { Id = request.Id, result = result };
                    }
                    break;

                case "resources/list":
                    var resources = await GetResourcesAsync();
                    return new McpResponse { Id = request.Id, result = resources };

                case "resources/read":
                    var readParams = JsonSerializer.Deserialize<ResourceReadParams>(JsonSerializer.Serialize(request.Params));
                    if (readParams != null)
                    {
                        var resourceData = await GetResourceAsync(readParams.Uri);
                        return new McpResponse { Id = request.Id, result = resourceData };
                    }
                    break;

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

            // 如果到达这里，说明参数反序列化失败
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

    // 工具实现方法
    private async Task<object> QueryDatabaseAsync(DatabaseQueryParams parameters)
    {
        // 这里集成您现有的数据库服务
        _logger.LogInformation("查询数据库: {Table}, 条件: {Where}", parameters.Table, parameters.Where);

        // 返回模拟数据，实际应该调用您的数据库服务
        return new
        {
            success = true,
            data = new[] { new { id = 1, name = "示例数据" } },
            total = 1
        };
    }

    private async Task<object> HandleFileOperationsAsync(FileOperationParams parameters)
    {
        _logger.LogInformation("文件操作: {Operation} {Path}", parameters.Operation, parameters.Path);

        switch (parameters.Operation.ToLower())
        {
            case "read":
                return new { success = true, content = "文件内容" };
            case "write":
                return new { success = true, message = "文件写入成功" };
            case "delete":
                return new { success = true, message = "文件删除成功" };
            default:
                throw new ArgumentException($"不支持的文件操作: {parameters.Operation}");
        }
    }

    private async Task<object> ExecuteBusinessLogicAsync(BusinessLogicParams parameters)
    {
        _logger.LogInformation("执行业务逻辑: {Module} {Action}", parameters.Module, parameters.Action);

        // 这里集成您现有的业务服务
        return new
        {
            success = true,
            result = $"业务逻辑 {parameters.Module}.{parameters.Action} 执行成功",
            data = parameters.Data
        };
    }

    private async Task<object> GetDatabaseResourceAsync(string uri)
    {
        // 返回数据库相关资源信息
        return new
        {
            tables = new[] { "Users", "Orders", "Products" },
            schemas = new { Users = new { id = "int", name = "string" } }
        };
    }

    private async Task<object> GetFileResourceAsync(string uri)
    {
        // 返回文件相关资源信息
        return new
        {
            templates = new[] { "user_template.xlsx", "order_template.xlsx" },
            formats = new[] { "xlsx", "csv", "json" }
        };
    }

    public async Task<object> CallToolWithProgressAsync(string toolName, object parameters,
        Action<string> progressCallback, CancellationToken cancellationToken = default)
    {
        if (_toolHandlers.TryGetValue(toolName, out var handler))
        {
            try
            {
                progressCallback($"开始执行工具: {toolName}");

                for (int i = 0; i < 10; i++)
                {
                    Thread.Sleep(1000); // 暂停1000毫秒（1秒）
                    progressCallback($"测试id: " + Guid.NewGuid());

                }
                progressCallback($"开始执行工具: {toolName}");

                // 模拟长时间运行的工具
                if (toolName == "long_running_tool")
                {
                    return await ExecuteLongRunningToolAsync(parameters, progressCallback, cancellationToken);
                }

                var result = await handler(parameters);
                progressCallback($"工具 {toolName} 执行完成");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "工具 {ToolName} 执行失败", toolName);
                progressCallback($"工具 {toolName} 执行失败: {ex.Message}");
                throw;
            }
        }

        throw new ArgumentException($"未知的工具: {toolName}");
    }

    public async IAsyncEnumerable<McpStreamEvent> CallToolStreamAsync(string toolName, object parameters,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_toolHandlers.TryGetValue(toolName, out var handler))
        {
            //try
            //{
            yield return new McpStreamEvent
            {
                EventType = "tool_started",
                Data = $"开始执行工具: {toolName}",
                Id = Guid.NewGuid().ToString()
            };

            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(1000); // 暂停1000毫秒（1秒）
                yield return new McpStreamEvent
                {
                    EventType = "tool_result",
                    Data = $"测试id: " + Guid.NewGuid(),
                    Id = Guid.NewGuid().ToString()
                };

            }

            // 模拟流式数据生成
            if (toolName == "streaming_tool")
            {
                await foreach (var item in GenerateStreamingDataAsync(cancellationToken))
                {
                    yield return new McpStreamEvent
                    {
                        EventType = "tool_progress",
                        Data = item,
                        Id = Guid.NewGuid().ToString()
                    };
                }
            }
            else
            {
                var result = await handler(parameters);
                yield return new McpStreamEvent
                {
                    EventType = "tool_result",
                    Data = result,
                    Id = Guid.NewGuid().ToString()
                };
            }

            yield return new McpStreamEvent
            {
                EventType = "tool_completed",
                Data = $"工具 {toolName} 执行完成",
                Id = Guid.NewGuid().ToString()
            };
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "工具 {ToolName} 执行失败", toolName);
            //    yield return new McpStreamEvent 
            //    { 
            //        EventType = "tool_error", 
            //        Data = ex.Message,
            //        Id = Guid.NewGuid().ToString()
            //    };
            //}
        }
        else
        {
            yield return new McpStreamEvent
            {
                EventType = "tool_error",
                Data = $"未知的工具: {toolName}",
                Id = Guid.NewGuid().ToString()
            };
        }
    }

    // 模拟长时间运行的工具
    private async Task<object> ExecuteLongRunningToolAsync(object parameters, Action<string> progressCallback, CancellationToken cancellationToken)
    {
        for (int i = 0; i <= 10; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progressCallback($"处理进度: {i * 10}%");
            await Task.Delay(1000, cancellationToken); // 模拟工作
        }

        return new { success = true, message = "长时间运行的工具执行完成" };
    }

    // 模拟流式数据生成
    private async IAsyncEnumerable<object> GenerateStreamingDataAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < 10; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new { index = i, data = $"流式数据 {i}", timestamp = DateTime.UtcNow };
            await Task.Delay(500, cancellationToken); // 模拟数据生成延迟
        }
    }
}