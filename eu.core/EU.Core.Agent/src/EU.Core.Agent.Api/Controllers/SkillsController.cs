using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Application.Skills;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Agent.Api.Controllers;

[ApiController]
[Route("api/skills")]
public sealed class SkillsController(
    SkillLifecycleService lifecycle,
    IAgentRepository agents) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? category,
        CancellationToken cancellationToken) =>
        Ok(await lifecycle.ListAsync(new SkillQuery(search, category), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        SkillDefinition? skill = await lifecycle.GetAsync(id, cancellationToken);
        if (skill is null)
        {
            return ApiProblemResults.Create(
                HttpContext,
                StatusCodes.Status404NotFound,
                SkillErrorCodes.NotFound,
                "The Skill was not found.");
        }

        IReadOnlyList<AgentDefinition> agentDefinitions = await agents.ListAsync(
            new AgentDefinitionQuery(),
            cancellationToken);
        return Ok(new
        {
            skill.Id,
            skill.Code,
            skill.Name,
            skill.Description,
            skill.Category,
            skill.DraftRevision,
            publishedVersions = skill.PublishedVersions.Select(version => new
            {
                version.Id,
                version.Label,
                version.ManifestSha256,
                version.PublishedAtUtc,
                version.Files,
                boundAgents = agentDefinitions
                    .Where(agent =>
                        agent.Draft.SkillVersionIds.Contains(version.Id) ||
                        agent.PublishedVersions.Any(published =>
                            published.Snapshot?.Skills.Any(binding =>
                                binding.SkillVersionId == version.Id) == true))
                    .Select(agent => new { agent.Id, agent.Code, agent.Name })
                    .DistinctBy(agent => agent.Id)
                    .OrderBy(agent => agent.Code, StringComparer.Ordinal)
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateSkillRequest request,
        CancellationToken cancellationToken)
    {
        SkillOperationResult<SkillDefinition> result = await lifecycle.CreateAsync(
            new CreateSkillCommand(
                request.Code,
                request.Name,
                request.Description,
                request.Category),
            cancellationToken);
        return result.Succeeded
            ? Created($"/api/skills/{result.Value!.Id}", result.Value)
            : FromError(result.Error!);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateSkillRequest request,
        CancellationToken cancellationToken)
    {
        SkillOperationResult<SkillDefinition> result = await lifecycle.UpdateAsync(
            new UpdateSkillCommand(
                id,
                request.ExpectedDraftRevision,
                request.Name,
                request.Description,
                request.Category),
            cancellationToken);
        return result.Succeeded ? Ok(result.Value) : FromError(result.Error!);
    }

    [HttpGet("{id:guid}/files")]
    public async Task<IActionResult> ListFiles(Guid id, CancellationToken cancellationToken)
    {
        SkillOperationResult<IReadOnlyList<SkillFileEntry>> result =
            await lifecycle.ListFilesAsync(id, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : FromError(result.Error!);
    }

    [HttpGet("{id:guid}/files/content")]
    public async Task<IActionResult> ReadFile(
        Guid id,
        [FromQuery] string? path,
        CancellationToken cancellationToken)
    {
        SkillOperationResult<string> result = await lifecycle.ReadFileAsync(
            id,
            path ?? string.Empty,
            cancellationToken);
        return result.Succeeded
            ? Ok(new { path, content = result.Value })
            : FromError(result.Error!);
    }

    [HttpPut("{id:guid}/files/content")]
    public async Task<IActionResult> SaveFile(
        Guid id,
        [FromBody] SaveSkillFileRequest request,
        CancellationToken cancellationToken)
    {
        SkillOperationResult<SkillDefinition> result = await lifecycle.SaveFileAsync(
            new SaveSkillFileCommand(
                id,
                request.ExpectedDraftRevision,
                request.Path,
                request.Content),
            cancellationToken);
        return result.Succeeded ? Ok(result.Value) : FromError(result.Error!);
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(
        Guid id,
        [FromBody] PublishSkillRequest request,
        CancellationToken cancellationToken)
    {
        SkillOperationResult<SkillDefinition> result = await lifecycle.PublishAsync(
            new PublishSkillCommand(
                id,
                request.ExpectedDraftRevision,
                request.VersionLabel),
            cancellationToken);
        return result.Succeeded ? Ok(result.Value) : FromError(result.Error!);
    }

    private IActionResult FromError(SkillError error)
    {
        int status = error.Code switch
        {
            SkillErrorCodes.NotFound or SkillErrorCodes.FileMissing =>
                StatusCodes.Status404NotFound,
            SkillErrorCodes.CodeConflict or
                SkillErrorCodes.RevisionConflict or
                SkillErrorCodes.VersionConflict =>
                StatusCodes.Status409Conflict,
            SkillErrorCodes.FileTooLarge =>
                StatusCodes.Status413PayloadTooLarge,
            _ =>
                StatusCodes.Status400BadRequest
        };
        return ApiProblemResults.Create(
            HttpContext,
            status,
            error.Code,
            "The Skill operation could not be completed.",
            error.Message);
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

public sealed record PublishSkillRequest(
    long ExpectedDraftRevision,
    string VersionLabel);
