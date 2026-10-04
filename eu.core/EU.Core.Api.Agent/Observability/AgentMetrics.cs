using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using EU.Core.IServices.Runtime;
using Microsoft.Extensions.Logging;

namespace EU.Core.Api.Agent.Observability;

public enum AgentResilienceEvent
{
    RateLimitRejected,
    CapacityAdmitted,
    CapacityCompleted,
    CapacityRejected,
    IdempotencyReserved,
    IdempotencyCompleted,
    IdempotencyReplayed,
    IdempotencyKeyReused,
    IdempotencyInProgress,
    IdempotencyOutcomeUnknown,
    IdempotencyRejected,
    IdempotencyAbandoned,
    IdempotencyIndeterminate,
    ChatStreamCompleted,
    ChatStreamPaused,
    ChatStreamConsumerCancelled,
    HostDrainStarted,
    HostDrainRejected
}

/// <summary>有界共享额度信号，不把用户、租户或额度作为指标标签。</summary>
public enum AgentUserQuotaSignal { Rejected, Frozen, Unavailable }

public sealed class AgentMetrics : IDisposable, IAgentRuntimeTelemetry
{
    public const string MeterName = "EU.Core.Api.Agent";

    private readonly Meter _meter = new(MeterName);
    private readonly Counter<long> _requests;
    private readonly Histogram<double> _duration;
    private readonly UpDownCounter<long> _activeRequests;
    private readonly Counter<long> _resilienceEvents;
    private readonly UpDownCounter<long> _activeExpensiveRequests;
    private readonly ConcurrentDictionary<RequestKey, long> _active = new();
    private readonly ConcurrentDictionary<CompletionKey, CompletionStats> _completed = new();
    private readonly ConcurrentDictionary<AgentResilienceEvent, long> _resilience = new();
    private long _activeExpensive;
    private readonly Counter<long> _runs;
    private readonly Counter<long> _tokens;
    private readonly Counter<long> _usageReports;
    private readonly Counter<long> _tools;
    private readonly Histogram<double> _runDuration;
    private readonly Histogram<double> _modelDuration;
    private readonly Histogram<double> _firstTextDuration;
    private readonly Histogram<double> _toolDuration;
    private readonly ConcurrentDictionary<AgentRunStatus, long> _runCounts = new();
    private readonly ConcurrentDictionary<AgentTokenUsageStatus, long> _usageCounts = new();
    private readonly ConcurrentDictionary<AgentRunEventKind, long> _toolCounts = new();
    private readonly ConcurrentDictionary<(AgentTokenUsageStatus Status, string Kind), long> _tokenCounts = new();
    private readonly DurationStats _runTimes = new();
    private readonly DurationStats _modelTimes = new();
    private readonly DurationStats _firstTextTimes = new();
    private readonly DurationStats _toolTimes = new();
    private readonly Counter<long> _budgetSignals;
    private readonly ConcurrentDictionary<(AgentTokenBudgetScope Scope, AgentTokenBudgetSignal Signal), long> _budgetCounts = new();
    private readonly ILogger<AgentMetrics>? _logger;
    private readonly Counter<long> _userQuotaSignals;
    private readonly ConcurrentDictionary<AgentUserQuotaSignal, long> _userQuotaCounts = new();

    public AgentMetrics(ILogger<AgentMetrics>? logger = null)
    {
        _logger = logger;
        _userQuotaSignals = _meter.CreateCounter<long>("agent.user_token_quota.signals", unit: "{signal}");
        _budgetSignals = _meter.CreateCounter<long>("agent.runtime.token_budget_signals", unit: "{signal}");
        _runs = _meter.CreateCounter<long>("agent.runtime.runs", unit: "{run}");
        _tokens = _meter.CreateCounter<long>("agent.runtime.tokens", unit: "{token}");
        _usageReports = _meter.CreateCounter<long>("agent.runtime.usage_reports", unit: "{run}");
        _tools = _meter.CreateCounter<long>("agent.runtime.tools", unit: "{call}");
        _runDuration = _meter.CreateHistogram<double>("agent.runtime.duration", unit: "ms");
        _modelDuration = _meter.CreateHistogram<double>("agent.model.duration", unit: "ms");
        _firstTextDuration = _meter.CreateHistogram<double>("agent.model.time_to_first_text", unit: "ms");
        _toolDuration = _meter.CreateHistogram<double>("agent.tool.duration", unit: "ms");
        _requests = _meter.CreateCounter<long>(
            "agent.api.requests",
            unit: "{request}");
        _duration = _meter.CreateHistogram<double>(
            "agent.api.duration",
            unit: "ms");
        _activeRequests = _meter.CreateUpDownCounter<long>(
            "agent.api.active_requests",
            unit: "{request}");
        _resilienceEvents = _meter.CreateCounter<long>(
            "agent.resilience.events",
            unit: "{event}");
        _activeExpensiveRequests = _meter.CreateUpDownCounter<long>(
            "agent.expensive.active_requests",
            unit: "{request}");
    }

