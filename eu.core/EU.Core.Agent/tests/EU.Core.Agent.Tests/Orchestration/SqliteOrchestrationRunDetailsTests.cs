using EU.Core.Agent.Application.Orchestration;
using EU.Core.Agent.Application.Runtime;
using EU.Core.Agent.Infrastructure.Persistence;
using Xunit;

namespace EU.Core.Agent.Tests.Orchestration;

public sealed class SqliteOrchestrationRunDetailsTests
{
    [Fact]
    public async Task Full_execution_details_survive_repository_reconstruction()
    {
        string root = Path.Combine(
            Path.GetTempPath(), $"eu-core-agent-orchestration-details-{Guid.NewGuid():N}");
        string database = Path.Combine(root, "agent.db");
        Directory.CreateDirectory(root);
        try
        {
            Guid runId = Guid.NewGuid();
            Guid orchestrationId = Guid.NewGuid();
            Guid agentRunId = Guid.NewGuid();
            Guid toolCallId = Guid.NewGuid();
            var details = new OrchestrationRunDetails(
                runId,
                orchestrationId,
                "original input",
                "final output",
                [
                    new OrchestrationNodeAttemptRecord(
                        "query",
                        1,
                        agentRunId,
                        "node input 1",
                        "input-sha-1",
                        "",
                        "",
                        OrchestrationNodeRunStatus.Failed,
                        DateTimeOffset.Parse("2026-07-29T01:00:00Z"),
                        DateTimeOffset.Parse("2026-07-29T01:00:01Z"),
                        "MODEL_INVOCATION_FAILED",
                        []),
                    new OrchestrationNodeAttemptRecord(
                        "query",
                        2,
                        agentRunId,
                        "node input 2",
                        "input-sha-2",
                        "node output",
                        "output-sha",
                        OrchestrationNodeRunStatus.Completed,
                        DateTimeOffset.Parse("2026-07-29T01:00:02Z"),
                        DateTimeOffset.Parse("2026-07-29T01:00:03Z"),
                        "",
                        [
                            new OrchestrationToolCallRecord(
                                toolCallId,
                                agentRunId,
                                Guid.NewGuid(),
                                "get_supplier",
                                AgentRunEventKind.ToolSucceeded,
                                """{"supplierId":"S1"}""",
                                """{"type":"module","id":"1"}""",
                                "result-sha",
                                26,
                                DateTimeOffset.Parse("2026-07-29T01:00:02Z"),
                                DateTimeOffset.Parse("2026-07-29T01:00:03Z"),
                                "")
                        ])
                ]);
            var first = new SqliteOrchestrationRunRepository(database);
            await first.SaveDetailsAsync(details);

            var restarted = new SqliteOrchestrationRunRepository(database);
            OrchestrationRunDetails restored =
                (await restarted.GetDetailsAsync(runId))!;

            Assert.Equal("original input", restored.Input);
            Assert.Equal("final output", restored.Output);
            Assert.Equal([1, 2], restored.Attempts.Select(value => value.Attempt));
            OrchestrationToolCallRecord tool = Assert.Single(restored.Attempts[1].ToolCalls);
            Assert.Equal(toolCallId, tool.ToolCallId);
            Assert.Equal("""{"supplierId":"S1"}""", tool.ArgumentsJson);
            Assert.Equal("""{"type":"module","id":"1"}""", tool.ResultContent);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
