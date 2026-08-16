using EU.Core.Agent.Application.Abstractions.Auditing;
using EU.Core.Agent.Application.Abstractions.Security;
using EU.Core.Agent.Application.Approvals;
using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Application.Runtime;
using EU.Core.Agent.Application.UnifiedEntry;
using EU.Core.Model.Entity;
using EU.Core.Repository.Base;
using EU.Core.Repository.UnitOfWorks;
using EU.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using SqlSugar;
using Xunit;

#nullable enable

namespace EU.Core.Tests.Service_Test;

public sealed class AgSqlServerPersistence_Should
{
    [SqlServerIntegrationFact]
    [Trait("Category", "SqlServerIntegration")]
    public async Task Persist_agent_state_through_real_sql_server()
    {
        string connectionString = Environment.GetEnvironmentVariable(
            SqlServerIntegrationFactAttribute.ConnectionEnvironmentVariable)
            ?? throw new InvalidOperationException(
                "The SQL Server integration connection string is unavailable.");

        using var db = new SqlSugarScope(new ConnectionConfig
        {
            ConnectionString = connectionString,
            DbType = DbType.SqlServer,
            IsAutoCloseConnection = false
        });
        db.Ado.Open();
        EnsureTablesExist(db);
        AssertAllTablesHaveIdPrimaryKeys(db);
        AssertNoUnicodeCharacterColumns(db);
        AssertAllTablesHaveDescriptions(db);
        AssertAllColumnsHaveDescriptions(db);

        DateTimeOffset started = DateTimeOffset.UtcNow;
        UnifiedEntryAggregate running =
            AgChatConversationPersistence_Should.CreateRunningAggregate(started);
        HttpIdempotencyRecord pending =
            AgApiIdempotencyPersistence_Should.CreatePending(
                $"sql-scope-{Guid.NewGuid():N}",
                $"sql-request-{Guid.NewGuid():N}",
                started);
        ToolApprovalRequestRecord approval =
            AgToolApprovalPersistence_Should.CreatePending(started);
        Guid operationAuditId = Guid.NewGuid();
        var operationStarted = new AgentOperationAuditRecord(
            operationAuditId,
            started,
            "sql-test-tenant",
            "sql-test-user",
            $"sql-correlation-{Guid.NewGuid():N}",
            "AgentRead",
            "GET",
            "/api/agents",
            0,
            "Started",
            null,
            0);
        Guid auditRunId = Guid.NewGuid();
        Guid auditAgentId = Guid.NewGuid();
        var auditRunning = new AgentRunAuditRecord(
            auditRunId,
            auditAgentId,
            Guid.NewGuid(),
            $"sql-audit-agent-{Guid.NewGuid():N}",
            AgentRunStatus.Running,
            started,
            null,
            new string('a', 64),
            0,
            0,
            string.Empty,
            []);

        try
        {
            var chat = new AgChatConversationServices(
                CreateRepository<AgChatConversation>(db));
            UnifiedEntryAggregate firstSave = await chat.SaveAsync(running);
            UnifiedEntryAggregate completed =
                AgChatConversationPersistence_Should.Complete(
                firstSave,
                started.AddSeconds(1));
            UnifiedEntryAggregate secondSave = await chat.SaveAsync(completed);
            UnifiedEntryAggregate? reloaded = await chat.GetAggregateForOwnerAsync(
                secondSave.Details.EntryRun.Id,
                "tenant-a",
                "user-a");
            Assert.NotNull(reloaded);
            Assert.Equal(UnifiedRunStatus.Completed, reloaded.Details.EntryRun.Status);
            Assert.Equal(2, reloaded.PersistenceRevision);

            var idempotency = new AgApiIdempotencyServices(
                CreateRepository<AgApiIdempotency>(db));
            Assert.True((await idempotency.BeginAsync(pending, started)).Acquired);
            Assert.True(await idempotency.CompleteAsync(
                pending.ScopeSha256,
                pending.RequestSha256,
                200,
                "application/json",
                string.Empty,
                [1, 2, 3]));
            HttpIdempotencyBeginResult replay = await idempotency.BeginAsync(
                pending,
                started.AddMilliseconds(100));
            Assert.False(replay.Acquired);
            Assert.Equal(HttpIdempotencyStatus.Completed, replay.Record.Status);
            Assert.Equal([1, 2, 3], replay.Record.ResponseBody);

            var approvals = new AgToolApprovalRequestServices(
                CreateRepository<AgToolApprovalRequest>(db));
            Assert.True(await approvals.TryCreateAsync(
                approval,
                "enc:v1:sql-server-test"));
            ToolApprovalRequestRecord persistedApproval =
                await approvals.GetAsync(approval.Id, approval.TenantId)
                ?? throw new InvalidOperationException(
                    "The SQL Server approval record was not persisted.");
            ToolApprovalRequestRecord approved = ToolApprovalStateMachine.Approve(
                persistedApproval,
                "sql-test-approver",
                "integration test",
                started.AddSeconds(1));
            Assert.True(await approvals.TryReplaceAsync(approved, 0));
            ToolApprovalExecutionClaim? claim =
                await approvals.TryClaimExecutionAsync(
                    approval.Id,
                    approval.TenantId,
                    1,
                    started.AddSeconds(2));
            Assert.NotNull(claim);
            DateTimeOffset finishedAt = started.AddSeconds(3);
            ToolApprovalRequestRecord consumed = ToolApprovalStateMachine.Complete(
                claim.Request,
                succeeded: true,
                errorCode: string.Empty,
                finishedAt);
            const string protectedContent = "enc:v1:sql-server-result";
            var executionResult = new ToolApprovalExecutionResultRecord(
                approval.Id,
                approval.TenantId,
                Succeeded: true,
                Blocked: false,
                protectedContent,
                AgToolApprovalPersistence_Should.Sha256(protectedContent),
                AgToolApprovalPersistence_Should.Sha256("sql-server-result"),
                string.Empty,
                finishedAt);
            Assert.True(await approvals.TryCompleteExecutionAsync(
                consumed,
                expectedLogicalRevision: 2,
                executionResult));
            ToolApprovalRequestRecord? reloadedApproval = await approvals.GetAsync(
                approval.Id,
                approval.TenantId);
            ToolApprovalExecutionResultRecord? reloadedExecutionResult =
                await approvals.GetExecutionResultAsync(
                    approval.Id,
                    approval.TenantId);
            Assert.NotNull(reloadedApproval);
            Assert.NotNull(reloadedExecutionResult);
            Assert.Equal(ToolApprovalStatus.Consumed, reloadedApproval.Status);
            Assert.Equal(
                executionResult with
                {
                    FinishedAtUtc = reloadedExecutionResult.FinishedAtUtc
                },
                reloadedExecutionResult);
            long finishedAtDifference = Math.Abs(
                (executionResult.FinishedAtUtc - reloadedExecutionResult.FinishedAtUtc).Ticks);
            Assert.InRange(
                finishedAtDifference,
                0,
                TimeSpan.FromMilliseconds(4).Ticks);

            var operationAudits = new AgAgentOperationAuditServices(
                CreateRepository<AgAgentOperationAudit>(db));
            await operationAudits.SaveAsync(operationStarted);
            await operationAudits.SaveAsync(operationStarted with
            {
                StatusCode = 200,
                Outcome = "Succeeded",
                DurationMilliseconds = 15
            });
            AgentOperationAuditRecord persistedOperation = Assert.Single(
                await operationAudits.ListAsync("sql-test-tenant", 100),
                value => value.Id == operationAuditId);
            Assert.Equal("Succeeded", persistedOperation.Outcome);
            Assert.Equal(200, persistedOperation.StatusCode);

            var runAudits = new AgAgentRunAuditServices(
                CreateRepository<AgAgentRunAudit>(db));
            await runAudits.SaveAsync(auditRunning);
            var auditToolCall = new AgentToolCallAuditRecord(
                Guid.NewGuid(),
                "sql_test_tool",
                McpToolRisk.ReadOnly,
                AgentRunEventKind.ToolSucceeded,
                started.AddMilliseconds(10),
                started.AddMilliseconds(20),
                string.Empty);
            await runAudits.SaveAsync(auditRunning with
            {
                Status = AgentRunStatus.Completed,
                FinishedAtUtc = started.AddSeconds(1),
                OutputCharacters = 8,
                ToolCallCount = 1,
                ToolCalls = [auditToolCall]
            });
            AgentRunAuditRecord persistedRunAudit = Assert.Single(
                await runAudits.ListAsync(auditAgentId, 10));
            Assert.Equal(AgentRunStatus.Completed, persistedRunAudit.Status);
            Assert.Equal(
                auditToolCall.ToolVersionId,
                Assert.Single(persistedRunAudit.ToolCalls).ToolVersionId);
        }
        finally
        {
            await DeleteChatRowsAsync(db, running);
            await db.Deleteable<AgApiIdempotency>()
                .Where(value => value.ScopeSha256 == pending.ScopeSha256)
                .ExecuteCommandAsync();
            await db.Deleteable<AgToolApprovalExecutionResult>()
                .Where(value => value.ApprovalId == approval.Id)
                .ExecuteCommandAsync();
            await db.Deleteable<AgToolApprovalDecision>()
                .Where(value => value.ApprovalId == approval.Id)
                .ExecuteCommandAsync();
            await db.Deleteable<AgToolApprovalPayload>()
                .Where(value => value.ApprovalId == approval.Id)
                .ExecuteCommandAsync();
            await db.Deleteable<AgToolApprovalRequest>()
                .Where(value => value.ID == approval.Id)
                .ExecuteCommandAsync();
            await db.Deleteable<AgAgentToolCallAudit>()
                .Where(value => value.RunId == auditRunId)
                .ExecuteCommandAsync();
            await db.Deleteable<AgAgentRunAudit>()
                .Where(value => value.ID == auditRunId)
                .ExecuteCommandAsync();
            await db.Deleteable<AgAgentOperationAudit>()
                .Where(value => value.ID == operationAuditId)
                .ExecuteCommandAsync();
        }
    }

