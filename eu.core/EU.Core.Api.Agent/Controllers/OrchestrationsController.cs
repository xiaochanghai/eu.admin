using EU.Core.IServices.Orchestration;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Errors;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Mvc;
using EU.Core.IServices;
using EU.Core.Services;
using Microsoft.AspNetCore.Authorization;
using EU.Core.Api.Agent.Security;

namespace EU.Core.Api.Agent.Controllers;

// 文件职责：OrchestrationsController 接口处理

/// <summary>
/// 提供编排定义及运行管理的 HTTP 接口。
/// </summary>
/// <param name="lifecycle">用于管理编排定义及发布状态的服务。</param>
/// <param name="runtime">用于执行已发布编排的运行服务。</param>
[Route("api/orchestrations")]
public sealed class OrchestrationsController(
    IOrchestrationLifecycleService lifecycle,
    OrchestrationRuntimeService runtime) : Base.ControllerBase
{
    #region 查询列表（List）
    /// <summary>
    /// 查询列表（List）
    /// </summary>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含编排摘要集合，失败时包含错误状态和提示。</returns>
    [HttpGet]
    [Authorize(Policy = AgentAuthorizationPolicies.Admin)]
    public async Task<ActionResult<ServiceResult<IReadOnlyList<OrchestrationListItem>>>> List([FromQuery] string? status, CancellationToken cancellationToken)
    {
        OrchestrationStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (string.Equals(status, nameof(OrchestrationStatus.Enabled), StringComparison.Ordinal))
                parsedStatus = OrchestrationStatus.Enabled;
            else if (string.Equals(status, nameof(OrchestrationStatus.Disabled), StringComparison.Ordinal))
                parsedStatus = OrchestrationStatus.Disabled;
            else if (string.Equals(status, nameof(OrchestrationStatus.Archived), StringComparison.Ordinal))
                parsedStatus = OrchestrationStatus.Archived;
            else
            {
                return FromError(
                    OrchestrationErrorCodes.LifecycleTransitionInvalid,
                    "Orchestration status must be Enabled, Disabled, or Archived.");
            }
        }
        return ServiceResult<IReadOnlyList<OrchestrationListItem>>.QuerySuccess(
            await lifecycle.ListAsync(parsedStatus, cancellationToken));
    }
    #endregion

    #region 获取（Get）
    /// <summary>
    /// 获取（Get）
    /// </summary>
    /// <param name="id">编排标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含编排定义，失败时包含错误状态和提示。</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AgentAuthorizationPolicies.Admin)]
    public async Task<ActionResult<ServiceResult<OrchestrationDefinition>>> Get(Guid id, CancellationToken cancellationToken)
    {
        OrchestrationDefinition? value = await lifecycle.GetAsync(id, cancellationToken);
        return value is null
            ? FromError(OrchestrationErrorCodes.NotFound, "The orchestration was not found.")
            : ServiceResult<OrchestrationDefinition>.QuerySuccess(value);
    }
    #endregion

    #region 创建（Create）
    /// <summary>
    /// 创建（Create）
    /// </summary>
    /// <param name="request">创建编排所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含编排定义，失败时包含错误状态和提示。</returns>
    [HttpPost]
    [Authorize(Policy = AgentAuthorizationPolicies.Admin)]
    public async Task<ActionResult<ServiceResult<OrchestrationDefinition>>> Create(
        [FromBody] CreateOrchestrationRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<OrchestrationDefinition> result = await lifecycle.CreateAsync(
            new CreateOrchestrationCommand(request.Code, request.Name, request.Description), cancellationToken);
        if (!result.Success)
            return FromServiceError(result);

        Response.Headers.Location = $"/api/orchestrations/{result.Data!.Id}";
        return OperationSuccess(result.Data, StatusCodes.Status201Created);
    }
    #endregion

    #region 保存（SaveDraft）
    /// <summary>
    /// 保存（SaveDraft）
    /// </summary>
    /// <param name="id">编排标识。</param>
    /// <param name="request">保存草稿编排所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含编排定义，失败时包含错误状态和提示。</returns>
    [HttpPut("{id:guid}/draft")]
    [Authorize(Policy = AgentAuthorizationPolicies.Admin)]
    public async Task<ActionResult<ServiceResult<OrchestrationDefinition>>> SaveDraft(
        Guid id,
        [FromBody] SaveOrchestrationRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<OrchestrationDefinition> result = await lifecycle.SaveDraftAsync(
            new SaveOrchestrationDraftCommand(
                id, request.ExpectedLogicalRevision, request.Name, request.Description,
                request.Status, request.StartNodeId, request.Nodes, request.Edges), cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }
    #endregion

    #region 发布（Publish）
    /// <summary>
    /// 发布（Publish）
    /// </summary>
    /// <param name="id">编排标识。</param>
    /// <param name="request">发布编排所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含编排定义，失败时包含错误状态和提示。</returns>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Policy = AgentAuthorizationPolicies.Admin)]
    public async Task<ActionResult<ServiceResult<OrchestrationDefinition>>> Publish(
        Guid id,
        [FromBody] PublishOrchestrationRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<OrchestrationDefinition> result = await lifecycle.PublishAsync(
            new PublishOrchestrationCommand(id, request.ExpectedLogicalRevision), cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }
    #endregion

    #region 设置（SetArchived）
    /// <summary>
    /// 设置（SetArchived）
    /// </summary>
    /// <param name="id">编排标识。</param>
    /// <param name="request">归档或恢复编排所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含编排定义，失败时包含错误状态和提示。</returns>
    [HttpPut("{id:guid}/archive")]
    [Authorize(Policy = AgentAuthorizationPolicies.Admin)]
    public async Task<ActionResult<ServiceResult<OrchestrationDefinition>>> SetArchived(
        Guid id,
        [FromBody] SetOrchestrationArchiveRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<OrchestrationDefinition> result = await lifecycle.SetArchivedAsync(
            new SetOrchestrationArchiveCommand(
                id,
                request.ExpectedLogicalRevision,
                request.Archived),
            cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }
    #endregion

    #region 处理（Start）
    /// <summary>
    /// 处理（Start）
    /// </summary>
    /// <param name="id">编排标识。</param>
    /// <param name="request">启动编排所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含编排运行记录，失败时包含错误状态和提示。</returns>
    [HttpPost("{id:guid}/runs")]
    [Authorize(Policy = AgentAuthorizationPolicies.Debug)]
    public async Task<ActionResult<ServiceResult<OrchestrationRunRecord>>> Start(
        Guid id,
        [FromBody] StartOrchestrationRunRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<OrchestrationRunRecord> result =
            await runtime.StartAsync(id, request.Input, cancellationToken);
        if (!result.Success)
            return FromServiceError(result);

        Response.Headers.Location = $"/api/orchestrations/{id}/runs/{result.Data!.Id}";
        return OperationSuccess(result.Data, StatusCodes.Status202Accepted);
    }
    #endregion

    #region 运行（Runs）
    /// <summary>
    /// 运行（Runs）
    /// </summary>
    /// <param name="id">编排标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含编排运行记录集合，失败时包含错误状态和提示。</returns>
    [HttpGet("{id:guid}/runs")]
    [Authorize(Policy = AgentAuthorizationPolicies.Debug)]
    public async Task<ActionResult<ServiceResult<IReadOnlyList<OrchestrationRunRecord>>>> Runs(
        Guid id, [FromQuery] int take = 20, CancellationToken cancellationToken = default) =>
        ServiceResult<IReadOnlyList<OrchestrationRunRecord>>.QuerySuccess(await runtime.ListAsync(
            id,
            Math.Clamp(take, 1, 100),
            cancellationToken));
    #endregion

    #region 运行（Run）
    /// <summary>
    /// 运行（Run）
    /// </summary>
    /// <param name="id">编排标识。</param>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含编排运行记录，失败时包含错误状态和提示。</returns>
    [HttpGet("{id:guid}/runs/{runId:guid}")]
    [Authorize(Policy = AgentAuthorizationPolicies.Debug)]
    public async Task<ActionResult<ServiceResult<OrchestrationRunRecord>>> Run(Guid id, Guid runId, CancellationToken cancellationToken)
    {
        OrchestrationRunRecord? value = await runtime.GetAsync(runId, cancellationToken);
        return value is null || value.OrchestrationId != id
            ? FromError(OrchestrationErrorCodes.RunNotFound, "The orchestration run was not found.")
            : ServiceResult<OrchestrationRunRecord>.QuerySuccess(value);
    }
    #endregion

    #region 取消（Cancel）
    /// <summary>
    /// 取消（Cancel）
    /// </summary>
    /// <param name="id">编排标识。</param>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含编排运行取消结果，失败时包含错误状态和提示。</returns>
    [HttpPost("{id:guid}/runs/{runId:guid}/cancel")]
    [Authorize(Policy = AgentAuthorizationPolicies.Debug)]
    public async Task<ActionResult<ServiceResult<OrchestrationRunCancelResponse>>> Cancel(Guid id, Guid runId, CancellationToken cancellationToken)
    {
        OrchestrationRunRecord? value = await runtime.GetAsync(runId, cancellationToken);
        if (value is null || value.OrchestrationId != id)
            return FromError(OrchestrationErrorCodes.RunNotFound, "The orchestration run was not found.");
        await runtime.CancelAsync(runId, cancellationToken);
        return OperationSuccess(
            new OrchestrationRunCancelResponse(runId),
            StatusCodes.Status202Accepted);
    }
    #endregion

    #region 处理（Details）
    /// <summary>
    /// 处理（Details）
    /// </summary>
    /// <param name="id">编排标识。</param>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含编排运行及节点尝试详情，失败时包含错误状态和提示。</returns>
    [HttpGet("{id:guid}/runs/{runId:guid}/details")]
    [Authorize(Policy = AgentAuthorizationPolicies.Debug)]
    public async Task<ActionResult<ServiceResult<OrchestrationRunDetails>>> Details(Guid id, Guid runId, CancellationToken cancellationToken)
    {
        OrchestrationRunRecord? value = await runtime.GetAsync(runId, cancellationToken);
        if (value is null || value.OrchestrationId != id)
            return FromError(OrchestrationErrorCodes.RunNotFound, "The orchestration run was not found.");
        OrchestrationRunDetails? details = await runtime.GetDetailsAsync(runId, cancellationToken);
        return details is null
            ? FromError(OrchestrationErrorCodes.RunNotFound, "The orchestration run details were not found.")
            : ServiceResult<OrchestrationRunDetails>.QuerySuccess(details);
    }
    #endregion

    #region 处理（Output）
    /// <summary>
    /// 处理（Output）
    /// </summary>
    /// <param name="id">编排标识。</param>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含编排运行输出，失败时包含错误状态和提示。</returns>
    [HttpGet("{id:guid}/runs/{runId:guid}/output")]
    [Authorize(Policy = AgentAuthorizationPolicies.Debug)]
    public async Task<ActionResult<ServiceResult<OrchestrationRunOutputResponse>>> Output(Guid id, Guid runId, CancellationToken cancellationToken)
    {
        OrchestrationRunRecord? value = await runtime.GetAsync(runId, cancellationToken);
        if (value is null || value.OrchestrationId != id)
            return FromError(OrchestrationErrorCodes.RunNotFound, "The orchestration run was not found.");
        if (value.Status != OrchestrationRunStatus.Completed)
            return FromError(OrchestrationErrorCodes.RunNotFound, "The orchestration run has not completed.");
        OrchestrationRunDetails? details = await runtime.GetDetailsAsync(runId, cancellationToken);
        return details is null
            ? NoContent()
            : ServiceResult<OrchestrationRunOutputResponse>.QuerySuccess(
                new OrchestrationRunOutputResponse(details.Output, false));
    }
    #endregion

    #region 转换（FromServiceError）
    /// <summary>
    /// 转换（FromServiceError）
    /// </summary>
    /// <param name="result">操作结果。</param>
    /// <typeparam name="T">待处理数据的泛型类型。</typeparam>
    /// <returns>将编排服务状态转换为业务错误码后生成的统一失败响应。</returns>
    private JsonResult FromServiceError<T>(ServiceResult<T> result) =>
        FromError(
            OrchestrationServiceStatusCodes.ToErrorCode(result.Status),
            result.Message);
    #endregion

    #region 处理（OperationSuccess）
    /// <summary>
    /// 处理（OperationSuccess）
    /// </summary>
    /// <param name="value">要封装到成功响应中的业务数据。</param>
    /// <param name="httpStatus">需要写入响应的 HTTP 状态码。</param>
    /// <typeparam name="T">待处理数据的泛型类型。</typeparam>
    /// <returns>使用指定 HTTP 状态码并以 ServiceResult 包装数据的成功 JSON 响应。</returns>
    private JsonResult OperationSuccess<T>( T value, int httpStatus) =>
        new JsonResult(
            Success(value))
        {
            StatusCode = httpStatus
        };
    #endregion

    #region 转换（FromError）
    /// <summary>
    /// 转换（FromError）
    /// </summary>
    /// <param name="errorCode">操作失败对应的业务错误码。</param>
    /// <param name="message">消息或提示文本。</param>
    /// <returns>包含编排错误码和请求跟踪标识的失败响应，HTTP 状态由错误解析器确定，未指定时为 500。</returns>
    private JsonResult FromError(string errorCode, string message)
    {
        AgentApiErrorDescriptor descriptor = AgentApiErrorResolver.Resolve(HttpContext, errorCode);
        return new JsonResult(
            ServiceResult<AgentApiErrorData>.Failure(
                descriptor.Status,
                message,
                new AgentApiErrorData(errorCode, HttpContext.TraceIdentifier)))
        {
            StatusCode = descriptor.HttpStatus ?? StatusCodes.Status500InternalServerError
        };
    }
    #endregion
}

/// <summary>
/// 创建编排定义的请求。
/// </summary>
/// <param name="Code">业务唯一编码。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
public sealed record CreateOrchestrationRequest(string Code, string Name, string Description);

/// <summary>
/// 保存编排定义的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Status">当前状态。</param>
/// <param name="StartNodeId">编排入口节点标识。</param>
/// <param name="Nodes">编排节点集合。</param>
/// <param name="Edges">编排节点之间的连接集合。</param>
public sealed record SaveOrchestrationRequest(
    long ExpectedLogicalRevision,
    string Name,
    string Description,
    OrchestrationStatus Status,
    string StartNodeId,
    IReadOnlyList<OrchestrationNode> Nodes,
    IReadOnlyList<OrchestrationEdge> Edges);

/// <summary>
/// 发布编排版本的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
public sealed record PublishOrchestrationRequest(long ExpectedLogicalRevision);

/// <summary>
/// 设置编排归档状态的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Archived">是否设置为归档状态。</param>
public sealed record SetOrchestrationArchiveRequest(
    long ExpectedLogicalRevision,
    bool Archived);

/// <summary>
/// 启动编排运行的请求。
/// </summary>
/// <param name="Input">运行或评测使用的输入内容。</param>
public sealed record StartOrchestrationRunRequest(string Input);

/// <summary>
/// 取消编排运行的响应。
/// </summary>
/// <param name="RunId">运行标识。</param>
public sealed record OrchestrationRunCancelResponse(Guid RunId);

/// <summary>
/// 编排运行输出响应。
/// </summary>
/// <param name="Output">运行产生的输出内容。</param>
/// <param name="Ephemeral">输出是否只在当前响应中临时存在。</param>
public sealed record OrchestrationRunOutputResponse(string Output, bool Ephemeral);
