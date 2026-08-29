using EU.Core.IServices.Agents;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Errors;
using EU.Core.IServices;
using EU.Core.Model;
using EU.Core.Model.Models;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace EU.Core.Api.Agent.Controllers;

[Route("api/agents")]
public sealed class AgentsController(IPublicModelProfileCatalog modelProfiles, IAgAgentDefinitionServices agentDefinitionServices) : Base.ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ServiceResult<AgentListItem[]>>> List([FromQuery] string? search, [FromQuery] string? status, CancellationToken cancellationToken)
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
                AgentApiErrorDescriptor descriptor = AgentApiErrorResolver.Resolve(HttpContext, "REQUEST_INVALID");
                return new JsonResult(
                    ServiceResult<AgentApiErrorData>.Failure(
                        descriptor.Status,
                        "The status filter is invalid.",
                        new AgentApiErrorData("REQUEST_INVALID", HttpContext.TraceIdentifier)))
                {
                    StatusCode = StatusCodes.Status200OK
                };
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
        return ServiceResult<AgentListItem[]>.QuerySuccess(values);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceResult<AgAgentDefinitionDetailDto>>> Get(Guid id, CancellationToken cancellationToken)
    {
        AgAgentDefinitionDetailDto? value = await agentDefinitionServices.QueryAgent(
            id,
            cancellationToken);
        if (value is null)
        {
            AgentApiErrorDescriptor descriptor = AgentApiErrorResolver.Resolve(HttpContext, "AGENT_NOT_FOUND");
            return new JsonResult(
                ServiceResult<AgentApiErrorData>.Failure(
                    descriptor.Status,
                    "The Agent was not found.",
                    new AgentApiErrorData("AGENT_NOT_FOUND", HttpContext.TraceIdentifier)))
            {
                StatusCode = StatusCodes.Status200OK
            };
        }

        return ServiceResult<AgAgentDefinitionDetailDto>.QuerySuccess(value);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResult<AgAgentDefinitionDetailDto>>> Create([FromBody] CreateAgentRequest request, CancellationToken cancellationToken)
    {
        var result = await agentDefinitionServices.CreateAsync(new CreateAgentCommand(request.Code, request.Name, request.Description), cancellationToken);
        if (!result.Success)
        {
            return new JsonResult(result)
            {
                StatusCode = StatusCodes.Status200OK
            };
        }

        AgAgentDefinitionDetailDto value = await agentDefinitionServices.QueryAgent(
            result.Data,
            cancellationToken)
            ?? throw new InvalidDataException("The newly created Agent could not be loaded.");
        Response.Headers.Location = $"/api/agents/{result.Data}";
        return new JsonResult(
            ServiceResult<AgAgentDefinitionDetailDto>.OprateSuccess(value, "创建成功"))
        {
            StatusCode = StatusCodes.Status201Created
        };
    }

    [HttpPut("{id:guid}/draft")]
    public async Task<ActionResult<ServiceResult<AgentDefinition>>> SaveDraft(Guid id, [FromBody] SaveAgentDraftRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.ModelProfileId) &&
            !await modelProfiles.ExistsAsync(request.ModelProfileId, cancellationToken))
        {
            AgentApiErrorDescriptor descriptor = AgentApiErrorResolver.Resolve(HttpContext, "REQUEST_INVALID");
            return new JsonResult(
                ServiceResult<AgentApiErrorData>.Failure(
                    descriptor.Status,
                    "The selected model profile is not available.",
                    new AgentApiErrorData("REQUEST_INVALID", HttpContext.TraceIdentifier)))
            {
                StatusCode = StatusCodes.Status200OK
            };
        }

        ServiceResult<AgentDefinition> result = await agentDefinitionServices.SaveDraftAsync(
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
        return result;
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<ServiceResult<AgentDefinition>> Publish(Guid id, [FromBody] ExpectedRevisionRequest request, CancellationToken cancellationToken)
    {
        return await agentDefinitionServices.PublishAsync(
            new PublishAgentCommand(id, request.ExpectedLogicalRevision),
            cancellationToken);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ServiceResult<AgentDefinition>> SetStatus(Guid id, [FromBody] SetAgentStatusRequest request, CancellationToken cancellationToken)
    {
        return await agentDefinitionServices.SetRuntimeStatusAsync(
            new SetAgentRuntimeStatusCommand(
                id,
                request.ExpectedLogicalRevision,
                request.RuntimeStatus),
            cancellationToken);
    }

    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id, CancellationToken cancellationToken)
    {
        ServiceResult<string> result = await agentDefinitionServices.ExportAsync(id, cancellationToken);
        return result.Success
            ? File(
                Encoding.UTF8.GetBytes(result.Data),
                "application/json",
                "agent-package.json")
            : new JsonResult(result)
            {
                StatusCode = StatusCodes.Status200OK
            };
    }

    [HttpPost("import")]
    public async Task<ActionResult<ServiceResult<AgentDefinition>>> Import(CancellationToken cancellationToken)
    {
        if (!IsJsonContentType(Request.ContentType))
        {
            const string errorCode = "REQUEST_UNSUPPORTED_MEDIA_TYPE";
            AgentApiErrorDescriptor descriptor = AgentApiErrorResolver.Resolve(HttpContext, errorCode);
            return new JsonResult(
                ServiceResult<AgentApiErrorData>.Failure(
                    descriptor.Status,
                    "The Agent package must use a JSON content type.",
                    new AgentApiErrorData(errorCode, HttpContext.TraceIdentifier)))
            {
                StatusCode = StatusCodes.Status200OK
            };
        }

        using var reader = new StreamReader(
            Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 4096,
            leaveOpen: true);
        string json = await reader.ReadToEndAsync(cancellationToken);
        ServiceResult<AgentDefinition> result =
            await agentDefinitionServices.ImportAsync(json, cancellationToken);
        if (!result.Success)
        {
            return result;
        }

        Response.Headers.Location = $"/api/agents/{result.Data.Id}";
        return new JsonResult(
            ServiceResult<AgentDefinition>.OprateSuccess(result.Data, "导入成功"))
        {
            StatusCode = StatusCodes.Status201Created
        };
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
