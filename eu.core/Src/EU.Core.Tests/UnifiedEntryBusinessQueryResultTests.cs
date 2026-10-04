#nullable enable
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using EU.Core.Api.Agent.Observability;
using EU.Core.IServices;
using EU.Core.IServices.MainAgent;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.Services;
using Xunit;

namespace EU.Core.Tests;

/// <summary>使用内存存储和替身运行时验证统一入口，不连接数据库或模型服务。</summary>
public sealed class UnifiedEntryBusinessQueryResultTests
{
    [Theory]
    [InlineData(80, true)]
    [InlineData(null, false)]
    public async Task Unified_entry_shares_host_monitor_and_warning_setting_with_delegated_budget(int? percent, bool warning)
    {
        using var metrics = new AgentMetrics();
        var fixture = new Fixture("completed", delegated: true,
            entryLimits: new UnifiedEntryLimits(MaxModelTotalTokens: 100, ModelTokenBudgetWarningPercent: percent), telemetry: metrics)
        { ModelSpend = 40 };
        AssertResult(fixture, await fixture.RunAsync());
        Assert.Equal(2, fixture.StreamedContexts.Count);
        string rendered = metrics.RenderPrometheus();
        Assert.Equal(warning, rendered.Contains("agent_runtime_token_budget_signals_total{scope=\"ExecutionTree\",signal=\"Warning\"} 1", StringComparison.Ordinal));
        Assert.DoesNotContain("signal=\"Exhausted\"", rendered);
        Assert.DoesNotContain("signal=\"UsageUnavailable\"", rendered);
    }

    [Fact]
    public async Task Unified_entry_passes_same_budget_to_main_and_delegated_agent()
    {
        var fixture = new Fixture("completed", delegated: true, entryLimits: new UnifiedEntryLimits(MaxModelTotalTokens: 100));
        var events = await fixture.RunAsync();
        AssertResult(fixture, events);
        Assert.Equal(2, fixture.StreamedContexts.Count);
        var budget = fixture.StreamedContexts[0].ModelTokenBudget;
        Assert.NotNull(budget);
        Assert.Same(budget, fixture.StreamedContexts[1].ModelTokenBudget);
        var independent = new Fixture("completed", entryLimits: new UnifiedEntryLimits(MaxModelTotalTokens: 100));
        await independent.RunAsync();
        Assert.NotSame(budget, Assert.Single(independent.StreamedContexts).ModelTokenBudget);
    }

    [Theory]
    [InlineData("completed", UnifiedRunStatus.Completed)]
    [InlineData("failed", UnifiedRunStatus.Failed)]
    [InlineData("token-budget", UnifiedRunStatus.Failed)]
    [InlineData("cancelled", UnifiedRunStatus.Cancelled)]
    [InlineData("throw-cancel", UnifiedRunStatus.Cancelled)]
    [InlineData("throw-failure", UnifiedRunStatus.Failed)]
    [InlineData("empty", UnifiedRunStatus.Completed)]
    [InlineData("approval", UnifiedRunStatus.WaitingForApproval)]
    public async Task Successful_query_is_saved_once_regardless_of_model_terminal(string ending, UnifiedRunStatus expected)
    {
        var fixture = new Fixture(ending);
        var events = await fixture.RunAsync();

        Assert.Equal(expected, fixture.Saved!.Details.EntryRun.Status);
        AssertResult(fixture, events);
        if (ending == "token-budget") Assert.Equal(AgentRunErrorCodes.ModelTokenBudgetExceeded, fixture.Saved.Details.EntryRun.ErrorCode);
        Assert.DoesNotContain(fixture.Saved.Messages, message => message.Kind == ConversationMessageKind.AssistantNarrative
            && !string.IsNullOrEmpty(message.Content));
        if (ending == "empty")
        {
            using var content = JsonDocument.Parse(Assert.Single(fixture.Saved.Messages,
                message => message.Kind == ConversationMessageKind.BusinessQueryResult).Content);
            Assert.Empty(content.RootElement.GetProperty("presentation").GetProperty("rows").EnumerateArray());
        }
    }

