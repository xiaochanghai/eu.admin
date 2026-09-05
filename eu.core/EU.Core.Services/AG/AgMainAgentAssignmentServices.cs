using EU.Core.IServices.MainAgent;

#nullable enable

namespace EU.Core.Services;

#region 文件职责：AgMainAgentAssignmentServices 职责实现

/// <summary>
/// 提供主 Agent 分配记录的持久化服务。
/// </summary>
public sealed class AgMainAgentAssignmentServices :
    BaseServices<AgMainAgentAssignment>,
    IAgMainAgentAssignmentServices,
    IMainAgentAssignmentRepository
{
    private const string AssignmentKey = "platform-main-agent";

    public AgMainAgentAssignmentServices(IBaseRepository<AgMainAgentAssignment> dal)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
    }

    public async Task<MainAgentAssignment?> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AgMainAgentAssignment? entity = await Db.Queryable<AgMainAgentAssignment>()
            .Where(value => value.AssignmentKey == AssignmentKey && !value.IsDeleted)
            .FirstAsync();
        return entity is null ? null : MapAssignment(entity);
    }

    public async Task<bool> TryReplaceAsync(MainAgentAssignment value, long? expectedLogicalRevision, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (expectedLogicalRevision == long.MaxValue ||
            value.LogicalRevision != (expectedLogicalRevision is null
                ? 0
                : expectedLogicalRevision.Value + 1))
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (expectedLogicalRevision is not null)
        {
            int updated = await Db.Updateable<AgMainAgentAssignment>()
                .SetColumns(_ => new AgMainAgentAssignment
                {
                    AgentId = value.AgentId,
                    AgentVersionId = value.AgentVersionId,
                    LogicalRevision = value.LogicalRevision,
                    UpdatedAtUtc = value.UpdatedAtUtc.UtcDateTime
                })
                .Where(entity =>
                    entity.AssignmentKey == AssignmentKey &&
                    entity.LogicalRevision == expectedLogicalRevision.Value &&
                    !entity.IsDeleted)
                .ExecuteCommandAsync();
            return updated == 1;
        }

        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            bool exists = await Db.Queryable<AgMainAgentAssignment>()
                .Where(entity => entity.AssignmentKey == AssignmentKey)
                .AnyAsync();
            if (exists)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            int inserted = await Db.Insertable(new AgMainAgentAssignment
            {
                ID = Guid.NewGuid(),
                AssignmentKey = AssignmentKey,
                AgentId = value.AgentId,
                AgentVersionId = value.AgentVersionId,
                LogicalRevision = value.LogicalRevision,
                UpdatedAtUtc = value.UpdatedAtUtc.UtcDateTime,
                IsDeleted = false,
                IsActive = true
            }).ExecuteCommandAsync();
            await Db.Ado.CommitTranAsync();
            return inserted == 1;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    private static MainAgentAssignment MapAssignment(AgMainAgentAssignment value) =>
        new(
            Required(value.AgentId, "AgentId"),
            Required(value.AgentVersionId, "AgentVersionId"),
            Required(value.LogicalRevision, "LogicalRevision"),
            new DateTimeOffset(DateTime.SpecifyKind(
                Required(value.UpdatedAtUtc, "UpdatedAtUtc"),
                DateTimeKind.Utc)));

    private static T Required<T>(T? value, string field) where T : struct =>
        value ?? throw new InvalidDataException(
            $"Main Agent assignment field '{field}' is missing.");
}

#endregion
