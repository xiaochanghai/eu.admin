using EU.Core.IServices.MainAgent;

#nullable enable

namespace EU.Core.Services;

// 文件职责：AgMainAgentAssignmentServices 职责实现

/// <summary>
/// 提供主 Agent 分配记录的持久化服务。
/// </summary>
public sealed class AgMainAgentAssignmentServices :
    BaseServices<AgMainAgentAssignment>,
    IAgMainAgentAssignmentServices,
    IMainAgentAssignmentRepository
{
    private const string AssignmentKey = "platform-main-agent";

    #region 构造（AgMainAgentAssignmentServices）
    /// <summary>
    /// 构造（AgMainAgentAssignmentServices）
    /// </summary>
    /// <param name="dal">当前服务使用的数据访问仓储。</param>
    public AgMainAgentAssignmentServices(IBaseRepository<AgMainAgentAssignment> dal)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
    }
    #endregion

    #region 获取（GetAsync）
    /// <summary>
    /// 获取（GetAsync）
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>异步任务，返回当前主 Agent 分配记录；尚未分配时返回 null。</returns>
    public async Task<MainAgentAssignment?> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AgMainAgentAssignment? entity = await Db.Queryable<AgMainAgentAssignment>()
            .Where(value => value.AssignmentKey == AssignmentKey && !value.IsDeleted)
            .FirstAsync();
        return entity is null ? null : MapAssignment(entity);
    }
    #endregion

    #region 按修订号创建或替换主 Agent 分配（TryReplaceAsync）
    /// <summary>
    /// 按修订号创建或替换主 Agent 分配（TryReplaceAsync）。
    /// </summary>
    /// <param name="value">新的主 Agent 分配；初次创建的修订号为零，替换时为预期修订号加一。</param>
    /// <param name="expectedLogicalRevision">为 null 时仅尝试初次创建；非 null 时要求现有记录修订号匹配，不允许为 long.MaxValue。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>成功创建或更新一条分配记录时返回 true；新修订号不合法、初次创建时记录已存在，或更新时未匹配到预期修订号的未删除记录时返回 false。</returns>
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
    #endregion

    #region 映射（MapAssignment）
    /// <summary>
    /// 映射（MapAssignment）
    /// </summary>
    /// <param name="value">本次操作使用的主 Agent 分配实体。</param>
    /// <returns>包含主 Agent、固定版本、逻辑版本及 UTC 更新时间的分配记录。</returns>
    private static MainAgentAssignment MapAssignment(AgMainAgentAssignment value) =>
        new(
            Required(value.AgentId, "AgentId"),
            Required(value.AgentVersionId, "AgentVersionId"),
            Required(value.LogicalRevision, "LogicalRevision"),
            new DateTimeOffset(DateTime.SpecifyKind(
                Required(value.UpdatedAtUtc, "UpdatedAtUtc"),
                DateTimeKind.Utc)));
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <typeparam name="T">必填字段的值类型。</typeparam>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="field">字段名称，用于校验和错误提示。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static T Required<T>(T? value, string field) where T : struct =>
        value ?? throw new InvalidDataException(
            $"Main Agent assignment field '{field}' is missing.");
    #endregion
}
