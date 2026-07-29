using System.Text;
using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Application.Skills;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Agent.Api.Controllers;

[ApiController]
[Route("api/agents")]
public sealed class AgentsController(
    AgentLifecycleService lifecycle,
    AgentQueryService queries,
    AgentPackageService packages,
    IPublicModelProfileCatalog modelProfiles) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
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
            else
            {
                return ApiProblemResults.Create(
                    HttpContext,
                    StatusCodes.Status400BadRequest,
                    "REQUEST_INVALID",
                    "The status filter is invalid.");
            }
        }

        IReadOnlyList<AgentListItem> values = await queries.ListAsync(
            new AgentDefinitionQuery(search, runtimeStatus),
            cancellationToken);
        return Ok(values);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        AgentDefinition? definition = await queries.GetAsync(id, cancellationToken);
        return definition is null
            ? ApiProblemResults.Create(
                HttpContext,
                StatusCodes.Status404NotFound,
                AgentErrorCodes.NotFound,
                "The Agent was not found.")
            : Ok(definition);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAgentRequest request,
        CancellationToken cancellationToken)
    {
        AgentOperationResult<AgentDefinition> result = await lifecycle.CreateAsync(
            new CreateAgentCommand(request.Code, request.Name, request.Description),
            cancellationToken);
        return result.Succeeded
            ? Created($"/api/agents/{result.Value!.Id}", result.Value)
            : FromError(result.Error!);
    }

    [HttpPut("{id:guid}/draft")]
    public async Task<IActionResult> SaveDraft(
        Guid id,
        [FromBody] SaveAgentDraftRequest request,
        CancellationToken cancellationToken)
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

        AgentOperationResult<AgentDefinition> result = await lifecycle.SaveDraftAsync(
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
                request.KnowledgeBaseIds),
            cancellationToken);
        return result.Succeeded ? Ok(result.Value) : FromError(result.Error!);
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(
        Guid id,
        [FromBody] ExpectedRevisionRequest request,
        CancellationToken cancellationToken)
    {
        AgentOperationResult<AgentDefinition> result = await lifecycle.PublishAsync(
            new PublishAgentCommand(id, request.ExpectedLogicalRevision),
            cancellationToken);
        return result.Succeeded ? Ok(result.Value) : FromError(result.Error!);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid id,
        [FromBody] SetAgentStatusRequest request,
        CancellationToken cancellationToken)
    {
        AgentOperationResult<AgentDefinition> result = await lifecycle.SetRuntimeStatusAsync(
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
        AgentOperationResult<string> result = await packages.ExportAsync(id, cancellationToken);
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
            await packages.ImportAsync(json, cancellationToken);
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
}

public sealed record CreateAgentRequest(string Code, string Name, string Description);

public sealed record SaveAgentDraftRequest(
    long ExpectedLogicalRevision,
    string Name,
    string Description,
    string Instructions,
    string ModelProfileId,
    AgentOutputMode OutputMode,
    string? OutputJsonSchema,
    IReadOnlyList<Guid>? SkillVersionIds,
    IReadOnlyList<Guid>? ToolVersionIds,
    IReadOnlyList<Guid>? KnowledgeBaseIds);

public sealed record ExpectedRevisionRequest(long ExpectedLogicalRevision);

public sealed record SetAgentStatusRequest(
    long ExpectedLogicalRevision,
    AgentRuntimeStatus RuntimeStatus);
