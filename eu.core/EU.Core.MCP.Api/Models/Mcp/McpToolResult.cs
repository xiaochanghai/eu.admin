using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClaudeMCP.API.Models.Mcp; 

public class McpToolResult
{
    [JsonPropertyName("content")]
    public McpContent[] Content { get; set; } = Array.Empty<McpContent>();
}