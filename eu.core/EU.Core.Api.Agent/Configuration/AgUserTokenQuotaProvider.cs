using EU.Core.IServices;
using EU.Core.IServices.Runtime;
using Microsoft.Extensions.Options;
using EU.Core.Api.Agent.Observability;

namespace EU.Core.Api.Agent.Configuration;

/// <summary>单例运行时与作用域数据库服务间的宿主适配器，不持有数据库作用域。</summary>
public sealed class AgUserTokenQuotaProvider(IServiceScopeFactory scopeFactory, IOptions<AgentUserTokenQuotaOptions> options, TimeProvider timeProvider, ILogger<AgUserTokenQuotaProvider> logger, AgentMetrics? metrics = null) : IAgentUserTokenQuota
{
    private readonly AgentMetrics? _metrics = metrics;
    #region 查询租户共享额度（GetBalanceAsync）
    /// <summary>复用当前集团、公司的配置和短生命周期业务服务；关闭时不解析数据库或时区。</summary>
    /// <param name="identity">HTTP 边界提供的可信身份。</param>
    /// <param name="cancellationToken">读取取消令牌。</param>
    /// <returns>当前集团公司的共享余额快照。</returns>
    public async Task<AgentUserTokenQuotaBalance> GetBalanceAsync(AgentExecutionIdentity identity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (configuration, _) = await ResolveConfigurationAsync(identity, cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (!configuration.Enabled) return new(false, configuration.TimeZoneId, now, null, null, []);
        var query = AgentUserTokenQuotaRequests.CreateQuery(identity, now, TimeZoneInfo.FindSystemTimeZoneById(configuration.TimeZoneId),
            configuration.DailyTotalTokens, configuration.MonthlyTotalTokens, configuration.RequestReservationTokens!.Value);
        IReadOnlyList<AgentUserTokenQuotaPeriodBalance>? periods = null;
        await ExecuteAsync(async service => periods = await service.GetBalanceAsync(query, cancellationToken), cancellationToken);
        if (periods is null || periods.Count != query.Windows.Count)
            throw new AgentRuntimeException(AgentRunErrorCodes.ModelTokenQuotaUnavailable, "The shared tenant Token quota ledger is unavailable.");
        return new(true, configuration.TimeZoneId, now, query.RequestReservationTokens,
            periods.All(x => x.CanReserve), periods);
    }
    #endregion

    #region 预占租户共享额度（ReserveAsync）
    /// <summary>关闭额度不解析数据库服务；启用时从可信运行身份创建周期预占。</summary>
    /// <param name="context">可信执行上下文。</param>
    /// <param name="cancellationToken">开始前取消令牌。</param>
    /// <returns>不跨请求复用的租约或空值。</returns>
    public async Task<IAgentUserTokenQuotaLease?> ReserveAsync(AgentRunContext context, CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled && !options.Value.ManagementEnabled) return null;
        var (configuration, revision) = await ResolveConfigurationAsync(context.ExecutionIdentity ??
            throw new AgentRuntimeException(AgentRunErrorCodes.ModelTokenQuotaUnavailable, "Quota identity is unavailable."), cancellationToken);
        if (!configuration.Enabled) return null;
        cancellationToken.ThrowIfCancellationRequested();
        var request = AgentUserTokenQuotaRequests.Create(context, timeProvider.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById(configuration.TimeZoneId),
            configuration.DailyTotalTokens, configuration.MonthlyTotalTokens, configuration.RequestReservationTokens!.Value) with { PolicyRevision = revision };
        try { await ExecuteAsync(service => service.ReserveAsync(request, cancellationToken), cancellationToken); }
        catch (AgentRuntimeException exception)
        {
            if (exception.ErrorCode == AgentRunErrorCodes.ModelTokenQuotaExceeded) RecordQuotaSignal(AgentUserQuotaSignal.Rejected);
            if (exception.ErrorCode == AgentRunErrorCodes.ModelTokenUsageUnavailable) RecordQuotaSignal(AgentUserQuotaSignal.Frozen);
            throw;
        }
        return new Lease(this, request);
    }
    #endregion

