using EU.Core.IServices.Runtime;
using EU.Core.IServices.Mcp;

#nullable enable

namespace EU.Core.Services;

#region 文件职责：AgAgentRunAuditServices 职责实现

/// <summary>
/// 提供 Agent 运行审计记录的持久化服务。
/// </summary>
public sealed class AgAgentRunAuditServices :
    BaseServices<AgAgentRunAudit>,
    IAgAgentRunAuditServices,
    IAgentRunAuditRepository
{
    public AgAgentRunAuditServices(IBaseRepository<AgAgentRunAudit> dal)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
    }

    public async Task SaveAsync(AgentRunAuditRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            AgAgentRunAudit? existing = await Db.Queryable<AgAgentRunAudit>()
                .Where(value => value.ID == record.RunId)
                .FirstAsync();
            if (existing is null)
            {
                await Db.Insertable(MapAuditEntity(record)).ExecuteCommandAsync();
                await InsertToolCallsAsync(record, cancellationToken);
            }
            else if (!existing.IsDeleted &&
                     Required(existing.AgentId, "AgentId") == record.AgentId &&
                     StoredDateTimeEquals(
                         Required(existing.StartedAtUtc, "StartedAtUtc"),
                         record.StartedAtUtc))
            {
                AgAgentRunAudit entity = MapAuditEntity(record);
                await Db.Updateable(entity)
                    .UpdateColumns(value => new
                    {
                        value.AgentVersionId,
                        value.AgentCode,
                        value.Status,
                        value.FinishedAtUtc,
                        value.InputSha256,
                        value.OutputCharacters,
                        value.ToolCallCount,
                        value.ErrorCode
                    })
                    .Where(value => value.ID == record.RunId && !value.IsDeleted)
                    .ExecuteCommandAsync();
                await Db.Deleteable<AgAgentToolCallAudit>()
                    .Where(value => value.RunId == record.RunId)
                    .ExecuteCommandAsync();
                await InsertToolCallsAsync(record, cancellationToken);
            }

            await Db.Ado.CommitTranAsync();
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<AgentRunAuditRecord>> ListAsync(Guid agentId, int take, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            List<AgAgentRunAudit> audits = await Db.Queryable<AgAgentRunAudit>()
                .Where(value => value.AgentId == agentId && !value.IsDeleted)
                .OrderBy(value => value.StartedAtUtc, OrderByType.Desc)
                .OrderBy(value => value.ID, OrderByType.Desc)
                .Take(Math.Clamp(take, 1, 100))
                .ToListAsync();
            IReadOnlyList<AgentRunAuditRecord> result = await LoadAuditsAsync(
                audits,
                cancellationToken);
            await Db.Ado.CommitTranAsync();
            return result;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    private async Task<IReadOnlyList<AgentRunAuditRecord>> LoadAuditsAsync(IReadOnlyList<AgAgentRunAudit> audits, CancellationToken cancellationToken)
    {
        if (audits.Count == 0)
        {
            return [];
        }

        Guid[] runIds = audits.Select(value => value.ID).ToArray();
        List<AgAgentToolCallAudit> toolCalls = await Db.Queryable<AgAgentToolCallAudit>()
            .Where(value => value.RunId.HasValue && runIds.Contains(value.RunId.Value) && !value.IsDeleted)
            .OrderBy(value => value.RunId)
            .OrderBy(value => value.Ordinal)
            .OrderBy(value => value.ID)
            .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyDictionary<Guid, AgAgentToolCallAudit[]> byRun = toolCalls
            .GroupBy(value => Required(value.RunId, "ToolCall.RunId"))
            .ToDictionary(group => group.Key, group => group.ToArray());
        return audits.Select(value => MapAudit(
                value,
                byRun.GetValueOrDefault(value.ID) ?? []))
            .Select(AgentRunContractCloner.Clone)
            .ToArray();
    }

    private async Task InsertToolCallsAsync(AgentRunAuditRecord record, CancellationToken cancellationToken)
    {
        if (record.ToolCalls.Count == 0)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await Db.Insertable(record.ToolCalls.Select((value, ordinal) =>
            MapToolCallEntity(record.RunId, ordinal, value)).ToList()).ExecuteCommandAsync();
    }

    private static AgentRunAuditRecord MapAudit(AgAgentRunAudit value, IReadOnlyList<AgAgentToolCallAudit> toolCalls) =>
        new(
            value.ID,
            Required(value.AgentId, "AgentId"),
            Required(value.AgentVersionId, "AgentVersionId"),
            Required(value.AgentCode, "AgentCode"),
            Enum.Parse<AgentRunStatus>(Required(value.Status, "Status"), false),
            ToOffset(Required(value.StartedAtUtc, "StartedAtUtc")),
            value.FinishedAtUtc.HasValue ? ToOffset(value.FinishedAtUtc.Value) : null,
            Required(value.InputSha256, "InputSha256"),
            Required(value.OutputCharacters, "OutputCharacters"),
            Required(value.ToolCallCount, "ToolCallCount"),
            Required(value.ErrorCode, "ErrorCode"),
            toolCalls.OrderBy(tool => Required(tool.Ordinal, "ToolCall.Ordinal"))
                .ThenBy(tool => tool.ID)
                .Select(MapToolCall)
                .ToArray());

    private static AgentToolCallAuditRecord MapToolCall(AgAgentToolCallAudit value) =>
        new(
            Required(value.ToolVersionId, "ToolCall.ToolVersionId"),
            Required(value.ToolName, "ToolCall.ToolName"),
            Enum.Parse<McpToolRisk>(Required(value.Risk, "ToolCall.Risk"), false),
            Enum.Parse<AgentRunEventKind>(Required(value.Status, "ToolCall.Status"), false),
            ToOffset(Required(value.StartedAtUtc, "ToolCall.StartedAtUtc")),
            ToOffset(Required(value.FinishedAtUtc, "ToolCall.FinishedAtUtc")),
            Required(value.ErrorCode, "ToolCall.ErrorCode"));

    private static AgAgentRunAudit MapAuditEntity(AgentRunAuditRecord value) => new()
    {
        ID = value.RunId,
        AgentId = value.AgentId,
        AgentVersionId = value.AgentVersionId,
        AgentCode = value.AgentCode,
        Status = value.Status.ToString(),
        StartedAtUtc = value.StartedAtUtc.UtcDateTime,
        FinishedAtUtc = value.FinishedAtUtc?.UtcDateTime,
        InputSha256 = value.InputSha256,
        OutputCharacters = value.OutputCharacters,
        ToolCallCount = value.ToolCallCount,
        ErrorCode = value.ErrorCode,
        IsDeleted = false,
        IsActive = true
    };

    private static AgAgentToolCallAudit MapToolCallEntity(Guid runId, int ordinal, AgentToolCallAuditRecord value) => new()
    {
        ID = Guid.NewGuid(),
        RunId = runId,
        Ordinal = ordinal,
        ToolVersionId = value.ToolVersionId,
        ToolName = value.ToolName,
        Risk = value.Risk.ToString(),
        Status = value.Status.ToString(),
        StartedAtUtc = value.StartedAtUtc.UtcDateTime,
        FinishedAtUtc = value.FinishedAtUtc.UtcDateTime,
        ErrorCode = value.ErrorCode,
        IsDeleted = false,
        IsActive = true
    };

    private static DateTimeOffset ToOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    // The current SQL Server provider binds DateTime parameters as legacy datetime,
    // whose 1/300-second precision can shift the stored value by up to 1.67 ms.
    private static bool StoredDateTimeEquals(DateTime stored, DateTimeOffset value) =>
        Math.Abs((stored - value.UtcDateTime).Ticks) <=
        TimeSpan.TicksPerMillisecond * 2;

    private static T Required<T>(T? value, string field) where T : struct =>
        value ?? throw new InvalidDataException(
            $"Agent run audit field '{field}' is missing.");

    private static string Required(string? value, string field) =>
        value ?? throw new InvalidDataException(
            $"Agent run audit field '{field}' is missing.");
}

#endregion
