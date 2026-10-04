using EU.Core.IServices.Runtime;

#nullable enable

namespace EU.Core.Services;

/// <summary>基于现有 SqlSugar 仓储的租户配额服务；日/月账本与请求记录共同提交。</summary>
public sealed partial class AgUserTokenQuotaServices : BaseServices<AgUserTokenQuotaPeriod>, IAgUserTokenQuotaServices
{
    #region 构造（AgUserTokenQuotaServices）
    /// <summary>使用既有仓储和主数据库，不创建第二套连接或存储。</summary>
    /// <param name="dal">配额周期仓储。</param>
    public AgUserTokenQuotaServices(IBaseRepository<AgUserTokenQuotaPeriod> dal) : base(dal) { }
    #endregion

    #region 查询当前租户周期余额（GetBalanceAsync）
    /// <summary>在只读事务内读取日/月快照；不插入、释放预占或修改任何账本。</summary>
    /// <param name="query">可信集团、公司及当前周期。</param>
    /// <param name="cancellationToken">查询取消令牌。</param>
    /// <returns>已结算量、预占及可用余额；冻结周期的余额为未知。</returns>
    public async Task<IReadOnlyList<AgentUserTokenQuotaPeriodBalance>> GetBalanceAsync(AgentUserTokenQuotaQuery query, CancellationToken cancellationToken = default)
    {
        ValidateQuery(query);
        var balances = new List<AgentUserTokenQuotaPeriodBalance>(2);
        await InTransactionAsync(async () =>
        {
            foreach (var window in query.Windows.OrderBy(x => x.Kind))
            {
                // 显式限定所有者后包含软删除行；不可将损坏/删除账本冒充全新零用量。
                var period = await Db.Queryable<AgUserTokenQuotaPeriod>().ClearFilter()
                    .Where(x => x.ID == window.Id && x.GroupId == query.GroupId && x.CompanyId == query.CompanyId).SingleAsync();
                if (period is not null && (period.IsDeleted || period.PeriodKind != window.Kind || period.PeriodKey != window.Key ||
                    period.StartUtc != window.StartUtc || period.EndUtc != window.EndUtc || period.UsedTokens < 0 || period.ReservedTokens < 0 || period.Revision < 0)) throw Unavailable();
                long used = period?.UsedTokens ?? 0;
                long reserved = period?.ReservedTokens ?? 0;
                bool unknown = period?.HasUnknownUsage ?? false;
                // 减法分段比较，避免超支或 long 溢出造成正余额。
                long remaining = used >= window.LimitTokens || reserved >= window.LimitTokens - used ? 0 : window.LimitTokens - used - reserved;
                balances.Add(new(window.Kind == 0 ? "Daily" : "Monthly", new DateTimeOffset(window.StartUtc, TimeSpan.Zero),
                    new DateTimeOffset(window.EndUtc, TimeSpan.Zero), window.LimitTokens, used, reserved, unknown ? null : remaining,
                    unknown, !unknown && remaining >= query.RequestReservationTokens));
            }
        }, cancellationToken);
        return balances;
    }
    #endregion

    #region 同时预占日/月额度（ReserveAsync）
    /// <summary>Serializable 事务及版本条件更新保证并发调用不能重复消费剩余额度。</summary>
    /// <param name="request">可信身份生成的预占命令。</param>
    /// <param name="cancellationToken">预占取消令牌。</param>
    /// <returns>两个周期和请求记录一起持久化完成的任务。</returns>
    public async Task ReserveAsync(AgentUserTokenQuotaRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        await InTransactionAsync(async () =>
        {
            // 不复用旧预占授权新的网络请求，重复 ID 失败关闭。
            if (request.PolicyRevision.HasValue && (await ReadPolicyAsync(request.GroupId, request.CompanyId)).Revision != request.PolicyRevision.Value)
                throw Conflict();
            if (await Db.Queryable<AgUserTokenQuotaReservation>().ClearFilter()
                .Where(x => x.ID == request.Id && x.GroupId == request.GroupId && x.CompanyId == request.CompanyId).AnyAsync()) throw Unavailable();
            foreach (var window in request.Windows.OrderBy(x => x.Kind))
            {
                var period = await LoadPeriodAsync(request, window);
                if (period is null)
                {
                    period = new() { ID = window.Id, GroupId = request.GroupId, CompanyId = request.CompanyId,
                        PeriodKind = window.Kind, PeriodKey = window.Key, StartUtc = window.StartUtc, EndUtc = window.EndUtc };
                    await Db.Insertable(period).ExecuteCommandAsync();
                }
                if (period.HasUnknownUsage) throw new AgentRuntimeException(AgentRunErrorCodes.ModelTokenUsageUnavailable, "Token quota usage is unresolved for the current period.");
                if (period.UsedTokens < 0 || period.ReservedTokens < 0 || period.Revision < 0) throw Unavailable();
                if (period.UsedTokens > window.LimitTokens || period.ReservedTokens > window.LimitTokens - period.UsedTokens ||
                    request.ReservedTokens > window.LimitTokens - period.UsedTokens - period.ReservedTokens)
                    throw new AgentRuntimeException(AgentRunErrorCodes.ModelTokenQuotaExceeded, "The shared tenant Token quota has insufficient capacity.");
                await UpdatePeriodAsync(request, period, period.UsedTokens, checked(period.ReservedTokens + request.ReservedTokens), false);
            }
            await Db.Insertable(new AgUserTokenQuotaReservation
            {
                ID = request.Id, RunId = request.RunId, GroupId = request.GroupId, CompanyId = request.CompanyId, ConsumerUserId = request.ConsumerUserId,
                DailyPeriodId = request.Windows.SingleOrDefault(x => x.Kind == 0)?.Id,
                MonthlyPeriodId = request.Windows.SingleOrDefault(x => x.Kind == 1)?.Id,
                ReservedTokens = request.ReservedTokens, State = 0
            }).ExecuteCommandAsync();
        }, cancellationToken);
    }
    #endregion

