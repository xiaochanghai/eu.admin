using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClaudeMCP.API.Models.Mcp;

public class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("method")]
    public string Method { get; set; } = "";
    
    [JsonPropertyName("params")]
    public JsonElement? Params { get; set; }
    
    [JsonPropertyName("id")]
    public JsonElement Id { get; set; }
}