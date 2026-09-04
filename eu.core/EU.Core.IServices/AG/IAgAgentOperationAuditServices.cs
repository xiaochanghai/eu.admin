using EU.Core.Model.Entity;
using EU.Core.IServices.Abstractions.Auditing;

namespace EU.Core.IServices;

#region 文件职责：IAgAgentOperationAuditServices 服务契约

/// <summary>
/// Agent API 操作审计服务。
/// </summary>
public interface IAgAgentOperationAuditServices
{
    Task SaveAsync(AgentOperationAuditRecord record, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgentOperationAuditRecord>> ListAsync(string tenantId, int take, CancellationToken cancellationToken = default);
}

#endregion
