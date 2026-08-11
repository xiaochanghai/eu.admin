using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using EU.Core.Agent.Application.Agents;
using Microsoft.Data.Sqlite;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqliteAgentRepository : IAgentRepository
{
    private const string CreateSchemaSql =
        """
        CREATE TABLE IF NOT EXISTS agent_definitions
        (
            id               TEXT    NOT NULL PRIMARY KEY,
            code             TEXT    NOT NULL UNIQUE COLLATE BINARY,
            logical_revision INTEGER NOT NULL CHECK (logical_revision >= 0),
            document_json    TEXT    NOT NULL CHECK (json_valid(document_json))
        ) WITHOUT ROWID;
        """;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _connectionString;

    public SqliteAgentRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        string fullPath = Path.GetFullPath(databasePath);
        string? directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("The SQLite database path must have a parent directory.", nameof(databasePath));
        }

        Directory.CreateDirectory(directory);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = false,
            DefaultTimeout = 5,
            ForeignKeys = true
        }.ToString();

        EnsureCreated();
    }

    public async Task<AgentDefinition?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenConnectionAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, code, logical_revision, document_json
            FROM agent_definitions
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id.ToString("D"));

        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadDefinition(reader) : null;
    }

    public async Task<AgentDefinition?> GetByCodeAsync(
        string normalizedCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedCode);

        await using SqliteConnection connection = await OpenConnectionAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, code, logical_revision, document_json
            FROM agent_definitions
            WHERE code = $code;
            """;
        command.Parameters.AddWithValue("$code", normalizedCode);

        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadDefinition(reader) : null;
    }

    public async Task<IReadOnlyList<AgentDefinition>> ListAsync(
        AgentDefinitionQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await using SqliteConnection connection = await OpenConnectionAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, code, logical_revision, document_json
            FROM agent_definitions
            ORDER BY code, id;
            """;

        var definitions = new List<AgentDefinition>();
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            AgentDefinition definition = ReadDefinition(reader);
            if (query.RuntimeStatus is null &&
                definition.RuntimeStatus is AgentRuntimeStatus.Archived)
            {
                continue;
            }

            if (query.RuntimeStatus is AgentRuntimeStatus status &&
                definition.RuntimeStatus != status)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string search = query.Search.Trim();
                if (!definition.Code.Contains(search, StringComparison.OrdinalIgnoreCase) &&
                    !definition.Name.Contains(search, StringComparison.OrdinalIgnoreCase) &&
                    !definition.Description.Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            definitions.Add(definition);
        }

        return new ReadOnlyCollection<AgentDefinition>(definitions);
    }

    public async Task<bool> TryCreateAsync(
        AgentDefinition definition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        await using SqliteConnection connection = await OpenConnectionAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT OR IGNORE INTO agent_definitions
                (id, code, logical_revision, document_json)
            VALUES
                ($id, $code, $logicalRevision, $documentJson);
            """;
        AddDefinitionParameters(command, definition);

        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> TryReplaceAsync(
        AgentDefinition definition,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (expectedLogicalRevision == long.MaxValue ||
            definition.LogicalRevision != expectedLogicalRevision + 1)
        {
            return false;
        }

        await using SqliteConnection connection = await OpenConnectionAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE agent_definitions
            SET logical_revision = $logicalRevision,
                document_json = $documentJson
            WHERE id = $id
              AND code = $code
              AND logical_revision = $expectedLogicalRevision;
            """;
        AddDefinitionParameters(command, definition);
        command.Parameters.AddWithValue("$expectedLogicalRevision", expectedLogicalRevision);

        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private void EnsureCreated()
    {
        using SqliteConnection connection = new(_connectionString);
        connection.Open();

        using SqliteCommand pragmas = connection.CreateCommand();
        pragmas.CommandText =
            """
            PRAGMA journal_mode = WAL;
            PRAGMA synchronous = NORMAL;
            PRAGMA busy_timeout = 5000;
            """;
        pragmas.ExecuteNonQuery();

        using SqliteCommand schema = connection.CreateCommand();
        schema.CommandText = CreateSchemaSql;
        schema.ExecuteNonQuery();
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "PRAGMA busy_timeout = 5000;";
            await command.ExecuteNonQueryAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private static void AddDefinitionParameters(
        SqliteCommand command,
        AgentDefinition definition)
    {
        command.Parameters.AddWithValue("$id", definition.Id.ToString("D"));
        command.Parameters.AddWithValue("$code", definition.Code);
        command.Parameters.AddWithValue("$logicalRevision", definition.LogicalRevision);
        command.Parameters.AddWithValue(
            "$documentJson",
            JsonSerializer.Serialize(definition, SerializerOptions));
    }

    private static AgentDefinition ReadDefinition(SqliteDataReader reader)
    {
        Guid id = Guid.Parse(reader.GetString(0));
        string code = reader.GetString(1);
        long logicalRevision = reader.GetInt64(2);
        string documentJson = reader.GetString(3);
        AgentDefinition definition = JsonSerializer.Deserialize<AgentDefinition>(
            documentJson,
            SerializerOptions) ??
            throw new InvalidDataException("The SQLite Agent document is empty.");

        if (definition.Id != id ||
            !string.Equals(definition.Code, code, StringComparison.Ordinal) ||
            definition.LogicalRevision != logicalRevision)
        {
            throw new InvalidDataException("The SQLite Agent index columns do not match the stored document.");
        }

        return AgentContractCloner.Clone(definition);
    }
}
