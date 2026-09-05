using EU.Core.IServices.UnifiedEntry;
using System.Text.Json;
using EU.Core.IServices.Agents;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.IServices.Orchestration;
using EU.Core.IServices.Runtime;
using EU.Core.Model;

#nullable enable

namespace EU.Core.Services;

// 文件职责：RunOrchestrationTool 职责实现

/// <summary>
/// 实现启动编排运行的内部工具。
/// </summary>
public sealed class RunOrchestrationTool : IAgentInternalTool
{
    private const int MaximumInputCharacters = 32_768;
    private const int MaximumReasonCharacters = 1_024;
    private static readonly TimeSpan CleanupObservationTimeout =
        TimeSpan.FromMilliseconds(250);
    private readonly AgentVersionSnapshot _authorizingSnapshot;
    private readonly OrchestrationRuntimeService _orchestrationRuntime;
    private readonly UnifiedAgentExecutionLease _parentLease;
    private readonly Guid _parentRunId;
    private readonly UnifiedEntryExecutionScope _scope;
    private readonly AgentExecutionIdentity? _executionIdentity;
    private readonly IAgentToolApprovalHandler? _toolApprovalHandler;
    private readonly AgentToolApprovalBinding? _toolApprovalBinding;
    private readonly string _description;
    private readonly string _inputSchemaJson;

    #region 构造（RunOrchestrationTool）
    /// <summary>
    /// 构造（RunOrchestrationTool）
    /// </summary>
    /// <param name="orchestrationRuntime">编排运行时服务。</param>
    /// <param name="scope">执行范围。</param>
    /// <param name="authorizingSnapshot">授予访问权限的发布快照。</param>
    /// <param name="parentLease">父级执行租约。</param>
    /// <param name="parentRunId">父运行标识。</param>
    /// <param name="executionIdentity">当前执行使用的用户、租户及权限身份。</param>
    /// <param name="toolApprovalHandler">工具调用审批处理器。</param>
    /// <param name="toolApprovalBinding">工具审批绑定。</param>
    public RunOrchestrationTool(
        OrchestrationRuntimeService orchestrationRuntime,
        UnifiedEntryExecutionScope scope,
        AgentVersionSnapshot authorizingSnapshot,
        UnifiedAgentExecutionLease parentLease,
        Guid parentRunId,
        AgentExecutionIdentity? executionIdentity = null,
        IAgentToolApprovalHandler? toolApprovalHandler = null,
        AgentToolApprovalBinding? toolApprovalBinding = null)
    {
        _orchestrationRuntime = orchestrationRuntime
            ?? throw new ArgumentNullException(nameof(orchestrationRuntime));
        _scope = scope
            ?? throw new ArgumentNullException(nameof(scope));
        _authorizingSnapshot = AgentContractCloner.Clone(
            authorizingSnapshot
            ?? throw new ArgumentNullException(nameof(authorizingSnapshot)));
        _parentLease = parentLease
            ?? throw new ArgumentNullException(nameof(parentLease));
        if (parentRunId == Guid.Empty)
        {
            throw new ArgumentException(
                "A parent unified Agent run ID is required.",
                nameof(parentRunId));
        }

        _parentRunId = parentRunId;
        _executionIdentity = executionIdentity;
        _toolApprovalHandler = toolApprovalHandler;
        _toolApprovalBinding = toolApprovalBinding;
        AgentOrchestrationBindingSnapshot[] bindings =
            _authorizingSnapshot.Orchestrations
                .OrderBy(value => value.OrchestrationId)
                .ThenBy(value => value.OrchestrationVersionId)
                .ToArray();
        _description =
            "Start one orchestration version frozen in the Main Agent publication. "
            + "Authorized bindings: "
            + string.Join(
                "; ",
                bindings.Select(value =>
                    $"orchestrationId={value.OrchestrationId}, orchestrationVersionId={value.OrchestrationVersionId}"));
        _inputSchemaJson = InternalToolSchemaBuilder.Build(
            "orchestrationVersionId",
            bindings.Select(value => value.OrchestrationVersionId).ToArray(),
            "input",
            MaximumInputCharacters,
            MaximumReasonCharacters);
    }
    #endregion

    /// <summary>
    /// 获取内部工具名称。
    /// </summary>
    public string Name => "run_orchestration";

    /// <summary>
    /// 获取内部工具说明。
    /// </summary>
    public string Description => _description;

    /// <summary>
    /// 获取内部工具输入参数的 JSON Schema。
    /// </summary>
    public string InputSchemaJson => _inputSchemaJson;

