using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClaudeMCP.API.Models.Mcp;

public class McpContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";
    
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
}