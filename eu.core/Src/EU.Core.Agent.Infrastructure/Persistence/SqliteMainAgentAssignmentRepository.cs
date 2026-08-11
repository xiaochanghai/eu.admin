using System.Globalization;
using EU.Core.Agent.Application.MainAgent;
using Microsoft.Data.Sqlite;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqliteMainAgentAssignmentRepository : IMainAgentAssignmentRepository
{
    private const string AssignmentKey = "platform-main-agent";

    private readonly string _connectionString;

    public SqliteMainAgentAssignmentRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        string fullPath = Path.GetFullPath(databasePath);
        string? directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("The SQLite database path must have a parent directory.", nameof(databasePath));
        }

        Directory.CreateDirectory(directory);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = false,
            DefaultTimeout = 5
        }.ToString();
        EnsureCreated();
    }

    public async Task<MainAgentAssignment?> GetAsync(CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenConnectionAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT agent_id, agent_version_id, logical_revision, updated_at_utc
            FROM main_agent_assignment
            WHERE assignment_key = $assignmentKey;
            """;
        command.Parameters.AddWithValue("$assignmentKey", AssignmentKey);
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
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

        await using SqliteConnection connection = await OpenConnectionAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO main_agent_assignment AS assignment
                (assignment_key, agent_id, agent_version_id, logical_revision, updated_at_utc)
            VALUES
                ($assignmentKey, $agentId, $agentVersionId, $logicalRevision, $updatedAtUtc)
            ON CONFLICT(assignment_key) DO UPDATE SET
                agent_id = excluded.agent_id,
                agent_version_id = excluded.agent_version_id,
                logical_revision = excluded.logical_revision,
                updated_at_utc = excluded.updated_at_utc
            WHERE $expectedLogicalRevision IS NOT NULL
              AND assignment.logical_revision = $expectedLogicalRevision;
            """;
        command.Parameters.AddWithValue("$assignmentKey", AssignmentKey);
        command.Parameters.AddWithValue("$agentId", value.AgentId.ToString("D"));
        command.Parameters.AddWithValue("$agentVersionId", value.AgentVersionId.ToString("D"));
        command.Parameters.AddWithValue("$logicalRevision", value.LogicalRevision);
        command.Parameters.AddWithValue("$updatedAtUtc", value.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$expectedLogicalRevision", (object?)expectedLogicalRevision ?? DBNull.Value);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private void EnsureCreated()
    {
        using SqliteConnection connection = new(_connectionString);
        connection.Open();
        SqliteMainAgentAssignmentSchema.EnsureCreated(connection);
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
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