    #region 调用（InvokeAsync）
    /// <summary>
    /// 调用（InvokeAsync）
    /// </summary>
    /// <param name="argumentsJson">工具调用参数的 JSON 文本。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>编排委派执行结果，成功时携带受保护输出，失败、超限或取消时携带错误码及安全提示。</returns>
    public async Task<AgentInternalToolResult> InvokeAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        if (!InternalToolArgumentParser.TryParse(
                argumentsJson,
                "orchestrationVersionId",
                "input",
                MaximumInputCharacters,
                MaximumReasonCharacters,
                _scope.Limits.InternalPayloadUtf8Bytes,
                out InternalToolArguments arguments))
        {
            return Failure(
                UnifiedEntryErrorCodes.InternalArgumentsInvalid,
                "The run_orchestration arguments are invalid.");
        }

        AgentOrchestrationBindingSnapshot? binding =
            _authorizingSnapshot.Orchestrations.FirstOrDefault(
                value => value.OrchestrationVersionId == arguments.VersionId);
        if (binding is null
            || _parentLease.AgentVersionId != _authorizingSnapshot.VersionId)
        {
            return Failure(
                UnifiedEntryErrorCodes.OrchestrationVersionUnauthorized,
                "The requested orchestration version is not authorized by the frozen Main Agent publication.");
        }

        OrchestrationRunRecord? started = null;
        bool linkRegistered = false;
        try
        {
            if (cancellationToken.IsCancellationRequested
                || _parentLease.CancellationToken.IsCancellationRequested)
            {
                return CancellationFailure(cancellationToken);
            }

            if (!_scope.ReserveOrchestration())
            {
                return Failure(
                    UnifiedEntryErrorCodes.OrchestrationCallLimitExceeded,
                    "The unified entry orchestration call limit was exceeded.");
            }

            using var effectiveCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    _parentLease.CancellationToken);
            CancellationToken effectiveToken = effectiveCancellation.Token;
            effectiveToken.ThrowIfCancellationRequested();
            ServiceResult<OrchestrationRunRecord> start =
                await _orchestrationRuntime.StartVersionAsync(
                    binding.OrchestrationId,
                    binding.OrchestrationVersionId,
                    arguments.Value,
                    new AgentRunExecutionOptions(
                        Array.Empty<IAgentInternalTool>(),
                        _scope)
                    {
                        McpResultGuard = _scope,
                        ExecutionIdentity = _executionIdentity,
                        ToolApprovalBinding = _toolApprovalBinding,
                        ToolApprovalHandler = _toolApprovalHandler
                    },
                    effectiveToken).ConfigureAwait(false);
            if (!start.Success)
            {
                return Failure(
                    OrchestrationServiceStatusCodes.ToErrorCode(start.Status),
                    "The frozen orchestration version could not be started.");
            }

            started = start.Data!;
            await RegisterLinkAsync(
                started,
                arguments.Value,
                arguments.Reason).ConfigureAwait(false);
            linkRegistered = true;
            effectiveToken.ThrowIfCancellationRequested();

            OrchestrationRunRecord? terminal =
                await _orchestrationRuntime.WaitForTerminalAsync(
                    started.Id,
                    effectiveToken).ConfigureAwait(false);
            if (terminal is null)
            {
                await TransitionLinkAsync(
                    started.Id,
                    UnifiedRunStatus.Failed,
                    string.Empty,
                    OrchestrationErrorCodes.RunNotFound,
                    _scope.GetUtcNow()).ConfigureAwait(false);
                return Failure(
                    OrchestrationErrorCodes.RunNotFound,
                    "The linked orchestration run could not be loaded.");
            }

            OrchestrationRunDetails? details =
                await _orchestrationRuntime.GetDetailsAsync(
                    started.Id,
                    effectiveToken).ConfigureAwait(false);
            if (details is null)
            {
                await TransitionLinkAsync(
                    started.Id,
                    UnifiedRunStatus.Failed,
                    string.Empty,
                    UnifiedEntryErrorCodes.OrchestrationDetailsMissing,
                    terminal.FinishedAtUtc ?? _scope.GetUtcNow()).ConfigureAwait(false);
                return Failure(
                    UnifiedEntryErrorCodes.OrchestrationDetailsMissing,
                    "The linked orchestration run details are unavailable.");
            }

