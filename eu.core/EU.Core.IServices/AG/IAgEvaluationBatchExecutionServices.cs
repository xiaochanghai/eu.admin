using EU.Core.IServices.Evaluation;
using EU.Core.IServices.Runtime;
using EU.Core.Model;

#nullable enable

namespace EU.Core.IServices;

#region 文件职责：IAgEvaluationBatchExecutionServices 服务契约

public interface IAgEvaluationBatchExecutionServices
{
    Task<EvaluationBatchRecord?> GetAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvaluationBatchRecord>> ListAsync(Guid suiteId, string tenantId, int take, CancellationToken cancellationToken = default);

    Task<ServiceResult<EvaluationBatchRecord>> RunAsync(
        Guid suiteId,
        Guid suiteVersionId,
        AgentExecutionIdentity identity,
        CancellationToken cancellationToken = default);
}

#endregion
