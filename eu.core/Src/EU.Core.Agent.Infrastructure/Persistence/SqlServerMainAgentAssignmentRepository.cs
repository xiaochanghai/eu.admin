using System.Globalization;
using EU.Core.Agent.Application.MainAgent;
using Microsoft.Data.SqlClient;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqlServerMainAgentAssignmentRepository : IMainAgentAssignmentRepository
{
    private const string AssignmentKey = "platform-main-agent";

    private readonly string _connectionString;

    public SqlServerMainAgentAssignmentRepository(string connectionString)
    {
        _connectionString = SqlServerAgentConnection.Validate(connectionString);
    }

    public async Task<MainAgentAssignment?> GetAsync(CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenConnectionAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT AgentId, AgentVersionId, LogicalRevision, UpdatedAtUtc
            FROM AgMainAgentAssignment
            WHERE AssignmentKey = @assignmentKey;
            """;
        command.Parameters.AddWithValue("@assignmentKey", AssignmentKey);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new MainAgentAssignment(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                reader.GetInt64(2),
                DateTimeOffset.Parse(reader.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind))
            : null;
    }

    public async Task<bool> TryReplaceAsync(
        MainAgentAssignment value,
        long? expectedLogicalRevision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (expectedLogicalRevision == long.MaxValue ||
            value.LogicalRevision != (expectedLogicalRevision is null ? 0 : expectedLogicalRevision.Value + 1))
        {
            return false;
        }

        await using SqlConnection connection = await OpenConnectionAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = expectedLogicalRevision is null
            ?
            """
            INSERT INTO AgMainAgentAssignment
                (AssignmentKey, AgentId, AgentVersionId, LogicalRevision, UpdatedAtUtc)
            SELECT @assignmentKey, @agentId, @agentVersionId, @logicalRevision, @updatedAtUtc
            WHERE NOT EXISTS
            (
                SELECT 1 FROM AgMainAgentAssignment WITH (UPDLOCK, HOLDLOCK)
                WHERE AssignmentKey = @assignmentKey
            );
            """
            :
            """
            UPDATE AgMainAgentAssignment WITH (UPDLOCK, HOLDLOCK)
            SET AgentId = @agentId,
                AgentVersionId = @agentVersionId,
                LogicalRevision = @logicalRevision,
                UpdatedAtUtc = @updatedAtUtc
            WHERE AssignmentKey = @assignmentKey
              AND LogicalRevision = @expectedLogicalRevision;
            """;
        command.Parameters.AddWithValue("@assignmentKey", AssignmentKey);
        command.Parameters.AddWithValue("@agentId", value.AgentId.ToString("D"));
        command.Parameters.AddWithValue("@agentVersionId", value.AgentVersionId.ToString("D"));
        command.Parameters.AddWithValue("@logicalRevision", value.LogicalRevision);
        command.Parameters.AddWithValue("@updatedAtUtc", value.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@expectedLogicalRevision", (object?)expectedLogicalRevision ?? DBNull.Value);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }



    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
