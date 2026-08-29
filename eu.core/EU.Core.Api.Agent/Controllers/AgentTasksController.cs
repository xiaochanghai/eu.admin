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

public sealed record AgentTaskDetailResponse(
    AgentTaskRecord Task,
    IReadOnlyList<AgentTaskAttemptRecord> Attempts,
    IReadOnlyList<AgentTaskEventRecord> Events);

public abstract class AgentTaskApiRequest
{
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed class CreateAgentTaskApiRequest : AgentTaskApiRequest
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Input { get; init; }
    public string? SourceType { get; init; }
    public string? SourceId { get; init; }
    public string? IdempotencyKey { get; init; }
    public Guid? ConversationId { get; init; }
    public int? Priority { get; init; }
    public int? MaximumAttempts { get; init; }
    public DateTimeOffset? AvailableAtUtc { get; init; }
}

public sealed class ClaimAgentTaskApiRequest : AgentTaskApiRequest
{
    public string? WorkerId { get; init; }
    public int? LeaseSeconds { get; init; }
}

public sealed class AgentTaskCheckpointApiRequest : AgentTaskApiRequest
{
    public string? WorkerId { get; init; }
    public long ExpectedLogicalRevision { get; init; }
    public Guid? RunId { get; init; }
    public Guid? ConversationId { get; init; }
    public string? CheckpointKind { get; init; }
    public string? CheckpointJson { get; init; }
}

public sealed class RenewAgentTaskLeaseApiRequest : AgentTaskApiRequest
{
    public string? WorkerId { get; init; }
    public long ExpectedLogicalRevision { get; init; }
    public int? LeaseSeconds { get; init; }
}

public sealed class CompleteAgentTaskApiRequest : AgentTaskApiRequest
{
    public string? WorkerId { get; init; }
    public long ExpectedLogicalRevision { get; init; }
    public Guid? RunId { get; init; }
}

public sealed class FailAgentTaskApiRequest : AgentTaskApiRequest
{
    public string? WorkerId { get; init; }
    public long ExpectedLogicalRevision { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public int? RetryDelaySeconds { get; init; }
}

public sealed class ResumeAgentTaskApiRequest : AgentTaskApiRequest
{
    public long ExpectedLogicalRevision { get; init; }
    public string? Input { get; init; }
}
