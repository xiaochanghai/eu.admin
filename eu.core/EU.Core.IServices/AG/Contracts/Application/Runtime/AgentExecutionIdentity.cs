#nullable enable

using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace EU.Core.IServices.Runtime;

/// <summary>
/// 表示 Agent 执行链路中的租户、用户和调用关系身份。
/// </summary>
public sealed partial class AgentExecutionIdentity
{
    #region 构造（AgentExecutionIdentity）
    /// <summary>
    /// 构造（AgentExecutionIdentity）
    /// </summary>
    /// <param name="userId">用户标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="permissions">当前执行身份拥有的权限集合。</param>
    /// <param name="correlationId">关联当前请求与运行记录的标识。</param>
    public AgentExecutionIdentity(string userId, string tenantId, IEnumerable<string> permissions, string correlationId)
    {
        UserId = Required(userId, nameof(userId));
        TenantId = Required(tenantId, nameof(tenantId));
        CorrelationId = Required(correlationId, nameof(correlationId));
        string[] frozenPermissions = permissions?
            .Select(value => value?.Trim() ?? string.Empty)
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray() ?? throw new ArgumentNullException(nameof(permissions));
        if (frozenPermissions.Length > 128
            || frozenPermissions.Any(value => !PermissionPattern().IsMatch(value)))
        {
            throw new ArgumentException("Execution permissions are invalid.", nameof(permissions));
        }

        Permissions = new ReadOnlyCollection<string>(frozenPermissions);
    }
    #endregion

    /// <summary>
    /// 获取用户标识。
    /// </summary>
    public string UserId { get; }

    /// <summary>
    /// 获取租户标识。
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// 获取执行身份拥有的权限集合。
    /// </summary>
    public IReadOnlyList<string> Permissions { get; }

    /// <summary>
    /// 获取关联标识。
    /// </summary>
    public string CorrelationId { get; }

    #region 处理（Required）
    /// <summary>
    /// 处理（Required）
    /// </summary>
    /// <param name="value">待校验的必填身份或文本字段。</param>
    /// <param name="parameterName">当前校验的参数名称，用于异常提示。</param>
    /// <returns>去除首尾空白、长度为 1 至 256 且不含控制字符的身份值；无效输入抛出 ArgumentException。</returns>
    private static string Required(string? value, string parameterName)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is 0 or > 256 || normalized.Any(char.IsControl))
        {
            throw new ArgumentException("Execution identity value is invalid.", parameterName);
        }

        return normalized;
    }
    #endregion

    #region 处理（PermissionPattern）
    /// <summary>
    /// 处理（PermissionPattern）
    /// </summary>
    /// <returns>用于校验执行身份权限名称格式的正则表达式。</returns>
    [GeneratedRegex("^[A-Za-z][A-Za-z0-9._:-]{0,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex PermissionPattern();
    #endregion
}
