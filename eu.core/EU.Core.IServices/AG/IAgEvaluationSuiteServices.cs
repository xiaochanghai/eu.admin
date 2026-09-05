using EU.Core.IServices.BASE;
using EU.Core.IServices.Evaluation;

namespace EU.Core.IServices;

#region 文件职责：IAgEvaluationSuiteServices 服务契约

/// <summary>
/// 评测套件规范化持久化服务。
/// </summary>
public interface IAgEvaluationSuiteServices : IBaseServices<AgEvaluationSuite>
{
    /// <summary>
    /// 判断指定 Agent 版本是否已经发布且具备可执行快照，可否作为评测目标。
    /// </summary>
    Task<bool> IsPublishedAsync(Guid agentId, Guid agentVersionId, CancellationToken cancellationToken = default);

    /// <summary>创建评测套件。</summary>
    Task<ServiceResult<EvaluationSuiteDefinition>> CreateAsync(CreateEvaluationSuiteCommand command, CancellationToken cancellationToken = default);

    /// <summary>获取评测套件。</summary>
    Task<EvaluationSuiteDefinition?> GetAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>查询评测套件列表。</summary>
    Task<IReadOnlyList<EvaluationSuiteDefinition>> ListAsync(
        string tenantId,
        EvaluationSuiteStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>保存评测套件草稿。</summary>
    Task<ServiceResult<EvaluationSuiteDefinition>> SaveDraftAsync(SaveEvaluationSuiteDraftCommand command, CancellationToken cancellationToken = default);

    /// <summary>发布评测套件。</summary>
    Task<ServiceResult<EvaluationSuiteDefinition>> PublishAsync(PublishEvaluationSuiteCommand command, CancellationToken cancellationToken = default);

    /// <summary>设置评测套件的归档状态。</summary>
    Task<ServiceResult<EvaluationSuiteDefinition>> SetArchivedAsync(SetEvaluationSuiteArchiveCommand command, CancellationToken cancellationToken = default);
}

#endregion
