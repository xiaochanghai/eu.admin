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

// 文件职责：ChatRunsController 接口处理

/// <summary>
/// 提供统一会话及聊天运行的 HTTP 接口。
/// </summary>
[Route("api/chat")]
public sealed class ChatRunsController : Base.ControllerBase
{
    /// <summary>返回运行标识使用的 HTTP 响应头名称。</summary>
    public const string RunIdHeaderName = "X-Agent-Run-ID";
    /// <summary>返回会话标识使用的 HTTP 响应头名称。</summary>
    public const string ConversationIdHeaderName = "X-Agent-Conversation-ID";
    /// <summary>运行事件查询的默认分页大小。</summary>
    public const int DefaultPageSize = 20;
    /// <summary>运行事件查询允许的最大分页大小。</summary>
    public const int MaximumPageSize = 100;
    /// <summary>流式事件重放的默认分页大小。</summary>
    public const int DefaultEventPageSize = 160;
    /// <summary>流式事件重放允许的最大分页大小。</summary>
    public const int MaximumEventPageSize = 500;

    private static readonly JsonSerializerOptions EventSerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly UnifiedEntryService _service;
    private readonly IUnifiedEntryRepository _repository;
    private readonly ICallerContext _caller;
    private readonly AgentMetrics? _metrics;

