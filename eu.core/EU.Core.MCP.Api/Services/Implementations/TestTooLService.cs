using ClaudeMCP.API.Models.Mcp;
using ClaudeMCP.API.Services.Interfaces;
using System.Text.Json;

namespace ClaudeMCP.API.Services.Implementations;

public class TestToolService : IToolService
{
    private readonly ILogger<TestToolService> _logger;

    public TestToolService(ILogger<TestToolService> logger)
    {
        _logger = logger;
    }

    public IEnumerable<McpTool> GetTools()
    {
        return new[]
        {
            new McpTool
            {
                Name = "test_hello",
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
        return toolName == "test_hello";
    }

    public async Task<McpToolResult> ExecuteToolAsync(string toolName, JsonElement arguments)
    {
        _logger.LogInformation($"Executing test tool: {toolName}");
        
        return toolName switch
        {
            "test_hello" => await HandleTestHello(arguments),
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
                    Text = $"Hello, {name}! MCP server is working! 🎉"
                }
            }
        };
    }
}