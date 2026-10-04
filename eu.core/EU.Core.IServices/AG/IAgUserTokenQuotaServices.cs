using EU.Core.IServices.Runtime;
using EU.Core.Model.Entity;

namespace EU.Core.IServices;

/// <summary>租户 Token 配额的原子持久化服务，由既有 Autofac 自动注册。</summary>
public interface IAgUserTokenQuotaServices : IBaseServices<AgUserTokenQuotaPeriod>
{
    /// <summary>读取集团、公司的覆盖设置，不创建默认记录。</summary>
    /// <param name="groupId">可信集团。</param>
    /// <param name="companyId">可信公司。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>覆盖设置或版本 0 默认设置。</returns>
    Task<AgentUserTokenQuotaPolicy> GetPolicyAsync(Guid groupId, Guid companyId, CancellationToken cancellationToken = default);
    /// <summary>原子修改额度并追加审计，不重置已用量。</summary>
    /// <param name="groupId">可信集团。</param>
    /// <param name="companyId">可信公司。</param>
    /// <param name="operatorId">已授权操作者。</param>
    /// <param name="input">幂等及版本化命令。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>提交后的设置。</returns>
    Task<AgentUserTokenQuotaPolicy> SavePolicyAsync(Guid groupId, Guid companyId, Guid operatorId, AgentUserTokenQuotaPolicyInput input, CancellationToken cancellationToken = default);
    /// <summary>按所有者读取有界待对账列表，活跃状态不代表允许释放。</summary>
    /// <param name="groupId">可信集团。</param>
    /// <param name="companyId">可信公司。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>最近 100 条未结算预占。</returns>
    Task<IReadOnlyList<AgentUserTokenQuotaPending>> GetPendingAsync(Guid groupId, Guid companyId, CancellationToken cancellationToken = default);
    /// <summary>依据核实用量结算原始周期并追加审计，禁止对活跃请求退款。</summary>
    /// <param name="groupId">可信集团。</param>
    /// <param name="companyId">可信公司。</param>
    /// <param name="operatorId">已授权操作者。</param>
    /// <param name="reservationId">待对账预占。</param>
    /// <param name="input">明确确认及依据。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>幂等对账回执。</returns>
    Task<AgentUserTokenQuotaReconciliation> ReconcileAsync(Guid groupId, Guid companyId, Guid operatorId, Guid reservationId, AgentUserTokenQuotaReconcileInput input, CancellationToken cancellationToken = default);
    /// <summary>只读获取当前所有者周期，未发生使用的周期按真实零用量返回，不创建账本。</summary>
    /// <param name="query">可信所有者及当前配置周期。</param>
    /// <param name="cancellationToken">查询取消令牌。</param>
    /// <returns>同一事务读取的周期快照。</returns>
    Task<IReadOnlyList<AgentUserTokenQuotaPeriodBalance>> GetBalanceAsync(AgentUserTokenQuotaQuery query, CancellationToken cancellationToken = default);
    /// <summary>在一个事务内预占所有周期，相同预占标识禁止重复使用。</summary>
    /// <param name="request">可信内部预占命令。</param>
    /// <param name="cancellationToken">预占取消令牌。</param>
    /// <returns>持久化完成的任务。</returns>
    Task ReserveAsync(AgentUserTokenQuotaRequest request, CancellationToken cancellationToken = default);
    /// <summary>标记请求已开始，同一预占只允许一次成功开始。</summary>
    /// <param name="request">原始预占命令及所有者。</param>
    /// <param name="cancellationToken">开始前取消令牌。</param>
    /// <returns>状态更新完成的任务。</returns>
    Task MarkStartedAsync(AgentUserTokenQuotaRequest request, CancellationToken cancellationToken = default);
    /// <summary>原子、幂等结算；未知用量冻结原始周期，不受请求取消影响。</summary>
    /// <param name="request">原始预占命令。</param>
    /// <param name="totalTokens">完整的非负总用量，null 为未知。</param>
    /// <param name="releaseUnstarted">仅允许释放从未开始的请求。</param>
    /// <returns>结算完成的任务。</returns>
    Task SettleAsync(AgentUserTokenQuotaRequest request, long? totalTokens, bool releaseUnstarted = false);
}
