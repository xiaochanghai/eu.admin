using System.Text.Json;
using System.Text.Json.Serialization;

namespace EU.Core.Api.MCP.Models.Mcp;

public class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonRpcError? Error { get; set; }

    [JsonPropertyName("id")]
    public JsonElement Id { get; set; }
}

/// <summary>
/// MCP 初始化结果
/// </summary>
public class McpInitializeResult
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = "2024-11-05";

    [JsonPropertyName("capabilities")]
    public McpServerCapabilities Capabilities { get; set; } = new();

    [JsonPropertyName("serverInfo")]
    public McpServerInfo ServerInfo { get; set; } = new();
}

/// <summary>
/// MCP 服务器能力
/// </summary>
public class McpServerCapabilities
{
    [JsonPropertyName("roots")]
    public McpRootsCapability? Roots { get; set; }

    [JsonPropertyName("sampling")]
    public object? Sampling { get; set; }

    [JsonPropertyName("tools")]
    public object? Tools { get; set; }

    [JsonPropertyName("resources")]
    public object? Resources { get; set; }

    [JsonPropertyName("prompts")]
    public object? Prompts { get; set; }
}

/// <summary>
/// MCP 服务器信息
/// </summary>
public class McpServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "EU.Core FastMCP Server";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";
}
/// <summary>
/// MCP 根能力
/// </summary>
public class McpRootsCapability
{
    [JsonPropertyName("listChanged")]
    public bool ListChanged { get; set; }
}