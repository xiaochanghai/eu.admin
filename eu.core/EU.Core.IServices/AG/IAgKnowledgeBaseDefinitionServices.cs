using EU.Core.Agent.Application.Knowledge;

#nullable enable

namespace EU.Core.IServices;

/// <summary>
/// 鐭ヨ瘑搴撹鑼冨寲鎸佷箙鍖栨湇鍔°€?
/// </summary>
public interface IAgKnowledgeBaseDefinitionServices
{
    Task<KnowledgeBaseDefinition?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<KnowledgeBaseDefinition?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgeBaseDefinition>> ListAsync(
        KnowledgeBaseQuery query,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<KnowledgeBaseDefinition>> CreateAsync(
        CreateKnowledgeBaseCommand command,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<KnowledgeBaseDefinition>> UpdateAsync(
        UpdateKnowledgeBaseCommand command,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<KnowledgeBaseDefinition>> ImportDocumentAsync(
        ImportKnowledgeDocumentCommand command,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<KnowledgeBaseDefinition>> ImportPdfDocumentAsync(
        ImportPdfKnowledgeDocumentCommand command,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<KnowledgeBaseDefinition>> SetArchivedAsync(
        SetKnowledgeBaseArchiveCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PublishedKnowledgeReference>> ListPublishedAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        IReadOnlyList<Guid> knowledgeBaseIds,
        string query,
        int take,
        CancellationToken cancellationToken = default);
}

