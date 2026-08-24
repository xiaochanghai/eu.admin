using System.Text.Json;
using EU.Core.Api.Agent.Configuration;
using EU.Core.IServices;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.Tasks;
using EU.Core.IServices.UnifiedEntry;
using EU.Core.Services;
using Microsoft.Extensions.Options;

#nullable enable

namespace EU.Core.Api.Agent.Background;

public sealed class ChatAgentTaskExecutor(
    IAgAgentTaskServices tasks,
    IUnifiedEntryRepository repository,
    UnifiedEntryService unifiedEntry,
    IOptions<AgentTaskWorkerOptions> options,
    TimeProvider timeProvider,
    ILogger<ChatAgentTaskExecutor> logger) : IAgentTaskExecutor
{
    private readonly AgentTaskWorkerOptions _options = options.Value;
    public string SourceType => "chat";

    public async Task ExecuteAsync(AgentTaskExecutionContext execution, CancellationToken cancellationToken)
    {
        AgentTaskRecord task = execution.Task;
        if (await ReconcileExistingRunAsync(execution, cancellationToken)) return;

        var identity = new AgentExecutionIdentity(
            task.UserId,
            task.TenantId,
            new HashSet<string>(_options.ExecutionPermissions ?? [], StringComparer.Ordinal),
            $"agent-task:{task.Id:D}");
        UnifiedEntryPreparationResult preparation = await unifiedEntry.PrepareAsync(
            task.Input, task.ConversationId, identity, cancellationToken);
        if (!preparation.Succeeded)
        {
            await FailAsync(execution, task, preparation.Error!.Code, preparation.Error.Message, cancellationToken);
            return;
        }

        UnifiedEntryContext context = preparation.Context!;
        task = await tasks.SaveCheckpointAsync(new SaveAgentTaskCheckpointCommand(
            task.Id, task.TenantId, execution.WorkerId, task.LogicalRevision, context.RunId,
            context.ConversationId, "unified-entry-prepared",
            JsonSerializer.Serialize(new { runId = context.RunId, conversationId = context.ConversationId }),
            timeProvider.GetUtcNow()), cancellationToken);

        using var executionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Exception? heartbeatFailure = null;
        Task heartbeat = RenewLeaseLoopAsync(execution, task, executionCancellation,
            value => task = value, exception => heartbeatFailure = exception);
        bool approvalRequired = false;
        bool userInputRequired = false;
        try
        {
            await foreach (UnifiedRunEvent runEvent in unifiedEntry.StreamAsync(context, executionCancellation.Token)
                .WithCancellation(executionCancellation.Token))
            {
                approvalRequired |= string.Equals(runEvent.Kind, "approval-required", StringComparison.Ordinal);
                userInputRequired |= string.Equals(runEvent.Kind, "user-input-required", StringComparison.Ordinal);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException) when (heartbeatFailure is not null)
        {
            logger.LogWarning(heartbeatFailure, "Agent task {TaskId} lost its execution lease.", task.Id);
            return;
        }
        catch (Exception exception)
        {
            executionCancellation.Cancel();
            await AwaitHeartbeatAsync(heartbeat);
            logger.LogError(exception,
                "Deferred Agent task {TaskId} run {RunId} failed.", task.Id, context.RunId);
            await FailAsync(execution, task, "AGENT_TASK_EXECUTION_FAILED",
                "The deferred Agent execution failed.", cancellationToken);
            return;
        }
        finally
        {
            executionCancellation.Cancel();
            await AwaitHeartbeatAsync(heartbeat);
        }

        if (heartbeatFailure is not null) return;
        if (userInputRequired)
        {
            await tasks.WaitAsync(new WaitAgentTaskCommand(
                task.Id, task.TenantId, execution.WorkerId, task.LogicalRevision,
                AgentTaskStatus.WaitingForUser, context.RunId, context.ConversationId,
                "user-input-required", JsonSerializer.Serialize(new { runId = context.RunId }),
                timeProvider.GetUtcNow()), cancellationToken);
            return;
        }

        if (approvalRequired)
        {
            await tasks.WaitAsync(new WaitAgentTaskCommand(
                task.Id, task.TenantId, execution.WorkerId, task.LogicalRevision,
                AgentTaskStatus.WaitingForApproval, context.RunId, context.ConversationId,
                "approval-required", JsonSerializer.Serialize(new { runId = context.RunId }),
                timeProvider.GetUtcNow()), cancellationToken);
            return;
        }

        UnifiedEntryRunRecord? run = await repository.GetRunAsync(context.RunId, cancellationToken);
        if (run?.Status == UnifiedRunStatus.Completed)
        {
            await tasks.CompleteAsync(new CompleteAgentTaskCommand(
                task.Id, task.TenantId, execution.WorkerId, task.LogicalRevision,
                context.RunId, timeProvider.GetUtcNow()), cancellationToken);
            return;
        }

        if (run?.Status == UnifiedRunStatus.Cancelled)
        {
            AgentTaskRecord? current = await tasks.GetAsync(task.Id, task.TenantId, null, cancellationToken);
            if (current?.Status != AgentTaskStatus.Cancelled)
            {
                await tasks.CancelAsync(task.Id, task.TenantId, task.UserId, timeProvider.GetUtcNow(), cancellationToken);
            }
            return;
        }

        await FailAsync(execution, task, run?.ErrorCode ?? "AGENT_TASK_EXECUTION_FAILED",
            "The deferred Agent execution did not complete successfully.", cancellationToken);
    }

    private async Task<bool> ReconcileExistingRunAsync(
        AgentTaskExecutionContext execution,
        CancellationToken cancellationToken)
    {
        AgentTaskRecord task = execution.Task;
        if (!task.CurrentRunId.HasValue) return false;

        UnifiedEntryRunRecord? run = await repository.GetRunAsync(task.CurrentRunId.Value, cancellationToken);
        if (run is null)
        {
            await FailAsync(execution, task, "UNIFIED_ENTRY_RUN_NOT_FOUND",
                "The persisted Agent run could not be recovered.", cancellationToken);
            return true;
        }

        switch (run.Status)
        {
            case UnifiedRunStatus.Completed:
                await tasks.CompleteAsync(new CompleteAgentTaskCommand(
                    task.Id, task.TenantId, execution.WorkerId, task.LogicalRevision,
                    run.Id, timeProvider.GetUtcNow()), cancellationToken);
                return true;
            case UnifiedRunStatus.WaitingForApproval:
                await tasks.WaitAsync(new WaitAgentTaskCommand(
                    task.Id, task.TenantId, execution.WorkerId, task.LogicalRevision,
                    AgentTaskStatus.WaitingForApproval, run.Id, run.ConversationId,
                    "approval-required", JsonSerializer.Serialize(new { runId = run.Id }),
                    timeProvider.GetUtcNow()), cancellationToken);
                return true;
            case UnifiedRunStatus.Cancelled:
                await tasks.CancelAsync(task.Id, task.TenantId, task.UserId,
                    timeProvider.GetUtcNow(), cancellationToken);
                return true;
            case UnifiedRunStatus.Failed:
            case UnifiedRunStatus.Blocked:
                await FailAsync(execution, task,
                    string.IsNullOrWhiteSpace(run.ErrorCode) ? "AGENT_TASK_EXECUTION_FAILED" : run.ErrorCode,
                    "The previous Agent run did not complete successfully.", cancellationToken);
                return true;
            default:
                await FailAsync(execution, task, "UNIFIED_ENTRY_HOST_INTERRUPTED",
                    "The previous Agent run was interrupted before its outcome was persisted.", cancellationToken);
                return true;
        }
    }

    private async Task RenewLeaseLoopAsync(
        AgentTaskExecutionContext execution,
        AgentTaskRecord initial,
        CancellationTokenSource executionCancellation,
        Action<AgentTaskRecord> update,
        Action<Exception> fail)
    {
        AgentTaskRecord current = initial;
        TimeSpan interval = TimeSpan.FromTicks(execution.LeaseDuration.Ticks / 3);
        try
        {
            while (true)
            {
                await Task.Delay(interval, executionCancellation.Token);
                current = await tasks.RenewLeaseAsync(new RenewAgentTaskLeaseCommand(
                    current.Id, current.TenantId, execution.WorkerId, current.LogicalRevision,
                    execution.LeaseDuration, timeProvider.GetUtcNow()), executionCancellation.Token);
                update(current);
            }
        }
        catch (OperationCanceledException) when (executionCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            fail(exception);
            executionCancellation.Cancel();
        }
    }

    private Task FailAsync(
        AgentTaskExecutionContext execution,
        AgentTaskRecord task,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken) =>
        tasks.FailAsync(new FailAgentTaskCommand(
            task.Id, task.TenantId, execution.WorkerId, task.LogicalRevision,
            errorCode, errorMessage,
            TimeSpan.FromSeconds(Math.Clamp(_options.RetryDelaySeconds, 0, 86_400)),
            timeProvider.GetUtcNow()), cancellationToken);

    private static async Task AwaitHeartbeatAsync(Task heartbeat)
    {
        try { await heartbeat; }
        catch (OperationCanceledException) { }
    }
}
