using EU.Core.IServices.BASE;
using EU.Core.IServices.Evaluation;

namespace EU.Core.IServices;

/// <summary>
/// 评测套件规范化持久化服务。
/// </summary>
public interface IAgEvaluationSuiteServices : IBaseServices<AgEvaluationSuite>
{
    /// <summary>
    /// 判断指定 Agent 版本是否已经发布且具备可执行快照，可否作为评测目标。
    /// </summary>
    Task<bool> IsPublishedAsync(
        Guid agentId,
        Guid agentVersionId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<EvaluationSuiteDefinition>> CreateAsync(
        CreateEvaluationSuiteCommand command,
        CancellationToken cancellationToken = default);

    Task<EvaluationSuiteDefinition?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvaluationSuiteDefinition>> ListAsync(
        string tenantId,
        EvaluationSuiteStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<EvaluationSuiteDefinition>> SaveDraftAsync(
        SaveEvaluationSuiteDraftCommand command,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<EvaluationSuiteDefinition>> PublishAsync(
        PublishEvaluationSuiteCommand command,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<EvaluationSuiteDefinition>> SetArchivedAsync(
        SetEvaluationSuiteArchiveCommand command,
        CancellationToken cancellationToken = default);
}
