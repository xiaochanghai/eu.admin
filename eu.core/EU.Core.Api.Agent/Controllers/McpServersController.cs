using EU.Core.IServices.Mcp;
using Microsoft.AspNetCore.Mvc;
using EU.Core.IServices;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;

namespace EU.Core.Api.Agent.Controllers;

[Route("api/mcp/servers")]
public sealed class McpServersController(
    IAgMcpServerDefinitionServices lifecycle) : Base.ControllerBase
{
    [HttpGet]
    public async Task<ServiceResult<IReadOnlyList<McpServerDefinition>>> List(
        [FromQuery] string? search,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        McpServerStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse(status, ignoreCase: false, out McpServerStatus value))
            {
                return ServiceResult<IReadOnlyList<McpServerDefinition>>.Failure(
                    McpServiceStatusCodes.ConfigurationInvalid,
                    "The MCP status filter is invalid.");
            }

            parsedStatus = value;
        }

        IReadOnlyList<McpServerDefinition> values = await lifecycle.ListAsync(
            new McpServerQuery(search, parsedStatus),
            cancellationToken);
        return ServiceResult<IReadOnlyList<McpServerDefinition>>.QuerySuccess(values);
    }

    [HttpGet("{id:guid}")]
    public async Task<ServiceResult<McpServerDefinition>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        McpServerDefinition? server = await lifecycle.GetAsync(id, cancellationToken);
        return server is null
            ? ServiceResult<McpServerDefinition>.Failure(
                McpServiceStatusCodes.NotFound,
                "The MCP Server was not found.")
            : ServiceResult<McpServerDefinition>.QuerySuccess(server);
    }

    [HttpPost]
    public async Task<ServiceResult<McpServerDefinition>> Create(
        [FromBody] CreateMcpServerRequest request,
        CancellationToken cancellationToken) =>
        await lifecycle.CreateAsync(
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

    [HttpPut("{id:guid}")]
    public async Task<ServiceResult<McpServerDefinition>> Update(
        Guid id,
        [FromBody] UpdateMcpServerRequest request,
        CancellationToken cancellationToken) =>
        await lifecycle.UpdateAsync(
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

    [HttpPost("{id:guid}/sync")]
    public async Task<ServiceResult<McpServerDefinition>> Sync(
        Guid id,
        [FromBody] SyncMcpServerRequest request,
        CancellationToken cancellationToken) =>
        await lifecycle.SyncAsync(
            new SyncMcpServerCommand(id, request.ExpectedLogicalRevision),
            cancellationToken);

    [HttpPut("{id:guid}/archive")]
    public async Task<ServiceResult<McpServerDefinition>> SetArchived(
        Guid id,
        [FromBody] SetMcpServerArchiveRequest request,
        CancellationToken cancellationToken) =>
        await lifecycle.SetArchivedAsync(
            new SetMcpServerArchiveCommand(
                id,
                request.ExpectedLogicalRevision,
                request.Archived),
            cancellationToken);

    [HttpPut("{id:guid}/tools/{toolVersionId:guid}/risk")]
    public async Task<ServiceResult<McpServerDefinition>> ClassifyTool(
        Guid id,
        Guid toolVersionId,
        [FromBody] ClassifyMcpToolRequest request,
        CancellationToken cancellationToken) =>
        await lifecycle.ClassifyToolAsync(
            new ClassifyMcpToolCommand(
                id,
                toolVersionId,
                request.ExpectedLogicalRevision,
                request.Risk),
            cancellationToken);
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