    #region 解析租户额度覆盖（ResolveConfigurationAsync）
    /// <summary>仅在显式启用管理时读取当前集团、公司的覆盖设置；保持原配置源和关闭时无数据库依赖。</summary>
    /// <param name="identity">可信所有者。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>本次请求设置及用于事务校验的版本。</returns>
    private async Task<(AgentUserTokenQuotaOptions Configuration, long? Revision)> ResolveConfigurationAsync(AgentExecutionIdentity identity, CancellationToken cancellationToken)
    {
        var configured = options.Value;
        if (!configured.ManagementEnabled) return (configured, null);
        if (identity.GroupId is not { } groupId || groupId == Guid.Empty ||
            identity.CompanyId is not { } companyId || companyId == Guid.Empty)
            throw new AgentRuntimeException(AgentRunErrorCodes.ModelTokenQuotaUnavailable, "Quota identity is unavailable.");
        AgentUserTokenQuotaPolicy? policy = null;
        await ExecuteAsync(async service => policy = await service.GetPolicyAsync(groupId, companyId, cancellationToken), cancellationToken);
        if (policy is null) throw new AgentRuntimeException(AgentRunErrorCodes.ModelTokenQuotaUnavailable, "Quota policy is unavailable.");
        return (policy.UseDefaults ? configured : new AgentUserTokenQuotaOptions { TimeZoneId = configured.TimeZoneId,
            DailyTotalTokens = policy.DailyTotalTokens, MonthlyTotalTokens = policy.MonthlyTotalTokens, RequestReservationTokens = policy.RequestReservationTokens }, policy.Revision);
    }
    #endregion

    #region 在短作用域内访问账本（ExecuteAsync）
    /// <summary>每次操作独立解析/释放现有业务服务；底层失败关闭而不是返回零额度。</summary>
    /// <param name="action">账本原子操作。</param>
    /// <param name="cancellationToken">只有调用方确实取消时才传播取消异常，结算默认不可取消。</param>
    /// <returns>操作完成的任务。</returns>
    private async Task ExecuteAsync(Func<IAgUserTokenQuotaServices, Task> action, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await action(scope.ServiceProvider.GetRequiredService<IAgUserTokenQuotaServices>());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (AgentRuntimeException exception)
        {
            if (exception.ErrorCode is AgentRunErrorCodes.ModelTokenQuotaUnavailable or AgentUserTokenQuotaManagementErrors.Conflict)
                RecordQuotaSignal(AgentUserQuotaSignal.Unavailable);
            throw;
        }
        catch (Exception exception)
        {
            // 不把数据库异常对象交给日志，避免其中的 SQL、连接串和用户数据泄漏。
            logger.LogError("{ErrorCode}; exception type: {ExceptionType}", AgentRunErrorCodes.ModelTokenQuotaUnavailable, exception.GetType().Name);
            RecordQuotaSignal(AgentUserQuotaSignal.Unavailable);
            throw new AgentRuntimeException(AgentRunErrorCodes.ModelTokenQuotaUnavailable, "The shared tenant Token quota ledger is unavailable.");
        }
    }
    #endregion

    #region 隔离监控故障（RecordQuotaSignal）
    /// <summary>监控监听器失败不能替代额度决策、重复结算或将已提交账本判为失败。</summary>
    /// <param name="signal">固定有界信号。</param>
    private void RecordQuotaSignal(AgentUserQuotaSignal signal)
    {
        try { _metrics?.RecordUserQuota(signal); }
        catch (Exception) { /* 指标是观测路径，不参与事务及准入决策。 */ }
    }
    #endregion

    private sealed class Lease(AgUserTokenQuotaProvider owner, AgentUserTokenQuotaRequest request) : IAgentUserTokenQuotaLease
    {
        private bool _started;
        private bool _settled;
        public long ReservedTokens => request.ReservedTokens;

        #region 保存已开始状态（MarkStartedAsync）
        /// <summary>开始状态提交后才允许供应商调用。</summary>
        /// <param name="cancellationToken">开始前取消令牌。</param>
        /// <returns>状态提交任务。</returns>
        public async Task MarkStartedAsync(CancellationToken cancellationToken = default)
        {
            if (_started || _settled) throw new InvalidOperationException("A quota lease cannot dispatch more than one request.");
            await owner.ExecuteAsync(service => service.MarkStartedAsync(request, cancellationToken), cancellationToken);
            _started = true;
        }
        #endregion

        #region 结算完整响应（CompleteAsync）
        /// <summary>结算不受已取消的请求令牌影响。</summary>
        /// <param name="totalTokens">实际总 Token，未知为空。</param>
        /// <returns>结算提交任务。</returns>
        public async Task CompleteAsync(long? totalTokens)
        {
            if (!_started || _settled) throw new InvalidOperationException("A quota lease must be started and unsettled.");
            await owner.ExecuteAsync(service => service.SettleAsync(request, totalTokens));
            if (totalTokens is null) owner.RecordQuotaSignal(AgentUserQuotaSignal.Frozen);
            _settled = true;
        }
        #endregion

        #region 异常及取消清理（DisposeAsync）
        /// <summary>未开始的请求退还预占；已开始但无完整响应的请求冻结周期，不退回可能的消耗。</summary>
        /// <returns>非取消清理任务，失败不得伪报成功。</returns>
        public async ValueTask DisposeAsync()
        {
            if (_settled) return;
            await owner.ExecuteAsync(service => service.SettleAsync(request, null, releaseUnstarted: !_started));
            if (_started) owner.RecordQuotaSignal(AgentUserQuotaSignal.Frozen);
            _settled = true;
        }
        #endregion
    }
}
