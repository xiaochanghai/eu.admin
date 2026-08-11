using System.Text.Json;
using System.Text.Json.Serialization;
using EU.Core.Agent.Application.Mcp;
using Microsoft.Data.Sqlite;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqliteMcpServerRepository :
    IMcpServerRepository,
    IPublishedMcpToolCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _connectionString;

    public SqliteMcpServerRepository(string databasePath)
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

    public async Task<McpServerDefinition?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT id, code, logical_revision, document_json FROM mcp_server_definitions WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    public async Task<IReadOnlyList<McpServerDefinition>> ListAsync(
        McpServerQuery query,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT id, code, logical_revision, document_json FROM mcp_server_definitions ORDER BY code, id;";
        var values = new List<McpServerDefinition>();
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            McpServerDefinition value = Read(reader);
            if (query.Status is null && value.Status is McpServerStatus.Archived)
            {
                continue;
            }

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

            if (query.Status is not null && value.Status != query.Status)
            {
                continue;
            }

            values.Add(value);
        }

        return McpContractCloner.ReadOnly(values);
    }

    public async Task<bool> TryCreateAsync(
        McpServerDefinition definition,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT OR IGNORE INTO mcp_server_definitions (id, code, logical_revision, document_json)
            VALUES ($id, $code, $revision, $json);
            """;
        Add(command, definition);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> TryReplaceAsync(
        McpServerDefinition definition,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default)
    {
        if (expectedLogicalRevision == long.MaxValue ||
            definition.LogicalRevision != expectedLogicalRevision + 1)
        {
            return false;
        }

        McpServerDefinition? existing =
            await GetByIdAsync(definition.Id, cancellationToken);
        if (existing is null ||
            !string.Equals(existing.Code, definition.Code, StringComparison.Ordinal) ||
            !McpContractCloner.PreservesToolHistory(existing, definition))
        {
            return false;
        }

        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE mcp_server_definitions
            SET logical_revision = $revision, document_json = $json
            WHERE id = $id AND code = $code AND logical_revision = $expected;
            """;
        Add(command, definition);
        command.Parameters.AddWithValue("$expected", expectedLogicalRevision);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> ExistsAsync(
        Guid toolVersionId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<McpServerDefinition> definitions =
            await ListAsync(new McpServerQuery(), cancellationToken);
        return definitions.Any(server =>
            server.ToolVersions.Any(tool =>
                tool.Id == toolVersionId &&
                tool.Risk != McpToolRisk.Unknown));
    }

    public async Task<IReadOnlyList<PublishedMcpToolReference>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<McpServerDefinition> definitions =
            await ListAsync(new McpServerQuery(), cancellationToken);
        return McpContractCloner.ReadOnly(definitions
            .Where(server => server.Enabled)
            .OrderBy(server => server.Code, StringComparer.Ordinal)
            .SelectMany(server => server.CurrentToolVersionIds
                .Select(id => server.ToolVersions.Single(tool => tool.Id == id))
                .Where(tool => tool.Risk != McpToolRisk.Unknown)
                .Select(tool => new PublishedMcpToolReference(
                    server.Id,
                    server.Code,
                    server.Name,
                    tool.Id,
                    tool.Name,
                    tool.Description,
                    tool.InputSchemaJson,
                    tool.Risk,
                    tool.Sha256))));
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
            CREATE TABLE IF NOT EXISTS mcp_server_definitions
            (
                id               TEXT    NOT NULL PRIMARY KEY,
                code             TEXT    NOT NULL UNIQUE COLLATE BINARY,
                logical_revision INTEGER NOT NULL CHECK (logical_revision >= 0),
                document_json    TEXT    NOT NULL CHECK (json_valid(document_json))
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

    private static void Add(SqliteCommand command, McpServerDefinition definition)
    {
        command.Parameters.AddWithValue("$id", definition.Id.ToString("D"));
        command.Parameters.AddWithValue("$code", definition.Code);
        command.Parameters.AddWithValue("$revision", definition.LogicalRevision);
        command.Parameters.AddWithValue(
            "$json",
            JsonSerializer.Serialize(definition, SerializerOptions));
    }

    private static McpServerDefinition Read(SqliteDataReader reader)
    {
        Guid id = Guid.Parse(reader.GetString(0));
        string code = reader.GetString(1);
        long revision = reader.GetInt64(2);
        McpServerDefinition definition =
            JsonSerializer.Deserialize<McpServerDefinition>(
                reader.GetString(3),
                SerializerOptions) ??
            throw new InvalidDataException("The SQLite MCP Server document is empty.");
        if (definition.Id != id ||
            !string.Equals(definition.Code, code, StringComparison.Ordinal) ||
            definition.LogicalRevision != revision)
        {
            throw new InvalidDataException(
                "The SQLite MCP Server index columns do not match the stored document.");
        }

        return McpContractCloner.Clone(definition);
    }
}
