using EU.Core.Agent.Application.Abstractions.Security;
using EU.Core.Agent.Application.Approvals;
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
        string[] tableNames =
        [
            "AgChatConversation",
            "AgChatMessage",
            "AgUnifiedEntryRun",
            "AgUnifiedAgentRun",
            "AgUnifiedOrchestrationLink",
            "AgUnifiedToolCall",
            "AgUnifiedRunEvent",
            "AgApiIdempotency",
            "AgToolApprovalRequest",
            "AgToolApprovalPayload",
            "AgToolApprovalDecision",
            "AgToolApprovalExecutionResult"
        ];
        string[] missing = tableNames
            .Where(name => !db.DbMaintenance.IsAnyTable(name, false))
            .ToArray();
        Assert.True(missing.Length == 0, $"Missing Agent tables: {string.Join(", ", missing)}");
    }

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
