using System.Text.Json;
using System.Text.Json.Serialization;
using EU.Core.Agent.Application.Mcp;
using Microsoft.Data.SqlClient;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqlServerMcpServerRepository :
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

    public SqlServerMcpServerRepository(string connectionString)
    {
        _connectionString = SqlServerAgentConnection.Validate(connectionString);
    }

    public async Task<McpServerDefinition?> GetByIdAsync(
        Guid Id,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT Id, Code, LogicalRevision, DocumentJson FROM AgMcpServerDefinition WHERE Id = @Id;";
        command.Parameters.AddWithValue("@Id", Id.ToString("D"));
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    public async Task<IReadOnlyList<McpServerDefinition>> ListAsync(
        McpServerQuery query,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT Id, Code, LogicalRevision, DocumentJson FROM AgMcpServerDefinition ORDER BY Code, Id;";
        var values = new List<McpServerDefinition>();
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
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
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO AgMcpServerDefinition (Id, Code, LogicalRevision, DocumentJson)
            SELECT @Id, @Code, @revision, @json
            WHERE NOT EXISTS
            (
                SELECT 1 FROM AgMcpServerDefinition WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @Id OR Code = @Code
            );
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

        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE AgMcpServerDefinition
            SET LogicalRevision = @revision, DocumentJson = @json
            WHERE Id = @Id AND Code = @Code AND LogicalRevision = @expected;
            """;
        Add(command, definition);
        command.Parameters.AddWithValue("@expected", expectedLogicalRevision);
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
                .Select(Id => server.ToolVersions.Single(tool => tool.Id == Id))
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



    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        return await SqlServerAgentConnection.OpenAsync(_connectionString, cancellationToken);
    }

    private static void Add(SqlCommand command, McpServerDefinition definition)
    {
        command.Parameters.AddWithValue("@Id", definition.Id.ToString("D"));
        command.Parameters.AddWithValue("@Code", definition.Code);
        command.Parameters.AddWithValue("@revision", definition.LogicalRevision);
        command.Parameters.AddWithValue(
            "@json",
            JsonSerializer.Serialize(definition, SerializerOptions));
    }

    private static McpServerDefinition Read(SqlDataReader reader)
    {
        Guid Id = Guid.Parse(reader.GetString(0));
        string Code = reader.GetString(1);
        long revision = reader.GetInt64(2);
        McpServerDefinition definition =
            JsonSerializer.Deserialize<McpServerDefinition>(
                reader.GetString(3),
                SerializerOptions) ??
            throw new InvalidDataException("The SQL Server MCP Server document is empty.");
        if (definition.Id != Id ||
            !string.Equals(definition.Code, Code, StringComparison.Ordinal) ||
            definition.LogicalRevision != revision)
        {
            throw new InvalidDataException(
                "The SQL Server MCP Server index columns do not match the stored document.");
        }

        return McpContractCloner.Clone(definition);
    }
}
