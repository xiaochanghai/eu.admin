using EU.Core.IServices;
using EU.Core.Model;
using EU.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Api.Agent.Controllers;

[Route("api/knowledge-bases")]
public sealed class KnowledgeBasesController(
    IAgKnowledgeBaseDefinitionServices knowledgeBaseDefinitionServices) : Base.ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
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
                return FromError(
                    "REQUEST_INVALID",
                    "Knowledge base status must be Enabled, Disabled, or Archived.");
            }
        }
        IReadOnlyList<KnowledgeBaseDefinition> definitions =
            await knowledgeBaseDefinitionServices.ListAsync(
            new KnowledgeBaseQuery(parsedStatus),
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
        return QuerySuccess(values);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        KnowledgeBaseDefinition? value =
            await knowledgeBaseDefinitionServices.GetByIdAsync(id, cancellationToken);
        return value is null
            ? FromError(KnowledgeErrorCodes.NotFound, "The knowledge base was not found.")
            : QuerySuccess(ToDetail(value));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateKnowledgeBaseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await knowledgeBaseDefinitionServices.CreateAsync(
            new CreateKnowledgeBaseCommand(request.Code, request.Name, request.Description),
            cancellationToken);
        if (!result.Success)
        {
            return FromServiceError(result, KnowledgeServiceStatusCodes.ToErrorCode);
        }

        Response.Headers.Location = $"/api/knowledge-bases/{result.Data!.Id}";
        return OperationSuccess(
            ToDetail(result.Data),
            StatusCodes.Status201Created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateKnowledgeBaseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await knowledgeBaseDefinitionServices.UpdateAsync(
            new UpdateKnowledgeBaseCommand(
                id, request.ExpectedLogicalRevision, request.Name,
                request.Description, request.Status),
            cancellationToken);
        return result.Success
            ? OperationSuccess(ToDetail(result.Data!))
            : FromServiceError(result, KnowledgeServiceStatusCodes.ToErrorCode);
    }

    [HttpPost("{id:guid}/documents")]
    public async Task<IActionResult> ImportDocument(
        Guid id,
        [FromBody] ImportKnowledgeDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await knowledgeBaseDefinitionServices.ImportDocumentAsync(
                new ImportKnowledgeDocumentCommand(
                    id, request.ExpectedLogicalRevision, request.FileName,
                    request.MediaType, request.Content),
                cancellationToken);
        return result.Success
            ? OperationSuccess(ToDetail(result.Data!))
            : FromServiceError(result, KnowledgeServiceStatusCodes.ToErrorCode);
    }

    [HttpPost("{id:guid}/documents/pdf")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(AgKnowledgeBaseDefinitionServices.MaximumPdfBytes + 65_536)]
    [RequestFormLimits(
        MultipartBodyLengthLimit = AgKnowledgeBaseDefinitionServices.MaximumPdfBytes + 65_536)]
    public async Task<ServiceResult<KnowledgeBaseDefinition>> ImportPdfDocument(
        Guid id,
        [FromForm] long expectedLogicalRevision,
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null
            || file.Length is 0 or > AgKnowledgeBaseDefinitionServices.MaximumPdfBytes)
        {
            return ServiceResult<KnowledgeBaseDefinition>.OprateFailed(
                $"A PDF file up to {AgKnowledgeBaseDefinitionServices.MaximumPdfBytes} bytes is required.");
        }

        using var buffer = new MemoryStream(checked((int)file.Length));
        await file.CopyToAsync(buffer, cancellationToken);
        var result = await knowledgeBaseDefinitionServices.ImportPdfDocumentAsync(
                 new ImportPdfKnowledgeDocumentCommand(
                     id,
                     expectedLogicalRevision,
                     file.FileName,
                     file.ContentType,
                     buffer.ToArray()),
                 cancellationToken);
        return result;
    }

    [HttpDelete("{id:guid}/documents/{documentId:guid}")]
    public async Task<IActionResult> DeleteDocument(
        Guid id,
        Guid documentId,
        [FromBody] DeleteKnowledgeDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await knowledgeBaseDefinitionServices.DeleteDocumentAsync(
            new DeleteKnowledgeDocumentCommand(
                id,
                documentId,
                request.ExpectedLogicalRevision),
            cancellationToken);
        return result.Success
            ? OperationSuccess(ToDetail(result.Data!))
            : FromServiceError(result, KnowledgeServiceStatusCodes.ToErrorCode);
    }

    [HttpPut("{id:guid}/archive")]
    public async Task<IActionResult> SetArchived(
        Guid id,
        [FromBody] SetKnowledgeBaseArchiveRequest request,
        CancellationToken cancellationToken)
    {
        var result = await knowledgeBaseDefinitionServices.SetArchivedAsync(
            new SetKnowledgeBaseArchiveCommand(
                id,
                request.ExpectedLogicalRevision,
                request.Archived),
            cancellationToken);
        return result.Success
            ? OperationSuccess(ToDetail(result.Data!))
            : FromServiceError(result, KnowledgeServiceStatusCodes.ToErrorCode);
    }

    [HttpGet("{id:guid}/documents")]
    public async Task<IActionResult> ListDocuments(
        Guid id,
        CancellationToken cancellationToken)
    {
        KnowledgeBaseDefinition? value =
            await knowledgeBaseDefinitionServices.GetByIdAsync(id, cancellationToken);
        if (value is null)
        {
            return FromError(
                KnowledgeErrorCodes.NotFound,
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
        return QuerySuccess(documents);
    }

    [HttpGet("{id:guid}/documents/{documentId:guid}/chunks")]
    public async Task<IActionResult> ListDocumentChunks(
        Guid id,
        Guid documentId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10,
        CancellationToken cancellationToken = default)
    {
        if (skip < 0 || take is < 1 or > 50)
        {
            return FromError(
                KnowledgeErrorCodes.DocumentInvalid,
                "Chunk paging requires skip >= 0 and take between 1 and 50.");
        }

        KnowledgeBaseDefinition? value =
            await knowledgeBaseDefinitionServices.GetByIdAsync(id, cancellationToken);
        if (value is null)
        {
            return FromError(
                KnowledgeErrorCodes.NotFound,
                "The knowledge base was not found.");
        }

        KnowledgeDocument? document = value.Documents.FirstOrDefault(item => item.Id == documentId);
        if (document is null)
        {
            return FromError(
                KnowledgeErrorCodes.DocumentNotFound,
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
        return QuerySuccess(new KnowledgeChunkPageResponse(
            document.Id,
            document.FileName,
            skip,
            take,
            allChunks.Length,
            items));
    }

    [HttpPost("{id:guid}/search")]
    public async Task<IActionResult> Search(
        Guid id,
        [FromBody] SearchKnowledgeRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return FromError(
                KnowledgeErrorCodes.DocumentInvalid,
                "Search query is required.");
        }
        IReadOnlyList<KnowledgeSearchResult> values = await knowledgeBaseDefinitionServices.SearchAsync(
            [id],
            request.Query.Trim(),
            request.Take,
            cancellationToken);
        return QuerySuccess(values);
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
}

[Route("api/knowledge-base-references")]
public sealed class KnowledgeBaseReferencesController(
    IAgKnowledgeBaseDefinitionServices knowledgeBaseDefinitionServices) : Base.ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<PublishedKnowledgeReference> values =
            await knowledgeBaseDefinitionServices.ListPublishedAsync(cancellationToken);
        return QuerySuccess(values);
    }
}

public sealed record CreateKnowledgeBaseRequest(string Code, string Name, string Description);
public sealed record UpdateKnowledgeBaseRequest(
    long ExpectedLogicalRevision,
    string Name,
    string Description,
    KnowledgeBaseStatus Status);
public sealed record ImportKnowledgeDocumentRequest(
    long ExpectedLogicalRevision,
    string FileName,
    string MediaType,
    string Content);
public sealed record DeleteKnowledgeDocumentRequest(long ExpectedLogicalRevision);
public sealed record SearchKnowledgeRequest(string Query, int Take = 6);
public sealed record SetKnowledgeBaseArchiveRequest(
    long ExpectedLogicalRevision,
    bool Archived);
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
public sealed record KnowledgeDocumentListItemResponse(
    Guid Id,
    string FileName,
    string MediaType,
    string Sha256,
    int CharacterCount,
    int ChunkCount,
    DateTimeOffset ImportedAtUtc);
public sealed record KnowledgeChunkResponse(
    Guid Id,
    int Sequence,
    string Content,
    int CharacterCount);
public sealed record KnowledgeChunkPageResponse(
    Guid DocumentId,
    string FileName,
    int Skip,
    int Take,
    int TotalCount,
    IReadOnlyList<KnowledgeChunkResponse> Items);

