using System.Text.Json;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Agent.Application.Abstractions.Auditing;
using EU.Core.Agent.Infrastructure.Skills;
using EU.Core.Agent.Runtime;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Health;

public sealed class AgentReadinessHealthCheck : IHealthCheck
{
    private readonly IAgentOperationAuditRepository _storage;
    private readonly IModelCredentialResolver _credentials;
    private readonly AgentAuthenticationOptions _authentication;
    private readonly AgentPlatformOptions _platform;
    private readonly string _skillRoot;
    private readonly HostDrainState _drainState;

    public AgentReadinessHealthCheck(
        IAgentOperationAuditRepository storage,
        IModelCredentialResolver credentials,
        IOptions<AgentAuthenticationOptions> authentication,
        IOptions<AgentPlatformOptions> platform,
        IOptions<AgentStorageOptions> storageOptions,
        IHostEnvironment environment,
        HostDrainState drainState)
    {
        _storage = storage;
        _credentials = credentials;
        _authentication = authentication.Value;
        _platform = platform.Value;
        _skillRoot = storageOptions.Value.ResolveSkillRootPath(
            environment.ContentRootPath);
        _drainState = drainState;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_drainState.IsDraining)
        {
            return HealthCheckResult.Unhealthy(
                "The Agent instance is draining.",
                data: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["hostLifecycle"] = "draining"
                });
        }

        var components = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["hostLifecycle"] = "ready",
            ["storage"] = await CheckStorageAsync(cancellationToken),
            ["skillStorage"] = await CheckSkillStorageAsync(cancellationToken),
            ["modelCredential"] = await CheckCredentialAsync(cancellationToken)
        };
        bool ready = components.Values.All(value =>
            string.Equals(value as string, "ready", StringComparison.Ordinal));
        return ready
            ? HealthCheckResult.Healthy("Agent dependencies are ready.", components)
            : HealthCheckResult.Unhealthy(
                "One or more Agent dependencies are unavailable.",
                data: components);
    }

    public static Task WriteResponseAsync(
        HttpContext context,
        HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        return JsonSerializer.SerializeAsync(
            context.Response.Body,
            new
            {
                status = report.Status.ToString(),
                checks = report.Entries
                    .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                    .Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        components = entry.Value.Data
                    })
            });
    }

    private async Task<string> CheckStorageAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await _storage.ListAsync(
                _authentication.TenantId,
                1,
                cancellationToken);
            return "ready";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return "unavailable";
        }
    }

    private async Task<string> CheckCredentialAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            string? credential = await _credentials.ResolveAsync(
                _platform.ModelCredentialAlias,
                cancellationToken);
            return string.IsNullOrWhiteSpace(credential)
                ? "unavailable"
                : "ready";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return "unavailable";
        }
    }

    private async Task<string> CheckSkillStorageAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var store = new ControlledSkillFileStore(_skillRoot);
            await store.ProbeReadinessAsync(cancellationToken);
            return "ready";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return "unavailable";
        }
    }
}
