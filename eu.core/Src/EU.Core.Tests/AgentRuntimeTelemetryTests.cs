#nullable enable
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics.Metrics;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using EU.Core.Agent.Runtime;
using EU.Core.Api.Agent.Observability;
using EU.Core.IServices.Runtime;
using EU.Core.Model;
using EU.Core.Model.Entity;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.Services;
using EU.Core.Tests.Service_Test;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI;
using Xunit;
using AgentRunContext = EU.Core.IServices.Runtime.AgentRunContext;
using EU.Core.IServices;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using EU.Core.Common;
using EU.Core.Extensions;
using EU.Core.IServices.Knowledge;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.Skills;
using Microsoft.Extensions.Configuration;

namespace EU.Core.Tests;

/// <summary>离线用量/监控回归，仅使用内存 SQLite、SDK HTTP 替身和假模型流。</summary>
[Collection("Agent policy registration")]
public sealed class AgentRuntimeTelemetryTests
{
    [Fact]
    public void Missing_usage_is_unknown_not_zero()
    {
        var usage = new ModelUsageAccumulator();
        usage.Observe("a", []);
        AgentModelUsage result = usage.Snapshot(true, 25, null);
        Assert.Null(result.InputTokens);
        Assert.Null(result.OutputTokens);
        Assert.Null(result.TotalTokens);
        Assert.Equal(AgentTokenUsageStatus.Unknown, result.Status);
        Assert.Null(result.TimeToFirstTextMilliseconds);
    }

    [Fact]
    public void Explicit_zero_is_preserved()
    {
        var usage = new ModelUsageAccumulator();
        usage.Observe("a", [Counts(0, 0, 0)]);
        var result = usage.Snapshot(true, 0, 0);
        Assert.Equal(0, result.TotalTokens);
        Assert.Equal(AgentTokenUsageStatus.Reported, result.Status);
    }

    [Fact]
    public void Response_snapshots_are_deduplicated_and_tool_loop_responses_are_summed()
    {
        var usage = new ModelUsageAccumulator();
        usage.Observe("a", [Counts(2, 3, 5)]);
        usage.Observe("a", [Counts(2, 3, 5)]);
        usage.Observe("a", [Counts(2, 4, 6)]);
        usage.Observe("b", [Counts(7, 11, 18)]);
        var result = usage.Snapshot(true, 10, 1);
        Assert.Equal(9, result.InputTokens);
        Assert.Equal(15, result.OutputTokens);
        Assert.Equal(24, result.TotalTokens);
        Assert.Equal(AgentTokenUsageStatus.Reported, result.Status);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public void Interrupted_missing_or_unidentified_response_usage_is_partial(bool completed, bool missingResponse, bool anonymous)
    {
        var usage = new ModelUsageAccumulator();
        usage.Observe(anonymous ? null : "a", [Counts(2, 3, 5)]);
        if (missingResponse) usage.Observe("b", []);
        Assert.Equal(AgentTokenUsageStatus.Partial, usage.Snapshot(completed, 5, null).Status);
    }

    [Fact]
    public void Partial_fields_are_not_inferred_and_invalid_counts_are_rejected()
    {
        var usage = new ModelUsageAccumulator();
        usage.Observe("a", [Counts(2, -1, null)]);
        var result = usage.Snapshot(true, 5, null);
        Assert.Equal(2, result.InputTokens);
        Assert.Null(result.OutputTokens);
        Assert.Null(result.TotalTokens);
        Assert.Equal(AgentTokenUsageStatus.Partial, result.Status);
    }

    [Fact]
    public void Overflow_and_unbounded_response_ids_do_not_break_execution()
    {
        var usage = new ModelUsageAccumulator();
        usage.Observe("a", [Counts(long.MaxValue, 0, long.MaxValue)]);
        usage.Observe("b", [Counts(1, 0, 1)]);
        usage.Observe(new string('x', 300), [Counts(1, 1, 2)]);
        var result = usage.Snapshot(true, 5, null);
        Assert.Null(result.InputTokens);
        Assert.Null(result.TotalTokens);
        Assert.Equal(AgentTokenUsageStatus.Partial, result.Status);
        for (int i = 0; i < 1000; i++) usage.Observe(i.ToString(), [Counts(0, 0, 0)]);
        Assert.Equal(AgentTokenUsageStatus.Partial, usage.Snapshot(true, 5, null).Status);
    }

    [Fact]
    public void Sdk_usage_only_update_does_not_produce_text()
    {
        var update = new AgentResponseUpdate { ResponseId = "a", Contents = [new UsageContent(Counts(2, 3, 5))] };
        var converted = MicrosoftAgentRuntimeEngine.ToModelUpdate(update);
        Assert.Empty(converted.Text);
        Assert.Equal("a", converted.ResponseId);
        Assert.Equal(5, Assert.Single(converted.Usages!).TotalTokenCount);
    }

    [Fact]
    public async Task Installed_sdk_requests_stream_usage_and_preserves_distinct_response_ids_across_tools()
    {
        using var handler = new StreamingHandler();
        using var http = new HttpClient(handler);
        using var client = new OpenAIClient(new ApiKeyCredential("offline-placeholder"), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://offline.invalid/v1"), Transport = new HttpClientPipelineTransport(http)
        }).GetChatClient("offline").AsIChatClient();
        AITool lookup = AIFunctionFactory.Create(() => "ok", name: "lookup");
        AIAgent agent = client.AsAIAgent(new ChatClientAgentOptions
        {
            ChatOptions = MicrosoftAgentRuntimeEngine.CreateChatOptions("offline", "instructions", [lookup], null)
        });
        var usage = new ModelUsageAccumulator();
        var observed = new List<string>();
        await foreach (var update in agent.RunStreamingAsync([new ChatMessage(ChatRole.User, "query")]))
        {
            observed.Add($"{update.ResponseId}: {string.Join(',', update.Contents.Select(content => content.GetType().Name))}");
            var converted = MicrosoftAgentRuntimeEngine.ToModelUpdate(update);
            usage.Observe(converted.ResponseId, converted.Usages!);
        }
        Assert.Equal(2, handler.Requests);
        var result = usage.Snapshot(true, 0, 0);
        Assert.Equal(9, result.InputTokens);
        Assert.Equal(14, result.OutputTokens);
        Assert.Equal(23, result.TotalTokens);
        Assert.True(result.Status == AgentTokenUsageStatus.Reported, string.Join("\n", observed));
    }

