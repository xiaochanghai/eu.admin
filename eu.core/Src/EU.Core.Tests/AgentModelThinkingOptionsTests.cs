#nullable enable
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Text;
using System.Text.Json;
using EU.Core.Agent.Runtime;
using Microsoft.Extensions.AI;
using OpenAI;
using Xunit;

namespace EU.Core.Tests;

public sealed class AgentModelThinkingOptionsTests
{
    [Theory]
    [InlineData("qwen3.8-max", false, false)]
    [InlineData("qwen3.8-max", true, true)]
    [InlineData("qwen-other", false, null)]
    [InlineData("other-model", false, null)]
    public async Task Thinking_override_is_top_level_boolean_for_exact_model_only(string model, bool enabled, bool? expected)
    {
        var options = new AgentRuntimeOptions(new Uri("https://offline.invalid/v1"), "alias:offline", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5))
        { QwenThinkingByModel = new Dictionary<string, bool> { ["qwen3.8-max"] = enabled } };
        using var handler = new CaptureHandler();
        using var http = new HttpClient(handler);
        using var client = new OpenAIClient(new ApiKeyCredential("offline-placeholder"), new OpenAIClientOptions
        { Endpoint = options.ModelEndpoint, Transport = new HttpClientPipelineTransport(http) }).GetChatClient(model).AsIChatClient();
        var factory = typeof(MicrosoftAgentRuntimeEngine).GetMethod("CreateChatOptions", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var chat = (ChatOptions)factory.Invoke(null, [model, "Preserve instructions", Array.Empty<AITool>(), options])!;
        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "query")], chat);
        using var json = JsonDocument.Parse(handler.Body!);
        Assert.Equal(model, json.RootElement.GetProperty("model").GetString());
        Assert.False(json.RootElement.TryGetProperty("extra_body", out _));
        if (expected is bool value) Assert.Equal(value, json.RootElement.GetProperty("enable_thinking").GetBoolean());
        else Assert.False(json.RootElement.TryGetProperty("enable_thinking", out _));
        Assert.Equal("Preserve instructions", chat.Instructions);
        Assert.False(chat.AllowMultipleToolCalls);
        if (chat.RawRepresentationFactory is not null)
            Assert.NotSame(chat.RawRepresentationFactory(client), chat.RawRepresentationFactory(client));
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string? Body { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"offline","object":"chat.completion","created":0,"model":"offline","choices":[{"index":0,"message":{"role":"assistant","content":"ok"},"finish_reason":"stop"}]}""", Encoding.UTF8, "application/json")
            };
        }
    }
}
