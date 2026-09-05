using EU.Core.IServices.Knowledge;

#nullable enable

namespace EU.Core.IServices;

#region 文件职责：IAgKnowledgeBaseDefinitionServices 服务契约

/// <summary>
/// 知识库定义、文档导入、发布目录和检索服务契约。
/// </summary>
public interface IAgKnowledgeBaseDefinitionServices : IBaseServices<AgKnowledgeBaseDefinition>, IKnowledgeRetriever
{
    /// <summary>从文档内容提取可索引文本。</summary>
    Task<KnowledgePdfExtractionResult> ExtractAsync(
        ReadOnlyMemory<byte> content,
        int maximumPages,
        int maximumCharacters,
        CancellationToken cancellationToken = default);

    /// <summary>按标识获取知识库定义。</summary>
    Task<KnowledgeBaseDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>按业务编码获取知识库定义。</summary>
    Task<KnowledgeBaseDefinition?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>查询知识库定义列表。</summary>
    Task<IReadOnlyList<KnowledgeBaseDefinition>> ListAsync(KnowledgeBaseStatus? status = null, CancellationToken cancellationToken = default);

    /// <summary>创建知识库定义。</summary>
    Task<ServiceResult<KnowledgeBaseDefinition>> CreateAsync(string code, string name, string description, CancellationToken cancellationToken = default);

    /// <summary>更新知识库定义。</summary>
    Task<ServiceResult<KnowledgeBaseDefinition>> UpdateAsync(
        Guid id,
        long expectedLogicalRevision,
        string name,
        string description,
        KnowledgeBaseStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>导入文本知识库文档。</summary>
    Task<ServiceResult<KnowledgeBaseDefinition>> ImportDocumentAsync(
        Guid knowledgeBaseId,
        long expectedLogicalRevision,
        string fileName,
        string mediaType,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>导入并解析 PDF 知识库文档。</summary>
    Task<ServiceResult<KnowledgeBaseDefinition>> ImportPdfDocumentAsync(
        Guid knowledgeBaseId,
        long expectedLogicalRevision,
        string fileName,
        string mediaType,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default);

    /// <summary>删除知识库文档及其索引分块。</summary>
    Task<ServiceResult<KnowledgeBaseDefinition>> DeleteDocumentAsync(
        Guid knowledgeBaseId,
        Guid documentId,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default);

    /// <summary>设置知识库定义的归档状态。</summary>
    Task<ServiceResult<KnowledgeBaseDefinition>> SetArchivedAsync(
        Guid id,
        long expectedLogicalRevision,
        bool archived,
        CancellationToken cancellationToken = default);
}

#endregion
