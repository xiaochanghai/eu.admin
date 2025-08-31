using System.Text.Json;
using System.Text.Json.Serialization;

namespace EU.Core.Api.MCP.Models.Mcp;

public class McpTool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
    
    [JsonPropertyName("inputSchema")]
    public object InputSchema { get; set; } = new { type = "object", properties = new { } };
}