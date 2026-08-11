using System.Globalization;
using EU.Core.Agent.Application.Abstractions.Security;
using Microsoft.Data.SqlClient;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqlServerHttpIdempotencyRepository : IHttpIdempotencyRepository
{
    private readonly string _connectionString;

    public SqlServerHttpIdempotencyRepository(string connectionString)
    {
        _connectionString = SqlServerAgentConnection.Validate(connectionString);
    }

    public async Task<HttpIdempotencyBeginResult> BeginAsync(
        HttpIdempotencyRecord pending,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using (SqlCommand cleanup = connection.CreateCommand())
        {
            cleanup.CommandText =
                "DELETE FROM AgApiIdempotency WHERE ExpiresAtUtc <= @now;";
            cleanup.Parameters.AddWithValue("@now", nowUtc.ToUniversalTime().ToString("O"));
            await cleanup.ExecuteNonQueryAsync(cancellationToken);
        }

        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO AgApiIdempotency
                (ScopeSha256, RequestSha256, Status, ResponseStatusCode,
                 ResponseContentType, ResponseLocation, ResponseBody,
                 CreatedAtUtc, ExpiresAtUtc)
            SELECT @scope, @request, @Status, 0, '', '', @emptyBody, @created, @expires
            WHERE NOT EXISTS
            (
                SELECT 1 FROM AgApiIdempotency WITH (UPDLOCK, HOLDLOCK)
                WHERE ScopeSha256=@scope
            );
            """;
        command.Parameters.AddWithValue("@scope", pending.ScopeSha256);
        command.Parameters.AddWithValue("@request", pending.RequestSha256);
        command.Parameters.AddWithValue("@Status", HttpIdempotencyStatus.InProgress.ToString());
        command.Parameters.AddWithValue("@created", pending.CreatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("@expires", pending.ExpiresAtUtc.ToString("O"));
        command.Parameters.AddWithValue("@emptyBody", Array.Empty<byte>());
        command.Parameters.AddWithValue("@now", nowUtc.ToUniversalTime().ToString("O"));
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
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE AgApiIdempotency
            SET Status=@completed, ResponseStatusCode=@responseStatus,
                ResponseContentType=@contentType, ResponseLocation=@location,
                ResponseBody=@body
            WHERE ScopeSha256=@scope AND RequestSha256=@request AND Status=@inProgress;
            """;
        command.Parameters.AddWithValue("@completed", HttpIdempotencyStatus.Completed.ToString());
        command.Parameters.AddWithValue("@responseStatus", responseStatusCode);
        command.Parameters.AddWithValue("@contentType", responseContentType);
        command.Parameters.AddWithValue("@location", responseLocation);
        command.Parameters.AddWithValue("@body", responseBody);
        command.Parameters.AddWithValue("@scope", scopeSha256);
        command.Parameters.AddWithValue("@request", requestSha256);
        command.Parameters.AddWithValue("@inProgress", HttpIdempotencyStatus.InProgress.ToString());
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task MarkIndeterminateAsync(
        string scopeSha256,
        string requestSha256,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE AgApiIdempotency SET Status=@indeterminate
            WHERE ScopeSha256=@scope AND RequestSha256=@request AND Status=@inProgress;
            """;
        command.Parameters.AddWithValue("@indeterminate", HttpIdempotencyStatus.Indeterminate.ToString());
        command.Parameters.AddWithValue("@scope", scopeSha256);
        command.Parameters.AddWithValue("@request", requestSha256);
        command.Parameters.AddWithValue("@inProgress", HttpIdempotencyStatus.InProgress.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AbandonAsync(
        string scopeSha256,
        string requestSha256,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            DELETE FROM AgApiIdempotency
            WHERE ScopeSha256=@scope AND RequestSha256=@request AND Status=@inProgress;
            """;
        command.Parameters.AddWithValue("@scope", scopeSha256);
        command.Parameters.AddWithValue("@request", requestSha256);
        command.Parameters.AddWithValue("@inProgress", HttpIdempotencyStatus.InProgress.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<HttpIdempotencyRecord?> GetAsync(
        SqlConnection connection,
        string scopeSha256,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT RequestSha256, Status, ResponseStatusCode,
                   ResponseContentType, ResponseLocation, ResponseBody,
                   CreatedAtUtc, ExpiresAtUtc
            FROM AgApiIdempotency WHERE ScopeSha256=@scope;
            """;
        command.Parameters.AddWithValue("@scope", scopeSha256);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
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



    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        return await SqlServerAgentConnection.OpenAsync(_connectionString, cancellationToken);
    }
}
