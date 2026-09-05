using System.Text.Json;
using System.Text.Json.Serialization;
using EU.Core.Api.Agent.Errors;
using EU.Core.Api.Agent.Security;
using EU.Core.IServices;
using EU.Core.IServices.Abstractions.Security;
using EU.Core.IServices.Tasks;
using EU.Core.IServices.Runtime;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EU.Core.Services;

namespace EU.Core.Api.Agent.Controllers;

// 文件职责：AgentTasksController 接口处理

/// <summary>
/// 提供可恢复 Agent 任务管理的 HTTP 接口。
/// </summary>
/// <param name="tasks">用于创建和管理可恢复 Agent 任务的服务。</param>
/// <param name="caller">提供当前调用方身份、租户及权限的上下文。</param>
/// <param name="timeProvider">用于获取当前时间的时间提供器。</param>
/// <param name="unifiedEntry">用于准备和执行统一入口运行的服务。</param>
[Route("api/agent-tasks")]
public sealed class AgentTasksController(
    IAgAgentTaskServices tasks,
    ICallerContext caller,
    TimeProvider timeProvider,
    UnifiedEntryService unifiedEntry) : Base.ControllerBase
{
    #region 创建（Create）
    /// <summary>
    /// 创建（Create）
    /// </summary>
    /// <param name="request">创建Agent 任务所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 任务记录，失败时包含错误状态和提示。</returns>
    [HttpPost]
    [Authorize(Policy = AgentAuthorizationPolicies.Chat)]
    public async Task<ActionResult<ServiceResult<AgentTaskRecord>>> Create([FromBody] CreateAgentTaskApiRequest request, CancellationToken cancellationToken)
    {
        if (HasUnknownProperties(request.AdditionalProperties)) return InvalidRequest();
        string sourceType = string.IsNullOrWhiteSpace(request.SourceType) ? "chat" : request.SourceType.Trim();
        if (!string.Equals(sourceType, "chat", StringComparison.OrdinalIgnoreCase))
            return FromError(AgentTaskErrorCodes.Invalid, "The Agent task source type is not supported.");

        try
        {
            AgentTaskRecord value = await tasks.CreateAsync(new CreateAgentTaskCommand(
                caller.TenantId, caller.UserId, request.Title ?? string.Empty,
                request.Description ?? string.Empty, request.Input ?? string.Empty,
                "chat",
                request.SourceId ?? string.Empty, request.IdempotencyKey ?? string.Empty,
                request.ConversationId, request.Priority ?? 0, request.MaximumAttempts ?? 3,
                request.AvailableAtUtc ?? timeProvider.GetUtcNow()), cancellationToken);
            return Success(value);
        }
        catch (AgentTaskException exception) { return FromError(exception.ErrorCode, exception.Message); }
    }
    #endregion

    #region 查询列表（List）
    /// <summary>
    /// 查询列表（List）
    /// </summary>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 任务记录集合，失败时包含错误状态和提示。</returns>
    [HttpGet]
    [Authorize(Policy = AgentAuthorizationPolicies.HistoryRead)]
    public async Task<ActionResult<ServiceResult<IReadOnlyList<AgentTaskRecord>>>> List([FromQuery] AgentTaskStatus? status = null, [FromQuery] int take = 40, CancellationToken cancellationToken = default)
    {
        try
        {
            return ServiceResult<IReadOnlyList<AgentTaskRecord>>.QuerySuccess(
                await tasks.ListAsync(new AgentTaskQuery(caller.TenantId, caller.UserId, status, take), cancellationToken));
        }
        catch (AgentTaskException exception) { return FromError(exception.ErrorCode, exception.Message); }
    }
    #endregion

    #region 获取（Get）
    /// <summary>
    /// 获取（Get）
    /// </summary>
    /// <param name="id">Agent 任务标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含任务及执行尝试、事件详情，失败时包含错误状态和提示。</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AgentAuthorizationPolicies.HistoryRead)]
    public async Task<ActionResult<ServiceResult<AgentTaskDetailResponse>>> Get(Guid id, CancellationToken cancellationToken)
    {
        AgentTaskRecord? task = await tasks.GetAsync(id, caller.TenantId, caller.UserId, cancellationToken);
        if (task is null) return FromError(AgentTaskErrorCodes.NotFound, "The Agent task was not found.");
        return ServiceResult<AgentTaskDetailResponse>.QuerySuccess(new AgentTaskDetailResponse(
            task,
            await tasks.ListAttemptsAsync(id, caller.TenantId, caller.UserId, cancellationToken),
            await tasks.ListEventsAsync(id, caller.TenantId, caller.UserId, cancellationToken: cancellationToken)));
    }
    #endregion

    #region 处理（ClaimNext）
    /// <summary>
    /// 处理（ClaimNext）
    /// </summary>
    /// <param name="request">认领Agent 任务所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含认领的任务；没有可认领任务时数据为 null，校验失败时返回任务错误响应。</returns>
    [HttpPost("claim-next")]
    [Authorize(Policy = AgentAuthorizationPolicies.Debug)]
    public async Task<ActionResult<ServiceResult<AgentTaskRecord?>>> ClaimNext([FromBody] ClaimAgentTaskApiRequest request, CancellationToken cancellationToken)
    {
        if (HasUnknownProperties(request.AdditionalProperties)) return InvalidRequest();
        try
        {
            AgentTaskRecord? task = await tasks.TryClaimNextAsync(new ClaimAgentTaskCommand(
                caller.TenantId, request.WorkerId ?? string.Empty,
                TimeSpan.FromSeconds(request.LeaseSeconds ?? 300), timeProvider.GetUtcNow()), cancellationToken);
            return ServiceResult<AgentTaskRecord?>.QuerySuccess(task);
        }
        catch (AgentTaskException exception) { return FromError(exception.ErrorCode, exception.Message); }
    }
    #endregion

    #region 处理（Checkpoint）
    /// <summary>
    /// 处理（Checkpoint）
    /// </summary>
    /// <param name="id">Agent 任务标识。</param>
    /// <param name="request">保存检查点Agent 任务所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 任务记录，失败时包含错误状态和提示。</returns>
    [HttpPost("{id:guid}/checkpoint")]
    [Authorize(Policy = AgentAuthorizationPolicies.Debug)]
    public async Task<ActionResult<ServiceResult<AgentTaskRecord>>> Checkpoint(Guid id, [FromBody] AgentTaskCheckpointApiRequest request, CancellationToken cancellationToken)
    {
        if (HasUnknownProperties(request.AdditionalProperties)) return InvalidRequest();
        try
        {
            return Success(await tasks.SaveCheckpointAsync(new SaveAgentTaskCheckpointCommand(
                id, caller.TenantId, request.WorkerId ?? string.Empty, request.ExpectedLogicalRevision,
                request.RunId, request.ConversationId, request.CheckpointKind ?? string.Empty, request.CheckpointJson ?? string.Empty,
                timeProvider.GetUtcNow()), cancellationToken));
        }
        catch (AgentTaskException exception) { return FromError(exception.ErrorCode, exception.Message); }
    }
    #endregion

    #region 处理（RenewLease）
    /// <summary>
    /// 处理（RenewLease）
    /// </summary>
    /// <param name="id">Agent 任务标识。</param>
    /// <param name="request">续租Agent 任务所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 任务记录，失败时包含错误状态和提示。</returns>
    [HttpPost("{id:guid}/renew-lease")]
    [Authorize(Policy = AgentAuthorizationPolicies.Debug)]
    public async Task<ActionResult<ServiceResult<AgentTaskRecord>>> RenewLease(Guid id, [FromBody] RenewAgentTaskLeaseApiRequest request, CancellationToken cancellationToken)
    {
        if (HasUnknownProperties(request.AdditionalProperties)) return InvalidRequest();
        try
        {
            return Success(await tasks.RenewLeaseAsync(new RenewAgentTaskLeaseCommand(
                id, caller.TenantId, request.WorkerId ?? string.Empty,
                request.ExpectedLogicalRevision, TimeSpan.FromSeconds(request.LeaseSeconds ?? 300),
                timeProvider.GetUtcNow()), cancellationToken));
        }
        catch (AgentTaskException exception) { return FromError(exception.ErrorCode, exception.Message); }
    }
    #endregion

    #region 处理（Complete）
    /// <summary>
    /// 处理（Complete）
    /// </summary>
    /// <param name="id">Agent 任务标识。</param>
    /// <param name="request">完成Agent 任务所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 任务记录，失败时包含错误状态和提示。</returns>
    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = AgentAuthorizationPolicies.Debug)]
    public async Task<ActionResult<ServiceResult<AgentTaskRecord>>> Complete(Guid id, [FromBody] CompleteAgentTaskApiRequest request, CancellationToken cancellationToken)
    {
        if (HasUnknownProperties(request.AdditionalProperties)) return InvalidRequest();
        try
        {
            return Success(await tasks.CompleteAsync(new CompleteAgentTaskCommand(
                id, caller.TenantId, request.WorkerId ?? string.Empty,
                request.ExpectedLogicalRevision, request.RunId, timeProvider.GetUtcNow()), cancellationToken));
        }
        catch (AgentTaskException exception) { return FromError(exception.ErrorCode, exception.Message); }
    }
    #endregion

    #region 处理（Fail）
    /// <summary>
    /// 处理（Fail）
    /// </summary>
    /// <param name="id">Agent 任务标识。</param>
    /// <param name="request">记录失败Agent 任务所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 任务记录，失败时包含错误状态和提示。</returns>
    [HttpPost("{id:guid}/fail")]
    [Authorize(Policy = AgentAuthorizationPolicies.Debug)]
    public async Task<ActionResult<ServiceResult<AgentTaskRecord>>> Fail(Guid id, [FromBody] FailAgentTaskApiRequest request, CancellationToken cancellationToken)
    {
        if (HasUnknownProperties(request.AdditionalProperties)) return InvalidRequest();
        try
        {
            return Success(await tasks.FailAsync(new FailAgentTaskCommand(
                id, caller.TenantId, request.WorkerId ?? string.Empty, request.ExpectedLogicalRevision,
                request.ErrorCode ?? string.Empty, request.ErrorMessage ?? string.Empty,
                TimeSpan.FromSeconds(request.RetryDelaySeconds ?? 30), timeProvider.GetUtcNow()), cancellationToken));
        }
        catch (AgentTaskException exception) { return FromError(exception.ErrorCode, exception.Message); }
    }
    #endregion

    #region 取消（Cancel）
    /// <summary>
    /// 取消（Cancel）
    /// </summary>
    /// <param name="id">Agent 任务标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 任务记录，失败时包含错误状态和提示。</returns>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = AgentAuthorizationPolicies.Chat)]
    public async Task<ActionResult<ServiceResult<AgentTaskRecord>>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            AgentTaskRecord cancelled = await tasks.CancelAsync(
                id, caller.TenantId, caller.UserId, timeProvider.GetUtcNow(), cancellationToken);
            if (cancelled.CurrentRunId.HasValue)
            {
                await unifiedEntry.CancelAsync(cancelled.CurrentRunId.Value,
                    new AgentExecutionIdentity(caller.UserId, caller.TenantId, caller.Permissions, caller.CorrelationId),
                    cancellationToken);
            }

            return Success(cancelled);
        }
        catch (AgentTaskException exception) { return FromError(exception.ErrorCode, exception.Message); }
    }
    #endregion

    #region 处理（ResumeWithUserInput）
    /// <summary>
    /// 处理（ResumeWithUserInput）
    /// </summary>
    /// <param name="id">Agent 任务标识。</param>
    /// <param name="request">补充用户输入并恢复Agent 任务所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 任务记录，失败时包含错误状态和提示。</returns>
    [HttpPost("{id:guid}/user-input")]
    [Authorize(Policy = AgentAuthorizationPolicies.Chat)]
    public async Task<ActionResult<ServiceResult<AgentTaskRecord>>> ResumeWithUserInput(
        Guid id,
        [FromBody] ResumeAgentTaskApiRequest request,
        CancellationToken cancellationToken)
    {
        if (HasUnknownProperties(request.AdditionalProperties)) return InvalidRequest();
        try
        {
            return Success(await tasks.ResumeWithUserInputAsync(
                new ResumeAgentTaskWithUserInputCommand(
                    id, caller.TenantId, caller.UserId, request.ExpectedLogicalRevision,
                    request.Input ?? string.Empty, timeProvider.GetUtcNow()), cancellationToken));
        }
        catch (AgentTaskException exception) { return FromError(exception.ErrorCode, exception.Message); }
    }
    #endregion

    #region 检查未知请求属性（HasUnknownProperties）
    /// <summary>
    /// 检查请求是否包含契约之外的 JSON 属性（HasUnknownProperties）。
    /// </summary>
    /// <param name="properties">请求中捕获的扩展 JSON 属性。</param>
    /// <returns>存在扩展属性时返回 true；集合为 null 或为空时返回 false。</returns>
    private static bool HasUnknownProperties(Dictionary<string, JsonElement>? properties) => properties is { Count: > 0 };
    #endregion
    #region 处理（InvalidRequest）
    /// <summary>
    /// 处理（InvalidRequest）
    /// </summary>
    /// <returns>表示请求包含不支持属性的任务错误 JSON 响应。</returns>
    private JsonResult InvalidRequest() => FromError(AgentTaskErrorCodes.Invalid, "The Agent task request contains an unsupported property.");
    #endregion
    #region 转换（FromError）
    /// <summary>
    /// 转换（FromError）
    /// </summary>
    /// <param name="errorCode">操作失败对应的业务错误码。</param>
    /// <param name="message">消息或提示文本。</param>
    /// <returns>包含任务错误码和请求跟踪标识的失败响应，HTTP 状态由错误解析器确定，未指定时为 500。</returns>
    private JsonResult FromError(string errorCode, string message)
    {
        AgentApiErrorDescriptor descriptor = AgentApiErrorResolver.Resolve(HttpContext, errorCode);
        return new JsonResult(ServiceResult<AgentApiErrorData>.Failure(descriptor.Status, message,
            new AgentApiErrorData(errorCode, HttpContext.TraceIdentifier)))
        { StatusCode = descriptor.HttpStatus ?? StatusCodes.Status500InternalServerError };
    }
    #endregion
}

