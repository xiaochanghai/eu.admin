using EU.Core.IServices.Evaluation;
using EU.Core.IServices.Runtime;

#nullable enable

namespace EU.Core.IServices;

public interface IAgEvaluationBatchExecutionServices
{
    Task<EvaluationBatchRecord?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvaluationBatchRecord>> ListAsync(
        Guid suiteId,
        string tenantId,
        int take,
        CancellationToken cancellationToken = default);

    Task<EvaluationBatchOperationResult> RunAsync(
        Guid suiteId,
        Guid suiteVersionId,
        AgentExecutionIdentity identity,
        CancellationToken cancellationToken = default);
}
