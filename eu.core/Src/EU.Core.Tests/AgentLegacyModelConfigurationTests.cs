#nullable enable
using EU.Core.Agent.Runtime;
using EU.Core.Api.Agent.Configuration;
using EU.Core.IServices;
using EU.Core.IServices.Runtime;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EU.Core.Tests;

/// <summary>模型旧配置清理回归；仅使用独立临时文件和替身，不访问模型或数据库。</summary>
public sealed class AgentLegacyModelConfigurationTests
{
    [Fact]
    public void Dotenv_ignores_old_model_keys_but_keeps_other_host_settings()
    {
        string root = Path.Combine(Path.GetTempPath(), "agent-config-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, ".env"), """
                AgentPlatform__ServiceName=offline-agent
                AgentPlatform__ModelEndpoint=https://old.example.test
                AgentPlatform__ModelCredentialAlias=alias:old
                AGENT_MODEL_DEFAULT_ID=old-model
                AgentControl__ModelProfileIds__0=old-model
                AgentExecution__ModelTimeoutSeconds=1
                AgentMcp__AllowedHosts__0=mcp.example.test
                AgentExecution__ToolCallTimeoutSeconds=45
                """);
            using var configuration = new ConfigurationManager();
            configuration["AgentPlatform:LoadDotEnv"] = "true";
            LocalDotEnvConfiguration.Apply(configuration, root, root);
            Assert.Equal("offline-agent", configuration["AgentPlatform:ServiceName"]);
            Assert.Equal("mcp.example.test", configuration["AgentMcp:AllowedHosts:0"]);
            Assert.Equal("45", configuration["AgentExecution:ToolCallTimeoutSeconds"]);
            Assert.Null(configuration["AgentPlatform:ModelEndpoint"]);
            Assert.Null(configuration["AgentPlatform:ModelCredentialAlias"]);
            Assert.Empty(configuration.GetSection("AgentControl").GetChildren());
            Assert.Null(configuration["AgentExecution:ModelTimeoutSeconds"]);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Empty_credentials_and_cancellation_stop_before_model_invocation(bool cancelled)
    {
        var resolver = new Resolver();
        var engine = new MicrosoftAgentRuntimeEngine(new AgentRuntimeOptions(TimeSpan.FromSeconds(5)), resolver, null!, NullLogger<MicrosoftAgentRuntimeEngine>.Instance);
        var snapshot = new AgentVersionSnapshot(Guid.NewGuid(), "agent", "instructions", "profile", default, null, [], []);
        var context = new AgentRunContext(Guid.NewGuid(), Guid.NewGuid(), snapshot, "hello", "", DateTimeOffset.UtcNow, []);
        var token = new CancellationToken(cancelled);
        async Task Invoke()
        {
            await foreach (var item in engine.StreamAsync(context, token)) Assert.Fail("No model output expected.");
        }
        var judge = new MicrosoftExtensionsModelJudgeEngine(resolver);
        if (cancelled)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(Invoke);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => judge.EvaluateAsync("input", "output", "profile", [], token));
        }
        else
        {
            var error = await Assert.ThrowsAsync<AgentRuntimeException>(Invoke);
            Assert.Equal(AgentRunErrorCodes.ModelCredentialMissing, error.ErrorCode);
            await Assert.ThrowsAsync<InvalidOperationException>(() => judge.EvaluateAsync("input", "output", "profile", []));
        }
    }

    private sealed class Resolver : IAgentModelProfileResolver
    {
        public Task<AgentModelRuntimeProfile> ResolveAsync(string profileCode, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new AgentModelRuntimeProfile { ApiKey = "", ModelName = "offline", Endpoint = new Uri("https://offline.invalid"), Timeout = TimeSpan.FromSeconds(5) });
        }
    }
}
