using System.Data.Common;
using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;
using EU.Core.Api.MCP.Services.BusinessQuery.Execution;
using Microsoft.Data.SqlClient;
using SqlSugar;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Execution;

public sealed class SqlServerReadOnlyConnectionFactory(
    IBusinessDataSourceCredentialResolver resolver) : IBusinessDbConnectionFactory
{
    public async Task<SqlSugarClient> CreateOpenConnectionAsync(
        BusinessDataSourceDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        if (!descriptor.ReadOnly
            || descriptor.Dialect != BusinessCatalogDialect.SqlServer
            || !string.Equals(
                descriptor.ProviderInvariantName,
                "Microsoft.Data.SqlClient",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The read-only data source is unavailable.");
        }

        string secret = await resolver.ResolveAsync(
            descriptor.CredentialAlias,
            cancellationToken);
        var builder = new SqlConnectionStringBuilder(secret)
        {
            ApplicationIntent = ApplicationIntent.ReadOnly,
            Encrypt = true,
            TrustServerCertificate = false
        };
        if (string.IsNullOrWhiteSpace(builder.DataSource)
            || string.IsNullOrWhiteSpace(builder.InitialCatalog)
            || !string.IsNullOrEmpty(builder.AttachDBFilename))
        {
            throw new InvalidOperationException("The read-only data source is unavailable.");
        }

        var database = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = builder.ConnectionString,
            DbType = SqlSugar.DbType.SqlServer,
            IsAutoCloseConnection = false,
            InitKeyType = InitKeyType.Attribute
        });
        try
        {
            if (database.Ado.Connection is not DbConnection connection)
            {
                throw new InvalidOperationException();
            }

            await connection.OpenAsync(cancellationToken);
            return database;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            database.Dispose();
            throw;
        }
        catch
        {
            database.Dispose();
            throw new InvalidOperationException("The read-only data source is unavailable.");
        }
    }
}
