using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using EU.Core.Agent.Application.MainAgent;
using EU.Core.Agent.Application.Orchestration;
using EU.Core.Agent.Application.Runtime;

namespace EU.Core.Agent.Application.UnifiedEntry;

public sealed record UnifiedEntryError(string Code, string Message);

public sealed record UnifiedEntryPreparationResult(
    UnifiedEntryContext? Context,
    UnifiedEntryError? Error)
{
    public bool Succeeded => Error is null;

    public static UnifiedEntryPreparationResult Success(UnifiedEntryContext context) =>
        new(context, null);

    public static UnifiedEntryPreparationResult Failure(
        string code,
        string message) =>
        new(null, new UnifiedEntryError(code, message));
}

public sealed record UnifiedRunEvent(
    Guid RunId,
    Guid ConversationId,
    long Sequence,
    string Kind,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId,
    Guid? ParentRunId,
    int Depth,
    string PayloadJson)
{
    public string Route { get; init; } = string.Empty;
}

public sealed class UnifiedEntryContext
{
    internal UnifiedEntryContext(
        AgentRunContext mainAgentContext,
        ActiveUnifiedEntryExecution execution)
    {
        MainAgentContext = mainAgentContext
            ?? throw new ArgumentNullException(nameof(mainAgentContext));
        Execution = execution
            ?? throw new ArgumentNullException(nameof(execution));
    }

    internal ActiveUnifiedEntryExecution Execution { get; }

    public Guid RunId => Execution.RunId;

    public Guid ConversationId => Execution.ConversationId;

    public Guid CorrelationId => Execution.Scope.CorrelationId;

    public AgentRunContext MainAgentContext { get; }
}

internal enum UnifiedEntryLifecycleState
{
    Prepared,
    RuntimeOwned,
    Finalizing,
    Retired
}

internal sealed class ActiveUnifiedEntryExecution(
    Guid runId,
    Guid conversationId,
    CancellationTokenSource cancellation,
    UnifiedEntryExecutionScope scope,
    UnifiedAgentExecutionLease mainLease,
    AgentRunContext mainContext)
{
    private int _streamStarted;
    private int _lifecycleState = (int)UnifiedEntryLifecycleState.Prepared;
    private int _recoveryScheduled;

    public Guid RunId { get; } = runId;

    public Guid ConversationId { get; } = conversationId;

    public CancellationTokenSource Cancellation { get; } = cancellation;

    public UnifiedEntryExecutionScope Scope { get; } = scope;

    public UnifiedAgentExecutionLease MainLease { get; } = mainLease;

    public AgentRunContext MainContext { get; } = mainContext;

    public SemaphoreSlim TerminalGate { get; } = new(1, 1);

    public UnifiedEntryAggregate? TerminalSnapshot { get; set; }

    public bool TerminalPersisted { get; set; }

    public UnifiedRunStatus TerminalStatus { get; set; }

    public string TerminalErrorCode { get; set; } = string.Empty;

    public bool TryStartStream() =>
        Interlocked.CompareExchange(ref _streamStarted, 1, 0) == 0;

    public TaskCompletionSource PrimaryFinalizationCompleted { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public UnifiedEntryLifecycleState LifecycleState =>
        (UnifiedEntryLifecycleState)Volatile.Read(ref _lifecycleState);

    public bool TryClaimRuntimeOwnership() =>
        Interlocked.CompareExchange(
            ref _lifecycleState,
            (int)UnifiedEntryLifecycleState.RuntimeOwned,
            (int)UnifiedEntryLifecycleState.Prepared)
        == (int)UnifiedEntryLifecycleState.Prepared;

    public bool TryClaimPreparedFinalization() =>
        TryTransitionToFinalizing(UnifiedEntryLifecycleState.Prepared);

    public bool TryClaimRuntimeFinalization() =>
        TryTransitionToFinalizing(UnifiedEntryLifecycleState.RuntimeOwned);

    public void MarkRetired() =>
        Interlocked.Exchange(
            ref _lifecycleState,
            (int)UnifiedEntryLifecycleState.Retired);

    public bool TryScheduleRecovery() =>
        Interlocked.CompareExchange(ref _recoveryScheduled, 1, 0) == 0;

    private bool TryTransitionToFinalizing(UnifiedEntryLifecycleState expected)
    {
        bool claimed = Interlocked.CompareExchange(
            ref _lifecycleState,
            (int)UnifiedEntryLifecycleState.Finalizing,
            (int)expected) == (int)expected;
        return claimed;
    }
}
