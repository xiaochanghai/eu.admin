using Microsoft.Data.Sqlite;

namespace EU.Core.Agent.Infrastructure.Persistence;

internal static class SqliteMainAgentAssignmentSchema
{
    private const string AssignmentKey = "platform-main-agent";
    private const string CreateSchemaSql =
        """
        CREATE TABLE IF NOT EXISTS main_agent_assignment
        (
            assignment_key   TEXT    NOT NULL PRIMARY KEY CHECK (assignment_key = 'platform-main-agent'),
            agent_id         TEXT    NOT NULL,
            agent_version_id TEXT    NOT NULL,
            logical_revision INTEGER NOT NULL CHECK (logical_revision >= 0),
            updated_at_utc   TEXT    NOT NULL
        ) WITHOUT ROWID;
        """;

    public static void EnsureCreated(SqliteConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        using (SqliteCommand busy = connection.CreateCommand())
        {
            busy.CommandText = "PRAGMA busy_timeout = 5000;";
            busy.ExecuteNonQuery();
        }

        using SqliteTransaction transaction =
            connection.BeginTransaction(deferred: false);
        try
        {
            EnsureCreated(connection, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public static void EnsureCreated(
        SqliteConnection connection,
        SqliteTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);
        using (SqliteCommand schema = connection.CreateCommand())
        {
            schema.Transaction = transaction;
            schema.CommandText = CreateSchemaSql;
            schema.ExecuteNonQuery();
        }

        if (!TableExists(connection, transaction, "main_agent_assignments"))
        {
            return;
        }

        AssignmentRow? legacy = ReadAssignment(
            connection,
            transaction,
            "main_agent_assignments");
        if (legacy is null)
        {
            RemoveLegacyTable(connection, transaction);
            return;
        }

        AssignmentRow? normalized = ReadAssignment(
            connection,
            transaction,
            "main_agent_assignment");
        if (normalized is null)
        {
            InsertAssignment(connection, transaction, legacy);
            normalized = ReadAssignment(
                connection,
                transaction,
                "main_agent_assignment");
        }

        if (normalized != legacy)
        {
            throw new InvalidDataException(
                "The legacy and normalized Main Agent assignments conflict; neither value was overwritten.");
        }

        RemoveLegacyTable(connection, transaction);
    }

    private static bool TableExists(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string table)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT 1
            FROM sqlite_master
            WHERE type = 'table' AND name = $table;
            """;
        command.Parameters.AddWithValue("$table", table);
        return command.ExecuteScalar() is not null;
    }

    private static AssignmentRow? ReadAssignment(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string table)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            $"""
            SELECT agent_id, agent_version_id, logical_revision, updated_at_utc
            FROM {table}
            WHERE assignment_key = $assignmentKey;
            """;
        command.Parameters.AddWithValue("$assignmentKey", AssignmentKey);
        using SqliteDataReader reader = command.ExecuteReader();
        return reader.Read()
            ? new AssignmentRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt64(2),
                reader.GetString(3))
            : null;
    }

    private static void InsertAssignment(
        SqliteConnection connection,
        SqliteTransaction transaction,
        AssignmentRow value)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO main_agent_assignment
                (assignment_key, agent_id, agent_version_id, logical_revision, updated_at_utc)
            VALUES
                ($assignmentKey, $agentId, $agentVersionId, $logicalRevision, $updatedAtUtc);
            """;
        command.Parameters.AddWithValue("$assignmentKey", AssignmentKey);
        command.Parameters.AddWithValue("$agentId", value.AgentId);
        command.Parameters.AddWithValue("$agentVersionId", value.AgentVersionId);
        command.Parameters.AddWithValue("$logicalRevision", value.LogicalRevision);
        command.Parameters.AddWithValue("$updatedAtUtc", value.UpdatedAtUtc);
        command.ExecuteNonQuery();
    }

    private static void RemoveLegacyTable(
        SqliteConnection connection,
        SqliteTransaction transaction)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DROP TABLE main_agent_assignments;";
        command.ExecuteNonQuery();
    }

    private sealed record AssignmentRow(
        string AgentId,
        string AgentVersionId,
        long LogicalRevision,
        string UpdatedAtUtc);
}
