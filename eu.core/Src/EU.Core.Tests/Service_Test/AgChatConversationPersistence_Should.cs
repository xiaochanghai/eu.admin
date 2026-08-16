using System.Security.Cryptography;
using System.Text;
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

public sealed class AgChatConversationPersistence_Should
{
    [Fact]
    public async Task Save_and_reload_completed_run_with_database_time_rounding()
    {
        using var fixture = new PersistenceFixture();
        DateTimeOffset started = DateTimeOffset.Parse("2026-08-16T03:34:34.9790452Z");
        UnifiedEntryAggregate running = CreateRunningAggregate(started);

        UnifiedEntryAggregate firstSave = await fixture.Service.SaveAsync(running);
        Assert.Equal(1, firstSave.PersistenceRevision);

        // Simulate the precision change observed when a provider binds a high-precision
        // DateTime through a lower-precision database parameter.
        DateTime rounded = started.UtcDateTime.AddMilliseconds(1);
        await fixture.Db.Updateable<AgChatConversation>()
            .SetColumns(value => value.CreatedAtUtc == rounded)
            .Where(value => value.ID == running.Conversation.Id)
            .ExecuteCommandAsync();
        await fixture.Db.Updateable<AgChatMessage>()
            .SetColumns(value => value.CreatedAtUtc == rounded)
            .Where(value => value.ID == running.Messages[0].Id)
            .ExecuteCommandAsync();

        UnifiedEntryAggregate completed = Complete(firstSave, started.AddSeconds(2));
        UnifiedEntryAggregate secondSave = await fixture.Service.SaveAsync(completed);
        UnifiedEntryAggregate? reloaded = await fixture.Service.GetAggregateForOwnerAsync(
            secondSave.Details.EntryRun.Id,
            "tenant-a",
            "user-a");

        Assert.NotNull(reloaded);
        Assert.Equal(2, secondSave.PersistenceRevision);
        Assert.Equal(UnifiedRunStatus.Completed, reloaded.Details.EntryRun.Status);
        Assert.Equal([ConversationMessageRole.User, ConversationMessageRole.Assistant],
            reloaded.Messages.Select(value => value.Role));
        Assert.Equal("model reply", reloaded.Details.EntryRun.Output);
    }

