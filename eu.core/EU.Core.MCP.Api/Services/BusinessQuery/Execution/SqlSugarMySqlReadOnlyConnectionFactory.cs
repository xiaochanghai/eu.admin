using System.Data;
using System.Data.Common;
using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;
using EU.Core.Api.MCP.Services.BusinessQuery.Execution;
using MySqlConnector;
using SqlSugar;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Execution;

public sealed class SqlSugarMySqlReadOnlyConnectionFactory(
    IBusinessDataSourceCredentialResolver resolver) : IBusinessDbConnectionFactory
{
    public async Task<SqlSugarClient> CreateOpenConnectionAsync(
        BusinessDataSourceDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        if (!descriptor.ReadOnly
            || descriptor.Dialect != BusinessCatalogDialect.MySql
            || !string.Equals(
                descriptor.ProviderInvariantName,
                "SqlSugar.MySql",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The read-only data source is unavailable.");
        }

        string secret = await resolver.ResolveAsync(
            descriptor.CredentialAlias,
            cancellationToken);
        ConnectionConfig configuration = CreateConnectionConfig(secret);
        var database = new SqlSugarClient(configuration);
        try
        {
            if (database.Ado.Connection is not DbConnection connection)
            {
                throw new InvalidOperationException();
            }
            await connection.OpenAsync(cancellationToken);
            if (!IsSupportedServerVersion(connection.ServerVersion))
            {
                throw new InvalidOperationException();
            }
            await ExecuteTrustedSessionCommandAsync(
                connection,
                "SET SESSION TRANSACTION READ ONLY",
                cancellationToken);
            object? readOnly = await ExecuteTrustedScalarAsync(
                connection,
                "SELECT @@session.transaction_read_only",
                cancellationToken);
            if (Convert.ToInt32(readOnly, System.Globalization.CultureInfo.InvariantCulture) != 1)
            {
                throw new InvalidOperationException();
            }
            await ExecuteTrustedSessionCommandAsync(
                connection,
                "START TRANSACTION READ ONLY",
                cancellationToken);
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
            throw new InvalidOperationException(
                "The read-only data source is unavailable.");
        }
    }

    internal static ConnectionConfig CreateConnectionConfig(string secret)
    {
        MySqlConnectionStringBuilder builder;
        try
        {
            builder = new MySqlConnectionStringBuilder(secret);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException(
                "The read-only data source is unavailable.");
        }

        if (string.IsNullOrWhiteSpace(builder.Server)
            || string.IsNullOrWhiteSpace(builder.Database)
            || string.IsNullOrWhiteSpace(builder.UserID)
            || string.IsNullOrWhiteSpace(builder.Password)
            || builder.SslMode is not (
                MySqlSslMode.Required or MySqlSslMode.VerifyCA or MySqlSslMode.VerifyFull))
        {
            throw new InvalidOperationException(
                "The read-only data source is unavailable.");
        }

        builder.AllowLoadLocalInfile = false;
        builder.AllowUserVariables = false;
        builder.ConnectionReset = true;
        builder.Pooling = true;
        return new ConnectionConfig
        {
            ConnectionString = builder.ConnectionString,
            DbType = SqlSugar.DbType.MySql,
            IsAutoCloseConnection = false,
            InitKeyType = InitKeyType.Attribute
        };
    }

    internal static bool IsSupportedServerVersion(string? serverVersion)
    {
        if (string.IsNullOrWhiteSpace(serverVersion)
            || serverVersion.Contains("MariaDB", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string numeric = serverVersion.Split('-', 2)[0];
        return Version.TryParse(numeric, out Version? version)
            && version.Major >= 8;
    }

    private static async Task ExecuteTrustedSessionCommandAsync(
        DbConnection connection,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        command.CommandType = CommandType.Text;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<object?> ExecuteTrustedScalarAsync(
        DbConnection connection,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        command.CommandType = CommandType.Text;
        return await command.ExecuteScalarAsync(cancellationToken);
    }

}