    [Theory]
    [InlineData(false, "wait")]
    [InlineData(true, "wait")]
    [InlineData(true, "audit-wait")]
    public async Task Query_is_durable_and_visible_before_model_finishes_and_survives_client_disconnect(bool delegated, string ending)
    {
        var fixture = new Fixture(ending, delegated: delegated);
        var prepared = await fixture.Service.PrepareAsync("sales query", null);
        Assert.True(prepared.Succeeded);
        using var cancellation = new CancellationTokenSource();
        var events = new List<UnifiedRunEvent>();
        await using (var stream = fixture.Service.StreamAsync(prepared.Context!, cancellation.Token).GetAsyncEnumerator())
        {
            while (true)
            {
                Task<bool> moveNext = stream.MoveNextAsync().AsTask();
                bool hasNext;
                try { hasNext = await moveNext.WaitAsync(TimeSpan.FromSeconds(10)); }
                catch (TimeoutException)
                {
                    cancellation.Cancel();
                    try { await moveNext; }
                    catch (OperationCanceledException) { }
                    throw;
                }
                if (!hasNext) break;
                events.Add(stream.Current);
                if (stream.Current.Kind != "business-query-result") continue;
                Assert.Equal(UnifiedRunStatus.Running, fixture.Saved!.Details.EntryRun.Status);
                AssertResult(fixture, events);
                if (delegated)
                {
                    var childSuccess = Assert.Single(events, value => value.Kind == "tool-succeeded" && value.Depth == 1);
                    Assert.True(childSuccess.Sequence < stream.Current.Sequence);
                }
                cancellation.Cancel();
                break;
            }
        }

        Assert.Equal(UnifiedRunStatus.Cancelled, fixture.Saved!.Details.EntryRun.Status);
        AssertResult(fixture, events);
    }

