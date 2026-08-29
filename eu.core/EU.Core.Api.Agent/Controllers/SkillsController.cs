using EU.Core.IServices.Skills;
using EU.Core.Api.Agent.Errors;
using EU.Core.IServices;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace EU.Core.Api.Agent.Controllers;

[Route("api/skills")]
public sealed class SkillsController(
    IAgSkillDefinitionServices lifecycle,
    IAgentDefinitionCatalog agents) : Base.ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ServiceResult<IReadOnlyList<SkillListItem>>>> List(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        SkillStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (string.Equals(status, nameof(SkillStatus.Active), StringComparison.Ordinal))
            {
                parsedStatus = SkillStatus.Active;
            }
            else if (string.Equals(status, nameof(SkillStatus.Archived), StringComparison.Ordinal))
            {
                parsedStatus = SkillStatus.Archived;
            }
            else
            {
                return FromError(
                    SkillErrorCodes.LifecycleTransitionInvalid,
                    "Skill status must be Active or Archived.",
                    StatusCodes.Status400BadRequest);
            }
        }

        IReadOnlyList<SkillListItem> values = await lifecycle.ListAsync(
            new SkillQuery(search, category, parsedStatus), cancellationToken);
        return ServiceResult<IReadOnlyList<SkillListItem>>.QuerySuccess(values);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceResult<SkillDefinitionDetailResponse>>> Get(Guid id, CancellationToken cancellationToken)
    {
        SkillDefinition? skill = await lifecycle.GetAsync(id, cancellationToken);
        if (skill is null)
        {
            return FromError(SkillErrorCodes.NotFound, "The Skill was not found.");
        }

        IReadOnlyList<AgentDefinition> agentDefinitions = await agents.ListDefinitionsAsync(
            new AgentDefinitionQuery(),
            cancellationToken);
        var value = new SkillDefinitionDetailResponse(
            skill.Id,
            skill.Code,
            skill.Name,
            skill.Description,
            skill.Category,
            skill.Status,
            skill.DraftRevision,
            skill.PublishedVersions.Select(version => new SkillPublishedVersionResponse(
                version.Id,
                version.Label,
                version.ManifestSha256,
                version.PublishedAtUtc,
                version.Files,
                agentDefinitions
                    .Where(agent =>
                        agent.Draft.SkillVersionIds.Contains(version.Id) ||
                        agent.PublishedVersions.Any(published =>
                            published.Snapshot?.Skills.Any(binding =>
                                binding.SkillVersionId == version.Id) == true))
                    .Select(agent => new SkillBoundAgentResponse(agent.Id, agent.Code, agent.Name))
                    .DistinctBy(agent => agent.Id)
                    .OrderBy(agent => agent.Code, StringComparer.Ordinal)
                    .ToArray()))
                .ToArray());
        return ServiceResult<SkillDefinitionDetailResponse>.QuerySuccess(value);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResult<SkillDefinition>>> Create(
        [FromBody] CreateSkillRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<SkillDefinition> result = await lifecycle.CreateAsync(
            new CreateSkillCommand(
                request.Code,
                request.Name,
                request.Description,
                request.Category),
            cancellationToken);
        if (!result.Success)
        {
            return FromServiceError(result);
        }

        Response.Headers.Location = $"/api/skills/{result.Data.Id}";
        return new JsonResult(
            ServiceResult<SkillDefinition>.OprateSuccess(result.Data, "创建成功"))
        {
            StatusCode = StatusCodes.Status201Created
        };
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ServiceResult<SkillDefinition>>> Update(
        Guid id,
        [FromBody] UpdateSkillRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<SkillDefinition> result = await lifecycle.UpdateAsync(
            new UpdateSkillCommand(
                id,
                request.ExpectedDraftRevision,
                request.Name,
                request.Description,
                request.Category),
            cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }

    [HttpGet("{id:guid}/files")]
    public async Task<ActionResult<ServiceResult<IReadOnlyList<SkillFileEntry>>>> ListFiles(Guid id, CancellationToken cancellationToken)
    {
        ServiceResult<IReadOnlyList<SkillFileEntry>> result =
            await lifecycle.ListFilesAsync(id, cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }

    [HttpGet("{id:guid}/files/content")]
    public async Task<IActionResult> ReadFile(
        Guid id,
        [FromQuery] string? path,
        CancellationToken cancellationToken)
    {
        ServiceResult<string> result = await lifecycle.ReadFileAsync(
            id,
            path ?? string.Empty,
            cancellationToken);
        return result.Success
            ? Content(result.Data, "text/plain", Encoding.UTF8)
            : FromServiceError(result);
    }

    [HttpPut("{id:guid}/files/content")]
    public async Task<ActionResult<ServiceResult<SkillDefinition>>> SaveFile(
        Guid id,
        [FromBody] SaveSkillFileRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<SkillDefinition> result = await lifecycle.SaveFileAsync(
            new SaveSkillFileCommand(
                id,
                request.ExpectedDraftRevision,
                request.Path,
                request.Content),
            cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }

    [HttpDelete("{id:guid}/files/content")]
    public async Task<ActionResult<ServiceResult<SkillDefinition>>> DeleteFile(
        Guid id,
        [FromBody] DeleteSkillFileRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<SkillDefinition> result = await lifecycle.DeleteFileAsync(
            new DeleteSkillFileCommand(
                id,
                request.ExpectedDraftRevision,
                request.Path),
            cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<ServiceResult<SkillDefinition>>> Publish(
        Guid id,
        [FromBody] PublishSkillRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<SkillDefinition> result = await lifecycle.PublishAsync(
            new PublishSkillCommand(
                id,
                request.ExpectedDraftRevision,
                request.VersionLabel),
            cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }

    [HttpPut("{id:guid}/archive")]
    public async Task<ActionResult<ServiceResult<SkillDefinition>>> SetArchived(
        Guid id,
        [FromBody] SetSkillArchiveRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<SkillDefinition> result = await lifecycle.SetArchivedAsync(
            new SetSkillArchiveCommand(id, request.ExpectedDraftRevision, request.Archived),
            cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }

    private JsonResult FromServiceError<T>(ServiceResult<T> result) =>
        FromError(SkillServiceStatusCodes.ToErrorCode(result.Status), result.Message);

    private JsonResult FromError(
        string errorCode,
        string message,
        int? httpStatus = null)
    {
        AgentApiErrorDescriptor descriptor = AgentApiErrorResolver.Resolve(HttpContext, errorCode);
        return new JsonResult(
            ServiceResult<AgentApiErrorData>.Failure(
                descriptor.Status,
                message,
                new AgentApiErrorData(errorCode, HttpContext.TraceIdentifier)))
        {
            StatusCode = httpStatus ?? descriptor.HttpStatus ?? StatusCodes.Status500InternalServerError
        };
    }
}

public sealed record CreateSkillRequest(
    string Code,
    string Name,
    string Description,
    string Category);

public sealed record UpdateSkillRequest(
    long ExpectedDraftRevision,
    string Name,
    string Description,
    string Category);

public sealed record SaveSkillFileRequest(
    long ExpectedDraftRevision,
    string Path,
    string Content);

public sealed record DeleteSkillFileRequest(
    long ExpectedDraftRevision,
    string Path);

public sealed record PublishSkillRequest(
    long ExpectedDraftRevision,
    string VersionLabel);

public sealed record SetSkillArchiveRequest(
    long ExpectedDraftRevision,
    bool Archived);

public sealed record SkillDefinitionDetailResponse(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string Category,
    SkillStatus Status,
    long DraftRevision,
    IReadOnlyList<SkillPublishedVersionResponse> PublishedVersions);

public sealed record SkillPublishedVersionResponse(
    Guid Id,
    string Label,
    string ManifestSha256,
    DateTimeOffset PublishedAtUtc,
    IReadOnlyList<SkillFileHash> Files,
    IReadOnlyList<SkillBoundAgentResponse> BoundAgents);

public sealed record SkillBoundAgentResponse(Guid Id, string Code, string Name);
