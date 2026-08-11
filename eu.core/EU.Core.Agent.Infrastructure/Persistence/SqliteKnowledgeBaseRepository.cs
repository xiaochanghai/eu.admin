using System.Text.Json;
using EU.Core.Agent.Application.Knowledge;
using Microsoft.Data.Sqlite;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqliteKnowledgeBaseRepository :
    IKnowledgeBaseRepository,
    IPublishedKnowledgeCatalog,
    IKnowledgeRetriever
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    private readonly string _connectionString;

    public SqliteKnowledgeBaseRepository(string databasePath)
    {
        string fullPath = Path.GetFullPath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath, Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared, Pooling = false, DefaultTimeout = 5
        }.ToString();
        EnsureCreated();
    }

    public async Task<KnowledgeBaseDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        (await ReadAllAsync(cancellationToken)).FirstOrDefault(value => value.Id == id);

    public async Task<KnowledgeBaseDefinition?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        (await ReadAllAsync(cancellationToken)).FirstOrDefault(value =>
            string.Equals(value.Code, code, StringComparison.Ordinal));

    public async Task<IReadOnlyList<KnowledgeBaseDefinition>> ListAsync(
        KnowledgeBaseQuery query,
        CancellationToken cancellationToken = default) =>
        KnowledgeContractCloner.ReadOnly((await ReadAllAsync(cancellationToken))
            .Where(value => query.Status.HasValue
                ? value.Status == query.Status.Value
                : value.Status is not KnowledgeBaseStatus.Archived)
            .OrderBy(value => value.Code, StringComparer.Ordinal));

    public async Task<bool> TryCreateAsync(KnowledgeBaseDefinition value, CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR IGNORE INTO knowledge_base_definitions (id, code, logical_revision, document_json)
            VALUES ($id, $code, $revision, $json);
            """;
        Add(command, value);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> TryReplaceAsync(
        KnowledgeBaseDefinition value,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE knowledge_base_definitions SET logical_revision = $revision, document_json = $json
            WHERE id = $id AND code = $code AND logical_revision = $expected;
            """;
        Add(command, value);
        command.Parameters.AddWithValue("$expected", expectedLogicalRevision);
        return value.LogicalRevision == expectedLogicalRevision + 1 &&
               await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    async Task<IReadOnlyList<PublishedKnowledgeReference>> IPublishedKnowledgeCatalog.ListAsync(
        CancellationToken cancellationToken) =>
        KnowledgeContractCloner.ReadOnly((await ReadAllAsync(cancellationToken))
            .Where(value => value.Status == KnowledgeBaseStatus.Enabled && value.Chunks.Count > 0)
            .Select(value => new PublishedKnowledgeReference(
                value.Id, value.Code, value.Name, value.LogicalRevision)));

    public async Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        IReadOnlyList<Guid> knowledgeBaseIds,
        string query,
        int take,
        CancellationToken cancellationToken = default)
    {
        var memory = new InMemoryKnowledgeBaseRepository();
        memory.Load(await ReadAllAsync(cancellationToken));
        return await memory.SearchAsync(knowledgeBaseIds, query, take, cancellationToken);
    }

    private async Task<List<KnowledgeBaseDefinition>> ReadAllAsync(CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT document_json FROM knowledge_base_definitions ORDER BY code;";
        var values = new List<KnowledgeBaseDefinition>();
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(KnowledgeContractCloner.Clone(
                JsonSerializer.Deserialize<KnowledgeBaseDefinition>(reader.GetString(0), JsonOptions) ??
                throw new InvalidDataException("The SQLite knowledge document is empty.")));
        }
        return values;
    }

    private void EnsureCreated()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode = WAL;
            PRAGMA busy_timeout = 5000;
            CREATE TABLE IF NOT EXISTS knowledge_base_definitions
            (
                id TEXT NOT NULL PRIMARY KEY,
                code TEXT NOT NULL UNIQUE COLLATE BINARY,
                logical_revision INTEGER NOT NULL CHECK (logical_revision >= 0),
                document_json TEXT NOT NULL CHECK (json_valid(document_json))
            ) WITHOUT ROWID;
            """;
        command.ExecuteNonQuery();
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static void Add(SqliteCommand command, KnowledgeBaseDefinition value)
    {
        command.Parameters.AddWithValue("$id", value.Id.ToString("D"));
        command.Parameters.AddWithValue("$code", value.Code);
        command.Parameters.AddWithValue("$revision", value.LogicalRevision);
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(value, JsonOptions));
    }
}
