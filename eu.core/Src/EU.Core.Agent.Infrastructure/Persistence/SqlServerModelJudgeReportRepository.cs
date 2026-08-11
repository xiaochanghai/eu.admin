using System.Text.Json;
using EU.Core.Agent.Application.Evaluation;
using Microsoft.Data.SqlClient;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqlServerModelJudgeReportRepository : IModelJudgeReportRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _connectionString;

    public SqlServerModelJudgeReportRepository(string connectionString)
    {
        _connectionString = SqlServerAgentConnection.Validate(connectionString);
    }

    public async Task<ModelJudgeReport?> GetAsync(
        Guid Id,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT DocumentJson FROM AgEvaluationModelJudgement WHERE Id=@Id AND TenantId=@tenantId;";
        command.Parameters.AddWithValue("@Id", Id.ToString("D"));
        command.Parameters.AddWithValue("@tenantId", tenantId);
        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return value is string json ? Read(json) : null;
    }

    public async Task<ModelJudgeReport?> GetByConfigurationAsync(
        Guid batchId,
        string tenantId,
        string configurationSha256,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT TOP (1) DocumentJson FROM AgEvaluationModelJudgement
            WHERE BatchId=@batchId AND TenantId=@tenantId
              AND ConfigurationSha256=@configurationSha256;
            """;
        command.Parameters.AddWithValue("@batchId", batchId.ToString("D"));
        command.Parameters.AddWithValue("@tenantId", tenantId);
        command.Parameters.AddWithValue("@configurationSha256", configurationSha256);
        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return value is string json ? Read(json) : null;
    }

    public async Task<IReadOnlyList<ModelJudgeReport>> ListAsync(
        Guid batchId,
        string tenantId,
        int take,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT DocumentJson FROM AgEvaluationModelJudgement
            WHERE BatchId=@batchId AND TenantId=@tenantId
            ORDER BY StartedAtUtc DESC, Id DESC
            OFFSET 0 ROWS FETCH NEXT @take ROWS ONLY;
            """;
        command.Parameters.AddWithValue("@batchId", batchId.ToString("D"));
        command.Parameters.AddWithValue("@tenantId", tenantId);
        command.Parameters.AddWithValue("@take", Math.Clamp(take, 1, 50));
        var values = new List<ModelJudgeReport>();
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(Read(reader.GetString(0)));
        }

        return ModelJudgeContractCloner.ReadOnly(values);
    }

    public async Task<bool> TryCreateAsync(
        ModelJudgeReport value,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO AgEvaluationModelJudgement
                (Id, TenantId, BatchId, ConfigurationSha256, StartedAtUtc, DocumentJson)
            SELECT @Id, @tenantId, @batchId, @configurationSha256, @started, @json
            WHERE NOT EXISTS
            (
                SELECT 1 FROM AgEvaluationModelJudgement WITH (UPDLOCK, HOLDLOCK)
                WHERE Id=@Id OR
                      (TenantId=@tenantId AND BatchId=@batchId
                       AND ConfigurationSha256=@configurationSha256)
            );
            """;
        command.Parameters.AddWithValue("@Id", value.Id.ToString("D"));
        command.Parameters.AddWithValue("@tenantId", value.TenantId);
        command.Parameters.AddWithValue("@batchId", value.BatchId.ToString("D"));
        command.Parameters.AddWithValue("@configurationSha256", value.ConfigurationSha256);
        command.Parameters.AddWithValue("@started", value.StartedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("@json", JsonSerializer.Serialize(value, JsonOptions));
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }



    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        return await SqlServerAgentConnection.OpenAsync(_connectionString, cancellationToken);
    }

    private static ModelJudgeReport Read(string json) =>
        ModelJudgeContractCloner.Clone(
            JsonSerializer.Deserialize<ModelJudgeReport>(json, JsonOptions)
            ?? throw new InvalidDataException("The SQL Server model judge document is empty."));
}
