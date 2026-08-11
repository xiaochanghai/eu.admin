using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using EU.Core.Agent.Application.Agents;
using Microsoft.Data.SqlClient;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqlServerAgentRepository : IAgentRepository
{
private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _connectionString;

    public SqlServerAgentRepository(string connectionString)
    {
        _connectionString = SqlServerAgentConnection.Validate(connectionString);
    }

    public async Task<AgentDefinition?> GetByIdAsync(
        Guid Id,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenConnectionAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Code, LogicalRevision, DocumentJson
            FROM AgAgentDefinition
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", Id.ToString("D"));

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadDefinition(reader) : null;
    }

    public async Task<AgentDefinition?> GetByCodeAsync(
        string normalizedCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedCode);

        await using SqlConnection connection = await OpenConnectionAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Code, LogicalRevision, DocumentJson
            FROM AgAgentDefinition
            WHERE Code = @Code;
            """;
        command.Parameters.AddWithValue("@Code", normalizedCode);

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadDefinition(reader) : null;
    }

    public async Task<IReadOnlyList<AgentDefinition>> ListAsync(
        AgentDefinitionQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await using SqlConnection connection = await OpenConnectionAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Code, LogicalRevision, DocumentJson
            FROM AgAgentDefinition
            ORDER BY Code, Id;
            """;

        var definitions = new List<AgentDefinition>();
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            AgentDefinition definition = ReadDefinition(reader);
            if (query.RuntimeStatus is null &&
                definition.RuntimeStatus is AgentRuntimeStatus.Archived)
            {
                continue;
            }

            if (query.RuntimeStatus is AgentRuntimeStatus Status &&
                definition.RuntimeStatus != Status)
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

        await using SqlConnection connection = await OpenConnectionAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO AgAgentDefinition
                (Id, Code, LogicalRevision, DocumentJson)
            SELECT @Id, @Code, @logicalRevision, @documentJson
            WHERE NOT EXISTS
            (
                SELECT 1 FROM AgAgentDefinition WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @Id OR Code = @Code
            );
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

        await using SqlConnection connection = await OpenConnectionAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE AgAgentDefinition
            SET LogicalRevision = @logicalRevision,
                DocumentJson = @documentJson
            WHERE Id = @Id
              AND Code = @Code
              AND LogicalRevision = @expectedLogicalRevision;
            """;
        AddDefinitionParameters(command, definition);
        command.Parameters.AddWithValue("@expectedLogicalRevision", expectedLogicalRevision);

        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }



    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        return await SqlServerAgentConnection.OpenAsync(_connectionString, cancellationToken);
    }

    private static void AddDefinitionParameters(
        SqlCommand command,
        AgentDefinition definition)
    {
        command.Parameters.AddWithValue("@Id", definition.Id.ToString("D"));
        command.Parameters.AddWithValue("@Code", definition.Code);
        command.Parameters.AddWithValue("@logicalRevision", definition.LogicalRevision);
        command.Parameters.AddWithValue(
            "@documentJson",
            JsonSerializer.Serialize(definition, SerializerOptions));
    }

    private static AgentDefinition ReadDefinition(SqlDataReader reader)
    {
        Guid Id = Guid.Parse(reader.GetString(0));
        string Code = reader.GetString(1);
        long logicalRevision = reader.GetInt64(2);
        string documentJson = reader.GetString(3);
        AgentDefinition definition = JsonSerializer.Deserialize<AgentDefinition>(
            documentJson,
            SerializerOptions) ??
            throw new InvalidDataException("The SQL Server Agent document is empty.");

        if (definition.Id != Id ||
            !string.Equals(definition.Code, Code, StringComparison.Ordinal) ||
            definition.LogicalRevision != logicalRevision)
        {
            throw new InvalidDataException("The SQL Server Agent index columns do not match the stored document.");
        }

        return AgentContractCloner.Clone(definition);
    }
}
