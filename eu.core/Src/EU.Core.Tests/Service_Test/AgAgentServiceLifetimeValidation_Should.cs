using EU.Core.Agent.Application.UnifiedEntry;
using EU.Core.Api.Agent.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public sealed class AgAgentServiceLifetimeValidation_Should
{
    [Fact]
    public void Reject_stateful_runtime_service_registered_as_scoped()
    {
        var services = new ServiceCollection();
        services.AddScoped<UnifiedEntryService>();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.ValidateAgentServiceLifetimes());

        Assert.Contains(
            typeof(UnifiedEntryService).FullName!,
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Accept_expected_agent_service_lifetimes()
    {
        var services = new ServiceCollection();
        services.AddSingleton<UnifiedEntryService>();

        IServiceCollection result = services.ValidateAgentServiceLifetimes();

        Assert.Same(services, result);
    }
}
