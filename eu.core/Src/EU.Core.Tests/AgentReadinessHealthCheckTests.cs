#nullable enable
using System.Reflection;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Health;
using EU.Core.Api.Agent.Observability;
using EU.Core.IServices;
using EU.Core.IServices.Abstractions.Auditing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace EU.Core.Tests;

/// <summary>就绪检查离线测试：模型与审计均为替身，只探测独立临时目录。</summary>
public sealed class AgentReadinessHealthCheckTests
{
    [Fact]
    public async Task Ready_checks_all_enabled_profiles_without_legacy_credentials()
    {
        using var fixture = new Fixture();
        var result = await fixture.Check.CheckHealthAsync(new());
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("ready", result.Data["modelCredential"]);
        Assert.Equal(new[] { "first", "second" }, fixture.Models.Resolved);
        Assert.Empty(Directory.GetFiles(fixture.Root));
        Assert.DoesNotContain("offline-secret", System.Text.Json.JsonSerializer.Serialize(result.Data));
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("list-failure")]
    [InlineData("resolve-failure")]
    [InlineData("blank-key")]
    [InlineData("null-profile")]
    public async Task Invalid_model_dependencies_are_unavailable_without_secret_details(string failure)
    {
        using var fixture = new Fixture();
        fixture.Models.Failure = failure;
        var result = await fixture.Check.CheckHealthAsync(new());
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("unavailable", result.Data["modelCredential"]);
        Assert.Null(result.Exception);
        Assert.DoesNotContain("offline-secret", result.Description ?? "");
        Assert.DoesNotContain("offline-secret", System.Text.Json.JsonSerializer.Serialize(result.Data));
    }

    [Fact]
    public async Task Drain_does_not_query_dependencies()
    {
        using var fixture = new Fixture();
        fixture.Drain.BeginDrain();
        var result = await fixture.Check.CheckHealthAsync(new());
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("draining", result.Data["hostLifecycle"]);
        Assert.Equal(0, fixture.Models.ListCalls);
    }

    [Fact]
    public async Task Cancellation_during_model_resolution_propagates()
    {
        using var fixture = new Fixture();
        using var cancellation = new CancellationTokenSource();
        fixture.Models.OnResolve = cancellation.Cancel;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fixture.Check.CheckHealthAsync(new(), cancellation.Token));
    }

    [Fact]
    public async Task Audit_failure_still_marks_storage_unavailable()
    {
        using var fixture = new Fixture();
        fixture.Audit.Fail = true;
        var result = await fixture.Check.CheckHealthAsync(new());
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("unavailable", result.Data["storage"]);
        Assert.Equal("ready", result.Data["modelCredential"]);
    }

    private sealed class Fixture : IDisposable
    {
        public string Root { get; } = Directory.CreateTempSubdirectory("agent-readiness-test-").FullName;
        private readonly AgentMetrics _metrics = new();
        public ModelProxy Models { get; }
        public AuditStub Audit { get; } = new();
        public HostDrainState Drain { get; }
        public AgentReadinessHealthCheck Check { get; }

        public Fixture()
        {
            var service = DispatchProxy.Create<IAgModelConfigServices, ModelProxy>();
            Models = (ModelProxy)(object)service;
            Drain = new HostDrainState(_metrics);
            Check = new AgentReadinessHealthCheck(Audit, service,
                Options.Create(new AgentStorageOptions { SkillRootPath = Root }),
                new TestEnvironment { ContentRootPath = Root }, Drain);
        }

        public void Dispose()
        {
            _metrics.Dispose();
            Directory.Delete(Root, recursive: true);
        }
    }

    public class ModelProxy : DispatchProxy
    {
        public string Failure { get; set; } = "";
        public int ListCalls { get; private set; }
        public List<string> Resolved { get; } = [];
        public Action? OnResolve { get; set; }

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == nameof(IAgModelConfigServices.ListAvailableProfilesAsync))
            {
                ListCalls++;
                if (Failure == "list-failure") throw new InvalidOperationException("offline-secret");
                return Task.FromResult<IReadOnlyList<string>>(Failure == "empty" ? [] : ["first", "second"]);
            }
            if (method?.Name == nameof(IAgModelConfigServices.ResolveRuntimeProfileAsync))
            {
                Resolved.Add((string)args![0]!);
                OnResolve?.Invoke();
                ((CancellationToken)args[1]!).ThrowIfCancellationRequested();
                if (Failure == "resolve-failure" && Resolved.Count == 2)
                    throw new InvalidOperationException("offline-secret");
                return Task.FromResult(Failure == "null-profile" ? null! : new AgentModelRuntimeProfile
                {
                    ApiKey = Failure == "blank-key" ? " " : "offline-secret",
                    Endpoint = new Uri("https://models.example.test/v1"),
                    ModelName = "offline-model", Timeout = TimeSpan.FromSeconds(150)
                });
            }
            throw new NotSupportedException(method?.Name);
        }
    }

    private sealed class AuditStub : IAgAgentOperationAuditServices
    {
        public bool Fail { get; set; }
        public Task SaveAsync(AgentOperationAuditRecord record, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<AgentOperationAuditRecord>> ListAsync(string tenantId, int take, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Fail) throw new InvalidOperationException("offline storage failure");
            return Task.FromResult<IReadOnlyList<AgentOperationAuditRecord>>([]);
        }
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "ReadinessTests";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
