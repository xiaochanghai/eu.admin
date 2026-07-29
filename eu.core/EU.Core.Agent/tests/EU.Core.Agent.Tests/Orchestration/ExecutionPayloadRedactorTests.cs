using System.Text.RegularExpressions;
using EU.Core.Agent.Application.Orchestration;
using EU.Core.Agent.Application.Runtime;
using Xunit;

namespace EU.Core.Agent.Tests.Orchestration;

public sealed class ExecutionPayloadRedactorTests
{
    [Fact]
    public void Redacts_nested_credentials_and_preserves_business_values()
    {
        string value = ExecutionPayloadRedactor.RedactJson(
            """
            {
              "supplierId": "S1",
              "auth": { "authorization": "Bearer x", "note": "keep" },
              "items": [{ "apiKey": "x" }, { "connection_string": "Server=x" }]
            }
            """);

        Assert.Contains("\"supplierId\":\"S1\"", value, StringComparison.Ordinal);
        Assert.Contains("\"note\":\"keep\"", value, StringComparison.Ordinal);
        Assert.DoesNotContain("Bearer x", value, StringComparison.Ordinal);
        Assert.DoesNotContain("Server=x", value, StringComparison.Ordinal);
        Assert.Equal(3, Regex.Matches(value, "\\[REDACTED\\]").Count);
    }

    [Fact]
    public void Detail_clone_does_not_share_nested_tool_call_collections()
    {
        var details = new OrchestrationRunDetails(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "input",
            "output",
            [
                new OrchestrationNodeAttemptRecord(
                    "node-1",
                    1,
                    Guid.NewGuid(),
                    "node input",
                    "input-sha",
                    "node output",
                    "output-sha",
                    OrchestrationNodeRunStatus.Completed,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    "",
                    [
                        new OrchestrationToolCallRecord(
                            Guid.NewGuid(),
                            Guid.NewGuid(),
                            Guid.NewGuid(),
                            "get_supplier",
                            AgentRunEventKind.ToolSucceeded,
                            """{"id":"S1"}""",
                            """{"type":"module"}""",
                            "result-sha",
                            17,
                            DateTimeOffset.UtcNow,
                            DateTimeOffset.UtcNow,
                            "")
                    ])
            ]);

        OrchestrationRunDetails clone = OrchestrationContractCloner.Clone(details);

        Assert.NotSame(details.Attempts, clone.Attempts);
        Assert.NotSame(details.Attempts[0].ToolCalls, clone.Attempts[0].ToolCalls);
        Assert.Equal("get_supplier", clone.Attempts[0].ToolCalls[0].ToolName);
    }
}
