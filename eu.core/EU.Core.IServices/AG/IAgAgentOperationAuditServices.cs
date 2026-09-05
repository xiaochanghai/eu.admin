using EU.Core.Model.Entity;
using EU.Core.IServices.Abstractions.Auditing;

namespace EU.Core.IServices;

// 文件职责：IAgAgentOperationAuditServices 服务契约

/// <summary>
/// Agent API 操作审计服务。
/// </summary>
public interface IAgAgentOperationAuditServices
{
    #region 保存Agent 操作审计记录。
    /// <summary>保存Agent 操作审计记录。</summary>
    /// <param name="record">业务记录。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示操作完成的异步任务。</returns>
    Task SaveAsync(AgentOperationAuditRecord record, CancellationToken cancellationToken = default);
    #endregion

    #region 查询Agent 操作审计记录列表。
    /// <summary>查询Agent 操作审计记录列表。</summary>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户未删除的操作审计记录，按发生时间及标识倒序排列，数量限制为 1 至 100 条。</returns>
    Task<IReadOnlyList<AgentOperationAuditRecord>> ListAsync(string tenantId, int take, CancellationToken cancellationToken = default);
    #endregion
}
