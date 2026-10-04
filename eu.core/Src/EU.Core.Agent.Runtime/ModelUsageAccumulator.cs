using EU.Core.IServices.Runtime;
using Microsoft.Extensions.AI;

namespace EU.Core.Agent.Runtime;

/// <summary>按模型响应标识合并累计统计，避免同一响应的重复 usage 被重复计费。</summary>
internal sealed class ModelUsageAccumulator
{
    private const int MaximumResponses = 256;
    private readonly Dictionary<string, Counts> _responses = new(StringComparer.Ordinal);
    private bool _incomplete;

    public void Observe(string? responseId, IReadOnlyList<UsageDetails> usages)
    {
        if (string.IsNullOrWhiteSpace(responseId) && usages.Count == 0) return;
        string key = string.IsNullOrWhiteSpace(responseId) ? "" : responseId;
        if (key.Length > 256 || (!_responses.ContainsKey(key) && _responses.Count >= MaximumResponses))
        {
            _incomplete = true;
            return;
        }
        if (key.Length == 0) _incomplete = true;
        if (!_responses.TryGetValue(key, out Counts? counts)) _responses[key] = counts = new Counts();
        foreach (UsageDetails usage in usages)
        {
            counts.Input = Merge(counts.Input, usage.InputTokenCount);
            counts.Output = Merge(counts.Output, usage.OutputTokenCount);
            counts.Total = Merge(counts.Total, usage.TotalTokenCount);
            if (usage.InputTokenCount < 0 || usage.OutputTokenCount < 0 || usage.TotalTokenCount < 0) _incomplete = true;
        }
    }

    public AgentModelUsage Snapshot(bool completed, long durationMilliseconds, long? firstTextMilliseconds)
    {
        long? input = Sum(value => value.Input);
        long? output = Sum(value => value.Output);
        long? total = Sum(value => value.Total);
        AgentTokenUsageStatus status = input is null && output is null && total is null
            ? AgentTokenUsageStatus.Unknown
            : completed && !_incomplete && _responses.Values.All(value => value.Input.HasValue && value.Output.HasValue && value.Total.HasValue)
                ? AgentTokenUsageStatus.Reported : AgentTokenUsageStatus.Partial;
        return new(input, output, total, status, durationMilliseconds, firstTextMilliseconds);
    }

    private long? Sum(Func<Counts, long?> selector)
    {
        long? result = null;
        foreach (Counts counts in _responses.Values)
        {
            if (selector(counts) is not long value) continue;
            if (result.HasValue && value > long.MaxValue - result.Value)
            {
                _incomplete = true;
                return null;
            }
            result = (result ?? 0) + value;
        }
        return result;
    }

    private static long? Merge(long? previous, long? current) => current is >= 0
        ? Math.Max(previous ?? 0, current.Value) : previous;

    private sealed class Counts
    {
        public long? Input;
        public long? Output;
        public long? Total;
    }
}
