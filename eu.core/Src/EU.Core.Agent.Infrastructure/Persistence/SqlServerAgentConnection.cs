using Microsoft.Data.SqlClient;

namespace EU.Core.Agent.Infrastructure.Persistence;

internal static class SqlServerAgentConnection
{
    public static string Validate(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException exception)
        {
            throw new ArgumentException(
                "The SQL Server connection string is invalid.",
                nameof(connectionString),
                exception);
        }

        if (string.IsNullOrWhiteSpace(builder.DataSource))
        {
            throw new ArgumentException(
                "The SQL Server connection string must specify a data source.",
                nameof(connectionString));
        }

        if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
        {
            throw new ArgumentException(
                "The SQL Server connection string must specify an initial catalog.",
                nameof(connectionString));
        }

        if (string.IsNullOrWhiteSpace(builder.ApplicationName))
        {
            builder.ApplicationName = "EU.Core.Api.Agent";
        }

        return builder.ConnectionString;
    }

    public static async Task<SqlConnection> OpenAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
