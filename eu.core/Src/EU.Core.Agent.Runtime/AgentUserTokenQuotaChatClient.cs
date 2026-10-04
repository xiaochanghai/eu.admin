using System.Runtime.CompilerServices;
using EU.Core.IServices.Runtime;
using Microsoft.Extensions.AI;

namespace EU.Core.Agent.Runtime;

/// <summary>位于 SDK 工具循环最内层，按真实供应商请求计费，而不是按父/子运行审计重复计费。</summary>
internal sealed class AgentUserTokenQuotaChatClient(IChatClient innerClient, IAgentUserTokenQuota? quota, AgentRunContext context) : DelegatingChatClient(innerClient)
{
    #region 执行非流式请求（GetResponseAsync）
    /// <summary>请求前持久化预占，结束后按实际用量结算；依赖失败则不调用供应商。</summary>
    /// <param name="messages">供应商输入。</param>
    /// <param name="options">供应商选项，配额限制只收紧输出，不估算输入。</param>
    /// <param name="cancellationToken">模型请求取消令牌。</param>
    /// <returns>通过结算和阈值检查的供应商原始响应。</returns>
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        await using var lease = quota is null ? null : await quota.ReserveAsync(context, cancellationToken);
        var requestOptions = LimitOutput(options, lease);
        if (lease is not null) await lease.MarkStartedAsync(cancellationToken);
        var response = await base.GetResponseAsync(messages, requestOptions, cancellationToken);
        if (lease is not null)
        {
            long? total = response.Usage?.TotalTokenCount is >= 0 ? response.Usage.TotalTokenCount : null;
            await lease.CompleteAsync(total);
            CheckResponse(total, response.FinishReason, response.Usage?.OutputTokenCount, requestOptions?.MaxOutputTokens, lease.ReservedTokens);
        }
        return response;
    }
    #endregion

    #region 执行流式请求（GetStreamingResponseAsync）
    /// <summary>每次物理调用一个租约，先交付真实用量更新再报告结算失败，保留终态审计数据。</summary>
    /// <param name="messages">本次模型输入。</param>
    /// <param name="options">模型选项。</param>
    /// <param name="cancellationToken">模型请求取消令牌，不用于账本清理。</param>
    /// <returns>供应商原始更新。</returns>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var lease = quota is null ? null : await quota.ReserveAsync(context, cancellationToken);
        var requestOptions = LimitOutput(options, lease);
        if (lease is not null) await lease.MarkStartedAsync(cancellationToken);
        var usage = new ModelUsageAccumulator();
        bool invalidTotal = false;
        ChatFinishReason? finish = null;
        await foreach (var update in base.GetStreamingResponseAsync(messages, requestOptions, cancellationToken).ConfigureAwait(false))
        {
            if (lease is not null)
            {
                var reports = update.Contents.OfType<UsageContent>().Select(x => x.Details).ToArray();
                invalidTotal |= reports.Any(x => x.TotalTokenCount is < 0);
                usage.Observe("response", reports);
                finish = update.FinishReason ?? finish;
            }
            yield return update;
        }
        if (lease is not null)
        {
            var snapshot = usage.Snapshot(true, 0, null);
            long? total = invalidTotal ? null : snapshot.TotalTokens;
            await lease.CompleteAsync(total);
            CheckResponse(total, finish, snapshot.OutputTokens, requestOptions?.MaxOutputTokens, lease.ReservedTokens);
        }
    }
    #endregion

    #region 收紧单次输出（LimitOutput）
    /// <summary>不改调用方选项，不放宽已有上限；总用量仍只能按实际报告事后检查。</summary>
    /// <param name="options">原始选项。</param>
    /// <param name="lease">预占租约。</param>
    /// <returns>启用时返回克隆选项，否则返回原对象。</returns>
    private static ChatOptions? LimitOutput(ChatOptions? options, IAgentUserTokenQuotaLease? lease)
    {
        if (lease is null) return options;
        var result = options?.Clone() ?? new ChatOptions();
        int maximum = (int)Math.Min(int.MaxValue, lease.ReservedTokens);
        result.MaxOutputTokens = result.MaxOutputTokens is int existing ? Math.Min(existing, maximum) : maximum;
        return result;
    }
    #endregion

    #region 核对完整响应用量（CheckResponse）
    /// <summary>实际消耗已持久化；未知或超过预占的请求不能伪报成功，不重复计费。</summary>
    /// <param name="total">实际总用量。</param>
    /// <param name="finish">结束原因。</param>
    /// <param name="output">实际输出用量。</param>
    /// <param name="outputLimit">发送的输出上限。</param>
    /// <param name="reserved">本次预占总量。</param>
    private static void CheckResponse(long? total, ChatFinishReason? finish, long? output, int? outputLimit, long reserved)
    {
        if (total is null) throw new AgentRuntimeException(AgentRunErrorCodes.ModelTokenUsageUnavailable, "The model did not report usable total Token usage for the shared tenant quota.");
        if (total > reserved || finish == ChatFinishReason.Length || output > outputLimit)
            throw new AgentRuntimeException(AgentRunErrorCodes.ModelTokenQuotaExceeded, "The model request exceeded its reserved Token quota.");
    }
    #endregion
}
