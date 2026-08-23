using EU.Core.Api.MCP.Services.BusinessQuery.Configuration;
using EU.Core.Api.MCP.Services.BusinessQuery.Persistence;
using EU.Core.Api.MCP.Services.BusinessQuery.Policy;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Policy;

public sealed class SqliteBusinessQueryQuotaStore : IBusinessQueryQuotaStore
{
    private readonly string _connectionString;
    private readonly BusinessQueryOptions _options;

    public SqliteBusinessQueryQuotaStore(
        BusinessQueryStorePath store,
        IOptions<BusinessQueryOptions> options)
    {
        _connectionString = store.ConnectionString;
        _options = options.Value;
        Initialize();
    }

    public async Task<BusinessQueryQuotaReservationResult> TryReserveAsync(
        BusinessQueryQuotaRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Guid id = Guid.NewGuid();
        DateTimeOffset expiresAt = request.EvaluatedAtUtc
            .AddSeconds(_options.QuotaReservationTtlSeconds);
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO business_query_quota (
                reservation_id, user_id, tenant_id, plan_hash, complexity,
                evaluated_at_utc, expires_at_utc, status)
            SELECT
                $reservationId, $userId, $tenantId, $planHash, $complexity,
                $evaluatedAtUtc, $expiresAtUtc, 'reserved'
            WHERE (
                SELECT COUNT(*) FROM business_query_quota
                WHERE user_id = $userId AND status = 'reserved'
                    AND expires_at_utc > $evaluatedAtUtc
            ) < $maximumPerUser
            AND (
                SELECT COUNT(*) FROM business_query_quota
                WHERE tenant_id = $tenantId AND status = 'reserved'
                    AND expires_at_utc > $evaluatedAtUtc
            ) < $maximumPerTenant;
            """;
        command.Parameters.AddWithValue("$reservationId", id.ToString("D"));
        command.Parameters.AddWithValue("$userId", request.UserId);
        command.Parameters.AddWithValue("$tenantId", request.TenantId);
        command.Parameters.AddWithValue("$planHash", request.PlanHash);
        command.Parameters.AddWithValue("$complexity", request.Complexity);
        command.Parameters.AddWithValue("$evaluatedAtUtc", request.EvaluatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$expiresAtUtc", expiresAt.ToString("O"));
        command.Parameters.AddWithValue("$maximumPerUser", _options.MaximumConcurrentQueriesPerUser);
        command.Parameters.AddWithValue("$maximumPerTenant", _options.MaximumConcurrentQueriesPerTenant);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1
            ? BusinessQueryQuotaReservationResult.Allow(id)
            : BusinessQueryQuotaReservationResult.Deny();
    }

    public async Task SettleAsync(
        Guid reservationId,
        BusinessQueryQuotaOutcome outcome,
        CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE business_query_quota
            SET status = $status, settled_at_utc = $settledAtUtc
            WHERE reservation_id = $reservationId AND status = 'reserved';
            """;
        command.Parameters.AddWithValue("$reservationId", reservationId.ToString("D"));
        command.Parameters.AddWithValue("$status", outcome.ToString().ToLowerInvariant());
        command.Parameters.AddWithValue("$settledAtUtc", DateTimeOffset.UtcNow.ToString("O"));
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            throw new InvalidOperationException("The quota reservation is unavailable.");
        }
    }

    public async Task VerifyAsync(CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM business_query_quota LIMIT 1;";
        await command.ExecuteScalarAsync(cancellationToken);
    }

    private void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS business_query_quota (
                reservation_id TEXT NOT NULL PRIMARY KEY,
                user_id TEXT NOT NULL,
                tenant_id TEXT NOT NULL,
                plan_hash TEXT NOT NULL,
                complexity INTEGER NOT NULL,
                evaluated_at_utc TEXT NOT NULL,
                expires_at_utc TEXT NOT NULL,
                status TEXT NOT NULL CHECK(status IN ('reserved','succeeded','failed','cancelled')),
                settled_at_utc TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_business_query_quota_user_active
                ON business_query_quota(user_id, status, expires_at_utc);
            CREATE INDEX IF NOT EXISTS ix_business_query_quota_tenant_active
                ON business_query_quota(tenant_id, status, expires_at_utc);
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