    #region 记录共享额度信号（RecordUserQuota）
    /// <summary>只接收固定枚举，不暴露所有者或供应商内容。</summary>
    /// <param name="signal">拒绝、冻结或依赖不可用。</param>
    public void RecordUserQuota(AgentUserQuotaSignal signal)
    {
        if (!Enum.IsDefined(signal)) return;
        _userQuotaCounts.AddOrUpdate(signal, 1, static (_, count) => count + 1);
        _userQuotaSignals.Add(1, new KeyValuePair<string, object?>("signal", signal.ToString()));
        _logger?.LogWarning("Agent shared Token quota signal: {QuotaSignal}.", signal);
    }
    #endregion

    #region 记录预算监控信号（RecordTokenBudget）
    /// <summary>只输出固定枚举标签与日志，不记录额度、用户、输入、输出或凭据。</summary>
    /// <param name="scope">预算范围。</param>
    /// <param name="signal">已由预算实例去重的信号。</param>
    public void RecordTokenBudget(AgentTokenBudgetScope scope, AgentTokenBudgetSignal signal)
    {
        if (!Enum.IsDefined(scope) || !Enum.IsDefined(signal)
            || (signal == AgentTokenBudgetSignal.OutputLimited && scope != AgentTokenBudgetScope.ModelOutput)) return;
        _budgetCounts.AddOrUpdate((scope, signal), 1, static (_, count) => count + 1);
        _budgetSignals.Add(1, new TagList { { "scope", scope.ToString() }, { "signal", signal.ToString() } });
        _logger?.LogWarning("Agent Token budget signal: {BudgetScope} / {BudgetSignal}.", scope, signal);
    }
    #endregion

    /// <summary>记录一次运行终态，未知用量不加入 Token 总量。</summary>
    public void RecordRun(AgentRunStatus status, AgentModelUsage? usage, long durationMilliseconds)
    {
        _runs.Add(1, new KeyValuePair<string, object?>("status", status.ToString()));
        _runCounts.AddOrUpdate(status, 1, static (_, count) => count + 1);
        RecordDuration(_runDuration, _runTimes, durationMilliseconds);
        AgentTokenUsageStatus usageStatus = usage?.Status ?? AgentTokenUsageStatus.Unknown;
        _usageReports.Add(1, new KeyValuePair<string, object?>("usage_status", usageStatus.ToString()));
        _usageCounts.AddOrUpdate(usageStatus, 1, static (_, count) => count + 1);
        if (usage is null) return;
        RecordTokens("input", usage.InputTokens, usageStatus);
        RecordTokens("output", usage.OutputTokens, usageStatus);
        RecordTokens("total", usage.TotalTokens, usageStatus);
        if (usage.ModelDurationMilliseconds is long modelTime) RecordDuration(_modelDuration, _modelTimes, modelTime);
        if (usage.TimeToFirstTextMilliseconds is long firstText) RecordDuration(_firstTextDuration, _firstTextTimes, firstText);
    }

    /// <summary>记录 MCP 工具终态；不使用工具名称作为指标标签。</summary>
    public void RecordTool(AgentRunEventKind status, long durationMilliseconds)
    {
        if (status is not (AgentRunEventKind.ToolSucceeded or AgentRunEventKind.ToolFailed or AgentRunEventKind.ToolBlocked)) return;
        _tools.Add(1, new KeyValuePair<string, object?>("status", status.ToString()));
        _toolCounts.AddOrUpdate(status, 1, static (_, count) => count + 1);
        RecordDuration(_toolDuration, _toolTimes, durationMilliseconds);
    }

    private void RecordTokens(string kind, long? count, AgentTokenUsageStatus status)
    {
        if (count is not >= 0) return;
        _tokens.Add(count.Value, new TagList { { "kind", kind }, { "usage_status", status.ToString() } });
        _tokenCounts.AddOrUpdate((status, kind), count.Value, (_, previous) => previous + count.Value);
    }

