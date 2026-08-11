using System.Text.Json;
using EU.Core.Agent.Application.Knowledge;
using Microsoft.Data.SqlClient;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqlServerKnowledgeBaseRepository :
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

    public SqlServerKnowledgeBaseRepository(string connectionString)
    {
        _connectionString = SqlServerAgentConnection.Validate(connectionString);
    }

    public async Task<KnowledgeBaseDefinition?> GetByIdAsync(Guid Id, CancellationToken cancellationToken = default) =>
        (await ReadAllAsync(cancellationToken)).FirstOrDefault(value => value.Id == Id);

    public async Task<KnowledgeBaseDefinition?> GetByCodeAsync(string Code, CancellationToken cancellationToken = default) =>
        (await ReadAllAsync(cancellationToken)).FirstOrDefault(value =>
            string.Equals(value.Code, Code, StringComparison.Ordinal));

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
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO AgKnowledgeBaseDefinition (Id, Code, LogicalRevision, DocumentJson)
            SELECT @Id, @Code, @revision, @json
            WHERE NOT EXISTS
            (
                SELECT 1 FROM AgKnowledgeBaseDefinition WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @Id OR Code = @Code
            );
            """;
        Add(command, value);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> TryReplaceAsync(
        KnowledgeBaseDefinition value,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE AgKnowledgeBaseDefinition SET LogicalRevision = @revision, DocumentJson = @json
            WHERE Id = @Id AND Code = @Code AND LogicalRevision = @expected;
            """;
        Add(command, value);
        command.Parameters.AddWithValue("@expected", expectedLogicalRevision);
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
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT DocumentJson FROM AgKnowledgeBaseDefinition ORDER BY Code;";
        var values = new List<KnowledgeBaseDefinition>();
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(KnowledgeContractCloner.Clone(
                JsonSerializer.Deserialize<KnowledgeBaseDefinition>(reader.GetString(0), JsonOptions) ??
                throw new InvalidDataException("The SQL Server knowledge document is empty.")));
        }
        return values;
    }



    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        return await SqlServerAgentConnection.OpenAsync(_connectionString, cancellationToken);
    }

    private static void Add(SqlCommand command, KnowledgeBaseDefinition value)
    {
        command.Parameters.AddWithValue("@Id", value.Id.ToString("D"));
        command.Parameters.AddWithValue("@Code", value.Code);
        command.Parameters.AddWithValue("@revision", value.LogicalRevision);
        command.Parameters.AddWithValue("@json", JsonSerializer.Serialize(value, JsonOptions));
    }
}