            UnifiedRunStatus linkedStatus = MapStatus(terminal.Status);
            string errorCode = linkedStatus == UnifiedRunStatus.Completed
                ? string.Empty
                : string.IsNullOrWhiteSpace(terminal.ErrorCode)
                    ? UnifiedEntryErrorCodes.OrchestrationExecutionFailed
                    : terminal.ErrorCode;
            await TransitionLinkAsync(
                started.Id,
                linkedStatus,
                details.Output,
                errorCode,
                terminal.FinishedAtUtc ?? _scope.GetUtcNow()).ConfigureAwait(false);

            if (terminal.Status != OrchestrationRunStatus.Completed)
            {
                return Failure(
                    errorCode,
                    "The linked orchestration run did not complete successfully.");
            }

            ProtectedUnifiedPayload protectedOutput = Protect(details.Output);
            return new AgentInternalToolResult(
                true,
                protectedOutput.Content,
                string.Empty);
        }
        catch (OperationCanceledException)
        {
            string errorCode = _scope.ClassifyCancellation(
                lease: null,
                cancellationToken);
            if (started is not null)
            {
                await CancelAndPersistLinkAsync(
                    started,
                    linkRegistered,
                    errorCode).ConfigureAwait(false);
            }

            return Failure(errorCode, CancellationMessage(errorCode));
        }
        catch (UnifiedEntryException exception)
        {
            if (started is not null)
            {
                await CancelAndPersistLinkAsync(
                    started,
                    linkRegistered,
                    exception.ErrorCode).ConfigureAwait(false);
            }

            return Failure(
                exception.ErrorCode,
                "The orchestration delegation was rejected by the unified entry scope.");
        }
        catch (ObjectDisposedException)
        {
            if (started is not null)
            {
                await CancelAndPersistLinkAsync(
                    started,
                    linkRegistered,
                    UnifiedEntryErrorCodes.InvalidState).ConfigureAwait(false);
            }

            return Failure(
                UnifiedEntryErrorCodes.InvalidState,
                "The unified entry execution scope is no longer active.");
        }
        catch
        {
            if (started is not null)
            {
                await CancelAndPersistLinkAsync(
                    started,
                    linkRegistered,
                    UnifiedEntryErrorCodes.OrchestrationExecutionFailed).ConfigureAwait(false);
            }

            return Failure(
                UnifiedEntryErrorCodes.OrchestrationExecutionFailed,
                "The orchestration delegation failed.");
        }
    }
    #endregion

    #region 处理（RegisterLinkAsync）
    /// <summary>
    /// 处理（RegisterLinkAsync）
    /// </summary>
    /// <param name="run">运行记录。</param>
    /// <param name="input">执行输入内容。</param>
    /// <param name="reason">操作原因或判断依据。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task RegisterLinkAsync(OrchestrationRunRecord run, string input, string reason)
    {
        ProtectedUnifiedPayload protectedInput = Protect(input);
        Guid linkId = Guid.NewGuid();
        int depth = checked(_parentLease.Depth + 1);
        string rawPayloadJson = JsonSerializer.Serialize(new
        {
            orchestrationRunId = run.Id,
            orchestrationId = run.OrchestrationId,
            orchestrationVersionId = run.OrchestrationVersionId,
            reason
        });
        ProtectedUnifiedPayload rawPayload = Protect(rawPayloadJson);
        string persistedPayloadJson = JsonSerializer.Serialize(new
        {
            orchestrationRunId = run.Id,
            orchestrationId = run.OrchestrationId,
            orchestrationVersionId = run.OrchestrationVersionId,
            reason = Protect(reason).Content
        });
        ProtectedUnifiedPayload persistedPayload = Protect(persistedPayloadJson);
        await _scope.MutateAggregateAndAppendEventAsync(
            aggregate =>
        {
            if (!aggregate.Details.AgentRuns.Any(value => value.Id == _parentRunId))
            {
                throw new UnifiedEntryException(
                    UnifiedEntryErrorCodes.InvalidState,
                    "The parent unified Agent run does not exist.");
            }

            var link = new UnifiedOrchestrationRunLink(
                linkId,
                aggregate.Details.EntryRun.Id,
                _parentRunId,
                run.Id,
                run.OrchestrationVersionId,
                depth,
                UnifiedRunStatus.Running,
                run.StartedAtUtc,
                null,
                null,
                protectedInput.Content,
                protectedInput.OriginalSha256,
                string.Empty,
                string.Empty,
                string.Empty);
            return aggregate.WithDetails(new UnifiedRunDetails(
                aggregate.Details.EntryRun,
                aggregate.Details.AgentRuns,
                aggregate.Details.Orchestrations.Append(link).ToArray(),
                aggregate.Details.ToolCalls));
        },
            (aggregate, sequence) => new UnifiedRunEventRecord(
                Guid.NewGuid(),
                aggregate.Details.EntryRun.Id,
                sequence,
                _scope.CorrelationId,
                "orchestration-started",
                run.StartedAtUtc,
                _parentRunId,
                depth,
                persistedPayload.Content,
                rawPayload.OriginalSha256),
            CancellationToken.None).ConfigureAwait(false);
    }
    #endregion

    #region 处理（TransitionLinkAsync）
    /// <summary>
    /// 处理（TransitionLinkAsync）
    /// </summary>
    /// <param name="orchestrationRunId">编排运行标识。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="output">执行输出内容。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <param name="finishedAt">完成时间（UTC）。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task TransitionLinkAsync(Guid orchestrationRunId, UnifiedRunStatus status, string output, string errorCode, DateTimeOffset finishedAt)
    {
        ProtectedUnifiedPayload protectedOutput = Protect(output);
        await _scope.MutateAggregateAsync(aggregate =>
        {
            UnifiedOrchestrationRunLink? current =
                aggregate.Details.Orchestrations.FirstOrDefault(
                    value => value.OrchestrationRunId == orchestrationRunId);
            if (current is null
                || current.Status is UnifiedRunStatus.Completed
                    or UnifiedRunStatus.Failed
                    or UnifiedRunStatus.Cancelled)
            {
                return aggregate;
            }

            UnifiedOrchestrationRunLink terminal = current with
            {
                Status = status,
                FinishedAtUtc = finishedAt,
                Duration = NonNegative(finishedAt - current.StartedAtUtc),
                Output = protectedOutput.Content,
                OutputSha256 = output.Length == 0
                    ? string.Empty
                    : protectedOutput.OriginalSha256,
                ErrorCode = errorCode
            };
            return aggregate.WithDetails(new UnifiedRunDetails(
                aggregate.Details.EntryRun,
                aggregate.Details.AgentRuns,
                aggregate.Details.Orchestrations
                    .Select(value =>
                        value.OrchestrationRunId == orchestrationRunId
                            ? terminal
                            : value)
                    .ToArray(),
                aggregate.Details.ToolCalls));
        }, CancellationToken.None).ConfigureAwait(false);
    }
    #endregion

    #region 取消（CancelAndPersistLinkAsync）
    /// <summary>
    /// 取消（CancelAndPersistLinkAsync）
    /// </summary>
    /// <param name="started">已启动的执行状态。</param>
    /// <param name="linkRegistered">是否已登记运行关联。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task CancelAndPersistLinkAsync(OrchestrationRunRecord started, bool linkRegistered, string errorCode)
    {
        OrchestrationRunRecord? terminal = null;
        OrchestrationRunDetails? details = null;
        try
        {
            using var cleanup = new CancellationTokenSource(
                CleanupObservationTimeout);
            Task<bool> cancelling = _orchestrationRuntime.CancelAsync(
                started.Id,
                CancellationToken.None);
            try
            {
                await cancelling.WaitAsync(cleanup.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
                when (cleanup.IsCancellationRequested)
            {
                ObserveInBackground(cancelling);
            }

            if (!cleanup.IsCancellationRequested)
            {
                Task<OrchestrationRunRecord?> waiting =
                    _orchestrationRuntime.WaitForTerminalAsync(
                        started.Id,
                        CancellationToken.None);
                try
                {
                    terminal = await waiting.WaitAsync(
                        cleanup.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                    when (cleanup.IsCancellationRequested)
                {
                    ObserveInBackground(waiting);
                }
            }

            if (!cleanup.IsCancellationRequested)
            {
                details = await _orchestrationRuntime.GetDetailsAsync(
                    started.Id,
                    cleanup.Token).ConfigureAwait(false);
            }
        }
        catch
        {
            // Link terminalization below is independent of cleanup success.
        }

        if (terminal is null
            || terminal.Status == OrchestrationRunStatus.Running)
        {
            try
            {
                using var forceTimeout = new CancellationTokenSource(
                    CleanupObservationTimeout);
                Task<OrchestrationRunRecord?> forcing =
                    _orchestrationRuntime.ForceCancelAsync(
                    started.Id,
                    forceTimeout.Token);
                try
                {
                    terminal = await forcing.WaitAsync(
                        forceTimeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                    when (forceTimeout.IsCancellationRequested)
                {
                    ObserveInBackground(forcing);
                }
            }
            catch
            {
                // The unified link is still terminalized below.
            }
        }

        if (linkRegistered)
        {
            await TryTransitionLinkAsync(
                started.Id,
                UnifiedRunStatus.Cancelled,
                details?.Output ?? string.Empty,
                errorCode,
                terminal?.FinishedAtUtc ?? _scope.GetUtcNow()).ConfigureAwait(false);
        }
    }
    #endregion

    #region 处理（ObserveInBackground）
    /// <summary>
    /// 处理（ObserveInBackground）
    /// </summary>
    /// <param name="task">任务对象。</param>
    private static void ObserveInBackground(Task task) =>
        _ = ObserveFailureAsync(task);
    #endregion

    #region 处理（ObserveFailureAsync）
    /// <summary>
    /// 处理（ObserveFailureAsync）
    /// </summary>
    /// <param name="task">任务对象。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private static async Task ObserveFailureAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch
        {
            // The tool already returned a stable cancellation result.
        }
    }
    #endregion

    #region 尝试执行（TryTransitionLinkAsync）
    /// <summary>
    /// 尝试执行（TryTransitionLinkAsync）
    /// </summary>
    /// <param name="orchestrationRunId">编排运行标识。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="output">执行输出内容。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <param name="finishedAt">完成时间（UTC）。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task TryTransitionLinkAsync(Guid orchestrationRunId, UnifiedRunStatus status, string output, string errorCode, DateTimeOffset finishedAt)
    {
        try
        {
            await TransitionLinkAsync(
                orchestrationRunId,
                status,
                output,
                errorCode,
                finishedAt).ConfigureAwait(false);
        }
        catch
        {
            // The internal-tool boundary returns only its stable primary error.
        }
    }
    #endregion

    #region 取消（CancellationFailure）
    /// <summary>
    /// 取消（CancellationFailure）
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>按执行范围取消原因分类的编排工具失败结果。</returns>
    private AgentInternalToolResult CancellationFailure(CancellationToken cancellationToken)
    {
        string errorCode = _scope.ClassifyCancellation(
            lease: null,
            cancellationToken);
        return Failure(errorCode, CancellationMessage(errorCode));
    }
    #endregion

    #region 加密保护（Protect）
    /// <summary>
    /// 加密保护（Protect）
    /// </summary>
    /// <param name="value">待校验字节上限并脱敏的原始文本；null 按空字符串处理。</param>
    /// <returns>按当前执行范围内部载荷上限处理的脱敏内容、原始摘要及字节数。</returns>
    private ProtectedUnifiedPayload Protect(string? value) =>
        UnifiedEntryPayloadProtector.Protect(
            value,
            _scope.Limits.InternalPayloadUtf8Bytes,
            _scope.Limits.InternalPayloadUtf8Bytes);
    #endregion

    #region 映射（MapStatus）
    /// <summary>
    /// 映射（MapStatus）
    /// </summary>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <returns>完成和取消对应的统一运行状态；其他编排状态映射为 Failed。</returns>
    private static UnifiedRunStatus MapStatus(OrchestrationRunStatus status) =>
        status switch
        {
            OrchestrationRunStatus.Completed => UnifiedRunStatus.Completed,
            OrchestrationRunStatus.Cancelled => UnifiedRunStatus.Cancelled,
            _ => UnifiedRunStatus.Failed
        };
    #endregion

    #region 取消（CancellationMessage）
    /// <summary>
    /// 取消（CancellationMessage）
    /// </summary>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <returns>入口超时对应的超时提示，或普通取消提示。</returns>
    private static string CancellationMessage(string errorCode) =>
        errorCode == UnifiedEntryErrorCodes.EntryTimeout
            ? "The unified entry execution timed out."
            : "The unified entry execution was cancelled.";
    #endregion

    #region 处理（NonNegative）
    /// <summary>
    /// 将负时长归零（NonNegative）。
    /// </summary>
    /// <param name="value">待检查的持续时间。</param>
    /// <returns>输入为负数时返回零时长，否则返回原时长。</returns>
    private static TimeSpan NonNegative(TimeSpan value) =>
        value < TimeSpan.Zero ? TimeSpan.Zero : value;
    #endregion

    #region 处理（Failure）
    /// <summary>
    /// 处理（Failure）
    /// </summary>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <param name="content">内部工具失败时对调用方展示的安全提示。</param>
    /// <returns>包含指定内容和错误码、成功标志为 false 的内部工具结果。</returns>
    private static AgentInternalToolResult Failure(string errorCode, string content) =>
        new(false, content, errorCode);
    #endregion
}
