using SqlSugar;
using System.Reflection;

namespace EU.Core.Api.MCP.Interfaces;

public class BaseService<TService, TEntity> : IBaseService where TService : class where TEntity : class
{
    private readonly ILogger<BaseService<TService, TEntity>> _logger;
    private readonly Dictionary<string, MethodInfo> _toolMethods;
    private readonly Lazy<TService> _serviceInstance;

    public IBaseRepository<TEntity> BaseDal { get; }

    public BaseService(ILogger<BaseService<TService, TEntity>> logger, IBaseRepository<TEntity> baseDal)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        BaseDal = baseDal ?? throw new ArgumentNullException(nameof(baseDal));
        _toolMethods = new Dictionary<string, MethodInfo>();
        _serviceInstance = new Lazy<TService>(() =>
        {
            if (this is TService service)
            {
                DiscoverMcpMethods();
                return service;
            }
            throw new InvalidOperationException($"Service must implement {typeof(TService).Name}");
        });
    }

    protected TService ServiceInstance => _serviceInstance.Value;

    public ISqlSugarClient Db => BaseDal.Db;

    private void DiscoverMcpMethods()
    {
        var type = typeof(TService);

        // 发现工具方法
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        foreach (var method in methods)
        {
            var toolAttr = method.GetCustomAttribute<McpToolAttribute>();
            if (toolAttr != null)
            {
                var name = string.IsNullOrEmpty(toolAttr.Name) ? method.Name : toolAttr.Name;
                _toolMethods.TryAdd(name, method);
                _logger.LogInformation("发现工具: {ToolName}", name);
            }
        }
    }

    public virtual object HandleInitialize(JsonElement? parameters)
    {
        _logger.LogInformation("MCP server initialized");
        return new
        {
            protocolVersion = "2024-11-05",
            capabilities = new
            {
                tools = new { }
            }
            ,
            serverInfo = new McpServerInfo
            {
                Name = "EU.Core FastMCP Server",
                Version = "1.0.0"
            }
        };
    }

    public virtual object GetAvailableTools()
    {
        var allTools = GetTools().ToArray();

        _logger.LogInformation("Returning {ToolCount} available tools", allTools.Length);
        return new { tools = allTools };
    }

    public virtual async Task<McpToolResult> HandleToolCallAsync(JsonElement? parameters)
    {
        if (parameters == null)
            throw new ArgumentException("Missing parameters");

        if (!parameters.Value.TryGetProperty("name", out var nameElement))
            throw new ArgumentException("Tool name is required");

        var toolName = nameElement.GetString();
        if (string.IsNullOrEmpty(toolName))
            throw new ArgumentException("Tool name cannot be empty");

        var arguments = parameters.Value.TryGetProperty("arguments", out var args) ? args : default;

        _logger.LogInformation("Executing tool: {ToolName}", toolName);

        if (!CanHandle(toolName))
            throw new ArgumentException($"No service found for tool: {toolName}");

        var dynamicObject = ConvertToDynamic(arguments);
        return await ExecuteToolAsync(toolName, arguments, dynamicObject);
    }

    public static dynamic? ConvertToDynamic(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ConvertObjectToDynamic(element),
            JsonValueKind.Array => ConvertArrayToDynamic(element),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => ConvertNumberToDynamic(element),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => element.GetRawText()
        };
    }

    private static dynamic ConvertObjectToDynamic(JsonElement element)
    {
        dynamic expando = new ExpandoObject();
        var expandoDict = (IDictionary<string, object?>)expando;
        foreach (var property in element.EnumerateObject())
        {
            expandoDict[property.Name] = ConvertToDynamic(property.Value);
        }
        return expando;
    }

    private static List<dynamic?> ConvertArrayToDynamic(JsonElement element)
    {
        var list = new List<dynamic?>();
        foreach (var item in element.EnumerateArray())
        {
            list.Add(ConvertToDynamic(item));
        }
        return list;
    }

    private static dynamic ConvertNumberToDynamic(JsonElement element)
    {
        if (element.TryGetInt32(out int intValue)) return intValue;
        if (element.TryGetInt64(out long longValue)) return longValue;
        return element.GetDouble();
    }

    public virtual IEnumerable<McpTool> GetTools()
    {
        _ = ServiceInstance; // 确保服务实例已初始化

        return _toolMethods.Select(kvp =>
        {
            var method = kvp.Value;
            var toolAttr = method.GetCustomAttribute<McpToolAttribute>();

            return new McpTool
            {
                Name = kvp.Key,
                Description = toolAttr?.Description ?? $"Tool: {method.Name}",
                InputSchema = toolAttr?.InputSchema ?? new
                {
                    type = "object",
                    properties = new { }
                }
            };
        });
    }

    public bool CanHandle(string toolName)
    {
        if (string.IsNullOrEmpty(toolName))
            return false;

        _ = ServiceInstance; // 确保服务实例已初始化
        return _toolMethods.ContainsKey(toolName);
    }

    public virtual async Task<McpToolResult> ExecuteToolAsync(string toolName, JsonElement arguments, dynamic? dynamicObject)
    {
        _logger.LogInformation("Executing tool: {ToolName}", toolName);

        if (!_toolMethods.TryGetValue(toolName, out var method))
        {
            throw new ArgumentException($"Unknown tool: {toolName}");
        }

        try
        {
            // 动态调用服务方法
            var result = method.Invoke(ServiceInstance, [dynamicObject]);

            // 处理异步方法
            if (result is Task task)
            {
                await task;
                result = GetTaskResult(task);
            }

            return result as McpToolResult ??
                   throw new InvalidOperationException($"Method {toolName} did not return McpToolResult");
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            _logger.LogError(ex.InnerException, "Error executing tool {ToolName}", toolName);
            throw new InvalidOperationException($"Error executing tool {toolName}: {ex.InnerException.Message}", ex.InnerException);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
            throw new InvalidOperationException($"Error executing tool {toolName}: {ex.Message}", ex);
        }
    }

    private static object? GetTaskResult(Task task)
    {
        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty?.GetValue(task);
    }



    #region 辅助方法
    /// <summary>
    /// 创建标准的错误响应
    /// </summary>
    /// <param name="message">消息</param>
    /// <returns></returns>
    public static McpToolResult CreateErrorResult(string message)
    {
        return new McpToolResult
        {
            Content =
            [
                new McpContent
                {
                    Type = "text",
                    Text = message
                }
            ]
        };
    }

    /// <summary>
    /// 创建模块响应
    /// </summary>
    /// <param name="moudleCode">模块代码</param>
    /// <param name="type">类型</param>
    /// <param name="id">数据ID</param>
    /// <returns></returns>
    public static McpToolResult CreateModuleResult(string moudleCode, string type, Guid? id = null)
    {
        var result = new
        {
            type,
            moudleCode
        };

        var resultWithId = id.HasValue ? new
        {
            type,
            id = id.Value,
            moudleCode
        } : (object)result;

        return new McpToolResult
        {
            Content =
            [
                new McpContent
                {
                    Type = "text",
                    Text = JsonHelper.ObjToJson(resultWithId)
                }
            ]
        };
    }

    #endregion
}