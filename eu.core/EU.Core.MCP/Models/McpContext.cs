using System.Text.Json.Serialization;

namespace EU.Core.MCP.Models;

/// <summary>
/// MCP 上下文 - 类似 FastMCP 的 Context
/// </summary>
public class McpContext
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    
    [JsonPropertyName("userId")]
    public string? UserId { get; set; }
    
    [JsonPropertyName("tenantId")]
    public string? TenantId { get; set; }
    
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <summary>
    /// 记录信息日志
    /// </summary>
    public async Task InfoAsync(string message, object? data = null)
    {
        await LogAsync("info", message, data);
    }
    
    /// <summary>
    /// 记录警告日志
    /// </summary>
    public async Task WarningAsync(string message, object? data = null)
    {
        await LogAsync("warning", message, data);
    }
    
    /// <summary>
    /// 记录错误日志
    /// </summary>
    public async Task ErrorAsync(string message, object? data = null)
    {
        await LogAsync("error", message, data);
    }
    
    /// <summary>
    /// 报告进度
    /// </summary>
    public async Task ReportProgressAsync(string message, double? percentage = null, object? data = null)
    {
        var progressData = new
        {
            message,
            percentage,
            data,
            timestamp = DateTime.UtcNow
        };
        
        await LogAsync("progress", message, progressData);
    }
    
    /// <summary>
    /// 请求 LLM 采样
    /// </summary>
    public async Task<string> SampleAsync(string prompt, object? options = null)
    {
        // 这里可以集成你的 LLM 服务
        // 暂时返回模拟响应
        await Task.Delay(100);
        return $"LLM 响应: {prompt}";
    }
    
    /// <summary>
    /// 读取资源
    /// </summary>
    public async Task<McpResourceData> ReadResourceAsync(string uri)
    {
        // 这里可以集成你的资源服务
        await Task.Delay(100);
        return new McpResourceData
        {
            Uri = uri,
            Content = $"资源内容: {uri}",
            MimeType = "text/plain"
        };
    }
    
    /// <summary>
    /// 发送 HTTP 请求
    /// </summary>
    public async Task<object> HttpRequestAsync(string method, string url, object? data = null, Dictionary<string, string>? headers = null)
    {
        // 这里可以集成你的 HTTP 客户端
        await Task.Delay(100);
        return new { method, url, data, headers, response = "HTTP 响应" };
    }
    
    private async Task LogAsync(string level, string message, object? data = null)
    {
        var logData = new
        {
            level,
            message,
            data,
            timestamp = DateTime.UtcNow,
            sessionId = SessionId
        };
        
        // 这里可以集成你的日志服务
        await Task.CompletedTask;
    }
}

/// <summary>
/// MCP 资源数据
/// </summary>
public class McpResourceData
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
    
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;
    
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
} 