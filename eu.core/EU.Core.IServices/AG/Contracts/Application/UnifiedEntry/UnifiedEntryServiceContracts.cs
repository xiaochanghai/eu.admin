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

    #region 处理（Success）
    /// <summary>
    /// 处理（Success）
    /// </summary>
    /// <param name="context">统一入口执行上下文，包含执行范围和主 Agent 运行信息。</param>
    /// <returns>包含执行上下文且无错误信息的统一入口准备成功结果。</returns>
    public static UnifiedEntryPreparationResult Success(UnifiedEntryContext context) =>
        new(context, null);
    #endregion

    #region 处理（Failure）
    /// <summary>
    /// 处理（Failure）
    /// </summary>
    /// <param name="code">对象编码或业务错误码。</param>
    /// <param name="message">消息或提示文本。</param>
    /// <returns>包含指定错误码和消息、不含执行上下文的统一入口准备失败结果。</returns>
    public static UnifiedEntryPreparationResult Failure(string code, string message) =>
        new(null, new UnifiedEntryError(code, message));
    #endregion
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
    #region 构造（UnifiedEntryContext）
    /// <summary>
    /// 构造（UnifiedEntryContext）
    /// </summary>
    /// <param name="mainAgentContext">主 Agent 的执行上下文。</param>
    /// <param name="execution">当前执行对象。</param>
    internal UnifiedEntryContext(AgentRunContext mainAgentContext, ActiveUnifiedEntryExecution execution)
    {
        MainAgentContext = mainAgentContext
            ?? throw new ArgumentNullException(nameof(mainAgentContext));
        Execution = execution
            ?? throw new ArgumentNullException(nameof(execution));
    }
    #endregion

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

/// <summary>
/// 跟踪统一入口运行的执行资源及生命周期状态。
/// </summary>
/// <param name="runId">关联的运行记录标识。</param>
/// <param name="conversationId">关联的会话标识。</param>
/// <param name="cancellation">控制本次统一入口执行取消的令牌源。</param>
/// <param name="scope">承载本次统一入口执行身份和资源的作用域。</param>
/// <param name="mainLease">主 Agent 执行所持有的租约。</param>
/// <param name="mainContext">主 Agent 的运行上下文。</param>
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

    #region 原子地认领事件流启动权（TryStartStream）
    /// <summary>
    /// 原子地认领事件流启动权（TryStartStream）。
    /// </summary>
    /// <returns>本次首次将事件流标记为已启动时返回 true；已经标记为启动时返回 false。</returns>
    public bool TryStartStream() =>
        Interlocked.CompareExchange(ref _streamStarted, 1, 0) == 0;
    #endregion

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

    #region 原子地认领运行执行权（TryClaimRuntimeOwnership）
    /// <summary>
    /// 原子地认领运行执行权（TryClaimRuntimeOwnership）。
    /// </summary>
    /// <returns>成功将 Prepared 状态转换为 RuntimeOwned 时返回 true；当前状态不是 Prepared 时返回 false。</returns>
    public bool TryClaimRuntimeOwnership() =>
        Interlocked.CompareExchange(
            ref _lifecycleState,
            (int)UnifiedEntryLifecycleState.RuntimeOwned,
            (int)UnifiedEntryLifecycleState.Prepared)
        == (int)UnifiedEntryLifecycleState.Prepared;
    #endregion

    #region 认领尚未启动运行的收尾权（TryClaimPreparedFinalization）
    /// <summary>
    /// 认领尚未启动运行的收尾权（TryClaimPreparedFinalization）。
    /// </summary>
    /// <returns>成功将 Prepared 状态转换为 Finalizing 时返回 true；状态不匹配时返回 false。</returns>
    public bool TryClaimPreparedFinalization() =>
        TryTransitionToFinalizing(UnifiedEntryLifecycleState.Prepared);
    #endregion

    #region 认领运行中的收尾权（TryClaimRuntimeFinalization）
    /// <summary>
    /// 认领运行中的收尾权（TryClaimRuntimeFinalization）。
    /// </summary>
    /// <returns>成功将 RuntimeOwned 状态转换为 Finalizing 时返回 true；状态不匹配时返回 false。</returns>
    public bool TryClaimRuntimeFinalization() =>
        TryTransitionToFinalizing(UnifiedEntryLifecycleState.RuntimeOwned);
    #endregion

    #region 处理（MarkRetired）
    /// <summary>
    /// 处理（MarkRetired）
    /// </summary>
    public void MarkRetired() =>
        Interlocked.Exchange(
            ref _lifecycleState,
            (int)UnifiedEntryLifecycleState.Retired);
    #endregion

    #region 原子地认领恢复调度权（TryScheduleRecovery）
    /// <summary>
    /// 原子地认领恢复调度权（TryScheduleRecovery）。
    /// </summary>
    /// <returns>本次首次将恢复标记设为已调度时返回 true；已被认领时返回 false；本方法不实际执行恢复任务。</returns>
    public bool TryScheduleRecovery() =>
        Interlocked.CompareExchange(ref _recoveryScheduled, 1, 0) == 0;
    #endregion

    #region 按预期生命周期状态认领收尾权（TryTransitionToFinalizing）
    /// <summary>
    /// 按预期生命周期状态认领收尾权（TryTransitionToFinalizing）。
    /// </summary>
    /// <param name="expected">认领收尾权所要求的当前生命周期状态。</param>
    /// <returns>当前生命周期状态等于 expected 且成功转换为 Finalizing 时返回 true，否则返回 false。</returns>
    private bool TryTransitionToFinalizing(UnifiedEntryLifecycleState expected)
    {
        bool claimed = Interlocked.CompareExchange(
            ref _lifecycleState,
            (int)UnifiedEntryLifecycleState.Finalizing,
            (int)expected) == (int)expected;
        return claimed;
    }
    #endregion
}
