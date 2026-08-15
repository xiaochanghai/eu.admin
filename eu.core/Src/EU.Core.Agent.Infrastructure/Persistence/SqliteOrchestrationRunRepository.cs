using System.Text.Json;
using EU.Core.Agent.Application.Orchestration;
using EU.Core.Agent.Application.Runtime;
using Microsoft.Data.Sqlite;

namespace EU.Core.Agent.Infrastructure.Persistence;

internal sealed class SqliteOrchestrationRunRepositoryHooks
{
    public Func<CancellationToken, Task>? BeforeInterruptedSummaryWriteAsync { get; init; }
}

public sealed class SqliteOrchestrationRunRepository : IOrchestrationRunRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    private readonly string _connectionString;
    private readonly SqliteOrchestrationRunRepositoryHooks _hooks;

    public SqliteOrchestrationRunRepository(string databasePath)
        : this(databasePath, new SqliteOrchestrationRunRepositoryHooks())
    {
    }

    internal SqliteOrchestrationRunRepository(
        string databasePath,
        SqliteOrchestrationRunRepositoryHooks hooks)
    {
        _connectionString = SqliteOrchestrationStore.CreateConnectionString(databasePath);
        _hooks = hooks;
        SqliteOrchestrationStore.EnsureCreated(_connectionString);
    }

    public async Task SaveAsync(OrchestrationRunRecord value, CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO orchestration_runs(id,orchestration_id,started_at_utc,document_json)
            VALUES($id,$orchestrationId,$started,$json)
            ON CONFLICT(id) DO UPDATE SET document_json=excluded.document_json
            WHERE json_extract(orchestration_runs.document_json,'$.status')=$running;
            """;
        command.Parameters.AddWithValue("$id", value.Id.ToString("D"));
        command.Parameters.AddWithValue("$orchestrationId", value.OrchestrationId.ToString("D"));
        command.Parameters.AddWithValue("$started", value.StartedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(value, JsonOptions));
        command.Parameters.AddWithValue("$running", (int)OrchestrationRunStatus.Running);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<OrchestrationRunRecord?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT document_json FROM orchestration_runs WHERE id=$id;";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is string json ? Read(json) : null;
    }

    public async Task<IReadOnlyList<OrchestrationRunRecord>> ListAsync(
        Guid orchestrationId, int take, CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT document_json FROM orchestration_runs
            WHERE orchestration_id=$id ORDER BY started_at_utc DESC LIMIT $take;
            """;
        command.Parameters.AddWithValue("$id", orchestrationId.ToString("D"));
        command.Parameters.AddWithValue("$take", Math.Clamp(take, 1, 100));
        var values = new List<OrchestrationRunRecord>();
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) values.Add(Read(reader.GetString(0)));
        return OrchestrationContractCloner.ReadOnly(values);
    }

    public async Task SaveDetailsAsync(
        OrchestrationRunDetails value,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteTransaction transaction =
            (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await WriteDetailsAsync(connection, transaction, value, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task WriteDetailsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        OrchestrationRunDetails value,
        CancellationToken cancellationToken)
    {
        await using (SqliteCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO orchestration_run_details(run_id,orchestration_id,input_text,output_text)
                VALUES($runId,$orchestrationId,$input,$output)
                ON CONFLICT(run_id) DO UPDATE SET
                  orchestration_id=excluded.orchestration_id,
                  input_text=excluded.input_text,
                  output_text=excluded.output_text;
                DELETE FROM orchestration_tool_calls WHERE run_id=$runId;
                DELETE FROM orchestration_node_attempts WHERE run_id=$runId;
                """;
            command.Parameters.AddWithValue("$runId", value.RunId.ToString("D"));
            command.Parameters.AddWithValue("$orchestrationId", value.OrchestrationId.ToString("D"));
            command.Parameters.AddWithValue("$input", value.Input);
            command.Parameters.AddWithValue("$output", value.Output);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        for (int attemptSequence = 0; attemptSequence < value.Attempts.Count; attemptSequence++)
        {
            OrchestrationNodeAttemptRecord attempt = value.Attempts[attemptSequence];
            await InsertAttemptAsync(
                connection, transaction, value.RunId, attemptSequence, attempt, cancellationToken);
            for (int toolSequence = 0; toolSequence < attempt.ToolCalls.Count; toolSequence++)
            {
                await InsertToolAsync(
                    connection, transaction, value.RunId, attempt.NodeId, attempt.Attempt,
                    toolSequence, attempt.ToolCalls[toolSequence], cancellationToken);
            }
        }
    }

    public async Task<OrchestrationRunDetails?> GetDetailsAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        Guid orchestrationId;
        string input;
        string output;
        await using (SqliteCommand command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT orchestration_id,input_text,output_text
                FROM orchestration_run_details WHERE run_id=$runId;
                """;
            command.Parameters.AddWithValue("$runId", runId.ToString("D"));
            await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
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
        await using (SqliteCommand command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT node_id,attempt,agent_run_id,input_text,input_sha256,
                       output_text,output_sha256,status,started_at_utc,
                       finished_at_utc,error_code
                FROM orchestration_node_attempts
                WHERE run_id=$runId ORDER BY sequence;
                """;
            command.Parameters.AddWithValue("$runId", runId.ToString("D"));
            await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                attemptRows.Add((
                    reader.GetString(0),
                    reader.GetInt32(1),
                    Guid.Parse(reader.GetString(2)),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5),
                    reader.GetString(6),
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
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteTransaction transaction =
            connection.BeginTransaction(deferred: false);
        bool isRunning;
        await using (SqliteCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                SELECT json_extract(document_json,'$.status')
                FROM orchestration_runs WHERE id=$id;
                """;
            command.Parameters.AddWithValue("$id", value.RunId.ToString("D"));
            object? result = await command.ExecuteScalarAsync(cancellationToken);
            isRunning = result is long status
                && status == (long)OrchestrationRunStatus.Running;
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
                "A terminal run status is required.");
        }
        if (nodeStatus is OrchestrationNodeRunStatus.Pending
            or OrchestrationNodeRunStatus.Running)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nodeStatus),
                "A terminal node status is required.");
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
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteTransaction transaction =
            connection.BeginTransaction(deferred: false);
        OrchestrationRunRecord? value;
        string? documentJson;
        await using (SqliteCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText =
                "SELECT document_json FROM orchestration_runs WHERE id=$id;";
            command.Parameters.AddWithValue("$id", runId.ToString("D"));
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
            await using (SqliteCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = """
                    SELECT EXISTS(
                      SELECT 1 FROM orchestration_run_details WHERE run_id=$id);
                    """;
                command.Parameters.AddWithValue("$id", runId.ToString("D"));
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
        await using (SqliteCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE orchestration_node_attempts
                SET status=$nodeStatus,finished_at_utc=$finished,error_code=$error
                WHERE run_id=$id
                  AND (status=$running
                    OR ($terminalizePending=1 AND status=$pending));
                UPDATE orchestration_tool_calls
                SET status=$toolFailed,finished_at_utc=$finished,error_code=$error
                WHERE run_id=$id AND status=$toolStarted;
                """;
            command.Parameters.AddWithValue("$id", runId.ToString("D"));
            command.Parameters.AddWithValue(
                "$nodeStatus",
                nodeStatus.ToString());
            command.Parameters.AddWithValue(
                "$pending",
                OrchestrationNodeRunStatus.Pending.ToString());
            command.Parameters.AddWithValue(
                "$running",
                OrchestrationNodeRunStatus.Running.ToString());
            command.Parameters.AddWithValue(
                "$terminalizePending",
                transitionPolicy
                    == OrchestrationTerminalTransitionPolicy.TerminalizePending
                    ? 1
                    : 0);
            command.Parameters.AddWithValue(
                "$toolFailed",
                AgentRunEventKind.ToolFailed.ToString());
            command.Parameters.AddWithValue(
                "$toolStarted",
                AgentRunEventKind.ToolStarted.ToString());
            command.Parameters.AddWithValue("$finished", finishedAt);
            command.Parameters.AddWithValue("$error", errorCode);
            await command.ExecuteNonQueryAsync(CancellationToken.None);
        }

        if (invokeRecoveryHook
            && _hooks.BeforeInterruptedSummaryWriteAsync is not null)
        {
            await _hooks.BeforeInterruptedSummaryWriteAsync(CancellationToken.None);
        }

        await using (SqliteCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE orchestration_runs SET document_json=$terminal
                WHERE id=$id AND document_json=$expected;
                """;
            command.Parameters.AddWithValue("$id", runId.ToString("D"));
            command.Parameters.AddWithValue(
                "$terminal",
                JsonSerializer.Serialize(terminal, JsonOptions));
            command.Parameters.AddWithValue("$expected", documentJson);
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
        OrchestrationNodeRunStatus status,
        OrchestrationTerminalTransitionPolicy transitionPolicy) =>
        status == OrchestrationNodeRunStatus.Running
        || (status == OrchestrationNodeRunStatus.Pending
            && transitionPolicy
                == OrchestrationTerminalTransitionPolicy.TerminalizePending);

    private static async Task InsertAttemptAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid runId,
        int sequence,
        OrchestrationNodeAttemptRecord value,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO orchestration_node_attempts(
              run_id,node_id,attempt,sequence,agent_run_id,input_text,input_sha256,
              output_text,output_sha256,status,started_at_utc,finished_at_utc,error_code)
            VALUES($runId,$nodeId,$attempt,$sequence,$agentRunId,$input,$inputSha,
              $output,$outputSha,$status,$started,$finished,$error);
            """;
        command.Parameters.AddWithValue("$runId", runId.ToString("D"));
        command.Parameters.AddWithValue("$nodeId", value.NodeId);
        command.Parameters.AddWithValue("$attempt", value.Attempt);
        command.Parameters.AddWithValue("$sequence", sequence);
        command.Parameters.AddWithValue("$agentRunId", value.AgentRunId.ToString("D"));
        command.Parameters.AddWithValue("$input", value.Input);
        command.Parameters.AddWithValue("$inputSha", value.InputSha256);
        command.Parameters.AddWithValue("$output", value.Output);
        command.Parameters.AddWithValue("$outputSha", value.OutputSha256);
        command.Parameters.AddWithValue("$status", value.Status.ToString());
        command.Parameters.AddWithValue("$started", value.StartedAtUtc.ToString("O"));
        command.Parameters.AddWithValue(
            "$finished",
            value.FinishedAtUtc is null ? DBNull.Value : value.FinishedAtUtc.Value.ToString("O"));
        command.Parameters.AddWithValue("$error", value.ErrorCode);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertToolAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid runId,
        string nodeId,
        int attempt,
        int sequence,
        OrchestrationToolCallRecord value,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO orchestration_tool_calls(
              tool_call_id,run_id,node_id,attempt,sequence,agent_run_id,tool_version_id,
              tool_name,status,arguments_json,result_content,result_sha256,result_characters,
              started_at_utc,finished_at_utc,error_code)
            VALUES($toolCallId,$runId,$nodeId,$attempt,$sequence,$agentRunId,$toolVersionId,
              $toolName,$status,$arguments,$result,$resultSha,$resultCharacters,
              $started,$finished,$error);
            """;
        command.Parameters.AddWithValue("$toolCallId", value.ToolCallId.ToString("D"));
        command.Parameters.AddWithValue("$runId", runId.ToString("D"));
        command.Parameters.AddWithValue("$nodeId", nodeId);
        command.Parameters.AddWithValue("$attempt", attempt);
        command.Parameters.AddWithValue("$sequence", sequence);
        command.Parameters.AddWithValue("$agentRunId", value.AgentRunId.ToString("D"));
        command.Parameters.AddWithValue("$toolVersionId", value.ToolVersionId.ToString("D"));
        command.Parameters.AddWithValue("$toolName", value.ToolName);
        command.Parameters.AddWithValue("$status", value.Status.ToString());
        command.Parameters.AddWithValue("$arguments", value.ArgumentsJson);
        command.Parameters.AddWithValue("$result", value.ResultContent);
        command.Parameters.AddWithValue("$resultSha", value.ResultSha256);
        command.Parameters.AddWithValue("$resultCharacters", value.ResultCharacters);
        command.Parameters.AddWithValue("$started", value.StartedAtUtc.ToString("O"));
        command.Parameters.AddWithValue(
            "$finished",
            value.FinishedAtUtc is null ? DBNull.Value : value.FinishedAtUtc.Value.ToString("O"));
        command.Parameters.AddWithValue("$error", value.ErrorCode);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<OrchestrationToolCallRecord>> ReadToolsAsync(
        SqliteConnection connection,
        Guid runId,
        string nodeId,
        int attempt,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT tool_call_id,agent_run_id,tool_version_id,tool_name,status,
                   arguments_json,result_content,result_sha256,result_characters,
                   started_at_utc,finished_at_utc,error_code
            FROM orchestration_tool_calls
            WHERE run_id=$runId AND node_id=$nodeId AND attempt=$attempt
            ORDER BY sequence;
            """;
        command.Parameters.AddWithValue("$runId", runId.ToString("D"));
        command.Parameters.AddWithValue("$nodeId", nodeId);
        command.Parameters.AddWithValue("$attempt", attempt);
        var values = new List<OrchestrationToolCallRecord>();
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
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
                reader.GetString(7),
                reader.GetInt32(8),
                DateTimeOffset.Parse(reader.GetString(9)),
                reader.IsDBNull(10) ? null : DateTimeOffset.Parse(reader.GetString(10)),
                reader.GetString(11)));
        }
        return OrchestrationContractCloner.ReadOnly(values);
    }
    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
    private static OrchestrationRunRecord Read(string json) =>
        OrchestrationContractCloner.Clone(
            JsonSerializer.Deserialize<OrchestrationRunRecord>(json, JsonOptions) ??
            throw new InvalidDataException("The SQLite orchestration run document is empty."));
}

internal static class SqliteOrchestrationStore
{
    public static string CreateConnectionString(string databasePath)
    {
        string fullPath = Path.GetFullPath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        return new SqliteConnectionStringBuilder
        {
            DataSource = fullPath, Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared, Pooling = false, DefaultTimeout = 5
        }.ToString();
    }

    public static void EnsureCreated(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=WAL;
            PRAGMA busy_timeout=5000;
            CREATE TABLE IF NOT EXISTS orchestration_definitions(
              id TEXT NOT NULL PRIMARY KEY, code TEXT NOT NULL UNIQUE COLLATE BINARY,
              logical_revision INTEGER NOT NULL, document_json TEXT NOT NULL CHECK(json_valid(document_json))
            ) WITHOUT ROWID;
            CREATE TABLE IF NOT EXISTS orchestration_runs(
              id TEXT NOT NULL PRIMARY KEY, orchestration_id TEXT NOT NULL,
              started_at_utc TEXT NOT NULL, document_json TEXT NOT NULL CHECK(json_valid(document_json))
            ) WITHOUT ROWID;
            CREATE INDEX IF NOT EXISTS ix_orchestration_runs_owner
              ON orchestration_runs(orchestration_id,started_at_utc DESC);
            CREATE TABLE IF NOT EXISTS orchestration_run_details(
              run_id TEXT NOT NULL PRIMARY KEY,
              orchestration_id TEXT NOT NULL,
              input_text TEXT NOT NULL,
              output_text TEXT NOT NULL
            ) WITHOUT ROWID;
            CREATE TABLE IF NOT EXISTS orchestration_node_attempts(
              run_id TEXT NOT NULL,
              node_id TEXT NOT NULL,
              attempt INTEGER NOT NULL,
              sequence INTEGER NOT NULL,
              agent_run_id TEXT NOT NULL,
              input_text TEXT NOT NULL,
              input_sha256 TEXT NOT NULL,
              output_text TEXT NOT NULL,
              output_sha256 TEXT NOT NULL,
              status TEXT NOT NULL,
              started_at_utc TEXT NOT NULL,
              finished_at_utc TEXT NULL,
              error_code TEXT NOT NULL,
              PRIMARY KEY(run_id,node_id,attempt)
            ) WITHOUT ROWID;
            CREATE INDEX IF NOT EXISTS ix_orchestration_attempt_order
              ON orchestration_node_attempts(run_id,sequence);
            CREATE TABLE IF NOT EXISTS orchestration_tool_calls(
              tool_call_id TEXT NOT NULL PRIMARY KEY,
              run_id TEXT NOT NULL,
              node_id TEXT NOT NULL,
              attempt INTEGER NOT NULL,
              sequence INTEGER NOT NULL,
              agent_run_id TEXT NOT NULL,
              tool_version_id TEXT NOT NULL,
              tool_name TEXT NOT NULL,
              status TEXT NOT NULL,
              arguments_json TEXT NOT NULL,
              result_content TEXT NOT NULL,
              result_sha256 TEXT NOT NULL,
              result_characters INTEGER NOT NULL,
              started_at_utc TEXT NOT NULL,
              finished_at_utc TEXT NULL,
              error_code TEXT NOT NULL
            ) WITHOUT ROWID;
            CREATE INDEX IF NOT EXISTS ix_orchestration_tool_order
              ON orchestration_tool_calls(run_id,node_id,attempt,sequence);
            """;
        command.ExecuteNonQuery();
    }
}
