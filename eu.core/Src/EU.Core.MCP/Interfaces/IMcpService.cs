using EU.Core.MCP.Models;

namespace EU.Core.MCP.Interfaces;

/// <summary>
/// MCP 服务接口
/// </summary>
public interface IMcpService
{
    /// <summary>
    /// 获取可用工具列表
    /// </summary>
    Task<List<McpTool>> GetToolsAsync();
    
    /// <summary>
    /// 获取可用资源列表
    /// </summary>
    Task<List<McpResource>> GetResourcesAsync();
    
    /// <summary>
    /// 执行工具调用
    /// </summary>
    Task<object> CallToolAsync(string toolName, object parameters);
    
    /// <summary>
    /// 执行工具调用（带进度回调）
    /// </summary>
    Task<object> CallToolWithProgressAsync(string toolName, object parameters, 
        Action<string> progressCallback, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取资源内容
    /// </summary>
    Task<object> GetResourceAsync(string uri);
    
    /// <summary>
    /// 处理 MCP 请求
    /// </summary>
    Task<McpResponse> HandleRequestAsync(McpRequest request);
    
    /// <summary>
    /// 开始流式工具调用
    /// </summary>
    IAsyncEnumerable<McpStreamEvent> CallToolStreamAsync(string toolName, object parameters, 
        CancellationToken cancellationToken = default);
}

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