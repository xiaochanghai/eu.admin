using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Execution;

public sealed record BusinessDataSourceDescriptor(
    string DataSourceCode,
    string ProviderInvariantName,
    BusinessCatalogDialect Dialect,
    string CredentialAlias,
    bool ReadOnly);
