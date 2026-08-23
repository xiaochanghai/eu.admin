using System.Text.Json;
using EU.Core.Api.MCP.Services.BusinessQuery.Persistence;
using Microsoft.Data.Sqlite;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Auditing;

public sealed class SqliteBusinessQueryAuditRepository : IBusinessQueryAuditRepository
{
    private const string Schema = """
        CREATE TABLE IF NOT EXISTS business_query_audits (
            query_id TEXT NOT NULL PRIMARY KEY,
            user_id TEXT NOT NULL,
            tenant_id TEXT NOT NULL,
            catalog_revision INTEGER NOT NULL,
            query_plan_hash TEXT NOT NULL,
            policy_rule_ids_json TEXT NOT NULL,
            sql_template_hash TEXT NOT NULL,
            row_count INTEGER NOT NULL,
            duration_ms INTEGER NOT NULL,
            terminal_status TEXT NOT NULL,
            error_code TEXT NULL,
            completed_at_utc TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS ix_business_query_audits_completed
            ON business_query_audits(completed_at_utc);
        CREATE TABLE IF NOT EXISTS business_query_security_audits (
            event_id TEXT NOT NULL PRIMARY KEY,
            event_type TEXT NOT NULL,
            terminal_status TEXT NOT NULL,
            error_code TEXT NOT NULL,
            completed_at_utc TEXT NOT NULL
        );
        """;

    private readonly string _connectionString;

    public SqliteBusinessQueryAuditRepository(BusinessQueryStorePath store)
    {
        _connectionString = store.ConnectionString;
        Initialize();
    }

    public async Task WriteTerminalAsync(
        BusinessQueryAuditRecord record,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO business_query_audits (
                query_id, user_id, tenant_id, catalog_revision, query_plan_hash,
                policy_rule_ids_json, sql_template_hash, row_count, duration_ms,
                terminal_status, error_code, completed_at_utc)
            VALUES (
                $queryId, $userId, $tenantId, $catalogRevision, $queryPlanHash,
                $policyRuleIds, $sqlTemplateHash, $rowCount, $durationMs,
                $terminalStatus, $errorCode, $completedAtUtc);
            """;
        command.Parameters.AddWithValue("$queryId", record.QueryId.ToString("D"));
        command.Parameters.AddWithValue("$userId", record.UserId);
        command.Parameters.AddWithValue("$tenantId", record.TenantId);
        command.Parameters.AddWithValue("$catalogRevision", record.CatalogRevision);
        command.Parameters.AddWithValue("$queryPlanHash", record.QueryPlanHash);
        command.Parameters.AddWithValue(
            "$policyRuleIds",
            JsonSerializer.Serialize(record.PolicyRuleIds.Order(StringComparer.Ordinal)));
        command.Parameters.AddWithValue("$sqlTemplateHash", record.SqlTemplateHash);
        command.Parameters.AddWithValue("$rowCount", record.RowCount);
        command.Parameters.AddWithValue("$durationMs", record.DurationMilliseconds);
        command.Parameters.AddWithValue("$terminalStatus", record.TerminalStatus);
        command.Parameters.AddWithValue("$errorCode", (object?)record.ErrorCode ?? DBNull.Value);
        command.Parameters.AddWithValue("$completedAtUtc", record.CompletedAtUtc.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task WriteSecurityRejectionAsync(
        BusinessQuerySecurityAuditRecord record,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO business_query_security_audits (
                event_id, event_type, terminal_status, error_code, completed_at_utc)
            VALUES ($eventId, $eventType, $terminalStatus, $errorCode, $completedAtUtc);
            """;
        command.Parameters.AddWithValue("$eventId", record.EventId.ToString("D"));
        command.Parameters.AddWithValue("$eventType", record.EventType);
        command.Parameters.AddWithValue("$terminalStatus", record.TerminalStatus);
        command.Parameters.AddWithValue("$errorCode", record.ErrorCode);
        command.Parameters.AddWithValue("$completedAtUtc", record.CompletedAtUtc.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task VerifyAsync(CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM business_query_audits LIMIT 1;";
        await command.ExecuteScalarAsync(cancellationToken);
    }

    private void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON; " + Schema;
        command.ExecuteNonQuery();
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
