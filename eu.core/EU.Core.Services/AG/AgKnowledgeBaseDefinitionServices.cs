using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Exceptions;

#nullable enable

namespace EU.Core.Services;

// 文件职责：AgKnowledgeBaseDefinitionServices 职责实现

/// <summary>
/// 知识库定义、文档和检索分块的规范化持久化服务。
/// </summary>
public sealed class AgKnowledgeBaseDefinitionServices : BaseServices<AgKnowledgeBaseDefinition>, IAgKnowledgeBaseDefinitionServices
{
    /// <summary>单个知识库文档允许的最大字符数。</summary>
    public const int MaximumDocumentCharacters = 1_000_000;
    /// <summary>单个知识库允许导入的最大文档数量。</summary>
    public const int MaximumDocuments = 100;
    /// <summary>单个 PDF 文档允许的最大字节数。</summary>
    public const int MaximumPdfBytes = 10_485_760;
    /// <summary>单个 PDF 文档允许的最大页数。</summary>
    public const int MaximumPdfPages = 200;

    private readonly Lazy<IAgentDefinitionCatalog>? agents;

    #region 构造

    /// <summary>
    /// 构造
    /// </summary>
    /// <param name="dal">当前服务使用的数据访问仓储。</param>
    /// <param name="agents">Agent 定义集合。</param>
    public AgKnowledgeBaseDefinitionServices(IBaseRepository<AgKnowledgeBaseDefinition> dal, Lazy<IAgentDefinitionCatalog>? agents = null)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
        this.agents = agents;
    }

    #endregion

    #region 知识库管理

    #region 创建（CreateAsync）
    /// <summary>
    /// 创建（CreateAsync）
    /// </summary>
    /// <param name="code">对象编码或业务错误码。</param>
    /// <param name="name">对象或字段名称。</param>
    /// <param name="description">对象说明文本。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含知识库定义，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<KnowledgeBaseDefinition>> CreateAsync(
        string code,
        string name,
        string description,
        CancellationToken cancellationToken = default)
    {
        code = (code ?? string.Empty).Trim().ToLowerInvariant();
        if (!Regex.IsMatch(code, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
        {
            return Failure(KnowledgeErrorCodes.CodeInvalid, "Knowledge base code must be lowercase kebab-case.");
        }
        var value = new KnowledgeBaseDefinition(
            Guid.NewGuid(), code, name?.Trim() ?? string.Empty,
            description?.Trim() ?? string.Empty, KnowledgeBaseStatus.Enabled,
            0, [], [], null);
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(IsolationLevel.Serializable);
        try
        {
            if (await AnyAsync(x =>
                    !x.IsDeleted &&
                    (x.ID == value.Id || x.Code == value.Code)))
            {
                await Db.Ado.RollbackTranAsync();
                return Failure(KnowledgeErrorCodes.CodeConflict, "A knowledge base already uses this code.");
            }

            await Db.Insertable(MapDefinitionEntity(value)).ExecuteCommandAsync();
            await InsertDocumentsAndChunksAsync(value, cancellationToken);
            await Db.Ado.CommitTranAsync();
            return Success(value);
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #region 更新（UpdateAsync）
    /// <summary>
    /// 更新（UpdateAsync）
    /// </summary>
    /// <param name="id">知识库标识。</param>
    /// <param name="expectedLogicalRevision">并发更新要求匹配的逻辑修订号。</param>
    /// <param name="name">对象或字段名称。</param>
    /// <param name="description">对象说明文本。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含知识库定义，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<KnowledgeBaseDefinition>> UpdateAsync(
        Guid id,
        long expectedLogicalRevision,
        string name,
        string description,
        KnowledgeBaseStatus status,
        CancellationToken cancellationToken = default)
    {
        KnowledgeBaseDefinition? existing = await GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return Failure(KnowledgeErrorCodes.NotFound, "The knowledge base was not found.");
        if (existing.LogicalRevision != expectedLogicalRevision)
            return Failure(KnowledgeErrorCodes.RowVersionConflict, "The knowledge base changed; reload and retry.");
        if (existing.Status is KnowledgeBaseStatus.Archived || status is KnowledgeBaseStatus.Archived)
        {
            return Failure(KnowledgeErrorCodes.LifecycleTransitionInvalid, "Use the archive operation to archive or restore a knowledge base.");
        }
        if (!Enum.IsDefined(status))
        {
            return Failure(KnowledgeErrorCodes.DocumentInvalid, "Knowledge base status is invalid.");
        }
        KnowledgeBaseDefinition updated = existing with
        {
            Name = name?.Trim() ?? string.Empty,
            Description = description?.Trim() ?? string.Empty,
            Status = status,
            LogicalRevision = existing.LogicalRevision + 1
        };
        return await TryReplaceAsync(updated, expectedLogicalRevision, cancellationToken)
            ? Success(updated)
            : Failure(KnowledgeErrorCodes.RowVersionConflict, "The knowledge base changed; reload and retry.");
    }
    #endregion

    #endregion

    #region 文档导入与解析

    #region 导入（ImportDocumentAsync）
    /// <summary>
    /// 导入（ImportDocumentAsync）
    /// </summary>
    /// <param name="knowledgeBaseId">知识库标识。</param>
    /// <param name="expectedLogicalRevision">并发更新要求匹配的逻辑修订号。</param>
    /// <param name="fileName">文件名称。</param>
    /// <param name="mediaType">内容的媒体类型。</param>
    /// <param name="content">需要规范化、导入或切分的文档文本。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含知识库定义，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<KnowledgeBaseDefinition>> ImportDocumentAsync(
        Guid knowledgeBaseId,
        long expectedLogicalRevision,
        string fileName,
        string mediaType,
        string content,
        CancellationToken cancellationToken = default)
    {
        mediaType = mediaType?.Trim().ToLowerInvariant() ?? string.Empty;
        content = NormalizeContent(content);
        if (mediaType is not ("text/plain" or "text/markdown"))
        {
            return Failure(
                KnowledgeErrorCodes.DocumentInvalid,
                $"Only non-empty text/plain and text/markdown documents up to {MaximumDocumentCharacters} characters are accepted by this endpoint.");
        }
        return await PersistDocumentAsync(
            knowledgeBaseId, expectedLogicalRevision, fileName,
            mediaType, content, cancellationToken);
    }
    #endregion

    #region 导入（ImportPdfDocumentAsync）
    /// <summary>
    /// 导入（ImportPdfDocumentAsync）
    /// </summary>
    /// <param name="knowledgeBaseId">知识库标识。</param>
    /// <param name="expectedLogicalRevision">并发更新要求匹配的逻辑修订号。</param>
    /// <param name="fileName">文件名称。</param>
    /// <param name="mediaType">内容的媒体类型。</param>
    /// <param name="content">待提取文本的原始 PDF 文件字节。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含知识库定义，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<KnowledgeBaseDefinition>> ImportPdfDocumentAsync(
        Guid knowledgeBaseId,
        long expectedLogicalRevision,
        string fileName,
        string mediaType,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default)
    {
        fileName = Path.GetFileName(fileName?.Trim() ?? string.Empty);
        mediaType = mediaType?.Trim().ToLowerInvariant() ?? string.Empty;
        ReadOnlyMemory<byte> bytes = content;
        if (string.IsNullOrWhiteSpace(fileName)
            || !string.Equals(Path.GetExtension(fileName), ".pdf", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(mediaType, "application/pdf", StringComparison.Ordinal)
            || bytes.Length is 0 or > MaximumPdfBytes || !HasPdfSignature(bytes.Span))
        {
            return Failure(
                KnowledgeErrorCodes.DocumentInvalid,
                $"Only PDF files up to {MaximumPdfBytes} bytes are accepted by this endpoint.");
        }
        KnowledgeBaseDefinition? target = await GetByIdAsync(knowledgeBaseId, cancellationToken);
        ServiceResult<KnowledgeBaseDefinition>? targetError =
            ValidateImportTarget(target, expectedLogicalRevision);
        if (targetError is not null) return targetError;
        KnowledgePdfExtractionResult extraction = await ExtractAsync(
            bytes, MaximumPdfPages, MaximumDocumentCharacters, cancellationToken);
        if (!extraction.Succeeded)
            return Failure(KnowledgeErrorCodes.DocumentInvalid, PdfFailureMessage(extraction.Failure));
        return await PersistDocumentAsync(
            knowledgeBaseId, expectedLogicalRevision, fileName,
            mediaType, NormalizeContent(extraction.Content), cancellationToken);
    }
    #endregion

    #region 处理（ExtractAsync）
    /// <summary>
    /// 处理（ExtractAsync）
    /// </summary>
    /// <param name="content">待提取文本的原始 PDF 文件字节。</param>
    /// <param name="maximumPages">允许处理的最大页数。</param>
    /// <param name="maximumCharacters">允许的最大字符数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>PDF 文本提取结果；无效输入、加密文件、页数或文本超限及无可提取文本均以失败原因返回，取消操作会抛出异常。</returns>
    public Task<KnowledgePdfExtractionResult> ExtractAsync(
        ReadOnlyMemory<byte> content,
        int maximumPages,
        int maximumCharacters,
        CancellationToken cancellationToken = default)
    {
        if (content.IsEmpty || maximumPages < 1 || maximumCharacters < 1)
        {
            return Task.FromResult(KnowledgePdfExtractionResult.Failed(
                KnowledgePdfExtractionFailure.Invalid));
        }

        return Task.Run(
            () => ExtractPdf(content, maximumPages, maximumCharacters, cancellationToken),
            cancellationToken);
    }
    #endregion

    #region 删除（DeleteDocumentAsync）
    /// <summary>
    /// 删除（DeleteDocumentAsync）
    /// </summary>
    /// <param name="knowledgeBaseId">知识库标识。</param>
    /// <param name="documentId">知识库文档标识。</param>
    /// <param name="expectedLogicalRevision">并发更新要求匹配的逻辑修订号。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含知识库定义，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<KnowledgeBaseDefinition>> DeleteDocumentAsync(
        Guid knowledgeBaseId,
        Guid documentId,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default)
    {
        KnowledgeBaseDefinition? existing = await GetByIdAsync(
            knowledgeBaseId,
            cancellationToken);
        if (existing is null)
            return Failure(
                KnowledgeErrorCodes.NotFound, "The knowledge base was not found.");
        if (existing.LogicalRevision != expectedLogicalRevision)
            return Failure(
                KnowledgeErrorCodes.RowVersionConflict,
                "The knowledge base changed; reload and retry.");
        if (existing.Status is KnowledgeBaseStatus.Archived)
        {
            return Failure(
                KnowledgeErrorCodes.LifecycleTransitionInvalid,
                "An archived knowledge base must be restored before documents can be deleted.");
        }
        if (existing.Documents.All(document => document.Id != documentId))
        {
            return Failure(
                KnowledgeErrorCodes.DocumentNotFound,
                "The knowledge document was not found.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            int updated = await Db.Updateable<AgKnowledgeBaseDefinition>()
                .SetColumns(value => new AgKnowledgeBaseDefinition
                {
                    LogicalRevision = expectedLogicalRevision + 1,
                    IndexedAtUtc = DateTime.UtcNow
                })
                .Where(value =>
                    value.ID == knowledgeBaseId &&
                    value.LogicalRevision == expectedLogicalRevision &&
                    !value.IsDeleted)
                .ExecuteCommandAsync();
            if (updated != 1)
            {
                await Db.Ado.RollbackTranAsync();
                return Failure(
                    KnowledgeErrorCodes.RowVersionConflict,
                    "The knowledge base changed; reload and retry.");
            }

            await Db.Updateable<AgKnowledgeChunk>()
                .SetColumns(value => new AgKnowledgeChunk { IsDeleted = true, IsActive = false })
                .Where(value =>
                    value.KnowledgeBaseId == knowledgeBaseId &&
                    value.DocumentId == documentId &&
                    !value.IsDeleted)
                .ExecuteCommandAsync();
            int deletedDocuments = await Db.Updateable<AgKnowledgeDocument>()
                .SetColumns(value => new AgKnowledgeDocument { IsDeleted = true, IsActive = false })
                .Where(value =>
                    value.ID == documentId &&
                    value.KnowledgeBaseId == knowledgeBaseId &&
                    !value.IsDeleted)
                .ExecuteCommandAsync();
            if (deletedDocuments != 1)
            {
                await Db.Ado.RollbackTranAsync();
                return Failure(
                    KnowledgeErrorCodes.DocumentNotFound,
                    "The knowledge document was not found.");
            }

            await Db.Ado.CommitTranAsync();
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }

        KnowledgeBaseDefinition? updatedDefinition = await GetByIdAsync(
            knowledgeBaseId,
            cancellationToken);
        return updatedDefinition is null
            ? Failure(
                KnowledgeErrorCodes.NotFound, "The knowledge base was not found.")
            : Success(updatedDefinition);
    }
    #endregion

    #region 处理（ExtractPdf）
    /// <summary>
    /// 处理（ExtractPdf）
    /// </summary>
    /// <param name="content">待提取文本的原始 PDF 文件字节。</param>
    /// <param name="maximumPages">允许处理的最大页数。</param>
    /// <param name="maximumCharacters">允许的最大字符数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>带页码标记的 PDF 文本和总页数，或对应的安全校验、读取失败原因；取消异常向上传播。</returns>
    private static KnowledgePdfExtractionResult ExtractPdf(
        ReadOnlyMemory<byte> content,
        int maximumPages,
        int maximumCharacters,
        CancellationToken cancellationToken)
    {
        try
        {
            using PdfDocument document = PdfDocument.Open(content);
            if (document.IsEncrypted)
            {
                return KnowledgePdfExtractionResult.Failed(
                    KnowledgePdfExtractionFailure.Encrypted);
            }

            if (document.NumberOfPages is < 1 || document.NumberOfPages > maximumPages)
            {
                return KnowledgePdfExtractionResult.Failed(
                    KnowledgePdfExtractionFailure.PageLimitExceeded);
            }

            var builder = new StringBuilder();
            for (int pageNumber = 1; pageNumber <= document.NumberOfPages; pageNumber++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string pageText = ContentOrderTextExtractor.GetText(
                    document.GetPage(pageNumber),
                    addDoubleNewline: true)
                    .Replace("\r\n", "\n", StringComparison.Ordinal)
                    .Replace('\r', '\n')
                    .Trim();
                if (pageText.Length == 0)
                {
                    continue;
                }

                string prefix = builder.Length == 0
                    ? $"[Page {pageNumber}]\n"
                    : $"\n\n[Page {pageNumber}]\n";
                if (builder.Length + prefix.Length + pageText.Length > maximumCharacters)
                {
                    return KnowledgePdfExtractionResult.Failed(
                        KnowledgePdfExtractionFailure.TextLimitExceeded);
                }

                builder.Append(prefix);
                builder.Append(pageText);
            }

            return builder.Length == 0
                ? KnowledgePdfExtractionResult.Failed(
                    KnowledgePdfExtractionFailure.NoExtractableText)
                : KnowledgePdfExtractionResult.Success(
                    builder.ToString(),
                    document.NumberOfPages);
        }
        catch (PdfDocumentEncryptedException)
        {
            return KnowledgePdfExtractionResult.Failed(
                KnowledgePdfExtractionFailure.Encrypted);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return KnowledgePdfExtractionResult.Failed(
                KnowledgePdfExtractionFailure.Invalid);
        }
    }
    #endregion

    #region 处理（PersistDocumentAsync）
    /// <summary>
    /// 处理（PersistDocumentAsync）
    /// </summary>
    /// <param name="knowledgeBaseId">知识库标识。</param>
    /// <param name="expectedLogicalRevision">并发更新要求匹配的逻辑修订号。</param>
    /// <param name="requestedFileName">请求指定的文件名称。</param>
    /// <param name="mediaType">内容的媒体类型。</param>
    /// <param name="content">需要规范化、导入或切分的文档文本。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含知识库定义，失败时包含错误状态和提示。</returns>
    private async Task<ServiceResult<KnowledgeBaseDefinition>> PersistDocumentAsync(
        Guid knowledgeBaseId, long expectedLogicalRevision, string requestedFileName,
        string mediaType, string content, CancellationToken cancellationToken)
    {
        KnowledgeBaseDefinition? existing = await GetByIdAsync(knowledgeBaseId, cancellationToken);
        ServiceResult<KnowledgeBaseDefinition>? targetError =
            ValidateImportTarget(existing, expectedLogicalRevision);
        if (targetError is not null) return targetError;
        KnowledgeBaseDefinition current = existing!;
        string fileName = Path.GetFileName(requestedFileName?.Trim() ?? string.Empty);
        if (string.IsNullOrWhiteSpace(fileName) || content.Length is 0 or > MaximumDocumentCharacters
            || content.Contains('\0'))
        {
            return Failure(
                KnowledgeErrorCodes.DocumentInvalid,
                $"The extracted document must contain between 1 and {MaximumDocumentCharacters} safe text characters.");
        }
        Guid documentId = Guid.NewGuid();
        var document = new KnowledgeDocument(
            documentId, fileName, mediaType,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant(),
            content, DateTimeOffset.UtcNow);
        IReadOnlyList<KnowledgeChunk> chunks = KnowledgeTextChunker.Chunk(documentId, content);
        KnowledgeBaseDefinition updated = current with
        {
            LogicalRevision = current.LogicalRevision + 1,
            Documents = Common.Extensions.CollectionExtensions.ToReadOnlyList(current.Documents.Append(document)),
            Chunks = Common.Extensions.CollectionExtensions.ToReadOnlyList(current.Chunks.Concat(chunks)),
            IndexedAtUtc = DateTimeOffset.UtcNow
        };
        return await TryReplaceAsync(updated, expectedLogicalRevision, cancellationToken)
            ? Success(updated)
            : Failure(
                KnowledgeErrorCodes.RowVersionConflict,
                "The knowledge base changed; reload and retry.");
    }
    #endregion

    #endregion

    #region 知识库状态与查询

    #region 设置（SetArchivedAsync）
    /// <summary>
    /// 设置（SetArchivedAsync）
    /// </summary>
    /// <param name="id">知识库标识。</param>
    /// <param name="expectedLogicalRevision">并发更新要求匹配的逻辑修订号。</param>
    /// <param name="archived">是否设置为归档状态。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含知识库定义，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<KnowledgeBaseDefinition>> SetArchivedAsync(
        Guid id,
        long expectedLogicalRevision,
        bool archived,
        CancellationToken cancellationToken = default)
    {
        KnowledgeBaseDefinition? existing = await GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return Failure(
                KnowledgeErrorCodes.NotFound, "The knowledge base was not found.");
        if (existing.LogicalRevision != expectedLogicalRevision)
            return Failure(
                KnowledgeErrorCodes.RowVersionConflict,
                "The knowledge base changed; reload and retry.");
        if (archived && existing.Status is not KnowledgeBaseStatus.Disabled)
        {
            return Failure(
                KnowledgeErrorCodes.LifecycleTransitionInvalid,
                "A knowledge base must be disabled before it can be archived.");
        }
        if (!archived && existing.Status is not KnowledgeBaseStatus.Archived)
        {
            return Failure(
                KnowledgeErrorCodes.LifecycleTransitionInvalid,
                "Only an archived knowledge base can be restored.");
        }
        if (archived && agents is not null)
        {
            IReadOnlyList<AgentDefinition> enabledAgents = await agents.Value.ListDefinitionsAsync(
                new AgentDefinitionQuery(RuntimeStatus: AgentRuntimeStatus.Enabled), cancellationToken);
            string[] blockers = enabledAgents
                .Where(value => value.PublishedVersions.LastOrDefault()?.Snapshot?.KnowledgeBases
                    .Any(binding => binding.KnowledgeBaseId == existing.Id) == true)
                .Select(value => value.Code).Take(8).ToArray();
            if (blockers.Length > 0)
            {
                return Failure(
                    KnowledgeErrorCodes.ArchiveBlocked,
                    $"The knowledge base is still referenced by Agent(s): {string.Join(", ", blockers)}.");
            }
        }
        var updated = existing with
        {
            Status = archived ? KnowledgeBaseStatus.Archived : KnowledgeBaseStatus.Disabled,
            LogicalRevision = existing.LogicalRevision + 1
        };
        return await TryReplaceAsync(updated, existing.LogicalRevision, cancellationToken)
            ? Success(updated)
            : Failure(
                KnowledgeErrorCodes.RowVersionConflict,
                "The knowledge base changed; reload and retry.");
    }
    #endregion

    #region 获取（GetByIdAsync）
    /// <summary>
    /// 获取（GetByIdAsync）
    /// </summary>
    /// <param name="id">知识库标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定标识的完整知识库定义；不存在时为 null。</returns>
    public async Task<KnowledgeBaseDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AgKnowledgeBaseDefinition? definition = await Db.Queryable<AgKnowledgeBaseDefinition>()
            .Where(value => value.ID == id && !value.IsDeleted)
            .FirstAsync();
        return definition is null
            ? null
            : await LoadDefinitionAsync(definition, cancellationToken);
    }
    #endregion

    #region 获取（GetByCodeAsync）
    /// <summary>
    /// 获取（GetByCodeAsync）
    /// </summary>
    /// <param name="code">对象编码或业务错误码。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定编码的完整知识库定义；不存在时为 null。</returns>
    public async Task<KnowledgeBaseDefinition?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AgKnowledgeBaseDefinition? definition = await Db.Queryable<AgKnowledgeBaseDefinition>()
            .Where(value => value.Code == code && !value.IsDeleted)
            .FirstAsync();
        return definition is null
            ? null
            : await LoadDefinitionAsync(definition, cancellationToken);
    }
    #endregion

    #region 查询列表（ListAsync）
    /// <summary>
    /// 查询列表（ListAsync）
    /// </summary>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>按编码及标识排列的知识库定义；未指定状态时排除已归档知识库。</returns>
    public async Task<IReadOnlyList<KnowledgeBaseDefinition>> ListAsync(KnowledgeBaseStatus? status = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var expression = Db.Queryable<AgKnowledgeBaseDefinition>()
            .Where(value => !value.IsDeleted);
        if (status.HasValue)
        {
            string statusName = status.Value.ToString();
            expression = expression.Where(value => value.Status == statusName);
        }
        else
        {
            expression = expression.Where(value => value.Status != nameof(KnowledgeBaseStatus.Archived));
        }

        List<AgKnowledgeBaseDefinition> definitions = await expression
            .OrderBy(value => value.Code)
            .OrderBy(value => value.ID)
            .ToListAsync();
        return await LoadDefinitionsAsync(definitions, cancellationToken);
    }
    #endregion

    #region 尝试执行（TryReplaceAsync）
    /// <summary>
    /// 尝试执行（TryReplaceAsync）
    /// </summary>
    /// <param name="value">本次操作使用的知识库定义。</param>
    /// <param name="expectedLogicalRevision">并发更新要求匹配的逻辑修订号。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>异步任务，其结果为：操作是否成功；未满足执行条件或更新未生效时返回 false。</returns>
    private async Task<bool> TryReplaceAsync(KnowledgeBaseDefinition value, long expectedLogicalRevision, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (expectedLogicalRevision == long.MaxValue ||
            value.LogicalRevision != expectedLogicalRevision + 1)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            HashSet<Guid> existingDocumentIds = (await Db.Queryable<AgKnowledgeDocument>()
                    .Where(candidate =>
                        candidate.KnowledgeBaseId == value.Id &&
                        !candidate.IsDeleted)
                    .Select(candidate => candidate.ID)
                    .ToListAsync())
                .ToHashSet();
            HashSet<Guid> existingChunkIds = (await Db.Queryable<AgKnowledgeChunk>()
                    .Where(candidate =>
                        candidate.KnowledgeBaseId == value.Id &&
                        !candidate.IsDeleted)
                    .Select(candidate => candidate.ID)
                    .ToListAsync())
                .ToHashSet();
            HashSet<Guid> requestedDocumentIds = value.Documents
                .Select(document => document.Id)
                .ToHashSet();
            HashSet<Guid> requestedChunkIds = value.Chunks
                .Select(chunk => chunk.Id)
                .ToHashSet();
            if (!existingDocumentIds.IsSubsetOf(requestedDocumentIds) ||
                !existingChunkIds.IsSubsetOf(requestedChunkIds))
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            var entity = MapDefinitionEntity(value);
            int updated = await Db.Updateable(entity)
                .UpdateColumns(candidate => new
                {
                    candidate.Name,
                    candidate.Description,
                    candidate.Status,
                    candidate.LogicalRevision,
                    candidate.IndexedAtUtc
                })
                .Where(candidate =>
                    candidate.ID == value.Id &&
                    candidate.Code == value.Code &&
                    candidate.LogicalRevision == expectedLogicalRevision &&
                    !candidate.IsDeleted)
                .ExecuteCommandAsync();
            if (updated != 1)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            await InsertDocumentsAndChunksAsync(
                value,
                cancellationToken,
                existingDocumentIds,
                existingChunkIds);
            await Db.Ado.CommitTranAsync();
            return true;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #endregion

    #region 已发布引用与检索

    #region 查询列表（ListPublishedAsync）
    /// <summary>
    /// 查询列表（ListPublishedAsync）
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>已启用且存在未删除分块的知识库引用，包含当前逻辑版本。</returns>
    public async Task<IReadOnlyList<PublishedKnowledgeReference>> ListPublishedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var definitions = await Db.Queryable<AgKnowledgeBaseDefinition>()
            .Where(definition =>
                !definition.IsDeleted &&
                definition.Status == nameof(KnowledgeBaseStatus.Enabled))
            .OrderBy(definition => definition.Code)
            .OrderBy(definition => definition.ID)
            .ToListAsync();
        if (definitions.Count == 0)
        {
            return [];
        }

        var definitionIds = definitions.Select(value => value.ID).ToArray();
        var populatedIds = await Db.Queryable<AgKnowledgeChunk>()
             .Where(chunk =>
                 chunk.KnowledgeBaseId.HasValue &&
                 definitionIds.Contains(chunk.KnowledgeBaseId.Value) &&
                 !chunk.IsDeleted)
             .Select(chunk => chunk.KnowledgeBaseId!.Value)
             .Distinct()
             .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();
        HashSet<Guid> populated = populatedIds.ToHashSet();
        return Common.Extensions.CollectionExtensions.ToReadOnlyList(definitions
            .Where(definition => populated.Contains(definition.ID))
            .Select(definition =>
            new PublishedKnowledgeReference(
                definition.ID,
                Required(definition.Code, "Code"),
                Required(definition.Name, "Name"),
                Required(definition.LogicalRevision, "LogicalRevision"))));
    }
    #endregion

    #region 处理（SearchAsync）
    /// <summary>
    /// 处理（SearchAsync）
    /// </summary>
    /// <param name="knowledgeBaseIds">知识库标识集合。</param>
    /// <param name="query">查询筛选条件。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定已启用知识库中得分大于零的词法检索结果，按相关度优先排序，最多 20 条。</returns>
    public async Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        IReadOnlyList<Guid> knowledgeBaseIds,
        string query,
        int take,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(knowledgeBaseIds);
        cancellationToken.ThrowIfCancellationRequested();
        if (knowledgeBaseIds.Count == 0)
        {
            return [];
        }

        Guid[] ids = knowledgeBaseIds.Distinct().ToArray();
        var rows = await Db
            .Queryable<AgKnowledgeBaseDefinition, AgKnowledgeDocument, AgKnowledgeChunk>(
                (definition, document, chunk) => new JoinQueryInfos(
                    JoinType.Inner, definition.ID == document.KnowledgeBaseId,
                    JoinType.Inner,
                    document.ID == chunk.DocumentId &&
                    definition.ID == chunk.KnowledgeBaseId))
            .Where((definition, document, chunk) =>
                ids.Contains(definition.ID) &&
                definition.Status == nameof(KnowledgeBaseStatus.Enabled) &&
                !definition.IsDeleted &&
                !document.IsDeleted &&
                !chunk.IsDeleted)
            .Select((definition, document, chunk) => new KnowledgeSearchRow
            {
                KnowledgeBaseId = definition.ID,
                KnowledgeBaseCode = definition.Code,
                DocumentId = document.ID,
                FileName = document.FileName,
                ChunkId = chunk.ID,
                ChunkSequence = chunk.Sequence,
                Content = chunk.Content
            })
            .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();

        HashSet<string> terms = KnowledgeLexicalSearch.Terms(query);
        return Common.Extensions.CollectionExtensions.ToReadOnlyList(rows
            .Select(row => new
            {
                Row = row,
                Score = KnowledgeLexicalSearch.Score(Required(row.Content, "Chunk.Content"), terms)
            })
            .Where(value => value.Score > 0)
            .Select(value => new KnowledgeSearchResult(
                value.Row.KnowledgeBaseId,
                Required(value.Row.KnowledgeBaseCode, "KnowledgeBase.Code"),
                value.Row.DocumentId,
                Required(value.Row.FileName, "Document.FileName"),
                value.Row.ChunkId,
                Required(value.Row.ChunkSequence, "Chunk.Sequence"),
                Required(value.Row.Content, "Chunk.Content"),
                value.Score))
            .OrderByDescending(value => value.Score)
            .ThenBy(value => value.KnowledgeBaseCode, StringComparer.Ordinal)
            .ThenBy(value => value.FileName, StringComparer.Ordinal)
            .ThenBy(value => value.ChunkSequence)
            .Take(Math.Clamp(take, 1, 20)));
    }
    #endregion

    #endregion

    #region 持久化加载与映射

    #region 加载（LoadDefinitionAsync）
    /// <summary>
    /// 加载（LoadDefinitionAsync）
    /// </summary>
    /// <param name="definition">定义记录。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>补齐文档、分块及索引时间的知识库定义。</returns>
    private async Task<KnowledgeBaseDefinition> LoadDefinitionAsync(AgKnowledgeBaseDefinition definition, CancellationToken cancellationToken)
    {
        IReadOnlyList<KnowledgeBaseDefinition> values = await LoadDefinitionsAsync(
            [definition],
            cancellationToken);
        return values[0];
    }
    #endregion

    #region 加载（LoadDefinitionsAsync）
    /// <summary>
    /// 加载（LoadDefinitionsAsync）
    /// </summary>
    /// <param name="definitions">定义记录集合。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>保持输入顺序并补齐文档及分块的知识库定义集合。</returns>
    private async Task<IReadOnlyList<KnowledgeBaseDefinition>> LoadDefinitionsAsync(
        IReadOnlyList<AgKnowledgeBaseDefinition> definitions,
        CancellationToken cancellationToken)
    {
        if (definitions.Count == 0)
        {
            return [];
        }

        var ids = definitions.Select(value => value.ID).ToArray();
        var documents = await Db.Queryable<AgKnowledgeDocument>()
            .Where(value =>
                value.KnowledgeBaseId.HasValue &&
                ids.Contains(value.KnowledgeBaseId.Value) &&
                !value.IsDeleted)
            .OrderBy(value => value.KnowledgeBaseId)
            .OrderBy(value => value.Ordinal)
            .OrderBy(value => value.ID)
            .ToListAsync();
        var chunks = await Db.Queryable<AgKnowledgeChunk>()
            .Where(value =>
                value.KnowledgeBaseId.HasValue &&
                ids.Contains(value.KnowledgeBaseId.Value) &&
                !value.IsDeleted)
            .OrderBy(value => value.KnowledgeBaseId)
            .OrderBy(value => value.DocumentId)
            .OrderBy(value => value.Sequence)
            .OrderBy(value => value.ID)
            .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyDictionary<Guid, AgKnowledgeDocument[]> documentsByDefinition = documents
            .GroupBy(value => Required(value.KnowledgeBaseId, "Document.KnowledgeBaseId"))
            .ToDictionary(group => group.Key, group => group.ToArray());
        IReadOnlyDictionary<Guid, AgKnowledgeChunk[]> chunksByDefinition = chunks
            .GroupBy(value => Required(value.KnowledgeBaseId, "Chunk.KnowledgeBaseId"))
            .ToDictionary(group => group.Key, group => group.ToArray());
        return Common.Extensions.CollectionExtensions.ToReadOnlyList(definitions.Select(definition => MapDefinition(
            definition,
            documentsByDefinition.GetValueOrDefault(definition.ID) ?? [],
            chunksByDefinition.GetValueOrDefault(definition.ID) ?? [])));
    }
    #endregion

    #region 新增（InsertDocumentsAndChunksAsync）
    /// <summary>
    /// 新增（InsertDocumentsAndChunksAsync）
    /// </summary>
    /// <param name="definition">定义记录。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <param name="existingDocumentIds">已存在的文档标识集合。</param>
    /// <param name="existingChunkIds">已存在的知识分块标识集合。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task InsertDocumentsAndChunksAsync(
        KnowledgeBaseDefinition definition,
        CancellationToken cancellationToken,
        IReadOnlySet<Guid>? existingDocumentIds = null,
        IReadOnlySet<Guid>? existingChunkIds = null)
    {
        int nextOrdinal = 0;
        if (existingDocumentIds is not null)
        {
            List<AgKnowledgeDocument> historicalDocuments =
                await Db.Queryable<AgKnowledgeDocument>()
                    .Filter(null, true)
                    .Where(document => document.KnowledgeBaseId == definition.Id)
                    .ToListAsync();
            if (historicalDocuments.Count > 0)
            {
                nextOrdinal = historicalDocuments.Max(document =>
                    Required(document.Ordinal, "Document.Ordinal")) + 1;
            }
        }

        List<AgKnowledgeDocument> documents = definition.Documents
            .Where(document => existingDocumentIds is null || !existingDocumentIds.Contains(document.Id))
            .Select((document, index) => MapDocumentEntity(
                definition.Id,
                document,
                nextOrdinal + index))
            .ToList();
        if (documents.Count > 0)
        {
            await Db.Insertable(documents).ExecuteCommandAsync();
        }

        List<AgKnowledgeChunk> chunks = definition.Chunks
            .Select(chunk => MapChunkEntity(definition.Id, chunk))
            .Where(chunk => existingChunkIds is null || !existingChunkIds.Contains(chunk.ID))
            .ToList();
        if (chunks.Count > 0)
        {
            await Db.Insertable(chunks).ExecuteCommandAsync();
        }
        cancellationToken.ThrowIfCancellationRequested();
    }
    #endregion

    #region 映射（MapDefinition）
    /// <summary>
    /// 映射（MapDefinition）
    /// </summary>
    /// <param name="definition">定义记录。</param>
    /// <param name="documents">知识库文档集合。</param>
    /// <param name="chunks">知识分块集合。</param>
    /// <returns>文档按导入序号、分块按文档顺序及分块序号排列的知识库定义。</returns>
    private static KnowledgeBaseDefinition MapDefinition(
        AgKnowledgeBaseDefinition definition,
        IReadOnlyList<AgKnowledgeDocument> documents,
        IReadOnlyList<AgKnowledgeChunk> chunks)
    {
        IReadOnlyDictionary<Guid, int> documentOrdinals = documents.ToDictionary(
            value => value.ID,
            value => Required(value.Ordinal, "Document.Ordinal"));
        return new(
            definition.ID,
            Required(definition.Code, "Code"),
            Required(definition.Name, "Name"),
            Required(definition.Description, "Description"),
            ParseStatus(definition.Status),
            Required(definition.LogicalRevision, "LogicalRevision"),
         Common.Extensions.CollectionExtensions.ToReadOnlyList(documents
                .OrderBy(value => Required(value.Ordinal, "Document.Ordinal"))
                .ThenBy(value => value.ID)
                .Select(MapDocument)),
        Common.Extensions.CollectionExtensions.ToReadOnlyList(chunks
                .OrderBy(value => documentOrdinals.GetValueOrDefault(
                    Required(value.DocumentId, "Chunk.DocumentId"),
                    int.MaxValue))
                .ThenBy(value => Required(value.Sequence, "Chunk.Sequence"))
                .ThenBy(value => value.ID)
                .Select(MapChunk)),
            ToDateTimeOffset(definition.IndexedAtUtc));
    }
    #endregion

    #region 映射（MapDefinitionEntity）
    /// <summary>
    /// 映射（MapDefinitionEntity）
    /// </summary>
    /// <param name="value">本次操作使用的知识库定义。</param>
    /// <returns>由知识库定义构造的主表持久化实体。</returns>
    private static AgKnowledgeBaseDefinition MapDefinitionEntity(KnowledgeBaseDefinition value) =>
        new()
        {
            ID = value.Id,
            Code = value.Code,
            Name = value.Name,
            Description = value.Description,
            Status = value.Status.ToString(),
            LogicalRevision = value.LogicalRevision,
            IndexedAtUtc = value.IndexedAtUtc?.UtcDateTime,
            IsDeleted = false,
            IsActive = true
        };
    #endregion

    #region 映射（MapDocument）
    /// <summary>
    /// 映射（MapDocument）
    /// </summary>
    /// <param name="value">本次操作使用的知识文档实体。</param>
    /// <returns>包含文件名、媒体类型、摘要及导入时间的知识文档。</returns>
    private static KnowledgeDocument MapDocument(AgKnowledgeDocument value) =>
        new(
            value.ID,
            Required(value.FileName, "Document.FileName"),
            Required(value.MediaType, "Document.MediaType"),
            Required(value.Sha256, "Document.Sha256"),
            Required(value.Content, "Document.Content"),
            RequiredDateTimeOffset(value.ImportedAtUtc, "Document.ImportedAtUtc"));
    #endregion

    #region 映射（MapDocumentEntity）
    /// <summary>
    /// 映射（MapDocumentEntity）
    /// </summary>
    /// <param name="knowledgeBaseId">知识库标识。</param>
    /// <param name="value">本次操作使用的知识文档。</param>
    /// <param name="ordinal">文档在知识库中的排序序号。</param>
    /// <returns>带有所属知识库及文档排序序号的文档持久化实体。</returns>
    private static AgKnowledgeDocument MapDocumentEntity(Guid knowledgeBaseId, KnowledgeDocument value, int ordinal) =>
        new()
        {
            ID = value.Id,
            KnowledgeBaseId = knowledgeBaseId,
            Ordinal = ordinal,
            FileName = value.FileName,
            MediaType = value.MediaType,
            Sha256 = value.Sha256,
            Content = value.Content,
            ImportedAtUtc = value.ImportedAtUtc.UtcDateTime,
            IsDeleted = false,
            IsActive = true
        };
    #endregion

    #region 映射（MapChunk）
    /// <summary>
    /// 映射（MapChunk）
    /// </summary>
    /// <param name="value">本次操作使用的知识分块实体。</param>
    /// <returns>包含文档标识、分块序号及文本的知识分块。</returns>
    private static KnowledgeChunk MapChunk(AgKnowledgeChunk value) =>
        new(
            value.ID,
            Required(value.DocumentId, "Chunk.DocumentId"),
            Required(value.Sequence, "Chunk.Sequence"),
            Required(value.Content, "Chunk.Content"));
    #endregion

    #region 映射（MapChunkEntity）
    /// <summary>
    /// 映射（MapChunkEntity）
    /// </summary>
    /// <param name="knowledgeBaseId">知识库标识。</param>
    /// <param name="value">本次操作使用的知识分块。</param>
    /// <returns>带有所属知识库和文档信息的分块持久化实体。</returns>
    private static AgKnowledgeChunk MapChunkEntity(Guid knowledgeBaseId, KnowledgeChunk value) =>
        new()
        {
            ID = value.Id,
            KnowledgeBaseId = knowledgeBaseId,
            DocumentId = value.DocumentId,
            Sequence = value.Sequence,
            Content = value.Content,
            IsDeleted = false,
            IsActive = true
        };
    #endregion

    #region 解析（ParseStatus）
    /// <summary>
    /// 解析并校验持久化枚举值（ParseStatus）。
    /// </summary>
    /// <param name="value">数据库中存储的枚举文本。</param>
    /// <returns>按区分大小写方式解析且已定义的枚举值；无效输入抛出异常。</returns>
    private static KnowledgeBaseStatus ParseStatus(string? value) =>
        Enum.TryParse(value, ignoreCase: false, out KnowledgeBaseStatus status) && Enum.IsDefined(status)
            ? status
            : throw new InvalidDataException($"Knowledge base status '{value}' is invalid.");
    #endregion

    #region 规范化（NormalizeContent）
    /// <summary>
    /// 规范化（NormalizeContent）
    /// </summary>
    /// <param name="content">需要规范化、导入或切分的文档文本。</param>
    /// <returns>统一为换行符 LF 并去除首尾空白的文本；输入为 null 时返回空字符串。</returns>
    private static string NormalizeContent(string? content) =>
        (content ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n').Trim();
    #endregion

    #region 检查 PDF 文件头标记（HasPdfSignature）
    /// <summary>
    /// 检查 PDF 文件头标记（HasPdfSignature）。
    /// </summary>
    /// <param name="content">待检查的文件原始字节。</param>
    /// <returns>内容至少有 5 字节且以 %PDF- 开头时返回 true，否则返回 false；不表示整个文件已通过 PDF 格式校验。</returns>
    private static bool HasPdfSignature(ReadOnlySpan<byte> content) =>
        content.Length >= 5 && content[0] == (byte)'%' && content[1] == (byte)'P'
        && content[2] == (byte)'D' && content[3] == (byte)'F' && content[4] == (byte)'-';
    #endregion

    #region 处理（PdfFailureMessage）
    /// <summary>
    /// 处理（PdfFailureMessage）
    /// </summary>
    /// <param name="failure">失败结果。</param>
    /// <returns>与 PDF 提取失败原因对应的安全提示，不包含原始解析异常详情。</returns>
    private static string PdfFailureMessage(KnowledgePdfExtractionFailure failure) => failure switch
    {
        KnowledgePdfExtractionFailure.Encrypted =>
            "Encrypted or password-protected PDF files are not accepted.",
        KnowledgePdfExtractionFailure.PageLimitExceeded =>
            $"The PDF exceeds the {MaximumPdfPages}-page safety limit.",
        KnowledgePdfExtractionFailure.TextLimitExceeded =>
            $"The PDF contains more than {MaximumDocumentCharacters} extracted text characters.",
        KnowledgePdfExtractionFailure.NoExtractableText =>
            "The PDF contains no extractable text; scanned-image OCR is not enabled.",
        _ => "The PDF is malformed or could not be read safely."
    };
    #endregion

    #region 校验（ValidateImportTarget）
    /// <summary>
    /// 校验（ValidateImportTarget）
    /// </summary>
    /// <param name="existing">已有数据。</param>
    /// <param name="expectedLogicalRevision">并发更新要求匹配的逻辑修订号。</param>
    /// <returns>知识库不存在、版本冲突、已归档或文档数已达上限时的失败服务结果；允许导入时为 null。</returns>
    private static ServiceResult<KnowledgeBaseDefinition>? ValidateImportTarget(KnowledgeBaseDefinition? existing, long expectedLogicalRevision)
    {
        if (existing is null)
            return Failure(
                KnowledgeErrorCodes.NotFound, "The knowledge base was not found.");
        if (existing.LogicalRevision != expectedLogicalRevision)
            return Failure(
                KnowledgeErrorCodes.RowVersionConflict,
                "The knowledge base changed; reload and retry.");
        if (existing.Status is KnowledgeBaseStatus.Archived)
        {
            return Failure(
                KnowledgeErrorCodes.LifecycleTransitionInvalid,
                "An archived knowledge base must be restored before documents can be imported.");
        }
        return existing.Documents.Count >= MaximumDocuments
            ? Failure(
                KnowledgeErrorCodes.DocumentInvalid,
                $"A knowledge base accepts at most {MaximumDocuments} documents.")
            : null;
    }
    #endregion

    #region 处理（Failure）
    /// <summary>
    /// 处理（Failure）
    /// </summary>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <param name="message">消息或提示文本。</param>
    /// <returns>包含对应业务错误状态和提示信息的失败服务结果。</returns>
    private static ServiceResult<KnowledgeBaseDefinition> Failure(string errorCode, string message) =>
        Failure<KnowledgeBaseDefinition>(KnowledgeServiceStatusCodes.FromErrorCode(errorCode), message);
    #endregion

    #endregion

    #region 文本分块与搜索算法

    private static class KnowledgeTextChunker
    {
        private const int TargetCharacters = 1200;
        private const int OverlapCharacters = 160;

        #region 处理（Chunk）
        /// <summary>
        /// 处理（Chunk）
        /// </summary>
        /// <param name="documentId">知识库文档标识。</param>
        /// <param name="content">需要规范化、导入或切分的文档文本。</param>
        /// <returns>按文本顺序生成的非空知识分块，使用目标长度、换行边界及重叠区域切分。</returns>
        public static IReadOnlyList<KnowledgeChunk> Chunk(Guid documentId, string content)
        {
            var values = new List<KnowledgeChunk>();
            int start = 0;
            int sequence = 0;
            while (start < content.Length)
            {
                int end = Math.Min(start + TargetCharacters, content.Length);
                if (end < content.Length)
                {
                    int boundary = content.LastIndexOf('\n', end - 1, end - start);
                    if (boundary > start + TargetCharacters / 2)
                    {
                        end = boundary;
                    }
                }

                string value = content[start..end].Trim();
                if (value.Length > 0)
                {
                    values.Add(new KnowledgeChunk(Guid.NewGuid(), documentId, sequence++, value));
                }

                if (end >= content.Length)
                {
                    break;
                }

                start = Math.Max(start + 1, end - OverlapCharacters);
            }

            return Common.Extensions.CollectionExtensions.ToReadOnlyList(values);
        }
        #endregion
    }

    private static class KnowledgeLexicalSearch
    {
        #region 处理（Terms）
        /// <summary>
        /// 处理（Terms）
        /// </summary>
        /// <param name="value">用于提取词项的查询或分块文本。</param>
        /// <returns>经 Unicode 兼容规范化及小写转换后的去重词项，包含多字符单词、连续中文双字词及孤立单字。</returns>
        public static HashSet<string> Terms(string value)
        {
            string normalized = value.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
            var terms = new HashSet<string>(StringComparer.Ordinal);
            var word = new StringBuilder();
            var cjk = new StringBuilder();

            #region 处理（FlushWord）
            void FlushWord()
            {
                if (word.Length > 1) terms.Add(word.ToString());
                word.Clear();
            }
            #endregion

            #region 处理（FlushCjk）
            void FlushCjk()
            {
                if (cjk.Length == 1) terms.Add(cjk.ToString());
                for (int i = 0; i + 1 < cjk.Length; i++) terms.Add(cjk.ToString(i, 2));
                cjk.Clear();
            }
            #endregion

            foreach (char character in normalized)
            {
                UnicodeCategory category = char.GetUnicodeCategory(character);
                bool isCjk = character is >= '\u3400' and <= '\u9fff';
                if (isCjk)
                {
                    FlushWord();
                    cjk.Append(character);
                }
                else if (char.IsLetterOrDigit(character) || category == UnicodeCategory.ConnectorPunctuation)
                {
                    FlushCjk();
                    word.Append(character);
                }
                else
                {
                    FlushWord();
                    FlushCjk();
                }
            }

            FlushWord();
            FlushCjk();
            return terms;
        }
        #endregion

        #region 处理（Score）
        /// <summary>
        /// 处理（Score）
        /// </summary>
        /// <param name="content">用于与查询词项计算相关度的分块文本。</param>
        /// <param name="queryTerms">用于检索匹配的查询词集合。</param>
        /// <returns>查询与内容词项交集数除以两者词项数乘积平方根的得分；查询为空或无匹配时为零。</returns>
        public static double Score(string content, HashSet<string> queryTerms)
        {
            if (queryTerms.Count == 0) return 0;
            HashSet<string> contentTerms = Terms(content);
            int matches = queryTerms.Count(contentTerms.Contains);
            return matches == 0
                ? 0
                : matches / Math.Sqrt(queryTerms.Count * (double)Math.Max(1, contentTerms.Count));
        }
        #endregion
    }

    #endregion

    #region 值转换与校验

    #region 转换（ToDateTimeOffset）
    /// <summary>
    /// 将数据库时间还原为 UTC 时间（ToDateTimeOffset）。
    /// </summary>
    /// <param name="value">按 UTC 语义存储的数据库时间。</param>
    /// <returns>将输入时间视为 UTC 后构造的零偏移时间。输入为 null 时返回 null。</returns>
    private static DateTimeOffset? ToDateTimeOffset(DateTime? value) =>
        value.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
            : null;
    #endregion

    #region 处理（RequiredDateTimeOffset）
    /// <summary>
    /// 处理（RequiredDateTimeOffset）
    /// </summary>
    /// <param name="value">数据库中按 UTC 存储的必填时间。</param>
    /// <param name="field">字段名称，用于校验和错误提示。</param>
    /// <returns>将必填时间视为 UTC 后构造的零偏移时间；字段缺失时抛出 InvalidDataException。</returns>
    private static DateTimeOffset RequiredDateTimeOffset(DateTime? value, string field) =>
        value.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
            : throw new InvalidDataException($"Knowledge base field '{field}' is missing.");
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <typeparam name="T">必填字段的值类型。</typeparam>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="field">字段名称，用于校验和错误提示。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static T Required<T>(T? value, string field) where T : struct =>
        value ?? throw new InvalidDataException($"Knowledge base field '{field}' is missing.");
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="field">字段名称，用于校验和错误提示。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static string Required(string? value, string field) =>
        value ?? throw new InvalidDataException($"Knowledge base field '{field}' is missing.");
    #endregion

    /// <summary>
    /// 知识库检索联表查询的内部投影行。
    /// </summary>
    private sealed class KnowledgeSearchRow
    {
        /// <summary>
        /// 知识库定义标识。
        /// </summary>
        public Guid KnowledgeBaseId { get; set; }

        /// <summary>
        /// 知识库业务编码。
        /// </summary>
        public string? KnowledgeBaseCode { get; set; }

        /// <summary>
        /// 来源文档标识。
        /// </summary>
        public Guid DocumentId { get; set; }

        /// <summary>
        /// 来源文档文件名。
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// 文档分块标识。
        /// </summary>
        public Guid ChunkId { get; set; }

        /// <summary>
        /// 分块在来源文档中的顺序号。
        /// </summary>
        public int? ChunkSequence { get; set; }

        /// <summary>
        /// 用于词法匹配和结果返回的分块正文。
        /// </summary>
        public string? Content { get; set; }
    }

    #endregion
}
