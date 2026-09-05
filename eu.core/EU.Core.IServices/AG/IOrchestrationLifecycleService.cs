using EU.Core.IServices.Orchestration;

#nullable enable

namespace EU.Core.IServices;

#region 文件职责：IOrchestrationLifecycleService 服务契约

/// <summary>
/// 定义编排生命周期管理服务。
/// </summary>
public interface IOrchestrationLifecycleService
{
    /// <summary>创建编排定义。</summary>
    Task<ServiceResult<OrchestrationDefinition>> CreateAsync(CreateOrchestrationCommand command, CancellationToken cancellationToken = default);

    /// <summary>保存编排定义草稿。</summary>
    Task<ServiceResult<OrchestrationDefinition>> SaveDraftAsync(SaveOrchestrationDraftCommand command, CancellationToken cancellationToken = default);

    /// <summary>发布编排定义。</summary>
    Task<ServiceResult<OrchestrationDefinition>> PublishAsync(PublishOrchestrationCommand command, CancellationToken cancellationToken = default);

    /// <summary>获取编排定义。</summary>
    Task<OrchestrationDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>查询编排定义列表。</summary>
    Task<IReadOnlyList<OrchestrationListItem>> ListAsync(OrchestrationStatus? status = null, CancellationToken cancellationToken = default);

    /// <summary>设置编排定义的归档状态。</summary>
    Task<ServiceResult<OrchestrationDefinition>> SetArchivedAsync(SetOrchestrationArchiveCommand command, CancellationToken cancellationToken = default);
}

#endregion
