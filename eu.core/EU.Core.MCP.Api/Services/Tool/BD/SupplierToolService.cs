using ClaudeMCP.API.Models.Mcp;
using ClaudeMCP.API.Interfaces;
using System.Text.Json;

namespace ClaudeMCP.API.Services.Tool;

public class SupplierToolService : ISupplierToolService
{
    private readonly ILogger<SupplierToolService> _logger;

    public SupplierToolService(ILogger<SupplierToolService> logger)
    {
        _logger = logger;
    }

    public IEnumerable<McpTool> GetTools()
    {
        return new[]
        {
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
        };
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
                    Text = $"Hello, {name},{DateTime.Now}! Supplier MCP server is working! 🎉"
                }
            }
        };
    }
}