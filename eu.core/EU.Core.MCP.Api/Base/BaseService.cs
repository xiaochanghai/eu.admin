using EU.Core.Api.MCP.Attributes;
using EU.Core.Api.MCP.Models.Mcp;
using EU.Core.IRepository.Base;
using SqlSugar;
using System.Dynamic;
using System.Reflection;
using System.Text.Json;

namespace EU.Core.Api.MCP.Interfaces;
 
public class BaseService<TService, TEntity> : IBaseService where TService : class where TEntity : class
{
    private readonly ILogger<BaseService<TService, TEntity>> _logger;
    private readonly Dictionary<string, MethodInfo> _toolMethods; 
    private readonly Lazy<TService> _serviceInstance;

    public  IBaseRepository<TEntity> BaseDal { get; set; } //通过在子类的构造函数中注入，这里是基类，不用构造函数
    public BaseService(ILogger<BaseService<TService, TEntity>> logger,IBaseRepository<TEntity> _baseDal)
    {
        _logger = logger;
        BaseDal = _baseDal;
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
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            var toolAttr = method.GetCustomAttribute<McpToolAttribute>();
            if (toolAttr != null)
            {
                var name = string.IsNullOrEmpty(toolAttr.Name) ? method.Name : toolAttr.Name;
                _toolMethods[name] = method;
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

        _logger.LogInformation($"Returning {allTools.Length} available tools");
        return new { tools = allTools };
    }

    public virtual async Task<McpToolResult> HandleToolCallAsync(JsonElement? parameters)
    {
        if (parameters == null)
            throw new ArgumentException("Missing parameters");

        var toolName = parameters.Value.GetProperty("name").GetString();
        var arguments = parameters.Value.TryGetProperty("arguments", out var args) ? args : default;

        if (string.IsNullOrEmpty(toolName))
            throw new ArgumentException("Tool name is required");

        _logger.LogInformation($"Executing tool: {toolName}");

        // Find the service that can handle this tool
        var isExist = CanHandle(toolName);

        if (!isExist)
            throw new ArgumentException($"No service found for tool: {toolName}");
        var dynamicObject = ConvertToDynamic(arguments);
        return await ExecuteToolAsync(toolName, arguments, dynamicObject);
    }

    public static dynamic? ConvertToDynamic(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                // 对于对象，创建一个 ExpandoObject
                dynamic expando = new ExpandoObject();
                var expandoDict = (IDictionary<string, object?>)expando;
                foreach (var property in element.EnumerateObject())
                {
                    // 递归转换属性值并添加到字典中
                    expandoDict[property.Name] = ConvertToDynamic(property.Value);
                }
                return expando;

            case JsonValueKind.Array:
                // 对于数组，创建一个 List<dynamic>
                var list = new List<dynamic?>();
                foreach (var item in element.EnumerateArray())
                {
                    // 递归转换数组项并添加到列表中
                    list.Add(ConvertToDynamic(item));
                }
                return list;

            // 对于基础类型，直接返回对应的 C# 值
            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                // 尝试获取不同精度的数字
                if (element.TryGetInt32(out int intValue)) return intValue;
                if (element.TryGetInt64(out long longValue)) return longValue;
                return element.GetDouble(); // 默认为 double

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;

            default:
                // 对于任何其他类型，返回其原始文本
                return element.GetRawText();
        }
    }

    public virtual IEnumerable<McpTool> GetTools()
    {
        _ = ServiceInstance;
        var tools = new List<McpTool>();

        // 动态生成工具列表
        foreach (var kvp in _toolMethods)
        {
            var method = kvp.Value;
            var toolAttr = method.GetCustomAttribute<McpToolAttribute>();

            tools.Add(new McpTool
            {
                Name = kvp.Key,
                Description = toolAttr?.Description ?? $"Tool: {method.Name}",
                InputSchema = toolAttr?.InputSchema ?? new
                {
                    type = "object",
                    properties = new
                    {
                    }
                }
                //InputSchema = new
                //{
                //    type = "object",
                //    properties = new
                //    {
                //        name = new { type = "string", description = "Name to greet" },
                //        name1 = new { type = "string", description = "Name to greet" }
                //    }
                //}
            });
        }

        return tools;
    }

    public bool CanHandle(string toolName)
    {
        _ = ServiceInstance;
        return _toolMethods.ContainsKey(toolName);
    }

    public virtual async Task<McpToolResult> ExecuteToolAsync(string toolName, JsonElement arguments, dynamic? dynamicObject)
    {
        _logger.LogInformation($"Executing tool: {toolName}");

        if (!_toolMethods.TryGetValue(toolName, out var method))
        {
            throw new ArgumentException($"Unknown tool: {toolName}");
        }

        try
        {
            // 动态调用服务方法
            var result = method.Invoke(ServiceInstance, [dynamicObject]);

            // 如果方法是异步的，等待完成
            if (result is Task task)
            {
                await task;

                // 获取Task的结果
                var resultProperty = task.GetType().GetProperty("Result");
                if (resultProperty != null)
                {
                    result = resultProperty.GetValue(task);
                }
            }

            return result as McpToolResult ?? throw new InvalidOperationException($"Method {toolName} did not return McpToolResult");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error executing tool {toolName}");
            throw new InvalidOperationException($"Error executing tool {toolName}: {ex.Message}", ex);
        }
    }
}