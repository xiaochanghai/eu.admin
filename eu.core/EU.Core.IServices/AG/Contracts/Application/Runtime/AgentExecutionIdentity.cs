#nullable enable

using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace EU.Core.IServices.Runtime;

/// <summary>
/// 表示 Agent 执行链路中的租户、用户和调用关系身份。
/// </summary>
public sealed partial class AgentExecutionIdentity
{
    public AgentExecutionIdentity(
        string userId,
        string tenantId,
        IEnumerable<string> permissions,
        string correlationId)
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

    private static string Required(string? value, string parameterName)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is 0 or > 256 || normalized.Any(char.IsControl))
        {
            throw new ArgumentException("Execution identity value is invalid.", parameterName);
        }

        return normalized;
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9._:-]{0,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex PermissionPattern();
}
