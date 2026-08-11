using System.Text.Json;
using EU.Core.Agent.Application.Abstractions.Auditing;
using Microsoft.Data.Sqlite;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqliteAgentOperationAuditRepository
    : IAgentOperationAuditRepository
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly string _connectionString;

    public SqliteAgentOperationAuditRepository(string databasePath)
    {
        string fullPath = Path.GetFullPath(
            string.IsNullOrWhiteSpace(databasePath)
                ? throw new ArgumentException(
                    "SQLite database path is required.",
                    nameof(databasePath))
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

    public async Task SaveAsync(
        AgentOperationAuditRecord record,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO agent_operation_audits
                (audit_id, tenant_id, occurred_at_utc, outcome, document_json)
            VALUES
                ($id, $tenantId, $occurredAt, $outcome, $json)
            ON CONFLICT(audit_id) DO UPDATE SET
                outcome = excluded.outcome,
                document_json = excluded.document_json
            WHERE agent_operation_audits.tenant_id = excluded.tenant_id
              AND agent_operation_audits.occurred_at_utc = excluded.occurred_at_utc
              AND agent_operation_audits.outcome = 'Started'
              AND json_extract(agent_operation_audits.document_json, '$.userId')
                  = json_extract(excluded.document_json, '$.userId')
              AND json_extract(agent_operation_audits.document_json, '$.correlationId')
                  = json_extract(excluded.document_json, '$.correlationId')
              AND json_extract(agent_operation_audits.document_json, '$.policy')
                  = json_extract(excluded.document_json, '$.policy')
              AND json_extract(agent_operation_audits.document_json, '$.method')
                  = json_extract(excluded.document_json, '$.method')
              AND json_extract(agent_operation_audits.document_json, '$.path')
                  = json_extract(excluded.document_json, '$.path');
            """;
        command.Parameters.AddWithValue("$id", record.Id.ToString("D"));
        command.Parameters.AddWithValue("$tenantId", record.TenantId);
        command.Parameters.AddWithValue("$occurredAt", record.OccurredAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$outcome", record.Outcome);
        command.Parameters.AddWithValue(
            "$json",
            JsonSerializer.Serialize(record, SerializerOptions));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AgentOperationAuditRecord>> ListAsync(
        string tenantId,
        int take,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT document_json
            FROM agent_operation_audits
            WHERE tenant_id = $tenantId
            ORDER BY occurred_at_utc DESC, audit_id DESC
            LIMIT $take;
            """;
        command.Parameters.AddWithValue("$tenantId", tenantId);
        command.Parameters.AddWithValue("$take", Math.Clamp(take, 1, 100));
        var records = new List<AgentOperationAuditRecord>();
        await using SqliteDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(
                JsonSerializer.Deserialize<AgentOperationAuditRecord>(
                    reader.GetString(0),
                    SerializerOptions) ??
                throw new InvalidDataException(
                    "The SQLite Agent operation audit is empty."));
        }

        return records;
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
            CREATE TABLE IF NOT EXISTS agent_operation_audits
            (
                audit_id        TEXT NOT NULL PRIMARY KEY,
                tenant_id       TEXT NOT NULL,
                occurred_at_utc TEXT NOT NULL,
                outcome         TEXT NOT NULL,
                document_json   TEXT NOT NULL CHECK (json_valid(document_json))
            ) WITHOUT ROWID;
            CREATE INDEX IF NOT EXISTS ix_agent_operation_audits_tenant_time
                ON agent_operation_audits
                    (tenant_id, occurred_at_utc DESC, audit_id DESC);
            """;
        command.ExecuteNonQuery();
    }

    private async Task<SqliteConnection> OpenAsync(
        CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