    #region 标记真实请求开始（MarkStartedAsync）
    /// <summary>持久化状态只能从预占转为开始一次，禁止重复派发。</summary>
    /// <param name="request">原始预占命令。</param>
    /// <param name="cancellationToken">开始前取消令牌。</param>
    /// <returns>状态提交完成的任务。</returns>
    public async Task MarkStartedAsync(AgentUserTokenQuotaRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        await InTransactionAsync(async () =>
        {
            var entry = await LoadReservationAsync(request);
            if (entry.State != 0) throw Unavailable();
            int changed = await Db.Updateable<AgUserTokenQuotaReservation>().SetColumns(x => x.State == 1)
                .Where(x => x.ID == request.Id && x.GroupId == request.GroupId && x.CompanyId == request.CompanyId && x.ConsumerUserId == request.ConsumerUserId && x.RunId == request.RunId && x.State == 0 && !x.IsDeleted)
                .ExecuteCommandAsync();
            if (changed != 1) throw Unavailable();
        }, cancellationToken);
    }
    #endregion

    #region 结算实际用量（SettleAsync）
    /// <summary>固定结算原始周期，跨日/月不转账；未知用量保留预占并冻结周期。</summary>
    /// <param name="request">原始预占命令和所有者。</param>
    /// <param name="totalTokens">完整报告的实际总用量，null 为未知。</param>
    /// <param name="releaseUnstarted">是否释放从未开始的预占。</param>
    /// <returns>幂等结算持久化任务，不使用请求取消令牌。</returns>
    public async Task SettleAsync(AgentUserTokenQuotaRequest request, long? totalTokens, bool releaseUnstarted = false)
    {
        Validate(request);
        if (totalTokens is < 0 || (releaseUnstarted && totalTokens is not null)) throw Unavailable();
        int target = releaseUnstarted ? 4 : totalTokens.HasValue ? 2 : 3;
        await InTransactionAsync(async () =>
        {
            var entry = await LoadReservationAsync(request);
            // 人工核实结果优先于迟到的未知清理，不得重复扣账；冲突的已知用量仍失败关闭。
            if (entry.State == 5 && (totalTokens is null || entry.ActualTokens == totalTokens)) return;
            if (entry.State == target && entry.ActualTokens == totalTokens) return;
            if (entry.State != (releaseUnstarted ? 0 : 1)) throw Unavailable();
            foreach (var window in request.Windows.OrderBy(x => x.Kind))
            {
                var period = await LoadPeriodAsync(request, window) ?? throw Unavailable();
                if (period.ReservedTokens < request.ReservedTokens || period.UsedTokens < 0) throw Unavailable();
                bool overflow = totalTokens.HasValue && totalTokens.Value > long.MaxValue - period.UsedTokens;
                long used = overflow ? long.MaxValue : checked(period.UsedTokens + (totalTokens ?? 0));
                long reserved = target == 3 ? period.ReservedTokens : period.ReservedTokens - request.ReservedTokens;
                await UpdatePeriodAsync(request, period, used, reserved, period.HasUnknownUsage || target == 3 || overflow);
            }
            int changed = await Db.Updateable<AgUserTokenQuotaReservation>()
                .SetColumns(x => new AgUserTokenQuotaReservation { State = target, ActualTokens = totalTokens, SettledAtUtc = DateTime.UtcNow })
                .Where(x => x.ID == request.Id && x.GroupId == request.GroupId && x.CompanyId == request.CompanyId && x.ConsumerUserId == request.ConsumerUserId && x.RunId == request.RunId && x.State == entry.State && !x.IsDeleted)
                .ExecuteCommandAsync();
            if (changed != 1) throw Unavailable();
        }, CancellationToken.None);
    }
    #endregion

    #region 加载指定所有者周期（LoadPeriodAsync）
    /// <summary>显式检查集团、公司及窗口事实；不能依赖全局租户过滤器。</summary>
    /// <param name="request">可信所有者。</param>
    /// <param name="window">原始周期。</param>
    /// <returns>一致的账本或空值。</returns>
    private async Task<AgUserTokenQuotaPeriod?> LoadPeriodAsync(AgentUserTokenQuotaRequest request, AgentUserTokenQuotaWindow window)
    {
        var row = await Db.Queryable<AgUserTokenQuotaPeriod>().ClearFilter()
            .Where(x => x.ID == window.Id && x.GroupId == request.GroupId && x.CompanyId == request.CompanyId && !x.IsDeleted).SingleAsync();
        if (row is not null && (row.PeriodKind != window.Kind || row.PeriodKey != window.Key || row.StartUtc != window.StartUtc || row.EndUtc != window.EndUtc)) throw Unavailable();
        return row;
    }
    #endregion

