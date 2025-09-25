namespace EU.Core.Models;

/// <summary>
/// MCP 流式事件
/// </summary>
public class McpStreamEvent
{
    public string EventType { get; set; } = string.Empty;
    public object Data { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Id { get; set; }
}
