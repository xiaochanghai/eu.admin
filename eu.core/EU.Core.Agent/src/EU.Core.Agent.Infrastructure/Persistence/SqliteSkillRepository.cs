using System.Text.Json;
using EU.Core.Agent.Application.Skills;
using Microsoft.Data.Sqlite;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqliteSkillRepository : ISkillRepository, IPublishedSkillVersionCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _connectionString;

    public SqliteSkillRepository(string databasePath)
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

    public async Task<SkillDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT id, code, draft_revision, document_json FROM skill_definitions WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    public async Task<SkillDefinition?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT id, code, draft_revision, document_json FROM skill_definitions WHERE code = $code;";
        command.Parameters.AddWithValue("$code", code);
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    public async Task<IReadOnlyList<SkillDefinition>> ListAsync(
        SkillQuery query,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT id, code, draft_revision, document_json FROM skill_definitions ORDER BY code, id;";
        var values = new List<SkillDefinition>();
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            SkillDefinition value = Read(reader);
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string search = query.Search.Trim();
                if (!value.Code.Contains(search, StringComparison.OrdinalIgnoreCase) &&
                    !value.Name.Contains(search, StringComparison.OrdinalIgnoreCase) &&
                    !value.Description.Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            if (!string.IsNullOrWhiteSpace(query.Category) &&
                !string.Equals(value.Category, query.Category.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            values.Add(value);
        }

        return SkillContractCloner.ReadOnly(values);
    }

    public async Task<bool> TryCreateAsync(SkillDefinition definition, CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT OR IGNORE INTO skill_definitions (id, code, draft_revision, document_json)
            VALUES ($id, $code, $revision, $json);
            """;
        Add(command, definition);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> TryReplaceAsync(
        SkillDefinition definition,
        long expectedDraftRevision,
        CancellationToken cancellationToken = default)
    {
        if (expectedDraftRevision == long.MaxValue ||
            definition.DraftRevision != expectedDraftRevision + 1)
        {
            return false;
        }

        SkillDefinition? existing = await GetByIdAsync(definition.Id, cancellationToken);
        if (existing is null ||
            !string.Equals(existing.Code, definition.Code, StringComparison.Ordinal) ||
            !SkillContractCloner.PreservesPublishedHistory(existing, definition))
        {
            return false;
        }

        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE skill_definitions
            SET draft_revision = $revision, document_json = $json
            WHERE id = $id AND code = $code AND draft_revision = $expected;
            """;
        Add(command, definition);
        command.Parameters.AddWithValue("$expected", expectedDraftRevision);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> ExistsAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SkillDefinition> definitions = await ListAsync(new SkillQuery(), cancellationToken);
        return definitions.Any(definition =>
            definition.PublishedVersions.Any(version => version.Id == versionId));
    }

    async Task<IReadOnlyList<PublishedSkillReference>> IPublishedSkillVersionCatalog.ListAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SkillDefinition> definitions = await ListAsync(new SkillQuery(), cancellationToken);
        return SkillContractCloner.ReadOnly(definitions.SelectMany(definition =>
            definition.PublishedVersions.Select(version => new PublishedSkillReference(
                definition.Id,
                version.Id,
                definition.Code,
                definition.Name,
                version.Label,
                version.ManifestSha256))));
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
            CREATE TABLE IF NOT EXISTS skill_definitions
            (
                id             TEXT    NOT NULL PRIMARY KEY,
                code           TEXT    NOT NULL UNIQUE COLLATE BINARY,
                draft_revision INTEGER NOT NULL CHECK (draft_revision >= 0),
                document_json  TEXT    NOT NULL CHECK (json_valid(document_json))
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

    private static void Add(SqliteCommand command, SkillDefinition definition)
    {
        command.Parameters.AddWithValue("$id", definition.Id.ToString("D"));
        command.Parameters.AddWithValue("$code", definition.Code);
        command.Parameters.AddWithValue("$revision", definition.DraftRevision);
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(definition, SerializerOptions));
    }

    private static SkillDefinition Read(SqliteDataReader reader)
    {
        Guid id = Guid.Parse(reader.GetString(0));
        string code = reader.GetString(1);
        long revision = reader.GetInt64(2);
        SkillDefinition definition = JsonSerializer.Deserialize<SkillDefinition>(
            reader.GetString(3),
            SerializerOptions) ??
            throw new InvalidDataException("The SQLite Skill document is empty.");
        if (definition.Id != id ||
            !string.Equals(definition.Code, code, StringComparison.Ordinal) ||
            definition.DraftRevision != revision)
        {
            throw new InvalidDataException("The SQLite Skill index columns do not match the stored document.");
        }

        return SkillContractCloner.Clone(definition);
    }
}
