using System.Globalization;
using EU.Core.Agent.Application.Abstractions.Security;
using Microsoft.Data.Sqlite;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqliteHttpIdempotencyRepository : IHttpIdempotencyRepository
{
    private readonly string _connectionString;

    public SqliteHttpIdempotencyRepository(string databasePath)
    {
        string fullPath = Path.GetFullPath(
            string.IsNullOrWhiteSpace(databasePath)
                ? throw new ArgumentException("SQLite database path is required.", nameof(databasePath))
                : databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = false,
            DefaultTimeout = 5
        }.ToString();
        EnsureCreated();
    }

    public async Task<HttpIdempotencyBeginResult> BeginAsync(
        HttpIdempotencyRecord pending,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using (SqliteCommand cleanup = connection.CreateCommand())
        {
            cleanup.CommandText =
                "DELETE FROM api_idempotency WHERE expires_at_utc <= $now;";
            cleanup.Parameters.AddWithValue("$now", nowUtc.ToUniversalTime().ToString("O"));
            await cleanup.ExecuteNonQueryAsync(cancellationToken);
        }

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO api_idempotency
                (scope_sha256, request_sha256, status, response_status_code,
                 response_content_type, response_location, response_body,
                 created_at_utc, expires_at_utc)
            VALUES
                ($scope, $request, $status, 0, '', '', X'', $created, $expires)
            ON CONFLICT(scope_sha256) DO UPDATE SET
                request_sha256=excluded.request_sha256,
                status=excluded.status,
                response_status_code=0,
                response_content_type='',
                response_location='',
                response_body=X'',
                created_at_utc=excluded.created_at_utc,
                expires_at_utc=excluded.expires_at_utc
            WHERE api_idempotency.expires_at_utc <= $now;
            """;
        command.Parameters.AddWithValue("$scope", pending.ScopeSha256);
        command.Parameters.AddWithValue("$request", pending.RequestSha256);
        command.Parameters.AddWithValue("$status", HttpIdempotencyStatus.InProgress.ToString());
        command.Parameters.AddWithValue("$created", pending.CreatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$expires", pending.ExpiresAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$now", nowUtc.ToUniversalTime().ToString("O"));
        bool acquired = await command.ExecuteNonQueryAsync(cancellationToken) == 1;
        HttpIdempotencyRecord record = acquired
            ? pending
            : await GetAsync(connection, pending.ScopeSha256, cancellationToken)
                ?? throw new InvalidOperationException("The idempotency record disappeared after a conflict.");
        return new HttpIdempotencyBeginResult(acquired, record);
    }

    public async Task<bool> CompleteAsync(
        string scopeSha256,
        string requestSha256,
        int responseStatusCode,
        string responseContentType,
        string responseLocation,
        byte[] responseBody,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE api_idempotency
            SET status=$completed, response_status_code=$responseStatus,
                response_content_type=$contentType, response_location=$location,
                response_body=$body
            WHERE scope_sha256=$scope AND request_sha256=$request AND status=$inProgress;
            """;
        command.Parameters.AddWithValue("$completed", HttpIdempotencyStatus.Completed.ToString());
        command.Parameters.AddWithValue("$responseStatus", responseStatusCode);
        command.Parameters.AddWithValue("$contentType", responseContentType);
        command.Parameters.AddWithValue("$location", responseLocation);
        command.Parameters.AddWithValue("$body", responseBody);
        command.Parameters.AddWithValue("$scope", scopeSha256);
        command.Parameters.AddWithValue("$request", requestSha256);
        command.Parameters.AddWithValue("$inProgress", HttpIdempotencyStatus.InProgress.ToString());
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task MarkIndeterminateAsync(
        string scopeSha256,
        string requestSha256,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE api_idempotency SET status=$indeterminate
            WHERE scope_sha256=$scope AND request_sha256=$request AND status=$inProgress;
            """;
        command.Parameters.AddWithValue("$indeterminate", HttpIdempotencyStatus.Indeterminate.ToString());
        command.Parameters.AddWithValue("$scope", scopeSha256);
        command.Parameters.AddWithValue("$request", requestSha256);
        command.Parameters.AddWithValue("$inProgress", HttpIdempotencyStatus.InProgress.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AbandonAsync(
        string scopeSha256,
        string requestSha256,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            DELETE FROM api_idempotency
            WHERE scope_sha256=$scope AND request_sha256=$request AND status=$inProgress;
            """;
        command.Parameters.AddWithValue("$scope", scopeSha256);
        command.Parameters.AddWithValue("$request", requestSha256);
        command.Parameters.AddWithValue("$inProgress", HttpIdempotencyStatus.InProgress.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<HttpIdempotencyRecord?> GetAsync(
        SqliteConnection connection,
        string scopeSha256,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT request_sha256, status, response_status_code,
                   response_content_type, response_location, response_body,
                   created_at_utc, expires_at_utc
            FROM api_idempotency WHERE scope_sha256=$scope;
            """;
        command.Parameters.AddWithValue("$scope", scopeSha256);
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new HttpIdempotencyRecord(
            scopeSha256,
            reader.GetString(0),
            Enum.Parse<HttpIdempotencyStatus>(reader.GetString(1), ignoreCase: false),
            reader.GetInt32(2),
            reader.GetString(3),
            reader.GetString(4),
            (byte[])reader[5],
            DateTimeOffset.Parse(
                reader.GetString(6),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind),
            DateTimeOffset.Parse(
                reader.GetString(7),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind));
    }

    private void EnsureCreated()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            PRAGMA journal_mode = WAL;
            PRAGMA busy_timeout = 5000;
            CREATE TABLE IF NOT EXISTS api_idempotency
            (
                scope_sha256          TEXT NOT NULL PRIMARY KEY,
                request_sha256        TEXT NOT NULL,
                status                TEXT NOT NULL,
                response_status_code  INTEGER NOT NULL,
                response_content_type TEXT NOT NULL,
                response_location     TEXT NOT NULL,
                response_body         BLOB NOT NULL,
                created_at_utc        TEXT NOT NULL,
                expires_at_utc        TEXT NOT NULL
            ) WITHOUT ROWID;
            CREATE INDEX IF NOT EXISTS ix_api_idempotency_expires
                ON api_idempotency (expires_at_utc);
            """;
        command.ExecuteNonQuery();
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
