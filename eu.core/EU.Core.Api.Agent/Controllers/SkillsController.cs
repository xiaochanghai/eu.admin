using EU.Core.Agent.Application.Skills;
using EU.Core.Api.Agent.Security;
using EU.Core.IServices;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace EU.Core.Api.Agent.Controllers;

[ApiController]
[Route("api/skills")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class SkillsController(
    IAgSkillDefinitionServices lifecycle,
    IAgentDefinitionCatalog agents) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
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
                return new JsonResult(
                    ServiceResult<IReadOnlyList<SkillListItem>>.OprateFailed(
                        "Skill status must be Active or Archived."))
                {
                    StatusCode = StatusCodes.Status200OK
                };
            }
        }

        IReadOnlyList<SkillListItem> values = await lifecycle.ListAsync(
            new SkillQuery(search, category, parsedStatus), cancellationToken);
        return new JsonResult(
            ServiceResult<IReadOnlyList<SkillListItem>>.QuerySuccess(values))
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        SkillDefinition? skill = await lifecycle.GetAsync(id, cancellationToken);
        if (skill is null)
        {
            return new JsonResult(
                ServiceResult<SkillDefinitionDetailResponse>.OprateFailed(
                    "The Skill was not found."))
            {
                StatusCode = StatusCodes.Status200OK
            };
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
        return new JsonResult(
            ServiceResult<SkillDefinitionDetailResponse>.QuerySuccess(value))
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    [HttpPost]
    public async Task<IActionResult> Create(
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
            return new JsonResult(result)
            {
                StatusCode = StatusCodes.Status200OK
            };
        }

        Response.Headers.Location = $"/api/skills/{result.Data.Id}";
        return new JsonResult(
            ServiceResult<SkillDefinition>.OprateSuccess(result.Data, "创建成功"))
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
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
        return new JsonResult(result)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    [HttpGet("{id:guid}/files")]
    public async Task<IActionResult> ListFiles(Guid id, CancellationToken cancellationToken)
    {
        ServiceResult<IReadOnlyList<SkillFileEntry>> result =
            await lifecycle.ListFilesAsync(id, cancellationToken);
        return new JsonResult(result)
        {
            StatusCode = StatusCodes.Status200OK
        };
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
            : new JsonResult(result)
            {
                StatusCode = StatusCodes.Status200OK
            };
    }

    [HttpPut("{id:guid}/files/content")]
    public async Task<IActionResult> SaveFile(
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
        return new JsonResult(result)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    [HttpDelete("{id:guid}/files/content")]
    public async Task<IActionResult> DeleteFile(
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
        return new JsonResult(result)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(
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
        return new JsonResult(result)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    [HttpPut("{id:guid}/archive")]
    public async Task<IActionResult> SetArchived(
        Guid id,
        [FromBody] SetSkillArchiveRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<SkillDefinition> result = await lifecycle.SetArchivedAsync(
            new SetSkillArchiveCommand(id, request.ExpectedDraftRevision, request.Archived),
            cancellationToken);
        return new JsonResult(result)
        {
            StatusCode = StatusCodes.Status200OK
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