    #region 校验原始预占（LoadReservationAsync）
    /// <summary>所有状态变更同时校验集团、公司、调用用户、运行及原始预占量/周期。</summary>
    /// <param name="request">原始命令。</param>
    /// <returns>归属一致的请求账本。</returns>
    private async Task<AgUserTokenQuotaReservation> LoadReservationAsync(AgentUserTokenQuotaRequest request)
    {
        var row = await Db.Queryable<AgUserTokenQuotaReservation>().ClearFilter()
            .Where(x => x.ID == request.Id && x.GroupId == request.GroupId && x.CompanyId == request.CompanyId && x.ConsumerUserId == request.ConsumerUserId && x.RunId == request.RunId && !x.IsDeleted).SingleAsync();
        if (row is null || row.ReservedTokens != request.ReservedTokens ||
            row.DailyPeriodId != request.Windows.SingleOrDefault(x => x.Kind == 0)?.Id ||
            row.MonthlyPeriodId != request.Windows.SingleOrDefault(x => x.Kind == 1)?.Id) throw Unavailable();
        return row;
    }
    #endregion

    #region 乐观并发更新账本（UpdatePeriodAsync）
    /// <summary>绝对值更新同时校验原版本，失败回滚整个日/月事务。</summary>
    /// <param name="request">可信所有者。</param>
    /// <param name="period">事务读取的周期快照。</param>
    /// <param name="used">新已用量。</param>
    /// <param name="reserved">新预占量。</param>
    /// <param name="unknown">是否冻结未知用量。</param>
    /// <returns>单行更新完成的任务。</returns>
    private async Task UpdatePeriodAsync(AgentUserTokenQuotaRequest request, AgUserTokenQuotaPeriod period, long used, long reserved, bool unknown)
    {
        long revision = checked(period.Revision + 1);
        int changed = await Db.Updateable<AgUserTokenQuotaPeriod>()
            .SetColumns(x => new AgUserTokenQuotaPeriod { UsedTokens = used, ReservedTokens = reserved, HasUnknownUsage = unknown, Revision = revision })
            .Where(x => x.ID == period.ID && x.GroupId == request.GroupId && x.CompanyId == request.CompanyId && x.Revision == period.Revision && !x.IsDeleted)
            .ExecuteCommandAsync();
        if (changed != 1) throw Unavailable();
    }
    #endregion

    #region 事务边界（InTransactionAsync）
    /// <summary>取消在提交前检查，清理/回滚不受调用方取消影响。</summary>
    /// <param name="action">原子账本操作。</param>
    /// <param name="cancellationToken">开始及提交前检查的取消令牌。</param>
    /// <returns>事务完成的任务。</returns>
    private async Task InTransactionAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            await action();
            cancellationToken.ThrowIfCancellationRequested();
            await Db.Ado.CommitTranAsync();
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #region 校验内部命令（Validate）
    /// <summary>拒绝空所有者、重复周期或无效金额，避免部分账本更新。</summary>
    /// <param name="request">内部预占命令。</param>
    private static void Validate(AgentUserTokenQuotaRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Id == Guid.Empty || request.RunId == Guid.Empty || request.ConsumerUserId == Guid.Empty) throw Unavailable();
        ValidateQuery(new(request.GroupId, request.CompanyId, request.ReservedTokens, request.Windows));
    }
    #endregion

    #region 校验内部周期查询（ValidateQuery）
    /// <summary>查询和预占共用所有者及周期校验，不为只读操作伪造运行标识。</summary>
    /// <param name="query">内部周期查询。</param>
    private static void ValidateQuery(AgentUserTokenQuotaQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.GroupId == Guid.Empty || query.CompanyId == Guid.Empty || query.RequestReservationTokens < 1 || query.Windows.Count is < 1 or > 2 ||
            query.Windows.Select(x => x.Kind).Distinct().Count() != query.Windows.Count ||
            query.Windows.Select(x => x.Id).Distinct().Count() != query.Windows.Count ||
            query.Windows.Any(x => x.Id == Guid.Empty || x.Kind is < 0 or > 1 || x.Key.Length is < 1 or > 160 ||
                x.StartUtc >= x.EndUtc || x.LimitTokens < query.RequestReservationTokens)) throw Unavailable();
    }
    #endregion

    #region 创建安全账本异常（Unavailable）
    /// <summary>不输出 SQL、用户标识、凭据或底层异常消息。</summary>
    /// <returns>固定领域错误。</returns>
    private static AgentRuntimeException Unavailable() => new(AgentRunErrorCodes.ModelTokenQuotaUnavailable, "The shared tenant Token quota ledger is unavailable or inconsistent.");
    #endregion
}
