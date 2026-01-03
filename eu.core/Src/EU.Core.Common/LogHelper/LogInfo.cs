namespace EU.Core.Common.LogHelper;

/// <summary>
/// 日志信息模型
/// </summary>
public class LogInfo
{
    /// <summary>
    /// 日志时间
    /// </summary>
    public DateTime Datetime { get; set; }

    /// <summary>
    /// 日志内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// IP 地址
    /// </summary>
    public string IP { get; set; } = string.Empty;

    /// <summary>
    /// 日志颜色标识
    /// </summary>
    public string LogColor { get; set; } = string.Empty;

    /// <summary>
    /// 重要程度（数值越大越重要）
    /// </summary>
    public int Import { get; set; } = 0;
}
