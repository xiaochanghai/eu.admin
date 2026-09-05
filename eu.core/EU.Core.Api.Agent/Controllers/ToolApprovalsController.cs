using System.Text.Json;
using System.Text.Json.Serialization;
using EU.Core.Api.Agent.Errors;
using EU.Core.Api.Agent.Security;
using EU.Core.IServices.Abstractions.Security;
using EU.Core.IServices.Approvals;
using EU.Core.IServices.Mcp;
using EU.Core.IServices;
using EU.Core.IServices.Tasks;
using EU.Core.IServices.UnifiedEntry;
using EU.Core.IServices.Runtime;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EU.Core.Services;

namespace EU.Core.Api.Agent.Controllers;


/// <summary>
/// 提供工具调用审批管理的 HTTP 接口。
/// </summary>
/// <param name="approvals">用于查询审批请求并处理审批决策的服务。</param>
/// <param name="caller">提供当前调用方身份、租户及权限的上下文。</param>
/// <param name="timeProvider">用于获取当前时间的时间提供器。</param>
[Route("api/tool-approvals")]
public sealed class ToolApprovalsController(
    IToolApprovalManagementService approvals,
    ICallerContext caller,
    TimeProvider timeProvider) : Base.ControllerBase
{
    #region 查询列表（List）
    /// <summary>
    /// 查询列表（List）
    /// </summary>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含工具审批请求记录集合，失败时包含错误状态和提示。</returns>
    [HttpGet]
    [Authorize(Policy = AgentAuthorizationPolicies.ApprovalRead)]
    public async Task<ActionResult<ServiceResult<IReadOnlyList<ToolApprovalRequestRecord>>>> List(
        [FromQuery] ToolApprovalStatus? status = null,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        if (take is < 1 or > ToolApprovalStateMachine.MaximumTake)
        {
            return FromError(
                ToolApprovalErrorCodes.Invalid,
                "The approval page size is invalid.");
        }

        IReadOnlyList<ToolApprovalRequestRecord> values = await approvals.ListAsync(
            caller.TenantId,
            status,
            take,
            cancellationToken);
        return ServiceResult<IReadOnlyList<ToolApprovalRequestRecord>>.QuerySuccess(values);
    }
    #endregion

    #region 获取（Get）
    /// <summary>
    /// 获取（Get）
    /// </summary>
    /// <param name="id">工具审批标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含工具审批及决策历史详情，失败时包含错误状态和提示。</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AgentAuthorizationPolicies.ApprovalRead)]
    public async Task<ActionResult<ServiceResult<ToolApprovalDetailResponse>>> Get(Guid id, CancellationToken cancellationToken)
    {
        ToolApprovalRequestRecord? approval = await approvals.GetAsync(
            id,
            caller.TenantId,
            cancellationToken);
        if (approval is null)
        {
            return NotFoundProblem();
        }

        return ServiceResult<ToolApprovalDetailResponse>.QuerySuccess(
            new ToolApprovalDetailResponse(
                approval,
                await approvals.ListDecisionsAsync(
                    id,
                    caller.TenantId,
                    cancellationToken)));
    }
    #endregion

    #region 处理（Approve）
    /// <summary>
    /// 处理（Approve）
    /// </summary>
    /// <param name="id">工具审批标识。</param>
    /// <param name="request">审批决策请求，包含决策原因；具体动作由当前接口指定。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含工具审批请求记录，失败时包含错误状态和提示。</returns>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = AgentAuthorizationPolicies.ApprovalDecide)]
    public Task<ActionResult<ServiceResult<ToolApprovalRequestRecord>>> Approve(
        Guid id,
        [FromBody] ToolApprovalDecisionApiRequest request,
        CancellationToken cancellationToken) =>
        Decide(id, request, ToolApprovalDecisionAction.Approve, cancellationToken);
    #endregion

    #region 处理（Reject）
    /// <summary>
    /// 处理（Reject）
    /// </summary>
    /// <param name="id">工具审批标识。</param>
    /// <param name="request">审批决策请求，包含决策原因；具体动作由当前接口指定。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含工具审批请求记录，失败时包含错误状态和提示。</returns>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = AgentAuthorizationPolicies.ApprovalDecide)]
    public Task<ActionResult<ServiceResult<ToolApprovalRequestRecord>>> Reject(
        Guid id,
        [FromBody] ToolApprovalDecisionApiRequest request,
        CancellationToken cancellationToken) =>
        Decide(id, request, ToolApprovalDecisionAction.Reject, cancellationToken);
    #endregion

    #region 取消（Cancel）
    /// <summary>
    /// 取消（Cancel）
    /// </summary>
    /// <param name="id">工具审批标识。</param>
    /// <param name="request">审批决策请求，包含决策原因；具体动作由当前接口指定。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含工具审批请求记录，失败时包含错误状态和提示。</returns>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = AgentAuthorizationPolicies.Chat)]
    public Task<ActionResult<ServiceResult<ToolApprovalRequestRecord>>> Cancel(
        Guid id,
        [FromBody] ToolApprovalDecisionApiRequest request,
        CancellationToken cancellationToken) =>
        Decide(id, request, ToolApprovalDecisionAction.Cancel, cancellationToken);
    #endregion

    #region 处理（Resume）
    /// <summary>
    /// 处理（Resume）
    /// </summary>
    /// <param name="id">工具审批标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含审批后的会话恢复结果，失败时包含错误状态和提示。</returns>
    [HttpPost("{id:guid}/resume")]
    [Authorize(Policy = AgentAuthorizationPolicies.Chat)]
    public async Task<ActionResult<ServiceResult<ToolApprovalConversationResumeResult>>> Resume(Guid id, CancellationToken cancellationToken)
    {
        ToolApprovalConversationResumeService? resumeService =
            HttpContext.RequestServices.GetService<
                ToolApprovalConversationResumeService>();
        if (resumeService is null)
        {
            return FromError(
                "TOOL_APPROVAL_DISABLED",
                "Tool approval execution is not enabled.");
        }

        try
        {
            ToolApprovalConversationResumeResult value = await resumeService.ResumeAsync(
                id,
                new AgentExecutionIdentity(
                    caller.UserId,
                    caller.TenantId,
                    caller.Permissions,
                    caller.CorrelationId),
                cancellationToken);
            AgentTaskStatus taskStatus = value.Status switch
            {
                UnifiedRunStatus.Completed => AgentTaskStatus.Completed,
                UnifiedRunStatus.Cancelled => AgentTaskStatus.Cancelled,
                _ => AgentTaskStatus.Failed
            };
            IAgAgentTaskServices? agentTasks =
                HttpContext.RequestServices.GetService<IAgAgentTaskServices>();
            if (agentTasks is not null)
            {
                await agentTasks.SynchronizeRunAsync(new SynchronizeAgentTaskRunCommand(
                    value.EntryRunId,
                    caller.TenantId,
                    caller.UserId,
                    taskStatus,
                    value.ErrorCode,
                    timeProvider.GetUtcNow()), cancellationToken);
            }
            return Success(value);
        }
        catch (ToolApprovalException exception)
        {
            return FromError(
                exception.ErrorCode,
                "The approved tool call could not be resumed.");
        }
    }
    #endregion

    #region 处理（Decide）
    /// <summary>
    /// 处理（Decide）
    /// </summary>
    /// <param name="id">工具审批标识。</param>
    /// <param name="request">审批决策请求，包含决策原因；具体动作由当前接口指定。</param>
    /// <param name="action">需要执行的操作委托。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含工具审批请求记录，失败时包含错误状态和提示。</returns>
    private async Task<ActionResult<ServiceResult<ToolApprovalRequestRecord>>> Decide(
        Guid id,
        ToolApprovalDecisionApiRequest request,
        ToolApprovalDecisionAction action,
        CancellationToken cancellationToken)
    {
        if (request.AdditionalProperties is { Count: > 0 })
        {
            return FromError(
                ToolApprovalErrorCodes.Invalid,
                "The approval decision contains an unsupported property.");
        }

        ToolApprovalRequestRecord? current = await approvals.GetAsync(
            id,
            caller.TenantId,
            cancellationToken);
        if (current is null)
        {
            return NotFoundProblem();
        }

        // TODO(agent-authorization): Re-enable this gate together with the Agent
        // permission policies when fine-grained authorization is introduced.
        // if (action == ToolApprovalDecisionAction.Approve
        //     && current.Risk == McpToolRisk.HighRisk
        //     && !HasPermission(
        //         AgentAuthorizationPolicies.ApprovalDecideHighRiskPermission))
        // {
        //     return FromError(
        //         "AUTHORIZATION_DENIED",
        //         "High-risk approval permission is required.");
        // }

        try
        {
            ToolApprovalRequestRecord decided = await approvals.DecideAsync(
                new ToolApprovalDecisionCommand(
                    id,
                    caller.TenantId,
                    caller.UserId,
                    action,
                    request.Reason ?? string.Empty,
                    timeProvider.GetUtcNow()),
                cancellationToken);
            return Success(decided);
        }
        catch (ToolApprovalException exception)
        {
            return FromError(
                exception.ErrorCode,
                "The approval decision could not be completed.");
        }
    }
    #endregion

    // TODO(agent-authorization): Re-enable with the high-risk approval gate above.
    // private bool HasPermission(string permission) =>
    //     caller.Permissions.Contains(permission, StringComparer.Ordinal)
    //     || caller.Permissions.Contains(
    //         AgentAuthorizationPolicies.AdminPermission,
    //         StringComparer.Ordinal);

    #region 处理（NotFoundProblem）
    /// <summary>
    /// 处理（NotFoundProblem）
    /// </summary>
    /// <returns>包含 TOOL_APPROVAL_NOT_FOUND 错误码的统一失败 JSON 响应。</returns>
    private JsonResult NotFoundProblem() =>
        FromError(
            "TOOL_APPROVAL_NOT_FOUND",
            "The tool approval was not found.");
    #endregion

    #region 转换（FromError）
    /// <summary>
    /// 转换（FromError）
    /// </summary>
    /// <param name="errorCode">操作失败对应的业务错误码。</param>
    /// <param name="message">消息或提示文本。</param>
    /// <returns>包含审批错误码和请求跟踪标识的失败响应，HTTP 状态由错误解析器确定，未指定时为 500。</returns>
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
/// 工具调用审批详情响应。
/// </summary>
/// <param name="Approval">工具调用审批主体记录。</param>
/// <param name="Decisions">审批决策历史集合。</param>
public sealed record ToolApprovalDetailResponse(
    ToolApprovalRequestRecord Approval,
    IReadOnlyList<ToolApprovalDecisionRecord> Decisions);

/// <summary>
/// 提交工具调用审批决策的接口输入。
/// </summary>
public sealed class ToolApprovalDecisionApiRequest
{
    /// <summary>
    /// 操作原因。
    /// </summary>
    public string? Reason { get; init; }

    [JsonExtensionData]
    /// <summary>
    /// 未识别的附加字段，用于严格输入校验。
    /// </summary>
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}
