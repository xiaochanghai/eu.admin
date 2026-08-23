using System.Data.Common;
using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;
using EU.Core.Api.MCP.Services.BusinessQuery.Execution;
using Microsoft.Data.Sqlite;
using SqlSugar;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Execution;

public sealed class DevelopmentSqliteReadOnlyConnectionFactory(
    DevelopmentSqliteDatabasePath databasePath) : IBusinessDbConnectionFactory
{
    public async Task<SqlSugarClient> CreateOpenConnectionAsync(
        BusinessDataSourceDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        if (!descriptor.ReadOnly
            || descriptor.Dialect != BusinessCatalogDialect.Sqlite
            || !string.Equals(
                descriptor.ProviderInvariantName,
                "Microsoft.Data.Sqlite",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The read-only data source is unavailable.");
        }

        string connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath.DatabasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Private,
            Pooling = true,
            DefaultTimeout = 5
        }.ToString();
        var database = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connectionString,
            DbType = SqlSugar.DbType.Sqlite,
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