    [Fact]
    public async Task Cancellation_at_success_receipt_does_not_cancel_result_persistence()
    {
        using var cancellation = new CancellationTokenSource();
        var fixture = new Fixture("wait") { OnQuerySucceeded = cancellation.Cancel };
        var prepared = await fixture.Service.PrepareAsync("sales query", null);
        Assert.True(prepared.Succeeded);
        try
        {
            await foreach (var value in fixture.Service.StreamAsync(prepared.Context!, cancellation.Token)) { }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        Assert.Equal(UnifiedRunStatus.Cancelled, fixture.Saved!.Details.EntryRun.Status);
        Assert.Single(fixture.Saved.Messages, value => value.Kind == ConversationMessageKind.BusinessQueryResult);
        Assert.Single(fixture.Saved.Events, value => value.Kind == "business-query-result");
    }

    [Theory]
    [InlineData('x', 150_000)]
    [InlineData('x', 1_000_000)]
    [InlineData('销', 70_000)]
    public async Task Valid_large_results_use_result_budget_including_json_event_escaping(char character, int count)
    {
        var fixture = new Fixture("completed", new string(character, count));
        Assert.True(Encoding.UTF8.GetByteCount(fixture.Payload) > 131_072);
        var events = await fixture.RunAsync();

        Assert.Equal(UnifiedRunStatus.Completed, fixture.Saved!.Details.EntryRun.Status);
        AssertResult(fixture, events);
        var tool = Assert.Single(fixture.Saved.Details.ToolCalls);
        Assert.Equal(fixture.Payload, tool.ResultContent);
        Assert.Contains(events, value => value.Kind == "tool-succeeded"
            && Encoding.UTF8.GetByteCount(value.PayloadJson) > 131_072);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Result_and_conversation_limits_still_reject_oversized_retention(bool conversationLimit)
    {
        var limits = conversationLimit
            ? new BusinessQueryResultLimits(1_048_576, 100)
            : new BusinessQueryResultLimits(100, 10_485_760);
        var fixture = new Fixture("completed", resultLimits: limits);
        var events = await fixture.RunAsync();

        Assert.Equal(UnifiedRunStatus.Failed, fixture.Saved!.Details.EntryRun.Status);
        Assert.Equal(UnifiedEntryErrorCodes.BusinessQueryResultLimitExceeded, fixture.Saved.Details.EntryRun.ErrorCode);
        Assert.DoesNotContain(fixture.Saved.Messages, message => message.Kind == ConversationMessageKind.BusinessQueryResult);
        Assert.DoesNotContain(events, value => value.Kind == "business-query-result");
    }

    [Fact]
    public async Task Exact_retention_budget_accepts_result_without_counting_it_again_at_finalization()
    {
        var probe = new Fixture("completed");
        using var payload = JsonDocument.Parse(probe.Payload);
        // 结果字节数不依赖固定长度 GUID 的实际值。
        int resultBytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(new
        {
            kind = "business-query-result", queryId = probe.QueryId,
            receipt = payload.RootElement.GetProperty("receipt"),
            presentation = payload.RootElement.GetProperty("presentation"), integritySha256 = new string('a', 64)
        }));
        var fixture = new Fixture("completed", resultLimits: new BusinessQueryResultLimits(resultBytes, resultBytes));
        var events = await fixture.RunAsync();
        Assert.Equal(UnifiedRunStatus.Completed, fixture.Saved!.Details.EntryRun.Status);
        AssertResult(fixture, events);
        Assert.Equal(resultBytes, Assert.Single(fixture.Saved.Messages,
            value => value.Kind == ConversationMessageKind.BusinessQueryResult).ContentUtf8Bytes);
    }

    [Fact]
    public async Task Ordinary_model_text_keeps_original_payload_limit()
    {
        var fixture = new Fixture("large-delta");
        await fixture.RunAsync();
        Assert.Equal(UnifiedRunStatus.Failed, fixture.Saved!.Details.EntryRun.Status);
        Assert.Equal(UnifiedEntryErrorCodes.PayloadLimitExceeded, fixture.Saved.Details.EntryRun.ErrorCode);
    }

    [Fact]
    public async Task Persistence_failure_is_not_reported_as_completed_and_result_can_be_recovered()
    {
        var fixture = new Fixture("completed") { FailResultSaveOnce = true };
        var events = await fixture.RunAsync();
        Assert.Equal(UnifiedRunStatus.Failed, fixture.Saved!.Details.EntryRun.Status);
        Assert.DoesNotContain(events, value => value.Kind == "completed");
        AssertResult(fixture, events);
    }

    [Theory]
    [InlineData("completed", UnifiedRunStatus.Completed)]
    [InlineData("failed", UnifiedRunStatus.Failed)]
    [InlineData("cancelled", UnifiedRunStatus.Cancelled)]
    [InlineData("throw-cancel", UnifiedRunStatus.Cancelled)]
    [InlineData("throw-failure", UnifiedRunStatus.Failed)]
    [InlineData("empty", UnifiedRunStatus.Completed)]
    [InlineData("token-budget", UnifiedRunStatus.Failed)]
    public async Task Delegated_success_is_saved_once_even_when_child_does_not_complete(string ending, UnifiedRunStatus expected)
    {
        var fixture = new Fixture(ending, delegated: true);
        var events = await fixture.RunAsync();
        // 子运行取消但入口请求未取消时，沿用现有致命工具错误的 Failed 终态。
        Assert.Equal(expected == UnifiedRunStatus.Cancelled ? UnifiedRunStatus.Failed : expected,
            fixture.Saved!.Details.EntryRun.Status);
        AssertResult(fixture, events);
        Assert.Equal(expected, Assert.Single(fixture.Saved.Details.AgentRuns,
            value => value.Kind == UnifiedAgentRunKind.Child).Status);
    }

    [Fact]
    public async Task Delegated_terminal_audit_failure_preserves_result_without_reporting_success()
    {
        var fixture = new Fixture("audit-failure", delegated: true);
        var events = await fixture.RunAsync();
        Assert.Equal(UnifiedRunStatus.Failed, fixture.Saved!.Details.EntryRun.Status);
        Assert.Equal(UnifiedRunStatus.Failed, Assert.Single(fixture.Saved.Details.AgentRuns,
            value => value.Kind == UnifiedAgentRunKind.Child).Status);
        Assert.DoesNotContain(events, value => value.Kind is "completed" or "child-agent-completed");
        AssertResult(fixture, events);
    }

    [Fact]
    public async Task Delegated_result_save_failure_while_child_waits_cancels_child_and_preserves_result()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var fixture = new Fixture("wait", delegated: true) { FailResultSaveOnce = true };
        var events = await fixture.RunAsync(cancellation.Token);
        Assert.Equal(UnifiedRunStatus.Failed, fixture.Saved!.Details.EntryRun.Status);
        Assert.DoesNotContain(events, value => value.Kind == "completed");
        AssertResult(fixture, events);
    }

