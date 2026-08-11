using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EU.Core.Agent.Application.Approvals;
using EU.Core.Agent.Application.Mcp;
using Microsoft.Data.SqlClient;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class SqlServerToolApprovalRepository : IToolApprovalRepository
{
    private readonly string _connectionString;

    public SqlServerToolApprovalRepository(string connectionString)
    {
        _connectionString = SqlServerAgentConnection.Validate(connectionString);
    }

    public async Task<bool> TryCreateAsync(
        ToolApprovalRequestRecord request,
        string protectedResumePayload,
        CancellationToken cancellationToken = default)
    {
        ToolApprovalStateMachine.ValidateNew(request, protectedResumePayload);
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using SqlCommand requestCommand = connection.CreateCommand();
        requestCommand.Transaction = transaction;
        requestCommand.CommandText =
            """
            INSERT INTO AgToolApprovalRequest
            (
                Id, TenantId, RequesterUserId, ConversationId, EntryRunId,
                AgentRunId, AgentVersionId, McpServerId, ToolVersionId, ToolName, Risk,
                ToolSchemaSha256, ArgumentsSha256, SafeArgumentsSummaryJson,
                Status, LogicalRevision, RequestedAtUtc, ExpiresAtUtc,
                DecisionUserId, DecisionReason, DecidedAtUtc, ClaimedAtUtc,
                FinishedAtUtc, ErrorCode
            )
            SELECT
                @Id, @tenantId, @requesterUserId, @conversationId, @entryRunId,
                @agentRunId, @agentVersionId, @mcpServerId, @toolVersionId, @toolName, @Risk,
                @toolSchemaSha256, @argumentsSha256, @summary, @Status, @revision,
                @requestedAtUtc, @expiresAtUtc, '', '', NULL, NULL, NULL, ''
            WHERE NOT EXISTS
                (SELECT 1 FROM AgToolApprovalRequest WITH (UPDLOCK, HOLDLOCK) WHERE Id=@Id);
            """;
        AddImmutableParameters(requestCommand, request);
        AddStateParameters(requestCommand, request);
        if (await requestCommand.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await using SqlCommand payloadCommand = connection.CreateCommand();
        payloadCommand.Transaction = transaction;
        payloadCommand.CommandText =
            """
            INSERT INTO AgToolApprovalPayload
                (ApprovalId, ProtectedPayload, ProtectedPayloadSha256)
            VALUES (@approvalId, @payload, @sha256);
            """;
        payloadCommand.Parameters.AddWithValue("@approvalId", request.Id.ToString("D"));
        payloadCommand.Parameters.AddWithValue("@payload", protectedResumePayload);
        payloadCommand.Parameters.AddWithValue(
            "@sha256",
            Sha256(protectedResumePayload));
        await payloadCommand.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<ToolApprovalRequestRecord?> GetAsync(
        Guid Id,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        RequiredTenant(tenantId);
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columns} FROM AgToolApprovalRequest WHERE Id = @Id AND TenantId = @tenantId;";
        command.Parameters.AddWithValue("@Id", Id.ToString("D"));
        command.Parameters.AddWithValue("@tenantId", tenantId);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    public async Task<IReadOnlyList<ToolApprovalRequestRecord>> ListAsync(
        ToolApprovalQuery query,
        CancellationToken cancellationToken = default)
    {
        ToolApprovalStateMachine.ValidateQuery(query);
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = query.Status is null
            ? $"SELECT {Columns} FROM AgToolApprovalRequest WHERE TenantId = @tenantId ORDER BY RequestedAtUtc DESC, Id OFFSET 0 ROWS FETCH NEXT @take ROWS ONLY;"
            : $"SELECT {Columns} FROM AgToolApprovalRequest WHERE TenantId = @tenantId AND Status = @Status ORDER BY RequestedAtUtc DESC, Id OFFSET 0 ROWS FETCH NEXT @take ROWS ONLY;";
        command.Parameters.AddWithValue("@tenantId", query.TenantId);
        command.Parameters.AddWithValue("@take", query.Take);
        if (query.Status is not null)
        {
            command.Parameters.AddWithValue("@Status", (int)query.Status.Value);
        }

        var values = new List<ToolApprovalRequestRecord>();
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
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
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, ApprovalId, TenantId, FromStatus, ToStatus,
                   DecisionUserId, DecisionReason, DecidedAtUtc,
                   ResultingLogicalRevision
            FROM AgToolApprovalDecision
            WHERE ApprovalId = @approvalId AND TenantId = @tenantId
            ORDER BY ResultingLogicalRevision, Id;
            """;
        command.Parameters.AddWithValue("@approvalId", approvalId.ToString("D"));
        command.Parameters.AddWithValue("@tenantId", tenantId);

        var values = new List<ToolApprovalDecisionRecord>();
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
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
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            UPDATE AgToolApprovalRequest
            SET Status = @Status,
                LogicalRevision = @revision,
                DecisionUserId = @decisionUserId,
                DecisionReason = @decisionReason,
                DecidedAtUtc = @decidedAtUtc,
                ClaimedAtUtc = @claimedAtUtc,
                FinishedAtUtc = @finishedAtUtc,
                ErrorCode = @errorCode
            WHERE Id = @Id
              AND TenantId = @tenantId
              AND LogicalRevision = @expectedRevision
              AND Status = @expectedStatus;
            """;
        command.Parameters.AddWithValue("@Id", replacement.Id.ToString("D"));
        command.Parameters.AddWithValue("@tenantId", replacement.TenantId);
        command.Parameters.AddWithValue("@expectedRevision", expectedLogicalRevision);
        command.Parameters.AddWithValue("@expectedStatus", (int)existing.Status);
        AddStateParameters(command, replacement);
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        if (IsHumanDecision(existing.Status, replacement.Status))
        {
            await using SqlCommand decision = connection.CreateCommand();
            decision.Transaction = transaction;
            decision.CommandText =
                """
                INSERT INTO AgToolApprovalDecision
                (
                    Id, ApprovalId, TenantId, FromStatus, ToStatus,
                    DecisionUserId, DecisionReason, DecidedAtUtc,
                    ResultingLogicalRevision
                )
                VALUES
                (
                    @Id, @approvalId, @tenantId, @fromStatus, @toStatus,
                    @decisionUserId, @decisionReason, @decidedAtUtc,
                    @resultingRevision
                );
                """;
            decision.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString("D"));
            decision.Parameters.AddWithValue("@approvalId", replacement.Id.ToString("D"));
            decision.Parameters.AddWithValue("@tenantId", replacement.TenantId);
            decision.Parameters.AddWithValue("@fromStatus", (int)existing.Status);
            decision.Parameters.AddWithValue("@toStatus", (int)replacement.Status);
            decision.Parameters.AddWithValue("@decisionUserId", replacement.DecisionUserId);
            decision.Parameters.AddWithValue("@decisionReason", replacement.DecisionReason);
            decision.Parameters.AddWithValue(
                "@decidedAtUtc",
                Format(replacement.DecidedAtUtc!.Value));
            decision.Parameters.AddWithValue(
                "@resultingRevision",
                replacement.LogicalRevision);
            await decision.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<ToolApprovalExecutionClaim?> TryClaimExecutionAsync(
        Guid Id,
        string tenantId,
        long expectedLogicalRevision,
        DateTimeOffset claimedAtUtc,
        CancellationToken cancellationToken = default)
    {
        RequiredTenant(tenantId);
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using SqlCommand update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText =
            """
            UPDATE AgToolApprovalRequest
            SET Status = @consuming,
                LogicalRevision = LogicalRevision + 1,
                ClaimedAtUtc = @claimedAtUtc
            WHERE Id = @Id
              AND TenantId = @tenantId
              AND LogicalRevision = @expectedRevision
              AND Status = @approved
              AND RequestedAtUtc <= @claimedAtUtc
              AND ExpiresAtUtc > @claimedAtUtc;
            """;
        update.Parameters.AddWithValue("@consuming", (int)ToolApprovalStatus.Consuming);
        update.Parameters.AddWithValue("@claimedAtUtc", Format(claimedAtUtc));
        update.Parameters.AddWithValue("@Id", Id.ToString("D"));
        update.Parameters.AddWithValue("@tenantId", tenantId);
        update.Parameters.AddWithValue("@expectedRevision", expectedLogicalRevision);
        update.Parameters.AddWithValue("@approved", (int)ToolApprovalStatus.Approved);
        if (await update.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        await using SqlCommand select = connection.CreateCommand();
        select.Transaction = transaction;
        select.CommandText =
            $"""
            SELECT {Columns}, payload.ProtectedPayload,
                   payload.ProtectedPayloadSha256
            FROM AgToolApprovalRequest AS approval
            INNER JOIN AgToolApprovalPayload AS payload
                ON payload.ApprovalId = approval.Id
            WHERE approval.Id = @Id AND approval.TenantId = @tenantId;
            """;
        select.Parameters.AddWithValue("@Id", Id.ToString("D"));
        select.Parameters.AddWithValue("@tenantId", tenantId);
        await using SqlDataReader reader = await select.ExecuteReaderAsync(cancellationToken);
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
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using SqlCommand update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText =
            """
            UPDATE AgToolApprovalRequest
            SET Status = @Status,
                LogicalRevision = @revision,
                DecisionUserId = @decisionUserId,
                DecisionReason = @decisionReason,
                DecidedAtUtc = @decidedAtUtc,
                ClaimedAtUtc = @claimedAtUtc,
                FinishedAtUtc = @finishedAtUtc,
                ErrorCode = @errorCode
            WHERE Id = @Id
              AND TenantId = @tenantId
              AND LogicalRevision = @expectedRevision
              AND Status = @expectedStatus;
            """;
        update.Parameters.AddWithValue("@Id", replacement.Id.ToString("D"));
        update.Parameters.AddWithValue("@tenantId", replacement.TenantId);
        update.Parameters.AddWithValue("@expectedRevision", expectedLogicalRevision);
        update.Parameters.AddWithValue("@expectedStatus", (int)existing.Status);
        AddStateParameters(update, replacement);
        if (await update.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await using SqlCommand insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText =
            """
            INSERT INTO AgToolApprovalExecutionResult
            (
                ApprovalId, TenantId, Succeeded, Blocked,
                ProtectedContent, ProtectedContentSha256,
                ContentSha256, ErrorCode, FinishedAtUtc
            )
            VALUES
            (
                @approvalId, @tenantId, @Succeeded, @Blocked,
                @Content, @protectedSha256, @contentSha256,
                @errorCode, @finishedAtUtc
            );
            """;
        insert.Parameters.AddWithValue("@approvalId", result.ApprovalId.ToString("D"));
        insert.Parameters.AddWithValue("@tenantId", result.TenantId);
        insert.Parameters.AddWithValue("@Succeeded", result.Succeeded ? 1 : 0);
        insert.Parameters.AddWithValue("@Blocked", result.Blocked ? 1 : 0);
        insert.Parameters.AddWithValue("@Content", result.ProtectedContent);
        insert.Parameters.AddWithValue("@protectedSha256", result.ProtectedContentSha256);
        insert.Parameters.AddWithValue("@contentSha256", result.ContentSha256);
        insert.Parameters.AddWithValue("@errorCode", result.ErrorCode);
        insert.Parameters.AddWithValue("@finishedAtUtc", Format(result.FinishedAtUtc));
        await insert.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<ToolApprovalExecutionResultRecord?> GetExecutionResultAsync(
        Guid Id,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        RequiredTenant(tenantId);
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT ApprovalId, TenantId, Succeeded, Blocked,
                   ProtectedContent, ProtectedContentSha256,
                   ContentSha256, ErrorCode, FinishedAtUtc
            FROM AgToolApprovalExecutionResult
            WHERE ApprovalId = @Id AND TenantId = @tenantId;
            """;
        command.Parameters.AddWithValue("@Id", Id.ToString("D"));
        command.Parameters.AddWithValue("@tenantId", tenantId);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
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
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE AgToolApprovalRequest
            SET Status = @failed,
                LogicalRevision = LogicalRevision + 1,
                FinishedAtUtc = CASE
                    WHEN ClaimedAtUtc > @recoveredAtUtc THEN ClaimedAtUtc
                    ELSE @recoveredAtUtc
                END,
                ErrorCode = @errorCode
            WHERE Status = @consuming
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM AgToolApprovalExecutionResult AS result
                  WHERE result.ApprovalId = AgToolApprovalRequest.Id
              );
            """;
        command.Parameters.AddWithValue("@failed", (int)ToolApprovalStatus.Failed);
        command.Parameters.AddWithValue("@consuming", (int)ToolApprovalStatus.Consuming);
        command.Parameters.AddWithValue("@recoveredAtUtc", Format(recoveredAtUtc));
        command.Parameters.AddWithValue(
            "@errorCode",
            ToolApprovalErrorCodes.ExecutionOutcomeUnknown);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }



    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        return await SqlServerAgentConnection.OpenAsync(_connectionString, cancellationToken);
    }

    private static void AddImmutableParameters(
        SqlCommand command,
        ToolApprovalRequestRecord value)
    {
        command.Parameters.AddWithValue("@Id", value.Id.ToString("D"));
        command.Parameters.AddWithValue("@tenantId", value.TenantId);
        command.Parameters.AddWithValue("@requesterUserId", value.RequesterUserId);
        command.Parameters.AddWithValue("@conversationId", value.ConversationId.ToString("D"));
        command.Parameters.AddWithValue("@entryRunId", value.EntryRunId.ToString("D"));
        command.Parameters.AddWithValue("@agentRunId", value.AgentRunId.ToString("D"));
        command.Parameters.AddWithValue("@agentVersionId", value.AgentVersionId.ToString("D"));
        command.Parameters.AddWithValue("@mcpServerId", value.McpServerId.ToString("D"));
        command.Parameters.AddWithValue("@toolVersionId", value.ToolVersionId.ToString("D"));
        command.Parameters.AddWithValue("@toolName", value.ToolName);
        command.Parameters.AddWithValue("@Risk", (int)value.Risk);
        command.Parameters.AddWithValue("@toolSchemaSha256", value.ToolSchemaSha256);
        command.Parameters.AddWithValue("@argumentsSha256", value.ArgumentsSha256);
        command.Parameters.AddWithValue("@summary", value.SafeArgumentsSummaryJson);
        command.Parameters.AddWithValue("@requestedAtUtc", Format(value.RequestedAtUtc));
        command.Parameters.AddWithValue("@expiresAtUtc", Format(value.ExpiresAtUtc));
    }

    private static void AddStateParameters(
        SqlCommand command,
        ToolApprovalRequestRecord value)
    {
        command.Parameters.AddWithValue("@Status", (int)value.Status);
        command.Parameters.AddWithValue("@revision", value.LogicalRevision);
        command.Parameters.AddWithValue("@decisionUserId", value.DecisionUserId);
        command.Parameters.AddWithValue("@decisionReason", value.DecisionReason);
        command.Parameters.AddWithValue("@decidedAtUtc", Nullable(value.DecidedAtUtc));
        command.Parameters.AddWithValue("@claimedAtUtc", Nullable(value.ClaimedAtUtc));
        command.Parameters.AddWithValue("@finishedAtUtc", Nullable(value.FinishedAtUtc));
        command.Parameters.AddWithValue("@errorCode", value.ErrorCode);
    }

    private static ToolApprovalRequestRecord Read(SqlDataReader reader) =>
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

    private static DateTimeOffset? ReadNullableDate(SqlDataReader reader, int Ordinal) =>
        reader.IsDBNull(Ordinal) ? null : Parse(reader.GetString(Ordinal));

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
        "Id, TenantId, RequesterUserId, ConversationId, EntryRunId, " +
        "AgentRunId, AgentVersionId, McpServerId, ToolVersionId, ToolName, Risk, " +
        "ToolSchemaSha256, ArgumentsSha256, SafeArgumentsSummaryJson, " +
        "Status, LogicalRevision, RequestedAtUtc, ExpiresAtUtc, " +
        "DecisionUserId, DecisionReason, DecidedAtUtc, ClaimedAtUtc, " +
        "FinishedAtUtc, ErrorCode";
}
