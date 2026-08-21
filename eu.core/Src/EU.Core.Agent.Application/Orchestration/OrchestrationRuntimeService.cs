using EU.Core.Agent.Application.Runtime;
using EU.Core.Model.ViewModels.Extend;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace EU.Core.Agent.Application.Orchestration;

public sealed class OrchestrationRuntimeService(
    IOrchestrationRepository orchestrations,
    IOrchestrationRunRepository runs,
    IAgentDefinitionCatalog agents,
    AgentRuntimeService agentRuntime,
    ExecutionPayloadLimits? payloadLimits = null)
{
    public const int MaximumInputCharacters = 32_768;
    private const string TerminalPersistenceFailureDataKey =
        "OrchestrationTerminalPersistenceFailure";
    private readonly ExecutionPayloadLimits _payloadLimits =
        payloadLimits ?? new ExecutionPayloadLimits();
    private readonly ConcurrentDictionary<Guid, ActiveExecution> _active = [];
    private readonly ConcurrentDictionary<Guid, byte> _retired = [];
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _runGates = [];
    private readonly ConcurrentDictionary<Guid, string> _outputs = [];
    private readonly ConcurrentQueue<Guid> _outputOrder = [];

    public async Task<OrchestrationOperationResult<OrchestrationRunRecord>> StartAsync(
        Guid orchestrationId,
        string? input,
        CancellationToken cancellationToken = default)
    {
        string normalized = input?.Trim() ?? "";
        if (normalized.Length is 0 or > MaximumInputCharacters)
            return OrchestrationOperationResult<OrchestrationRunRecord>.Failure(
                OrchestrationErrorCodes.RunInputInvalid,
                $"Input must contain from 1 through {MaximumInputCharacters} characters.");
        OrchestrationDefinition? definition =
            await orchestrations.GetByIdAsync(orchestrationId, cancellationToken);
        if (definition is null)
            return OrchestrationOperationResult<OrchestrationRunRecord>.Failure(
                OrchestrationErrorCodes.NotFound, "The orchestration was not found.");
        if (definition.Status != OrchestrationStatus.Enabled)
            return OrchestrationOperationResult<OrchestrationRunRecord>.Failure(
                OrchestrationErrorCodes.Disabled, "The orchestration is disabled.");
        OrchestrationVersionSnapshot? snapshot =
            definition.PublishedVersions.LastOrDefault()?.Snapshot;
        if (snapshot is null)
            return OrchestrationOperationResult<OrchestrationRunRecord>.Failure(
                OrchestrationErrorCodes.VersionMissing, "The orchestration has no published version.");

        return await StartSnapshotAsync(definition, snapshot, normalized, cancellationToken);
    }

    public async Task<OrchestrationOperationResult<OrchestrationRunRecord>> StartVersionAsync(
        Guid orchestrationId,
        Guid orchestrationVersionId,
        string input,
        CancellationToken cancellationToken = default)
        => await StartVersionAsync(
            orchestrationId,
            orchestrationVersionId,
            input,
            executionOptions: null,
            cancellationToken);

    public async Task<OrchestrationOperationResult<OrchestrationRunRecord>> StartVersionAsync(
        Guid orchestrationId,
        Guid orchestrationVersionId,
        string input,
        AgentRunExecutionOptions? executionOptions,
        CancellationToken cancellationToken = default)
    {
        string normalized = input?.Trim() ?? "";
        if (normalized.Length is 0 or > MaximumInputCharacters)
            return OrchestrationOperationResult<OrchestrationRunRecord>.Failure(
                OrchestrationErrorCodes.RunInputInvalid,
                $"Input must contain from 1 through {MaximumInputCharacters} characters.");
        OrchestrationDefinition? definition =
            await orchestrations.GetByIdAsync(orchestrationId, cancellationToken);
        if (definition is null)
            return OrchestrationOperationResult<OrchestrationRunRecord>.Failure(
                OrchestrationErrorCodes.NotFound, "The orchestration was not found.");
        if (definition.Status != OrchestrationStatus.Enabled)
            return OrchestrationOperationResult<OrchestrationRunRecord>.Failure(
                OrchestrationErrorCodes.Disabled, "The orchestration is disabled.");
        OrchestrationVersionSnapshot? snapshot = definition.PublishedVersions
            .FirstOrDefault(version => version.Id == orchestrationVersionId)
            ?.Snapshot;
        if (snapshot is null)
            return OrchestrationOperationResult<OrchestrationRunRecord>.Failure(
                OrchestrationErrorCodes.VersionMissing,
                "The requested orchestration version is not published by this orchestration.");

        return await StartSnapshotAsync(
            definition,
            snapshot,
            normalized,
            executionOptions,
            cancellationToken);
    }

    private async Task<OrchestrationOperationResult<OrchestrationRunRecord>> StartSnapshotAsync(
        OrchestrationDefinition definition,
        OrchestrationVersionSnapshot snapshot,
        string normalized,
        CancellationToken cancellationToken) =>
        await StartSnapshotAsync(
            definition,
            snapshot,
            normalized,
            executionOptions: null,
            cancellationToken);

    private async Task<OrchestrationOperationResult<OrchestrationRunRecord>> StartSnapshotAsync(
        OrchestrationDefinition definition,
        OrchestrationVersionSnapshot snapshot,
        string normalized,
        AgentRunExecutionOptions? executionOptions,
        CancellationToken cancellationToken)
    {
        foreach (OrchestrationAgentBinding binding in snapshot.Agents)
        {
            AgentDefinition? agent = await agents.GetDefinitionAsync(binding.AgentId, cancellationToken);
            if (agent?.RuntimeStatus != AgentRuntimeStatus.Enabled ||
                agent.PublishedVersions.LastOrDefault()?.Id != binding.AgentVersionId)
                return OrchestrationOperationResult<OrchestrationRunRecord>.Failure(
                    OrchestrationErrorCodes.AgentUnavailable,
                    $"Bound Agent version '{binding.AgentVersionId}' is no longer current and enabled.");
        }

        var record = new OrchestrationRunRecord(
            Guid.NewGuid(), definition.Id, snapshot.VersionId, definition.Code,
            OrchestrationRunStatus.Running, DateTimeOffset.UtcNow, null,
            Hash(normalized), "", OrchestrationContractCloner.ReadOnly(snapshot.Nodes.Select(node =>
            {
                OrchestrationAgentBinding binding = snapshot.Agents.Single(value => value.AgentId == node.AgentId);
                return new OrchestrationNodeRunRecord(
                    node.Id, node.Name, node.AgentId, binding.AgentVersionId,
                    OrchestrationNodeRunStatus.Pending, 0, null, null, 0, "", "");
            })));
        var source = new CancellationTokenSource();
        var registered = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Task execution = ExecuteAfterRegistrationAsync(
            record,
            snapshot,
            normalized,
            executionOptions,
            source,
            registered.Task);
        var active = new ActiveExecution(source, execution);
        if (!_active.TryAdd(record.Id, active))
        {
            registered.TrySetResult(false);
            source.Dispose();
            throw new InvalidOperationException("Run identifier collision.");
        }

        bool runRecordPersisted = false;
        var initialDetails = new OrchestrationRunDetails(
            record.Id,
            record.OrchestrationId,
            RedactContent(normalized),
            "",
            OrchestrationContractCloner.ReadOnly(
                Array.Empty<OrchestrationNodeAttemptRecord>()));
        try
        {
            await runs.SaveAsync(record, cancellationToken);
            runRecordPersisted = true;
            bool detailsPersisted = await runs.TrySaveRunningDetailsAsync(
                initialDetails,
                cancellationToken);
            if (!detailsPersisted)
            {
                throw new InvalidOperationException(
                    "The orchestration run became terminal before its details were initialized.");
            }
        }
        catch (Exception exception)
        {
            source.Dispose();
            if (runRecordPersisted)
            {
                bool cancelled =
                    exception is OperationCanceledException &&
                    cancellationToken.IsCancellationRequested;
                await ReconcileInitializationFailureAsync(
                    record.Id,
                    initialDetails,
                    cancelled
                        ? OrchestrationRunStatus.Cancelled
                        : OrchestrationRunStatus.Failed,
                    cancelled
                        ? OrchestrationNodeRunStatus.Cancelled
                        : OrchestrationNodeRunStatus.Failed,
                    cancelled
                        ? "ORCHESTRATION_RUN_CANCELLED"
                        : "ORCHESTRATION_RUN_FAILED",
                    exception);
            }

            _active.TryRemove(record.Id, out _);
            registered.TrySetResult(false);
            await execution;
            throw;
        }

        registered.TrySetResult(true);
        return OrchestrationOperationResult<OrchestrationRunRecord>.Success(record);
    }

    private async Task ReconcileInitializationFailureAsync(
        Guid runId,
        OrchestrationRunDetails detailsIfMissing,
        OrchestrationRunStatus runStatus,
        OrchestrationNodeRunStatus nodeStatus,
        string errorCode,
        Exception originalException)
    {
        try
        {
            OrchestrationRunRecord? authoritative =
                await runs.GetAsync(runId, CancellationToken.None)
                    .ConfigureAwait(false);
            if (authoritative is null
                || authoritative.Status != OrchestrationRunStatus.Running)
            {
                return;
            }

            await runs.TryFinalizeRunningAsync(
                runId,
                runStatus,
                nodeStatus,
                OrchestrationTerminalTransitionPolicy.TerminalizePending,
                DateTimeOffset.UtcNow,
                errorCode,
                detailsIfMissing,
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception terminalException)
        {
            try
            {
                OrchestrationRunRecord? authoritative =
                    await runs.GetAsync(runId, CancellationToken.None)
                        .ConfigureAwait(false);
                if (authoritative?.Status == OrchestrationRunStatus.Running)
                {
                    originalException.Data[TerminalPersistenceFailureDataKey] =
                        terminalException;
                }
            }
            catch (Exception reconciliationException)
            {
                originalException.Data[TerminalPersistenceFailureDataKey] =
                    new AggregateException(
                        terminalException,
                        reconciliationException);
            }
        }
    }

    public async Task<OrchestrationRunRecord?> GetAsync(Guid runId, CancellationToken cancellationToken = default) =>
        await RecoverIfInterruptedAsync(await runs.GetAsync(runId, cancellationToken), cancellationToken);

    public Task<OrchestrationRunDetails?> GetDetailsAsync(
        Guid runId,
        CancellationToken cancellationToken = default) =>
        runs.GetDetailsAsync(runId, cancellationToken);

    public async Task<IReadOnlyList<OrchestrationRunRecord>> ListAsync(
        Guid orchestrationId, int take, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OrchestrationRunRecord> values =
            await runs.ListAsync(orchestrationId, take, cancellationToken);
        var recovered = new List<OrchestrationRunRecord>(values.Count);
        foreach (OrchestrationRunRecord value in values)
            recovered.Add((await RecoverIfInterruptedAsync(value, cancellationToken))!);
        return OrchestrationContractCloner.ReadOnly(recovered);
    }

    public async Task<bool> CancelAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        if (!_active.TryGetValue(runId, out ActiveExecution? active))
            return await runs.GetAsync(runId, cancellationToken) is not null;
        try
        {
            await active.Cancellation.CancelAsync();
        }
        catch (ObjectDisposedException)
        {
            // Execution reached its terminal record between lookup and cancellation.
        }
        return true;
    }

    public async Task<OrchestrationRunRecord?> ForceCancelAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        SemaphoreSlim gate = GetRunGate(runId);
        ActiveExecution? retiredExecution = null;
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            OrchestrationRunRecord? value =
                await runs.GetAsync(
                    runId,
                    CancellationToken.None).ConfigureAwait(false);
            if (value is null)
            {
                _active.TryRemove(runId, out retiredExecution);
                return null;
            }

            OrchestrationRunRecord terminal = value;
            if (value.Status == OrchestrationRunStatus.Running)
            {
                DateTimeOffset finishedAt = DateTimeOffset.UtcNow;
                OrchestrationRunTransitionResult result =
                    await runs.TryFinalizeRunningAsync(
                        runId,
                        OrchestrationRunStatus.Cancelled,
                        OrchestrationNodeRunStatus.Cancelled,
                        OrchestrationTerminalTransitionPolicy.TerminalizePending,
                        finishedAt,
                        "ORCHESTRATION_RUN_CANCELLED",
                        detailsIfMissing: null,
                        CancellationToken.None).ConfigureAwait(false);
                terminal = result.Run ?? value;
                if (result.Transitioned)
                {
                    _retired[runId] = 0;
                }
            }

            _active.TryRemove(runId, out retiredExecution);
            return terminal;
        }
        finally
        {
            gate.Release();
            if (retiredExecution is not null)
            {
                CancelRetiredExecutionBestEffort(retiredExecution);
                ObserveRetiredExecution(runId, retiredExecution.Execution);
            }
        }
    }

    public async Task<OrchestrationRunRecord?> WaitForTerminalAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        if (_active.TryGetValue(runId, out ActiveExecution? active))
            await active.Execution.WaitAsync(cancellationToken);
        return await GetAsync(runId, cancellationToken);
    }

    public string? GetEphemeralOutput(Guid runId) =>
        _outputs.TryGetValue(runId, out string? value) ? value : null;

    private async Task ExecuteAfterRegistrationAsync(
        OrchestrationRunRecord record,
        OrchestrationVersionSnapshot snapshot,
        string initialInput,
        AgentRunExecutionOptions? executionOptions,
        CancellationTokenSource source,
        Task<bool> registered)
    {
        if (!await registered) return;
        await ExecuteGuardedAsync(
            record,
            snapshot,
            initialInput,
            executionOptions,
            source);
    }

    private async Task ExecuteGuardedAsync(
        OrchestrationRunRecord record,
        OrchestrationVersionSnapshot snapshot,
        string initialInput,
        AgentRunExecutionOptions? executionOptions,
        CancellationTokenSource source)
    {
        try
        {
            await ExecuteAsync(
                record,
                snapshot,
                initialInput,
                executionOptions,
                source.Token);
        }
        catch (OrchestrationOwnershipLostException)
        {
            // Another runtime made a durable terminal state authoritative.
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested)
        {
            await FinishAsync(record.Id, OrchestrationRunStatus.Cancelled, "ORCHESTRATION_RUN_CANCELLED");
        }
        catch (AgentRuntimeException exception)
        {
            await FinishAsync(record.Id, OrchestrationRunStatus.Failed, exception.ErrorCode);
        }
        catch
        {
            await FinishAsync(record.Id, OrchestrationRunStatus.Failed, "ORCHESTRATION_RUN_FAILED");
        }
        finally
        {
            _active.TryRemove(record.Id, out _);
            source.Dispose();
            _retired.TryRemove(record.Id, out _);
            _runGates.TryRemove(record.Id, out _);
        }
    }

    private async Task ExecuteAsync(
        OrchestrationRunRecord initial,
        OrchestrationVersionSnapshot snapshot,
        string initialInput,
        AgentRunExecutionOptions? executionOptions,
        CancellationToken cancellationToken)
    {
        string nodeId = snapshot.StartNodeId;
        string previousOutput = "";
        bool previousSucceeded = true;
        int visited = 0;
        while (nodeId.Length > 0 && visited++ < snapshot.Nodes.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OrchestrationNode node = snapshot.Nodes.Single(value => value.Id == nodeId);
            string nodeInput = BuildInput(node, initialInput, previousOutput);
            Guid agentVersionId = initial.Nodes
                .Single(value => value.NodeId == node.Id)
                .AgentVersionId;
            (bool succeeded, string output, string errorCode) result =
                await ExecuteNodeAsync(
                    initial.Id,
                    node,
                    agentVersionId,
                    nodeInput,
                    executionOptions,
                    cancellationToken);
            previousSucceeded = result.succeeded;
            previousOutput = result.output;
            OrchestrationEdge? edge = snapshot.Edges
                .Where(value => value.FromNodeId == nodeId)
                .OrderBy(value => value.Order)
                .FirstOrDefault(value => Matches(value, previousSucceeded, previousOutput));
            if (edge is null)
            {
                if (previousSucceeded)
                {
                    EnsureWithinLimit(
                        previousOutput, _payloadLimits.FinalOutputCharacters, "final output");
                    EnsureOwnership(
                        initial.Id,
                        await UpdateDetailsOutputAsync(
                            initial.Id,
                            RedactContent(previousOutput)));
                }
                bool finalized = await FinishAsync(
                    initial.Id,
                    previousSucceeded ? OrchestrationRunStatus.Completed : OrchestrationRunStatus.Failed,
                    previousSucceeded ? "" : result.errorCode);
                EnsureOwnership(initial.Id, finalized);
                if (previousSucceeded)
                {
                    CacheOutput(initial.Id, previousOutput);
                }
                return;
            }
            nodeId = edge.ToNodeId;
        }
        await FinishAsync(initial.Id, OrchestrationRunStatus.Failed, "ORCHESTRATION_STEP_LIMIT");
    }

    private async Task<(bool succeeded, string output, string errorCode)> ExecuteNodeAsync(
        Guid runId,
        OrchestrationNode node,
        Guid agentVersionId,
        string input,
        AgentRunExecutionOptions? executionOptions,
        CancellationToken cancellationToken)
    {
        string lastError = "";
        for (int attempt = 1; attempt <= node.MaximumRetries + 1; attempt++)
        {
            DateTimeOffset started = DateTimeOffset.UtcNow;
            string persistedInput = RedactContent(input);
            EnsureWithinLimit(
                persistedInput, _payloadLimits.NodeInputCharacters, "node input");
            var attemptRecord = new OrchestrationNodeAttemptRecord(
                node.Id,
                attempt,
                Guid.Empty,
                persistedInput,
                Hash(input),
                "",
                "",
                OrchestrationNodeRunStatus.Running,
                started,
                null,
                "",
                OrchestrationContractCloner.ReadOnly(Array.Empty<OrchestrationToolCallRecord>()));
            EnsureOwnership(
                runId,
                await UpsertAttemptAsync(runId, attemptRecord, cancellationToken));
            EnsureOwnership(
                runId,
                await UpdateNodeAsync(runId, node.Id, current => current with
            {
                Status = OrchestrationNodeRunStatus.Running,
                Attempts = attempt,
                StartedAtUtc = current.StartedAtUtc ?? started,
                InputSha256 = Hash(input),
                ErrorCode = ""
            }));
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(node.TimeoutSeconds));
            try
            {
                AgentRunPreparationResult prepared =
                    await agentRuntime.PrepareVersionAsync(
                        node.AgentId, agentVersionId, input, timeout.Token);
                if (!prepared.Succeeded)
                {
                    lastError = prepared.Error!.Code;
                    attemptRecord = attemptRecord with
                    {
                        Status = OrchestrationNodeRunStatus.Failed,
                        FinishedAtUtc = DateTimeOffset.UtcNow,
                        ErrorCode = lastError
                    };
                    EnsureOwnership(
                        runId,
                        await UpsertAttemptAsync(
                            runId,
                            attemptRecord,
                            cancellationToken));
                    continue;
                }
                AgentRunContext executionContext = prepared.Context! with
                {
                    InternalTools = executionOptions?.InternalTools?.ToArray()
                        ?? prepared.Context.InternalTools,
                    McpCallGuard = executionOptions?.McpCallGuard
                        ?? prepared.Context.McpCallGuard,
                    McpResultGuard = executionOptions?.McpResultGuard
                        ?? prepared.Context.McpResultGuard,
                    ExecutionIdentity = executionOptions?.ExecutionIdentity
                        ?? prepared.Context.ExecutionIdentity,
                    ToolApprovalBinding = executionOptions?.ToolApprovalBinding
                        ?? prepared.Context.ToolApprovalBinding,
                    ToolApprovalHandler = executionOptions?.ToolApprovalHandler
                        ?? prepared.Context.ToolApprovalHandler
                };
                attemptRecord = attemptRecord with
                {
                    AgentRunId = executionContext.RunId
                };
                EnsureOwnership(
                    runId,
                    await UpsertAttemptAsync(
                        runId,
                        attemptRecord,
                        cancellationToken));
                var output = new StringBuilder();
                var toolCalls = new List<OrchestrationToolCallRecord>();
                bool failed = false;
                await foreach (AgentRunEvent value in agentRuntime.StreamAsync(
                    executionContext,
                    timeout.Token))
                {
                    if (value.Kind == AgentRunEventKind.Delta)
                    {
                        string candidate = output.ToString() + value.Text;
                        EnsureWithinLimit(
                            candidate, _payloadLimits.NodeOutputCharacters, "node output");
                        output.Append(value.Text);
                    }
                    if (value.Kind == AgentRunEventKind.ToolStarted)
                    {
                        string arguments = RedactContent(value.ArgumentsJson);
                        EnsureWithinLimit(
                            arguments, _payloadLimits.ToolArgumentsCharacters, "tool arguments");
                        toolCalls.Add(new OrchestrationToolCallRecord(
                            value.ToolCallId ?? Guid.NewGuid(),
                            executionContext.RunId,
                            value.ToolVersionId ?? Guid.Empty,
                            value.ToolName,
                            value.Kind,
                            arguments,
                            "",
                            "",
                            0,
                            value.OccurredAtUtc,
                            null,
                            ""));
                    }
                    if (value.Kind is AgentRunEventKind.ToolSucceeded or
                        AgentRunEventKind.ToolBlocked or
                        AgentRunEventKind.ToolFailed)
                    {
                        string resultContent = RedactContent(value.Text);
                        EnsureWithinLimit(
                            resultContent, _payloadLimits.ToolResultCharacters, "tool result");
                        int index = value.ToolCallId is Guid callId
                            ? toolCalls.FindIndex(call => call.ToolCallId == callId)
                            : -1;
                        OrchestrationToolCallRecord current = index >= 0
                            ? toolCalls[index]
                            : new OrchestrationToolCallRecord(
                                value.ToolCallId ?? Guid.NewGuid(),
                                executionContext.RunId,
                                value.ToolVersionId ?? Guid.Empty,
                                value.ToolName,
                                AgentRunEventKind.ToolStarted,
                                "",
                                "",
                                "",
                                0,
                                value.OccurredAtUtc,
                                null,
                                "");
                        OrchestrationToolCallRecord terminal = current with
                        {
                            Status = value.Kind,
                            ResultContent = resultContent,
                            ResultSha256 = Hash(value.Text),
                            ResultCharacters = value.Text.Length,
                            FinishedAtUtc = value.OccurredAtUtc,
                            ErrorCode = value.ErrorCode
                        };
                        if (index >= 0) toolCalls[index] = terminal;
                        else toolCalls.Add(terminal);
                    }
                    if (value.Kind is AgentRunEventKind.Failed or AgentRunEventKind.Cancelled)
                    {
                        failed = true;
                        lastError = value.ErrorCode;
                    }
                    attemptRecord = attemptRecord with
                    {
                        Output = RedactContent(output.ToString()),
                        OutputSha256 = output.Length == 0 ? "" : Hash(output.ToString()),
                        ToolCalls = OrchestrationContractCloner.ReadOnly(toolCalls)
                    };
                    EnsureOwnership(
                        runId,
                        await UpsertAttemptAsync(
                            runId,
                            attemptRecord,
                            timeout.Token));
                }
                if (!failed)
                {
                    attemptRecord = attemptRecord with
                    {
                        Status = OrchestrationNodeRunStatus.Completed,
                        FinishedAtUtc = DateTimeOffset.UtcNow,
                        Output = RedactContent(output.ToString()),
                        OutputSha256 = Hash(output.ToString()),
                        ToolCalls = OrchestrationContractCloner.ReadOnly(toolCalls)
                    };
                    EnsureOwnership(
                        runId,
                        await UpsertAttemptAsync(
                            runId,
                            attemptRecord,
                            cancellationToken));
                    EnsureOwnership(
                        runId,
                        await UpdateNodeAsync(runId, node.Id, current => current with
                    {
                        Status = OrchestrationNodeRunStatus.Completed,
                        FinishedAtUtc = DateTimeOffset.UtcNow,
                        OutputCharacters = output.Length
                    }));
                    return (true, output.ToString(), "");
                }
                attemptRecord = attemptRecord with
                {
                    Status = OrchestrationNodeRunStatus.Failed,
                    FinishedAtUtc = DateTimeOffset.UtcNow,
                    ErrorCode = lastError.Length == 0 ? "ORCHESTRATION_NODE_FAILED" : lastError,
                    Output = RedactContent(output.ToString()),
                    OutputSha256 = output.Length == 0 ? "" : Hash(output.ToString()),
                    ToolCalls = OrchestrationContractCloner.ReadOnly(toolCalls)
                };
                EnsureOwnership(
                    runId,
                    await UpsertAttemptAsync(
                        runId,
                        attemptRecord,
                        cancellationToken));
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                lastError = "ORCHESTRATION_NODE_TIMEOUT";
                attemptRecord = attemptRecord with
                {
                    Status = OrchestrationNodeRunStatus.Failed,
                    FinishedAtUtc = DateTimeOffset.UtcNow,
                    ErrorCode = lastError
                };
                EnsureOwnership(
                    runId,
                    await UpsertAttemptAsync(
                        runId,
                        attemptRecord,
                        CancellationToken.None));
            }
            catch (AgentRuntimeException exception)
                when (exception.ErrorCode == OrchestrationErrorCodes.PayloadLimitExceeded)
            {
                attemptRecord = attemptRecord with
                {
                    Status = OrchestrationNodeRunStatus.Failed,
                    FinishedAtUtc = DateTimeOffset.UtcNow,
                    ErrorCode = exception.ErrorCode
                };
                EnsureOwnership(
                    runId,
                    await UpsertAttemptAsync(
                        runId,
                        attemptRecord,
                        CancellationToken.None));
                EnsureOwnership(
                    runId,
                    await UpdateNodeAsync(runId, node.Id, current => current with
                {
                    Status = OrchestrationNodeRunStatus.Failed,
                    FinishedAtUtc = DateTimeOffset.UtcNow,
                    ErrorCode = exception.ErrorCode
                }));
                throw;
            }
            if (attempt <= node.MaximumRetries) continue;
        }
        EnsureOwnership(
            runId,
            await UpdateNodeAsync(runId, node.Id, current => current with
        {
            Status = OrchestrationNodeRunStatus.Failed,
            FinishedAtUtc = DateTimeOffset.UtcNow,
            ErrorCode = lastError.Length == 0 ? "ORCHESTRATION_NODE_FAILED" : lastError
        }));
        return (false, "", lastError);
    }

    private async Task<bool> UpdateNodeAsync(
        Guid runId,
        string nodeId,
        Func<OrchestrationNodeRunRecord, OrchestrationNodeRunRecord> update)
    {
        SemaphoreSlim gate = GetRunGate(runId);
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            OrchestrationRunRecord? value = await runs.GetAsync(runId);
            if (_retired.ContainsKey(runId)
                || value is null
                || value.Status != OrchestrationRunStatus.Running)
            {
                return false;
            }

            await runs.SaveAsync(value with
            {
                Nodes = OrchestrationContractCloner.ReadOnly(value.Nodes.Select(node =>
                    node.NodeId == nodeId ? update(node) : node))
            });
            OrchestrationRunRecord? authoritative =
                await runs.GetAsync(runId, CancellationToken.None);
            return authoritative?.Status == OrchestrationRunStatus.Running;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<bool> UpsertAttemptAsync(
        Guid runId,
        OrchestrationNodeAttemptRecord attempt,
        CancellationToken cancellationToken)
    {
        SemaphoreSlim gate = GetRunGate(runId);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            OrchestrationRunRecord? run =
                await runs.GetAsync(
                    runId,
                    CancellationToken.None).ConfigureAwait(false);
            if (_retired.ContainsKey(runId)
                || run is null
                || run.Status != OrchestrationRunStatus.Running)
            {
                return false;
            }

            OrchestrationRunDetails details =
                await runs.GetDetailsAsync(runId, CancellationToken.None) ??
                throw new InvalidOperationException("Run details disappeared.");
            var values = details.Attempts
                .Where(value =>
                    value.NodeId != attempt.NodeId
                    || value.Attempt != attempt.Attempt)
                .Append(attempt)
                .OrderBy(value => value.StartedAtUtc)
                .ThenBy(value => value.NodeId, StringComparer.Ordinal)
                .ThenBy(value => value.Attempt)
                .ToArray();
            return await runs.TrySaveRunningDetailsAsync(details with
            {
                Attempts = OrchestrationContractCloner.ReadOnly(values)
            }, CancellationToken.None);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<bool> UpdateDetailsOutputAsync(Guid runId, string output)
    {
        SemaphoreSlim gate = GetRunGate(runId);
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            OrchestrationRunRecord? run =
                await runs.GetAsync(runId).ConfigureAwait(false);
            if (_retired.ContainsKey(runId)
                || run is null
                || run.Status != OrchestrationRunStatus.Running)
            {
                return false;
            }

            OrchestrationRunDetails details =
                await runs.GetDetailsAsync(runId) ??
                throw new InvalidOperationException("Run details disappeared.");
            return await runs.TrySaveRunningDetailsAsync(
                details with { Output = output },
                CancellationToken.None);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<bool> FinishAsync(
        Guid runId,
        OrchestrationRunStatus status,
        string errorCode)
    {
        SemaphoreSlim gate = GetRunGate(runId);
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            OrchestrationRunRecord? value = await runs.GetAsync(runId);
            if (_retired.ContainsKey(runId)
                || value is null
                || value.Status != OrchestrationRunStatus.Running)
            {
                return false;
            }

            OrchestrationNodeRunStatus nodeStatus =
                status switch
                {
                    OrchestrationRunStatus.Completed =>
                        OrchestrationNodeRunStatus.Completed,
                    OrchestrationRunStatus.Cancelled =>
                        OrchestrationNodeRunStatus.Cancelled,
                    _ => OrchestrationNodeRunStatus.Failed
                };
            DateTimeOffset finishedAt = DateTimeOffset.UtcNow;
            OrchestrationRunTransitionResult result =
                await runs.TryFinalizeRunningAsync(
                runId,
                status,
                nodeStatus,
                status == OrchestrationRunStatus.Completed
                    ? OrchestrationTerminalTransitionPolicy.PreservePending
                    : OrchestrationTerminalTransitionPolicy.TerminalizePending,
                finishedAt,
                errorCode,
                detailsIfMissing: null,
                CancellationToken.None);
            return result.Transitioned;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<OrchestrationRunRecord?> RecoverIfInterruptedAsync(
        OrchestrationRunRecord? value,
        CancellationToken cancellationToken)
    {
        if (value is null || value.Status != OrchestrationRunStatus.Running)
        {
            return value;
        }

        Guid runId = value.Id;
        SemaphoreSlim gate = GetRunGate(runId);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            OrchestrationRunRecord? authoritative =
                await runs.GetAsync(
                    runId,
                    CancellationToken.None).ConfigureAwait(false);
            if (authoritative is null
                || authoritative.Status != OrchestrationRunStatus.Running
                || _active.ContainsKey(runId))
            {
                return authoritative;
            }

            const string recoveryErrorCode = "ORCHESTRATION_HOST_INTERRUPTED";
            DateTimeOffset recoveredAtUtc = DateTimeOffset.UtcNow;
            OrchestrationRunTransitionResult result =
                await runs.RecoverInterruptedAsync(
                    runId,
                    recoveredAtUtc,
                    recoveryErrorCode,
                    CancellationToken.None).ConfigureAwait(false);
            return result.Run;
        }
        finally
        {
            gate.Release();
        }
    }

    private static string BuildInput(OrchestrationNode node, string initial, string previous) =>
        node.InputMode switch
        {
            OrchestrationNodeInputMode.InitialInput => initial,
            OrchestrationNodeInputMode.PreviousOutput => previous,
            OrchestrationNodeInputMode.Template => (node.InputTemplate ?? "")
                .Replace("{{input}}", initial, StringComparison.Ordinal)
                .Replace("{{previous}}", previous, StringComparison.Ordinal),
            _ => initial
        };

    private static bool Matches(OrchestrationEdge edge, bool succeeded, string output) =>
        edge.Condition switch
        {
            OrchestrationEdgeCondition.Always => true,
            OrchestrationEdgeCondition.Succeeded => succeeded,
            OrchestrationEdgeCondition.Failed => !succeeded,
            OrchestrationEdgeCondition.OutputContains =>
                output.Contains(edge.ConditionValue ?? "", StringComparison.OrdinalIgnoreCase),
            _ => false
        };

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string RedactContent(string value) =>
        ExecutionPayloadRedactor.RedactJson(value);

    private static void EnsureWithinLimit(string value, int maximum, string payloadName)
    {
        if (value.Length <= maximum) return;
        throw new AgentRuntimeException(
            OrchestrationErrorCodes.PayloadLimitExceeded,
            $"The {payloadName} exceeds the configured {maximum}-character limit.");
    }

    private void CacheOutput(Guid runId, string value)
    {
        _outputs[runId] = value.Length <= 65_536 ? value : value[..65_536];
        _outputOrder.Enqueue(runId);
        while (_outputOrder.Count > 100 && _outputOrder.TryDequeue(out Guid expired))
            _outputs.TryRemove(expired, out _);
    }

    private void EnsureOwnership(Guid runId, bool mutationAccepted)
    {
        if (mutationAccepted)
        {
            return;
        }

        _retired[runId] = 0;
        _outputs.TryRemove(runId, out _);
        if (_active.TryGetValue(runId, out ActiveExecution? active))
        {
            try
            {
                active.Cancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The execution reached cleanup while ownership was being retired.
            }
        }
        throw new OrchestrationOwnershipLostException(runId);
    }

    private SemaphoreSlim GetRunGate(Guid runId) =>
        _runGates.GetOrAdd(runId, static _ => new SemaphoreSlim(1, 1));

    private static void CancelRetiredExecutionBestEffort(
        ActiveExecution retiredExecution)
    {
        try
        {
            retiredExecution.Cancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The execution completed cleanup after it was retired.
        }
        catch (AggregateException)
        {
            // Cancellation callbacks cannot invalidate the durable terminal state.
        }
    }

    private void ObserveRetiredExecution(Guid runId, Task execution) =>
        _ = ObserveRetiredExecutionAsync(runId, execution);

    private async Task ObserveRetiredExecutionAsync(Guid runId, Task execution)
    {
        try
        {
            await execution.ConfigureAwait(false);
        }
        catch
        {
            // A forced terminal record is authoritative after retirement.
        }
        finally
        {
            _retired.TryRemove(runId, out _);
            _runGates.TryRemove(runId, out _);
        }
    }

    private sealed record ActiveExecution(
        CancellationTokenSource Cancellation,
        Task Execution);

    private sealed class OrchestrationOwnershipLostException(Guid runId) :
        Exception($"Orchestration run '{runId}' is no longer owned by this runtime.");
}