    #region 构造（ChatRunsController）
    /// <summary>
    /// 构造（ChatRunsController）
    /// </summary>
    /// <param name="service">当前接口依赖的业务服务。</param>
    /// <param name="repository">当前操作使用的持久化仓储。</param>
    /// <param name="caller">当前请求的用户、租户及权限上下文。</param>
    /// <param name="metrics">运行指标采集器。</param>
    public ChatRunsController(UnifiedEntryService service, IUnifiedEntryRepository repository, ICallerContext caller, AgentMetrics? metrics = null)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _caller = caller ?? throw new ArgumentNullException(nameof(caller));
        _metrics = metrics;
    }
    #endregion

    #region 处理（Start）
    /// <summary>
    /// 处理（Start）
    /// </summary>
    /// <param name="request">待启动的会话运行请求，包含输入文本及可选会话标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示 SSE 事件流写入完成的异步任务，事件直接写入 HTTP 响应。</returns>
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
    #endregion

    #region 查询列表（ListConversations）
    /// <summary>
    /// 查询列表（ListConversations）
    /// </summary>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含会话记录集合，失败时包含错误状态和提示。</returns>
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
    #endregion

    #region 获取（GetConversation）
    /// <summary>
    /// 获取（GetConversation）
    /// </summary>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含会话及消息详情，失败时包含错误状态和提示。</returns>
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
    #endregion

    #region 查询列表（ListRuns）
    /// <summary>
    /// 查询列表（ListRuns）
    /// </summary>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含统一入口运行记录集合，失败时包含错误状态和提示。</returns>
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
    #endregion

    #region 获取（GetRun）
    /// <summary>
    /// 获取（GetRun）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含统一入口运行记录，失败时包含错误状态和提示。</returns>
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
    #endregion

    #region 获取（GetDetails）
    /// <summary>
    /// 获取（GetDetails）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含统一入口运行详情，失败时包含错误状态和提示。</returns>
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
    #endregion

    #region 获取（GetEvents）
    /// <summary>
    /// 获取（GetEvents）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含统一运行事件集合，失败时包含错误状态和提示。</returns>
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
    #endregion

    #region 取消（Cancel）
    /// <summary>
    /// 取消（Cancel）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含会话运行取消结果，失败时包含错误状态和提示。</returns>
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
    #endregion

    #region 写入（WriteFrameAsync）
    /// <summary>
    /// 写入（WriteFrameAsync）
    /// </summary>
    /// <param name="eventName">SSE 事件名称。</param>
    /// <param name="sequence">事件或记录序号。</param>
    /// <param name="json">写入 SSE data 字段的已序列化 JSON 载荷。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示操作完成的异步任务。</returns>
    private async Task WriteFrameAsync(string eventName, long sequence, string json, CancellationToken cancellationToken)
    {
        string frame = $"id: {sequence}\nevent: {eventName}\ndata: {json}\n\n";
        await Response.Body.WriteAsync(
            Encoding.UTF8.GetBytes(frame),
            cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
    #endregion

    #region 判断是否包含业务展示内容（IncludeBusinessPresentation）
    /// <summary>
    /// 根据调用方权限判断响应是否包含业务展示内容（IncludeBusinessPresentation）。
    /// </summary>
    /// <returns>调用方具有审计读取权限，但既无聊天权限也无业务数据读取权限时返回 false；其余情况返回 true。</returns>
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
    #endregion

    #region 转换（FromPreparationError）
    /// <summary>
    /// 转换（FromPreparationError）
    /// </summary>
    /// <param name="error">错误信息。</param>
    /// <returns>包含准备失败详情的统一错误响应；错误解析器未指定 HTTP 状态时使用准备错误分类的默认值。</returns>
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
    #endregion

    #region 处理（InvalidId）
    /// <summary>
    /// 处理（InvalidId）
    /// </summary>
    /// <returns>包含 InvalidId 错误码的失败 JSON 响应，默认 HTTP 状态为 400。</returns>
    private JsonResult InvalidId() =>
        Error(
            StatusCodes.Status400BadRequest,
            ChatApiErrorCodes.InvalidId,
            "The requested identifier is invalid.");
    #endregion

    #region 处理（InvalidTake）
    /// <summary>
    /// 处理（InvalidTake）
    /// </summary>
    /// <param name="defaultTake">未指定查询数量时使用的默认值。</param>
    /// <param name="maximumTake">单次查询允许返回的最大记录数。</param>
    /// <returns>包含 InvalidTake 错误码、有效取值范围及默认数量的失败 JSON 响应，默认 HTTP 状态为 400。</returns>
    private JsonResult InvalidTake(int defaultTake, int maximumTake) =>
        Error(
            StatusCodes.Status400BadRequest,
            ChatApiErrorCodes.InvalidTake,
            "The requested page size is invalid.",
            $"The take value must be from 1 through {maximumTake}; the default is {defaultTake}.");
    #endregion

    #region 处理（ConversationNotFound）
    /// <summary>
    /// 处理（ConversationNotFound）
    /// </summary>
    /// <returns>包含 ConversationNotFound 错误码的失败 JSON 响应，默认 HTTP 状态为 404。</returns>
    private JsonResult ConversationNotFound() =>
        Error(
            StatusCodes.Status404NotFound,
            UnifiedEntryErrorCodes.ConversationNotFound,
            "The conversation was not found.");
    #endregion

    #region 运行（RunNotFound）
    /// <summary>
    /// 运行（RunNotFound）
    /// </summary>
    /// <returns>包含运行不存在错误码的失败 JSON 响应，默认 HTTP 状态为 404。</returns>
    private JsonResult RunNotFound() =>
        Error(
            StatusCodes.Status404NotFound,
            ChatApiErrorCodes.RunNotFound,
            "The chat run was not found.");
    #endregion

    #region 处理（Error）
    /// <summary>
    /// 处理（Error）
    /// </summary>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="code">对象编码或业务错误码。</param>
    /// <param name="title">错误响应的标题说明。</param>
    /// <param name="detail">明细数据。</param>
    /// <returns>包含业务错误码和请求跟踪标识的失败响应；HTTP 状态优先采用错误解析器结果，否则使用传入状态。</returns>
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
    #endregion

    #region 处理（OperationSuccess）
    /// <summary>
    /// 处理（OperationSuccess）
    /// </summary>
    /// <param name="value">要封装到成功响应中的业务数据。</param>
    /// <param name="httpStatus">需要写入响应的 HTTP 状态码。</param>
    /// <typeparam name="T">待处理数据的泛型类型。</typeparam>
    /// <returns>使用指定 HTTP 状态码并以 ServiceResult 包装数据的成功 JSON 响应。</returns>
    private JsonResult OperationSuccess<T>(T value, int httpStatus) => new JsonResult( Success(value))
    { StatusCode = httpStatus };
    #endregion

    #region 处理（ExecutePersistenceOperationAsync）
    /// <summary>
    /// 处理（ExecutePersistenceOperationAsync）
    /// </summary>
    /// <param name="operation">需要执行的异步持久化操作。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <typeparam name="T">待处理数据的泛型类型。</typeparam>
    /// <returns>持久化操作返回的响应；捕获到持久化异常时转换为统一错误响应。</returns>
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
    #endregion

    #region 处理（Bound）
    /// <summary>
    /// 处理（Bound）
    /// </summary>
    /// <param name="take">最多返回的记录数。</param>
    /// <returns>限制在 1 至 MaximumPageSize 范围内的读取数量。</returns>
    private static int Bound(int take) =>
        Math.Clamp(take, 1, MaximumPageSize);
    #endregion

    #region 解析非空运行或会话标识（TryParseId）
    /// <summary>
    /// 尝试将文本解析为 D 格式且非空的 GUID（TryParseId）。
    /// </summary>
    /// <param name="value">待解析的带连字符 GUID 文本。</param>
    /// <param name="id">解析成功时输出标识；解析失败或输入为空 GUID 时输出 Guid.Empty。</param>
    /// <returns>文本符合 D 格式且解析结果不是 Guid.Empty 时返回 true，否则返回 false。</returns>
    private static bool TryParseId(string value, out Guid id) =>
        Guid.TryParseExact(value, "D", out id) && id != Guid.Empty;
    #endregion

    #region 处理（ExecutionIdentity）
    /// <summary>
    /// 处理（ExecutionIdentity）
    /// </summary>
    /// <returns>由当前调用方的用户、租户、权限和关联标识构造的 Agent 执行身份。</returns>
    private AgentExecutionIdentity ExecutionIdentity() => new(
        _caller.UserId,
        _caller.TenantId,
        _caller.Permissions,
        _caller.CorrelationId);
    #endregion
}

/// <summary>
/// 定义聊天接口边界使用的错误码。
/// </summary>
public static class ChatApiErrorCodes
{
    /// <summary>表示 <c>UnknownProperty</c> 场景的错误码。</summary>
    public const string UnknownProperty = "REQUEST_UNKNOWN_PROPERTY";
    /// <summary>表示 <c>InvalidId</c> 场景的错误码。</summary>
    public const string InvalidId = "REQUEST_INVALID_ID";
    /// <summary>表示 <c>InvalidTake</c> 场景的错误码。</summary>
    public const string InvalidTake = "REQUEST_INVALID_TAKE";
    /// <summary>表示 <c>RunNotFound</c> 场景的错误码。</summary>
    public const string RunNotFound = "UNIFIED_ENTRY_RUN_NOT_FOUND";
}

/// <summary>
/// 取消聊天运行的响应。
/// </summary>
/// <param name="RunId">运行标识。</param>
public sealed record ChatRunCancelResponse(Guid RunId);

/// <summary>
/// 聊天会话详情响应。
/// </summary>
/// <param name="Conversation">会话主体记录。</param>
/// <param name="Messages">会话消息集合。</param>
public sealed record ChatConversationDetailResponse(
    ConversationRecord Conversation,
    IReadOnlyList<ConversationMessageRecord> Messages);

/// <summary>
/// 启动聊天运行的请求。
/// </summary>
/// <param name="Input">运行或评测使用的输入内容。</param>
/// <param name="ConversationId">需要继续的会话标识；为空时创建新会话。</param>
public sealed record StartChatRunRequest(
    string? Input,
    Guid? ConversationId)
{
    [JsonExtensionData]
    /// <summary>
    /// 未识别的附加字段，用于严格输入校验。
    /// </summary>
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}
