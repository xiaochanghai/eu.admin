using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EU.Core.IServices.MainAgent;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EU.Core.Api.Agent.Security;
using EU.Core.IServices.Abstractions.Security;
using EU.Core.Api.Agent.Observability;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Errors;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.Services;

namespace EU.Core.Api.Agent.Controllers;

#region 文件职责：ChatRunsController 接口处理

[Route("api/chat")]
public sealed class ChatRunsController : Base.ControllerBase
{
    public const string RunIdHeaderName = "X-Agent-Run-ID";
    public const string ConversationIdHeaderName = "X-Agent-Conversation-ID";
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;
    public const int DefaultEventPageSize = 160;
    public const int MaximumEventPageSize = 500;

    private static readonly JsonSerializerOptions EventSerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly UnifiedEntryService _service;
    private readonly IUnifiedEntryRepository _repository;
    private readonly ICallerContext _caller;
    private readonly AgentMetrics? _metrics;

    public ChatRunsController(UnifiedEntryService service, IUnifiedEntryRepository repository, ICallerContext caller, AgentMetrics? metrics = null)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _caller = caller ?? throw new ArgumentNullException(nameof(caller));
        _metrics = metrics;
    }

    [HttpPost("runs")]
    [Authorize(Policy = AgentAuthorizationPolicies.Chat)]
    public async Task<IActionResult> Start([FromBody] StartChatRunRequest request, CancellationToken cancellationToken)
    {
        if (request.AdditionalProperties is { Count: > 0 })
        {
            return Error(
                StatusCodes.Status400BadRequest,
                ChatApiErrorCodes.UnknownProperty,
                "The chat run request contains an unsupported property.");
        }

        UnifiedEntryPreparationResult preparation = await _service.PrepareAsync(
            request.Input,
            request.ConversationId,
            new AgentExecutionIdentity(
                _caller.UserId,
                _caller.TenantId,
                _caller.Permissions,
                _caller.CorrelationId),
            cancellationToken);
        if (!preparation.Succeeded)
        {
            return FromPreparationError(preparation.Error!);
        }

        bool streamCompleted = false;
        bool streamPaused = false;
        try
        {
            Response.StatusCode = StatusCodes.Status200OK;
            Response.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache, no-store";
            Response.Headers.Append("X-Accel-Buffering", "no");
            Response.Headers[RunIdHeaderName] = preparation.Context!.RunId.ToString("D");
            Response.Headers[ConversationIdHeaderName] =
                preparation.Context.ConversationId.ToString("D");
            await Response.StartAsync(cancellationToken);

            await foreach (UnifiedRunEvent value in _service
                .StreamAsync(preparation.Context!, cancellationToken)
                .WithCancellation(cancellationToken))
            {
                if (value.Kind == "approval-required") streamPaused = true;
                string json = JsonSerializer.Serialize(
                    value,
                    EventSerializerOptions);
                await WriteFrameAsync(
                    value.Kind,
                    value.Sequence,
                    json,
                    cancellationToken);
            }

            streamCompleted = true;
            _metrics?.RecordResilience(streamPaused
                ? AgentResilienceEvent.ChatStreamPaused
                : AgentResilienceEvent.ChatStreamCompleted);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Disposing the async enumerator propagates disconnect cancellation
            // into the unified execution and performs its terminal cleanup.
            HttpContext.Items[AgentOperationAuditMiddleware.CancelledItemKey] = true;
            _metrics?.RecordResilience(
                AgentResilienceEvent.ChatStreamConsumerCancelled);
        }
        finally
        {
            if (!streamCompleted)
            {
                await _service.CancelAsync(
                    preparation.Context!.RunId,
                    CancellationToken.None);
            }
        }

        return new EmptyResult();
    }

    [HttpGet("conversations")]
    [Authorize(Policy = AgentAuthorizationPolicies.HistoryRead)]
    public async Task<ActionResult<ServiceResult<IReadOnlyList<ConversationRecord>>>> ListConversations(
        [FromQuery] int take = DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        await ExecutePersistenceOperationAsync<IReadOnlyList<ConversationRecord>>(
            async () => ServiceResult<IReadOnlyList<ConversationRecord>>.QuerySuccess(
                await _repository.ListConversationsForOwnerAsync(
                    _caller.TenantId,
                    _caller.UserId,
                    Bound(take),
                    cancellationToken)),
            cancellationToken);

    [HttpGet("conversations/{conversationId}")]
    [Authorize(Policy = AgentAuthorizationPolicies.HistoryRead)]
    public async Task<ActionResult<ServiceResult<ChatConversationDetailResponse>>> GetConversation(
        string conversationId,
        [FromQuery] int take = UnifiedEntryReadLimits.DefaultMessageTake,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseId(conversationId, out Guid id))
        {
            return InvalidId();
        }

        if (take is < 1 or > UnifiedEntryReadLimits.MaximumMessageTake)
        {
            return InvalidTake(
                UnifiedEntryReadLimits.DefaultMessageTake,
                UnifiedEntryReadLimits.MaximumMessageTake);
        }

        return await ExecutePersistenceOperationAsync<ChatConversationDetailResponse>(
            async () =>
            {
                ConversationRecord? conversation =
                    await _repository.GetConversationForOwnerAsync(
                        id, _caller.TenantId, _caller.UserId, cancellationToken);
                if (conversation is null)
                {
                    return ConversationNotFound();
                }

                IReadOnlyList<ConversationMessageRecord> messages =
                    await _repository.ListMessagesForOwnerAsync(
                        id,
                        _caller.TenantId,
                        _caller.UserId,
                        take,
                        cancellationToken);
                return ServiceResult<ChatConversationDetailResponse>.QuerySuccess(new ChatConversationDetailResponse(
                    conversation,
                    BusinessQueryResultProjector.ProjectMessages(
                        messages, IncludeBusinessPresentation())));
            },
            cancellationToken);
    }

    [HttpGet("conversations/{conversationId}/runs")]
    [Authorize(Policy = AgentAuthorizationPolicies.HistoryRead)]
    public async Task<ActionResult<ServiceResult<IReadOnlyList<UnifiedEntryRunRecord>>>> ListRuns(
        string conversationId,
        [FromQuery] int take = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseId(conversationId, out Guid id))
        {
            return InvalidId();
        }

        return await ExecutePersistenceOperationAsync<IReadOnlyList<UnifiedEntryRunRecord>>(
            async () =>
            {
                if (await _repository.GetConversationForOwnerAsync(
                        id,
                        _caller.TenantId,
                        _caller.UserId,
                        cancellationToken) is null)
                {
                    return ConversationNotFound();
                }

                return ServiceResult<IReadOnlyList<UnifiedEntryRunRecord>>.QuerySuccess(
                    await _repository.ListRunsForOwnerAsync(
                    id,
                    _caller.TenantId,
                    _caller.UserId,
                    Bound(take),
                    cancellationToken));
            },
            cancellationToken);
    }

    [HttpGet("runs/{runId}")]
    [Authorize(Policy = AgentAuthorizationPolicies.HistoryRead)]
    public async Task<ActionResult<ServiceResult<UnifiedEntryRunRecord>>> GetRun(string runId, CancellationToken cancellationToken)
    {
        if (!TryParseId(runId, out Guid id))
        {
            return InvalidId();
        }

        return await ExecutePersistenceOperationAsync<UnifiedEntryRunRecord>(
            async () =>
            {
                UnifiedEntryRunRecord? run =
                    await _repository.GetRunForOwnerAsync(
                        id, _caller.TenantId, _caller.UserId, cancellationToken);
                return run is null
                    ? RunNotFound()
                    : ServiceResult<UnifiedEntryRunRecord>.QuerySuccess(run);
            },
            cancellationToken);
    }

    [HttpGet("runs/{runId}/details")]
    [Authorize(Policy = AgentAuthorizationPolicies.HistoryRead)]
    public async Task<ActionResult<ServiceResult<UnifiedRunDetails>>> GetDetails(string runId, CancellationToken cancellationToken)
    {
        if (!TryParseId(runId, out Guid id))
        {
            return InvalidId();
        }

        return await ExecutePersistenceOperationAsync<UnifiedRunDetails>(
            async () =>
            {
                UnifiedRunDetails? details =
                    await _repository.GetDetailsForOwnerAsync(
                        id, _caller.TenantId, _caller.UserId, cancellationToken);
                return details is null
                    ? RunNotFound()
                    : ServiceResult<UnifiedRunDetails>.QuerySuccess(
                        BusinessQueryResultProjector.ProjectDetails(
                        details, IncludeBusinessPresentation()));
            },
            cancellationToken);
    }

    [HttpGet("runs/{runId}/events")]
    [Authorize(Policy = AgentAuthorizationPolicies.HistoryRead)]
    public async Task<ActionResult<ServiceResult<IReadOnlyList<UnifiedRunEventRecord>>>> GetEvents(
        string runId,
        [FromQuery] int take = DefaultEventPageSize,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseId(runId, out Guid id))
        {
            return InvalidId();
        }

        if (take is < 1 or > MaximumEventPageSize)
        {
            return InvalidTake(DefaultEventPageSize, MaximumEventPageSize);
        }

        return await ExecutePersistenceOperationAsync<IReadOnlyList<UnifiedRunEventRecord>>(
            async () =>
            {
                if (await _repository.GetRunForOwnerAsync(
                        id, _caller.TenantId, _caller.UserId, cancellationToken) is null)
                {
                    return RunNotFound();
                }

                IReadOnlyList<UnifiedRunEventRecord> events =
                    await _repository.ListEventsForOwnerAsync(
                        id, _caller.TenantId, _caller.UserId, cancellationToken);
                return ServiceResult<IReadOnlyList<UnifiedRunEventRecord>>.QuerySuccess(
                    BusinessQueryResultProjector.ProjectEvents(
                    events.TakeLast(take).ToArray(),
                    IncludeBusinessPresentation()));
            },
            cancellationToken);
    }

    [HttpPost("runs/{runId}/cancel")]
    [Authorize(Policy = AgentAuthorizationPolicies.Chat)]
    public async Task<ActionResult<ServiceResult<ChatRunCancelResponse>>> Cancel(string runId, CancellationToken cancellationToken)
    {
        if (!TryParseId(runId, out Guid id))
        {
            return InvalidId();
        }

        return await ExecutePersistenceOperationAsync<ChatRunCancelResponse>(
            async () =>
            {
                bool cancelled = await _service.CancelAsync(
                    id,
                    ExecutionIdentity(),
                    cancellationToken);
                return cancelled
                    ? OperationSuccess(new ChatRunCancelResponse(id), StatusCodes.Status202Accepted)
                    : RunNotFound();
            },
            cancellationToken);
    }

    private async Task WriteFrameAsync(string eventName, long sequence, string json, CancellationToken cancellationToken)
    {
        string frame = $"id: {sequence}\nevent: {eventName}\ndata: {json}\n\n";
        await Response.Body.WriteAsync(
            Encoding.UTF8.GetBytes(frame),
            cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    private bool IncludeBusinessPresentation()
    {
        bool auditReader = _caller.Permissions.Contains(
            AgentAuthorizationPolicies.AuditReadPermission,
            StringComparer.Ordinal);
        bool chatUser = _caller.Permissions.Contains(
            AgentAuthorizationPolicies.ChatPermission,
            StringComparer.Ordinal);
        bool businessReader = _caller.Permissions.Contains(
            AgentAuthorizationPolicies.BusinessDataReadPermission,
            StringComparer.Ordinal);
        return !auditReader || chatUser || businessReader;
    }

    private JsonResult FromPreparationError(UnifiedEntryError error)
    {
        int status = error.Code switch
        {
            MainAgentErrorCodes.NotConfigured
                or MainAgentErrorCodes.AgentNotFound
                or AgentRunErrorCodes.AgentNotFound
                or UnifiedEntryErrorCodes.ConversationNotFound =>
                StatusCodes.Status404NotFound,
            MainAgentErrorCodes.AgentDisabled
                or MainAgentErrorCodes.VersionMissing
                or AgentRunErrorCodes.AgentDisabled
                or AgentRunErrorCodes.VersionMissing
                or AgentRunErrorCodes.KnowledgeRevisionStale
                or AgentRunErrorCodes.KnowledgeBindingUnavailable =>
                StatusCodes.Status409Conflict,
            UnifiedEntryErrorCodes.PersistenceFailed =>
                StatusCodes.Status500InternalServerError,
            AgentRunErrorCodes.KnowledgeServiceUnavailable =>
                StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status400BadRequest
        };
        return Error(
            status,
            error.Code,
            "The chat run could not be started.",
            error.Message);
    }

    private JsonResult InvalidId() =>
        Error(
            StatusCodes.Status400BadRequest,
            ChatApiErrorCodes.InvalidId,
            "The requested identifier is invalid.");

    private JsonResult InvalidTake(int defaultTake, int maximumTake) =>
        Error(
            StatusCodes.Status400BadRequest,
            ChatApiErrorCodes.InvalidTake,
            "The requested page size is invalid.",
            $"The take value must be from 1 through {maximumTake}; the default is {defaultTake}.");

    private JsonResult ConversationNotFound() =>
        Error(
            StatusCodes.Status404NotFound,
            UnifiedEntryErrorCodes.ConversationNotFound,
            "The conversation was not found.");

    private JsonResult RunNotFound() =>
        Error(
            StatusCodes.Status404NotFound,
            ChatApiErrorCodes.RunNotFound,
            "The chat run was not found.");

    private JsonResult Error(int status, string code, string title, string? detail = null)
    {
        AgentApiErrorDescriptor descriptor = AgentApiErrorResolver.Resolve(HttpContext, code);
        string message = string.IsNullOrWhiteSpace(detail) ? title : detail;
        return new JsonResult(
            ServiceResult<AgentApiErrorData>.Failure(
                descriptor.Status,
                message,
                new AgentApiErrorData(code, HttpContext.TraceIdentifier)))
        { StatusCode = descriptor.HttpStatus ?? status };
    }

    private JsonResult OperationSuccess<T>(T value, int httpStatus) => new JsonResult( Success(value))
    { StatusCode = httpStatus };

    private async Task<ActionResult<ServiceResult<T>>> ExecutePersistenceOperationAsync<T>(
        Func<Task<ActionResult<ServiceResult<T>>>> operation,
        CancellationToken cancellationToken)
    {
        try
        {
            return await operation();
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (UnifiedEntryException exception)
        {
            int status = exception.ErrorCode switch
            {
                UnifiedEntryErrorCodes.PersistenceFailed =>
                    StatusCodes.Status500InternalServerError,
                UnifiedEntryErrorCodes.InvalidState =>
                    StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };
            return Error(
                status,
                exception.ErrorCode,
                "The chat operation could not be completed.");
        }
        catch (Exception)
        {
            return Error(
                StatusCodes.Status500InternalServerError,
                UnifiedEntryErrorCodes.PersistenceFailed,
                "The chat operation could not be completed.");
        }
    }

    private static int Bound(int take) =>
        Math.Clamp(take, 1, MaximumPageSize);

    private static bool TryParseId(string value, out Guid id) =>
        Guid.TryParseExact(value, "D", out id) && id != Guid.Empty;

    private AgentExecutionIdentity ExecutionIdentity() => new(
        _caller.UserId,
        _caller.TenantId,
        _caller.Permissions,
        _caller.CorrelationId);
}

public static class ChatApiErrorCodes
{
    public const string UnknownProperty = "REQUEST_UNKNOWN_PROPERTY";
    public const string InvalidId = "REQUEST_INVALID_ID";
    public const string InvalidTake = "REQUEST_INVALID_TAKE";
    public const string RunNotFound = "UNIFIED_ENTRY_RUN_NOT_FOUND";
}

public sealed record ChatRunCancelResponse(Guid RunId);

public sealed record ChatConversationDetailResponse(
    ConversationRecord Conversation,
    IReadOnlyList<ConversationMessageRecord> Messages);

public sealed record StartChatRunRequest(
    string? Input,
    Guid? ConversationId)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

#endregion