    [Fact]
    public async Task Result_notifications_are_buffered_coalesced_and_reusable_after_cancelled_wait()
    {
        using var scope = new UnifiedEntryExecutionScope();
        Parallel.For(0, 100, _ => NotifyResultReady(scope));
        Assert.True(await WaitForResultReady(scope, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10)));
        using var cancellation = new CancellationTokenSource();
        Task<bool> wait = WaitForResultReady(scope, cancellation.Token);
        Assert.False(wait.IsCompleted);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
        NotifyResultReady(scope);
        Assert.True(await WaitForResultReady(scope, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public async Task Scope_disposal_releases_pending_notification_wait_and_ignores_late_notifications()
    {
        using var scope = new UnifiedEntryExecutionScope();
        Task<bool> wait = WaitForResultReady(scope, CancellationToken.None);
        Assert.False(wait.IsCompleted);
        scope.Dispose();
        await Assert.ThrowsAsync<ChannelClosedException>(() => wait);
        NotifyResultReady(scope);
    }

    [Theory]
    [InlineData('x', 50_000)]
    [InlineData('x', 1_000_000)]
    [InlineData('销', 70_000)]
    public async Task Delegated_large_results_use_mcp_budget_not_internal_payload_limit(char character, int count)
    {
        var fixture = new Fixture("completed", new string(character, count), delegated: true);
        var events = await fixture.RunAsync();
        Assert.Equal(UnifiedRunStatus.Completed, fixture.Saved!.Details.EntryRun.Status);
        AssertResult(fixture, events);
        var toolEvent = Assert.Single(events, value => value.Kind == "tool-succeeded" && value.Depth == 1);
        using var content = JsonDocument.Parse(toolEvent.PayloadJson);
        Assert.Equal(fixture.Payload, content.RootElement.GetProperty("text").GetString());
        Assert.True(Encoding.UTF8.GetByteCount(toolEvent.PayloadJson) > 32_768);
    }

    [Fact]
    public async Task Delegated_cancellation_at_query_receipt_keeps_result_and_tool_event()
    {
        using var cancellation = new CancellationTokenSource();
        var fixture = new Fixture("wait", delegated: true) { OnQuerySucceeded = cancellation.Cancel };
        var prepared = await fixture.Service.PrepareAsync("sales query", null);
        Assert.True(prepared.Succeeded);
        try
        {
            await foreach (var value in fixture.Service.StreamAsync(prepared.Context!, cancellation.Token)) { }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        Assert.Equal(UnifiedRunStatus.Cancelled, fixture.Saved!.Details.EntryRun.Status);
        Assert.Single(fixture.Saved.Messages, value => value.Kind == ConversationMessageKind.BusinessQueryResult);
        Assert.Single(fixture.Saved.Events, value => value.Kind == "business-query-result");
        Assert.Single(fixture.Saved.Events, value => value.Kind == "tool-succeeded" && value.Depth == 1);
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("missing-start")]
    [InlineData("wrong-call")]
    [InlineData("wrong-run")]
    [InlineData("wrong-tool-name")]
    [InlineData("invalid-start")]
    [InlineData("repeat-start")]
    public async Task Delegated_invalid_evidence_is_not_saved_as_query_result(string scenario)
    {
        var fixture = new Fixture(scenario, delegated: true);
        var events = await fixture.RunAsync();
        Assert.Equal(UnifiedRunStatus.Failed, fixture.Saved!.Details.EntryRun.Status);
        Assert.DoesNotContain(fixture.Saved.Messages, value => value.Kind == ConversationMessageKind.BusinessQueryResult);
        Assert.DoesNotContain(events, value => value.Kind == "business-query-result");
    }

    [Fact]
    public async Task Ordinary_delegated_model_text_keeps_internal_payload_limit()
    {
        var fixture = new Fixture("large-delta", delegated: true);
        await fixture.RunAsync();
        Assert.Equal(UnifiedRunStatus.Failed, fixture.Saved!.Details.EntryRun.Status);
        Assert.Equal(UnifiedEntryErrorCodes.PayloadLimitExceeded, fixture.Saved.Details.EntryRun.ErrorCode);
    }

    private static void AssertResult(Fixture fixture, IReadOnlyList<UnifiedRunEvent> events)
    {
        var message = Assert.Single(fixture.Saved!.Messages,
            value => value.Kind == ConversationMessageKind.BusinessQueryResult);
        var resultEvent = Assert.Single(events, value => value.Kind == "business-query-result");
        Assert.Single(fixture.Saved.Events, value => value.Kind == "business-query-result");
        Assert.Equal(message.Content, resultEvent.PayloadJson);
        Assert.Equal(fixture.QueryId, message.BusinessQueryId);
        using var persisted = JsonDocument.Parse(message.Content);
        Assert.Equal(message.BusinessQueryReceiptJson, persisted.RootElement.GetProperty("receipt").GetRawText());
        Assert.Equal(message.BusinessQueryPresentationJson, persisted.RootElement.GetProperty("presentation").GetRawText());
        Assert.Equal(message.BusinessQueryIntegritySha256, persisted.RootElement.GetProperty("integritySha256").GetString());
        Assert.Equal(fixture.Saved.Conversation.Id, resultEvent.ConversationId);
        Assert.Equal(fixture.Saved.Details.EntryRun.Id, resultEvent.RunId);
    }

    // 内部通知属于 Services 边界；测试不扩大契约的可见性。
    private static void NotifyResultReady(UnifiedEntryExecutionScope scope) =>
        typeof(UnifiedEntryExecutionScope).GetMethod("NotifyBusinessQueryResultReady", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(scope, null);

    private static Task<bool> WaitForResultReady(UnifiedEntryExecutionScope scope, CancellationToken cancellationToken) =>
        ((ValueTask<bool>)typeof(UnifiedEntryExecutionScope)
            .GetMethod("WaitForBusinessQueryResultAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(scope, [cancellationToken])!).AsTask();

    private sealed class Fixture
    {
        public UnifiedEntryService Service { get; }
        public UnifiedEntryAggregate? Saved { get; private set; }
        public Guid QueryId { get; } = Guid.NewGuid();
        public string Payload { get; }
        public List<AgentRunContext> StreamedContexts { get; } = [];
        public bool FailResultSaveOnce { get; init; }
        public Action? OnQuerySucceeded { get; init; }
        public long? ModelSpend { get; init; }
        private bool _saveFailed;
        private readonly string _ending;
        private readonly PublishedMcpToolReference _tool;
        private readonly bool _delegated;
        private readonly Guid _childAgentId = Guid.NewGuid();
        private readonly Guid _childVersionId = Guid.NewGuid();

        public Fixture(string ending, string displayValue = "0", BusinessQueryResultLimits? resultLimits = null, bool delegated = false, UnifiedEntryLimits? entryLimits = null, IAgentRuntimeTelemetry? telemetry = null)
        {
            _ending = ending;
            _delegated = delegated;
            _tool = new PublishedMcpToolReference(Guid.NewGuid(), "business-query", "Business Query", Guid.NewGuid(),
                "query_business_data", "test", "{}", McpToolRisk.ReadOnly, new string('b', 64));
            var policy = new BusinessQueryToolPolicy("business-query", "query_business_data", new Uri("https://localhost"),
                "eu-agent", "eu-mcp", "alias:project-jwt", 1, new string('a', 64), new string('b', 64), TimeSpan.FromSeconds(45), false);
            var snapshot = new AgentVersionSnapshot(Guid.NewGuid(), "test", "test", "test", AgentOutputMode.Text, null, [], []);
            if (delegated) snapshot = snapshot with
            {
                ChildAgents = [new AgentChildBindingSnapshot(_childAgentId, _childVersionId) { AgentCode = "business-query" }]
            };
            var context = new AgentRunContext(Guid.NewGuid(), Guid.NewGuid(), snapshot, "query", "", DateTimeOffset.UtcNow, delegated ? [] : [_tool]);
            var childContext = new AgentRunContext(Guid.NewGuid(), _childAgentId,
                snapshot with { VersionId = _childVersionId, ChildAgents = [] }, "query", "", DateTimeOffset.UtcNow, ending == "large-delta" ? [] : [_tool]);
            Payload = JsonSerializer.Serialize(new
            {
                succeeded = true,
                result = new { resultSha256 = new string('c', 64) },
                presentation = new { title = "sales", formatterVersion = "1.0", rows = ending == "empty"
                    ? Array.Empty<object>() : new object[] { new { total = new { displayValue } } } },
                receipt = new
                {
                    queryId = QueryId, catalogRevision = 1, catalogHash = new string('a', 64),
                    toolSchemaHash = new string('b', 64), queryPlanHash = new string('c', 64), policyDecisionId = Guid.NewGuid(),
                    rowCount = ending == "empty" ? 0 : 1, truncated = false, terminalStatus = "succeeded", resultHash = new string('c', 64)
                }
            });
            var assignments = CreateProxy<IMainAgentAssignmentService>((method, _) => method.Name == "GetAsync"
                ? Task.FromResult(ServiceResult<MainAgentAssignment>.QuerySuccess(new MainAgentAssignment(context.AgentId, snapshot.VersionId, 1, DateTimeOffset.UtcNow)))
                : throw new NotSupportedException(method.Name));
            var runtime = CreateProxy<IAgentRuntimeService>((method, args) => method.Name switch
            {
                "PrepareVersionAsync" => Task.FromResult(AgentRunPreparationResult.Success(
                    delegated && (Guid)args![0]! == _childAgentId ? childContext : context)),
                "StreamAsync" => Stream((AgentRunContext)args![0]!, (CancellationToken)args[1]!),
                "TerminatePreparedRunAsync" => Task.CompletedTask,
                _ => throw new NotSupportedException(method.Name)
            });
            var repository = CreateProxy<IUnifiedEntryRepository>((method, args) =>
            {
                if (method.Name != "SaveAsync") throw new NotSupportedException(method.Name);
                ((CancellationToken)args![1]!).ThrowIfCancellationRequested();
                var aggregate = (UnifiedEntryAggregate)args[0]!;
                if (FailResultSaveOnce && !_saveFailed && aggregate.Messages.Any(value => value.Kind == ConversationMessageKind.BusinessQueryResult))
                {
                    _saveFailed = true;
                    throw new IOException("Test persistence failure");
                }
                Saved = aggregate.WithPersistenceRevision(aggregate.PersistenceRevision + 1);
                return Task.FromResult(Saved);
            });
            Service = new UnifiedEntryService(assignments, runtime, new OrchestrationRuntimeService(null!, null!, null!, runtime),
                repository, limits: entryLimits, businessQueryPolicy: new BusinessQueryToolPolicyAccessor(policy), businessQueryResultLimits: resultLimits, telemetry: telemetry);
        }

        public async Task<List<UnifiedRunEvent>> RunAsync(CancellationToken cancellationToken = default)
        {
            var prepared = await Service.PrepareAsync("sales query", null);
            Assert.True(prepared.Succeeded, prepared.Error?.Message);
            var events = new List<UnifiedRunEvent>();
            await foreach (var value in Service.StreamAsync(prepared.Context!, cancellationToken)) events.Add(value);
            return events;
        }

        private async IAsyncEnumerable<AgentRunEvent> Stream(AgentRunContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            StreamedContexts.Add(context);
            if (ModelSpend is long spend)
            {
                // 模拟一次完整供应商响应，在工具委派前释放预算门。
                using var lease = await context.ModelTokenBudget!.ReserveAsync(cancellationToken);
                lease.MarkStarted();
                lease.Complete(spend, false);
            }
            if (_delegated && context.AgentId != _childAgentId)
            {
                var tool = Assert.Single(context.InternalTools, value => value.Name == "delegate_to_agent");
                var startedDelegate = new AgentRunEvent(context.RunId, 1, AgentRunEventKind.ToolStarted,
                    DateTimeOffset.UtcNow, ToolName: tool.Name, ToolCallId: Guid.NewGuid());
                yield return startedDelegate;
                var result = await tool.InvokeAsync(JsonSerializer.Serialize(new
                {
                    agentVersionId = _childVersionId, task = "sales query", reason = "test delegation"
                }), cancellationToken);
                yield return startedDelegate with
                {
                    Sequence = 2, Kind = result.Succeeded ? AgentRunEventKind.ToolSucceeded : AgentRunEventKind.ToolFailed,
                    Text = result.Content, ErrorCode = result.ErrorCode
                };
                yield return new AgentRunEvent(context.RunId, 3,
                    result.Succeeded ? AgentRunEventKind.Completed
                        : result.ErrorCode == UnifiedEntryErrorCodes.Cancelled ? AgentRunEventKind.Cancelled : AgentRunEventKind.Failed,
                    DateTimeOffset.UtcNow, ErrorCode: result.ErrorCode);
                yield break;
            }
            if (_ending == "large-delta")
            {
                yield return new AgentRunEvent(context.RunId, 1, AgentRunEventKind.Delta, DateTimeOffset.UtcNow, new string('x', 140_000));
                yield break;
            }
            var started = new AgentRunEvent(context.RunId, 1, AgentRunEventKind.ToolStarted, DateTimeOffset.UtcNow,
                ToolVersionId: _tool.ToolVersionId, ToolName: _tool.ToolName, ToolCallId: Guid.NewGuid());
            if (_ending != "missing-start")
                yield return _ending == "invalid-start" ? started with { ToolName = "unexpected-tool" } : started;
            if (_ending == "repeat-start") yield return started with { Sequence = 2, ToolCallId = Guid.NewGuid() };
            OnQuerySucceeded?.Invoke();
            yield return started with
            {
                Sequence = 2, Kind = AgentRunEventKind.ToolSucceeded, Text = _ending == "malformed" ? "{}" : Payload,
                ToolCallId = _ending == "wrong-call" ? Guid.NewGuid() : started.ToolCallId,
                RunId = _ending == "wrong-run" ? Guid.NewGuid() : started.RunId,
                ToolName = _ending == "wrong-tool-name" ? "unexpected-tool" : started.ToolName
            };
            if (_ending == "approval")
            {
                yield return started with
                {
                    Sequence = 3, Kind = AgentRunEventKind.ApprovalRequired,
                    ToolCallId = Guid.NewGuid(), ApprovalId = Guid.NewGuid()
                };
                yield break;
            }
            if (_ending == "wait") await Task.Delay(Timeout.Infinite, cancellationToken);
            if (_ending == "throw-cancel") throw new OperationCanceledException();
            if (_ending == "throw-failure") throw new AgentRuntimeException("TEST_MODEL_FAILED", "Test model failure");
            if (_ending == "audit-failure") throw new IOException("Test child terminal audit persistence failure");
            yield return new AgentRunEvent(context.RunId, 3, _ending switch
            {
                "failed" or "token-budget" => AgentRunEventKind.Failed,
                "cancelled" => AgentRunEventKind.Cancelled,
                _ => AgentRunEventKind.Completed
            }, DateTimeOffset.UtcNow, ErrorCode: _ending switch
            {
                "failed" => "TEST_MODEL_FAILED",
                "token-budget" => AgentRunErrorCodes.ModelTokenBudgetExceeded,
                _ => ""
            });
            // 模拟 Completed 事件已产生，但子运行的终态审计尚未返回。
            if (_ending == "audit-wait") await Task.Delay(Timeout.Infinite, cancellationToken);
        }
    }

    private static T CreateProxy<T>(Func<MethodInfo, object?[]?, object?> invoke) where T : class
    {
        T proxy = DispatchProxy.Create<T, TestProxy>();
        ((TestProxy)(object)proxy).Handler = invoke;
        return proxy;
    }

    public class TestProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!, args);
    }
}
