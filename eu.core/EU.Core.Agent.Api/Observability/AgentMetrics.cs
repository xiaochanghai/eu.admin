using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace EU.Core.Agent.Api.Observability;

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

public sealed class AgentMetrics : IDisposable
{
    public const string MeterName = "EU.Core.Agent.Api";

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

    public AgentMetrics()
    {
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
}
