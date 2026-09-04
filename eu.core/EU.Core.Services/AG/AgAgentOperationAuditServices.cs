using EU.Core.IServices.Abstractions.Auditing;

#nullable enable

namespace EU.Core.Services;

#region 文件职责：AgAgentOperationAuditServices 职责实现

public sealed class AgAgentOperationAuditServices :
    BaseServices<AgAgentOperationAudit>,
    IAgAgentOperationAuditServices
{
    public AgAgentOperationAuditServices(IBaseRepository<AgAgentOperationAudit> dal)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
    }

    public async Task SaveAsync(AgentOperationAuditRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();
        var existing = await Db.Queryable<AgAgentOperationAudit>()
            .Where(value => value.ID == record.Id)
            .FirstAsync();
        if (existing is null)
        {
            await Db.Insertable(MapEntity(record)).ExecuteCommandAsync();
        }
        else if (!existing.IsDeleted &&
                 string.Equals(existing.Outcome, "Started", StringComparison.Ordinal) &&
                 SameIdentity(existing, record))
        {
            var entity = MapEntity(record);
            await Db.Updateable(entity)
                .UpdateColumns(value => new
                {
                    value.StatusCode,
                    value.Outcome,
                    value.ErrorCode,
                    value.DurationMilliseconds
                })
                .Where(value => value.ID == record.Id &&
                                !value.IsDeleted &&
                                value.Outcome == "Started")
                .ExecuteCommandAsync();
        }
    }

    public async Task<IReadOnlyList<AgentOperationAuditRecord>> ListAsync(string tenantId, int take, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        cancellationToken.ThrowIfCancellationRequested();
        var records = await Db.Queryable<AgAgentOperationAudit>()
            .Where(value => value.TenantId == tenantId && !value.IsDeleted)
            .OrderBy(value => value.OccurredAtUtc, OrderByType.Desc)
            .OrderBy(value => value.ID, OrderByType.Desc)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return records.Select(MapRecord).ToArray();
    }

    private static bool SameIdentity(AgAgentOperationAudit current, AgentOperationAuditRecord replacement) =>
        StoredDateTimeEquals(Required(current.OccurredAtUtc, "OccurredAtUtc"), replacement.OccurredAtUtc) &&
        string.Equals(current.TenantId, replacement.TenantId, StringComparison.Ordinal) &&
        string.Equals(current.UserId, replacement.UserId, StringComparison.Ordinal) &&
        string.Equals(current.CorrelationId, replacement.CorrelationId, StringComparison.Ordinal) &&
        string.Equals(current.Policy, replacement.Policy, StringComparison.Ordinal) &&
        string.Equals(current.Method, replacement.Method, StringComparison.Ordinal) &&
        string.Equals(current.Path, replacement.Path, StringComparison.Ordinal);

    private static AgAgentOperationAudit MapEntity(AgentOperationAuditRecord value) => new()
    {
        ID = value.Id,
        TenantId = value.TenantId,
        UserId = value.UserId,
        CorrelationId = value.CorrelationId,
        Policy = value.Policy,
        Method = value.Method,
        Path = value.Path,
        StatusCode = value.StatusCode,
        Outcome = value.Outcome,
        ErrorCode = value.ErrorCode,
        DurationMilliseconds = value.DurationMilliseconds,
        OccurredAtUtc = value.OccurredAtUtc.UtcDateTime,
        IsDeleted = false,
        IsActive = true
    };

    private static AgentOperationAuditRecord MapRecord(AgAgentOperationAudit value) => new(
        value.ID,
        ToOffset(Required(value.OccurredAtUtc, "OccurredAtUtc")),
        Required(value.TenantId, "TenantId"),
        Required(value.UserId, "UserId"),
        Required(value.CorrelationId, "CorrelationId"),
        Required(value.Policy, "Policy"),
        Required(value.Method, "Method"),
        Required(value.Path, "Path"),
        Required(value.StatusCode, "StatusCode"),
        Required(value.Outcome, "Outcome"),
        value.ErrorCode,
        Required(value.DurationMilliseconds, "DurationMilliseconds"));

    private static DateTimeOffset ToOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static bool StoredDateTimeEquals(DateTime stored, DateTimeOffset value) =>
        Math.Abs((stored - value.UtcDateTime).Ticks) <= TimeSpan.TicksPerMillisecond * 2;

    private static T Required<T>(T? value, string field) where T : struct =>
        value ?? throw new InvalidDataException($"Agent operation audit field '{field}' is missing.");

    private static string Required(string? value, string field) =>
        value ?? throw new InvalidDataException($"Agent operation audit field '{field}' is missing.");
}

#endregion
