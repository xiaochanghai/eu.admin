namespace EU.Core.Api.MCP.Models.Mcp;

public class McpTool
{
    /// <summary>工具显式声明的风险提示；不是授权或运行时校验。</summary>
    [JsonPropertyName("annotations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpToolAnnotations? Annotations { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
    
    [JsonPropertyName("inputSchema")]
    public object InputSchema { get; set; } = new { type = "object", properties = new { } };
}

/// <summary>MCP 工具读写及重试风险提示。</summary>
public sealed class McpToolAnnotations
{
    [JsonPropertyName("readOnlyHint")]
    public bool ReadOnlyHint { get; set; }
    [JsonPropertyName("destructiveHint")]
    public bool DestructiveHint { get; set; }
    [JsonPropertyName("idempotentHint")]
    public bool IdempotentHint { get; set; }
    [JsonPropertyName("openWorldHint")]
    public bool OpenWorldHint { get; set; }
}
