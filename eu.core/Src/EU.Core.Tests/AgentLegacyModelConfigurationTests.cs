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
    [Theory]
    [InlineData(null, "agent-api", true)]
    [InlineData("custom-agent", "custom-agent", true)]
    [InlineData("", "", false)]
    [InlineData("Invalid_Name", "Invalid_Name", false)]
    public void Service_name_uses_default_without_dotenv_and_validates_overrides(string? configured, string expected, bool valid)
    {
        using var configuration = new ConfigurationManager();
        if (configured is not null) configuration["AgentPlatform:ServiceName"] = configured;
        var options = configuration.GetSection(AgentPlatformOptions.SectionName).Get<AgentPlatformOptions>() ?? new AgentPlatformOptions();
        Assert.Equal(expected, options.ServiceName);
        Assert.Equal(valid, new AgentPlatformOptionsValidator(configuration).Validate(null, options).Succeeded);
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
