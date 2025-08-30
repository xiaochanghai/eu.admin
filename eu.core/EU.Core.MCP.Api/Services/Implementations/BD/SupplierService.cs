using ClaudeMCP.API.Models.Mcp;
using ClaudeMCP.API.Interfaces;
using System.Text.Json;

namespace ClaudeMCP.API.Services.Implementations;

public class SupplierService : ISupplierService
{ 
    private readonly ILogger<McpService> _logger;

    public SupplierService( ILogger<McpService> logger)
    { 
        _logger = logger;
    }

    public object HandleInitialize(JsonElement? parameters)
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

    public object GetAvailableTools()
    {
        var allTools = GetTools().ToArray();

        _logger.LogInformation($"Returning {allTools.Length} available tools");
        return new { tools = allTools };
    }

    public async Task<McpToolResult> HandleToolCallAsync(JsonElement? parameters)
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

    public IEnumerable<McpTool> GetTools()
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


    public async Task<McpToolResult> ExecuteToolAsync(string toolName, JsonElement arguments)
    {
        _logger.LogInformation($"Executing test tool: {toolName}");

        return toolName switch
        {
            "test_hello_Supplier" => await HandleTestHello(arguments),
            _ => throw new ArgumentException($"Unknown tool: {toolName}")
        };
    }

    private async Task<McpToolResult> HandleTestHello(JsonElement arguments)
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
}