namespace EU.Core.Api.MCP.Services.BusinessQuery.Execution;

public interface IBusinessDataSourceCredentialResolver
{
    ValueTask<string> ResolveAsync(
        string credentialAlias,
        CancellationToken cancellationToken);
}
