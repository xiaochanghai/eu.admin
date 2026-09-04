using EU.Core.IServices.Orchestration;

#nullable enable

namespace EU.Core.IServices;

public interface IOrchestrationLifecycleService
{
    Task<ServiceResult<OrchestrationDefinition>> CreateAsync(
        CreateOrchestrationCommand command,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<OrchestrationDefinition>> SaveDraftAsync(
        SaveOrchestrationDraftCommand command,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<OrchestrationDefinition>> PublishAsync(
        PublishOrchestrationCommand command,
        CancellationToken cancellationToken = default);

    Task<OrchestrationDefinition?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrchestrationListItem>> ListAsync(
        OrchestrationStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<OrchestrationDefinition>> SetArchivedAsync(
        SetOrchestrationArchiveCommand command,
        CancellationToken cancellationToken = default);
}