    private static BaseRepository<TEntity> CreateRepository<TEntity>(
        SqlSugarScope db)
        where TEntity : class, new()
    {
        var unitOfWork = new UnitOfWorkManage(
            db,
            NullLogger<UnitOfWorkManage>.Instance);
        return new BaseRepository<TEntity>(unitOfWork);
    }

    private static void EnsureTablesExist(SqlSugarScope db)
    {
        string[] missing = GetCurrentAgentTableNames(db)
            .Where(name => !db.DbMaintenance.IsAnyTable(name, false))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Assert.True(missing.Length == 0, $"Missing Agent tables: {string.Join(", ", missing)}");
    }

    private static void AssertNoUnicodeCharacterColumns(SqlSugarScope db)
    {
        const string sql = """
            SELECT
                TABLE_NAME AS TableName,
                COLUMN_NAME AS ColumnName,
                DATA_TYPE AS DataType
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'dbo'
              AND TABLE_NAME LIKE 'Ag%'
              AND DATA_TYPE IN ('nvarchar', 'nchar', 'ntext')
            ORDER BY TABLE_NAME, ORDINAL_POSITION
            """;
        HashSet<string> currentAgentTables = GetCurrentAgentTableNames(db);
        List<UnicodeCharacterColumn> columns = db.Ado
            .SqlQuery<UnicodeCharacterColumn>(sql)
            .Where(value => currentAgentTables.Contains(value.TableName))
            .ToList();
        Assert.True(
            columns.Count == 0,
            $"Agent columns must use varchar/char types: {string.Join(", ", columns.Select(value => $"{value.TableName}.{value.ColumnName} ({value.DataType})"))}");
    }

