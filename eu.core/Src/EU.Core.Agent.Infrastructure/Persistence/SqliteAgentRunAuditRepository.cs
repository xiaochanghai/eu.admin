using System.Text.Json;
using System.Text.Json.Serialization;
using EU.Core.Agent.Application.Runtime;
using Microsoft.Data.Sqlite;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqliteAgentRunAuditRepository : IAgentRunAuditRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _connectionString;

    public SqliteAgentRunAuditRepository(string databasePath)
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
        AgentRunAuditRecord record,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO agent_run_audits
                (run_id, agent_id, started_at_utc, status, document_json)
            VALUES
                ($runId, $agentId, $startedAt, $status, $json)
            ON CONFLICT(run_id) DO UPDATE SET
                status = excluded.status,
                document_json = excluded.document_json
            WHERE agent_run_audits.agent_id = excluded.agent_id
              AND agent_run_audits.started_at_utc = excluded.started_at_utc;
            """;
        command.Parameters.AddWithValue("$runId", record.RunId.ToString("D"));
        command.Parameters.AddWithValue("$agentId", record.AgentId.ToString("D"));
        command.Parameters.AddWithValue(
            "$startedAt",
            record.StartedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$status", record.Status.ToString());
        command.Parameters.AddWithValue(
            "$json",
            JsonSerializer.Serialize(record, SerializerOptions));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AgentRunAuditRecord>> ListAsync(
        Guid agentId,
        int take,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT document_json
            FROM agent_run_audits
            WHERE agent_id = $agentId
            ORDER BY started_at_utc DESC, run_id DESC
            LIMIT $take;
            """;
        command.Parameters.AddWithValue("$agentId", agentId.ToString("D"));
        command.Parameters.AddWithValue("$take", Math.Clamp(take, 1, 100));
        var values = new List<AgentRunAuditRecord>();
        await using SqliteDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            AgentRunAuditRecord value =
                JsonSerializer.Deserialize<AgentRunAuditRecord>(
                    reader.GetString(0),
                    SerializerOptions) ??
                throw new InvalidDataException("The SQLite Agent run audit is empty.");
            values.Add(AgentRunContractCloner.Clone(value));
        }

        return values;
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
            CREATE TABLE IF NOT EXISTS agent_run_audits
            (
                run_id         TEXT NOT NULL PRIMARY KEY,
                agent_id       TEXT NOT NULL,
                started_at_utc TEXT NOT NULL,
                status         TEXT NOT NULL,
                document_json  TEXT NOT NULL CHECK (json_valid(document_json))
            ) WITHOUT ROWID;
            CREATE INDEX IF NOT EXISTS ix_agent_run_audits_agent_started
                ON agent_run_audits (agent_id, started_at_utc DESC);
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
