using EU.Core.Agent.Application.Mcp;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Agent.Api.Controllers;

[ApiController]
[Route("api/mcp/servers")]
public sealed class McpServersController(McpLifecycleService lifecycle) : ControllerBase
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
                return ApiProblemResults.Create(
                    HttpContext,
                    StatusCodes.Status400BadRequest,
                    "REQUEST_INVALID",
                    "The MCP status filter is invalid.");
            }

            parsedStatus = value;
        }

        return Ok(await lifecycle.ListAsync(
            new McpServerQuery(search, parsedStatus),
            cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        McpServerDefinition? server = await lifecycle.GetAsync(id, cancellationToken);
        return server is null
            ? ApiProblemResults.Create(
                HttpContext,
                StatusCodes.Status404NotFound,
                McpErrorCodes.NotFound,
                "The MCP Server was not found.")
            : Ok(server);
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
        return result.Succeeded
            ? Created($"/api/mcp/servers/{result.Value!.Id}", result.Value)
            : FromError(result.Error!);
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
        return result.Succeeded ? Ok(result.Value) : FromError(result.Error!);
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
        return result.Succeeded ? Ok(result.Value) : FromError(result.Error!);
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
        return result.Succeeded ? Ok(result.Value) : FromError(result.Error!);
    }

    private IActionResult FromError(McpError error)
    {
        int status = error.Code switch
        {
            McpErrorCodes.NotFound or McpErrorCodes.ToolNotFound =>
                StatusCodes.Status404NotFound,
            McpErrorCodes.CodeConflict or McpErrorCodes.RevisionConflict =>
                StatusCodes.Status409Conflict,
            McpErrorCodes.DiscoveryFailed =>
                StatusCodes.Status502BadGateway,
            _ =>
                StatusCodes.Status400BadRequest
        };
        return ApiProblemResults.Create(
            HttpContext,
            status,
            error.Code,
            "The MCP operation could not be completed.",
            error.Message);
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

public sealed record ClassifyMcpToolRequest(
    long ExpectedLogicalRevision,
    McpToolRisk Risk);