    private static void AssertAllTablesHaveIdPrimaryKeys(SqlSugarScope db)
    {
        const string sql = """
            SELECT tables.name AS TableName
            FROM sys.tables AS tables
            INNER JOIN sys.schemas AS schemas
                ON schemas.schema_id = tables.schema_id
            WHERE schemas.name = N'dbo'
              AND tables.name LIKE N'Ag%'
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM sys.indexes AS indexes
                  INNER JOIN sys.index_columns AS indexColumns
                      ON indexColumns.object_id = indexes.object_id
                     AND indexColumns.index_id = indexes.index_id
                  INNER JOIN sys.columns AS columns
                      ON columns.object_id = indexColumns.object_id
                     AND columns.column_id = indexColumns.column_id
                  WHERE indexes.object_id = tables.object_id
                    AND indexes.is_primary_key = 1
                    AND columns.name = N'ID'
              )
            ORDER BY tables.name
            """;
        HashSet<string> currentAgentTables = GetCurrentAgentTableNames(db);
        List<TableWithoutIdPrimaryKey> tables = db.Ado
            .SqlQuery<TableWithoutIdPrimaryKey>(sql)
            .Where(value => currentAgentTables.Contains(value.TableName))
            .ToList();
        Assert.True(
            tables.Count == 0,
            $"Agent tables must have primary keys on ID: {string.Join(", ", tables.Select(value => value.TableName))}");
    }

