using System.Text.Json;
using EU.Core.Agent.Application.Evaluation;
using Microsoft.Data.Sqlite;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqliteEvaluationSuiteRepository : IEvaluationSuiteRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _connectionString;

    public SqliteEvaluationSuiteRepository(string databasePath)
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

    public async Task<EvaluationSuiteDefinition?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT document_json FROM evaluation_suites WHERE id=$id AND tenant_id=$tenantId;";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        command.Parameters.AddWithValue("$tenantId", tenantId);
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is string json ? Read(json) : null;
    }

    public async Task<IReadOnlyList<EvaluationSuiteDefinition>> ListAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT document_json FROM evaluation_suites WHERE tenant_id=$tenantId ORDER BY code;";
        command.Parameters.AddWithValue("$tenantId", tenantId);
        var values = new List<EvaluationSuiteDefinition>();
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(Read(reader.GetString(0)));
        }

        return EvaluationSuiteContractCloner.ReadOnly(values);
    }

    public async Task<bool> TryCreateAsync(
        EvaluationSuiteDefinition value,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT OR IGNORE INTO evaluation_suites
                (id, tenant_id, code, logical_revision, document_json)
            VALUES
                ($id, $tenantId, $code, $revision, $json);
            """;
        Add(command, value);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> TryReplaceAsync(
        EvaluationSuiteDefinition value,
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
            UPDATE evaluation_suites
            SET logical_revision=$revision, document_json=$json
            WHERE id=$id AND tenant_id=$tenantId AND code=$code
              AND logical_revision=$expected;
            """;
        Add(command, value);
        command.Parameters.AddWithValue("$expected", expectedLogicalRevision);
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
            CREATE TABLE IF NOT EXISTS evaluation_suites
            (
                id               TEXT NOT NULL PRIMARY KEY,
                tenant_id        TEXT NOT NULL,
                code             TEXT NOT NULL,
                logical_revision INTEGER NOT NULL CHECK (logical_revision >= 0),
                document_json    TEXT NOT NULL CHECK (json_valid(document_json)),
                UNIQUE (tenant_id, code)
            ) WITHOUT ROWID;
            CREATE INDEX IF NOT EXISTS ix_evaluation_suites_tenant_code
                ON evaluation_suites (tenant_id, code);
            """;
        command.ExecuteNonQuery();
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static void Add(SqliteCommand command, EvaluationSuiteDefinition value)
    {
        command.Parameters.AddWithValue("$id", value.Id.ToString("D"));
        command.Parameters.AddWithValue("$tenantId", value.TenantId);
        command.Parameters.AddWithValue("$code", value.Code);
        command.Parameters.AddWithValue("$revision", value.LogicalRevision);
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(value, JsonOptions));
    }

    private static EvaluationSuiteDefinition Read(string json) =>
        EvaluationSuiteContractCloner.Clone(
            JsonSerializer.Deserialize<EvaluationSuiteDefinition>(json, JsonOptions)
            ?? throw new InvalidDataException("The SQLite evaluation suite document is empty."));
}
