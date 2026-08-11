using System.Text.Json;
using EU.Core.Agent.Application.Evaluation;
using EU.Core.Agent.Application.UnifiedEntry;
using Microsoft.Data.SqlClient;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqlServerEvaluationBatchRepository :
    IEvaluationBatchRepository,
    IEvaluationBatchRecovery
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _connectionString;

    public SqlServerEvaluationBatchRepository(string connectionString)
    {
        _connectionString = SqlServerAgentConnection.Validate(connectionString);
    }

    public async Task<EvaluationBatchRecord?> GetAsync(
        Guid Id,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT DocumentJson FROM AgEvaluationBatch WHERE Id=@Id AND TenantId=@tenantId;";
        command.Parameters.AddWithValue("@Id", Id.ToString("D"));
        command.Parameters.AddWithValue("@tenantId", tenantId);
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is string json ? Read(json) : null;
    }

    public async Task<IReadOnlyList<EvaluationBatchRecord>> ListAsync(
        Guid suiteId,
        string tenantId,
        int take,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT DocumentJson FROM AgEvaluationBatch
            WHERE SuiteId=@suiteId AND TenantId=@tenantId
            ORDER BY StartedAtUtc DESC, Id DESC
            OFFSET 0 ROWS FETCH NEXT @take ROWS ONLY;
            """;
        command.Parameters.AddWithValue("@suiteId", suiteId.ToString("D"));
        command.Parameters.AddWithValue("@tenantId", tenantId);
        command.Parameters.AddWithValue("@take", Math.Clamp(take, 1, 100));
        var values = new List<EvaluationBatchRecord>();
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(Read(reader.GetString(0)));
        }

        return EvaluationBatchContractCloner.ReadOnly(values);
    }

    public async Task<bool> TryCreateAsync(
        EvaluationBatchRecord value,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO AgEvaluationBatch
                (Id, TenantId, SuiteId, SuiteVersionId, Status,
                 LogicalRevision, StartedAtUtc, DocumentJson)
            SELECT @Id, @tenantId, @suiteId, @suiteVersionId, @Status,
                   @revision, @started, @json
            WHERE NOT EXISTS
            (
                SELECT 1 FROM AgEvaluationBatch WITH (UPDLOCK, HOLDLOCK)
                WHERE Id=@Id
            );
            """;
        Add(command, value);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> TryReplaceAsync(
        EvaluationBatchRecord value,
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
            UPDATE AgEvaluationBatch
            SET Status=@Status, LogicalRevision=@revision, DocumentJson=@json
            WHERE Id=@Id AND TenantId=@tenantId AND SuiteId=@suiteId
              AND SuiteVersionId=@suiteVersionId
              AND Status=@running AND LogicalRevision=@expected;
            """;
        Add(command, value);
        command.Parameters.AddWithValue("@running", EvaluationBatchStatus.Running.ToString());
        command.Parameters.AddWithValue("@expected", expectedLogicalRevision);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<int> RecoverInterruptedAsync(
        DateTimeOffset recoveredAtUtc,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<EvaluationBatchRecord> running = await ListRunningAsync(cancellationToken);
        int recovered = 0;
        foreach (EvaluationBatchRecord value in running)
        {
            EvaluationCaseExecutionRecord[] cases = value.Cases.Select(item =>
                item.Status == EvaluationCaseExecutionStatus.Running
                    ? item with
                    {
                        Status = EvaluationCaseExecutionStatus.Failed,
                        ErrorCode = UnifiedEntryErrorCodes.HostInterrupted
                    }
                    : item with { }).ToArray();
            EvaluationBatchRecord updated = value with
            {
                Status = EvaluationBatchStatus.Failed,
                LogicalRevision = value.LogicalRevision + 1,
                FinishedAtUtc = recoveredAtUtc.ToUniversalTime(),
                Cases = EvaluationBatchContractCloner.CloneCases(cases),
                ErrorCode = UnifiedEntryErrorCodes.HostInterrupted
            };
            if (await TryReplaceAsync(updated, value.LogicalRevision, cancellationToken))
            {
                recovered++;
            }
        }

        return recovered;
    }

    private async Task<IReadOnlyList<EvaluationBatchRecord>> ListRunningAsync(
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT DocumentJson FROM AgEvaluationBatch WHERE Status=@Status;";
        command.Parameters.AddWithValue("@Status", EvaluationBatchStatus.Running.ToString());
        var values = new List<EvaluationBatchRecord>();
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(Read(reader.GetString(0)));
        }

        return values;
    }



    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        return await SqlServerAgentConnection.OpenAsync(_connectionString, cancellationToken);
    }

    private static void Add(SqlCommand command, EvaluationBatchRecord value)
    {
        command.Parameters.AddWithValue("@Id", value.Id.ToString("D"));
        command.Parameters.AddWithValue("@tenantId", value.TenantId);
        command.Parameters.AddWithValue("@suiteId", value.SuiteId.ToString("D"));
        command.Parameters.AddWithValue("@suiteVersionId", value.SuiteVersionId.ToString("D"));
        command.Parameters.AddWithValue("@Status", value.Status.ToString());
        command.Parameters.AddWithValue("@revision", value.LogicalRevision);
        command.Parameters.AddWithValue("@started", value.StartedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("@json", JsonSerializer.Serialize(value, JsonOptions));
    }

    private static EvaluationBatchRecord Read(string json) =>
        EvaluationBatchContractCloner.Clone(
            JsonSerializer.Deserialize<EvaluationBatchRecord>(json, JsonOptions)
            ?? throw new InvalidDataException("The SQL Server evaluation batch document is empty."));
}