    private static void RecordDuration(Histogram<double> histogram, DurationStats stats, long milliseconds)
    {
        milliseconds = Math.Max(0, milliseconds);
        histogram.Record(milliseconds);
        stats.Record(milliseconds);
    }

    public void RecordResilience(AgentResilienceEvent resilienceEvent)
    {
        (string control, string outcome) = ResilienceLabels(resilienceEvent);
        _resilienceEvents.Add(1,
            new TagList
            {
                { "agent.resilience.control", control },
                { "agent.resilience.outcome", outcome }
            });
        _resilience.AddOrUpdate(resilienceEvent, 1, static (_, value) => value + 1);
    }

    public void RecordExpensiveStarted()
    {
        _activeExpensiveRequests.Add(1);
        Interlocked.Increment(ref _activeExpensive);
        RecordResilience(AgentResilienceEvent.CapacityAdmitted);
    }

    public void RecordExpensiveCompleted()
    {
        _activeExpensiveRequests.Add(-1);
        long value = Interlocked.Decrement(ref _activeExpensive);
        if (value < 0) Interlocked.Exchange(ref _activeExpensive, 0);
        RecordResilience(AgentResilienceEvent.CapacityCompleted);
    }

    public void RecordStarted(string method, string route, string policy)
    {
        TagList tags = StartTags(method, route, policy);
        _activeRequests.Add(1, tags);
        _active.AddOrUpdate(new RequestKey(method, route, policy), 1, static (_, value) => value + 1);
    }

    public void RecordCompleted(
        string method,
        string route,
        string policy,
        int statusCode,
        string outcome,
        long durationMilliseconds)
    {
        TagList tags = StartTags(method, route, policy);
        tags.Add("http.response.status_code", statusCode);
        tags.Add("agent.outcome", outcome);
        _requests.Add(1, tags);
        _duration.Record(Math.Max(0, durationMilliseconds), tags);
        _activeRequests.Add(-1, StartTags(method, route, policy));
        _active.AddOrUpdate(new RequestKey(method, route, policy), 0,
            static (_, value) => Math.Max(0, value - 1));
        CompletionStats stats = _completed.GetOrAdd(
            new CompletionKey(method, route, policy, statusCode, outcome),
            static _ => new CompletionStats());
        Interlocked.Increment(ref stats.Count);
        Interlocked.Add(ref stats.DurationMilliseconds, Math.Max(0, durationMilliseconds));
    }

