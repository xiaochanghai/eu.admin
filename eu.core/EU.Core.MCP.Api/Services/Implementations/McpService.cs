using ClaudeMCP.API.Models.Mcp;
using ClaudeMCP.API.Interfaces;
using System.Text.Json;

namespace ClaudeMCP.API.Services.Implementations;

public class McpService : IMcpService
{
    private readonly IEnumerable<IToolService> _toolServices;
    private readonly ILogger<McpService> _logger;

    public McpService(IEnumerable<IToolService> toolServices, ILogger<McpService> logger)
    {
        _toolServices = toolServices;
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
        var allTools = _toolServices
            .SelectMany(service => service.GetTools())
            .ToArray();
            
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
        var toolService = _toolServices.FirstOrDefault(service => service.CanHandle(toolName));
        
        if (toolService == null)
            throw new ArgumentException($"No service found for tool: {toolName}");

        return await toolService.ExecuteToolAsync(toolName, arguments);
    }
}