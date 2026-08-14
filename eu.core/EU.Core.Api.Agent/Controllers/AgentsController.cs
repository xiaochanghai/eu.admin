using EU.Core.Agent.Application.Agents;
using EU.Core.Api.Agent.Security;
using EU.Core.IServices;
using EU.Core.Model;
using EU.Core.Model.Models;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace EU.Core.Api.Agent.Controllers;

[ApiController]
[Route("api/agents")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class AgentsController(IPublicModelProfileCatalog modelProfiles, IAgAgentDefinitionServices agentDefinitionServices) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        AgentRuntimeStatus? runtimeStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (string.Equals(status, nameof(AgentRuntimeStatus.Enabled), StringComparison.Ordinal))
            {
                runtimeStatus = AgentRuntimeStatus.Enabled;
            }
            else if (string.Equals(status, nameof(AgentRuntimeStatus.Disabled), StringComparison.Ordinal))
            {
                runtimeStatus = AgentRuntimeStatus.Disabled;
            }
            else if (string.Equals(status, nameof(AgentRuntimeStatus.Archived), StringComparison.Ordinal))
            {
                runtimeStatus = AgentRuntimeStatus.Archived;
            }
            else
            {
                return ApiProblemResults.Create(
                    HttpContext,
                    StatusCodes.Status400BadRequest,
                    "REQUEST_INVALID",
                    "The status filter is invalid.");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        var definitions = await agentDefinitionServices.QueryAgentList(
            search,
            runtimeStatus?.ToString(),
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        AgentListItem[] values = definitions.Select(definition => new AgentListItem(
            definition.ID,
            definition.Code,
            definition.Name,
            definition.Description,
            ParseRuntimeStatus(definition.RuntimeStatus),
            definition.LogicalRevision ?? throw new InvalidDataException(
                $"Agent '{definition.Code}' does not have a LogicalRevision."),
            definition.DraftLabel,
            definition.DraftModelProfileId,
            definition.CurrentPublishedLabel)).ToArray();
        return Ok(values);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        AgAgentDefinitionDetailDto? value = await agentDefinitionServices.QueryAgent(
            id,
            cancellationToken);
        return value is null
            ? ApiProblemResults.Create(
                HttpContext,
                StatusCodes.Status404NotFound,
                AgentErrorCodes.NotFound,
                "The Agent was not found.")
            : Ok(value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAgentRequest request, CancellationToken cancellationToken)
    {
        var result = await agentDefinitionServices.CreateAsync( new CreateAgentCommand(request.Code, request.Name, request.Description), cancellationToken);
        if (!result.Success)
        {
            return ApiProblemResults.Create(
                HttpContext,
                StatusCodes.Status409Conflict,
                AgentErrorCodes.CodeConflict,
                "The Agent operation could not be completed.",
                result.Message);
        }

        AgAgentDefinitionDetailDto value = await agentDefinitionServices.QueryAgent(
            result.Data,
            cancellationToken)
            ?? throw new InvalidDataException("The newly created Agent could not be loaded.");
        return Created($"/api/agents/{result.Data}", value);
    }

    [HttpPut("{id:guid}/draft")]
    public async Task<IActionResult> SaveDraft(Guid id, [FromBody] SaveAgentDraftRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.ModelProfileId) &&
            !await modelProfiles.ExistsAsync(request.ModelProfileId, cancellationToken))
        {
            return ApiProblemResults.Create(
                HttpContext,
                StatusCodes.Status400BadRequest,
                AgentErrorCodes.ReferenceMissing,
                "The selected model profile is not available.");
        }

        AgentOperationResult<AgentDefinition> result = await agentDefinitionServices.SaveDraftAsync(
            new SaveAgentDraftCommand(
                id,
                request.ExpectedLogicalRevision,
                request.Instructions,
                request.ModelProfileId,
                request.OutputMode,
                request.OutputJsonSchema,
                request.Name,
                request.Description,
                request.SkillVersionIds,
                request.ToolVersionIds,
                request.KnowledgeBaseIds)
            {
                ChildAgentIds = request.ChildAgentIds,
                OrchestrationIds = request.OrchestrationIds
            },
            cancellationToken);
        return result.Succeeded ? Ok(result.Value) : FromError(result.Error!);
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, [FromBody] ExpectedRevisionRequest request, CancellationToken cancellationToken)
    {
        AgentOperationResult<AgentDefinition> result = await agentDefinitionServices.PublishAsync(
            new PublishAgentCommand(id, request.ExpectedLogicalRevision),
            cancellationToken);
        return result.Succeeded ? Ok(result.Value) : FromError(result.Error!);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetAgentStatusRequest request, CancellationToken cancellationToken)
    {
        AgentOperationResult<AgentDefinition> result = await agentDefinitionServices.SetRuntimeStatusAsync(
            new SetAgentRuntimeStatusCommand(
                id,
                request.ExpectedLogicalRevision,
                request.RuntimeStatus),
            cancellationToken);
        return result.Succeeded ? Ok(result.Value) : FromError(result.Error!);
    }

    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id, CancellationToken cancellationToken)
    {
        AgentOperationResult<string> result = await agentDefinitionServices.ExportAsync(id, cancellationToken);
        return result.Succeeded
            ? File(
                Encoding.UTF8.GetBytes(result.Value!),
                "application/json",
                "agent-package.json")
            : FromError(result.Error!);
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import(CancellationToken cancellationToken)
    {
        if (!IsJsonContentType(Request.ContentType))
        {
            return ApiProblemResults.Create(
                HttpContext,
                StatusCodes.Status415UnsupportedMediaType,
                "REQUEST_INVALID",
                "The Agent package must use a JSON content type.");
        }

        using var reader = new StreamReader(
            Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 4096,
            leaveOpen: true);
        string json = await reader.ReadToEndAsync(cancellationToken);
        AgentOperationResult<AgentDefinition> result =
            await agentDefinitionServices.ImportAsync(json, cancellationToken);
        return result.Succeeded
            ? Created($"/api/agents/{result.Value!.Id}", result.Value)
            : FromError(result.Error!);
    }

    private IActionResult FromError(AgentError error)
    {
        int status = error.Code switch
        {
            AgentErrorCodes.CodeConflict or AgentErrorCodes.RowVersionConflict =>
                StatusCodes.Status409Conflict,
            AgentErrorCodes.NotFound =>
                StatusCodes.Status404NotFound,
            _ =>
                StatusCodes.Status400BadRequest
        };
        return ApiProblemResults.Create(
            HttpContext,
            status,
            error.Code,
            "The Agent operation could not be completed.",
            error.Message);
    }

    private static bool IsJsonContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        string mediaType = contentType.Split(';', 2)[0].Trim();
        return string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase) ||
               mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
    }

    private static AgentRuntimeStatus ParseRuntimeStatus(string value) =>
        Enum.TryParse(value, ignoreCase: false, out AgentRuntimeStatus status)
            ? status
            : throw new InvalidDataException($"Unsupported Agent runtime status '{value}'.");
}

public sealed record CreateAgentRequest(string Code, string Name, string Description);

public sealed record SaveAgentDraftRequest(long ExpectedLogicalRevision, string Name, string Description, string Instructions, string ModelProfileId,
    AgentOutputMode OutputMode, string? OutputJsonSchema, IReadOnlyList<Guid>? SkillVersionIds, IReadOnlyList<Guid>? ToolVersionIds, IReadOnlyList<Guid>? KnowledgeBaseIds)
{
    public IReadOnlyList<Guid>? ChildAgentIds { get; init; }
    public IReadOnlyList<Guid>? OrchestrationIds { get; init; }
}

public sealed record ExpectedRevisionRequest(long ExpectedLogicalRevision);

public sealed record SetAgentStatusRequest(long ExpectedLogicalRevision, AgentRuntimeStatus RuntimeStatus);