    public string RenderPrometheus()
    {
        var output = new StringBuilder(2048);
        output.AppendLine("# HELP agent_api_requests_total Completed Agent API requests.");
        output.AppendLine("# TYPE agent_api_requests_total counter");
        foreach ((CompletionKey key, CompletionStats stats) in _completed.OrderBy(value => value.Key))
        {
            string labels = CompletionLabels(key);
            output.Append("agent_api_requests_total{").Append(labels).Append("} ")
                .AppendLine(Interlocked.Read(ref stats.Count).ToString(CultureInfo.InvariantCulture));
        }

        output.AppendLine("# HELP agent_api_duration_milliseconds Agent API request duration in milliseconds.");
        output.AppendLine("# TYPE agent_api_duration_milliseconds summary");
        foreach ((CompletionKey key, CompletionStats stats) in _completed.OrderBy(value => value.Key))
        {
            string labels = CompletionLabels(key);
            output.Append("agent_api_duration_milliseconds_sum{").Append(labels).Append("} ")
                .AppendLine(Interlocked.Read(ref stats.DurationMilliseconds).ToString(CultureInfo.InvariantCulture));
            output.Append("agent_api_duration_milliseconds_count{").Append(labels).Append("} ")
                .AppendLine(Interlocked.Read(ref stats.Count).ToString(CultureInfo.InvariantCulture));
        }

        output.AppendLine("# HELP agent_api_active_requests Current active Agent API requests.");
        output.AppendLine("# TYPE agent_api_active_requests gauge");
        foreach ((RequestKey key, long value) in _active.OrderBy(value => value.Key))
        {
            output.Append("agent_api_active_requests{").Append(RequestLabels(key)).Append("} ")
                .AppendLine(value.ToString(CultureInfo.InvariantCulture));
        }

        output.AppendLine("# HELP agent_resilience_events_total Bounded Agent resilience control events.");
        output.AppendLine("# TYPE agent_resilience_events_total counter");
        foreach ((AgentResilienceEvent key, long value) in _resilience.OrderBy(value => value.Key))
        {
            (string control, string outcome) = ResilienceLabels(key);
            output.Append("agent_resilience_events_total{control=\"")
                .Append(control).Append("\",outcome=\"").Append(outcome).Append("\"} ")
                .AppendLine(value.ToString(CultureInfo.InvariantCulture));
        }

        output.AppendLine("# HELP agent_expensive_active_requests Current admitted expensive requests.");
        output.AppendLine("# TYPE agent_expensive_active_requests gauge");
        output.Append("agent_expensive_active_requests ")
            .AppendLine(Math.Max(0, Interlocked.Read(ref _activeExpensive))
                .ToString(CultureInfo.InvariantCulture));

        output.AppendLine("# TYPE agent_runtime_runs_total counter");
        foreach (var (status, count) in _runCounts.OrderBy(value => value.Key))
            output.AppendLine($"agent_runtime_runs_total{{status=\"{status}\"}} {count.ToString(CultureInfo.InvariantCulture)}");
        output.AppendLine("# TYPE agent_runtime_usage_reports_total counter");
        foreach (var (status, count) in _usageCounts.OrderBy(value => value.Key))
            output.AppendLine($"agent_runtime_usage_reports_total{{usage_status=\"{status}\"}} {count.ToString(CultureInfo.InvariantCulture)}");
        output.AppendLine("# TYPE agent_runtime_tokens_total counter");
        foreach (var (key, count) in _tokenCounts.OrderBy(value => value.Key.Status).ThenBy(value => value.Key.Kind))
            output.AppendLine($"agent_runtime_tokens_total{{usage_status=\"{key.Status}\",kind=\"{key.Kind}\"}} {count.ToString(CultureInfo.InvariantCulture)}");
        output.AppendLine("# TYPE agent_runtime_tool_calls_total counter");
        foreach (var (status, count) in _toolCounts.OrderBy(value => value.Key))
            output.AppendLine($"agent_runtime_tool_calls_total{{status=\"{status}\"}} {count.ToString(CultureInfo.InvariantCulture)}");
        output.AppendLine("# HELP agent_user_token_quota_signals_total Shared quota rejection, freezing and unavailability signals, not balance gauges.");
        output.AppendLine("# TYPE agent_user_token_quota_signals_total counter");
        foreach (var (signal, count) in _userQuotaCounts.OrderBy(value => value.Key))
            output.AppendLine($"agent_user_token_quota_signals_total{{signal=\"{signal}\"}} {count.ToString(CultureInfo.InvariantCulture)}");
        output.AppendLine("# HELP agent_runtime_token_budget_signals_total Deduplicated signals from enabled Token budgets, not failed-run counts.");
        output.AppendLine("# TYPE agent_runtime_token_budget_signals_total counter");
        foreach (var (key, count) in _budgetCounts.OrderBy(value => value.Key.Scope).ThenBy(value => value.Key.Signal))
            output.AppendLine($"agent_runtime_token_budget_signals_total{{scope=\"{key.Scope}\",signal=\"{key.Signal}\"}} {count.ToString(CultureInfo.InvariantCulture)}");
        _runTimes.Render(output, "agent_runtime_duration_milliseconds");
        _modelTimes.Render(output, "agent_model_duration_milliseconds");
        _firstTextTimes.Render(output, "agent_model_time_to_first_text_milliseconds");
        _toolTimes.Render(output, "agent_tool_duration_milliseconds");
        return output.ToString();
    }

    public void Dispose() => _meter.Dispose();

    private static TagList StartTags(
        string method,
        string route,
        string policy) =>
        new()
        {
            { "http.request.method", method },
            { "http.route", route },
            { "agent.policy", policy }
        };

    private static string CompletionLabels(CompletionKey key) =>
        $"{RequestLabels(key.Request)},status_code=\"{key.StatusCode.ToString(CultureInfo.InvariantCulture)}\",outcome=\"{Escape(key.Outcome)}\"";

