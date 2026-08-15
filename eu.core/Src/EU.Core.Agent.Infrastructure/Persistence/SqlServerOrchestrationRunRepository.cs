using System.Text.Json;
using EU.Core.Agent.Application.Orchestration;
using EU.Core.Agent.Application.Runtime;
using Microsoft.Data.SqlClient;

namespace EU.Core.Agent.Infrastructure.Persistence;

internal sealed class SqlServerOrchestrationRunRepositoryHooks
{
    public Func<CancellationToken, Task>? BeforeInterruptedSummaryWriteAsync { get; init; }
}

public sealed class SqlServerOrchestrationRunRepository : IOrchestrationRunRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    private readonly string _connectionString;
    private readonly SqlServerOrchestrationRunRepositoryHooks _hooks;

    public SqlServerOrchestrationRunRepository(string connectionString)
        : this(connectionString, new SqlServerOrchestrationRunRepositoryHooks())
    {
    }

    internal SqlServerOrchestrationRunRepository(
        string connectionString,
        SqlServerOrchestrationRunRepositoryHooks hooks)
    {
        _connectionString = SqlServerOrchestrationStore.CreateConnectionString(connectionString);
        _hooks = hooks;
    }

    public async Task SaveAsync(OrchestrationRunRecord value, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlTransaction transaction = connection.BeginTransaction();
        try
        {
            string? existingJson;
            await using (SqlCommand read = connection.CreateCommand())
            {
                read.Transaction = transaction;
                read.CommandText =
                    "SELECT DocumentJson FROM AgOrchestrationRun WITH (UPDLOCK, HOLDLOCK) WHERE Id=@Id;";
                read.Parameters.AddWithValue("@Id", value.Id.ToString("D"));
                existingJson = await read.ExecuteScalarAsync(cancellationToken) as string;
            }

            if (existingJson is null)
            {
                await using SqlCommand insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO AgOrchestrationRun(Id,OrchestrationId,StartedAtUtc,DocumentJson)
                    VALUES(@Id,@orchestrationId,@started,@json);
                    """;
                insert.Parameters.AddWithValue("@Id", value.Id.ToString("D"));
                insert.Parameters.AddWithValue("@orchestrationId", value.OrchestrationId.ToString("D"));
                insert.Parameters.AddWithValue("@started", value.StartedAtUtc.ToString("O"));
                insert.Parameters.AddWithValue("@json", JsonSerializer.Serialize(value, JsonOptions));
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }
            else if (Read(existingJson).Status is OrchestrationRunStatus.Running)
            {
                await using SqlCommand update = connection.CreateCommand();
                update.Transaction = transaction;
                update.CommandText =
                    "UPDATE AgOrchestrationRun SET DocumentJson=@json WHERE Id=@Id;";
                update.Parameters.AddWithValue("@Id", value.Id.ToString("D"));
                update.Parameters.AddWithValue("@json", JsonSerializer.Serialize(value, JsonOptions));
                await update.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<OrchestrationRunRecord?> GetAsync(Guid Id, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT DocumentJson FROM AgOrchestrationRun WHERE Id=@Id;";
        command.Parameters.AddWithValue("@Id", Id.ToString("D"));
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is string json ? Read(json) : null;
    }

    public async Task<IReadOnlyList<OrchestrationRunRecord>> ListAsync(
        Guid orchestrationId, int take, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT DocumentJson FROM AgOrchestrationRun
            WHERE OrchestrationId=@Id ORDER BY StartedAtUtc DESC OFFSET 0 ROWS FETCH NEXT @take ROWS ONLY;
            """;
        command.Parameters.AddWithValue("@Id", orchestrationId.ToString("D"));
        command.Parameters.AddWithValue("@take", Math.Clamp(take, 1, 100));
        var values = new List<OrchestrationRunRecord>();
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) values.Add(Read(reader.GetString(0)));
        return OrchestrationContractCloner.ReadOnly(values);
    }

    public async Task SaveDetailsAsync(
        OrchestrationRunDetails value,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await WriteDetailsAsync(connection, transaction, value, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task WriteDetailsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        OrchestrationRunDetails value,
        CancellationToken cancellationToken)
    {
        await using (SqlCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE AgOrchestrationRunDetail
                SET OrchestrationId=@orchestrationId, InputText=@input, OutputText=@output
                WHERE RunId=@runId;
                IF @@ROWCOUNT = 0
                BEGIN
                    INSERT INTO AgOrchestrationRunDetail(RunId,OrchestrationId,InputText,OutputText)
                    VALUES(@runId,@orchestrationId,@input,@output);
                END;
                DELETE FROM AgOrchestrationToolCall WHERE RunId=@runId;
                DELETE FROM AgOrchestrationNodeAttempt WHERE RunId=@runId;
                """;
            command.Parameters.AddWithValue("@runId", value.RunId.ToString("D"));
            command.Parameters.AddWithValue("@orchestrationId", value.OrchestrationId.ToString("D"));
            command.Parameters.AddWithValue("@input", value.Input);
            command.Parameters.AddWithValue("@output", value.Output);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        for (int attemptSequence = 0; attemptSequence < value.Attempts.Count; attemptSequence++)
        {
            OrchestrationNodeAttemptRecord Attempt = value.Attempts[attemptSequence];
            await InsertAttemptAsync(
                connection, transaction, value.RunId, attemptSequence, Attempt, cancellationToken);
            for (int toolSequence = 0; toolSequence < Attempt.ToolCalls.Count; toolSequence++)
            {
                await InsertToolAsync(
                    connection, transaction, value.RunId, Attempt.NodeId, Attempt.Attempt,
                    toolSequence, Attempt.ToolCalls[toolSequence], cancellationToken);
            }
        }
    }

    public async Task<OrchestrationRunDetails?> GetDetailsAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        Guid orchestrationId;
        string input;
        string output;
        await using (SqlCommand command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT OrchestrationId,InputText,OutputText
                FROM AgOrchestrationRunDetail WHERE RunId=@runId;
                """;
            command.Parameters.AddWithValue("@runId", runId.ToString("D"));
            await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            orchestrationId = Guid.Parse(reader.GetString(0));
            input = reader.GetString(1);
            output = reader.GetString(2);
        }

        var attemptRows = new List<(
            string NodeId,
            int Attempt,
            Guid AgentRunId,
            string Input,
            string InputSha256,
            string Output,
            string OutputSha256,
            OrchestrationNodeRunStatus Status,
            DateTimeOffset StartedAtUtc,
            DateTimeOffset? FinishedAtUtc,
            string ErrorCode)>();
        await using (SqlCommand command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT NodeId,Attempt,AgentRunId,InputText,InputSha256,
                       OutputText,OutputSha256,Status,StartedAtUtc,
                       FinishedAtUtc,ErrorCode
                FROM AgOrchestrationNodeAttempt
                WHERE RunId=@runId ORDER BY Sequence;
                """;
            command.Parameters.AddWithValue("@runId", runId.ToString("D"));
            await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                attemptRows.Add((
                    ReadFixedLengthText(reader, 0),
                    reader.GetInt32(1),
                    Guid.Parse(reader.GetString(2)),
                    reader.GetString(3),
                    ReadFixedLengthText(reader, 4),
                    reader.GetString(5),
                    ReadFixedLengthText(reader, 6),
                    Enum.Parse<OrchestrationNodeRunStatus>(reader.GetString(7)),
                    DateTimeOffset.Parse(reader.GetString(8)),
                    reader.IsDBNull(9) ? null : DateTimeOffset.Parse(reader.GetString(9)),
                    reader.GetString(10)));
            }
        }

        var attempts = new List<OrchestrationNodeAttemptRecord>(attemptRows.Count);
        foreach (var row in attemptRows)
        {
            IReadOnlyList<OrchestrationToolCallRecord> tools = await ReadToolsAsync(
                connection, runId, row.NodeId, row.Attempt, cancellationToken);
            attempts.Add(new OrchestrationNodeAttemptRecord(
                row.NodeId,
                row.Attempt,
                row.AgentRunId,
                row.Input,
                row.InputSha256,
                row.Output,
                row.OutputSha256,
                row.Status,
                row.StartedAtUtc,
                row.FinishedAtUtc,
                row.ErrorCode,
                tools));
        }

        return new OrchestrationRunDetails(
            runId, orchestrationId, input, output,
            OrchestrationContractCloner.ReadOnly(attempts));
    }

    public async Task<bool> TrySaveRunningDetailsAsync(
        OrchestrationRunDetails value,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlTransaction transaction =
            connection.BeginTransaction();
        bool isRunning;
        await using (SqlCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText =
                "SELECT DocumentJson FROM AgOrchestrationRun WHERE Id=@Id;";
            command.Parameters.AddWithValue("@Id", value.RunId.ToString("D"));
            object? result = await command.ExecuteScalarAsync(cancellationToken);
            isRunning = result is string json
                && Read(json).Status is OrchestrationRunStatus.Running;
        }

        if (!isRunning)
        {
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        await WriteDetailsAsync(connection, transaction, value, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public Task<OrchestrationRunTransitionResult> TryFinalizeRunningAsync(
        Guid runId,
        OrchestrationRunStatus runStatus,
        OrchestrationNodeRunStatus nodeStatus,
        OrchestrationTerminalTransitionPolicy transitionPolicy,
        DateTimeOffset finishedAtUtc,
        string errorCode,
        OrchestrationRunDetails? detailsIfMissing,
        CancellationToken cancellationToken = default)
    {
        if (runStatus == OrchestrationRunStatus.Running)
        {
            throw new ArgumentOutOfRangeException(
                nameof(runStatus),
                "A terminal run Status is required.");
        }
        if (nodeStatus is OrchestrationNodeRunStatus.Pending
            or OrchestrationNodeRunStatus.Running)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nodeStatus),
                "A terminal node Status is required.");
        }

        return TransitionRunningAsync(
            runId,
            runStatus,
            nodeStatus,
            transitionPolicy,
            finishedAtUtc,
            errorCode,
            detailsIfMissing,
            invokeRecoveryHook: false,
            cancellationToken);
    }

    public Task<OrchestrationRunTransitionResult> RecoverInterruptedAsync(
        Guid runId,
        DateTimeOffset recoveredAtUtc,
        string errorCode,
        CancellationToken cancellationToken = default) =>
        TransitionRunningAsync(
            runId,
            OrchestrationRunStatus.Failed,
            OrchestrationNodeRunStatus.Failed,
            OrchestrationTerminalTransitionPolicy.TerminalizePending,
            recoveredAtUtc,
            errorCode,
            detailsIfMissing: null,
            invokeRecoveryHook: true,
            cancellationToken);

    private async Task<OrchestrationRunTransitionResult> TransitionRunningAsync(
        Guid runId,
        OrchestrationRunStatus runStatus,
        OrchestrationNodeRunStatus nodeStatus,
        OrchestrationTerminalTransitionPolicy transitionPolicy,
        DateTimeOffset finishedAtUtc,
        string errorCode,
        OrchestrationRunDetails? detailsIfMissing,
        bool invokeRecoveryHook,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlTransaction transaction =
            connection.BeginTransaction();
        OrchestrationRunRecord? value;
        string? documentJson;
        await using (SqlCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText =
                "SELECT DocumentJson FROM AgOrchestrationRun WHERE Id=@Id;";
            command.Parameters.AddWithValue("@Id", runId.ToString("D"));
            object? result = await command.ExecuteScalarAsync(cancellationToken);
            documentJson = result as string;
            value = documentJson is null ? null : Read(documentJson);
        }

        if (value is null || value.Status != OrchestrationRunStatus.Running)
        {
            await transaction.CommitAsync(cancellationToken);
            return new OrchestrationRunTransitionResult(value, false);
        }

        OrchestrationRunRecord terminal = value with
        {
            Status = runStatus,
            FinishedAtUtc = finishedAtUtc,
            ErrorCode = errorCode,
            Nodes = OrchestrationContractCloner.ReadOnly(value.Nodes.Select(node =>
                ShouldTerminalize(node.Status, transitionPolicy)
                    ? node with
                    {
                        Status = nodeStatus,
                        FinishedAtUtc = finishedAtUtc,
                        ErrorCode = errorCode
                    }
                    : node))
        };
        if (detailsIfMissing is not null)
        {
            if (detailsIfMissing.RunId != runId)
            {
                throw new InvalidOperationException(
                    "Fallback orchestration details do not belong to the transitioned run.");
            }
            bool detailsExist;
            await using (SqlCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = """
                    SELECT EXISTS(
                      SELECT 1 FROM AgOrchestrationRunDetail WHERE RunId=@Id);
                    """;
                command.Parameters.AddWithValue("@Id", runId.ToString("D"));
                detailsExist = Convert.ToInt32(
                    await command.ExecuteScalarAsync(CancellationToken.None)) == 1;
            }
            if (!detailsExist)
            {
                await WriteDetailsAsync(
                    connection,
                    transaction,
                    detailsIfMissing,
                    CancellationToken.None);
            }
        }

        string finishedAt = finishedAtUtc.ToString("O");
        await using (SqlCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE AgOrchestrationNodeAttempt
                SET Status=@nodeStatus,FinishedAtUtc=@finished,ErrorCode=@error
                WHERE RunId=@Id
                  AND (Status=@running
                    OR (@terminalizePending=1 AND Status=@pending));
                UPDATE AgOrchestrationToolCall
                SET Status=@toolFailed,FinishedAtUtc=@finished,ErrorCode=@error
                WHERE RunId=@Id AND Status=@toolStarted;
                """;
            command.Parameters.AddWithValue("@Id", runId.ToString("D"));
            command.Parameters.AddWithValue(
                "@nodeStatus",
                nodeStatus.ToString());
            command.Parameters.AddWithValue(
                "@pending",
                OrchestrationNodeRunStatus.Pending.ToString());
            command.Parameters.AddWithValue(
                "@running",
                OrchestrationNodeRunStatus.Running.ToString());
            command.Parameters.AddWithValue(
                "@terminalizePending",
                transitionPolicy
                    == OrchestrationTerminalTransitionPolicy.TerminalizePending
                    ? 1
                    : 0);
            command.Parameters.AddWithValue(
                "@toolFailed",
                AgentRunEventKind.ToolFailed.ToString());
            command.Parameters.AddWithValue(
                "@toolStarted",
                AgentRunEventKind.ToolStarted.ToString());
            command.Parameters.AddWithValue("@finished", finishedAt);
            command.Parameters.AddWithValue("@error", errorCode);
            await command.ExecuteNonQueryAsync(CancellationToken.None);
        }

        if (invokeRecoveryHook
            && _hooks.BeforeInterruptedSummaryWriteAsync is not null)
        {
            await _hooks.BeforeInterruptedSummaryWriteAsync(CancellationToken.None);
        }

        await using (SqlCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE AgOrchestrationRun SET DocumentJson=@terminal
                WHERE Id=@Id AND DocumentJson=@expected;
                """;
            command.Parameters.AddWithValue("@Id", runId.ToString("D"));
            command.Parameters.AddWithValue(
                "@terminal",
                JsonSerializer.Serialize(terminal, JsonOptions));
            command.Parameters.AddWithValue("@expected", documentJson);
            int affected = await command.ExecuteNonQueryAsync(CancellationToken.None);
            if (affected != 1)
            {
                throw new InvalidOperationException(
                    $"Interrupted orchestration run '{runId}' changed during recovery.");
            }
        }

        await transaction.CommitAsync(CancellationToken.None);
        return new OrchestrationRunTransitionResult(terminal, true);
    }

    private static bool ShouldTerminalize(
        OrchestrationNodeRunStatus Status,
        OrchestrationTerminalTransitionPolicy transitionPolicy) =>
        Status == OrchestrationNodeRunStatus.Running
        || (Status == OrchestrationNodeRunStatus.Pending
            && transitionPolicy
                == OrchestrationTerminalTransitionPolicy.TerminalizePending);

    private static async Task InsertAttemptAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid runId,
        int Sequence,
        OrchestrationNodeAttemptRecord value,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO AgOrchestrationNodeAttempt(
              RunId,NodeId,Attempt,Sequence,AgentRunId,InputText,InputSha256,
              OutputText,OutputSha256,Status,StartedAtUtc,FinishedAtUtc,ErrorCode)
            VALUES(@runId,@nodeId,@Attempt,@Sequence,@agentRunId,@input,@inputSha,
              @output,@outputSha,@Status,@started,@finished,@error);
            """;
        command.Parameters.AddWithValue("@runId", runId.ToString("D"));
        command.Parameters.AddWithValue("@nodeId", value.NodeId);
        command.Parameters.AddWithValue("@Attempt", value.Attempt);
        command.Parameters.AddWithValue("@Sequence", Sequence);
        command.Parameters.AddWithValue("@agentRunId", value.AgentRunId.ToString("D"));
        command.Parameters.AddWithValue("@input", value.Input);
        command.Parameters.AddWithValue("@inputSha", value.InputSha256);
        command.Parameters.AddWithValue("@output", value.Output);
        command.Parameters.AddWithValue("@outputSha", value.OutputSha256);
        command.Parameters.AddWithValue("@Status", value.Status.ToString());
        command.Parameters.AddWithValue("@started", value.StartedAtUtc.ToString("O"));
        command.Parameters.AddWithValue(
            "@finished",
            value.FinishedAtUtc is null ? DBNull.Value : value.FinishedAtUtc.Value.ToString("O"));
        command.Parameters.AddWithValue("@error", value.ErrorCode);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertToolAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid runId,
        string nodeId,
        int Attempt,
        int Sequence,
        OrchestrationToolCallRecord value,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO AgOrchestrationToolCall(
              ToolCallId,RunId,NodeId,Attempt,Sequence,AgentRunId,ToolVersionId,
              ToolName,Status,ArgumentsJson,ResultContent,ResultSha256,ResultCharacters,
              StartedAtUtc,FinishedAtUtc,ErrorCode)
            VALUES(@toolCallId,@runId,@nodeId,@Attempt,@Sequence,@agentRunId,@toolVersionId,
              @toolName,@Status,@arguments,@result,@resultSha,@resultCharacters,
              @started,@finished,@error);
            """;
        command.Parameters.AddWithValue("@toolCallId", value.ToolCallId.ToString("D"));
        command.Parameters.AddWithValue("@runId", runId.ToString("D"));
        command.Parameters.AddWithValue("@nodeId", nodeId);
        command.Parameters.AddWithValue("@Attempt", Attempt);
        command.Parameters.AddWithValue("@Sequence", Sequence);
        command.Parameters.AddWithValue("@agentRunId", value.AgentRunId.ToString("D"));
        command.Parameters.AddWithValue("@toolVersionId", value.ToolVersionId.ToString("D"));
        command.Parameters.AddWithValue("@toolName", value.ToolName);
        command.Parameters.AddWithValue("@Status", value.Status.ToString());
        command.Parameters.AddWithValue("@arguments", value.ArgumentsJson);
        command.Parameters.AddWithValue("@result", value.ResultContent);
        command.Parameters.AddWithValue("@resultSha", value.ResultSha256);
        command.Parameters.AddWithValue("@resultCharacters", value.ResultCharacters);
        command.Parameters.AddWithValue("@started", value.StartedAtUtc.ToString("O"));
        command.Parameters.AddWithValue(
            "@finished",
            value.FinishedAtUtc is null ? DBNull.Value : value.FinishedAtUtc.Value.ToString("O"));
        command.Parameters.AddWithValue("@error", value.ErrorCode);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<OrchestrationToolCallRecord>> ReadToolsAsync(
        SqlConnection connection,
        Guid runId,
        string nodeId,
        int Attempt,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT ToolCallId,AgentRunId,ToolVersionId,ToolName,Status,
                   ArgumentsJson,ResultContent,ResultSha256,ResultCharacters,
                   StartedAtUtc,FinishedAtUtc,ErrorCode
            FROM AgOrchestrationToolCall
            WHERE RunId=@runId AND NodeId=@nodeId AND Attempt=@Attempt
            ORDER BY Sequence;
            """;
        command.Parameters.AddWithValue("@runId", runId.ToString("D"));
        command.Parameters.AddWithValue("@nodeId", nodeId);
        command.Parameters.AddWithValue("@Attempt", Attempt);
        var values = new List<OrchestrationToolCallRecord>();
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(new OrchestrationToolCallRecord(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                Guid.Parse(reader.GetString(2)),
                reader.GetString(3),
                Enum.Parse<AgentRunEventKind>(reader.GetString(4)),
                reader.GetString(5),
                reader.GetString(6),
                ReadFixedLengthText(reader, 7),
                reader.GetInt32(8),
                DateTimeOffset.Parse(reader.GetString(9)),
                reader.IsDBNull(10) ? null : DateTimeOffset.Parse(reader.GetString(10)),
                reader.GetString(11)));
        }
        return OrchestrationContractCloner.ReadOnly(values);
    }

    private static string ReadFixedLengthText(SqlDataReader reader, int ordinal) =>
        reader.GetString(ordinal).TrimEnd();

    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        return await SqlServerAgentConnection.OpenAsync(_connectionString, cancellationToken);
    }
    private static OrchestrationRunRecord Read(string json) =>
        OrchestrationContractCloner.Clone(
            JsonSerializer.Deserialize<OrchestrationRunRecord>(json, JsonOptions) ??
            throw new InvalidDataException("The SQL Server orchestration run document is empty."));
}

internal static class SqlServerOrchestrationStore
{
    public static string CreateConnectionString(string connectionString)
    {
        return SqlServerAgentConnection.Validate(connectionString);
    }


}
