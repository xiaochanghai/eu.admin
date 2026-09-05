using EU.Core.IServices;
using EU.Core.Api.Agent.Security;
using EU.Core.IServices.Mcp;
using EU.Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace EU.Core.Api.Agent.Controllers;

// 文件职责：McpServersController 接口处理

/// <summary>
/// 提供 MCP 服务定义管理的 HTTP 接口。
/// </summary>
/// <param name="lifecycle">用于管理 MCP 服务定义及其生命周期的服务。</param>
[Route("api/mcp/servers")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class McpServersController(
    IAgMcpServerDefinitionServices lifecycle) : Base.ControllerBase
{
    #region 查询列表（List）
    /// <summary>
    /// 查询列表（List）
    /// </summary>
    /// <param name="search">用于筛选记录的搜索文本。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含MCP 服务定义集合，失败时包含错误状态和提示。</returns>
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
    #endregion

    #region 获取（Get）
    /// <summary>
    /// 获取（Get）
    /// </summary>
    /// <param name="id">MCP 服务标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含MCP 服务定义，失败时包含错误状态和提示。</returns>
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
    #endregion

    #region 创建（Create）
    /// <summary>
    /// 创建（Create）
    /// </summary>
    /// <param name="request">创建MCP 服务所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含MCP 服务定义，失败时包含错误状态和提示。</returns>
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
    #endregion

    #region 更新（Update）
    /// <summary>
    /// 更新（Update）
    /// </summary>
    /// <param name="id">MCP 服务标识。</param>
    /// <param name="request">更新MCP 服务所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含MCP 服务定义，失败时包含错误状态和提示。</returns>
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
    #endregion

    #region 处理（Sync）
    /// <summary>
    /// 处理（Sync）
    /// </summary>
    /// <param name="id">MCP 服务标识。</param>
    /// <param name="request">同步工具MCP 服务所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含MCP 服务定义，失败时包含错误状态和提示。</returns>
    [HttpPost("{id:guid}/sync")]
    public async Task<ServiceResult<McpServerDefinition>> Sync(Guid id, [FromBody] SyncMcpServerRequest request, CancellationToken cancellationToken) =>
        await lifecycle.SyncAsync(
            new SyncMcpServerCommand(id, request.ExpectedLogicalRevision),
            cancellationToken);
    #endregion

    #region 设置（SetArchived）
    /// <summary>
    /// 设置（SetArchived）
    /// </summary>
    /// <param name="id">MCP 服务标识。</param>
    /// <param name="request">归档或恢复MCP 服务所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含MCP 服务定义，失败时包含错误状态和提示。</returns>
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
    #endregion

    #region 处理（ClassifyTool）
    /// <summary>
    /// 处理（ClassifyTool）
    /// </summary>
    /// <param name="id">MCP 服务标识。</param>
    /// <param name="toolVersionId">工具版本标识。</param>
    /// <param name="request">设置工具风险分类MCP 服务所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含MCP 服务定义，失败时包含错误状态和提示。</returns>
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
    #endregion
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
public sealed record SyncMcpServerRequest(long ExpectedLogicalRevision);

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
public sealed record ClassifyMcpToolRequest(
    long ExpectedLogicalRevision,
    McpToolRisk Risk);
