using EU.Core.IServices.Evaluation;
using EU.Core.IServices.Runtime;
using EU.Core.Model;

#nullable enable

namespace EU.Core.IServices;

#region 文件职责：IAgEvaluationBatchExecutionServices 服务契约

/// <summary>
/// 定义评测批次执行与恢复服务。
/// </summary>
public interface IAgEvaluationBatchExecutionServices
{
    /// <summary>获取评测批次。</summary>
    Task<EvaluationBatchRecord?> GetAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>查询评测批次列表。</summary>
    Task<IReadOnlyList<EvaluationBatchRecord>> ListAsync(Guid suiteId, string tenantId, int take, CancellationToken cancellationToken = default);

    /// <summary>启动并执行评测批次。</summary>
    Task<ServiceResult<EvaluationBatchRecord>> RunAsync(
        Guid suiteId,
        Guid suiteVersionId,
        AgentExecutionIdentity identity,
        CancellationToken cancellationToken = default);
}

#endregion
