using System.Text.Json;
using EU.Core.Api.MCP.Services.BD;
using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;
using EU.Core.Api.MCP.Services.BusinessQuery.Execution;
using EU.Core.Api.MCP.Services.BusinessQuery.Auditing;
using EU.Core.Api.MCP.Services.BusinessQuery.Configuration;
using EU.Core.Api.MCP.Services.BusinessQuery.Health;
using EU.Core.Api.MCP.Services.BusinessQuery.Persistence;
using EU.Core.Api.MCP.Services.BusinessQuery.Policy;
using EU.Core.Api.MCP.Services.BusinessQuery.Security;
using EU.Core.Api.MCP.Services.BusinessQuery;
using EU.Core.Api.MCP.Services.BusinessQuery.Protection;
using EU.Core.Api.MCP.Services.BusinessQuery.Tooling;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace EU.Core.MCP.Api.Extensions;

public static class McpServiceExtensions
{
    private const string EnabledKey = "BusinessQuery:Enabled";

    public static IServiceCollection AddMcpServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ISupplierService, SupplierService>();
        if (!configuration.GetValue<bool>(EnabledKey))
        {
            return services;
        }

        services.AddOptions<BusinessQueryOptions>()
            .BindConfiguration(BusinessQueryOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<BusinessQueryOptions>, BusinessQueryOptionsValidator>();
        services.AddSingleton(serviceProvider =>
        {
            BusinessQueryOptions options = serviceProvider
                .GetRequiredService<IOptions<BusinessQueryOptions>>().Value;
            IHostEnvironment environment = serviceProvider.GetRequiredService<IHostEnvironment>();
            string path = Path.GetFullPath(options.CatalogPath, environment.ContentRootPath);
            string root = Path.GetFullPath(environment.ContentRootPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
            {
                throw new InvalidOperationException("The configured business Catalog is unavailable.");
            }

            BusinessCatalogLoadResult loaded = new BusinessSemanticCatalogLoader().Load(
                File.ReadAllText(path),
                options.ExpectedCatalogHash);
            BusinessCatalogSnapshot catalog = loaded.Snapshot
                ?? throw new InvalidOperationException(loaded.Error?.Code ?? "BUSINESS_CATALOG_INVALID");
            BusinessCatalogDialect configuredDialect = options.Dialect switch
            {
                "SqlServer" => BusinessCatalogDialect.SqlServer,
                "MySql" => BusinessCatalogDialect.MySql,
                "Sqlite" when environment.IsDevelopment() && options.AllowDevelopmentSqlite =>
                    BusinessCatalogDialect.Sqlite,
                _ => throw new InvalidOperationException("The configured business data provider is unavailable.")
            };
            if (!string.Equals(catalog.DataSourceCode, options.DataSourceCode, StringComparison.Ordinal)
                || catalog.Dialect != configuredDialect)
            {
                throw new InvalidOperationException("The active Catalog does not match Host configuration.");
            }

            return catalog;
        });
        services.AddSingleton(serviceProvider => new BusinessQueryToolSchemaBuilder().Build(
            serviceProvider.GetRequiredService<BusinessCatalogSnapshot>()));
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<BusinessQueryStorePath>();
        services.AddSingleton<SqliteBusinessQueryAuditRepository>();
        services.AddSingleton<IBusinessQueryAuditRepository>(serviceProvider =>
            serviceProvider.GetRequiredService<SqliteBusinessQueryAuditRepository>());
        services.AddSingleton<SqliteBusinessQueryReplayRepository>();
        services.AddSingleton<BusinessQueryExecutionContextKeyResolver>();
        services.AddSingleton<BusinessQueryServiceTokenResolver>();
        services.AddSingleton<BusinessQueryExecutionContextAccessor>();
        services.AddSingleton<BusinessQueryExecutionContextVerifier>();
        services.AddSingleton<SqliteBusinessQueryQuotaStore>();
        services.AddSingleton<IBusinessQueryQuotaStore>(serviceProvider =>
            serviceProvider.GetRequiredService<SqliteBusinessQueryQuotaStore>());
        services.AddSingleton<BusinessQueryReadiness>();
        services.AddSingleton<BusinessQueryResultProtector>();
        services.AddSingleton<IBusinessQueryExecutor, SqlSugarBusinessQueryExecutor>();
        services.AddSingleton<BusinessQueryService>();
        services.AddSingleton<IBusinessQueryService>(serviceProvider =>
            serviceProvider.GetRequiredService<BusinessQueryService>());
        services.AddSingleton<McpServerTool>(serviceProvider =>
        {
            BusinessQueryService target = serviceProvider.GetRequiredService<BusinessQueryService>();
            BusinessQueryToolDefinition definition = serviceProvider
                .GetRequiredService<BusinessQueryToolDefinition>();
            McpServerTool tool = McpServerTool.Create(
                typeof(BusinessQueryService).GetMethod(nameof(BusinessQueryService.QueryAsync))!,
                target,
                new McpServerToolCreateOptions
                {
                    Services = serviceProvider,
                    Name = definition.Name,
                    Description = definition.Description,
                    ReadOnly = true,
                    Destructive = false,
                    OpenWorld = false,
                    Idempotent = true
                });
            tool.ProtocolTool.InputSchema = JsonSerializer.Deserialize<JsonElement>(
                definition.InputSchemaJson);
            return tool;
        });
        services.AddMcpServer()
            .WithHttpTransport()
            .WithRequestFilters(filters => filters.AddCallToolFilter(
                next => async (context, cancellationToken) =>
                {
                    BusinessQueryExecutionContextVerifier verifier = context.Services!
                        .GetRequiredService<BusinessQueryExecutionContextVerifier>();
                    return await verifier.CreateFilter()(next)(context, cancellationToken);
                }));
        return services;
    }
}
