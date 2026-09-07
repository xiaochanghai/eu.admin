#nullable enable
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using EU.Core.Agent.Runtime;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EU.Core.Tests;

/// <summary>使用真实 SDK 工具循环与离线模型／MCP 替身，不调用外部服务。</summary>
public sealed class BusinessQuerySingleShotRuntimeTests
{
    [Theory]
    [InlineData(true, true, 1)]
    [InlineData(false, true, 2)]
    [InlineData(true, false, 2)]
    public async Task Only_successful_single_shot_tools_stop_the_sdk_loop(bool singleShot, bool succeeded, int expectedModelCalls)
    {
        var invoker = new Invoker(succeeded);
        var (function, channel) = BuildFunction(invoker, singleShot);
        var model = new Model(function.Name);
        using var client = new FunctionInvokingChatClient(model);
        await foreach (var update in client.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "query")], new ChatOptions { Tools = [function], AllowMultipleToolCalls = false }))
        {
            _ = update;
        }
        Assert.Equal(expectedModelCalls, model.Calls);
        Assert.Equal(1, invoker.Calls);
        var events = new List<AgentRunEvent>();
        while (channel.Reader.TryRead(out var value)) events.Add(value);
        Assert.Single(events, value => value.Kind == AgentRunEventKind.ToolStarted);
        Assert.Single(events, value => value.Kind == (succeeded ? AgentRunEventKind.ToolSucceeded : AgentRunEventKind.ToolFailed));
        Assert.DoesNotContain(events, value => value.Kind == AgentRunEventKind.ToolBlocked);
    }

    [Fact]
    public async Task Single_shot_does_not_remove_the_hard_call_limit()
    {
        var invoker = new Invoker(true);
        var (function, _) = BuildFunction(invoker, true);
        await function.InvokeAsync(new AIFunctionArguments());
        var exception = await Assert.ThrowsAsync<AgentRuntimeException>(async () =>
            await function.InvokeAsync(new AIFunctionArguments()));
        Assert.Equal(UnifiedEntryErrorCodes.BusinessQueryCallLimitExceeded, exception.ErrorCode);
        Assert.Equal(1, invoker.Calls);
    }

    private static (AIFunction Function, Channel<AgentRunEvent> Events) BuildFunction(Invoker invoker, bool singleShot)
    {
        var tool = new PublishedMcpToolReference(Guid.NewGuid(), "business-query", "Business Query", Guid.NewGuid(),
            "query_business_data", "test", "{\"type\":\"object\",\"properties\":{}}", McpToolRisk.ReadOnly, new string('b', 64));
        var policy = new BusinessQueryToolPolicy("business-query", "query_business_data", new Uri("https://localhost"),
            "eu-agent", "eu-mcp", "alias:project-jwt", 1, new string('a', 64), new string('b', 64), TimeSpan.FromSeconds(45), false);
        var limit = Assert.Single(BusinessQueryMcpToolCallLimits.Create(policy, [tool]));
        Assert.True(limit.CompleteAfterSuccess);
        var context = new AgentRunContext(Guid.NewGuid(), Guid.NewGuid(), null!, "test", "", DateTimeOffset.UtcNow, [tool])
        {
            McpToolCallLimits = [limit with { CompleteAfterSuccess = singleShot }]
        };
        var engine = new MicrosoftAgentRuntimeEngine(new AgentRuntimeOptions(new Uri("https://localhost"), "alias:offline",
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5)), null!, invoker, NullLogger<MicrosoftAgentRuntimeEngine>.Instance);
        var channel = Channel.CreateUnbounded<AgentRunEvent>();
        var build = typeof(MicrosoftAgentRuntimeEngine).GetMethod("BuildTools", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var tools = (IReadOnlyList<AITool>)build.Invoke(engine, [context, channel.Writer])!;
        return (Assert.IsAssignableFrom<AIFunction>(Assert.Single(tools)), channel);
    }

    private sealed class Invoker(bool succeeded) : IMcpRuntimeToolInvoker
    {
        public int Calls { get; private set; }
        public Task<McpRuntimeToolResult> InvokeAsync(Guid toolVersionId, McpToolRisk expectedRisk,
            IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new McpRuntimeToolResult(succeeded, false,
                "{\"succeeded\":true,\"amount\":null}", succeeded ? "" : "MCP_TOOL_CALL_FAILED"));
        }
    }

    private sealed class Model(string functionName) : IChatClient
    {
        public int Calls { get; private set; }
        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            await Task.CompletedTask;
            yield return Calls == 1
                ? new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = [new FunctionCallContent("call-1", functionName, new Dictionary<string, object?>())] }
                : new ChatResponseUpdate(ChatRole.Assistant, "done");
        }
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }
}
