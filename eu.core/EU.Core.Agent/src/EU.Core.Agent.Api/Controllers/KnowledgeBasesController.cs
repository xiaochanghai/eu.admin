using EU.Core.Agent.Application.Knowledge;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Agent.Api.Controllers;

[ApiController]
[Route("api/knowledge-bases")]
public sealed class KnowledgeBasesController(KnowledgeLifecycleService lifecycle) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await lifecycle.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        KnowledgeBaseDefinition? value = await lifecycle.GetAsync(id, cancellationToken);
        return value is null
            ? Problem(KnowledgeErrorCodes.NotFound, "The knowledge base was not found.", 404)
            : Ok(value);
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
            ? Created($"/api/knowledge-bases/{result.Value!.Id}", result.Value)
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
        return result.Succeeded ? Ok(result.Value) : FromError(result.Error!);
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
        return result.Succeeded ? Ok(result.Value) : FromError(result.Error!);
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
                KnowledgeErrorCodes.CodeConflict or KnowledgeErrorCodes.RowVersionConflict => 409,
                _ => 400
            });

    private IActionResult Problem(string code, string detail, int status) =>
        ApiProblemResults.Create(
            HttpContext, status, code, "The knowledge operation could not be completed.", detail);
}

[ApiController]
[Route("api/knowledge-base-references")]
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
