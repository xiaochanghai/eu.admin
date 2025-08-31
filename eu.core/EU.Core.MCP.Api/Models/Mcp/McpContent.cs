using System.Text.Json;
using System.Text.Json.Serialization;

namespace EU.Core.Api.MCP.Models.Mcp;

public class McpContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";
    
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
}