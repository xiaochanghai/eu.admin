using EU.Core.IServices.Abstractions.Auditing;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.Runtime;
using EU.Core.Model.Entity;
using EU.Core.Services;
using Xunit;

#nullable enable

namespace EU.Core.Tests.Service_Test;

public sealed class AgAgentAuditPersistence_Should
{
    [Fact]
    public async Task Complete_started_operation_and_keep_tenant_lists_isolated()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgAgentOperationAudit));
        var service = new AgAgentOperationAuditServices(
            fixture.CreateRepository<AgAgentOperationAudit>());
        Guid id = Guid.NewGuid();
        DateTimeOffset occurredAt = DateTimeOffset.Parse("2026-08-16T07:00:00Z");
        var started = new AgentOperationAuditRecord(
            id,
            occurredAt,
            "tenant-a",
            "user-a",
            "correlation-a",
            "AgentRead",
            "GET",
            "/api/agents",
            0,
            "Started",
            null,
            0);

        await service.SaveAsync(started);
        await service.SaveAsync(started with
        {
            StatusCode = 200,
            Outcome = "Succeeded",
            DurationMilliseconds = 25
        });

        AgentOperationAuditRecord persisted = Assert.Single(
            await service.ListAsync("tenant-a", 10));
        Assert.Equal(id, persisted.Id);
        Assert.Equal(200, persisted.StatusCode);
        Assert.Equal("Succeeded", persisted.Outcome);
        Assert.Equal(25, persisted.DurationMilliseconds);
        Assert.Empty(await service.ListAsync("tenant-b", 10));

        await service.SaveAsync(started with
        {
            StatusCode = 500,
            Outcome = "Failed",
            ErrorCode = "LATE_WRITE",
            DurationMilliseconds = 50
        });
        AgentOperationAuditRecord unchanged = Assert.Single(
            await service.ListAsync("tenant-a", 10));
        Assert.Equal("Succeeded", unchanged.Outcome);
        Assert.Null(unchanged.ErrorCode);
    }

    [Fact]
    public async Task Complete_agent_run_with_tool_calls_and_reject_identity_mismatch()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgAgentRunAudit),
            typeof(AgAgentToolCallAudit));
        var service = new AgAgentRunAuditServices(
            fixture.CreateRepository<AgAgentRunAudit>());
        Guid runId = Guid.NewGuid();
        Guid agentId = Guid.NewGuid();
        Guid agentVersionId = Guid.NewGuid();
        DateTimeOffset startedAt = DateTimeOffset.Parse("2026-08-16T08:00:00Z");
        var running = new AgentRunAuditRecord(
            runId,
            agentId,
            agentVersionId,
            "audit-test-agent",
            AgentRunStatus.Running,
            startedAt,
            null,
            new string('a', 64),
            0,
            0,
            string.Empty,
            []);

        await service.SaveAsync(running);
        var toolCall = new AgentToolCallAuditRecord(
            Guid.NewGuid(),
            "query_business_data",
            McpToolRisk.ReadOnly,
            AgentRunEventKind.ToolSucceeded,
            startedAt.AddMilliseconds(10),
            startedAt.AddMilliseconds(20),
            string.Empty);
        AgentRunAuditRecord completed = running with
        {
            Status = AgentRunStatus.Completed,
            FinishedAtUtc = startedAt.AddSeconds(1),
            OutputCharacters = 12,
            ToolCallCount = 1,
            ToolCalls = [toolCall]
        };

        await service.SaveAsync(completed);

        AgentRunAuditRecord persisted = Assert.Single(
            await service.ListAsync(agentId, 10));
        Assert.Equal(AgentRunStatus.Completed, persisted.Status);
        Assert.Equal(12, persisted.OutputCharacters);
        Assert.Equal(1, persisted.ToolCallCount);
        AgentToolCallAuditRecord persistedTool = Assert.Single(persisted.ToolCalls);
        Assert.Equal(toolCall.ToolVersionId, persistedTool.ToolVersionId);
        Assert.Equal("query_business_data", persistedTool.ToolName);
        Assert.Equal(AgentRunEventKind.ToolSucceeded, persistedTool.Status);

        await service.SaveAsync(completed with
        {
            AgentId = Guid.NewGuid(),
            Status = AgentRunStatus.Failed,
            ErrorCode = "IDENTITY_MISMATCH",
            ToolCalls = []
        });
        AgentRunAuditRecord unchanged = Assert.Single(
            await service.ListAsync(agentId, 10));
        Assert.Equal(AgentRunStatus.Completed, unchanged.Status);
        Assert.Equal(string.Empty, unchanged.ErrorCode);
        Assert.Single(unchanged.ToolCalls);
    }
}
