#nullable enable
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using EU.Core.Agent.Runtime;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Observability;
using EU.Core.IServices;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;
using EU.Core.IServices.Orchestration;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI;
using Xunit;
using AgentRunContext = EU.Core.IServices.Runtime.AgentRunContext;

namespace EU.Core.Tests;

/// <summary>单次运行 Token 预算的离线回归；只使用假模型、内存审计及拦截 SDK HTTP 的处理器。</summary>
public sealed class AgentTokenBudgetTests
{
    [Theory]
    [InlineData(null, null, true)]
    [InlineData(1, 1L, true)]
    [InlineData(100, 5L, true)]
    [InlineData(0, null, false)]
    [InlineData(-1, null, false)]
    [InlineData(null, 0L, false)]
    [InlineData(null, -1L, false)]
    public void Host_and_runtime_validate_optional_positive_limits(int? output, long? total, bool valid)
    {
        var host = new AgentExecutionOptions { MaximumModelOutputTokens = output, MaximumRunTotalTokens = total };
        Assert.Equal(valid, new AgentExecutionOptionsValidator().Validate(null, host).Succeeded);
        var runtime = Options(output, total);
        if (valid) { using var client = new AgentTokenBudgetChatClient(new ScriptedClient(), runtime); }
        else Assert.Throws<ArgumentOutOfRangeException>(() => new AgentTokenBudgetChatClient(new ScriptedClient(), runtime));
    }

