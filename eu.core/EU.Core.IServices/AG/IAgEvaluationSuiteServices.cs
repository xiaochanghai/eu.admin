using EU.Core.IServices.BASE;
using EU.Core.IServices.Evaluation;

namespace EU.Core.IServices;

// 文件职责：IAgEvaluationSuiteServices 服务契约

/// <summary>
/// 评测套件规范化持久化服务。
/// </summary>
public interface IAgEvaluationSuiteServices : IBaseServices<AgEvaluationSuite>
{
    #region 判断指定 Agent 版本是否已经发布且具备可执行快照，可否作为评测目标。
    /// <summary>
    /// 判断指定 Agent 版本是否已经发布且具备可执行快照，可否作为评测目标。
    /// </summary>
    /// <param name="agentId">Agent 定义标识。</param>
    /// <param name="agentVersionId">Agent 版本标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>异步查询结果：指定版本属于该 Agent 的已发布版本且具有非空快照时返回 true，否则返回 false。</returns>
    Task<bool> IsPublishedAsync(Guid agentId, Guid agentVersionId, CancellationToken cancellationToken = default);
    #endregion

    #region 创建评测套件。
    /// <summary>创建评测套件。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含评测套件定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<EvaluationSuiteDefinition>> CreateAsync(CreateEvaluationSuiteCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region 获取评测套件。
    /// <summary>获取评测套件。</summary>
    /// <param name="id">评测套件标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户下包含草稿、发布版本及用例的评测套件；不存在时为 null。</returns>
    Task<EvaluationSuiteDefinition?> GetAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);
    #endregion

    #region 查询评测套件列表。
    /// <summary>查询评测套件列表。</summary>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户下匹配状态的评测套件；未指定状态时排除已归档套件。</returns>
    Task<IReadOnlyList<EvaluationSuiteDefinition>> ListAsync(
        string tenantId,
        EvaluationSuiteStatus? status = null,
        CancellationToken cancellationToken = default);
    #endregion

    #region 保存评测套件草稿。
    /// <summary>保存评测套件草稿。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含评测套件定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<EvaluationSuiteDefinition>> SaveDraftAsync(SaveEvaluationSuiteDraftCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region 发布评测套件。
    /// <summary>发布评测套件。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含评测套件定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<EvaluationSuiteDefinition>> PublishAsync(PublishEvaluationSuiteCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region 设置评测套件的归档状态。
    /// <summary>设置评测套件的归档状态。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含评测套件定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<EvaluationSuiteDefinition>> SetArchivedAsync(SetEvaluationSuiteArchiveCommand command, CancellationToken cancellationToken = default);
    #endregion
}
