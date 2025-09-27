namespace EU.Core.Api.MCP.Models.Mcp;

public class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("method")]
    public string Method { get; set; } = "";
    
    [JsonPropertyName("params")]
    public JsonElement? Params { get; set; }
    
    [JsonPropertyName("id")]
    public object? Id { get; set; }
}