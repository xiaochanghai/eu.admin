namespace EU.Core.Model.Models;

/// <summary>允许提供给前端的配置；新增配置需显式扩展此模型及服务端映射。</summary>
public class ClientConfigDto
{
    /// <summary>主题配置键值集合，键去掉 theme. 前缀，值保持配置表中的字符串形式。</summary>
    public Dictionary<string, string> Theme { get; set; } = new();
}