    private static string RequestLabels(RequestKey key) =>
        $"method=\"{Escape(key.Method)}\",route=\"{Escape(key.Route)}\",policy=\"{Escape(key.Policy)}\"";

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal);

    private static (string Control, string Outcome) ResilienceLabels(
        AgentResilienceEvent value) => value switch
    {
        AgentResilienceEvent.RateLimitRejected => ("rate_limit", "rejected"),
        AgentResilienceEvent.CapacityAdmitted => ("capacity", "admitted"),
        AgentResilienceEvent.CapacityCompleted => ("capacity", "completed"),
        AgentResilienceEvent.CapacityRejected => ("capacity", "rejected"),
        AgentResilienceEvent.IdempotencyReserved => ("idempotency", "reserved"),
        AgentResilienceEvent.IdempotencyCompleted => ("idempotency", "completed"),
        AgentResilienceEvent.IdempotencyReplayed => ("idempotency", "replayed"),
        AgentResilienceEvent.IdempotencyKeyReused => ("idempotency", "key_reused"),
        AgentResilienceEvent.IdempotencyInProgress => ("idempotency", "in_progress"),
        AgentResilienceEvent.IdempotencyOutcomeUnknown => ("idempotency", "outcome_unknown"),
        AgentResilienceEvent.IdempotencyRejected => ("idempotency", "rejected"),
        AgentResilienceEvent.IdempotencyAbandoned => ("idempotency", "abandoned"),
        AgentResilienceEvent.IdempotencyIndeterminate => ("idempotency", "indeterminate"),
        AgentResilienceEvent.ChatStreamCompleted => ("chat_stream", "completed"),
        AgentResilienceEvent.ChatStreamPaused => ("chat_stream", "paused"),
        AgentResilienceEvent.ChatStreamConsumerCancelled => ("chat_stream", "consumer_cancelled"),
        AgentResilienceEvent.HostDrainStarted => ("host_lifecycle", "drain_started"),
        AgentResilienceEvent.HostDrainRejected => ("host_lifecycle", "drain_rejected"),
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };

    private readonly record struct RequestKey(string Method, string Route, string Policy)
        : IComparable<RequestKey>
    {
        public int CompareTo(RequestKey other)
        {
            int value = string.CompareOrdinal(Method, other.Method);
            if (value != 0) return value;
            value = string.CompareOrdinal(Route, other.Route);
            return value != 0 ? value : string.CompareOrdinal(Policy, other.Policy);
        }
    }

    private readonly record struct CompletionKey(
        string Method,
        string Route,
        string Policy,
        int StatusCode,
        string Outcome) : IComparable<CompletionKey>
    {
        public RequestKey Request => new(Method, Route, Policy);

        public int CompareTo(CompletionKey other)
        {
            int value = Request.CompareTo(other.Request);
            if (value != 0) return value;
            value = StatusCode.CompareTo(other.StatusCode);
            return value != 0 ? value : string.CompareOrdinal(Outcome, other.Outcome);
        }
    }

    private sealed class CompletionStats
    {
        public long Count;
        public long DurationMilliseconds;
    }

    // 固定桶及枚举标签限制基数，不携带用户、运行、模型地址、参数或输出内容。
    private sealed class DurationStats
    {
        private static readonly long[] Bounds = [100, 500, 1000, 2500, 5000, 10000, 30000, 60000, 120000, 300000];
        private readonly long[] _buckets = new long[Bounds.Length];
        private long _count;
        private long _sum;

        public void Record(long milliseconds)
        {
            Interlocked.Increment(ref _count);
            Interlocked.Add(ref _sum, milliseconds);
            for (int index = 0; index < Bounds.Length; index++)
                if (milliseconds <= Bounds[index]) Interlocked.Increment(ref _buckets[index]);
        }

        public void Render(StringBuilder output, string name)
        {
            output.AppendLine($"# TYPE {name} histogram");
            for (int index = 0; index < Bounds.Length; index++)
                output.AppendLine($"{name}_bucket{{le=\"{Bounds[index].ToString(CultureInfo.InvariantCulture)}\"}} {Interlocked.Read(ref _buckets[index]).ToString(CultureInfo.InvariantCulture)}");
            long count = Interlocked.Read(ref _count);
            output.AppendLine($"{name}_bucket{{le=\"+Inf\"}} {count.ToString(CultureInfo.InvariantCulture)}");
            output.AppendLine($"{name}_sum {Interlocked.Read(ref _sum).ToString(CultureInfo.InvariantCulture)}");
            output.AppendLine($"{name}_count {count.ToString(CultureInfo.InvariantCulture)}");
        }
    }
}