    [Fact]
    public void Existing_host_configuration_section_binds_limits_without_new_sources()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AgentExecution:MaximumModelOutputTokens"] = "4096",
            ["AgentExecution:MaximumRunTotalTokens"] = "20000"
        }).Build();
        using var cleanup = configuration as IDisposable;
        var host = configuration.GetSection(AgentExecutionOptions.SectionName).Get<AgentExecutionOptions>()!;
        Assert.Equal(4096, host.MaximumModelOutputTokens);
        Assert.Equal(20000, host.MaximumRunTotalTokens);
        Assert.Equal(32, host.MaximumMcpToolCalls);
    }

    [Fact]
    public async Task Limits_clone_options_preserve_extensions_and_never_raise_existing_output_cap()
    {
        var inner = new ScriptedClient(Reports(3, 1));
        using var client = new AgentTokenBudgetChatClient(inner, Options(10, 20));
        AITool lookup = AIFunctionFactory.Create(() => "ok", name: "lookup");
        ChatOptions options = MicrosoftAgentRuntimeEngine.CreateChatOptions("qwen-offline", "keep instructions", [lookup], false);
        options.MaxOutputTokens = 2;
        await client.GetResponseAsync(Messages(), options);
        var sent = Assert.Single(inner.Options)!;
        Assert.NotSame(options, sent);
        Assert.Equal(2, options.MaxOutputTokens);
        Assert.Equal(2, sent.MaxOutputTokens);
        Assert.Equal(options.Instructions, sent.Instructions);
        Assert.Same(options.RawRepresentationFactory, sent.RawRepresentationFactory);
        Assert.Same(lookup, Assert.Single(sent.Tools!));
    }

    [Fact]
    public async Task Disabled_budget_preserves_unknown_usage_and_existing_length_finish_behavior()
    {
        var inner = new ScriptedClient([new ChatResponseUpdate { FinishReason = ChatFinishReason.Length }]);
        using var client = new AgentTokenBudgetChatClient(inner, Options());
        var original = new ChatOptions { MaxOutputTokens = 3 };
        await ReadAsync(client, original);
        Assert.Same(original, Assert.Single(inner.Options));
    }

    [Fact]
    public async Task Streaming_snapshots_are_coalesced_per_response_and_tool_rounds_share_remaining_total()
    {
        var first = Reports(3, 1).Concat(Reports(5, 2)).Concat(Reports(5, 2)).ToArray();
        var inner = new ScriptedClient(first, Reports(7, 3), Reports(8, 4));
        using var client = new AgentTokenBudgetChatClient(inner, Options(10, 20));
        for (int i = 0; i < 3; i++) await ReadAsync(client);
        var exception = await Assert.ThrowsAsync<AgentRuntimeException>(() => ReadAsync(client));
        Assert.Equal(AgentRunErrorCodes.ModelTokenBudgetExceeded, exception.ErrorCode);
        Assert.Equal(new int?[] { 10, 10, 8 }, inner.Options.Select(value => value?.MaxOutputTokens));
        Assert.Equal(3, inner.Requests);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1L)]
    public async Task Enabled_total_budget_rejects_missing_or_invalid_total_and_prevents_retry(long? total)
    {
        var inner = new ScriptedClient(Reports(total, 1));
        using var client = new AgentTokenBudgetChatClient(inner, Options(total: 10));
        var events = new List<ChatResponseUpdate>();
        var exception = await Assert.ThrowsAsync<AgentRuntimeException>(() => ReadAsync(client, captured: events));
        Assert.Equal(AgentRunErrorCodes.ModelTokenUsageUnavailable, exception.ErrorCode);
        Assert.NotEmpty(events);
        var retry = await Assert.ThrowsAsync<AgentRuntimeException>(() => ReadAsync(client));
        Assert.Equal(exception.ErrorCode, retry.ErrorCode);
        Assert.Equal(1, inner.Requests);
    }

    [Fact]
    public async Task Reported_zero_is_not_missing_and_total_can_be_used_without_other_fields()
    {
        var inner = new ScriptedClient(Reports(0, null), Reports(5, null));
        using var client = new AgentTokenBudgetChatClient(inner, Options(total: 10));
        await ReadAsync(client);
        await ReadAsync(client);
        Assert.Equal(new int?[] { 10, 10 }, inner.Options.Select(value => value?.MaxOutputTokens));
    }

    [Fact]
    public async Task Over_budget_response_delivers_actual_usage_then_fails_without_another_request()
    {
        var inner = new ScriptedClient(Reports(5, 2));
        using var client = new AgentTokenBudgetChatClient(inner, Options(total: 4));
        var events = new List<ChatResponseUpdate>();
        var exception = await Assert.ThrowsAsync<AgentRuntimeException>(() => ReadAsync(client, captured: events));
        Assert.Equal(AgentRunErrorCodes.ModelTokenBudgetExceeded, exception.ErrorCode);
        Assert.Equal(5, Assert.Single(events.SelectMany(value => value.Contents).OfType<UsageContent>()).Details.TotalTokenCount);
        await Assert.ThrowsAsync<AgentRuntimeException>(() => ReadAsync(client));
        Assert.Equal(1, inner.Requests);
    }

    [Theory]
    [InlineData("length", 2L)]
    [InlineData("stop", 4L)]
    public async Task Token_truncation_or_provider_ignoring_output_cap_is_not_success(string finish, long output)
    {
        using var metrics = new AgentMetrics();
        var inner = new ScriptedClient(Reports(5, output, finish));
        using var client = new AgentTokenBudgetChatClient(inner, Options(output: 3), telemetry: metrics);
        var exception = await Assert.ThrowsAsync<AgentRuntimeException>(() => ReadAsync(client));
        Assert.Equal(AgentRunErrorCodes.ModelTokenBudgetExceeded, exception.ErrorCode);
        string signal = finish == "length" ? "OutputLimited" : "Exceeded";
        Assert.Contains($"agent_runtime_token_budget_signals_total{{scope=\"ModelOutput\",signal=\"{signal}\"}} 1", metrics.RenderPrometheus());
        Assert.DoesNotContain($"signal=\"{(finish == "length" ? "Exceeded" : "OutputLimited")}\"", metrics.RenderPrometheus());
    }

    [Fact]
    public async Task Output_only_cap_does_not_require_total_usage()
    {
        var inner = new ScriptedClient(Reports(null, null));
        using var client = new AgentTokenBudgetChatClient(inner, Options(output: 3));
        await ReadAsync(client);
        Assert.Equal(3, Assert.Single(inner.Options)!.MaxOutputTokens);
    }

    [Fact]
    public async Task Cancelled_stream_releases_request_gate_but_unknown_spend_cannot_be_retried()
    {
        var inner = new ScriptedClient(Reports(5, 1));
        using var client = new AgentTokenBudgetChatClient(inner, Options(total: 10));
        using var cancellation = new CancellationTokenSource();
        await using (var reader = client.GetStreamingResponseAsync(Messages(), cancellationToken: cancellation.Token).GetAsyncEnumerator())
        {
            Assert.True(await reader.MoveNextAsync());
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await reader.MoveNextAsync());
        }
        var exception = await Assert.ThrowsAsync<AgentRuntimeException>(() => ReadAsync(client));
        Assert.Equal(AgentRunErrorCodes.ModelTokenUsageUnavailable, exception.ErrorCode);
        Assert.Equal(1, inner.Requests);
    }

    [Fact]
    public async Task Concurrent_requests_are_serialized_and_separate_runs_do_not_share_budget()
    {
        var firstInner = new ScriptedClient(Reports(5, 2), Reports(5, 2));
        using var first = new AgentTokenBudgetChatClient(firstInner, Options(total: 10));
        await Task.WhenAll(first.GetResponseAsync(Messages()), first.GetResponseAsync(Messages()));
        await Assert.ThrowsAsync<AgentRuntimeException>(() => first.GetResponseAsync(Messages()));
        var secondInner = new ScriptedClient(Reports(5, 2));
        using var second = new AgentTokenBudgetChatClient(secondInner, Options(total: 10));
        await second.GetResponseAsync(Messages());
        Assert.Equal(2, firstInner.Requests);
        Assert.Equal(1, secondInner.Requests);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(1L, true)]
    [InlineData(long.MaxValue, true)]
    [InlineData(0L, false)]
    [InlineData(-1L, false)]
    public void Shared_limits_validate_at_host_and_scope(long? total, bool valid)
    {
        var host = new UnifiedEntryOptions { MaximumModelTotalTokens = total };
        Assert.Equal(valid, new UnifiedEntryOptionsValidator().Validate(null, host).Succeeded);
        Assert.Equal(total, host.ToLimits().MaxModelTotalTokens);
        if (valid)
        {
            using var scope = new UnifiedEntryExecutionScope(limits: host.ToLimits());
            Assert.Equal(total.HasValue, scope.ModelTokenBudget is not null);
        }
        else Assert.Throws<ArgumentOutOfRangeException>(() => new UnifiedEntryExecutionScope(limits: host.ToLimits()));
    }

    [Fact]
    public void Shared_limit_binds_existing_unified_entry_configuration()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["UnifiedEntry:MaximumModelTotalTokens"] = "30000" }).Build();
        using var cleanup = configuration as IDisposable;
        var host = configuration.GetSection(UnifiedEntryOptions.SectionName).Get<UnifiedEntryOptions>()!;
        Assert.Equal(30000, host.ToLimits().MaxModelTotalTokens);
        Assert.Equal(8, host.MaximumChildCalls);
    }

    [Fact]
    public async Task Parent_and_children_use_one_total_and_independent_entries_do_not_share_it()
    {
        using var scope = SharedScope(20);
        var parentInner = new ScriptedClient(Reports(5, 2), Reports(8, 2));
        var childInner = new ScriptedClient(Reports(7, 2));
        using var parent = new AgentTokenBudgetChatClient(parentInner, Options(), scope.ModelTokenBudget);
        using var child = new AgentTokenBudgetChatClient(childInner, Options(), scope.ModelTokenBudget);
        await ReadAsync(parent);
        await child.GetResponseAsync(Messages());
        await ReadAsync(parent);
        using var freshChild = new AgentTokenBudgetChatClient(new ScriptedClient(), Options(), scope.ModelTokenBudget);
        var exception = await Assert.ThrowsAsync<AgentRuntimeException>(() => ReadAsync(freshChild));
        Assert.Equal(AgentRunErrorCodes.ModelTokenBudgetExceeded, exception.ErrorCode);
        Assert.Equal(new int?[] { 20, 8 }, parentInner.Options.Select(value => value?.MaxOutputTokens));
        Assert.Equal(15, Assert.Single(childInner.Options)!.MaxOutputTokens);
        using var otherScope = SharedScope(20);
        using var other = new AgentTokenBudgetChatClient(new ScriptedClient(Reports(20, 2)), Options(), otherScope.ModelTokenBudget);
        await ReadAsync(other);
    }

    [Fact]
    public async Task Concurrent_children_wait_for_actual_charge_before_sending_their_request()
    {
        using var scope = SharedScope(10);
        var firstInner = new ScriptedClient(Reports(5, 1));
        var secondInner = new ScriptedClient(Reports(5, 1));
        using var first = new AgentTokenBudgetChatClient(firstInner, Options(), scope.ModelTokenBudget);
        using var second = new AgentTokenBudgetChatClient(secondInner, Options(), scope.ModelTokenBudget);
        await using (var reader = first.GetStreamingResponseAsync(Messages()).GetAsyncEnumerator())
        {
            Assert.True(await reader.MoveNextAsync());
            Task waiting = second.GetResponseAsync(Messages());
            Assert.False(waiting.IsCompleted);
            Assert.Equal(0, secondInner.Requests);
            while (await reader.MoveNextAsync()) { }
            await waiting.WaitAsync(TimeSpan.FromSeconds(5));
        }
        Assert.Equal(10, Assert.Single(firstInner.Options)!.MaxOutputTokens);
        Assert.Equal(5, Assert.Single(secondInner.Options)!.MaxOutputTokens);
        await Assert.ThrowsAsync<AgentRuntimeException>(() => ReadAsync(first));
    }

    [Fact]
    public async Task Cancellation_while_waiting_does_not_charge_or_poison_the_shared_budget()
    {
        using var metrics = new AgentMetrics();
        using var scope = new UnifiedEntryExecutionScope(limits: new UnifiedEntryLimits(MaxModelTotalTokens: 10), telemetry: metrics);
        using var first = new AgentTokenBudgetChatClient(new ScriptedClient(Reports(4, 1)), Options(), scope.ModelTokenBudget);
        var childInner = new ScriptedClient(Reports(6, 1));
        using var child = new AgentTokenBudgetChatClient(childInner, Options(), scope.ModelTokenBudget);
        await using var reader = first.GetStreamingResponseAsync(Messages()).GetAsyncEnumerator();
        Assert.True(await reader.MoveNextAsync());
        using var cancellation = new CancellationTokenSource();
        Task waiting = child.GetResponseAsync(Messages(), cancellationToken: cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        Assert.Equal(0, childInner.Requests);
        while (await reader.MoveNextAsync()) { }
        await child.GetResponseAsync(Messages());
        Assert.Equal(6, Assert.Single(childInner.Options)!.MaxOutputTokens);
        Assert.DoesNotContain("signal=\"UsageUnavailable\"", metrics.RenderPrometheus());
        Assert.Contains("agent_runtime_token_budget_signals_total{scope=\"ExecutionTree\",signal=\"Exhausted\"} 1", metrics.RenderPrometheus());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1L)]
    [InlineData(11L)]
    public async Task Invalid_or_excess_shared_spend_blocks_fresh_child_clients(long? total)
    {
        using var scope = SharedScope(10);
        using var first = new AgentTokenBudgetChatClient(new ScriptedClient(Reports(total, 1)), Options(), scope.ModelTokenBudget);
        var expected = total > 10 ? AgentRunErrorCodes.ModelTokenBudgetExceeded : AgentRunErrorCodes.ModelTokenUsageUnavailable;
        var values = new List<ChatResponseUpdate>();
        var exception = await Assert.ThrowsAsync<AgentRuntimeException>(() => ReadAsync(first, captured: values));
        Assert.Equal(expected, exception.ErrorCode);
        Assert.NotEmpty(values); // 实际 usage 先交给运行审计。
        var childInner = new ScriptedClient();
        using var child = new AgentTokenBudgetChatClient(childInner, Options(), scope.ModelTokenBudget);
        Assert.Equal(expected, (await Assert.ThrowsAsync<AgentRuntimeException>(() => ReadAsync(child))).ErrorCode);
        Assert.Equal(0, childInner.Requests);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Interrupted_request_closes_tree_budget_but_releasing_unstarted_lease_does_not(bool cancel)
    {
        using var metrics = new AgentMetrics();
        using var scope = new UnifiedEntryExecutionScope(limits: new UnifiedEntryLimits(MaxModelTotalTokens: 10), telemetry: metrics);
        using (await scope.ModelTokenBudget!.ReserveAsync()) { }
        Assert.DoesNotContain("signal=\"UsageUnavailable\"", metrics.RenderPrometheus());
        using var first = new AgentTokenBudgetChatClient(new ScriptedClient(Reports(4, 1)), Options(total: 10), scope.ModelTokenBudget, metrics);
        using var cancellation = new CancellationTokenSource();
        await using (var reader = first.GetStreamingResponseAsync(Messages(), cancellationToken: cancellation.Token).GetAsyncEnumerator())
        {
            Assert.True(await reader.MoveNextAsync()); // 提前释放而非收到完整响应。
            if (cancel)
            {
                cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await reader.MoveNextAsync());
            }
        }
        using var child = new AgentTokenBudgetChatClient(new ScriptedClient(), Options(), scope.ModelTokenBudget);
        var exception = await Assert.ThrowsAsync<AgentRuntimeException>(() => ReadAsync(child));
        Assert.Equal(AgentRunErrorCodes.ModelTokenUsageUnavailable, exception.ErrorCode);
        foreach (string range in new[] { "AgentRun", "ExecutionTree" })
            Assert.Contains($"agent_runtime_token_budget_signals_total{{scope=\"{range}\",signal=\"UsageUnavailable\"}} 1", metrics.RenderPrometheus());
        Assert.DoesNotContain("signal=\"Warning\"", metrics.RenderPrometheus());
    }

    [Fact]
    public void Shared_budget_is_not_serialized_with_context_or_execution_options()
    {
        using var scope = SharedScope(10);
        var context = Context() with { ModelTokenBudget = scope.ModelTokenBudget };
        var options = new AgentRunExecutionOptions { ModelTokenBudget = scope.ModelTokenBudget };
        Assert.DoesNotContain("ModelTokenBudget", JsonSerializer.Serialize(context));
        Assert.DoesNotContain("ModelTokenBudget", Newtonsoft.Json.JsonConvert.SerializeObject(context));
        Assert.DoesNotContain("ModelTokenBudget", JsonSerializer.Serialize(options));
        Assert.DoesNotContain("ModelTokenBudget", Newtonsoft.Json.JsonConvert.SerializeObject(options));
    }

    [Fact]
    public async Task Local_failure_still_charges_shared_usage_and_smaller_limits_are_preserved()
    {
        using var scope = SharedScope(20);
        var firstInner = new ScriptedClient(Reports(5, 1));
        using var first = new AgentTokenBudgetChatClient(firstInner, Options(8, 4), scope.ModelTokenBudget);
        await Assert.ThrowsAsync<AgentRuntimeException>(() => ReadAsync(first));
        Assert.Equal(4, Assert.Single(firstInner.Options)!.MaxOutputTokens);
        var childInner = new ScriptedClient(Reports(5, 1));
        using var child = new AgentTokenBudgetChatClient(childInner, Options(18, 19), scope.ModelTokenBudget);
        var original = new ChatOptions { MaxOutputTokens = 2 };
        await child.GetResponseAsync(Messages(), original);
        Assert.Equal(2, Assert.Single(childInner.Options)!.MaxOutputTokens);
        Assert.Equal(2, original.MaxOutputTokens);
        using var remaining = await scope.ModelTokenBudget!.ReserveAsync();
        Assert.Equal(10, remaining.RemainingTokens);
    }

    [Fact]
    public async Task Shared_zero_usage_and_duplicate_snapshots_do_not_spend_twice()
    {
        using var scope = SharedScope(10);
        var reports = Reports(2, 1).Concat(Reports(5, 2)).Concat(Reports(5, 2)).ToArray();
        using var client = new AgentTokenBudgetChatClient(new ScriptedClient(Reports(0, null), reports), Options(), scope.ModelTokenBudget);
        await ReadAsync(client);
        await ReadAsync(client);
        using var lease = await scope.ModelTokenBudget!.ReserveAsync();
        Assert.Equal(5, lease.RemainingTokens);
        lease.MarkStarted();
        lease.Complete(0, false);
        Assert.Throws<InvalidOperationException>(() => lease.Complete(0, false));
    }

    [Fact]
    public async Task Scope_disposal_cancels_budget_waiters_and_allows_inflight_release()
    {
        using var scope = SharedScope(10);
        using var lease = await scope.ModelTokenBudget!.ReserveAsync();
        Task waiting = scope.ModelTokenBudget.ReserveAsync().AsTask();
        Assert.False(waiting.IsCompleted);
        scope.Dispose();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        lease.Dispose();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scope.ModelTokenBudget.ReserveAsync().AsTask());
    }

    [Theory]
    [InlineData(11L, 6L, 1, AgentRunErrorCodes.ModelTokenBudgetExceeded)]
    [InlineData(14L, 3L, 2, null)]
    [InlineData(12L, 3L, 2, AgentRunErrorCodes.ModelTokenBudgetExceeded)]
    public async Task Installed_sdk_releases_parent_request_before_child_and_audits_parent_only(
        long maximum, long childSpend, int requests, string? error)
    {
        using var scope = SharedScope(maximum);
        using var handler = new BudgetHandler(true);
        using var http = new HttpClient(handler);
        var inner = new OpenAIClient(new ApiKeyCredential("offline-placeholder"), new OpenAIClientOptions
        { Endpoint = new Uri("https://offline.invalid/v1"), Transport = new HttpClientPipelineTransport(http) })
            .GetChatClient("qwen-offline").AsIChatClient();
        using var parent = new AgentTokenBudgetChatClient(inner, Options(10), scope.ModelTokenBudget);
        var childInner = new ScriptedClient(Reports(childSpend, 1));
        AITool delegateTool = AIFunctionFactory.Create(async () =>
        {
            using var child = new AgentTokenBudgetChatClient(childInner, Options(), scope.ModelTokenBudget);
            await child.GetResponseAsync(Messages());
            return "child completed";
        }, name: "lookup");
        var audit = new MemoryAudit();
        var runtime = new AgentRuntimeService(null!, null!, new ProducerEngine(Options(), new SdkModel(parent, delegateTool)), audit, new JsonSchemaValidator());
        async Task RunAsync() { await foreach (var value in runtime.StreamAsync(Context())) { } }
        await RunAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, childInner.Requests);
        Assert.Equal((int)(maximum - 5), Assert.Single(childInner.Options)!.MaxOutputTokens);
        Assert.Equal(requests, handler.OutputCaps.Count);
        if (requests == 2) Assert.Equal((int)(maximum - 5 - childSpend), handler.OutputCaps[1]);
        var saved = Assert.Single(audit.Records);
        Assert.Equal(error is null ? AgentRunStatus.Completed : AgentRunStatus.Failed, saved.Status);
        Assert.Equal(error ?? "", saved.ErrorCode);
        Assert.Equal(requests == 2 ? 11 : 5, saved.ModelUsage!.TotalTokens); // 父审计不重复累计子用量。
        Assert.False(audit.SaveToken.CanBeCanceled);
    }

    private static UnifiedEntryExecutionScope SharedScope(long total) => new(limits: new UnifiedEntryLimits(MaxModelTotalTokens: total));

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Real_budget_path_reports_warning_and_exhaustion_once(bool shared, bool streaming)
    {
        using var metrics = new AgentMetrics();
        using var scope = new UnifiedEntryExecutionScope(limits: new UnifiedEntryLimits(MaxModelTotalTokens: shared ? 100 : null), telemetry: metrics);
        var inner = new ScriptedClient(Reports(80, 2), Reports(10, 2), Reports(10, 2));
        using var client = new AgentTokenBudgetChatClient(inner, Options(total: shared ? null : 100), scope.ModelTokenBudget, metrics);
        async Task Request() { if (streaming) await ReadAsync(client); else await client.GetResponseAsync(Messages()); }
        for (int i = 0; i < 3; i++) await Request();
        string range = shared ? "ExecutionTree" : "AgentRun";
        Assert.Contains($"agent_runtime_token_budget_signals_total{{scope=\"{range}\",signal=\"Warning\"}} 1", metrics.RenderPrometheus());
        Assert.Contains($"agent_runtime_token_budget_signals_total{{scope=\"{range}\",signal=\"Exhausted\"}} 1", metrics.RenderPrometheus());
        string beforeRetry = metrics.RenderPrometheus();
        await Assert.ThrowsAsync<AgentRuntimeException>(Request);
        Assert.Equal(beforeRetry, metrics.RenderPrometheus());
        Assert.Equal(3, inner.Requests);
    }

    [Theory]
    [InlineData(null, AgentTokenBudgetSignal.UsageUnavailable)]
    [InlineData(-1L, AgentTokenBudgetSignal.UsageUnavailable)]
    [InlineData(101L, AgentTokenBudgetSignal.Exceeded)]
    public async Task Both_enabled_ranges_observe_failure_before_shared_guard_throws(long? total, AgentTokenBudgetSignal signal)
    {
        using var metrics = new AgentMetrics();
        using var scope = new UnifiedEntryExecutionScope(limits: new UnifiedEntryLimits(MaxModelTotalTokens: 100), telemetry: metrics);
        using var client = new AgentTokenBudgetChatClient(new ScriptedClient(Reports(total, 2)), Options(total: 100), scope.ModelTokenBudget, metrics);
        await Assert.ThrowsAsync<AgentRuntimeException>(() => ReadAsync(client));
        foreach (string range in new[] { "AgentRun", "ExecutionTree" })
            Assert.Contains($"agent_runtime_token_budget_signals_total{{scope=\"{range}\",signal=\"{signal}\"}} 1", metrics.RenderPrometheus());
        Assert.DoesNotContain("signal=\"Warning\"", metrics.RenderPrometheus());
    }

    [Fact]
    public async Task Warning_is_optional_and_disabled_budget_produces_no_signals()
    {
        using var metrics = new AgentMetrics();
        using (var disabled = new AgentTokenBudgetChatClient(new ScriptedClient(Reports(null, 1000, "length")), Options(), telemetry: metrics))
            await ReadAsync(disabled);
        Assert.DoesNotContain("agent_runtime_token_budget_signals_total{", metrics.RenderPrometheus());
        var options = Options(total: 100) with { TokenBudgetWarningPercent = null };
        using var client = new AgentTokenBudgetChatClient(new ScriptedClient(Reports(90, 2), Reports(10, 2)), options, telemetry: metrics);
        await ReadAsync(client);
        await ReadAsync(client);
        Assert.DoesNotContain("signal=\"Warning\"", metrics.RenderPrometheus());
        Assert.Contains("scope=\"AgentRun\",signal=\"Exhausted\"} 1", metrics.RenderPrometheus());
        await Assert.ThrowsAsync<AgentRuntimeException>(() => ReadAsync(client));
    }

    [Fact]
    public async Task Output_warning_and_truncation_are_distinct_and_do_not_change_runtime_failure()
    {
        using var metrics = new AgentMetrics();
        using var client = new AgentTokenBudgetChatClient(new ScriptedClient(Reports(null, 8), Reports(null, 9), Reports(null, 2, "length")),
            Options(output: 10), telemetry: metrics);
        await ReadAsync(client);
        await ReadAsync(client);
        var exception = await Assert.ThrowsAsync<AgentRuntimeException>(() => ReadAsync(client));
        Assert.Equal(AgentRunErrorCodes.ModelTokenBudgetExceeded, exception.ErrorCode);
        Assert.Contains("scope=\"ModelOutput\",signal=\"Warning\"} 1", metrics.RenderPrometheus());
        Assert.Contains("scope=\"ModelOutput\",signal=\"OutputLimited\"} 1", metrics.RenderPrometheus());
        Assert.DoesNotContain("signal=\"Exceeded\"", metrics.RenderPrometheus());
    }

    [Theory]
    [InlineData(10L, 2, true)]
    [InlineData(5L, 1, false)]
    public async Task Orchestration_node_retries_keep_shared_budget_in_new_run_contexts(long maximum, int requests, bool succeeded)
    {
        using var scope = SharedScope(maximum);
        var inner = new ScriptedClient(Reports(5, 1), Reports(5, 1));
        var contexts = new List<AgentRunContext>();
        async IAsyncEnumerable<AgentRunEvent> Stream(AgentRunContext context, [EnumeratorCancellation] CancellationToken token)
        {
            contexts.Add(context);
            using var client = new AgentTokenBudgetChatClient(inner, Options(), context.ModelTokenBudget);
            string error = "";
            try { await client.GetResponseAsync(Messages(), cancellationToken: token); }
            catch (AgentRuntimeException exception) { error = exception.ErrorCode; }
            yield return new AgentRunEvent(context.RunId, 1,
                contexts.Count == 1 || error.Length > 0 ? AgentRunEventKind.Failed : AgentRunEventKind.Completed,
                DateTimeOffset.UtcNow, ErrorCode: error.Length > 0 ? error : contexts.Count == 1 ? "TEST_RETRY" : "");
        }
        var runtime = Proxy<IAgentRuntimeService>((method, args) => method.Name switch
        {
            "PrepareVersionAsync" => Task.FromResult(AgentRunPreparationResult.Success(Context())),
            "StreamAsync" => Stream((AgentRunContext)args![0]!, (CancellationToken)args[1]!),
            _ => throw new NotSupportedException(method.Name)
        });
        Guid runId = Guid.NewGuid();
        Guid orchestrationId = Guid.NewGuid();
        var node = new OrchestrationNode("test", "offline", Guid.NewGuid(), OrchestrationNodeInputMode.InitialInput, "", 1, 10);
        var nodeRun = new OrchestrationNodeRunRecord(node.Id, node.Name, node.AgentId, Guid.NewGuid(),
            OrchestrationNodeRunStatus.Pending, 0, null, null, 0, "", "");
        var record = new OrchestrationRunRecord(runId, orchestrationId, Guid.NewGuid(), "offline",
            OrchestrationRunStatus.Running, DateTimeOffset.UtcNow, null, "", "", [nodeRun]);
        var details = new OrchestrationRunDetails(runId, orchestrationId, "offline", "", []);
        var runs = Proxy<IOrchestrationRunRepository>((method, args) =>
        {
            switch (method.Name)
            {
                case "GetAsync": return Task.FromResult<OrchestrationRunRecord?>(record);
                case "GetDetailsAsync": return Task.FromResult<OrchestrationRunDetails?>(details);
                case "SaveAsync": record = (OrchestrationRunRecord)args![0]!; return Task.CompletedTask;
                case "TrySaveRunningDetailsAsync": details = (OrchestrationRunDetails)args![0]!; return Task.FromResult(true);
                default: throw new NotSupportedException(method.Name);
            }
        });
        var service = new OrchestrationRuntimeService(null!, runs, null!, runtime);
        var options = new AgentRunExecutionOptions { ModelTokenBudget = scope.ModelTokenBudget };
        var execution = (Task<(bool succeeded, string output, string errorCode)>)typeof(OrchestrationRuntimeService)
            .GetMethod("ExecuteNodeAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(service, [runId, node, nodeRun.AgentVersionId, "offline", options, CancellationToken.None])!;
        var result = await execution.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(succeeded, result.succeeded);
        Assert.Equal(succeeded ? "" : AgentRunErrorCodes.ModelTokenBudgetExceeded, result.errorCode);
        Assert.Equal(2, contexts.Count);
        Assert.All(contexts, context => Assert.Same(scope.ModelTokenBudget, context.ModelTokenBudget));
        Assert.NotEqual(contexts[0].RunId, contexts[1].RunId);
        Assert.Equal(requests, inner.Requests);
        Assert.Equal(2, details.Attempts.Count);
        Assert.Equal(succeeded ? OrchestrationNodeRunStatus.Completed : OrchestrationNodeRunStatus.Failed, record.Nodes[0].Status);
        if (succeeded) Assert.Equal(new int?[] { 10, 5 }, inner.Options.Select(value => value?.MaxOutputTokens));
    }

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
    {
        T result = DispatchProxy.Create<T, TestProxy>();
        ((TestProxy)(object)result).Handler = handler;
        return result;
    }

    public class TestProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!, args);
    }

    [Theory]
    [InlineData(null, true, 2, null)]
    [InlineData(12L, true, 2, null)]
    [InlineData(5L, true, 1, AgentRunErrorCodes.ModelTokenBudgetExceeded)]
    [InlineData(4L, true, 1, AgentRunErrorCodes.ModelTokenBudgetExceeded)]
    [InlineData(12L, false, 1, AgentRunErrorCodes.ModelTokenUsageUnavailable)]
    public async Task Installed_sdk_applies_budget_inside_tool_loop_and_preserves_usage_in_failed_audit(
        long? total, bool reportUsage, int requests, string? error)
    {
        using var handler = new BudgetHandler(reportUsage);
        using var http = new HttpClient(handler);
        var inner = new OpenAIClient(new ApiKeyCredential("offline-placeholder"), new OpenAIClientOptions
        { Endpoint = new Uri("https://offline.invalid/v1"), Transport = new HttpClientPipelineTransport(http) })
            .GetChatClient("qwen-offline").AsIChatClient();
        var options = Options(10, total);
        using var budget = new AgentTokenBudgetChatClient(inner, options);
        int toolCalls = 0;
        AITool lookup = AIFunctionFactory.Create(() => { toolCalls++; return "ok"; }, name: "lookup");
        var audit = new MemoryAudit();
        var runtime = new AgentRuntimeService(null!, null!, new ProducerEngine(options, new SdkModel(budget, lookup)),
            audit, new JsonSchemaValidator());
        var events = new List<AgentRunEvent>();
        await foreach (var value in runtime.StreamAsync(Context())) events.Add(value);
        var saved = Assert.Single(audit.Records);
        Assert.Equal(requests, handler.OutputCaps.Count);
        Assert.Equal(total.HasValue ? Math.Min(10, (int)total.Value) : 10, handler.OutputCaps[0]);
        if (requests == 2) Assert.Equal(total.HasValue ? 7 : 10, handler.OutputCaps[1]);
        Assert.Equal(total == 4 || !reportUsage ? 0 : 1, toolCalls);
        Assert.All(handler.Thinking, value => Assert.False(value));
        Assert.Equal(error is null ? AgentRunStatus.Completed : AgentRunStatus.Failed, saved.Status);
        Assert.Equal(error ?? "", saved.ErrorCode);
        Assert.Equal(reportUsage ? requests == 2 ? 11L : 5L : null, saved.ModelUsage!.TotalTokens);
        Assert.Equal(error is null ? AgentTokenUsageStatus.Reported : reportUsage ? AgentTokenUsageStatus.Partial : AgentTokenUsageStatus.Unknown, saved.ModelUsage.Status);
        Assert.False(audit.SaveToken.CanBeCanceled);
        Assert.DoesNotContain(events, value => value.Kind == AgentRunEventKind.ModelUsage);
        Assert.Contains(events, value => value.Kind == (error is null ? AgentRunEventKind.Completed : AgentRunEventKind.Failed));
        Assert.DoesNotContain(events, value => error is not null && value.Kind == AgentRunEventKind.Completed);
    }

    private static AgentRuntimeOptions Options(int? output = null, long? total = null) =>
        new(TimeSpan.FromSeconds(5), MaximumModelOutputTokens: output, MaximumRunTotalTokens: total);

    private static ChatMessage[] Messages() => [new(ChatRole.User, "offline query")];

    private static ChatResponseUpdate[] Reports(long? total, long? output, string finish = "stop") =>
    [
        new() { ResponseId = "same-vendor-id", Role = ChatRole.Assistant, Contents = [new TextContent("ok")] },
        new() { ResponseId = "same-vendor-id", Contents = [new UsageContent(new UsageDetails { TotalTokenCount = total, OutputTokenCount = output })], FinishReason = new ChatFinishReason(finish) }
    ];

    private static async Task ReadAsync(IChatClient client, ChatOptions? options = null, List<ChatResponseUpdate>? captured = null)
    {
        await foreach (var value in client.GetStreamingResponseAsync(Messages(), options)) captured?.Add(value);
    }

    private sealed class ScriptedClient(params ChatResponseUpdate[][] responses) : IChatClient
    {
        public int Requests;
        public List<ChatOptions?> Options { get; } = [];
        private ChatResponseUpdate[] Next(ChatOptions? options)
        {
            Options.Add(options);
            return responses[Requests++];
        }
        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var updates = Next(options);
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok"))
            { Usage = updates.SelectMany(value => value.Contents).OfType<UsageContent>().LastOrDefault()?.Details, FinishReason = updates.LastOrDefault()?.FinishReason };
        }
        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var updates = Next(options);
            foreach (var update in updates)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                yield return update;
            }
        }
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }

    private sealed class BudgetHandler(bool reportUsage) : HttpMessageHandler
    {
        public List<int> OutputCaps { get; } = [];
        public List<bool> Thinking { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var root = json.RootElement;
            Assert.True(root.GetProperty("stream_options").GetProperty("include_usage").GetBoolean());
            OutputCaps.Add(root.TryGetProperty("max_completion_tokens", out var cap) ? cap.GetInt32() : root.GetProperty("max_tokens").GetInt32());
            Thinking.Add(root.GetProperty("enable_thinking").GetBoolean());
            bool first = OutputCaps.Count == 1;
            string id = first ? "a" : "b";
            string delta = first
                ? """{"tool_calls":[{"index":0,"id":"lookup-1","type":"function","function":{"name":"lookup","arguments":"{}"}}]}"""
                : """{"content":"ok"}""";
            string usage = first ? """{"prompt_tokens":3,"completion_tokens":2,"total_tokens":5}""" : """{"prompt_tokens":4,"completion_tokens":2,"total_tokens":6}""";
            string data = $"data: {{\"id\":\"{id}\",\"object\":\"chat.completion.chunk\",\"created\":0,\"model\":\"qwen-offline\",\"choices\":[{{\"index\":0,\"delta\":{delta},\"finish_reason\":null}}]}}\n\n" +
                $"data: {{\"id\":\"{id}\",\"object\":\"chat.completion.chunk\",\"created\":0,\"model\":\"qwen-offline\",\"choices\":[{{\"index\":0,\"delta\":{{}},\"finish_reason\":\"{(first ? "tool_calls" : "stop")}\"}}]}}\n\n";
            if (reportUsage) data += $"data: {{\"id\":\"{id}\",\"object\":\"chat.completion.chunk\",\"created\":0,\"model\":\"qwen-offline\",\"choices\":[],\"usage\":{usage}}}\n\n";
            return new(HttpStatusCode.OK) { Content = new StringContent(data + "data: [DONE]\n\n", Encoding.UTF8, "text/event-stream") };
        }
    }

    private static AgentRunContext Context() => new(Guid.NewGuid(), Guid.NewGuid(),
        new AgentVersionSnapshot(Guid.NewGuid(), "offline", "instructions", "offline-profile", AgentOutputMode.Text, null, [], []),
        "hello", "hash", DateTimeOffset.UtcNow, []);

    private sealed class SdkModel(IChatClient client, AITool tool) : IMicrosoftAgentRuntimeModelClient
    {
        public async IAsyncEnumerable<MicrosoftAgentRuntimeModelUpdate> StreamAsync(AgentRunContext context, string apiKey,
            IReadOnlyList<AITool> tools, IReadOnlyList<ChatMessage> messages, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var agent = client.AsAIAgent(new ChatClientAgentOptions
            { ChatOptions = MicrosoftAgentRuntimeEngine.CreateChatOptions("qwen-offline", "instructions", [tool], false) });
            await foreach (var update in agent.RunStreamingAsync(Messages(), cancellationToken: cancellationToken))
                yield return MicrosoftAgentRuntimeEngine.ToModelUpdate(update);
        }
    }

    private sealed class ProducerEngine(AgentRuntimeOptions options, IMicrosoftAgentRuntimeModelClient model) : IAgentRuntimeEngine
    {
        public async IAsyncEnumerable<AgentRunEvent> StreamAsync(AgentRunContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var engine = new MicrosoftAgentRuntimeEngine(options, new UnusedResolver(), null!, NullLogger<MicrosoftAgentRuntimeEngine>.Instance);
            var channel = Channel.CreateUnbounded<AgentRunEvent>();
            var produce = (Task)typeof(MicrosoftAgentRuntimeEngine).GetMethod("ProduceAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(engine, [context, "offline-placeholder", Array.Empty<AITool>(), Messages(), channel.Writer, model, cancellationToken])!;
            await foreach (var value in channel.Reader.ReadAllAsync()) yield return value;
            await produce;
        }
    }

    private sealed class UnusedResolver : IAgentModelProfileResolver
    {
        public Task<AgentModelRuntimeProfile> ResolveAsync(string profileCode, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class MemoryAudit : IAgentRunAuditRepository
    {
        public List<AgentRunAuditRecord> Records { get; } = [];
        public CancellationToken SaveToken;
        public Task SaveAsync(AgentRunAuditRecord record, CancellationToken cancellationToken = default)
        { SaveToken = cancellationToken; Records.Add(record); return Task.CompletedTask; }
        public Task<IReadOnlyList<AgentRunAuditRecord>> ListAsync(Guid agentId, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AgentRunAuditRecord>>(Records);
    }
}
