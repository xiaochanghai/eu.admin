using System.Text.Json;
using EU.Core.Agent.Application.Evaluation;
using EU.Core.Agent.Application.UnifiedEntry;
using Microsoft.Data.Sqlite;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqliteEvaluationBatchRepository :
    IEvaluationBatchRepository,
    IEvaluationBatchRecovery
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _connectionString;

    public SqliteEvaluationBatchRepository(string databasePath)
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

    public async Task<EvaluationBatchRecord?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT document_json FROM evaluation_batches WHERE id=$id AND tenant_id=$tenantId;";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        command.Parameters.AddWithValue("$tenantId", tenantId);
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is string json ? Read(json) : null;
    }

    public async Task<IReadOnlyList<EvaluationBatchRecord>> ListAsync(
        Guid suiteId,
        string tenantId,
        int take,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT document_json FROM evaluation_batches
            WHERE suite_id=$suiteId AND tenant_id=$tenantId
            ORDER BY started_at_utc DESC, id DESC LIMIT $take;
            """;
        command.Parameters.AddWithValue("$suiteId", suiteId.ToString("D"));
        command.Parameters.AddWithValue("$tenantId", tenantId);
        command.Parameters.AddWithValue("$take", Math.Clamp(take, 1, 100));
        var values = new List<EvaluationBatchRecord>();
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(Read(reader.GetString(0)));
        }

        return EvaluationBatchContractCloner.ReadOnly(values);
    }

    public async Task<bool> TryCreateAsync(
        EvaluationBatchRecord value,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT OR IGNORE INTO evaluation_batches
                (id, tenant_id, suite_id, suite_version_id, status,
                 logical_revision, started_at_utc, document_json)
            VALUES
                ($id, $tenantId, $suiteId, $suiteVersionId, $status,
                 $revision, $started, $json);
            """;
        Add(command, value);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> TryReplaceAsync(
        EvaluationBatchRecord value,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default)
    {
        if (value.LogicalRevision != expectedLogicalRevision + 1)
        {
            return false;
        }

        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE evaluation_batches
            SET status=$status, logical_revision=$revision, document_json=$json
            WHERE id=$id AND tenant_id=$tenantId AND suite_id=$suiteId
              AND suite_version_id=$suiteVersionId
              AND status=$running AND logical_revision=$expected;
            """;
        Add(command, value);
        command.Parameters.AddWithValue("$running", EvaluationBatchStatus.Running.ToString());
        command.Parameters.AddWithValue("$expected", expectedLogicalRevision);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<int> RecoverInterruptedAsync(
        DateTimeOffset recoveredAtUtc,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<EvaluationBatchRecord> running = await ListRunningAsync(cancellationToken);
        int recovered = 0;
        foreach (EvaluationBatchRecord value in running)
        {
            EvaluationCaseExecutionRecord[] cases = value.Cases.Select(item =>
                item.Status == EvaluationCaseExecutionStatus.Running
                    ? item with
                    {
                        Status = EvaluationCaseExecutionStatus.Failed,
                        ErrorCode = UnifiedEntryErrorCodes.HostInterrupted
                    }
                    : item with { }).ToArray();
            EvaluationBatchRecord updated = value with
            {
                Status = EvaluationBatchStatus.Failed,
                LogicalRevision = value.LogicalRevision + 1,
                FinishedAtUtc = recoveredAtUtc.ToUniversalTime(),
                Cases = EvaluationBatchContractCloner.CloneCases(cases),
                ErrorCode = UnifiedEntryErrorCodes.HostInterrupted
            };
            if (await TryReplaceAsync(updated, value.LogicalRevision, cancellationToken))
            {
                recovered++;
            }
        }

        return recovered;
    }

    private async Task<IReadOnlyList<EvaluationBatchRecord>> ListRunningAsync(
        CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT document_json FROM evaluation_batches WHERE status=$status;";
        command.Parameters.AddWithValue("$status", EvaluationBatchStatus.Running.ToString());
        var values = new List<EvaluationBatchRecord>();
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(Read(reader.GetString(0)));
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
            CREATE TABLE IF NOT EXISTS evaluation_batches
            (
                id                TEXT NOT NULL PRIMARY KEY,
                tenant_id         TEXT NOT NULL,
                suite_id          TEXT NOT NULL,
                suite_version_id  TEXT NOT NULL,
                status            TEXT NOT NULL,
                logical_revision  INTEGER NOT NULL CHECK (logical_revision >= 0),
                started_at_utc    TEXT NOT NULL,
                document_json     TEXT NOT NULL CHECK (json_valid(document_json))
            ) WITHOUT ROWID;
            CREATE INDEX IF NOT EXISTS ix_evaluation_batches_suite_started
                ON evaluation_batches (tenant_id, suite_id, started_at_utc DESC);
            CREATE INDEX IF NOT EXISTS ix_evaluation_batches_status
                ON evaluation_batches (status);
            """;
        command.ExecuteNonQuery();
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static void Add(SqliteCommand command, EvaluationBatchRecord value)
    {
        command.Parameters.AddWithValue("$id", value.Id.ToString("D"));
        command.Parameters.AddWithValue("$tenantId", value.TenantId);
        command.Parameters.AddWithValue("$suiteId", value.SuiteId.ToString("D"));
        command.Parameters.AddWithValue("$suiteVersionId", value.SuiteVersionId.ToString("D"));
        command.Parameters.AddWithValue("$status", value.Status.ToString());
        command.Parameters.AddWithValue("$revision", value.LogicalRevision);
        command.Parameters.AddWithValue("$started", value.StartedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(value, JsonOptions));
    }

    private static EvaluationBatchRecord Read(string json) =>
        EvaluationBatchContractCloner.Clone(
            JsonSerializer.Deserialize<EvaluationBatchRecord>(json, JsonOptions)
            ?? throw new InvalidDataException("The SQLite evaluation batch document is empty."));
}
