using EU.Core.IServices.Abstractions.Auditing;

#nullable enable

namespace EU.Core.Services;

// 文件职责：AgAgentOperationAuditServices 职责实现

/// <summary>
/// 提供 Agent 接口操作审计记录的持久化服务。
/// </summary>
public sealed class AgAgentOperationAuditServices :
    BaseServices<AgAgentOperationAudit>,
    IAgAgentOperationAuditServices
{
    #region 构造（AgAgentOperationAuditServices）
    /// <summary>
    /// 构造（AgAgentOperationAuditServices）
    /// </summary>
    /// <param name="dal">当前服务使用的数据访问仓储。</param>
    public AgAgentOperationAuditServices(IBaseRepository<AgAgentOperationAudit> dal)
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
    #endregion

    #region 查询列表（ListAsync）
    /// <summary>
    /// 查询列表（ListAsync）
    /// </summary>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户未删除的操作审计记录，按发生时间及标识倒序排列，数量限制为 1 至 100 条。</returns>
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
    #endregion

    #region 比较操作审计的不可变身份字段（SameIdentity）
    /// <summary>
    /// 比较操作审计的不可变身份字段（SameIdentity）。
    /// </summary>
    /// <param name="current">数据库中现有的操作审计记录。</param>
    /// <param name="replacement">待写入的替换审计记录。</param>
    /// <returns>发生时间在允许误差内，且租户、用户、关联标识、策略、HTTP 方法及路径全部一致时返回 true，否则返回 false。</returns>
    private static bool SameIdentity(AgAgentOperationAudit current, AgentOperationAuditRecord replacement) =>
        StoredDateTimeEquals(Required(current.OccurredAtUtc, "OccurredAtUtc"), replacement.OccurredAtUtc) &&
        string.Equals(current.TenantId, replacement.TenantId, StringComparison.Ordinal) &&
        string.Equals(current.UserId, replacement.UserId, StringComparison.Ordinal) &&
        string.Equals(current.CorrelationId, replacement.CorrelationId, StringComparison.Ordinal) &&
        string.Equals(current.Policy, replacement.Policy, StringComparison.Ordinal) &&
        string.Equals(current.Method, replacement.Method, StringComparison.Ordinal) &&
        string.Equals(current.Path, replacement.Path, StringComparison.Ordinal);
    #endregion

    #region 映射（MapEntity）
    /// <summary>
    /// 映射（MapEntity）
    /// </summary>
    /// <param name="value">本次操作使用的Agent 操作审计记录。</param>
    /// <returns>由操作审计记录构造的持久化实体。</returns>
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
    #endregion

    #region 映射（MapRecord）
    /// <summary>
    /// 映射（MapRecord）
    /// </summary>
    /// <param name="value">本次操作使用的操作审计实体。</param>
    /// <returns>从持久化字段还原的操作审计记录。</returns>
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

    #region 按数据库时间精度比较 UTC 时间（StoredDateTimeEquals）
    /// <summary>
    /// 按数据库时间精度比较 UTC 时间（StoredDateTimeEquals）。
    /// </summary>
    /// <param name="stored">数据库中保存的 UTC 时间。</param>
    /// <param name="value">待比较的带时区偏移时间，比较时转换为 UTC。</param>
    /// <returns>已存储时间与待比较值的 UTC 时间相差不超过 2 毫秒时返回 true，否则返回 false。</returns>
    private static bool StoredDateTimeEquals(DateTime stored, DateTimeOffset value) =>
        Math.Abs((stored - value.UtcDateTime).Ticks) <= TimeSpan.TicksPerMillisecond * 2;
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
        value ?? throw new InvalidDataException($"Agent operation audit field '{field}' is missing.");
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="field">字段名称，用于校验和错误提示。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static string Required(string? value, string field) =>
        value ?? throw new InvalidDataException($"Agent operation audit field '{field}' is missing.");
    #endregion
}
