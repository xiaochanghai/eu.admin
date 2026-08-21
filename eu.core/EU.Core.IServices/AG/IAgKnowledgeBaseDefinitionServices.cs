using EU.Core.IServices.Knowledge;

#nullable enable

namespace EU.Core.IServices;

/// <summary>
/// 知识库定义、文档导入、发布目录和检索服务契约。
/// </summary>
public interface IAgKnowledgeBaseDefinitionServices : IKnowledgeRetriever
{
    Task<KnowledgePdfExtractionResult> ExtractAsync(
        ReadOnlyMemory<byte> content,
        int maximumPages,
        int maximumCharacters,
        CancellationToken cancellationToken = default);

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
}
