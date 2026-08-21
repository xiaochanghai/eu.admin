using EU.Core.Api.Agent.Errors;
using EU.Core.Api.Agent.Security;
using EU.Core.IServices;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Api.Agent.Controllers;

[ApiController]
[Route("api/knowledge-bases")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class KnowledgeBasesController(
    IAgKnowledgeBaseDefinitionServices knowledgeBaseDefinitionServices) : ControllerBase
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
        ServiceResult<KnowledgeBaseDefinition> result = await knowledgeBaseDefinitionServices.CreateAsync(
            new CreateKnowledgeBaseCommand(request.Code, request.Name, request.Description),
            cancellationToken);
        if (!result.Success)
        {
            return FromServiceError(result);
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
        ServiceResult<KnowledgeBaseDefinition> result = await knowledgeBaseDefinitionServices.UpdateAsync(
            new UpdateKnowledgeBaseCommand(
                id, request.ExpectedLogicalRevision, request.Name,
                request.Description, request.Status),
            cancellationToken);
        return result.Success
            ? OperationSuccess(ToDetail(result.Data!))
            : FromServiceError(result);
    }

    [HttpPost("{id:guid}/documents")]
    public async Task<IActionResult> ImportDocument(
        Guid id,
        [FromBody] ImportKnowledgeDocumentRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<KnowledgeBaseDefinition> result =
            await knowledgeBaseDefinitionServices.ImportDocumentAsync(
                new ImportKnowledgeDocumentCommand(
                    id, request.ExpectedLogicalRevision, request.FileName,
                    request.MediaType, request.Content),
                cancellationToken);
        return result.Success
            ? OperationSuccess(ToDetail(result.Data!))
            : FromServiceError(result);
    }

    [HttpPost("{id:guid}/documents/pdf")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(AgKnowledgeBaseDefinitionServices.MaximumPdfBytes + 65_536)]
    [RequestFormLimits(
        MultipartBodyLengthLimit = AgKnowledgeBaseDefinitionServices.MaximumPdfBytes + 65_536)]
    public async Task<IActionResult> ImportPdfDocument(
        Guid id,
        [FromForm] long expectedLogicalRevision,
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null
            || file.Length is 0 or > AgKnowledgeBaseDefinitionServices.MaximumPdfBytes)
        {
            return FromError(
                KnowledgeErrorCodes.DocumentInvalid,
                $"A PDF file up to {AgKnowledgeBaseDefinitionServices.MaximumPdfBytes} bytes is required.");
        }

        using var buffer = new MemoryStream(checked((int)file.Length));
        await file.CopyToAsync(buffer, cancellationToken);
        ServiceResult<KnowledgeBaseDefinition> result =
            await knowledgeBaseDefinitionServices.ImportPdfDocumentAsync(
                new ImportPdfKnowledgeDocumentCommand(
                    id,
                    expectedLogicalRevision,
                    file.FileName,
                    file.ContentType,
                    buffer.ToArray()),
                cancellationToken);
        return result.Success
            ? OperationSuccess(ToDetail(result.Data!))
            : FromServiceError(result);
    }

    [HttpPut("{id:guid}/archive")]
    public async Task<IActionResult> SetArchived(
        Guid id,
        [FromBody] SetKnowledgeBaseArchiveRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<KnowledgeBaseDefinition> result = await knowledgeBaseDefinitionServices.SetArchivedAsync(
            new SetKnowledgeBaseArchiveCommand(
                id,
                request.ExpectedLogicalRevision,
                request.Archived),
            cancellationToken);
        return result.Success
            ? OperationSuccess(ToDetail(result.Data!))
            : FromServiceError(result);
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

    private IActionResult FromServiceError(
        ServiceResult<KnowledgeBaseDefinition> result) =>
        FromError(
            KnowledgeServiceStatusCodes.ToErrorCode(result.Status),
            result.Message);

    private IActionResult QuerySuccess<T>(T value) =>
        new JsonResult(
            ServiceResult<T>.QuerySuccess(value))
        {
            StatusCode = StatusCodes.Status200OK
        };

    private IActionResult OperationSuccess<T>(
        T value,
        int httpStatus = StatusCodes.Status200OK) =>
        new JsonResult(
            ServiceResult<T>.OprateSuccess(value))
        {
            StatusCode = httpStatus
        };

    private IActionResult FromError(string errorCode, string message)
    {
        AgentApiErrorDescriptor descriptor = AgentApiErrorResolver.Resolve(HttpContext, errorCode);
        return new JsonResult(
            ServiceResult<AgentApiErrorData>.Failure(
                descriptor.Status,
                message,
                new AgentApiErrorData(errorCode, HttpContext.TraceIdentifier)))
        {
            StatusCode = descriptor.HttpStatus ?? StatusCodes.Status500InternalServerError
        };
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

[ApiController]
[Route("api/knowledge-base-references")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class KnowledgeBaseReferencesController(
    IAgKnowledgeBaseDefinitionServices knowledgeBaseDefinitionServices) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<PublishedKnowledgeReference> values =
            await knowledgeBaseDefinitionServices.ListPublishedAsync(cancellationToken);
        return new JsonResult(
            ServiceResult<IReadOnlyList<PublishedKnowledgeReference>>.QuerySuccess(values))
        {
            StatusCode = StatusCodes.Status200OK
        };
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

