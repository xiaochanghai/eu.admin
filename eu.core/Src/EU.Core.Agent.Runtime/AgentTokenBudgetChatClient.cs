using System.Runtime.CompilerServices;
using EU.Core.IServices.Runtime;
using Microsoft.Extensions.AI;

namespace EU.Core.Agent.Runtime;

/// <summary>单个 Agent 运行的模型预算客户端；位于 SDK 工具循环内侧，可接受当前执行树共享额度。</summary>
/// <remarks>累计预算依赖供应商事后报告的总 Token；不估算提示词，不保证首次请求的预付费硬上限。</remarks>
internal sealed class AgentTokenBudgetChatClient : DelegatingChatClient
{
    private readonly int? _maximumOutputTokens;
    private readonly long? _maximumTotalTokens;
    private readonly IAgentModelTokenBudget? _sharedBudget;
    private readonly AgentTokenBudgetMonitor _runMonitor;
    private readonly AgentTokenBudgetMonitor _outputMonitor;
    private readonly SemaphoreSlim _requests = new(1, 1);
    private long _usedTokens;
    private string? _denialCode;

    #region 初始化单次运行预算
    /// <summary>创建只属于当前模型循环的预算；空值保持现有调用行为，非正值拒绝配置。</summary>
    /// <param name="innerClient">执行单次供应商请求的客户端，不应已包含另一层工具循环。</param>
    /// <param name="options">当前宿主的运行预算配置。</param>
    /// <param name="sharedBudget">同一执行树的预算实例；null 不改变独立运行行为，不由本客户端释放。</param>
    /// <param name="telemetry">宿主可选预算监控，不改变拦截及响应语义。</param>
    public AgentTokenBudgetChatClient(IChatClient innerClient, AgentRuntimeOptions options, IAgentModelTokenBudget? sharedBudget = null, IAgentRuntimeTelemetry? telemetry = null) : base(innerClient)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.MaximumModelOutputTokens is < 1) throw new ArgumentOutOfRangeException(nameof(options.MaximumModelOutputTokens));
        if (options.MaximumRunTotalTokens is < 1) throw new ArgumentOutOfRangeException(nameof(options.MaximumRunTotalTokens));
        _maximumOutputTokens = options.MaximumModelOutputTokens;
        _maximumTotalTokens = options.MaximumRunTotalTokens;
        _sharedBudget = sharedBudget;
        _runMonitor = new(AgentTokenBudgetScope.AgentRun, options.TokenBudgetWarningPercent, telemetry);
        _outputMonitor = new(AgentTokenBudgetScope.ModelOutput, options.TokenBudgetWarningPercent, telemetry);
    }
    #endregion

    #region 执行非流式模型请求（GetResponseAsync）
    /// <summary>应用剩余预算并核对单次响应的实际用量，不修改调用方的选项对象。</summary>
    /// <param name="messages">本轮模型输入。</param>
    /// <param name="options">SDK 提供的请求选项，保留指令、工具和供应商扩展。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <returns>通过预算检查的原始响应。</returns>
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        await _requests.WaitAsync(cancellationToken).ConfigureAwait(false);
        bool finished = false;
        bool started = false;
        IAgentModelTokenBudgetLease? sharedLease = null;
        try
        {
            ChatOptions? requestOptions = PrepareOptions(options);
            sharedLease = _sharedBudget is null ? null : await _sharedBudget.ReserveAsync(cancellationToken).ConfigureAwait(false);
            requestOptions = ApplySharedLimit(requestOptions, sharedLease);
            sharedLease?.MarkStarted();
            started = true;
            ChatResponse response = await base.GetResponseAsync(messages, requestOptions, cancellationToken).ConfigureAwait(false);
            finished = true;
            ObserveResponse(response.Usage?.TotalTokenCount, response.Usage?.OutputTokenCount,
                requestOptions?.MaxOutputTokens, response.FinishReason, response.Usage?.TotalTokenCount is < 0);
            sharedLease?.Complete(response.Usage?.TotalTokenCount, response.Usage?.TotalTokenCount is < 0);
            CompleteResponse(response.Usage?.TotalTokenCount, response.Usage?.OutputTokenCount,
                requestOptions?.MaxOutputTokens, response.FinishReason, response.Usage?.TotalTokenCount is < 0);
            return response;
        }
        finally
        {
            sharedLease?.Dispose();
            if (started && !finished && _maximumTotalTokens.HasValue) _runMonitor.Record(AgentTokenBudgetSignal.UsageUnavailable);
            if (!finished && _maximumTotalTokens.HasValue) _denialCode ??= AgentRunErrorCodes.ModelTokenUsageUnavailable;
            _requests.Release();
        }
    }
    #endregion

    #region 执行流式模型请求（GetStreamingResponseAsync）
    /// <summary>合并单次响应的累计 usage 快照；先交付用量再报告预算失败，供运行服务保存终态审计。</summary>
    /// <param name="messages">本轮模型输入。</param>
    /// <param name="options">本轮请求选项，克隆后设置输出 Token 限制。</param>
    /// <param name="cancellationToken">流读取及模型请求取消令牌。</param>
    /// <returns>原始响应更新；预算失败时在尾部抛出领域异常。</returns>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await _requests.WaitAsync(cancellationToken).ConfigureAwait(false);
        bool finished = false;
        bool started = false;
        IAgentModelTokenBudgetLease? sharedLease = null;
        bool invalidTotal = false;
        ChatFinishReason? finishReason = null;
        var usage = new ModelUsageAccumulator();
        try
        {
            ChatOptions? requestOptions = PrepareOptions(options);
            sharedLease = _sharedBudget is null ? null : await _sharedBudget.ReserveAsync(cancellationToken).ConfigureAwait(false);
            requestOptions = ApplySharedLimit(requestOptions, sharedLease);
            sharedLease?.MarkStarted();
            started = true;
            await foreach (ChatResponseUpdate update in base.GetStreamingResponseAsync(messages, requestOptions, cancellationToken).ConfigureAwait(false))
            {
                UsageDetails[] reports = update.Contents.OfType<UsageContent>().Select(value => value.Details).ToArray();
                // 此层一次调用对应一个供应商响应，响应 ID 缺失也不必猜测调用次数。
                usage.Observe("response", reports);
                invalidTotal |= reports.Any(value => value.TotalTokenCount is < 0);
                finishReason = update.FinishReason ?? finishReason;
                yield return update;
            }
            finished = true;
            AgentModelUsage snapshot = usage.Snapshot(true, 0, null);
            ObserveResponse(snapshot.TotalTokens, snapshot.OutputTokens, requestOptions?.MaxOutputTokens, finishReason, invalidTotal);
            sharedLease?.Complete(snapshot.TotalTokens, invalidTotal);
            CompleteResponse(snapshot.TotalTokens, snapshot.OutputTokens, requestOptions?.MaxOutputTokens, finishReason, invalidTotal);
        }
        finally
        {
            sharedLease?.Dispose();
            if (started && !finished && _maximumTotalTokens.HasValue) _runMonitor.Record(AgentTokenBudgetSignal.UsageUnavailable);
            // 失败、取消或提前释放后的消耗不能视为零，也不能在同一预算下继续重试。
            if (!finished && _maximumTotalTokens.HasValue) _denialCode ??= AgentRunErrorCodes.ModelTokenUsageUnavailable;
            _requests.Release();
        }
    }
    #endregion

    #region 按执行树剩余额度收紧输出（ApplySharedLimit）
    /// <summary>不放宽单运行或已有输出限制；只限制输出，不估算下一请求的输入用量。</summary>
    /// <param name="options">已经应用单运行预算的请求选项。</param>
    /// <param name="lease">当前供应商请求的执行树预算租约。</param>
    /// <returns>启用共享限制时返回克隆选项，否则返回原对象。</returns>
    private static ChatOptions? ApplySharedLimit(ChatOptions? options, IAgentModelTokenBudgetLease? lease)
    {
        if (lease is null) return options;
        int remaining = (int)Math.Min(int.MaxValue, lease.RemainingTokens);
        ChatOptions result = options?.Clone() ?? new ChatOptions();
        result.MaxOutputTokens = result.MaxOutputTokens is int existing ? Math.Min(existing, remaining) : remaining;
        return result;
    }
    #endregion

    #region 按剩余额度准备请求（PrepareOptions）
    /// <summary>累计阈值耗尽时不发出下一次请求；限制输出但不将剩余额度冒充输入 Token 估算。</summary>
    /// <param name="options">调用方选项；已有更小的输出限制不会被放宽。</param>
    /// <returns>需要调整时返回克隆的选项，否则返回原选项。</returns>
    private ChatOptions? PrepareOptions(ChatOptions? options)
    {
        if (_denialCode is not null) throw Failure(_denialCode);
        int? maximum = _maximumOutputTokens;
        if (_maximumTotalTokens is long total)
        {
            long remaining = total - _usedTokens;
            if (remaining <= 0) throw Deny(AgentRunErrorCodes.ModelTokenBudgetExceeded);
            int remainingOutput = (int)Math.Min(int.MaxValue, remaining);
            maximum = maximum.HasValue ? Math.Min(maximum.Value, remainingOutput) : remainingOutput;
        }
        if (maximum is null) return options;
        ChatOptions result = options?.Clone() ?? new ChatOptions();
        result.MaxOutputTokens = result.MaxOutputTokens is int existing ? Math.Min(existing, maximum.Value) : maximum;
        return result;
    }
    #endregion

    #region 独立观察本轮预算信号（ObserveResponse）
    /// <summary>在任一预算抛错前观察所有已启用范围，不重复收费，不决定请求结果。</summary>
    /// <param name="totalTokens">供应商报告的单次总用量。</param>
    /// <param name="outputTokens">供应商报告的单次输出用量。</param>
    /// <param name="outputLimit">本轮实际发送的输出上限。</param>
    /// <param name="finishReason">供应商结束原因。</param>
    /// <param name="invalidTotal">是否曾收到负总用量。</param>
    private void ObserveResponse(long? totalTokens, long? outputTokens, int? outputLimit, ChatFinishReason? finishReason, bool invalidTotal)
    {
        bool limited = _maximumOutputTokens.HasValue || _maximumTotalTokens.HasValue || _sharedBudget is not null;
        if (limited && outputTokens is >= 0 && outputLimit is > 0) _outputMonitor.Observe(outputTokens.Value, outputLimit.Value);
        if (limited && finishReason == ChatFinishReason.Length) _outputMonitor.Record(AgentTokenBudgetSignal.OutputLimited);
        if (_maximumTotalTokens is not long maximum) return;
        if (invalidTotal || totalTokens is null or < 0) _runMonitor.Record(AgentTokenBudgetSignal.UsageUnavailable);
        else if (totalTokens.Value > maximum - _usedTokens) _runMonitor.Record(AgentTokenBudgetSignal.Exceeded);
        else _runMonitor.Observe(_usedTokens + totalTokens.Value, maximum);
    }
    #endregion

    #region 核对单次响应用量（CompleteResponse）
    /// <summary>只累计可信的总 Token，缺失统计时关闭后续调用；被截断的输出不能伪报完成。</summary>
    /// <param name="totalTokens">供应商报告的本轮总用量。</param>
    /// <param name="outputTokens">供应商报告的本轮输出用量。</param>
    /// <param name="outputLimit">实际发送的输出 Token 限制。</param>
    /// <param name="finishReason">供应商结束原因。</param>
    /// <param name="invalidTotal">本轮是否收到非法的负总用量。</param>
    private void CompleteResponse(long? totalTokens, long? outputTokens, int? outputLimit, ChatFinishReason? finishReason, bool invalidTotal)
    {
        if (_maximumTotalTokens is long maximum)
        {
            if (invalidTotal || totalTokens is null or < 0) throw Deny(AgentRunErrorCodes.ModelTokenUsageUnavailable);
            if (totalTokens.Value > maximum - _usedTokens) throw Deny(AgentRunErrorCodes.ModelTokenBudgetExceeded);
            _usedTokens += totalTokens.Value;
        }
        if ((_maximumOutputTokens.HasValue || _maximumTotalTokens.HasValue || _sharedBudget is not null)
            && (finishReason == ChatFinishReason.Length || (outputLimit.HasValue && outputTokens > outputLimit.Value)))
            throw Deny(AgentRunErrorCodes.ModelTokenBudgetExceeded);
    }
    #endregion

    #region 记录预算拒绝（Deny）
    /// <summary>记住预算拒绝，防止 SDK 或调用方在当前运行内重试绕过。</summary>
    /// <param name="errorCode">固定领域错误码。</param>
    /// <returns>不包含输入、地址或凭据的领域异常。</returns>
    private AgentRuntimeException Deny(string errorCode)
    {
        _denialCode = errorCode;
        return Failure(errorCode);
    }
    #endregion

    #region 创建固定预算错误（Failure）
    /// <summary>创建可安全传给审计及事件的预算错误。</summary>
    /// <param name="errorCode">超限或用量不可用错误码。</param>
    /// <returns>固定消息的领域异常。</returns>
    private static AgentRuntimeException Failure(string errorCode) => new(errorCode,
        errorCode == AgentRunErrorCodes.ModelTokenUsageUnavailable
            ? "The model did not report usable total Token usage for the enabled run budget."
            : "The configured model Token budget was exceeded or exhausted.");
    #endregion

    #region 释放预算客户端（Dispose）
    /// <summary>运行结束后释放请求串行门及所拥有的供应商客户端。</summary>
    /// <param name="disposing">是否释放托管资源。</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing) _requests.Dispose();
        base.Dispose(disposing);
    }
    #endregion
}