    [Fact]
    public async Task Reconcile_identical_retry_but_reject_stale_conflicting_revision()
    {
        using var fixture = new PersistenceFixture();
        DateTimeOffset started = DateTimeOffset.Parse("2026-08-16T04:00:00.1234567Z");
        UnifiedEntryAggregate firstSave = await fixture.Service.SaveAsync(
            CreateRunningAggregate(started));
        UnifiedEntryAggregate completed = Complete(firstSave, started.AddSeconds(1));
        UnifiedEntryAggregate secondSave = await fixture.Service.SaveAsync(completed);

        UnifiedEntryAggregate reconciled = await fixture.Service.SaveAsync(completed);
        Assert.Equal(secondSave.PersistenceRevision, reconciled.PersistenceRevision);

        UnifiedEntryRunRecord conflictingEntry = completed.Details.EntryRun with
        {
            Output = "different output",
            OutputSha256 = Sha256("different output")
        };
        var conflicting = new UnifiedEntryAggregate(
            completed.Conversation,
            completed.Messages,
            completed.Details.WithEntryRun(conflictingEntry),
            completed.Events,
            completed.PersistenceRevision);

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SaveAsync(conflicting));
        Assert.Equal("The unified entry aggregate revision is stale.", error.Message);
    }

    internal static UnifiedEntryAggregate CreateRunningAggregate(DateTimeOffset started)
    {
        Guid conversationId = Guid.NewGuid();
        Guid entryRunId = Guid.NewGuid();
        Guid correlationId = Guid.NewGuid();
        Guid agentId = Guid.NewGuid();
        Guid agentVersionId = Guid.NewGuid();
        string input = "hello";
        var conversation = new ConversationRecord(
            conversationId,
            input,
            started,
            started)
        {
            TenantId = "tenant-a",
            UserId = "user-a"
        };
        var message = new ConversationMessageRecord(
            Guid.NewGuid(),
            conversationId,
            ConversationMessageRole.User,
            input,
            Sha256(input),
            Encoding.UTF8.GetByteCount(input),
            started)
        {
            Kind = ConversationMessageKind.UserInput
        };
        var entry = new UnifiedEntryRunRecord(
            entryRunId,
            conversationId,
            correlationId,
            agentVersionId,
            UnifiedRunStatus.Running,
            started,
            null,
            null,
            input,
            Sha256(input),
            string.Empty,
            string.Empty,
            string.Empty)
        {
            TenantId = "tenant-a",
            UserId = "user-a"
        };
        var agent = new UnifiedAgentRunRecord(
            Guid.NewGuid(),
            entryRunId,
            null,
            UnifiedAgentRunKind.Main,
            agentId,
            agentVersionId,
            0,
            UnifiedRunStatus.Running,
            started,
            null,
            null,
            input,
            Sha256(input),
            string.Empty,
            string.Empty,
            string.Empty);
        return new UnifiedEntryAggregate(
            conversation,
            [message],
            new UnifiedRunDetails(entry, [agent], [], []),
            []);
    }

    internal static UnifiedEntryAggregate Complete(
        UnifiedEntryAggregate value,
        DateTimeOffset finished)
    {
        string output = "model reply";
        UnifiedEntryRunRecord entry = value.Details.EntryRun with
        {
            Status = UnifiedRunStatus.Completed,
            FinishedAtUtc = finished,
            Duration = finished - value.Details.EntryRun.StartedAtUtc,
            Output = output,
            OutputSha256 = Sha256(output)
        };
        UnifiedAgentRunRecord agent = value.Details.AgentRuns[0] with
        {
            Status = UnifiedRunStatus.Completed,
            FinishedAtUtc = finished,
            Duration = finished - value.Details.AgentRuns[0].StartedAtUtc,
            Output = output,
            OutputSha256 = Sha256(output)
        };
        var assistant = new ConversationMessageRecord(
            Guid.NewGuid(),
            value.Conversation.Id,
            ConversationMessageRole.Assistant,
            output,
            Sha256(output),
            Encoding.UTF8.GetByteCount(output),
            finished)
        {
            Kind = ConversationMessageKind.AssistantNarrative
        };
        string payload = "{\"output\":\"model reply\",\"errorCode\":\"\"}";
        var completedEvent = new UnifiedRunEventRecord(
            Guid.NewGuid(),
            entry.Id,
            1,
            entry.CorrelationId,
            "completed",
            finished,
            null,
            0,
            payload,
            Sha256(payload));
        return new UnifiedEntryAggregate(
            value.Conversation with { UpdatedAtUtc = finished },
            value.Messages.Append(assistant).ToArray(),
            new UnifiedRunDetails(entry, [agent], [], []),
            [completedEvent],
            value.PersistenceRevision);
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class PersistenceFixture : IDisposable
    {
        public PersistenceFixture()
        {
            Db = new SqlSugarScope(new ConnectionConfig
            {
                ConnectionString = "Data Source=:memory:",
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = false
            });
            Db.Ado.Open();
            Db.CodeFirst.InitTables(
                typeof(AgChatConversation),
                typeof(AgChatMessage),
                typeof(AgUnifiedEntryRun),
                typeof(AgUnifiedAgentRun),
                typeof(AgUnifiedOrchestrationLink),
                typeof(AgUnifiedToolCall),
                typeof(AgUnifiedRunEvent));
            var unitOfWork = new UnitOfWorkManage(
                Db,
                NullLogger<UnitOfWorkManage>.Instance);
            var repository = new BaseRepository<AgChatConversation>(unitOfWork);
            Service = new AgChatConversationServices(repository);
        }

        public SqlSugarScope Db { get; }

        public AgChatConversationServices Service { get; }

        public void Dispose()
        {
            Db.Dispose();
        }
    }
}
