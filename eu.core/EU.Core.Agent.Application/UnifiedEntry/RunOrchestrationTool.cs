using System.Text.Json;
using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Application.Orchestration;
using EU.Core.Agent.Application.Runtime;

namespace EU.Core.Agent.Application.UnifiedEntry;

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

    public string Name => "run_orchestration";

    public string Description => _description;

    public string InputSchemaJson => _inputSchemaJson;

    public async Task<AgentInternalToolResult> InvokeAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
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
            OrchestrationOperationResult<OrchestrationRunRecord> start =
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
            if (!start.Succeeded)
            {
                return Failure(
                    start.Error!.Code,
                    "The frozen orchestration version could not be started.");
            }

            started = start.Value!;
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

    private async Task RegisterLinkAsync(
        OrchestrationRunRecord run,
        string input,
        string reason)
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

    private async Task TransitionLinkAsync(
        Guid orchestrationRunId,
        UnifiedRunStatus status,
        string output,
        string errorCode,
        DateTimeOffset finishedAt)
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

    private async Task CancelAndPersistLinkAsync(
        OrchestrationRunRecord started,
        bool linkRegistered,
        string errorCode)
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

    private static void ObserveInBackground(Task task) =>
        _ = ObserveFailureAsync(task);

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

    private async Task TryTransitionLinkAsync(
        Guid orchestrationRunId,
        UnifiedRunStatus status,
        string output,
        string errorCode,
        DateTimeOffset finishedAt)
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

    private AgentInternalToolResult CancellationFailure(
        CancellationToken cancellationToken)
    {
        string errorCode = _scope.ClassifyCancellation(
            lease: null,
            cancellationToken);
        return Failure(errorCode, CancellationMessage(errorCode));
    }

    private ProtectedUnifiedPayload Protect(string? value) =>
        UnifiedEntryPayloadProtector.Protect(
            value,
            _scope.Limits.InternalPayloadUtf8Bytes,
            _scope.Limits.InternalPayloadUtf8Bytes);

    private static UnifiedRunStatus MapStatus(OrchestrationRunStatus status) =>
        status switch
        {
            OrchestrationRunStatus.Completed => UnifiedRunStatus.Completed,
            OrchestrationRunStatus.Cancelled => UnifiedRunStatus.Cancelled,
            _ => UnifiedRunStatus.Failed
        };

    private static string CancellationMessage(string errorCode) =>
        errorCode == UnifiedEntryErrorCodes.EntryTimeout
            ? "The unified entry execution timed out."
            : "The unified entry execution was cancelled.";

    private static TimeSpan NonNegative(TimeSpan value) =>
        value < TimeSpan.Zero ? TimeSpan.Zero : value;

    private static AgentInternalToolResult Failure(
        string errorCode,
        string content) =>
        new(false, content, errorCode);
}
