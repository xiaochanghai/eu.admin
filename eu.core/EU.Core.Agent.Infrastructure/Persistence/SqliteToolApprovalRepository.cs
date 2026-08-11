using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EU.Core.Agent.Application.Approvals;
using EU.Core.Agent.Application.Mcp;
using Microsoft.Data.Sqlite;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqliteToolApprovalRepository : IToolApprovalRepository
{
    private readonly string _connectionString;

    public SqliteToolApprovalRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        string fullPath = Path.GetFullPath(databasePath);
        string? directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException(
                "The SQLite database path must have a parent directory.",
                nameof(databasePath));
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

    public async Task<bool> TryCreateAsync(
        ToolApprovalRequestRecord request,
        string protectedResumePayload,
        CancellationToken cancellationToken = default)
    {
        ToolApprovalStateMachine.ValidateNew(request, protectedResumePayload);
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteTransaction transaction =
            (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using SqliteCommand requestCommand = connection.CreateCommand();
        requestCommand.Transaction = transaction;
        requestCommand.CommandText =
            """
            INSERT OR IGNORE INTO tool_approval_requests
            (
                id, tenant_id, requester_user_id, conversation_id, entry_run_id,
                agent_run_id, agent_version_id, mcp_server_id, tool_version_id, tool_name, risk,
                tool_schema_sha256, arguments_sha256, safe_arguments_summary_json,
                status, logical_revision, requested_at_utc, expires_at_utc,
                decision_user_id, decision_reason, decided_at_utc, claimed_at_utc,
                finished_at_utc, error_code
            )
            VALUES
            (
                $id, $tenantId, $requesterUserId, $conversationId, $entryRunId,
                $agentRunId, $agentVersionId, $mcpServerId, $toolVersionId, $toolName, $risk,
                $toolSchemaSha256, $argumentsSha256, $summary, $status, $revision,
                $requestedAtUtc, $expiresAtUtc, '', '', NULL, NULL, NULL, ''
            );
            """;
        AddImmutableParameters(requestCommand, request);
        AddStateParameters(requestCommand, request);
        if (await requestCommand.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await using SqliteCommand payloadCommand = connection.CreateCommand();
        payloadCommand.Transaction = transaction;
        payloadCommand.CommandText =
            """
            INSERT INTO tool_approval_payloads
                (approval_id, protected_payload, protected_payload_sha256)
            VALUES ($approvalId, $payload, $sha256);
            """;
        payloadCommand.Parameters.AddWithValue("$approvalId", request.Id.ToString("D"));
        payloadCommand.Parameters.AddWithValue("$payload", protectedResumePayload);
        payloadCommand.Parameters.AddWithValue(
            "$sha256",
            Sha256(protectedResumePayload));
        await payloadCommand.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<ToolApprovalRequestRecord?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        RequiredTenant(tenantId);
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columns} FROM tool_approval_requests WHERE id = $id AND tenant_id = $tenantId;";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        command.Parameters.AddWithValue("$tenantId", tenantId);
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    public async Task<IReadOnlyList<ToolApprovalRequestRecord>> ListAsync(
        ToolApprovalQuery query,
        CancellationToken cancellationToken = default)
    {
        ToolApprovalStateMachine.ValidateQuery(query);
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = query.Status is null
            ? $"SELECT {Columns} FROM tool_approval_requests WHERE tenant_id = $tenantId ORDER BY requested_at_utc DESC, id LIMIT $take;"
            : $"SELECT {Columns} FROM tool_approval_requests WHERE tenant_id = $tenantId AND status = $status ORDER BY requested_at_utc DESC, id LIMIT $take;";
        command.Parameters.AddWithValue("$tenantId", query.TenantId);
        command.Parameters.AddWithValue("$take", query.Take);
        if (query.Status is not null)
        {
            command.Parameters.AddWithValue("$status", (int)query.Status.Value);
        }

        var values = new List<ToolApprovalRequestRecord>();
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(Read(reader));
        }

        return new ReadOnlyCollection<ToolApprovalRequestRecord>(values);
    }

    public async Task<IReadOnlyList<ToolApprovalDecisionRecord>> ListDecisionsAsync(
        Guid approvalId,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        RequiredTenant(tenantId);
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, approval_id, tenant_id, from_status, to_status,
                   decision_user_id, decision_reason, decided_at_utc,
                   resulting_logical_revision
            FROM tool_approval_decisions
            WHERE approval_id = $approvalId AND tenant_id = $tenantId
            ORDER BY resulting_logical_revision, id;
            """;
        command.Parameters.AddWithValue("$approvalId", approvalId.ToString("D"));
        command.Parameters.AddWithValue("$tenantId", tenantId);

        var values = new List<ToolApprovalDecisionRecord>();
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(new ToolApprovalDecisionRecord(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                reader.GetString(2),
                (ToolApprovalStatus)reader.GetInt32(3),
                (ToolApprovalStatus)reader.GetInt32(4),
                reader.GetString(5),
                reader.GetString(6),
                Parse(reader.GetString(7)),
                reader.GetInt64(8)));
        }

        return new ReadOnlyCollection<ToolApprovalDecisionRecord>(values);
    }

    public async Task<bool> TryReplaceAsync(
        ToolApprovalRequestRecord replacement,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default)
    {
        ToolApprovalRequestRecord? existing = await GetAsync(
            replacement.Id,
            replacement.TenantId,
            cancellationToken);
        if (existing is null || existing.LogicalRevision != expectedLogicalRevision)
        {
            return false;
        }

        ToolApprovalStateMachine.ValidateReplacement(existing, replacement);
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteTransaction transaction =
            (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            UPDATE tool_approval_requests
            SET status = $status,
                logical_revision = $revision,
                decision_user_id = $decisionUserId,
                decision_reason = $decisionReason,
                decided_at_utc = $decidedAtUtc,
                claimed_at_utc = $claimedAtUtc,
                finished_at_utc = $finishedAtUtc,
                error_code = $errorCode
            WHERE id = $id
              AND tenant_id = $tenantId
              AND logical_revision = $expectedRevision
              AND status = $expectedStatus;
            """;
        command.Parameters.AddWithValue("$id", replacement.Id.ToString("D"));
        command.Parameters.AddWithValue("$tenantId", replacement.TenantId);
        command.Parameters.AddWithValue("$expectedRevision", expectedLogicalRevision);
        command.Parameters.AddWithValue("$expectedStatus", (int)existing.Status);
        AddStateParameters(command, replacement);
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        if (IsHumanDecision(existing.Status, replacement.Status))
        {
            await using SqliteCommand decision = connection.CreateCommand();
            decision.Transaction = transaction;
            decision.CommandText =
                """
                INSERT INTO tool_approval_decisions
                (
                    id, approval_id, tenant_id, from_status, to_status,
                    decision_user_id, decision_reason, decided_at_utc,
                    resulting_logical_revision
                )
                VALUES
                (
                    $id, $approvalId, $tenantId, $fromStatus, $toStatus,
                    $decisionUserId, $decisionReason, $decidedAtUtc,
                    $resultingRevision
                );
                """;
            decision.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("D"));
            decision.Parameters.AddWithValue("$approvalId", replacement.Id.ToString("D"));
            decision.Parameters.AddWithValue("$tenantId", replacement.TenantId);
            decision.Parameters.AddWithValue("$fromStatus", (int)existing.Status);
            decision.Parameters.AddWithValue("$toStatus", (int)replacement.Status);
            decision.Parameters.AddWithValue("$decisionUserId", replacement.DecisionUserId);
            decision.Parameters.AddWithValue("$decisionReason", replacement.DecisionReason);
            decision.Parameters.AddWithValue(
                "$decidedAtUtc",
                Format(replacement.DecidedAtUtc!.Value));
            decision.Parameters.AddWithValue(
                "$resultingRevision",
                replacement.LogicalRevision);
            await decision.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<ToolApprovalExecutionClaim?> TryClaimExecutionAsync(
        Guid id,
        string tenantId,
        long expectedLogicalRevision,
        DateTimeOffset claimedAtUtc,
        CancellationToken cancellationToken = default)
    {
        RequiredTenant(tenantId);
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteTransaction transaction =
            (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using SqliteCommand update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText =
            """
            UPDATE tool_approval_requests
            SET status = $consuming,
                logical_revision = logical_revision + 1,
                claimed_at_utc = $claimedAtUtc
            WHERE id = $id
              AND tenant_id = $tenantId
              AND logical_revision = $expectedRevision
              AND status = $approved
              AND requested_at_utc <= $claimedAtUtc
              AND expires_at_utc > $claimedAtUtc;
            """;
        update.Parameters.AddWithValue("$consuming", (int)ToolApprovalStatus.Consuming);
        update.Parameters.AddWithValue("$claimedAtUtc", Format(claimedAtUtc));
        update.Parameters.AddWithValue("$id", id.ToString("D"));
        update.Parameters.AddWithValue("$tenantId", tenantId);
        update.Parameters.AddWithValue("$expectedRevision", expectedLogicalRevision);
        update.Parameters.AddWithValue("$approved", (int)ToolApprovalStatus.Approved);
        if (await update.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        await using SqliteCommand select = connection.CreateCommand();
        select.Transaction = transaction;
        select.CommandText =
            $"""
            SELECT {Columns}, payload.protected_payload,
                   payload.protected_payload_sha256
            FROM tool_approval_requests AS approval
            INNER JOIN tool_approval_payloads AS payload
                ON payload.approval_id = approval.id
            WHERE approval.id = $id AND approval.tenant_id = $tenantId;
            """;
        select.Parameters.AddWithValue("$id", id.ToString("D"));
        select.Parameters.AddWithValue("$tenantId", tenantId);
        await using SqliteDataReader reader = await select.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        ToolApprovalRequestRecord request = Read(reader);
        string payload = reader.GetString(24);
        string payloadSha256 = reader.GetString(25);
        if (!string.Equals(Sha256(payload), payloadSha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "The protected tool approval payload failed its integrity check.");
        }

        try
        {
            ToolApprovalStateMachine.ValidateProtectedPayload(payload);
        }
        catch (ToolApprovalException exception)
        {
            throw new InvalidDataException(
                "The protected tool approval payload envelope is invalid.",
                exception);
        }

        await transaction.CommitAsync(cancellationToken);
        return new ToolApprovalExecutionClaim(request, payload, payloadSha256);
    }

    public async Task<bool> TryCompleteExecutionAsync(
        ToolApprovalRequestRecord replacement,
        long expectedLogicalRevision,
        ToolApprovalExecutionResultRecord result,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ToolApprovalStateMachine.ValidateExecutionResultEnvelope(result);
        }
        catch (ToolApprovalException)
        {
            return false;
        }

        ToolApprovalRequestRecord? existing = await GetAsync(
            replacement.Id,
            replacement.TenantId,
            cancellationToken);
        if (existing is null
            || existing.LogicalRevision != expectedLogicalRevision
            || !ValidResult(replacement, result))
        {
            return false;
        }

        ToolApprovalStateMachine.ValidateReplacement(existing, replacement);
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteTransaction transaction =
            (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using SqliteCommand update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText =
            """
            UPDATE tool_approval_requests
            SET status = $status,
                logical_revision = $revision,
                decision_user_id = $decisionUserId,
                decision_reason = $decisionReason,
                decided_at_utc = $decidedAtUtc,
                claimed_at_utc = $claimedAtUtc,
                finished_at_utc = $finishedAtUtc,
                error_code = $errorCode
            WHERE id = $id
              AND tenant_id = $tenantId
              AND logical_revision = $expectedRevision
              AND status = $expectedStatus;
            """;
        update.Parameters.AddWithValue("$id", replacement.Id.ToString("D"));
        update.Parameters.AddWithValue("$tenantId", replacement.TenantId);
        update.Parameters.AddWithValue("$expectedRevision", expectedLogicalRevision);
        update.Parameters.AddWithValue("$expectedStatus", (int)existing.Status);
        AddStateParameters(update, replacement);
        if (await update.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await using SqliteCommand insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText =
            """
            INSERT INTO tool_approval_execution_results
            (
                approval_id, tenant_id, succeeded, blocked,
                protected_content, protected_content_sha256,
                content_sha256, error_code, finished_at_utc
            )
            VALUES
            (
                $approvalId, $tenantId, $succeeded, $blocked,
                $content, $protectedSha256, $contentSha256,
                $errorCode, $finishedAtUtc
            );
            """;
        insert.Parameters.AddWithValue("$approvalId", result.ApprovalId.ToString("D"));
        insert.Parameters.AddWithValue("$tenantId", result.TenantId);
        insert.Parameters.AddWithValue("$succeeded", result.Succeeded ? 1 : 0);
        insert.Parameters.AddWithValue("$blocked", result.Blocked ? 1 : 0);
        insert.Parameters.AddWithValue("$content", result.ProtectedContent);
        insert.Parameters.AddWithValue("$protectedSha256", result.ProtectedContentSha256);
        insert.Parameters.AddWithValue("$contentSha256", result.ContentSha256);
        insert.Parameters.AddWithValue("$errorCode", result.ErrorCode);
        insert.Parameters.AddWithValue("$finishedAtUtc", Format(result.FinishedAtUtc));
        await insert.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<ToolApprovalExecutionResultRecord?> GetExecutionResultAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        RequiredTenant(tenantId);
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT approval_id, tenant_id, succeeded, blocked,
                   protected_content, protected_content_sha256,
                   content_sha256, error_code, finished_at_utc
            FROM tool_approval_execution_results
            WHERE approval_id = $id AND tenant_id = $tenantId;
            """;
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        command.Parameters.AddWithValue("$tenantId", tenantId);
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var result = new ToolApprovalExecutionResultRecord(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            reader.GetInt32(2) == 1,
            reader.GetInt32(3) == 1,
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetString(7),
            Parse(reader.GetString(8)));
        try
        {
            ToolApprovalStateMachine.ValidateExecutionResultEnvelope(result);
        }
        catch (ToolApprovalException exception)
        {
            throw new InvalidDataException(
                "The protected tool approval result envelope is invalid.",
                exception);
        }

        return result;
    }

    public async Task<int> RecoverInterruptedExecutionsAsync(
        DateTimeOffset recoveredAtUtc,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE tool_approval_requests
            SET status = $failed,
                logical_revision = logical_revision + 1,
                finished_at_utc = CASE
                    WHEN claimed_at_utc > $recoveredAtUtc THEN claimed_at_utc
                    ELSE $recoveredAtUtc
                END,
                error_code = $errorCode
            WHERE status = $consuming
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM tool_approval_execution_results AS result
                  WHERE result.approval_id = tool_approval_requests.id
              );
            """;
        command.Parameters.AddWithValue("$failed", (int)ToolApprovalStatus.Failed);
        command.Parameters.AddWithValue("$consuming", (int)ToolApprovalStatus.Consuming);
        command.Parameters.AddWithValue("$recoveredAtUtc", Format(recoveredAtUtc));
        command.Parameters.AddWithValue(
            "$errorCode",
            ToolApprovalErrorCodes.ExecutionOutcomeUnknown);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private void EnsureCreated()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            PRAGMA journal_mode = WAL;
            PRAGMA busy_timeout = 5000;
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS tool_approval_requests
            (
                id                          TEXT    NOT NULL PRIMARY KEY,
                tenant_id                   TEXT    NOT NULL,
                requester_user_id           TEXT    NOT NULL,
                conversation_id             TEXT    NOT NULL,
                entry_run_id                TEXT    NOT NULL,
                agent_run_id                TEXT    NOT NULL,
                agent_version_id            TEXT    NOT NULL,
                mcp_server_id               TEXT    NOT NULL,
                tool_version_id             TEXT    NOT NULL,
                tool_name                   TEXT    NOT NULL,
                risk                        INTEGER NOT NULL,
                tool_schema_sha256          TEXT    NOT NULL,
                arguments_sha256            TEXT    NOT NULL,
                safe_arguments_summary_json TEXT    NOT NULL CHECK (json_valid(safe_arguments_summary_json)),
                status                      INTEGER NOT NULL,
                logical_revision            INTEGER NOT NULL CHECK (logical_revision >= 0),
                requested_at_utc             TEXT    NOT NULL,
                expires_at_utc               TEXT    NOT NULL,
                decision_user_id             TEXT    NOT NULL,
                decision_reason              TEXT    NOT NULL,
                decided_at_utc               TEXT    NULL,
                claimed_at_utc               TEXT    NULL,
                finished_at_utc              TEXT    NULL,
                error_code                   TEXT    NOT NULL
            ) WITHOUT ROWID;

            CREATE INDEX IF NOT EXISTS ix_tool_approval_tenant_status_requested
            ON tool_approval_requests (tenant_id, status, requested_at_utc DESC);

            CREATE TABLE IF NOT EXISTS tool_approval_payloads
            (
                approval_id             TEXT NOT NULL PRIMARY KEY,
                protected_payload       TEXT NOT NULL,
                protected_payload_sha256 TEXT NOT NULL,
                FOREIGN KEY (approval_id) REFERENCES tool_approval_requests(id)
                    ON DELETE CASCADE
            ) WITHOUT ROWID;

            CREATE TABLE IF NOT EXISTS tool_approval_decisions
            (
                id                         TEXT    NOT NULL PRIMARY KEY,
                approval_id                TEXT    NOT NULL,
                tenant_id                  TEXT    NOT NULL,
                from_status                INTEGER NOT NULL,
                to_status                  INTEGER NOT NULL,
                decision_user_id           TEXT    NOT NULL,
                decision_reason            TEXT    NOT NULL,
                decided_at_utc              TEXT    NOT NULL,
                resulting_logical_revision INTEGER NOT NULL,
                UNIQUE (approval_id, resulting_logical_revision),
                FOREIGN KEY (approval_id) REFERENCES tool_approval_requests(id)
                    ON DELETE CASCADE
            ) WITHOUT ROWID;

            CREATE INDEX IF NOT EXISTS ix_tool_approval_decision_tenant_approval
            ON tool_approval_decisions
                (tenant_id, approval_id, resulting_logical_revision);

            CREATE TABLE IF NOT EXISTS tool_approval_execution_results
            (
                approval_id              TEXT    NOT NULL PRIMARY KEY,
                tenant_id                TEXT    NOT NULL,
                succeeded                INTEGER NOT NULL CHECK (succeeded IN (0, 1)),
                blocked                  INTEGER NOT NULL CHECK (blocked IN (0, 1)),
                protected_content        TEXT    NOT NULL,
                protected_content_sha256 TEXT    NOT NULL,
                content_sha256           TEXT    NOT NULL,
                error_code               TEXT    NOT NULL,
                finished_at_utc           TEXT    NOT NULL,
                FOREIGN KEY (approval_id) REFERENCES tool_approval_requests(id)
                    ON DELETE CASCADE
            ) WITHOUT ROWID;

            CREATE INDEX IF NOT EXISTS ix_tool_approval_result_tenant
            ON tool_approval_execution_results (tenant_id, approval_id);
            """;
        command.ExecuteNonQuery();
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            await using SqliteCommand pragma = connection.CreateCommand();
            pragma.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000;";
            await pragma.ExecuteNonQueryAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private static void AddImmutableParameters(
        SqliteCommand command,
        ToolApprovalRequestRecord value)
    {
        command.Parameters.AddWithValue("$id", value.Id.ToString("D"));
        command.Parameters.AddWithValue("$tenantId", value.TenantId);
        command.Parameters.AddWithValue("$requesterUserId", value.RequesterUserId);
        command.Parameters.AddWithValue("$conversationId", value.ConversationId.ToString("D"));
        command.Parameters.AddWithValue("$entryRunId", value.EntryRunId.ToString("D"));
        command.Parameters.AddWithValue("$agentRunId", value.AgentRunId.ToString("D"));
        command.Parameters.AddWithValue("$agentVersionId", value.AgentVersionId.ToString("D"));
        command.Parameters.AddWithValue("$mcpServerId", value.McpServerId.ToString("D"));
        command.Parameters.AddWithValue("$toolVersionId", value.ToolVersionId.ToString("D"));
        command.Parameters.AddWithValue("$toolName", value.ToolName);
        command.Parameters.AddWithValue("$risk", (int)value.Risk);
        command.Parameters.AddWithValue("$toolSchemaSha256", value.ToolSchemaSha256);
        command.Parameters.AddWithValue("$argumentsSha256", value.ArgumentsSha256);
        command.Parameters.AddWithValue("$summary", value.SafeArgumentsSummaryJson);
        command.Parameters.AddWithValue("$requestedAtUtc", Format(value.RequestedAtUtc));
        command.Parameters.AddWithValue("$expiresAtUtc", Format(value.ExpiresAtUtc));
    }

    private static void AddStateParameters(
        SqliteCommand command,
        ToolApprovalRequestRecord value)
    {
        command.Parameters.AddWithValue("$status", (int)value.Status);
        command.Parameters.AddWithValue("$revision", value.LogicalRevision);
        command.Parameters.AddWithValue("$decisionUserId", value.DecisionUserId);
        command.Parameters.AddWithValue("$decisionReason", value.DecisionReason);
        command.Parameters.AddWithValue("$decidedAtUtc", Nullable(value.DecidedAtUtc));
        command.Parameters.AddWithValue("$claimedAtUtc", Nullable(value.ClaimedAtUtc));
        command.Parameters.AddWithValue("$finishedAtUtc", Nullable(value.FinishedAtUtc));
        command.Parameters.AddWithValue("$errorCode", value.ErrorCode);
    }

    private static ToolApprovalRequestRecord Read(SqliteDataReader reader) =>
        new(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            reader.GetString(2),
            Guid.Parse(reader.GetString(3)),
            Guid.Parse(reader.GetString(4)),
            Guid.Parse(reader.GetString(5)),
            Guid.Parse(reader.GetString(6)),
            Guid.Parse(reader.GetString(7)),
            Guid.Parse(reader.GetString(8)),
            reader.GetString(9),
            (McpToolRisk)reader.GetInt32(10),
            reader.GetString(11),
            reader.GetString(12),
            reader.GetString(13),
            (ToolApprovalStatus)reader.GetInt32(14),
            reader.GetInt64(15),
            Parse(reader.GetString(16)),
            Parse(reader.GetString(17)),
            reader.GetString(18),
            reader.GetString(19),
            ReadNullableDate(reader, 20),
            ReadNullableDate(reader, 21),
            ReadNullableDate(reader, 22),
            reader.GetString(23));

    private static DateTimeOffset? ReadNullableDate(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Parse(reader.GetString(ordinal));

    private static string Format(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset Parse(string value) =>
        DateTimeOffset.Parse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);

    private static object Nullable(DateTimeOffset? value) =>
        value is null ? DBNull.Value : Format(value.Value);

    private static string Sha256(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static bool ValidResult(
        ToolApprovalRequestRecord replacement,
        ToolApprovalExecutionResultRecord result) =>
        result.ApprovalId == replacement.Id
        && string.Equals(result.TenantId, replacement.TenantId, StringComparison.Ordinal)
        && result.FinishedAtUtc == replacement.FinishedAtUtc
        && result.Succeeded == (replacement.Status == ToolApprovalStatus.Consumed)
        && string.Equals(result.ErrorCode, replacement.ErrorCode, StringComparison.Ordinal);

    private static bool IsHumanDecision(
        ToolApprovalStatus from,
        ToolApprovalStatus to) =>
        from == ToolApprovalStatus.Pending
        && to is ToolApprovalStatus.Approved
            or ToolApprovalStatus.Rejected
            or ToolApprovalStatus.Cancelled;

    private static void RequiredTenant(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ToolApprovalException(
                ToolApprovalErrorCodes.Invalid,
                "The tool approval tenant is required.");
        }
    }

    private const string Columns =
        "id, tenant_id, requester_user_id, conversation_id, entry_run_id, " +
        "agent_run_id, agent_version_id, mcp_server_id, tool_version_id, tool_name, risk, " +
        "tool_schema_sha256, arguments_sha256, safe_arguments_summary_json, " +
        "status, logical_revision, requested_at_utc, expires_at_utc, " +
        "decision_user_id, decision_reason, decided_at_utc, claimed_at_utc, " +
        "finished_at_utc, error_code";
}
