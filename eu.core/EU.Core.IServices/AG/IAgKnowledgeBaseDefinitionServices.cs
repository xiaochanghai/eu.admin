using EU.Core.IServices.Knowledge;

#nullable enable

namespace EU.Core.IServices;

// 文件职责：IAgKnowledgeBaseDefinitionServices 服务契约

/// <summary>
/// 知识库定义、文档导入、发布目录和检索服务契约。
/// </summary>
public interface IAgKnowledgeBaseDefinitionServices : IBaseServices<AgKnowledgeBaseDefinition>, IKnowledgeRetriever
{
    #region 从文档内容提取可索引文本。
    /// <summary>从文档内容提取可索引文本。</summary>
    /// <param name="content">待提取文本的原始 PDF 文件字节。</param>
    /// <param name="maximumPages">允许处理的最大页数。</param>
    /// <param name="maximumCharacters">允许的最大字符数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>PDF 文本提取结果；无效输入、加密文件、页数或文本超限及无可提取文本均以失败原因返回，取消操作会抛出异常。</returns>
    Task<KnowledgePdfExtractionResult> ExtractAsync(
        ReadOnlyMemory<byte> content,
        int maximumPages,
        int maximumCharacters,
        CancellationToken cancellationToken = default);
    #endregion

    #region 按标识获取知识库定义。
    /// <summary>按标识获取知识库定义。</summary>
    /// <param name="id">知识库标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定标识的完整知识库定义；不存在时为 null。</returns>
    Task<KnowledgeBaseDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    #endregion

    #region 按业务编码获取知识库定义。
    /// <summary>按业务编码获取知识库定义。</summary>
    /// <param name="code">对象编码或业务错误码。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定编码的完整知识库定义；不存在时为 null。</returns>
    Task<KnowledgeBaseDefinition?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    #endregion

    #region 查询知识库定义列表。
    /// <summary>查询知识库定义列表。</summary>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>按编码及标识排列的知识库定义；未指定状态时排除已归档知识库。</returns>
    Task<IReadOnlyList<KnowledgeBaseDefinition>> ListAsync(KnowledgeBaseStatus? status = null, CancellationToken cancellationToken = default);
    #endregion

    #region 创建知识库定义。
    /// <summary>创建知识库定义。</summary>
    /// <param name="code">对象编码或业务错误码。</param>
    /// <param name="name">对象或字段名称。</param>
    /// <param name="description">对象说明文本。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含知识库定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<KnowledgeBaseDefinition>> CreateAsync(string code, string name, string description, CancellationToken cancellationToken = default);
    #endregion

    #region 更新知识库定义。
    /// <summary>更新知识库定义。</summary>
    /// <param name="id">知识库标识。</param>
    /// <param name="expectedLogicalRevision">并发更新要求匹配的逻辑修订号。</param>
    /// <param name="name">对象或字段名称。</param>
    /// <param name="description">对象说明文本。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含知识库定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<KnowledgeBaseDefinition>> UpdateAsync(
        Guid id,
        long expectedLogicalRevision,
        string name,
        string description,
        KnowledgeBaseStatus status,
        CancellationToken cancellationToken = default);
    #endregion

    #region 导入文本知识库文档。
    /// <summary>导入文本知识库文档。</summary>
    /// <param name="knowledgeBaseId">知识库标识。</param>
    /// <param name="expectedLogicalRevision">并发更新要求匹配的逻辑修订号。</param>
    /// <param name="fileName">文件名称。</param>
    /// <param name="mediaType">内容的媒体类型。</param>
    /// <param name="content">需要规范化、导入或切分的文档文本。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含知识库定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<KnowledgeBaseDefinition>> ImportDocumentAsync(
        Guid knowledgeBaseId,
        long expectedLogicalRevision,
        string fileName,
        string mediaType,
        string content,
        CancellationToken cancellationToken = default);
    #endregion

    #region 导入并解析 PDF 知识库文档。
    /// <summary>导入并解析 PDF 知识库文档。</summary>
    /// <param name="knowledgeBaseId">知识库标识。</param>
    /// <param name="expectedLogicalRevision">并发更新要求匹配的逻辑修订号。</param>
    /// <param name="fileName">文件名称。</param>
    /// <param name="mediaType">内容的媒体类型。</param>
    /// <param name="content">待提取文本的原始 PDF 文件字节。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含知识库定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<KnowledgeBaseDefinition>> ImportPdfDocumentAsync(
        Guid knowledgeBaseId,
        long expectedLogicalRevision,
        string fileName,
        string mediaType,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default);
    #endregion

    #region 删除知识库文档及其索引分块。
    /// <summary>删除知识库文档及其索引分块。</summary>
    /// <param name="knowledgeBaseId">知识库标识。</param>
    /// <param name="documentId">知识库文档标识。</param>
    /// <param name="expectedLogicalRevision">并发更新要求匹配的逻辑修订号。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含知识库定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<KnowledgeBaseDefinition>> DeleteDocumentAsync(
        Guid knowledgeBaseId,
        Guid documentId,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default);
    #endregion

    #region 设置知识库定义的归档状态。
    /// <summary>设置知识库定义的归档状态。</summary>
    /// <param name="id">知识库标识。</param>
    /// <param name="expectedLogicalRevision">并发更新要求匹配的逻辑修订号。</param>
    /// <param name="archived">是否设置为归档状态。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含知识库定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<KnowledgeBaseDefinition>> SetArchivedAsync(
        Guid id,
        long expectedLogicalRevision,
        bool archived,
        CancellationToken cancellationToken = default);
    #endregion
}
