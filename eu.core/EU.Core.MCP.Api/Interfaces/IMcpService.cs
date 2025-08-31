using EU.Core.Api.MCP.Models.Mcp;
using System.Text.Json;

namespace EU.Core.Api.MCP.Interfaces;

public interface IMcpService
{
    /// <summary>
    /// Handle MCP initialization
    /// </summary>
    object HandleInitialize(JsonElement? parameters);
    
    /// <summary>
    /// Get all available tools
    /// </summary>
    object GetAvailableTools();
    
    /// <summary>
    /// Execute a tool call
    /// </summary>
    Task<McpToolResult> HandleToolCallAsync(JsonElement? parameters);
}