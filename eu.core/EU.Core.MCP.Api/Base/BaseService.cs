using EU.Core.Api.MCP.Attributes;
using EU.Core.Api.MCP.Models.Mcp;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EU.Core.Api.MCP.Interfaces;

public class BaseService<IServiceBase> : IBaseService
{
    private readonly ILogger<BaseService<IServiceBase>> _logger;
    private readonly Dictionary<string, MethodInfo> _toolMethods;

    public BaseService(ILogger<BaseService<IServiceBase>> logger)
    {
        _logger = logger;
        _toolMethods = new Dictionary<string, MethodInfo>();

        // 自动发现工具、资源和提示
        DiscoverMcpMethods();
    }


    private void DiscoverMcpMethods()
    {
        var type = typeof(IServiceBase);

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

        return await ExecuteToolAsync(toolName, arguments);
    }

    public virtual IEnumerable<McpTool> GetTools()
    {
        return
        [
            new McpTool
            {
                Name = "test_hello_Supplier",
                Description = "A simple test tool that says hello",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Name to greet" }
                    }
                }
            }
        ];
    }


    public bool CanHandle(string toolName)
    {
        var tools = GetTools();
        return tools.Any(x => x.Name == toolName); ;
    }


    public virtual async Task<McpToolResult> ExecuteToolAsync(string toolName, JsonElement arguments)
    {
        _logger.LogInformation($"Executing test tool: {toolName}");

        return toolName switch
        {
            "test_hello_Supplier" => await HandleTestHello(arguments),
            _ => throw new ArgumentException($"Unknown tool: {toolName}")
        };
    }

    [McpTool("test_hello_Supplier", "A simple test supplier tool that says hello")]
    public async Task<McpToolResult> HandleTestHello(JsonElement arguments)
    {
        await Task.Delay(10); // Simulate async work

        var name = "World";
        if (arguments.ValueKind != JsonValueKind.Undefined &&
            arguments.TryGetProperty("name", out var nameProperty))
        {
            name = nameProperty.GetString() ?? "World";
        }

        return new McpToolResult
        {
            Content = new[]
            {
                new McpContent
                {
                    Type = "text",
                    Text = $"Hello, {name},{DateTime.Now}! Supplier MCP server is working! "
                }
            }
        };
    }

    ///// <summary>
    ///// 反射调用service方法
    ///// </summary>
    ///// <param name="methodName"></param>
    ///// <param name="parameters"></param>
    ///// <returns></returns>
    //[NonAction]
    //private object InvokeService(string methodName, object[] parameters)
    //{
    //    return _service.GetType().GetMethod(methodName).Invoke(_service, parameters);
    //}


    //[NonAction]
    //private async Task<object> InvokeServiceAsync(string methodName, object[] parameters)
    //{
    //    var task = _service.GetType().InvokeMember(methodName, BindingFlags.InvokeMethod, null, _service, parameters) as Task;
    //    if (task != null) await task;
    //    var result = task?.GetType().GetProperty("Result")?.GetValue(task);
    //    return result;
    //}
}