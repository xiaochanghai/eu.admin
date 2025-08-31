using EU.Core.Api.MCP.Models.Mcp;
using EU.Core.Api.MCP.Interfaces;
using System.Text.Json;
using EU.Core.Api.MCP.Attributes;

namespace EU.Core.Api.MCP.Services.Implementations;

public class SupplierService : BaseService<SupplierService>, ISupplierService
{  

    public SupplierService(ILogger<SupplierService> logger) : base( logger)
    {  
    }

    [McpTool("test_hello_Supplier2", "A simple test supplier tool that says hello")]
    public async Task<McpToolResult> HandleTestHello1(JsonElement arguments)
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

    //public object GetAvailableTools()
    //{
    //    var allTools = GetTools().ToArray();

    //    _logger.LogInformation($"Returning {allTools.Length} available tools");
    //    return new { tools = allTools };
    //}

    //public async Task<McpToolResult> HandleToolCallAsync(JsonElement? parameters)
    //{
    //    if (parameters == null)
    //        throw new ArgumentException("Missing parameters");

    //    var toolName = parameters.Value.GetProperty("name").GetString();
    //    var arguments = parameters.Value.TryGetProperty("arguments", out var args) ? args : default;

    //    if (string.IsNullOrEmpty(toolName))
    //        throw new ArgumentException("Tool name is required");

    //    _logger.LogInformation($"Executing tool: {toolName}");

    //    // Find the service that can handle this tool
    //    var isExist = CanHandle(toolName);

    //    if (!isExist)
    //        throw new ArgumentException($"No service found for tool: {toolName}");

    //    return await ExecuteToolAsync(toolName, arguments);
    //}

    //public IEnumerable<McpTool> GetTools()
    //{
    //    return
    //    [
    //        new McpTool
    //        {
    //            Name = "test_hello_Supplier",
    //            Description = "A simple test tool that says hello",
    //            InputSchema = new
    //            {
    //                type = "object",
    //                properties = new
    //                {
    //                    name = new { type = "string", description = "Name to greet" }
    //                }
    //            }
    //        }
    //    ];
    //}

    //public bool CanHandle(string toolName)
    //{
    //    var tools = GetTools();
    //    return tools.Any(x => x.Name == toolName); ;
    //}


    //public async Task<McpToolResult> ExecuteToolAsync(string toolName, JsonElement arguments)
    //{
    //    _logger.LogInformation($"Executing test tool: {toolName}");

    //    return toolName switch
    //    {
    //        "test_hello_Supplier" => await HandleTestHello(arguments),
    //        _ => throw new ArgumentException($"Unknown tool: {toolName}")
    //    };
    //}

    //private async Task<McpToolResult> HandleTestHello(JsonElement arguments)
    //{
    //    await Task.Delay(10); // Simulate async work

    //    var name = "World";
    //    if (arguments.ValueKind != JsonValueKind.Undefined &&
    //        arguments.TryGetProperty("name", out var nameProperty))
    //    {
    //        name = nameProperty.GetString() ?? "World";
    //    }

    //    return new McpToolResult
    //    {
    //        Content = new[]
    //        {
    //            new McpContent
    //            {
    //                Type = "text",
    //                Text = $"Hello, {name},{DateTime.Now}! Supplier MCP server is working! "
    //            }
    //        }
    //    };
    //}
}