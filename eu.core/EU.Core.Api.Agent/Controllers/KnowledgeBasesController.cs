using EU.Core.Agent.Application.Knowledge;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EU.Core.Api.Agent.Security;

namespace EU.Core.Api.Agent.Controllers;

[ApiController]
[Route("api/knowledge-bases")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class KnowledgeBasesController(KnowledgeLifecycleService lifecycle) : ControllerBase
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
                return Problem(
                    KnowledgeErrorCodes.LifecycleTransitionInvalid,
                    "Knowledge base status must be Enabled, Disabled, or Archived.",
                    400);
            }
        }
        return Ok(await lifecycle.ListAsync(new KnowledgeBaseQuery(parsedStatus), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        KnowledgeBaseDefinition? value = await lifecycle.GetAsync(id, cancellationToken);
        return value is null
            ? Problem(KnowledgeErrorCodes.NotFound, "The knowledge base was not found.", 404)
            : Ok(ToDetail(value));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateKnowledgeBaseRequest request,
        CancellationToken cancellationToken)
    {
        KnowledgeOperationResult<KnowledgeBaseDefinition> result = await lifecycle.CreateAsync(
            new CreateKnowledgeBaseCommand(request.Code, request.Name, request.Description),
            cancellationToken);
        return result.Succeeded
            ? Created($"/api/knowledge-bases/{result.Value!.Id}", ToDetail(result.Value))
            : FromError(result.Error!);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateKnowledgeBaseRequest request,
        CancellationToken cancellationToken)
    {
        KnowledgeOperationResult<KnowledgeBaseDefinition> result = await lifecycle.UpdateAsync(
            new UpdateKnowledgeBaseCommand(
                id, request.ExpectedLogicalRevision, request.Name,
                request.Description, request.Status),
            cancellationToken);
        return result.Succeeded ? Ok(ToDetail(result.Value!)) : FromError(result.Error!);
    }

    [HttpPost("{id:guid}/documents")]
    public async Task<IActionResult> ImportDocument(
        Guid id,
        [FromBody] ImportKnowledgeDocumentRequest request,
        CancellationToken cancellationToken)
    {
        KnowledgeOperationResult<KnowledgeBaseDefinition> result =
            await lifecycle.ImportDocumentAsync(
                new ImportKnowledgeDocumentCommand(
                    id, request.ExpectedLogicalRevision, request.FileName,
                    request.MediaType, request.Content),
                cancellationToken);
        return result.Succeeded ? Ok(ToDetail(result.Value!)) : FromError(result.Error!);
    }

    [HttpPost("{id:guid}/documents/pdf")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(KnowledgeLifecycleService.MaximumPdfBytes + 65_536)]
    [RequestFormLimits(
        MultipartBodyLengthLimit = KnowledgeLifecycleService.MaximumPdfBytes + 65_536)]
    public async Task<IActionResult> ImportPdfDocument(
        Guid id,
        [FromForm] long expectedLogicalRevision,
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null
            || file.Length is 0 or > KnowledgeLifecycleService.MaximumPdfBytes)
        {
            return Problem(
                KnowledgeErrorCodes.DocumentInvalid,
                $"A PDF file up to {KnowledgeLifecycleService.MaximumPdfBytes} bytes is required.",
                400);
        }

        using var buffer = new MemoryStream(checked((int)file.Length));
        await file.CopyToAsync(buffer, cancellationToken);
        KnowledgeOperationResult<KnowledgeBaseDefinition> result =
            await lifecycle.ImportPdfDocumentAsync(
                new ImportPdfKnowledgeDocumentCommand(
                    id,
                    expectedLogicalRevision,
                    file.FileName,
                    file.ContentType,
                    buffer.ToArray()),
                cancellationToken);
        return result.Succeeded ? Ok(ToDetail(result.Value!)) : FromError(result.Error!);
    }

    [HttpPut("{id:guid}/archive")]
    public async Task<IActionResult> SetArchived(
        Guid id,
        [FromBody] SetKnowledgeBaseArchiveRequest request,
        CancellationToken cancellationToken)
    {
        KnowledgeOperationResult<KnowledgeBaseDefinition> result = await lifecycle.SetArchivedAsync(
            new SetKnowledgeBaseArchiveCommand(
                id,
                request.ExpectedLogicalRevision,
                request.Archived),
            cancellationToken);
        return result.Succeeded ? Ok(ToDetail(result.Value!)) : FromError(result.Error!);
    }

    [HttpGet("{id:guid}/documents")]
    public async Task<IActionResult> ListDocuments(
        Guid id,
        CancellationToken cancellationToken)
    {
        KnowledgeBaseDefinition? value = await lifecycle.GetAsync(id, cancellationToken);
        if (value is null)
        {
            return Problem(KnowledgeErrorCodes.NotFound, "The knowledge base was not found.", 404);
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
        return Ok(documents);
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
            return Problem(
                KnowledgeErrorCodes.DocumentInvalid,
                "Chunk paging requires skip >= 0 and take between 1 and 50.",
                400);
        }

        KnowledgeBaseDefinition? value = await lifecycle.GetAsync(id, cancellationToken);
        if (value is null)
        {
            return Problem(KnowledgeErrorCodes.NotFound, "The knowledge base was not found.", 404);
        }

        KnowledgeDocument? document = value.Documents.FirstOrDefault(item => item.Id == documentId);
        if (document is null)
        {
            return Problem(
                KnowledgeErrorCodes.DocumentNotFound,
                "The knowledge document was not found.",
                404);
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
        return Ok(new KnowledgeChunkPageResponse(
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
            return Problem(KnowledgeErrorCodes.DocumentInvalid, "Search query is required.", 400);
        }
        return Ok(await lifecycle.SearchAsync(id, request.Query.Trim(), request.Take, cancellationToken));
    }

    private IActionResult FromError(KnowledgeError error) =>
        Problem(
            error.Code,
            error.Message,
            error.Code switch
            {
                KnowledgeErrorCodes.NotFound => 404,
                KnowledgeErrorCodes.DocumentNotFound => 404,
                KnowledgeErrorCodes.CodeConflict or KnowledgeErrorCodes.RowVersionConflict => 409,
                _ => 400
            });

    private IActionResult Problem(string code, string detail, int status) =>
        ApiProblemResults.Create(
            HttpContext, status, code, "The knowledge operation could not be completed.", detail);

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

[ApiController]
[Route("api/knowledge-base-references")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class KnowledgeBaseReferencesController(IPublishedKnowledgeCatalog catalog) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await catalog.ListAsync(cancellationToken));
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
