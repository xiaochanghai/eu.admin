namespace EU.Core.Api.MCP.Services.BusinessQuery.Auditing;

public sealed record BusinessQueryAuditRecord(
    Guid QueryId,
    string UserId,
    string TenantId,
    long CatalogRevision,
    string QueryPlanHash,
    IReadOnlyList<string> PolicyRuleIds,
    string SqlTemplateHash,
    int RowCount,
    long DurationMilliseconds,
    string TerminalStatus,
    string? ErrorCode,
    DateTimeOffset CompletedAtUtc);

public sealed record BusinessQuerySecurityAuditRecord(
    Guid EventId,
    string EventType,
    string TerminalStatus,
    string ErrorCode,
    DateTimeOffset CompletedAtUtc);

public interface IBusinessQueryAuditRepository
{
    Task WriteTerminalAsync(
        BusinessQueryAuditRecord record,
        CancellationToken cancellationToken);

    Task WriteSecurityRejectionAsync(
        BusinessQuerySecurityAuditRecord record,
        CancellationToken cancellationToken);
}
