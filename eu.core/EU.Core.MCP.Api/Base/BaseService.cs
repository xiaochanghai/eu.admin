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
    private IServiceBase? _serviceInstance; // 移除 readonly

    public BaseService(ILogger<BaseService<IServiceBase>> logger)
    {
        _logger = logger;
        _toolMethods = new Dictionary<string, MethodInfo>();
    }

    // 添加一个受保护的方法来设置服务实例
    protected void InitializeService(IServiceBase serviceInstance)
    {
        _serviceInstance = serviceInstance;
       
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
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Name to greet" }
                    }
                }
            });
        }

        return tools;
    }

    public bool CanHandle(string toolName)=> _toolMethods.ContainsKey(toolName);

    public virtual async Task<McpToolResult> ExecuteToolAsync(string toolName, JsonElement arguments)
    {
        _logger.LogInformation($"Executing tool: {toolName}");

        if (!_toolMethods.TryGetValue(toolName, out var method))
        {
            throw new ArgumentException($"Unknown tool: {toolName}");
        }

        try
        {
            // 动态调用服务方法
            var result = method.Invoke(_serviceInstance, [arguments]);

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