using System.Text.Json;
using EU.Core.Agent.Application.Abstractions.Auditing;
using Microsoft.Data.SqlClient;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqlServerAgentOperationAuditRepository
    : IAgentOperationAuditRepository
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly string _connectionString;

    public SqlServerAgentOperationAuditRepository(string connectionString)
    {
        _connectionString = SqlServerAgentConnection.Validate(connectionString);
    }

    public async Task SaveAsync(
        AgentOperationAuditRecord record,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;
            UPDATE AgAgentOperationAudit WITH (UPDLOCK, HOLDLOCK)
            SET Outcome = @Outcome, DocumentJson = @json
            WHERE AuditId = @Id
              AND TenantId = @tenantId
              AND OccurredAtUtc = @occurredAt
              AND Outcome = 'Started';
            IF @@ROWCOUNT = 0 AND NOT EXISTS
                (SELECT 1 FROM AgAgentOperationAudit WHERE AuditId = @Id)
            BEGIN
                INSERT INTO AgAgentOperationAudit
                    (AuditId, TenantId, OccurredAtUtc, Outcome, DocumentJson)
                VALUES
                    (@Id, @tenantId, @occurredAt, @Outcome, @json);
            END;
            COMMIT TRANSACTION;
            """;
        command.Parameters.AddWithValue("@Id", record.Id.ToString("D"));
        command.Parameters.AddWithValue("@tenantId", record.TenantId);
        command.Parameters.AddWithValue("@occurredAt", record.OccurredAtUtc.ToString("O"));
        command.Parameters.AddWithValue("@Outcome", record.Outcome);
        command.Parameters.AddWithValue(
            "@json",
            JsonSerializer.Serialize(record, SerializerOptions));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AgentOperationAuditRecord>> ListAsync(
        string tenantId,
        int take,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT DocumentJson
            FROM AgAgentOperationAudit
            WHERE TenantId = @tenantId
            ORDER BY OccurredAtUtc DESC, AuditId DESC
            OFFSET 0 ROWS FETCH NEXT @take ROWS ONLY;
            """;
        command.Parameters.AddWithValue("@tenantId", tenantId);
        command.Parameters.AddWithValue("@take", Math.Clamp(take, 1, 100));
        var records = new List<AgentOperationAuditRecord>();
        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(
                JsonSerializer.Deserialize<AgentOperationAuditRecord>(
                    reader.GetString(0),
                    SerializerOptions) ??
                throw new InvalidDataException(
                    "The SQL Server Agent operation audit is empty."));
        }

        return records;
    }



    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        return await SqlServerAgentConnection.OpenAsync(_connectionString, cancellationToken);
    }
}
