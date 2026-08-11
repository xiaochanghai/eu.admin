using System.Text.Json;
using EU.Core.Agent.Application.Evaluation;
using Microsoft.Data.Sqlite;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqliteModelJudgeReportRepository : IModelJudgeReportRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _connectionString;

    public SqliteModelJudgeReportRepository(string databasePath)
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

    public async Task<ModelJudgeReport?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT document_json FROM evaluation_model_judgements WHERE id=$id AND tenant_id=$tenantId;";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        command.Parameters.AddWithValue("$tenantId", tenantId);
        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return value is string json ? Read(json) : null;
    }

    public async Task<ModelJudgeReport?> GetByConfigurationAsync(
        Guid batchId,
        string tenantId,
        string configurationSha256,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT document_json FROM evaluation_model_judgements
            WHERE batch_id=$batchId AND tenant_id=$tenantId
              AND configuration_sha256=$configurationSha256 LIMIT 1;
            """;
        command.Parameters.AddWithValue("$batchId", batchId.ToString("D"));
        command.Parameters.AddWithValue("$tenantId", tenantId);
        command.Parameters.AddWithValue("$configurationSha256", configurationSha256);
        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return value is string json ? Read(json) : null;
    }

    public async Task<IReadOnlyList<ModelJudgeReport>> ListAsync(
        Guid batchId,
        string tenantId,
        int take,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT document_json FROM evaluation_model_judgements
            WHERE batch_id=$batchId AND tenant_id=$tenantId
            ORDER BY started_at_utc DESC, id DESC LIMIT $take;
            """;
        command.Parameters.AddWithValue("$batchId", batchId.ToString("D"));
        command.Parameters.AddWithValue("$tenantId", tenantId);
        command.Parameters.AddWithValue("$take", Math.Clamp(take, 1, 50));
        var values = new List<ModelJudgeReport>();
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(Read(reader.GetString(0)));
        }

        return ModelJudgeContractCloner.ReadOnly(values);
    }

    public async Task<bool> TryCreateAsync(
        ModelJudgeReport value,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT OR IGNORE INTO evaluation_model_judgements
                (id, tenant_id, batch_id, configuration_sha256, started_at_utc, document_json)
            VALUES
                ($id, $tenantId, $batchId, $configurationSha256, $started, $json);
            """;
        command.Parameters.AddWithValue("$id", value.Id.ToString("D"));
        command.Parameters.AddWithValue("$tenantId", value.TenantId);
        command.Parameters.AddWithValue("$batchId", value.BatchId.ToString("D"));
        command.Parameters.AddWithValue("$configurationSha256", value.ConfigurationSha256);
        command.Parameters.AddWithValue("$started", value.StartedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(value, JsonOptions));
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
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
            CREATE TABLE IF NOT EXISTS evaluation_model_judgements
            (
                id                    TEXT NOT NULL PRIMARY KEY,
                tenant_id             TEXT NOT NULL,
                batch_id              TEXT NOT NULL,
                configuration_sha256  TEXT NOT NULL,
                started_at_utc        TEXT NOT NULL,
                document_json         TEXT NOT NULL CHECK (json_valid(document_json)),
                UNIQUE (tenant_id, batch_id, configuration_sha256)
            ) WITHOUT ROWID;
            CREATE INDEX IF NOT EXISTS ix_evaluation_model_judgements_batch_started
                ON evaluation_model_judgements (tenant_id, batch_id, started_at_utc DESC);
            """;
        command.ExecuteNonQuery();
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static ModelJudgeReport Read(string json) =>
        ModelJudgeContractCloner.Clone(
            JsonSerializer.Deserialize<ModelJudgeReport>(json, JsonOptions)
            ?? throw new InvalidDataException("The SQLite model judge document is empty."));
}
