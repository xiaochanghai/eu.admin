using EU.Core.Agent.Application.Mcp;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EU.Core.Api.Agent.Security;
using EU.Core.IServices;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;

namespace EU.Core.Api.Agent.Controllers;

[ApiController]
[Route("api/mcp/servers")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class McpServersController(IAgMcpServerDefinitionServices lifecycle) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        McpServerStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse(status, ignoreCase: false, out McpServerStatus value))
            {
                return FromError(
                    "REQUEST_INVALID",
                    "The MCP status filter is invalid.");
            }

            parsedStatus = value;
        }

        IReadOnlyList<McpServerDefinition> values = await lifecycle.ListAsync(
            new McpServerQuery(search, parsedStatus),
            cancellationToken);
        return QuerySuccess(values);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        McpServerDefinition? server = await lifecycle.GetAsync(id, cancellationToken);
        return server is null
            ? FromError(McpErrorCodes.NotFound, "The MCP Server was not found.")
            : QuerySuccess(server);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateMcpServerRequest request,
        CancellationToken cancellationToken)
    {
        McpOperationResult<McpServerDefinition> result = await lifecycle.CreateAsync(
            new CreateMcpServerCommand(
                request.Code,
                request.Name,
                request.Description,
                request.Transport,
                request.Endpoint,
                request.Command,
                request.Arguments,
                request.CredentialAlias,
                request.Enabled),
            cancellationToken);
        if (!result.Succeeded)
        {
            return FromError(result.Error!);
        }

        Response.Headers.Location = $"/api/mcp/servers/{result.Value!.Id}";
        return OperationSuccess(result.Value, StatusCodes.Status201Created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateMcpServerRequest request,
        CancellationToken cancellationToken)
    {
        McpOperationResult<McpServerDefinition> result = await lifecycle.UpdateAsync(
            new UpdateMcpServerCommand(
                id,
                request.ExpectedLogicalRevision,
                request.Name,
                request.Description,
                request.Transport,
                request.Endpoint,
                request.Command,
                request.Arguments,
                request.CredentialAlias,
                request.Enabled),
            cancellationToken);
        return result.Succeeded
            ? OperationSuccess(result.Value!)
            : FromError(result.Error!);
    }

    [HttpPost("{id:guid}/sync")]
    public async Task<IActionResult> Sync(
        Guid id,
        [FromBody] SyncMcpServerRequest request,
        CancellationToken cancellationToken)
    {
        McpOperationResult<McpServerDefinition> result = await lifecycle.SyncAsync(
            new SyncMcpServerCommand(id, request.ExpectedLogicalRevision),
            cancellationToken);
        return result.Succeeded
            ? OperationSuccess(result.Value!)
            : FromError(result.Error!);
    }

    [HttpPut("{id:guid}/archive")]
    public async Task<IActionResult> SetArchived(
        Guid id,
        [FromBody] SetMcpServerArchiveRequest request,
        CancellationToken cancellationToken)
    {
        McpOperationResult<McpServerDefinition> result =
            await lifecycle.SetArchivedAsync(
                new SetMcpServerArchiveCommand(
                    id,
                    request.ExpectedLogicalRevision,
                    request.Archived),
                cancellationToken);
        return result.Succeeded
            ? OperationSuccess(result.Value!)
            : FromError(result.Error!);
    }

    [HttpPut("{id:guid}/tools/{toolVersionId:guid}/risk")]
    public async Task<IActionResult> ClassifyTool(
        Guid id,
        Guid toolVersionId,
        [FromBody] ClassifyMcpToolRequest request,
        CancellationToken cancellationToken)
    {
        McpOperationResult<McpServerDefinition> result =
            await lifecycle.ClassifyToolAsync(
                new ClassifyMcpToolCommand(
                    id,
                    toolVersionId,
                    request.ExpectedLogicalRevision,
                    request.Risk),
                cancellationToken);
        return result.Succeeded
            ? OperationSuccess(result.Value!)
            : FromError(result.Error!);
    }

    private IActionResult QuerySuccess<T>(T value) =>
        new JsonResult(
            ServiceResult<T>.QuerySuccess(value),
            AgentJsonSerialization.PascalCase)
        {
            StatusCode = StatusCodes.Status200OK
        };

    private IActionResult OperationSuccess<T>(
        T value,
        int httpStatus = StatusCodes.Status200OK) =>
        new JsonResult(
            ServiceResult<T>.OprateSuccess(value),
            AgentJsonSerialization.PascalCase)
        {
            StatusCode = httpStatus
        };

    private IActionResult FromError(McpError error) =>
        FromError(error.Code, error.Message);

    private IActionResult FromError(string errorCode, string message)
    {
        AgentApiErrorDescriptor descriptor = AgentApiErrorCatalog.Resolve(errorCode);
        return new JsonResult(
            ServiceResult<AgentApiErrorData>.Failure(
                descriptor.Status,
                message,
                new AgentApiErrorData(errorCode, HttpContext.TraceIdentifier)),
            AgentJsonSerialization.PascalCase)
        {
            StatusCode = descriptor.HttpStatus ?? StatusCodes.Status500InternalServerError
        };
    }
}

public sealed record CreateMcpServerRequest(
    string Code,
    string Name,
    string Description,
    McpTransportKind Transport,
    string Endpoint,
    string Command,
    IReadOnlyList<string>? Arguments,
    string CredentialAlias,
    bool Enabled);

public sealed record UpdateMcpServerRequest(
    long ExpectedLogicalRevision,
    string Name,
    string Description,
    McpTransportKind Transport,
    string Endpoint,
    string Command,
    IReadOnlyList<string>? Arguments,
    string CredentialAlias,
    bool Enabled);

public sealed record SyncMcpServerRequest(long ExpectedLogicalRevision);

public sealed record SetMcpServerArchiveRequest(
    long ExpectedLogicalRevision,
    bool Archived);

public sealed record ClassifyMcpToolRequest(
    long ExpectedLogicalRevision,
    McpToolRisk Risk);
