using EU.Core.IServices.Runtime;
using EU.Core.IServices.Mcp;

#nullable enable

namespace EU.Core.Services;

// 文件职责：AgAgentRunAuditServices 职责实现

/// <summary>
/// 提供 Agent 运行审计记录的持久化服务。
/// </summary>
public sealed class AgAgentRunAuditServices :
    BaseServices<AgAgentRunAudit>,
    IAgAgentRunAuditServices,
    IAgentRunAuditRepository
{
    #region 构造（AgAgentRunAuditServices）
    /// <summary>
    /// 构造（AgAgentRunAuditServices）
    /// </summary>
    /// <param name="dal">当前服务使用的数据访问仓储。</param>
    public AgAgentRunAuditServices(IBaseRepository<AgAgentRunAudit> dal)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
    }
    #endregion

    #region 保存（SaveAsync）
    /// <summary>
    /// 保存（SaveAsync）
    /// </summary>
    /// <param name="record">业务记录。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
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
    #endregion

    #region 查询列表（ListAsync）
    /// <summary>
    /// 查询列表（ListAsync）
    /// </summary>
    /// <param name="agentId">Agent 定义标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定 Agent 最近的运行审计及工具调用记录，按开始时间及标识倒序排列，最多 100 条。</returns>
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
    #endregion

    #region 加载（LoadAuditsAsync）
    /// <summary>
    /// 加载（LoadAuditsAsync）
    /// </summary>
    /// <param name="audits">审计记录集合。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>保持输入审计顺序并补齐工具调用明细的审计记录集合；输入为空时返回空集合。</returns>
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
    #endregion

    #region 新增（InsertToolCallsAsync）
    /// <summary>
    /// 新增（InsertToolCallsAsync）
    /// </summary>
    /// <param name="record">业务记录。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
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
    #endregion

    #region 映射（MapAudit）
    /// <summary>
    /// 映射（MapAudit）
    /// </summary>
    /// <param name="value">本次操作使用的运行审计实体。</param>
    /// <param name="toolCalls">工具调用记录集合。</param>
    /// <returns>包含按序号及标识排序的工具调用明细的运行审计记录。</returns>
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
    #endregion

    #region 映射（MapToolCall）
    /// <summary>
    /// 映射（MapToolCall）
    /// </summary>
    /// <param name="value">本次操作使用的工具调用审计实体。</param>
    /// <returns>从持久化字段还原的工具调用审计记录。</returns>
    private static AgentToolCallAuditRecord MapToolCall(AgAgentToolCallAudit value) =>
        new(
            Required(value.ToolVersionId, "ToolCall.ToolVersionId"),
            Required(value.ToolName, "ToolCall.ToolName"),
            Enum.Parse<McpToolRisk>(Required(value.Risk, "ToolCall.Risk"), false),
            Enum.Parse<AgentRunEventKind>(Required(value.Status, "ToolCall.Status"), false),
            ToOffset(Required(value.StartedAtUtc, "ToolCall.StartedAtUtc")),
            ToOffset(Required(value.FinishedAtUtc, "ToolCall.FinishedAtUtc")),
            Required(value.ErrorCode, "ToolCall.ErrorCode"));
    #endregion

    #region 映射（MapAuditEntity）
    /// <summary>
    /// 映射（MapAuditEntity）
    /// </summary>
    /// <param name="value">本次操作使用的Agent 运行审计记录。</param>
    /// <returns>由运行审计记录构造的持久化实体。</returns>
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
    #endregion

    #region 映射（MapToolCallEntity）
    /// <summary>
    /// 映射（MapToolCallEntity）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="ordinal">工具调用在审计记录中的排序序号。</param>
    /// <param name="value">本次操作使用的工具调用审计记录。</param>
    /// <returns>具有新标识、运行标识及调用序号的工具调用审计实体。</returns>
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
    #endregion

    #region 转换（ToOffset）
    /// <summary>
    /// 将数据库时间还原为 UTC 时间（ToOffset）。
    /// </summary>
    /// <param name="value">按 UTC 语义存储的数据库时间。</param>
    /// <returns>将输入时间视为 UTC 后构造的零偏移时间。</returns>
    private static DateTimeOffset ToOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    #endregion

    // The current SQL Server provider binds DateTime parameters as legacy datetime,
    // whose 1/300-second precision can shift the stored value by up to 1.67 ms.
    #region 按数据库时间精度比较 UTC 时间（StoredDateTimeEquals）
    /// <summary>
    /// 按数据库时间精度比较 UTC 时间（StoredDateTimeEquals）。
    /// </summary>
    /// <param name="stored">数据库中保存的 UTC 时间。</param>
    /// <param name="value">待比较的带时区偏移时间，比较时转换为 UTC。</param>
    /// <returns>已存储时间与待比较值的 UTC 时间相差不超过 2 毫秒时返回 true，否则返回 false。</returns>
    private static bool StoredDateTimeEquals(DateTime stored, DateTimeOffset value) =>
        Math.Abs((stored - value.UtcDateTime).Ticks) <=
        TimeSpan.TicksPerMillisecond * 2;
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
            $"Agent run audit field '{field}' is missing.");
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="field">字段名称，用于校验和错误提示。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static string Required(string? value, string field) =>
        value ?? throw new InvalidDataException(
            $"Agent run audit field '{field}' is missing.");
    #endregion
}
