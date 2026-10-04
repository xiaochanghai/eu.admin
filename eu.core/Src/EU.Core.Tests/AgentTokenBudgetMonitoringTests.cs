#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Reflection;
using EU.Core.Agent.Runtime;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Observability;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EU.Core.Tests;

/// <summary>预算监控的离线回归；不访问数据库、真实模型或外部告警服务。</summary>
public sealed class AgentTokenBudgetMonitoringTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData(1, true)]
    [InlineData(80, true)]
    [InlineData(99, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(100, false)]
    public void Warning_percent_validates_without_changing_limits(int? percent, bool valid)
    {
        var run = new AgentExecutionOptions { TokenBudgetWarningPercent = percent };
        var entry = new UnifiedEntryOptions { TokenBudgetWarningPercent = percent };
        Assert.Equal(valid, new AgentExecutionOptionsValidator().Validate(null, run).Succeeded);
        Assert.Equal(valid, new UnifiedEntryOptionsValidator().Validate(null, entry).Succeeded);
        Assert.Equal(percent, entry.ToLimits().ModelTokenBudgetWarningPercent);
        if (valid)
        {
            using var scope = new UnifiedEntryExecutionScope(limits: entry.ToLimits());
            Assert.Null(scope.ModelTokenBudget);
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AgentTokenBudgetMonitor(AgentTokenBudgetScope.AgentRun, percent, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnifiedEntryExecutionScope(limits: entry.ToLimits()));
        }
    }

    [Fact]
    public void Warning_configuration_binds_without_new_sources()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["AgentExecution:TokenBudgetWarningPercent"] = "75", ["UnifiedEntry:TokenBudgetWarningPercent"] = "90" }).Build();
        using var cleanup = configuration as IDisposable;
        Assert.Equal(75, configuration.GetSection("AgentExecution").Get<AgentExecutionOptions>()!.TokenBudgetWarningPercent);
        Assert.Equal(90, configuration.GetSection("UnifiedEntry").Get<UnifiedEntryOptions>()!.ToLimits().ModelTokenBudgetWarningPercent);
        Assert.Equal(80, new AgentRuntimeOptions(TimeSpan.FromSeconds(5)).TokenBudgetWarningPercent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(85)]
    public void Json_warning_setting_can_explicitly_disable_the_default(int? expected)
    {
        string json = System.Text.Json.JsonSerializer.Serialize(new
        {
            AgentExecution = new { TokenBudgetWarningPercent = expected },
            UnifiedEntry = new { TokenBudgetWarningPercent = expected }
        });
        using var source = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        var configuration = new ConfigurationBuilder().AddJsonStream(source).Build();
        using var cleanup = configuration as IDisposable;
        Assert.Equal(expected, configuration.GetSection("AgentExecution").Get<AgentExecutionOptions>()!.TokenBudgetWarningPercent);
        Assert.Equal(expected, configuration.GetSection("UnifiedEntry").Get<UnifiedEntryOptions>()!.TokenBudgetWarningPercent);
        Assert.Equal(80, new AgentExecutionOptions().TokenBudgetWarningPercent);
        Assert.Equal(80, new UnifiedEntryOptions().TokenBudgetWarningPercent);
    }

    [Fact]
    public void Warning_boundary_is_exact_and_safe_for_large_token_counts()
    {
        var telemetry = new CaptureTelemetry();
        var monitor = new AgentTokenBudgetMonitor(AgentTokenBudgetScope.AgentRun, 80, telemetry);
        monitor.Observe(79, 100);
        Assert.Empty(telemetry.Signals);
        monitor.Observe(80, 100);
        monitor.Observe(99, 100);
        monitor.Observe(100, 100);
        monitor.Observe(101, 100);
        Assert.Equal(3, telemetry.Signals.Count);
        Assert.All(telemetry.Signals.Values, count => Assert.Equal(1, count));
        var huge = new AgentTokenBudgetMonitor(AgentTokenBudgetScope.ExecutionTree, 99, telemetry);
        huge.Observe(long.MaxValue - 1, long.MaxValue);
        Assert.Equal(1, telemetry.Count(AgentTokenBudgetScope.ExecutionTree, AgentTokenBudgetSignal.Warning));
    }

    [Theory]
    [InlineData(100L, AgentTokenBudgetSignal.Exhausted)]
    [InlineData(101L, AgentTokenBudgetSignal.Exceeded)]
    public void Jump_to_upper_limit_does_not_invent_an_early_warning(long used, AgentTokenBudgetSignal signal)
    {
        var telemetry = new CaptureTelemetry();
        new AgentTokenBudgetMonitor(AgentTokenBudgetScope.AgentRun, 80, telemetry).Observe(used, 100);
        Assert.Equal(1, telemetry.Count(AgentTokenBudgetScope.AgentRun, signal));
        Assert.Single(telemetry.Signals);
    }

    [Fact]
    public void Null_percent_disables_warning_only_and_invalid_stats_are_not_zero()
    {
        var telemetry = new CaptureTelemetry();
        var monitor = new AgentTokenBudgetMonitor(AgentTokenBudgetScope.AgentRun, null, telemetry);
        monitor.Observe(-1, 100);
        monitor.Observe(80, 0);
        monitor.Observe(80, 100);
        Assert.Empty(telemetry.Signals);
        monitor.Observe(100, 100);
        Assert.Equal(1, telemetry.Count(AgentTokenBudgetScope.AgentRun, AgentTokenBudgetSignal.Exhausted));
    }

    [Fact]
    public void Concurrent_observations_are_deduplicated_per_budget_instance()
    {
        var telemetry = new CaptureTelemetry();
        var monitor = new AgentTokenBudgetMonitor(AgentTokenBudgetScope.ExecutionTree, 80, telemetry);
        Parallel.For(0, 1000, _ => monitor.Observe(90, 100));
        Assert.Equal(1, telemetry.Count(AgentTokenBudgetScope.ExecutionTree, AgentTokenBudgetSignal.Warning));
        new AgentTokenBudgetMonitor(AgentTokenBudgetScope.ExecutionTree, 80, telemetry).Observe(90, 100);
        Assert.Equal(2, telemetry.Count(AgentTokenBudgetScope.ExecutionTree, AgentTokenBudgetSignal.Warning));
    }

    [Fact]
    public void Metrics_log_and_meter_use_only_bounded_scope_and_signal_labels()
    {
        var logger = new CaptureLogger();
        using var metrics = new AgentMetrics(logger);
        var ownCounter = typeof(AgentMetrics).GetField("_budgetSignals", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(metrics);
        var measurements = new ConcurrentBag<KeyValuePair<string, object?>[]>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, active) =>
        {
            // 其他并行测试可创建同名 Meter，只观察本测试实例，避免跨用例信号混入。
            if (ReferenceEquals(instrument, ownCounter)) active.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) => measurements.Add(tags.ToArray()));
        listener.Start();
        Parallel.For(0, 100, _ => metrics.RecordTokenBudget(AgentTokenBudgetScope.AgentRun, AgentTokenBudgetSignal.Warning));
        metrics.RecordTokenBudget((AgentTokenBudgetScope)999, AgentTokenBudgetSignal.Warning);
        metrics.RecordTokenBudget(AgentTokenBudgetScope.AgentRun, (AgentTokenBudgetSignal)999);
        metrics.RecordTokenBudget(AgentTokenBudgetScope.AgentRun, AgentTokenBudgetSignal.OutputLimited);
        Assert.Contains("agent_runtime_token_budget_signals_total{scope=\"AgentRun\",signal=\"Warning\"} 100", metrics.RenderPrometheus());
        Assert.DoesNotContain("999", metrics.RenderPrometheus());
        Assert.Equal(100, logger.Messages.Count);
        Assert.All(logger.Messages, value => Assert.Equal("Agent Token budget signal: AgentRun / Warning.", value));
        Assert.All(measurements, tags =>
        {
            Assert.Equal(new[] { "scope", "signal" }, tags.Select(tag => tag.Key));
            Assert.Equal("AgentRun", tags[0].Value);
            Assert.Equal("Warning", tags[1].Value);
        });
        Assert.Equal(100, measurements.Count(tags => tags[0].Value?.ToString() == "AgentRun"));
    }

    [Fact]
    public async Task Shared_monitor_failure_cannot_release_extra_quota_or_replace_domain_error()
    {
        var telemetry = new CaptureTelemetry { Fail = true };
        using var scope = new UnifiedEntryExecutionScope(limits: new UnifiedEntryLimits(MaxModelTotalTokens: 10), telemetry: telemetry);
        using (var first = await scope.ModelTokenBudget!.ReserveAsync()) { first.MarkStarted(); first.Complete(8, false); }
        using (var last = await scope.ModelTokenBudget!.ReserveAsync()) { last.MarkStarted(); last.Complete(2, false); }
        var exception = await Assert.ThrowsAsync<AgentRuntimeException>(() => scope.ModelTokenBudget!.ReserveAsync().AsTask());
        Assert.Equal(AgentRunErrorCodes.ModelTokenBudgetExceeded, exception.ErrorCode);
        Assert.Equal(1, telemetry.Count(AgentTokenBudgetScope.ExecutionTree, AgentTokenBudgetSignal.Warning));
        Assert.Equal(1, telemetry.Count(AgentTokenBudgetScope.ExecutionTree, AgentTokenBudgetSignal.Exhausted));
    }

    private sealed class CaptureTelemetry : IAgentRuntimeTelemetry
    {
        public ConcurrentDictionary<(AgentTokenBudgetScope, AgentTokenBudgetSignal), int> Signals { get; } = new();
        public bool Fail { get; init; }
        public int Count(AgentTokenBudgetScope scope, AgentTokenBudgetSignal signal) => Signals.GetValueOrDefault((scope, signal));
        public void RecordTokenBudget(AgentTokenBudgetScope scope, AgentTokenBudgetSignal signal)
        {
            Signals.AddOrUpdate((scope, signal), 1, static (_, count) => count + 1);
            if (Fail) throw new InvalidOperationException("offline monitor failure");
        }
        public void RecordRun(AgentRunStatus status, AgentModelUsage? usage, long durationMilliseconds) { }
        public void RecordTool(AgentRunEventKind status, long durationMilliseconds) { }
    }

    private sealed class CaptureLogger : ILogger<AgentMetrics>
    {
        public ConcurrentBag<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        { Assert.Null(exception); Assert.Equal(LogLevel.Warning, logLevel); Messages.Add(formatter(state, exception)); }
    }
}
