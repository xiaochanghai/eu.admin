using EU.Core.IServices.Orchestration;

#nullable enable

namespace EU.Core.IServices;

// 文件职责：IOrchestrationLifecycleService 服务契约

/// <summary>
/// 定义编排生命周期管理服务。
/// </summary>
public interface IOrchestrationLifecycleService
{
    #region 创建编排定义。
    /// <summary>创建编排定义。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含编排定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<OrchestrationDefinition>> CreateAsync(CreateOrchestrationCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region 保存编排定义草稿。
    /// <summary>保存编排定义草稿。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含编排定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<OrchestrationDefinition>> SaveDraftAsync(SaveOrchestrationDraftCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region 发布编排定义。
    /// <summary>发布编排定义。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含编排定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<OrchestrationDefinition>> PublishAsync(PublishOrchestrationCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region 获取编排定义。
    /// <summary>获取编排定义。</summary>
    /// <param name="id">编排标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定标识的编排定义；不存在时为 null。</returns>
    Task<OrchestrationDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    #endregion

    #region 查询编排定义列表。
    /// <summary>查询编排定义列表。</summary>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>匹配状态的编排摘要集合；未指定状态时排除已归档编排。</returns>
    Task<IReadOnlyList<OrchestrationListItem>> ListAsync(OrchestrationStatus? status = null, CancellationToken cancellationToken = default);
    #endregion

    #region 设置编排定义的归档状态。
    /// <summary>设置编排定义的归档状态。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含编排定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<OrchestrationDefinition>> SetArchivedAsync(SetOrchestrationArchiveCommand command, CancellationToken cancellationToken = default);
    #endregion
}
