using EU.Core.IServices;
using EU.Core.Api.Agent.Security;
using EU.Core.Model;
using EU.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace EU.Core.Api.Agent.Controllers;

#region 文件职责：KnowledgeBasesController 接口处理

/// <summary>
/// 提供知识库和文档管理的 HTTP 接口。
/// </summary>
[Route("api/knowledge-bases")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class KnowledgeBasesController(
    IAgKnowledgeBaseDefinitionServices knowledgeBaseDefinitionServices) : Base.ControllerBase
{
    [HttpGet]
    public async Task<ServiceResult<IReadOnlyList<KnowledgeBaseListItem>>> List([FromQuery] string? status, CancellationToken cancellationToken)
    {
        KnowledgeBaseStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (string.Equals(status, nameof(KnowledgeBaseStatus.Enabled), StringComparison.Ordinal))
                parsedStatus = KnowledgeBaseStatus.Enabled;
            else if (string.Equals(status, nameof(KnowledgeBaseStatus.Disabled), StringComparison.Ordinal))
                parsedStatus = KnowledgeBaseStatus.Disabled;
            else if (string.Equals(status, nameof(KnowledgeBaseStatus.Archived), StringComparison.Ordinal))
                parsedStatus = KnowledgeBaseStatus.Archived;
            else
            {
                return ServiceResult<IReadOnlyList<KnowledgeBaseListItem>>.Failure(
                    KnowledgeServiceStatusCodes.DocumentInvalid,
                    "Knowledge base status must be Enabled, Disabled, or Archived.");
            }
        }
        IReadOnlyList<KnowledgeBaseDefinition> definitions =
            await knowledgeBaseDefinitionServices.ListAsync(
                parsedStatus,
                cancellationToken);
        IReadOnlyList<KnowledgeBaseListItem> values = definitions
            .Select(value => new KnowledgeBaseListItem(
                value.Id,
                value.Code,
                value.Name,
                value.Description,
                value.Status,
                value.LogicalRevision,
                value.Documents.Count,
                value.Chunks.Count,
                value.IndexedAtUtc))
            .ToArray();
        return ServiceResult<IReadOnlyList<KnowledgeBaseListItem>>.QuerySuccess(values);
    }

    [HttpGet("{id:guid}")]
    public async Task<ServiceResult<KnowledgeBaseDetailResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        KnowledgeBaseDefinition? value =
            await knowledgeBaseDefinitionServices.GetByIdAsync(id, cancellationToken);
        return value is null
            ? ServiceResult<KnowledgeBaseDetailResponse>.Failure(
                KnowledgeServiceStatusCodes.NotFound,
                "The knowledge base was not found.")
            : ServiceResult<KnowledgeBaseDetailResponse>.QuerySuccess(ToDetail(value));
    }

    [HttpPost]
    public async Task<ServiceResult<KnowledgeBaseDetailResponse>> Create(
        [FromBody] CreateKnowledgeBaseRequest request,
        CancellationToken cancellationToken) => ToDetail(await knowledgeBaseDefinitionServices.CreateAsync(
        request.Code,
        request.Name,
        request.Description,
        cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<ServiceResult<KnowledgeBaseDetailResponse>> Update(
        Guid id,
        [FromBody] UpdateKnowledgeBaseRequest request,
        CancellationToken cancellationToken) => ToDetail(await knowledgeBaseDefinitionServices.UpdateAsync(
        id,
        request.ExpectedLogicalRevision,
        request.Name,
        request.Description,
        request.Status,
        cancellationToken));

    [HttpPost("{id:guid}/documents")]
    public async Task<ServiceResult<KnowledgeBaseDetailResponse>> ImportDocument(
        Guid id,
        [FromBody] ImportKnowledgeDocumentRequest request,
        CancellationToken cancellationToken) => ToDetail(await knowledgeBaseDefinitionServices.ImportDocumentAsync(
        id,
        request.ExpectedLogicalRevision,
        request.FileName,
        request.MediaType,
        request.Content,
        cancellationToken));


    [HttpPost("{id:guid}/documents/pdf"), Consumes("multipart/form-data")]
    [RequestSizeLimit(AgKnowledgeBaseDefinitionServices.MaximumPdfBytes + 65_536)]
    [RequestFormLimits(
        MultipartBodyLengthLimit = AgKnowledgeBaseDefinitionServices.MaximumPdfBytes + 65_536)]
    public async Task<ServiceResult<KnowledgeBaseDetailResponse>> ImportPdfDocument(
        Guid id,
        [FromForm] long expectedLogicalRevision,
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null
            || file.Length is 0 or > AgKnowledgeBaseDefinitionServices.MaximumPdfBytes)
        {
            return ServiceResult<KnowledgeBaseDetailResponse>.Failure(
                KnowledgeServiceStatusCodes.DocumentInvalid,
                $"A PDF file up to {AgKnowledgeBaseDefinitionServices.MaximumPdfBytes} bytes is required.");
        }

        using var buffer = new MemoryStream(checked((int)file.Length));
        await file.CopyToAsync(buffer, cancellationToken);
        return ToDetail(await knowledgeBaseDefinitionServices.ImportPdfDocumentAsync(
            id,
            expectedLogicalRevision,
            file.FileName,
            file.ContentType,
            buffer.ToArray(),
            cancellationToken));
    }

    [HttpDelete("{id:guid}/documents/{documentId:guid}")]
    public async Task<ServiceResult<KnowledgeBaseDetailResponse>> DeleteDocument(
        Guid id,
        Guid documentId,
        [FromBody] DeleteKnowledgeDocumentRequest request,
        CancellationToken cancellationToken) => ToDetail(await knowledgeBaseDefinitionServices.DeleteDocumentAsync(
        id,
        documentId,
        request.ExpectedLogicalRevision,
        cancellationToken));

    [HttpPut("{id:guid}/archive")]
    public async Task<ServiceResult<KnowledgeBaseDetailResponse>> SetArchived(
        Guid id,
        [FromBody] SetKnowledgeBaseArchiveRequest request,
        CancellationToken cancellationToken) => ToDetail(await knowledgeBaseDefinitionServices.SetArchivedAsync(
        id,
        request.ExpectedLogicalRevision,
        request.Archived,
        cancellationToken));

    [HttpGet("{id:guid}/documents")]
    public async Task<ServiceResult<IReadOnlyList<KnowledgeDocumentListItemResponse>>> ListDocuments(Guid id, CancellationToken cancellationToken)
    {
        KnowledgeBaseDefinition? value =
            await knowledgeBaseDefinitionServices.GetByIdAsync(id, cancellationToken);
        if (value is null)
        {
            return ServiceResult<IReadOnlyList<KnowledgeDocumentListItemResponse>>.Failure(
                KnowledgeServiceStatusCodes.NotFound,
                "The knowledge base was not found.");
        }

        IReadOnlyDictionary<Guid, int> chunkCounts = value.Chunks
            .GroupBy(chunk => chunk.DocumentId)
            .ToDictionary(group => group.Key, group => group.Count());
        IReadOnlyList<KnowledgeDocumentListItemResponse> documents = value.Documents
            .OrderByDescending(document => document.ImportedAtUtc)
            .ThenBy(document => document.FileName, StringComparer.Ordinal)
            .Select(document => new KnowledgeDocumentListItemResponse(
                document.Id,
                document.FileName,
                document.MediaType,
                document.Sha256,
                document.Content.Length,
                chunkCounts.GetValueOrDefault(document.Id),
                document.ImportedAtUtc))
            .ToArray();
        return ServiceResult<IReadOnlyList<KnowledgeDocumentListItemResponse>>.QuerySuccess(documents);
    }

    [HttpGet("{id:guid}/documents/{documentId:guid}/chunks")]
    public async Task<ServiceResult<KnowledgeChunkPageResponse>> ListDocumentChunks(
        Guid id,
        Guid documentId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10,
        CancellationToken cancellationToken = default)
    {
        if (skip < 0 || take is < 1 or > 50)
        {
            return ServiceResult<KnowledgeChunkPageResponse>.Failure(
                KnowledgeServiceStatusCodes.DocumentInvalid,
                "Chunk paging requires skip >= 0 and take between 1 and 50.");
        }

        KnowledgeBaseDefinition? value =
            await knowledgeBaseDefinitionServices.GetByIdAsync(id, cancellationToken);
        if (value is null)
        {
            return ServiceResult<KnowledgeChunkPageResponse>.Failure(
                KnowledgeServiceStatusCodes.NotFound,
                "The knowledge base was not found.");
        }

        KnowledgeDocument? document = value.Documents.FirstOrDefault(item => item.Id == documentId);
        if (document is null)
        {
            return ServiceResult<KnowledgeChunkPageResponse>.Failure(
                KnowledgeServiceStatusCodes.DocumentNotFound,
                "The knowledge document was not found.");
        }

        KnowledgeChunk[] allChunks = value.Chunks
            .Where(chunk => chunk.DocumentId == documentId)
            .OrderBy(chunk => chunk.Sequence)
            .ToArray();
        IReadOnlyList<KnowledgeChunkResponse> items = allChunks
            .Skip(skip)
            .Take(take)
            .Select(chunk => new KnowledgeChunkResponse(
                chunk.Id,
                chunk.Sequence,
                chunk.Content,
                chunk.Content.Length))
            .ToArray();
        return ServiceResult<KnowledgeChunkPageResponse>.QuerySuccess(new KnowledgeChunkPageResponse(
            document.Id,
            document.FileName,
            skip,
            take,
            allChunks.Length,
            items));
    }

    [HttpPost("{id:guid}/search")]
    public async Task<ServiceResult<IReadOnlyList<KnowledgeSearchResult>>> Search(Guid id, [FromBody] SearchKnowledgeRequest request, CancellationToken cancellationToken)
    {
        if (request.Query.IsNullOrEmpty())
            return ServiceResult<IReadOnlyList<KnowledgeSearchResult>>.Failure(
                     KnowledgeServiceStatusCodes.DocumentInvalid,
                     "Search query is required.");

        IReadOnlyList<KnowledgeSearchResult> values = await knowledgeBaseDefinitionServices.SearchAsync(
            [id],
            request.Query.Trim(),
            request.Take,
            cancellationToken);
        return ServiceResult<IReadOnlyList<KnowledgeSearchResult>>.QuerySuccess(values);
    }

    private static KnowledgeBaseDetailResponse ToDetail(KnowledgeBaseDefinition value) =>
        new(
            value.Id,
            value.Code,
            value.Name,
            value.Description,
            value.Status,
            value.LogicalRevision,
            value.Documents.Count,
            value.Chunks.Count,
            value.IndexedAtUtc);

    private static ServiceResult<KnowledgeBaseDetailResponse> ToDetail(ServiceResult<KnowledgeBaseDefinition> result) =>
        new()
        {
            Status = result.Status,
            Success = result.Success,
            Message = result.Message,
            MessageDev = result.MessageDev,
            Count = result.Count,
            Data = result.Data is null ? null! : ToDetail(result.Data)
        };
}

