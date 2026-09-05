#nullable enable

namespace EU.Core.IServices.Abstractions.Security;

/// <summary>
/// 提供当前请求调用方的身份与租户上下文。
/// </summary>
public interface ICallerContext
{
    /// <summary>获取当前调用用户标识。</summary>
    string UserId { get; }

    /// <summary>获取当前调用租户标识。</summary>
    string TenantId { get; }

    /// <summary>获取当前调用方的权限集合。</summary>
    IReadOnlySet<string> Permissions { get; }

    /// <summary>获取当前请求的链路关联标识。</summary>
    string CorrelationId { get; }
}
