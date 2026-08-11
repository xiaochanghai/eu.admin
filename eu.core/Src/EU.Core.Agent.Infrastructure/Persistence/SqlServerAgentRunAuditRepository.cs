using System.Text.Json;
using System.Text.Json.Serialization;
using EU.Core.Agent.Application.Runtime;
using Microsoft.Data.SqlClient;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqlServerAgentRunAuditRepository : IAgentRunAuditRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _connectionString;

    public SqlServerAgentRunAuditRepository(string connectionString)
    {
        _connectionString = SqlServerAgentConnection.Validate(connectionString);
    }

    public async Task SaveAsync(
        AgentRunAuditRecord record,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;
            UPDATE AgAgentRunAudit WITH (UPDLOCK, HOLDLOCK)
            SET Status = @Status, DocumentJson = @json
            WHERE RunId = @runId
              AND AgentId = @agentId
              AND StartedAtUtc = @startedAt;
            IF @@ROWCOUNT = 0 AND NOT EXISTS
                (SELECT 1 FROM AgAgentRunAudit WHERE RunId = @runId)
            BEGIN
                INSERT INTO AgAgentRunAudit
                    (RunId, AgentId, StartedAtUtc, Status, DocumentJson)
                VALUES
                    (@runId, @agentId, @startedAt, @Status, @json);
            END;
            COMMIT TRANSACTION;
            """;
        command.Parameters.AddWithValue("@runId", record.RunId.ToString("D"));
        command.Parameters.AddWithValue("@agentId", record.AgentId.ToString("D"));
        command.Parameters.AddWithValue(
            "@startedAt",
            record.StartedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("@Status", record.Status.ToString());
        command.Parameters.AddWithValue(
            "@json",
            JsonSerializer.Serialize(record, SerializerOptions));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AgentRunAuditRecord>> ListAsync(
        Guid agentId,
        int take,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT DocumentJson
            FROM AgAgentRunAudit
            WHERE AgentId = @agentId
            ORDER BY StartedAtUtc DESC, RunId DESC
            OFFSET 0 ROWS FETCH NEXT @take ROWS ONLY;
            """;
        command.Parameters.AddWithValue("@agentId", agentId.ToString("D"));
        command.Parameters.AddWithValue("@take", Math.Clamp(take, 1, 100));
        var values = new List<AgentRunAuditRecord>();
        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            AgentRunAuditRecord value =
                JsonSerializer.Deserialize<AgentRunAuditRecord>(
                    reader.GetString(0),
                    SerializerOptions) ??
                throw new InvalidDataException("The SQL Server Agent run audit is empty.");
            values.Add(AgentRunContractCloner.Clone(value));
        }

        return values;
    }



    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        return await SqlServerAgentConnection.OpenAsync(_connectionString, cancellationToken);
    }
}