/// <summary>
/// 提供已发布知识库引用查询的 HTTP 接口。
/// </summary>
[Route("api/knowledge-base-references")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class KnowledgeBaseReferencesController(IAgKnowledgeBaseDefinitionServices knowledgeBaseDefinitionServices) : Base.ControllerBase
{
    [HttpGet]
    public async Task<ServiceResult<IReadOnlyList<PublishedKnowledgeReference>>> List(CancellationToken cancellationToken)
    {
        var values = await knowledgeBaseDefinitionServices.ListPublishedAsync(cancellationToken);
        return ServiceResult<IReadOnlyList<PublishedKnowledgeReference>>.QuerySuccess(values);
    }
}

/// <summary>
/// 创建知识库的请求。
/// </summary>
/// <param name="Code">业务唯一编码。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <summary>
/// 创建知识库的请求。
/// </summary>
/// <param name="Code">业务唯一编码。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
public sealed record CreateKnowledgeBaseRequest(string Code, string Name, string Description);

/// <summary>
/// 更新知识库的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Status">当前状态。</param>
/// <summary>
/// 更新知识库的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Status">当前状态。</param>
public sealed record UpdateKnowledgeBaseRequest(
    long ExpectedLogicalRevision,
    string Name,
    string Description,
    KnowledgeBaseStatus Status);

/// <summary>
/// 导入知识库文档的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="FileName">文件名称。</param>
/// <param name="MediaType">文件媒体类型。</param>
/// <param name="Content">文本内容。</param>
/// <summary>
/// 导入知识库文档的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="FileName">文件名称。</param>
/// <param name="MediaType">文件媒体类型。</param>
/// <param name="Content">文本内容。</param>
public sealed record ImportKnowledgeDocumentRequest(
    long ExpectedLogicalRevision,
    string FileName,
    string MediaType,
    string Content);

/// <summary>
/// 删除知识库文档的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <summary>
/// 删除知识库文档的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
public sealed record DeleteKnowledgeDocumentRequest(long ExpectedLogicalRevision);

/// <summary>
/// 检索知识库的请求。
/// </summary>
/// <param name="Query">知识检索查询文本。</param>
/// <param name="Take">本次请求返回的最大数量。</param>
/// <summary>
/// 检索知识库的请求。
/// </summary>
/// <param name="Query">知识检索查询文本。</param>
/// <param name="Take">本次请求返回的最大数量。</param>
public sealed record SearchKnowledgeRequest(string Query, int Take = 6);

/// <summary>
/// 设置知识库归档状态的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Archived">是否设置为归档状态。</param>
/// <summary>
/// 设置知识库归档状态的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Archived">是否设置为归档状态。</param>
public sealed record SetKnowledgeBaseArchiveRequest(
    long ExpectedLogicalRevision,
    bool Archived);

/// <summary>
/// 知识库详情响应。
/// </summary>
/// <param name="Id">对象标识。</param>
/// <param name="Code">业务唯一编码。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Status">当前状态。</param>
/// <param name="LogicalRevision">当前逻辑版本。</param>
/// <param name="DocumentCount">知识库中的文档数量。</param>
/// <param name="ChunkCount">知识分块数量。</param>
/// <param name="IndexedAtUtc">最近完成索引的 UTC 时间。</param>
/// <summary>
/// 知识库详情响应。
/// </summary>
/// <param name="Id">对象标识。</param>
/// <param name="Code">业务唯一编码。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Status">当前状态。</param>
/// <param name="LogicalRevision">当前逻辑版本。</param>
/// <param name="DocumentCount">知识库中的文档数量。</param>
/// <param name="ChunkCount">知识分块数量。</param>
/// <param name="IndexedAtUtc">最近完成索引的 UTC 时间。</param>
public sealed record KnowledgeBaseDetailResponse(
    Guid Id,
    string Code,
    string Name,
    string Description,
    KnowledgeBaseStatus Status,
    long LogicalRevision,
    int DocumentCount,
    int ChunkCount,
    DateTimeOffset? IndexedAtUtc);

/// <summary>
/// 知识库文档列表项响应。
/// </summary>
/// <param name="Id">对象标识。</param>
/// <param name="FileName">文件名称。</param>
/// <param name="MediaType">文件媒体类型。</param>
/// <param name="Sha256">内容的 SHA-256 摘要。</param>
/// <param name="CharacterCount">文本字符数量。</param>
/// <param name="ChunkCount">知识分块数量。</param>
/// <param name="ImportedAtUtc">文档导入的 UTC 时间。</param>
/// <summary>
/// 知识库文档列表项响应。
/// </summary>
/// <param name="Id">对象标识。</param>
/// <param name="FileName">文件名称。</param>
/// <param name="MediaType">文件媒体类型。</param>
/// <param name="Sha256">内容的 SHA-256 摘要。</param>
/// <param name="CharacterCount">文本字符数量。</param>
/// <param name="ChunkCount">知识分块数量。</param>
/// <param name="ImportedAtUtc">文档导入的 UTC 时间。</param>
public sealed record KnowledgeDocumentListItemResponse(
    Guid Id,
    string FileName,
    string MediaType,
    string Sha256,
    int CharacterCount,
    int ChunkCount,
    DateTimeOffset ImportedAtUtc);

/// <summary>
/// 知识分块响应。
/// </summary>
/// <param name="Id">对象标识。</param>
/// <param name="Sequence">分块或事件的顺序号。</param>
/// <param name="Content">文本内容。</param>
/// <param name="CharacterCount">文本字符数量。</param>
/// <summary>
/// 知识分块响应。
/// </summary>
/// <param name="Id">对象标识。</param>
/// <param name="Sequence">分块或事件的顺序号。</param>
/// <param name="Content">文本内容。</param>
/// <param name="CharacterCount">文本字符数量。</param>
public sealed record KnowledgeChunkResponse(
    Guid Id,
    int Sequence,
    string Content,
    int CharacterCount);

/// <summary>
/// 知识分块分页响应。
/// </summary>
/// <param name="DocumentId">知识库文档标识。</param>
/// <param name="FileName">文件名称。</param>
/// <param name="Skip">跳过的记录数量。</param>
/// <param name="Take">本次请求返回的最大数量。</param>
/// <param name="TotalCount">符合条件的记录总数。</param>
/// <param name="Items">当前页的数据项集合。</param>
/// <summary>
/// 知识分块分页响应。
/// </summary>
/// <param name="DocumentId">知识库文档标识。</param>
/// <param name="FileName">文件名称。</param>
/// <param name="Skip">跳过的记录数量。</param>
/// <param name="Take">本次请求返回的最大数量。</param>
/// <param name="TotalCount">符合条件的记录总数。</param>
/// <param name="Items">当前页的数据项集合。</param>
public sealed record KnowledgeChunkPageResponse(
    Guid DocumentId,
    string FileName,
    int Skip,
    int Take,
    int TotalCount,
    IReadOnlyList<KnowledgeChunkResponse> Items);

#endregion
