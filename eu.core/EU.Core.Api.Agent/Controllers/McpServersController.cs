using EU.Core.IServices;
using EU.Core.Api.Agent.Security;
using EU.Core.IServices.Mcp;
using EU.Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace EU.Core.Api.Agent.Controllers;

#region 文件职责：McpServersController 接口处理

/// <summary>
/// 提供 MCP 服务定义管理的 HTTP 接口。
/// </summary>
[Route("api/mcp/servers")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
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
    public async Task<ServiceResult<McpServerDefinition>> Get(Guid id, CancellationToken cancellationToken)
    {
        McpServerDefinition? server = await lifecycle.GetAsync(id, cancellationToken);
        return server is null
            ? ServiceResult<McpServerDefinition>.Failure(
                McpServiceStatusCodes.NotFound,
                "The MCP Server was not found.")
            : ServiceResult<McpServerDefinition>.QuerySuccess(server);
    }

    [HttpPost]
    public async Task<ServiceResult<McpServerDefinition>> Create([FromBody] CreateMcpServerRequest request, CancellationToken cancellationToken) =>
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
    public async Task<ServiceResult<McpServerDefinition>> Update(Guid id, [FromBody] UpdateMcpServerRequest request, CancellationToken cancellationToken) =>
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
    public async Task<ServiceResult<McpServerDefinition>> Sync(Guid id, [FromBody] SyncMcpServerRequest request, CancellationToken cancellationToken) =>
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

/// <summary>
/// 创建 MCP 服务定义的请求。
/// </summary>
/// <param name="Code">业务唯一编码。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Transport">MCP 服务使用的传输方式。</param>
/// <param name="Endpoint">MCP 服务端点地址。</param>
/// <param name="Command">启动标准输入输出服务的命令。</param>
/// <param name="Arguments">启动命令的参数集合。</param>
/// <param name="CredentialAlias">访问 MCP 服务使用的凭据别名。</param>
/// <param name="Enabled">是否启用。</param>
/// <summary>
/// 创建 MCP 服务定义的请求。
/// </summary>
/// <param name="Code">业务唯一编码。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Transport">MCP 服务使用的传输方式。</param>
/// <param name="Endpoint">MCP 服务端点地址。</param>
/// <param name="Command">启动标准输入输出服务的命令。</param>
/// <param name="Arguments">启动命令的参数集合。</param>
/// <param name="CredentialAlias">访问 MCP 服务使用的凭据别名。</param>
/// <param name="Enabled">是否启用。</param>
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

/// <summary>
/// 更新 MCP 服务定义的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Transport">MCP 服务使用的传输方式。</param>
/// <param name="Endpoint">MCP 服务端点地址。</param>
/// <param name="Command">启动标准输入输出服务的命令。</param>
/// <param name="Arguments">启动命令的参数集合。</param>
/// <param name="CredentialAlias">访问 MCP 服务使用的凭据别名。</param>
/// <param name="Enabled">是否启用。</param>
/// <summary>
/// 更新 MCP 服务定义的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Transport">MCP 服务使用的传输方式。</param>
/// <param name="Endpoint">MCP 服务端点地址。</param>
/// <param name="Command">启动标准输入输出服务的命令。</param>
/// <param name="Arguments">启动命令的参数集合。</param>
/// <param name="CredentialAlias">访问 MCP 服务使用的凭据别名。</param>
/// <param name="Enabled">是否启用。</param>
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

/// <summary>
/// 同步 MCP 服务工具的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <summary>
/// 同步 MCP 服务工具的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
public sealed record SyncMcpServerRequest(long ExpectedLogicalRevision);

/// <summary>
/// 设置 MCP 服务归档状态的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Archived">是否设置为归档状态。</param>
/// <summary>
/// 设置 MCP 服务归档状态的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Archived">是否设置为归档状态。</param>
public sealed record SetMcpServerArchiveRequest(
    long ExpectedLogicalRevision,
    bool Archived);

/// <summary>
/// 设置 MCP 工具风险等级的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Risk">工具风险等级。</param>
/// <summary>
/// 设置 MCP 工具风险等级的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Risk">工具风险等级。</param>
public sealed record ClassifyMcpToolRequest(
    long ExpectedLogicalRevision,
    McpToolRisk Risk);

#endregion
