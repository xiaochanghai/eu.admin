using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Application.Runtime;

namespace EU.Core.Agent.Application.Orchestration;

public sealed class OrchestrationRuntimeService(
    IOrchestrationRepository orchestrations,
    IOrchestrationRunRepository runs,
    IAgentRepository agents,
    AgentRuntimeService agentRuntime,
    ExecutionPayloadLimits? payloadLimits = null)
{
    public const int MaximumInputCharacters = 32_768;
    private readonly ExecutionPayloadLimits _payloadLimits =
        payloadLimits ?? new ExecutionPayloadLimits();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _active = [];
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

        foreach (OrchestrationAgentBinding binding in snapshot.Agents)
        {
            AgentDefinition? agent = await agents.GetByIdAsync(binding.AgentId, cancellationToken);
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
        await runs.SaveAsync(record, cancellationToken);
        await runs.SaveDetailsAsync(new OrchestrationRunDetails(
            record.Id,
            record.OrchestrationId,
            RedactContent(normalized),
            "",
            OrchestrationContractCloner.ReadOnly(Array.Empty<OrchestrationNodeAttemptRecord>())),
            cancellationToken);
        var source = new CancellationTokenSource();
        if (!_active.TryAdd(record.Id, source))
        {
            source.Dispose();
            throw new InvalidOperationException("Run identifier collision.");
        }
        _ = ExecuteGuardedAsync(record, snapshot, normalized, source);
        return OrchestrationOperationResult<OrchestrationRunRecord>.Success(record);
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
        if (!_active.TryGetValue(runId, out CancellationTokenSource? source))
            return await runs.GetAsync(runId, cancellationToken) is not null;
        await source.CancelAsync();
        return true;
    }

    public string? GetEphemeralOutput(Guid runId) =>
        _outputs.TryGetValue(runId, out string? value) ? value : null;

    private async Task ExecuteGuardedAsync(
        OrchestrationRunRecord record,
        OrchestrationVersionSnapshot snapshot,
        string initialInput,
        CancellationTokenSource source)
    {
        try
        {
            await ExecuteAsync(record, snapshot, initialInput, source.Token);
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
        }
    }

    private async Task ExecuteAsync(
        OrchestrationRunRecord initial,
        OrchestrationVersionSnapshot snapshot,
        string initialInput,
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
            (bool succeeded, string output, string errorCode) result =
                await ExecuteNodeAsync(initial.Id, node, nodeInput, cancellationToken);
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
                    await UpdateDetailsOutputAsync(initial.Id, RedactContent(previousOutput));
                    CacheOutput(initial.Id, previousOutput);
                }
                await FinishAsync(
                    initial.Id,
                    previousSucceeded ? OrchestrationRunStatus.Completed : OrchestrationRunStatus.Failed,
                    previousSucceeded ? "" : result.errorCode);
                return;
            }
            nodeId = edge.ToNodeId;
        }
        await FinishAsync(initial.Id, OrchestrationRunStatus.Failed, "ORCHESTRATION_STEP_LIMIT");
    }

    private async Task<(bool succeeded, string output, string errorCode)> ExecuteNodeAsync(
        Guid runId,
        OrchestrationNode node,
        string input,
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
            await UpsertAttemptAsync(runId, attemptRecord, cancellationToken);
            await UpdateNodeAsync(runId, node.Id, current => current with
            {
                Status = OrchestrationNodeRunStatus.Running,
                Attempts = attempt,
                StartedAtUtc = current.StartedAtUtc ?? started,
                InputSha256 = Hash(input),
                ErrorCode = ""
            });
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(node.TimeoutSeconds));
            try
            {
                AgentRunPreparationResult prepared =
                    await agentRuntime.PrepareAsync(node.AgentId, input, timeout.Token);
                if (!prepared.Succeeded)
                {
                    lastError = prepared.Error!.Code;
                    attemptRecord = attemptRecord with
                    {
                        Status = OrchestrationNodeRunStatus.Failed,
                        FinishedAtUtc = DateTimeOffset.UtcNow,
                        ErrorCode = lastError
                    };
                    await UpsertAttemptAsync(runId, attemptRecord, cancellationToken);
                    continue;
                }
                attemptRecord = attemptRecord with
                {
                    AgentRunId = prepared.Context!.RunId
                };
                await UpsertAttemptAsync(runId, attemptRecord, cancellationToken);
                var output = new StringBuilder();
                var toolCalls = new List<OrchestrationToolCallRecord>();
                bool failed = false;
                await foreach (AgentRunEvent value in agentRuntime.StreamAsync(prepared.Context, timeout.Token))
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
                            prepared.Context.RunId,
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
                                prepared.Context.RunId,
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
                    await UpsertAttemptAsync(runId, attemptRecord, timeout.Token);
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
                    await UpsertAttemptAsync(runId, attemptRecord, cancellationToken);
                    await UpdateNodeAsync(runId, node.Id, current => current with
                    {
                        Status = OrchestrationNodeRunStatus.Completed,
                        FinishedAtUtc = DateTimeOffset.UtcNow,
                        OutputCharacters = output.Length
                    });
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
                await UpsertAttemptAsync(runId, attemptRecord, cancellationToken);
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
                await UpsertAttemptAsync(runId, attemptRecord, CancellationToken.None);
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
                await UpsertAttemptAsync(runId, attemptRecord, CancellationToken.None);
                await UpdateNodeAsync(runId, node.Id, current => current with
                {
                    Status = OrchestrationNodeRunStatus.Failed,
                    FinishedAtUtc = DateTimeOffset.UtcNow,
                    ErrorCode = exception.ErrorCode
                });
                throw;
            }
            if (attempt <= node.MaximumRetries) continue;
        }
        await UpdateNodeAsync(runId, node.Id, current => current with
        {
            Status = OrchestrationNodeRunStatus.Failed,
            FinishedAtUtc = DateTimeOffset.UtcNow,
            ErrorCode = lastError.Length == 0 ? "ORCHESTRATION_NODE_FAILED" : lastError
        });
        return (false, "", lastError);
    }

    private async Task UpdateNodeAsync(
        Guid runId,
        string nodeId,
        Func<OrchestrationNodeRunRecord, OrchestrationNodeRunRecord> update)
    {
        OrchestrationRunRecord value = await runs.GetAsync(runId) ??
            throw new InvalidOperationException("Run disappeared.");
        await runs.SaveAsync(value with
        {
            Nodes = OrchestrationContractCloner.ReadOnly(value.Nodes.Select(node =>
                node.NodeId == nodeId ? update(node) : node))
        });
    }

    private async Task UpsertAttemptAsync(
        Guid runId,
        OrchestrationNodeAttemptRecord attempt,
        CancellationToken cancellationToken)
    {
        OrchestrationRunDetails details = await runs.GetDetailsAsync(runId, cancellationToken) ??
            throw new InvalidOperationException("Run details disappeared.");
        var values = details.Attempts
            .Where(value => value.NodeId != attempt.NodeId || value.Attempt != attempt.Attempt)
            .Append(attempt)
            .OrderBy(value => value.StartedAtUtc)
            .ThenBy(value => value.NodeId, StringComparer.Ordinal)
            .ThenBy(value => value.Attempt)
            .ToArray();
        await runs.SaveDetailsAsync(details with
        {
            Attempts = OrchestrationContractCloner.ReadOnly(values)
        }, cancellationToken);
    }

    private async Task UpdateDetailsOutputAsync(Guid runId, string output)
    {
        OrchestrationRunDetails details = await runs.GetDetailsAsync(runId) ??
            throw new InvalidOperationException("Run details disappeared.");
        await runs.SaveDetailsAsync(details with { Output = output });
    }

    private async Task FinishAsync(Guid runId, OrchestrationRunStatus status, string errorCode)
    {
        OrchestrationRunRecord? value = await runs.GetAsync(runId);
        if (value is null || value.Status != OrchestrationRunStatus.Running) return;
        OrchestrationNodeRunStatus nodeStatus = status == OrchestrationRunStatus.Cancelled
            ? OrchestrationNodeRunStatus.Cancelled : OrchestrationNodeRunStatus.Failed;
        await runs.SaveAsync(value with
        {
            Status = status,
            FinishedAtUtc = DateTimeOffset.UtcNow,
            ErrorCode = errorCode,
            Nodes = OrchestrationContractCloner.ReadOnly(value.Nodes.Select(node =>
                node.Status == OrchestrationNodeRunStatus.Running
                    ? node with { Status = nodeStatus, FinishedAtUtc = DateTimeOffset.UtcNow, ErrorCode = errorCode }
                    : node))
        });
    }

    private async Task<OrchestrationRunRecord?> RecoverIfInterruptedAsync(
        OrchestrationRunRecord? value,
        CancellationToken cancellationToken)
    {
        if (value is null || value.Status != OrchestrationRunStatus.Running || _active.ContainsKey(value.Id))
            return value;
        OrchestrationRunRecord recovered = value with
        {
            Status = OrchestrationRunStatus.Failed,
            FinishedAtUtc = DateTimeOffset.UtcNow,
            ErrorCode = "ORCHESTRATION_HOST_INTERRUPTED",
            Nodes = OrchestrationContractCloner.ReadOnly(value.Nodes.Select(node =>
                node.Status == OrchestrationNodeRunStatus.Running
                    ? node with
                    {
                        Status = OrchestrationNodeRunStatus.Failed,
                        FinishedAtUtc = DateTimeOffset.UtcNow,
                        ErrorCode = "ORCHESTRATION_HOST_INTERRUPTED"
                    }
                    : node))
        };
        await runs.SaveAsync(recovered, cancellationToken);
        return recovered;
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
}
