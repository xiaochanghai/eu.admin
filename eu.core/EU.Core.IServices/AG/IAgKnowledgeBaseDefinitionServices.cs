using EU.Core.IServices.Knowledge;

#nullable enable

namespace EU.Core.IServices;

/// <summary>
/// 知识库定义、文档导入、发布目录和检索服务契约。
/// </summary>
public interface IAgKnowledgeBaseDefinitionServices : IBaseServices<AgKnowledgeBaseDefinition>, IKnowledgeRetriever
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
        KnowledgeBaseStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<KnowledgeBaseDefinition>> CreateAsync(
        string code,
        string name,
        string description,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<KnowledgeBaseDefinition>> UpdateAsync(
        Guid id,
        long expectedLogicalRevision,
        string name,
        string description,
        KnowledgeBaseStatus status,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<KnowledgeBaseDefinition>> ImportDocumentAsync(
        Guid knowledgeBaseId,
        long expectedLogicalRevision,
        string fileName,
        string mediaType,
        string content,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<KnowledgeBaseDefinition>> ImportPdfDocumentAsync(
        Guid knowledgeBaseId,
        long expectedLogicalRevision,
        string fileName,
        string mediaType,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<KnowledgeBaseDefinition>> DeleteDocumentAsync(
        Guid knowledgeBaseId,
        Guid documentId,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<KnowledgeBaseDefinition>> SetArchivedAsync(
        Guid id,
        long expectedLogicalRevision,
        bool archived,
        CancellationToken cancellationToken = default);
}
