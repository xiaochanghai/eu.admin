using EU.Core.Api.MCP.Models.Mcp;
using System.Text.Json;

namespace EU.Core.Api.MCP.Interfaces;

public interface IToolService
{
    /// <summary>
    /// Get all tools provided by this service
    /// </summary>
    IEnumerable<McpTool> GetTools();
    
    /// <summary>
    /// Execute a specific tool
    /// </summary>
    Task<McpToolResult> ExecuteToolAsync(string toolName, JsonElement arguments);
    
    /// <summary>
    /// Check if this service can handle the given tool
    /// </summary>
    bool CanHandle(string toolName);
}