using System.Text.RegularExpressions;
using EU.Core.Api.MCP.Services.BusinessQuery.Configuration;
using EU.Core.Api.MCP.Services.BusinessQuery.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Security;

public sealed partial class SqliteBusinessQueryReplayRepository
{
    private readonly string _connectionString;
    private readonly int _maximumEntries;
    private readonly TimeProvider _timeProvider;

    public SqliteBusinessQueryReplayRepository(
        BusinessQueryStorePath store,
        IOptions<BusinessQueryOptions> options,
        TimeProvider timeProvider)
    {
        _connectionString = store.ConnectionString;
        _maximumEntries = options.Value.MaximumReplayEntries;
        _timeProvider = timeProvider;
        Initialize();
    }

    public async Task<bool> TryRegisterAsync(
        string jti,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (!JtiPattern().IsMatch(jti ?? string.Empty)
            || expiresAtUtc <= now
            || expiresAtUtc > now.AddSeconds(90))
        {
            return false;
        }

        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using (SqliteCommand cleanup = connection.CreateCommand())
        {
            cleanup.CommandText = "DELETE FROM business_query_replay WHERE expires_at_utc <= $now;";
            cleanup.Parameters.AddWithValue("$now", now.ToString("O"));
            await cleanup.ExecuteNonQueryAsync(cancellationToken);
        }

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO business_query_replay (jti, expires_at_utc, registered_at_utc)
            SELECT $jti, $expiresAtUtc, $registeredAtUtc
            WHERE (SELECT COUNT(*) FROM business_query_replay) < $maximumEntries
            ON CONFLICT(jti) DO NOTHING;
            """;
        command.Parameters.AddWithValue("$jti", jti);
        command.Parameters.AddWithValue("$expiresAtUtc", expiresAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$registeredAtUtc", now.ToString("O"));
        command.Parameters.AddWithValue("$maximumEntries", _maximumEntries);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task VerifyAsync(CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM business_query_replay LIMIT 1;";
        await command.ExecuteScalarAsync(cancellationToken);
    }

    private void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS business_query_replay (
                jti TEXT NOT NULL PRIMARY KEY,
                expires_at_utc TEXT NOT NULL,
                registered_at_utc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_business_query_replay_expires
                ON business_query_replay(expires_at_utc);
            """;
        command.ExecuteNonQuery();
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{16,128}$", RegexOptions.CultureInvariant)]
    private static partial Regex JtiPattern();
}