    [Theory]
    [InlineData("success", AgentTokenUsageStatus.Reported)]
    [InlineData("text", AgentTokenUsageStatus.Reported)]
    [InlineData("fail", AgentTokenUsageStatus.Partial)]
    [InlineData("cancel", AgentTokenUsageStatus.Partial)]
    public async Task Engine_emits_usage_before_channel_terminal_even_on_failure(string outcome, AgentTokenUsageStatus expected)
    {
        using var cancellation = new CancellationTokenSource();
        var engine = new MicrosoftAgentRuntimeEngine(new AgentRuntimeOptions(TimeSpan.FromSeconds(5)), new UnusedResolver(), null!, NullLogger<MicrosoftAgentRuntimeEngine>.Instance);
        var channel = Channel.CreateUnbounded<AgentRunEvent>();
        var producer = (Task)typeof(MicrosoftAgentRuntimeEngine).GetMethod("ProduceAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(engine, [Context(), "offline-placeholder", Array.Empty<AITool>(), Array.Empty<ChatMessage>(), channel.Writer,
                new FakeModel(outcome, cancellation), cancellation.Token])!;
        var events = new List<AgentRunEvent>();
        Exception? failure = null;
        try { await foreach (var value in channel.Reader.ReadAllAsync()) events.Add(value); }
        catch (Exception exception) { failure = exception; }
        await producer;
        var result = Assert.Single(events, value => value.Kind == AgentRunEventKind.ModelUsage).ModelUsage!;
        Assert.Equal(5, result.TotalTokens);
        Assert.Equal(expected, result.Status);
        if (outcome == "text")
        {
            Assert.NotNull(result.TimeToFirstTextMilliseconds);
            Assert.Equal("hello", Assert.Single(events, value => value.Kind == AgentRunEventKind.Delta).Text);
        }
        else
        {
            Assert.Null(result.TimeToFirstTextMilliseconds);
            Assert.DoesNotContain(events, value => value.Kind == AgentRunEventKind.Delta);
        }
        if (outcome is "success" or "text") Assert.Null(failure); else Assert.NotNull(failure);
    }

    [Theory]
    [InlineData("success", AgentRunStatus.Completed)]
    [InlineData("fail", AgentRunStatus.Failed)]
    [InlineData("cancel", AgentRunStatus.Cancelled)]
    [InlineData("cancel-delta", AgentRunStatus.Cancelled)]
    [InlineData("approval", AgentRunStatus.WaitingForApproval)]
    public async Task Runtime_persists_usage_on_all_terminal_paths_without_forwarding_internal_event(string outcome, AgentRunStatus expected)
    {
        using var cancellation = new CancellationTokenSource();
        var audit = new MemoryAudit();
        using var metrics = new AgentMetrics();
        var runtime = new AgentRuntimeService(null!, null!, new FakeEngine(outcome, cancellation), audit, new JsonSchemaValidator(), telemetry: metrics);
        var events = new List<AgentRunEvent>();
        try { await foreach (var value in runtime.StreamAsync(Context(), cancellation.Token)) events.Add(value); }
        catch (OperationCanceledException) when (outcome.StartsWith("cancel", StringComparison.Ordinal)) { }
        var saved = Assert.Single(audit.Records);
        Assert.Equal(expected, saved.Status);
        Assert.Equal("offline-profile", saved.ModelProfileId);
        Assert.Equal(5, saved.ModelUsage!.TotalTokens);
        Assert.False(audit.SaveToken.CanBeCanceled);
        Assert.DoesNotContain(events, value => value.Kind == AgentRunEventKind.ModelUsage);
        Assert.Contains($"agent_runtime_runs_total{{status=\"{expected}\"}} 1", metrics.RenderPrometheus());
    }

    [Fact]
    public async Task Audit_persistence_failure_is_not_reported_as_success_and_usage_still_reaches_metrics()
    {
        using var cancellation = new CancellationTokenSource();
        using var metrics = new AgentMetrics();
        var audit = new MemoryAudit { Fail = true };
        var runtime = new AgentRuntimeService(null!, null!, new FakeEngine("success", cancellation), audit, new JsonSchemaValidator(), telemetry: metrics);
        await Assert.ThrowsAsync<IOException>(async () => { await foreach (var _ in runtime.StreamAsync(Context())) { } });
        Assert.Contains("agent_runtime_runs_total{status=\"Failed\"} 1", metrics.RenderPrometheus());
        Assert.Contains("kind=\"total\"} 5", metrics.RenderPrometheus());
    }

    [Theory]
    [InlineData("success", AgentRunStatus.Completed)]
    [InlineData("fail", AgentRunStatus.Failed)]
    [InlineData("cancel", AgentRunStatus.Cancelled)]
    [InlineData("cancel-delta", AgentRunStatus.Cancelled)]
    [InlineData("approval", AgentRunStatus.WaitingForApproval)]
    public async Task Throwing_telemetry_cannot_change_audited_terminal_status_or_cancellation(string outcome, AgentRunStatus expected)
    {
        using var cancellation = new CancellationTokenSource();
        var audit = new MemoryAudit();
        var telemetry = new ThrowingTelemetry(true, true);
        var runtime = new AgentRuntimeService(null!, null!, new FakeEngine(outcome, cancellation), audit, new JsonSchemaValidator(), telemetry: telemetry);
        var events = new List<AgentRunEvent>();
        var error = await Record.ExceptionAsync(async () =>
        {
            await foreach (var value in runtime.StreamAsync(Context(), cancellation.Token)) events.Add(value);
        });
        if (expected == AgentRunStatus.Cancelled) Assert.IsAssignableFrom<OperationCanceledException>(error);
        else Assert.Null(error);
        var saved = Assert.Single(audit.Records);
        Assert.Equal(expected, saved.Status);
        Assert.Equal(expected == AgentRunStatus.Failed ? AgentRunErrorCodes.ModelFailed :
            expected == AgentRunStatus.WaitingForApproval ? AgentRunErrorCodes.ToolApprovalRequired : "", saved.ErrorCode);
        Assert.False(audit.SaveToken.CanBeCanceled);
        Assert.Equal(expected, Assert.Single(telemetry.Runs));
        if (expected == AgentRunStatus.Completed) Assert.Single(events, value => value.Kind == AgentRunEventKind.Completed);
        if (expected != AgentRunStatus.Failed) Assert.DoesNotContain(events, value => value.Kind == AgentRunEventKind.Failed);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Run_and_tool_telemetry_failures_do_not_skip_later_tool_records(bool throwRun, bool throwTool)
    {
        var audit = new MemoryAudit();
        var telemetry = new ThrowingTelemetry(throwRun, throwTool);
        var tool = new PublishedMcpToolReference(Guid.NewGuid(), "offline", "Offline", Guid.NewGuid(), "lookup", "", "{}", McpToolRisk.ReadOnly, "hash");
        var context = Context() with { Tools = [tool] };
        var runtime = new AgentRuntimeService(null!, null!, new RepeatingToolEngine(2), audit, new JsonSchemaValidator(), telemetry: telemetry);
        var events = new List<AgentRunEvent>();
        await foreach (var value in runtime.StreamAsync(context)) events.Add(value);
        var saved = Assert.Single(audit.Records);
        Assert.Equal(AgentRunStatus.Completed, saved.Status);
        Assert.Equal(2, saved.ToolCallCount);
        Assert.Equal(AgentRunStatus.Completed, Assert.Single(telemetry.Runs));
        Assert.Equal(2, telemetry.Tools.Count);
        Assert.All(telemetry.Tools, status => Assert.Equal(AgentRunEventKind.ToolSucceeded, status));
        Assert.Single(events, value => value.Kind == AgentRunEventKind.Completed);
        Assert.DoesNotContain(events, value => value.Kind == AgentRunEventKind.Failed);
    }

    [Fact]
    public async Task Broken_meter_listener_cannot_turn_successful_audit_into_failed_stream()
    {
        using var metrics = new AgentMetrics();
        using var listener = new MeterListener();
        int callbacks = 0;
        listener.InstrumentPublished = (instrument, meter) =>
        {
            if (instrument.Meter.Name == AgentMetrics.MeterName && instrument.Name is "agent.runtime.runs" or "agent.runtime.tools")
                meter.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, _, _, _) =>
        {
            Interlocked.Increment(ref callbacks);
            throw new InvalidOperationException("offline listener failure");
        });
        listener.Start();
        var audit = new MemoryAudit();
        var tool = new PublishedMcpToolReference(Guid.NewGuid(), "offline", "Offline", Guid.NewGuid(), "lookup", "", "{}", McpToolRisk.ReadOnly, "hash");
        var runtime = new AgentRuntimeService(null!, null!, new RepeatingToolEngine(2), audit, new JsonSchemaValidator(), telemetry: metrics);
        var events = new List<AgentRunEvent>();
        await foreach (var value in runtime.StreamAsync(Context() with { Tools = [tool] })) events.Add(value);
        Assert.Equal(AgentRunStatus.Completed, Assert.Single(audit.Records).Status);
        Assert.Equal(3, callbacks);
        Assert.Single(events, value => value.Kind == AgentRunEventKind.Completed);
        Assert.DoesNotContain(events, value => value.Kind == AgentRunEventKind.Failed);
    }

    [Fact]
    public async Task Telemetry_failure_does_not_mask_audit_persistence_failure()
    {
        using var cancellation = new CancellationTokenSource();
        var telemetry = new ThrowingTelemetry(true, true);
        var runtime = new AgentRuntimeService(null!, null!, new FakeEngine("success", cancellation), new MemoryAudit { Fail = true },
            new JsonSchemaValidator(), telemetry: telemetry);
        var error = await Assert.ThrowsAsync<IOException>(async () => { await foreach (var _ in runtime.StreamAsync(Context())) { } });
        Assert.Equal("offline persistence unavailable", error.Message);
        Assert.Equal(AgentRunStatus.Failed, Assert.Single(telemetry.Runs));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Normalized_audit_roundtrip_preserves_nullable_and_known_usage(bool known)
    {
        using var fixture = new AgentPersistenceSqliteFixture(typeof(AgAgentRunAudit), typeof(AgAgentToolCallAudit));
        var service = new AgAgentRunAuditServices(fixture.CreateRepository<AgAgentRunAudit>());
        var context = Context();
        var running = new AgentRunAuditRecord(context.RunId, context.AgentId, context.Snapshot.VersionId, "offline", AgentRunStatus.Running,
            context.StartedAtUtc, null, "hash", 0, 0, "", []);
        await service.SaveAsync(running);
        Assert.Null(Assert.Single(await service.ListAsync(context.AgentId, 10)).ModelUsage);
        var completed = running with
        {
            Status = AgentRunStatus.Completed, FinishedAtUtc = context.StartedAtUtc.AddSeconds(1), ModelProfileId = "offline-profile",
            ModelUsage = new AgentModelUsage(known ? 0 : null, known ? 3 : null, known ? 3 : null,
                known ? AgentTokenUsageStatus.Reported : AgentTokenUsageStatus.Unknown, 25, null)
        };
        await service.SaveAsync(completed);
        var saved = Assert.Single(await service.ListAsync(context.AgentId, 10));
        Assert.Equal(completed.ModelUsage, saved.ModelUsage);
        Assert.Equal(completed.ModelProfileId, saved.ModelProfileId);
    }

    [Fact]
    public async Task Metrics_are_thread_safe_bounded_and_expose_histograms_and_partial_usage_separately()
    {
        using var metrics = new AgentMetrics();
        await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
        {
            metrics.RecordRun(AgentRunStatus.Completed, new AgentModelUsage(2, 3, 5, AgentTokenUsageStatus.Reported, 250, 100), 500);
            metrics.RecordTool(AgentRunEventKind.ToolSucceeded, 100);
        })));
        metrics.RecordRun(AgentRunStatus.Cancelled, new AgentModelUsage(null, null, null, AgentTokenUsageStatus.Unknown, 10, null), 10);
        metrics.RecordRun(AgentRunStatus.Failed, new AgentModelUsage(1, null, null, AgentTokenUsageStatus.Partial, 10, null), 10);
        metrics.RecordTool(AgentRunEventKind.ToolFailed, 500);
        metrics.RecordTool(AgentRunEventKind.ToolBlocked, 0);
        string text = metrics.RenderPrometheus();
        Assert.Contains("agent_runtime_tokens_total{usage_status=\"Reported\",kind=\"total\"} 500", text);
        Assert.Contains("agent_runtime_tokens_total{usage_status=\"Partial\",kind=\"input\"} 1", text);
        Assert.DoesNotContain("agent_runtime_tokens_total{usage_status=\"Unknown\"", text);
        Assert.Contains("agent_model_time_to_first_text_milliseconds_count 100", text);
        Assert.Contains("agent_runtime_tool_calls_total{status=\"ToolSucceeded\"} 100", text);
        Assert.Contains("agent_tool_duration_milliseconds_bucket{le=\"100\"} 101", text);
        Assert.Contains("agent_tool_duration_milliseconds_bucket{le=\"+Inf\"} 102", text);
        Assert.DoesNotContain("profile", text);
        Assert.DoesNotContain("user_id", text);
        Assert.DoesNotContain("run_id", text);
    }

    [Fact]
    public async Task Actual_autofac_runtime_resolution_shares_host_metrics_across_request_scopes()
    {
        var original = AppSettings.Configuration;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        using var cancellation = new CancellationTokenSource();
        try
        {
            AppSettings.Configuration = configuration;
            var services = new ServiceCollection();
            services.AddSingleton<AgentMetrics>();
            services.AddSingleton<IAgentRuntimeTelemetry>(provider => provider.GetRequiredService<AgentMetrics>());
            var builder = new ContainerBuilder();
            builder.Populate(services);
            builder.RegisterModule(new AutofacModuleRegister());
            RegisterUnused<IAgentDefinitionCatalog>(builder);
            RegisterUnused<IPublishedMcpToolCatalog>(builder);
            RegisterUnused<IKnowledgeRetriever>(builder);
            RegisterUnused<IPublishedSkillVersionCatalog>(builder);
            RegisterUnused<IPublishedSkillContentStore>(builder);
            builder.RegisterInstance(new FakeEngine("success", cancellation)).As<IAgentRuntimeEngine>();
            builder.RegisterInstance(new MemoryAudit()).As<IAgentRunAuditRepository>();
            builder.RegisterInstance(new JsonSchemaValidator());
            using var container = builder.Build();
            AgentMetrics metrics = container.Resolve<AgentMetrics>();
            IAgentRuntimeService? previousRuntime = null;
            for (int i = 0; i < 2; i++)
            {
                using var scope = container.BeginLifetimeScope();
                Assert.Same(metrics, scope.Resolve<IAgentRuntimeTelemetry>());
                var runtime = scope.Resolve<IAgentRuntimeService>();
                Assert.NotSame(previousRuntime, runtime);
                previousRuntime = runtime;
                await foreach (var _ in runtime.StreamAsync(Context())) { }
            }
            Assert.Contains("agent_runtime_runs_total{status=\"Completed\"} 2", metrics.RenderPrometheus());
        }
        finally { AppSettings.Configuration = original; (configuration as IDisposable)?.Dispose(); }
    }

    [Fact]
    public async Task Repeated_mcp_terminal_events_count_once_and_do_not_expose_tool_names()
    {
        using var metrics = new AgentMetrics();
        var audit = new MemoryAudit();
        var tool = new PublishedMcpToolReference(Guid.NewGuid(), "offline-server", "Offline", Guid.NewGuid(), "private-tool-name", "", "{}", McpToolRisk.ReadOnly, "hash");
        var context = Context() with { Tools = [tool] };
        var runtime = new AgentRuntimeService(null!, null!, new RepeatingToolEngine(), audit, new JsonSchemaValidator(), telemetry: metrics);
        await foreach (var _ in runtime.StreamAsync(context)) { }
        Assert.Equal(1, Assert.Single(audit.Records).ToolCallCount);
        string text = metrics.RenderPrometheus();
        Assert.Contains("agent_runtime_tool_calls_total{status=\"ToolSucceeded\"} 1", text);
        Assert.Contains("agent_tool_duration_milliseconds_sum 15", text);
        Assert.DoesNotContain("private-tool-name", text);
    }

    [Fact]
    public void Audit_json_adds_nullable_usage_without_changing_existing_keys()
    {
        var audit = new AgentRunAuditRecord(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "offline", AgentRunStatus.Completed,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "hash", 0, 0, "", [])
        { ModelUsage = new AgentModelUsage(null, null, null, AgentTokenUsageStatus.Unknown, 25, null) };
        var options = new JsonSerializerOptions();
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(audit, options));
        Assert.Equal(audit.RunId, json.RootElement.GetProperty("RunId").GetGuid());
        Assert.Equal("Completed", json.RootElement.GetProperty("Status").GetString());
        Assert.Equal("Unknown", json.RootElement.GetProperty("ModelUsage").GetProperty("Status").GetString());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("ModelUsage").GetProperty("TotalTokens").ValueKind);
    }

    private sealed class RepeatingToolEngine(int callCount = 1) : IAgentRuntimeEngine
    {
        public async IAsyncEnumerable<AgentRunEvent> StreamAsync(AgentRunContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            var tool = Assert.Single(context.Tools);
            for (int i = 0; i < callCount; i++)
            {
                var started = new AgentRunEvent(context.RunId, 0, AgentRunEventKind.ToolStarted, DateTimeOffset.UtcNow,
                    ToolVersionId: tool.ToolVersionId, ToolName: tool.ToolName, ToolCallId: Guid.NewGuid());
                yield return started;
                var succeeded = started with { Kind = AgentRunEventKind.ToolSucceeded, OccurredAtUtc = started.OccurredAtUtc.AddMilliseconds(15) };
                yield return succeeded;
                yield return succeeded;
            }
        }
    }

    private sealed class ThrowingTelemetry(bool throwRun, bool throwTool) : IAgentRuntimeTelemetry
    {
        public List<AgentRunStatus> Runs { get; } = [];
        public List<AgentRunEventKind> Tools { get; } = [];
        public void RecordRun(AgentRunStatus status, AgentModelUsage? usage, long durationMilliseconds)
        {
            Runs.Add(status);
            if (throwRun) throw new InvalidOperationException("offline run telemetry failure");
        }
        public void RecordTool(AgentRunEventKind status, long durationMilliseconds)
        {
            Tools.Add(status);
            if (throwTool) throw new InvalidOperationException("offline tool telemetry failure");
        }
    }

    private static void RegisterUnused<T>(ContainerBuilder builder) where T : class =>
        builder.RegisterInstance(DispatchProxy.Create<T, UnavailableProxy>()).As<T>();

    public class UnavailableProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => throw new NotSupportedException("Not used by offline runtime streaming test.");
    }

    private static UsageDetails Counts(long? input, long? output, long? total) => new()
    { InputTokenCount = input, OutputTokenCount = output, TotalTokenCount = total };

    private static AgentRunContext Context() => new(Guid.NewGuid(), Guid.NewGuid(),
        new AgentVersionSnapshot(Guid.NewGuid(), "offline", "instructions", "offline-profile", AgentOutputMode.Text, null, [], []),
        "hello", "hash", DateTimeOffset.UtcNow, []);

    private sealed class UnusedResolver : IAgentModelProfileResolver
    {
        public Task<AgentModelRuntimeProfile> ResolveAsync(string profileCode, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakeModel(string outcome, CancellationTokenSource cancellation) : IMicrosoftAgentRuntimeModelClient
    {
        public async IAsyncEnumerable<MicrosoftAgentRuntimeModelUpdate> StreamAsync(AgentRunContext context, string apiKey,
            IReadOnlyList<AITool> tools, IReadOnlyList<ChatMessage> messages, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new("", ResponseId: "a", Usages: [Counts(2, 3, 5)]);
            if (outcome == "text") yield return new("hello", ResponseId: "a");
            if (outcome == "fail") throw new IOException("offline failure");
            if (outcome == "cancel") { cancellation.Cancel(); cancellationToken.ThrowIfCancellationRequested(); }
        }
    }

    private sealed class FakeEngine(string outcome, CancellationTokenSource cancellation) : IAgentRuntimeEngine
    {
        public async IAsyncEnumerable<AgentRunEvent> StreamAsync(AgentRunContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            if (outcome.StartsWith("cancel", StringComparison.Ordinal)) cancellation.Cancel();
            if (outcome == "cancel-delta") yield return new(context.RunId, 0, AgentRunEventKind.Delta, DateTimeOffset.UtcNow, "already cancelled");
            yield return new(context.RunId, 0, AgentRunEventKind.ModelUsage, DateTimeOffset.UtcNow)
            { ModelUsage = new AgentModelUsage(2, 3, 5, outcome == "success" ? AgentTokenUsageStatus.Reported : AgentTokenUsageStatus.Partial, 25, null) };
            if (outcome == "fail") throw new IOException("offline failure");
            if (outcome.StartsWith("cancel", StringComparison.Ordinal)) cancellationToken.ThrowIfCancellationRequested();
            if (outcome == "approval") yield return new(context.RunId, 0, AgentRunEventKind.ApprovalRequired, DateTimeOffset.UtcNow);
        }
    }

    private sealed class MemoryAudit : IAgentRunAuditRepository
    {
        public List<AgentRunAuditRecord> Records { get; } = [];
        public CancellationToken SaveToken;
        public bool Fail;
        public Task SaveAsync(AgentRunAuditRecord record, CancellationToken cancellationToken = default)
        {
            if (Fail) throw new IOException("offline persistence unavailable");
            SaveToken = cancellationToken;
            Records.Add(record);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<AgentRunAuditRecord>> ListAsync(Guid agentId, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AgentRunAuditRecord>>(Records);
    }

    private sealed class StreamingHandler : HttpMessageHandler
    {
        public int Requests;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            Assert.True(body.RootElement.GetProperty("stream_options").GetProperty("include_usage").GetBoolean());
            int call = ++Requests;
            string id = call == 1 ? "a" : "b";
            string delta = call == 1
                ? """{"tool_calls":[{"index":0,"id":"lookup-1","type":"function","function":{"name":"lookup","arguments":"{}"}}]}"""
                : """{"content":"ok"}""";
            string usage = call == 1 ? """{"prompt_tokens":2,"completion_tokens":3,"total_tokens":5}""" : """{"prompt_tokens":7,"completion_tokens":11,"total_tokens":18}""";
            string content = $"data: {{\"id\":\"{id}\",\"object\":\"chat.completion.chunk\",\"created\":0,\"model\":\"offline\",\"choices\":[{{\"index\":0,\"delta\":{delta},\"finish_reason\":null}}]}}\n\n" +
                $"data: {{\"id\":\"{id}\",\"object\":\"chat.completion.chunk\",\"created\":0,\"model\":\"offline\",\"choices\":[{{\"index\":0,\"delta\":{{}},\"finish_reason\":\"{(call == 1 ? "tool_calls" : "stop")}\"}}]}}\n\n" +
                $"data: {{\"id\":\"{id}\",\"object\":\"chat.completion.chunk\",\"created\":0,\"model\":\"offline\",\"choices\":[],\"usage\":{usage}}}\n\ndata: [DONE]\n\n";
            return new(HttpStatusCode.OK) { Content = new StringContent(content, Encoding.UTF8, "text/event-stream") };
        }
    }
}