/// <summary>
/// Agent 任务详情响应。
/// </summary>
/// <param name="Task">任务主体记录。</param>
/// <param name="Attempts">任务执行尝试记录集合。</param>
/// <param name="Events">任务或运行事件集合。</param>
public sealed record AgentTaskDetailResponse(
    AgentTaskRecord Task,
    IReadOnlyList<AgentTaskAttemptRecord> Attempts,
    IReadOnlyList<AgentTaskEventRecord> Events);

/// <summary>
/// Agent 任务请求的公共输入基类。
/// </summary>
public abstract class AgentTaskApiRequest
{
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

/// <summary>
/// 创建 Agent 任务的接口输入。
/// </summary>
public sealed class CreateAgentTaskApiRequest : AgentTaskApiRequest
{
    /// <summary>
    /// 任务标题。
    /// </summary>
    public string? Title { get; init; }
    /// <summary>
    /// 说明文本。
    /// </summary>
    public string? Description { get; init; }
    /// <summary>
    /// 任务或运行输入。
    /// </summary>
    public string? Input { get; init; }
    /// <summary>
    /// 任务来源类型。
    /// </summary>
    public string? SourceType { get; init; }
    /// <summary>
    /// 任务来源标识。
    /// </summary>
    public string? SourceId { get; init; }
    /// <summary>
    /// 请求幂等键。
    /// </summary>
    public string? IdempotencyKey { get; init; }
    /// <summary>
    /// 会话标识。
    /// </summary>
    public Guid? ConversationId { get; init; }
    /// <summary>
    /// 任务优先级。
    /// </summary>
    public int? Priority { get; init; }
    /// <summary>
    /// 任务允许的最大执行次数。
    /// </summary>
    public int? MaximumAttempts { get; init; }
    /// <summary>
    /// 任务允许被领取的 UTC 时间。
    /// </summary>
    public DateTimeOffset? AvailableAtUtc { get; init; }
}

/// <summary>
/// 认领 Agent 任务的接口输入。
/// </summary>
public sealed class ClaimAgentTaskApiRequest : AgentTaskApiRequest
{
    /// <summary>
    /// 后台任务执行器标识。
    /// </summary>
    public string? WorkerId { get; init; }
    /// <summary>
    /// 任务租约时长，单位为秒。
    /// </summary>
    public int? LeaseSeconds { get; init; }
}

/// <summary>
/// 保存 Agent 任务检查点的接口输入。
/// </summary>
public sealed class AgentTaskCheckpointApiRequest : AgentTaskApiRequest
{
    /// <summary>
    /// 后台任务执行器标识。
    /// </summary>
    public string? WorkerId { get; init; }
    /// <summary>
    /// 用于并发控制的预期逻辑版本。
    /// </summary>
    public long ExpectedLogicalRevision { get; init; }
    /// <summary>
    /// 运行标识。
    /// </summary>
    public Guid? RunId { get; init; }
    /// <summary>
    /// 会话标识。
    /// </summary>
    public Guid? ConversationId { get; init; }
    /// <summary>
    /// 任务检查点类型。
    /// </summary>
    public string? CheckpointKind { get; init; }
    /// <summary>
    /// 任务检查点数据 JSON。
    /// </summary>
    public string? CheckpointJson { get; init; }
}

/// <summary>
/// 续订 Agent 任务租约的接口输入。
/// </summary>
public sealed class RenewAgentTaskLeaseApiRequest : AgentTaskApiRequest
{
    /// <summary>
    /// 后台任务执行器标识。
    /// </summary>
    public string? WorkerId { get; init; }
    /// <summary>
    /// 用于并发控制的预期逻辑版本。
    /// </summary>
    public long ExpectedLogicalRevision { get; init; }
    /// <summary>
    /// 任务租约时长，单位为秒。
    /// </summary>
    public int? LeaseSeconds { get; init; }
}

/// <summary>
/// 完成 Agent 任务的接口输入。
/// </summary>
public sealed class CompleteAgentTaskApiRequest : AgentTaskApiRequest
{
    /// <summary>
    /// 后台任务执行器标识。
    /// </summary>
    public string? WorkerId { get; init; }
    /// <summary>
    /// 用于并发控制的预期逻辑版本。
    /// </summary>
    public long ExpectedLogicalRevision { get; init; }
    /// <summary>
    /// 运行标识。
    /// </summary>
    public Guid? RunId { get; init; }
}

/// <summary>
/// 记录 Agent 任务失败的接口输入。
/// </summary>
public sealed class FailAgentTaskApiRequest : AgentTaskApiRequest
{
    /// <summary>
    /// 后台任务执行器标识。
    /// </summary>
    public string? WorkerId { get; init; }
    /// <summary>
    /// 用于并发控制的预期逻辑版本。
    /// </summary>
    public long ExpectedLogicalRevision { get; init; }
    /// <summary>
    /// 错误代码。
    /// </summary>
    public string? ErrorCode { get; init; }
    /// <summary>
    /// 错误说明。
    /// </summary>
    public string? ErrorMessage { get; init; }
    /// <summary>
    /// 失败后的重试延迟，单位为秒。
    /// </summary>
    public int? RetryDelaySeconds { get; init; }
}

/// <summary>
/// 使用用户输入恢复 Agent 任务的接口输入。
/// </summary>
public sealed class ResumeAgentTaskApiRequest : AgentTaskApiRequest
{
    /// <summary>
    /// 用于并发控制的预期逻辑版本。
    /// </summary>
    public long ExpectedLogicalRevision { get; init; }
    /// <summary>
    /// 任务或运行输入。
    /// </summary>
    public string? Input { get; init; }
}
