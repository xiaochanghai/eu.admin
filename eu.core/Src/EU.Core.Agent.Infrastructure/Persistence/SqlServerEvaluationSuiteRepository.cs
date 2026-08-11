using System.Text.Json;
using EU.Core.Agent.Application.Evaluation;
using Microsoft.Data.SqlClient;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqlServerEvaluationSuiteRepository : IEvaluationSuiteRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _connectionString;

    public SqlServerEvaluationSuiteRepository(string connectionString)
    {
        _connectionString = SqlServerAgentConnection.Validate(connectionString);
    }

    public async Task<EvaluationSuiteDefinition?> GetAsync(
        Guid Id,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT DocumentJson FROM AgEvaluationSuite WHERE Id=@Id AND TenantId=@tenantId;";
        command.Parameters.AddWithValue("@Id", Id.ToString("D"));
        command.Parameters.AddWithValue("@tenantId", tenantId);
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is string json ? Read(json) : null;
    }

    public async Task<IReadOnlyList<EvaluationSuiteDefinition>> ListAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT DocumentJson FROM AgEvaluationSuite WHERE TenantId=@tenantId ORDER BY Code;";
        command.Parameters.AddWithValue("@tenantId", tenantId);
        var values = new List<EvaluationSuiteDefinition>();
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
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
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO AgEvaluationSuite
                (Id, TenantId, Code, LogicalRevision, DocumentJson)
            SELECT @Id, @tenantId, @Code, @revision, @json
            WHERE NOT EXISTS
            (
                SELECT 1 FROM AgEvaluationSuite WITH (UPDLOCK, HOLDLOCK)
                WHERE Id=@Id OR (TenantId=@tenantId AND Code=@Code)
            );
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

        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE AgEvaluationSuite
            SET LogicalRevision=@revision, DocumentJson=@json
            WHERE Id=@Id AND TenantId=@tenantId AND Code=@Code
              AND LogicalRevision=@expected;
            """;
        Add(command, value);
        command.Parameters.AddWithValue("@expected", expectedLogicalRevision);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }



    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        return await SqlServerAgentConnection.OpenAsync(_connectionString, cancellationToken);
    }

    private static void Add(SqlCommand command, EvaluationSuiteDefinition value)
    {
        command.Parameters.AddWithValue("@Id", value.Id.ToString("D"));
        command.Parameters.AddWithValue("@tenantId", value.TenantId);
        command.Parameters.AddWithValue("@Code", value.Code);
        command.Parameters.AddWithValue("@revision", value.LogicalRevision);
        command.Parameters.AddWithValue("@json", JsonSerializer.Serialize(value, JsonOptions));
    }

    private static EvaluationSuiteDefinition Read(string json) =>
        EvaluationSuiteContractCloner.Clone(
            JsonSerializer.Deserialize<EvaluationSuiteDefinition>(json, JsonOptions)
            ?? throw new InvalidDataException("The SQL Server evaluation suite document is empty."));
}
