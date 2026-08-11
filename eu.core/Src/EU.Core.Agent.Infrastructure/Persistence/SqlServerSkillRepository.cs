using System.Text.Json;
using EU.Core.Agent.Application.Skills;
using Microsoft.Data.SqlClient;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqlServerSkillRepository : ISkillRepository, IPublishedSkillVersionCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _connectionString;

    public SqlServerSkillRepository(string connectionString)
    {
        _connectionString = SqlServerAgentConnection.Validate(connectionString);
    }

    public async Task<SkillDefinition?> GetByIdAsync(Guid Id, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT Id, Code, DraftRevision, DocumentJson FROM AgSkillDefinition WHERE Id = @Id;";
        command.Parameters.AddWithValue("@Id", Id.ToString("D"));
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    public async Task<SkillDefinition?> GetByCodeAsync(string Code, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT Id, Code, DraftRevision, DocumentJson FROM AgSkillDefinition WHERE Code = @Code;";
        command.Parameters.AddWithValue("@Code", Code);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    public async Task<IReadOnlyList<SkillDefinition>> ListAsync(
        SkillQuery query,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT Id, Code, DraftRevision, DocumentJson FROM AgSkillDefinition ORDER BY Code, Id;";
        var values = new List<SkillDefinition>();
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            SkillDefinition value = Read(reader);
            if (query.Status.HasValue
                ? value.Status != query.Status.Value
                : value.Status is SkillStatus.Archived)
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
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO AgSkillDefinition (Id, Code, DraftRevision, DocumentJson)
            SELECT @Id, @Code, @revision, @json
            WHERE NOT EXISTS
            (
                SELECT 1 FROM AgSkillDefinition WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @Id OR Code = @Code
            );
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

        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE AgSkillDefinition
            SET DraftRevision = @revision, DocumentJson = @json
            WHERE Id = @Id AND Code = @Code AND DraftRevision = @expected;
            """;
        Add(command, definition);
        command.Parameters.AddWithValue("@expected", expectedDraftRevision);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> ExistsAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SkillDefinition> definitions = await ListAsync(
            new SkillQuery(Status: SkillStatus.Active), cancellationToken);
        return definitions.Any(definition =>
            definition.PublishedVersions.Any(version => version.Id == versionId));
    }

    async Task<IReadOnlyList<PublishedSkillReference>> IPublishedSkillVersionCatalog.ListAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SkillDefinition> definitions = await ListAsync(
            new SkillQuery(Status: SkillStatus.Active), cancellationToken);
        return SkillContractCloner.ReadOnly(definitions.SelectMany(definition =>
            definition.PublishedVersions.Select(version => new PublishedSkillReference(
                definition.Id,
                version.Id,
                definition.Code,
                definition.Name,
                version.Label,
                version.ManifestSha256))));
    }



    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        return await SqlServerAgentConnection.OpenAsync(_connectionString, cancellationToken);
    }

    private static void Add(SqlCommand command, SkillDefinition definition)
    {
        command.Parameters.AddWithValue("@Id", definition.Id.ToString("D"));
        command.Parameters.AddWithValue("@Code", definition.Code);
        command.Parameters.AddWithValue("@revision", definition.DraftRevision);
        command.Parameters.AddWithValue("@json", JsonSerializer.Serialize(definition, SerializerOptions));
    }

    private static SkillDefinition Read(SqlDataReader reader)
    {
        Guid Id = Guid.Parse(reader.GetString(0));
        string Code = reader.GetString(1);
        long revision = reader.GetInt64(2);
        SkillDefinition definition = JsonSerializer.Deserialize<SkillDefinition>(
            reader.GetString(3),
            SerializerOptions) ??
            throw new InvalidDataException("The SQL Server Skill document is empty.");
        if (definition.Id != Id ||
            !string.Equals(definition.Code, Code, StringComparison.Ordinal) ||
            definition.DraftRevision != revision)
        {
            throw new InvalidDataException("The SQL Server Skill index columns do not match the stored document.");
        }

        return SkillContractCloner.Clone(definition);
    }
}
