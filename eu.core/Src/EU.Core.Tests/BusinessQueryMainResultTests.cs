#nullable enable
using System.Text.Json;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;
using EU.Core.Services;
using Xunit;

namespace EU.Core.Tests;

public sealed class BusinessQueryMainResultTests
{
    [Theory]
    [InlineData("valid")]
    [InlineData("missing-start")]
    [InlineData("wrong-call")]
    [InlineData("wrong-run")]
    [InlineData("wrong-hash")]
    [InlineData("malformed")]
    public void Main_result_requires_bound_call_and_valid_receipt(string scenario)
    {
        var tool = new PublishedMcpToolReference(Guid.NewGuid(), "business-query", "Business Query", Guid.NewGuid(),
            "query_business_data", "test", "{}", McpToolRisk.ReadOnly, new string('b', 64));
        var policy = new BusinessQueryToolPolicy("business-query", "query_business_data", new Uri("https://localhost"),
            "eu-agent", "eu-mcp", "alias:project-jwt", 1, new string('a', 64), new string('b', 64), TimeSpan.FromSeconds(45), false);
        var context = new AgentRunContext(Guid.NewGuid(), Guid.NewGuid(), null!, "test", "", DateTimeOffset.UtcNow, [tool]);
        var collector = new BusinessQueryRunResultCollector(policy, context);
        var started = new AgentRunEvent(context.RunId, 1, AgentRunEventKind.ToolStarted, DateTimeOffset.UtcNow,
            ToolVersionId: tool.ToolVersionId, ToolName: tool.ToolName, ToolCallId: Guid.NewGuid());
        if (scenario != "missing-start") Assert.Null(collector.Observe(started));
        string payload = JsonSerializer.Serialize(new
        {
            succeeded = true,
            result = new { resultSha256 = new string('c', 64) },
            presentation = new { title = "sales", formatterVersion = "1.0", rows = new[] { new { total = new { displayValue = "—" } } } },
            receipt = new
            {
                queryId = Guid.NewGuid(), catalogRevision = 1, catalogHash = new string(scenario == "wrong-hash" ? 'd' : 'a', 64),
                toolSchemaHash = new string('b', 64), queryPlanHash = new string('c', 64), policyDecisionId = Guid.NewGuid(),
                rowCount = 1, truncated = false, terminalStatus = "succeeded", resultHash = new string('c', 64)
            }
        });
        var completed = started with
        {
            Kind = AgentRunEventKind.ToolSucceeded,
            Text = scenario == "malformed" ? "{}" : payload,
            RunId = scenario == "wrong-run" ? Guid.NewGuid() : context.RunId,
            ToolCallId = scenario == "wrong-call" ? Guid.NewGuid() : started.ToolCallId
        };
        if (scenario != "valid")
        {
            Assert.Throws<UnifiedEntryException>(() => collector.Observe(completed));
            return;
        }
        var result = Assert.IsType<BusinessQueryAuthoritativeResult>(collector.Observe(completed));
        Assert.Equal(1, result.RowCount);
        using var persisted = JsonDocument.Parse(result.ToPersistedContent());
        Assert.Equal("business-query-result", persisted.RootElement.GetProperty("kind").GetString());
        Assert.Equal("—", persisted.RootElement.GetProperty("presentation").GetProperty("rows")[0].GetProperty("total").GetProperty("displayValue").GetString());
        Assert.Throws<UnifiedEntryException>(() => collector.Observe(completed));
        Assert.Throws<UnifiedEntryException>(() => collector.Observe(started));
        Assert.Null(new BusinessQueryRunResultCollector(null, context).Observe(completed));
    }
}
