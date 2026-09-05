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

#region 文件职责：AgentTasksController 接口处理

/// <summary>
/// 提供可恢复 Agent 任务管理的 HTTP 接口。
/// </summary>
[Route("api/agent-tasks")]
public sealed class AgentTasksController(
    IAgAgentTaskServices tasks,
    ICallerContext caller,
    TimeProvider timeProvider,
    UnifiedEntryService unifiedEntry) : Base.ControllerBase
{
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

    private static bool HasUnknownProperties(Dictionary<string, JsonElement>? properties) => properties is { Count: > 0 };
    private JsonResult InvalidRequest() => FromError(AgentTaskErrorCodes.Invalid, "The Agent task request contains an unsupported property.");
    private JsonResult FromError(string errorCode, string message)
    {
        AgentApiErrorDescriptor descriptor = AgentApiErrorResolver.Resolve(HttpContext, errorCode);
        return new JsonResult(ServiceResult<AgentApiErrorData>.Failure(descriptor.Status, message,
            new AgentApiErrorData(errorCode, HttpContext.TraceIdentifier)))
        { StatusCode = descriptor.HttpStatus ?? StatusCodes.Status500InternalServerError };
    }
}

/// <summary>
/// Agent 任务详情响应。
/// </summary>
/// <param name="Task">任务主体记录。</param>
/// <param name="Attempts">任务执行尝试记录集合。</param>
/// <param name="Events">任务或运行事件集合。</param>
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

#endregion
