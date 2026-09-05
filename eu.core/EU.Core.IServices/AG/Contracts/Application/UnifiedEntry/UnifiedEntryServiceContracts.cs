#nullable enable

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using EU.Core.IServices.MainAgent;
using EU.Core.IServices.Orchestration;
using EU.Core.IServices.Runtime;

namespace EU.Core.IServices.UnifiedEntry;

/// <summary>
/// 统一入口准备或执行错误。
/// </summary>
/// <param name="Code">业务唯一编码或检查项编码。</param>
/// <param name="Message">面向调用方的错误说明。</param>
public sealed record UnifiedEntryError(string Code, string Message);

/// <summary>
/// 统一入口运行准备结果。
/// </summary>
/// <param name="Context">准备完成的统一入口运行上下文。</param>
/// <param name="Error">运行准备失败信息。</param>
public sealed record UnifiedEntryPreparationResult(
    UnifiedEntryContext? Context,
    UnifiedEntryError? Error)
{
    /// <summary>
    /// 获取操作是否成功。
    /// </summary>
    public bool Succeeded => Error is null;

    public static UnifiedEntryPreparationResult Success(UnifiedEntryContext context) =>
        new(context, null);

    public static UnifiedEntryPreparationResult Failure(
        string code,
        string message) =>
        new(null, new UnifiedEntryError(code, message));
}

/// <summary>
/// 统一入口向调用方发布的运行事件。
/// </summary>
/// <param name="RunId">运行标识。</param>
/// <param name="ConversationId">关联会话标识。</param>
/// <param name="Sequence">运行事件的顺序号。</param>
/// <param name="Kind">运行或事件类型。</param>
/// <param name="OccurredAtUtc">事件发生的 UTC 时间。</param>
/// <param name="CorrelationId">用于关联完整执行链路的标识。</param>
/// <param name="ParentRunId">父级 Agent 或编排运行标识。</param>
/// <param name="Depth">在统一执行树中的嵌套深度。</param>
/// <param name="PayloadJson">事件附带的数据 JSON。</param>
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
    /// <summary>
    /// 本次执行采用的路由。
    /// </summary>
    public string Route { get; init; } = string.Empty;
}

/// <summary>
/// 封装一次统一入口运行所需的服务和执行上下文。
/// </summary>
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

    /// <summary>
    /// 获取统一入口运行标识。
    /// </summary>
    public Guid RunId => Execution.RunId;

    /// <summary>
    /// 获取会话标识。
    /// </summary>
    public Guid ConversationId => Execution.ConversationId;

    /// <summary>
    /// 获取关联标识。
    /// </summary>
    public Guid CorrelationId => Execution.Scope.CorrelationId;

    /// <summary>
    /// 获取主 Agent 执行上下文。
    /// </summary>
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

    /// <summary>
    /// 获取统一入口运行标识。
    /// </summary>
    public Guid RunId { get; } = runId;

    /// <summary>
    /// 获取会话标识。
    /// </summary>
    public Guid ConversationId { get; } = conversationId;

    /// <summary>
    /// 获取统一入口执行使用的取消令牌源。
    /// </summary>
    public CancellationTokenSource Cancellation { get; } = cancellation;

    /// <summary>
    /// 获取统一入口执行范围。
    /// </summary>
    public UnifiedEntryExecutionScope Scope { get; } = scope;

    /// <summary>
    /// 获取主 Agent 执行租约。
    /// </summary>
    public UnifiedAgentExecutionLease MainLease { get; } = mainLease;

    /// <summary>
    /// 获取主 Agent 执行上下文。
    /// </summary>
    public AgentRunContext MainContext { get; } = mainContext;

    public SemaphoreSlim TerminalGate { get; } = new(1, 1);

    /// <summary>
    /// 运行终态快照。
    /// </summary>
    public UnifiedEntryAggregate? TerminalSnapshot { get; set; }

    /// <summary>
    /// 终态是否已成功持久化。
    /// </summary>
    public bool TerminalPersisted { get; set; }

    /// <summary>
    /// 运行终态。
    /// </summary>
    public UnifiedRunStatus TerminalStatus { get; set; }

    /// <summary>
    /// 终态错误代码。
    /// </summary>
    public string TerminalErrorCode { get; set; } = string.Empty;

    public bool TryStartStream() =>
        Interlocked.CompareExchange(ref _streamStarted, 1, 0) == 0;

    /// <summary>
    /// 获取或设置主终结流程是否完成。
    /// </summary>
    public TaskCompletionSource PrimaryFinalizationCompleted { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// 获取或设置当前生命周期状态。
    /// </summary>
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
