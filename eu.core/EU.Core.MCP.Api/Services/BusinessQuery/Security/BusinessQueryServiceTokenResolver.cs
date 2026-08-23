using EU.Core.Api.MCP.Services.BusinessQuery.Configuration;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Security;

public sealed class BusinessQueryServiceTokenResolver(
    IOptions<BusinessQueryOptions> options,
    IHostEnvironment environment)
{
    public string Resolve()
    {
        BusinessQueryOptions configuration = options.Value;
        if (environment.IsDevelopment()
            && !string.IsNullOrEmpty(configuration.DevelopmentServiceToken))
        {
            return configuration.DevelopmentServiceToken;
        }

        return Environment.GetEnvironmentVariable(
            configuration.ServiceTokenEnvironmentVariable) ?? string.Empty;
    }
}
