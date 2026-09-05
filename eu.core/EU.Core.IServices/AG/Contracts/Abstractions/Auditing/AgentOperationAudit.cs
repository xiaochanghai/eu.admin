#nullable enable

namespace EU.Core.IServices.Abstractions.Auditing;

/// <summary>
/// Agent 接口操作的审计记录。
/// </summary>
/// <param name="Id">记录标识。</param>
/// <param name="OccurredAtUtc">操作发生的 UTC 时间。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="UserId">用户标识。</param>
/// <param name="CorrelationId">用于关联请求链路的标识。</param>
/// <param name="Policy">命中的授权策略。</param>
/// <param name="Method">HTTP 请求方法。</param>
/// <param name="Path">HTTP 请求路径或包内相对路径。</param>
/// <param name="StatusCode">HTTP 响应状态码。</param>
/// <param name="Outcome">操作结果。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
/// <param name="DurationMilliseconds">操作耗时，单位为毫秒。</param>
public sealed record AgentOperationAuditRecord(
    Guid Id,
    DateTimeOffset OccurredAtUtc,
    string TenantId,
    string UserId,
    string CorrelationId,
    string Policy,
    string Method,
    string Path,
    int StatusCode,
    string Outcome,
    string? ErrorCode,
    long DurationMilliseconds);
