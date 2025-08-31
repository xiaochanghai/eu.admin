using System.Text.Json;
using System.Text.Json.Serialization;

namespace EU.Core.Api.MCP.Models.Mcp; 

public class McpToolResult
{
    [JsonPropertyName("content")]
    public McpContent[] Content { get; set; } = Array.Empty<McpContent>();
}