    private static void AssertAllColumnsHaveDescriptions(SqlSugarScope db)
    {
        const string sql = """
            SELECT
                tables.name AS TableName,
                columns.name AS ColumnName
            FROM sys.tables AS tables
            INNER JOIN sys.schemas AS schemas
                ON schemas.schema_id = tables.schema_id
            INNER JOIN sys.columns AS columns
                ON columns.object_id = tables.object_id
            LEFT JOIN sys.extended_properties AS descriptions
                ON descriptions.major_id = tables.object_id
               AND descriptions.minor_id = columns.column_id
               AND descriptions.name = N'MS_Description'
            WHERE schemas.name = N'dbo'
              AND tables.name LIKE N'Ag%'
              AND NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(max), descriptions.value))), N'') IS NULL
            ORDER BY tables.name, columns.column_id
            """;
        HashSet<string> currentAgentTables = GetCurrentAgentTableNames(db);
        List<UndocumentedColumn> columns = db.Ado
            .SqlQuery<UndocumentedColumn>(sql)
            .Where(value => currentAgentTables.Contains(value.TableName))
            .ToList();
        Assert.True(
            columns.Count == 0,
            $"Agent columns must have MS_Description values: {string.Join(", ", columns.Select(value => $"{value.TableName}.{value.ColumnName}"))}");
    }

    private static void AssertAllTablesHaveDescriptions(SqlSugarScope db)
    {
        const string sql = """
            SELECT tables.name AS TableName
            FROM sys.tables AS tables
            INNER JOIN sys.schemas AS schemas
                ON schemas.schema_id = tables.schema_id
            LEFT JOIN sys.extended_properties AS descriptions
                ON descriptions.major_id = tables.object_id
               AND descriptions.minor_id = 0
               AND descriptions.name = N'MS_Description'
            WHERE schemas.name = N'dbo'
              AND tables.name LIKE N'Ag%'
              AND NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(max), descriptions.value))), N'') IS NULL
            ORDER BY tables.name
            """;
        HashSet<string> currentAgentTables = GetCurrentAgentTableNames(db);
        List<UndocumentedTable> tables = db.Ado
            .SqlQuery<UndocumentedTable>(sql)
            .Where(value => currentAgentTables.Contains(value.TableName))
            .ToList();
        Assert.True(
            tables.Count == 0,
            $"Agent tables must have MS_Description values: {string.Join(", ", tables.Select(value => value.TableName))}");
    }

    private static HashSet<string> GetCurrentAgentTableNames(SqlSugarScope db) =>
        typeof(AgAgentDefinition).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } &&
                           string.Equals(
                               type.Namespace,
                               "EU.Core.Model.Entity",
                               StringComparison.Ordinal) &&
                           type.Name.StartsWith("Ag", StringComparison.Ordinal))
            .Select(type => db.EntityMaintenance.GetEntityInfo(type).DbTableName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static async Task DeleteChatRowsAsync(
        SqlSugarScope db,
        UnifiedEntryAggregate running)
    {
        Guid entryRunId = running.Details.EntryRun.Id;
        Guid conversationId = running.Conversation.Id;
        await db.Deleteable<AgUnifiedRunEvent>()
            .Where(value => value.EntryRunId == entryRunId)
            .ExecuteCommandAsync();
        await db.Deleteable<AgUnifiedToolCall>()
            .Where(value => value.EntryRunId == entryRunId)
            .ExecuteCommandAsync();
        await db.Deleteable<AgUnifiedOrchestrationLink>()
            .Where(value => value.EntryRunId == entryRunId)
            .ExecuteCommandAsync();
        await db.Deleteable<AgUnifiedAgentRun>()
            .Where(value => value.EntryRunId == entryRunId)
            .ExecuteCommandAsync();
        await db.Deleteable<AgUnifiedEntryRun>()
            .Where(value => value.ID == entryRunId)
            .ExecuteCommandAsync();
        await db.Deleteable<AgChatMessage>()
            .Where(value => value.ConversationId == conversationId)
            .ExecuteCommandAsync();
        await db.Deleteable<AgChatConversation>()
            .Where(value => value.ID == conversationId)
            .ExecuteCommandAsync();
    }

    private sealed class UnicodeCharacterColumn
    {
        public string TableName { get; set; } = string.Empty;

        public string ColumnName { get; set; } = string.Empty;

        public string DataType { get; set; } = string.Empty;
    }

    private sealed class UndocumentedColumn
    {
        public string TableName { get; set; } = string.Empty;

        public string ColumnName { get; set; } = string.Empty;
    }

    private sealed class UndocumentedTable
    {
        public string TableName { get; set; } = string.Empty;
    }

    private sealed class TableWithoutIdPrimaryKey
    {
        public string TableName { get; set; } = string.Empty;
    }
}

public sealed class SqlServerIntegrationFactAttribute : FactAttribute
{
    public const string ConnectionEnvironmentVariable =
        "EUCORE_AGENT_SQLSERVER_INTEGRATION";

    public SqlServerIntegrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(
            ConnectionEnvironmentVariable)))
        {
            Skip = $"Set {ConnectionEnvironmentVariable} to run the SQL Server integration test.";
        }
    }
}
