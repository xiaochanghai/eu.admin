using EU.Core.IServices.Orchestration;
using EU.Core.IServices.Runtime;
using EU.Core.Model.Entity;
using EU.Core.Services;
using Xunit;

#nullable enable

namespace EU.Core.Tests.Service_Test;

public sealed class AgOrchestrationRunPersistence_Should
{
    [Fact]
    public async Task Persist_details_and_terminalize_running_run_once()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgOrchestrationRun),
            typeof(AgOrchestrationRunNode),
            typeof(AgOrchestrationRunDetail),
            typeof(AgOrchestrationNodeAttempt),
            typeof(AgOrchestrationToolCall));
        var service = new AgOrchestrationRunServices(
            fixture.CreateRepository<AgOrchestrationRun>());
        DateTimeOffset startedAt = DateTimeOffset.Parse("2026-08-16T10:00:00Z");
        Guid runId = Guid.NewGuid();
        Guid orchestrationId = Guid.NewGuid();
        var running = new OrchestrationRunRecord(
            runId,
            orchestrationId,
            Guid.NewGuid(),
            "orchestration-persistence-test",
            OrchestrationRunStatus.Running,
            startedAt,
            null,
            Hash('a'),
            string.Empty,
            [
                CreateNode("node-1", OrchestrationNodeRunStatus.Running, startedAt),
                CreateNode("node-2", OrchestrationNodeRunStatus.Pending, null)
            ]);

        await service.SaveAsync(running);
        OrchestrationRunRecord persisted = Assert.IsType<OrchestrationRunRecord>(
            await service.GetAsync(runId));
        Assert.Equal(OrchestrationRunStatus.Running, persisted.Status);
        Assert.Equal(2, persisted.Nodes.Count);
        Assert.Single(await service.ListAsync(orchestrationId, 10));

        Guid agentRunId = Guid.NewGuid();
        var toolCall = new OrchestrationToolCallRecord(
            Guid.NewGuid(),
            agentRunId,
            Guid.NewGuid(),
            "query_business_data",
            AgentRunEventKind.ToolStarted,
            "{}",
            string.Empty,
            Hash('b'),
            0,
            startedAt.AddMilliseconds(20),
            null,
            string.Empty);
        var details = new OrchestrationRunDetails(
            runId,
            orchestrationId,
            "test input",
            string.Empty,
            [
                new OrchestrationNodeAttemptRecord(
                    "node-1",
                    1,
                    agentRunId,
                    "test input",
                    Hash('c'),
                    string.Empty,
                    Hash('d'),
                    OrchestrationNodeRunStatus.Running,
                    startedAt.AddMilliseconds(10),
                    null,
                    string.Empty,
                    [toolCall])
            ]);
        Assert.True(await service.TrySaveRunningDetailsAsync(details));
        OrchestrationRunDetails persistedDetails =
            Assert.IsType<OrchestrationRunDetails>(await service.GetDetailsAsync(runId));
        Assert.Equal("test input", persistedDetails.Input);
        Assert.Equal(
            AgentRunEventKind.ToolStarted,
            Assert.Single(Assert.Single(persistedDetails.Attempts).ToolCalls).Status);

        DateTimeOffset finishedAt = startedAt.AddSeconds(1);
        OrchestrationRunTransitionResult transition = await service.TryFinalizeRunningAsync(
            runId,
            OrchestrationRunStatus.Failed,
            OrchestrationNodeRunStatus.Failed,
            OrchestrationTerminalTransitionPolicy.TerminalizePending,
            finishedAt,
            "TEST_INTERRUPTED",
            detailsIfMissing: null);

        Assert.True(transition.Transitioned);
        Assert.Equal(OrchestrationRunStatus.Failed, transition.Run?.Status);
        Assert.All(transition.Run!.Nodes, node =>
        {
            Assert.Equal(OrchestrationNodeRunStatus.Failed, node.Status);
            Assert.Equal("TEST_INTERRUPTED", node.ErrorCode);
        });
        Assert.False(await service.TrySaveRunningDetailsAsync(details));
        Assert.False((await service.RecoverInterruptedAsync(
            runId,
            finishedAt.AddSeconds(1),
            "LATE_RECOVERY")).Transitioned);

        OrchestrationRunDetails terminalDetails =
            Assert.IsType<OrchestrationRunDetails>(await service.GetDetailsAsync(runId));
        OrchestrationNodeAttemptRecord terminalAttempt = Assert.Single(
            terminalDetails.Attempts);
        Assert.Equal(OrchestrationNodeRunStatus.Failed, terminalAttempt.Status);
        Assert.Equal("TEST_INTERRUPTED", terminalAttempt.ErrorCode);
        OrchestrationToolCallRecord terminalTool = Assert.Single(terminalAttempt.ToolCalls);
        Assert.Equal(AgentRunEventKind.ToolFailed, terminalTool.Status);
        Assert.Equal("TEST_INTERRUPTED", terminalTool.ErrorCode);
    }

    private static OrchestrationNodeRunRecord CreateNode(
        string nodeId,
        OrchestrationNodeRunStatus status,
        DateTimeOffset? startedAt) => new(
        nodeId,
        nodeId,
        Guid.NewGuid(),
        Guid.NewGuid(),
        status,
        status == OrchestrationNodeRunStatus.Running ? 1 : 0,
        startedAt,
        null,
        0,
        Hash(nodeId[0]),
        string.Empty);

    private static string Hash(char value) => new(value, 64);
}
