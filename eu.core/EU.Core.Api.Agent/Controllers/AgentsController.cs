using EU.Core.Api.Agent.Security;
using EU.Core.IServices;
using EU.Core.IServices.Agents;
using EU.Core.Model;
using EU.Core.Model.Models;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace EU.Core.Api.Agent.Controllers;

#region 文件职责：AgentsController 接口处理

[Route("api/agents")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class AgentsController(IPublicModelProfileCatalog modelProfiles, IAgAgentDefinitionServices agentDefinitionServices) : Base.ControllerBase
{
    [HttpGet]
    public async Task<ServiceResult<AgentListItem[]>> List([FromQuery] string? search, [FromQuery] string? status, CancellationToken cancellationToken)
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
                throw new Exception("The status filter is invalid.");
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
    public async Task<ServiceResult<AgAgentDefinitionDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        AgAgentDefinitionDetailDto? value = await agentDefinitionServices.QueryAgent(
            id,
            cancellationToken);
        if (value is null)
        {
            throw new Exception("The Agent was not found.");
        }

        return ServiceResult<AgAgentDefinitionDetailDto>.QuerySuccess(value);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ServiceResult<AgAgentDefinitionDetailDto>),
        StatusCodes.Status201Created)]
    public async Task<ServiceResult<AgAgentDefinitionDetailDto>> Create([FromBody] CreateAgentRequest request, CancellationToken cancellationToken)
    {
        var result = await agentDefinitionServices.CreateAsync(new CreateAgentCommand(request.Code, request.Name, request.Description), cancellationToken);
        if (!result.Success)
        {
            return ServiceResult<AgAgentDefinitionDetailDto>.Failure(
                result.Status,
                result.Message);
        }

        AgAgentDefinitionDetailDto value = await agentDefinitionServices.QueryAgent(
            result.Data,
            cancellationToken)
            ?? throw new InvalidDataException("The newly created Agent could not be loaded.");
        Response.Headers.Location = $"/api/agents/{result.Data}";
        Response.StatusCode = StatusCodes.Status201Created;
        return Success(value, "创建成功");
    }

    [HttpPut("{id:guid}/draft")]
    public async Task<ServiceResult<AgentDefinition>> SaveDraft(Guid id, [FromBody] SaveAgentDraftRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.ModelProfileId) &&
            !await modelProfiles.ExistsAsync(request.ModelProfileId, cancellationToken))
        {
            throw new Exception("The selected model profile is not available.");
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
    [ProducesResponseType(
        typeof(ServiceResult<AgentDefinition>),
        StatusCodes.Status201Created)]
    public async Task<ServiceResult<AgentDefinition>> Import(CancellationToken cancellationToken)
    {
        if (!IsJsonContentType(Request.ContentType))
        {
            throw new Exception("The Agent package must use a JSON content type.");
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
        Response.StatusCode = StatusCodes.Status201Created;
        return Success(result.Data, "导入成功");
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

#endregion
