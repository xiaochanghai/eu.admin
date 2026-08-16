using EU.Core.Agent.Application.Evaluation;
using EU.Core.Agent.Application.Orchestration;
using EU.Core.Agent.Application.UnifiedEntry;
using EU.Core.Model.Entity;
using EU.Core.Services;
using Xunit;

#nullable enable

namespace EU.Core.Tests.Service_Test;

public sealed class AgDefinitionPersistence_Should
{
    [Fact]
    public async Task Persist_evaluation_suite_draft_versions_rules_and_revision()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgEvaluationSuite),
            typeof(AgEvaluationSuiteVersion),
            typeof(AgEvaluationCase),
            typeof(AgEvaluationCaseRule));
        var service = new AgEvaluationSuiteServices(
            fixture.CreateRepository<AgEvaluationSuite>());
        DateTimeOffset now = DateTimeOffset.Parse("2026-08-16T13:00:00Z");
        EvaluationCaseDefinition draftCase = CreateEvaluationCase("draft-case");
        EvaluationCaseDefinition publishedCase = CreateEvaluationCase("published-case");
        var published = new PublishedEvaluationSuiteVersion(
            Guid.NewGuid(),
            "1.0.0",
            Hash('a'),
            now,
            "user-a",
            [publishedCase]);
        var definition = new EvaluationSuiteDefinition(
            Guid.NewGuid(),
            "tenant-a",
            $"suite-{Guid.NewGuid():N}",
            "Suite A",
            "description",
            0,
            now,
            now,
            "user-a",
            "user-a",
            new EvaluationSuiteDraft([draftCase]),
            [published]);

        Assert.True(await service.TryCreateAsync(definition));
        Assert.False(await service.TryCreateAsync(definition with { Id = Guid.NewGuid() }));
        Assert.Null(await service.GetAsync(definition.Id, "tenant-b"));

        EvaluationSuiteDefinition persisted = Assert.IsType<EvaluationSuiteDefinition>(
            await service.GetAsync(definition.Id, definition.TenantId));
        Assert.Equal(draftCase.Id, Assert.Single(persisted.Draft.Cases).Id);
        EvaluationCaseDefinition restoredPublished = Assert.Single(
            Assert.Single(persisted.PublishedVersions).Cases);
        Assert.Equal(["ORCHID-7319"], restoredPublished.Specification.OutputContains);
        Assert.Equal(["NOT FOUND"], restoredPublished.Specification.OutputExcludes);
        Assert.Equal(["knowledge-citation"], restoredPublished.Specification.RequiredEventKinds);
        Assert.Single(await service.ListAsync(definition.TenantId));
        Assert.Empty(await service.ListAsync("tenant-b"));

        PublishedEvaluationSuiteVersion secondPublished = new(
            Guid.NewGuid(),
            "2.0.0",
            Hash('b'),
            now.AddMinutes(1),
            "user-b",
            [draftCase]);
        EvaluationSuiteDefinition replacement = definition with
        {
            Name = "Suite B",
            LogicalRevision = 1,
            UpdatedAtUtc = now.AddMinutes(1),
            UpdatedBy = "user-b",
            Draft = new EvaluationSuiteDraft([CreateEvaluationCase("new-draft")]),
            PublishedVersions = [published, secondPublished]
        };
        Assert.True(await service.TryReplaceAsync(replacement, 0));
        Assert.False(await service.TryReplaceAsync(replacement, 0));
        EvaluationSuiteDefinition replaced = Assert.IsType<EvaluationSuiteDefinition>(
            await service.GetAsync(definition.Id, definition.TenantId));
        Assert.Equal("Suite B", replaced.Name);
        Assert.Equal(2, replaced.PublishedVersions.Count);
        Assert.Equal("new-draft", Assert.Single(replaced.Draft.Cases).Name);
    }

    [Fact]
    public async Task Persist_orchestration_graph_snapshots_and_published_catalog()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgOrchestrationDefinition),
            typeof(AgOrchestrationVersion),
            typeof(AgOrchestrationNode),
            typeof(AgOrchestrationEdge),
            typeof(AgOrchestrationAgentBinding));
        var service = new AgOrchestrationDefinitionServices(
            fixture.CreateRepository<AgOrchestrationDefinition>());
        string code = $"orchestration-{Guid.NewGuid():N}";
        OrchestrationVersion draft = CreateOrchestrationVersion(code, "draft", true);
        OrchestrationVersion published = CreateOrchestrationVersion(code, "1.0.0", false);
        var definition = new OrchestrationDefinition(
            Guid.NewGuid(),
            code,
            "Orchestration A",
            "description",
            OrchestrationStatus.Enabled,
            0,
            draft,
            [published]);

        Assert.True(await service.TryCreateAsync(definition));
        Assert.False(await service.TryCreateAsync(definition with { Id = Guid.NewGuid() }));
        OrchestrationDefinition persisted = Assert.IsType<OrchestrationDefinition>(
            await service.GetByIdAsync(definition.Id));
        Assert.Equal(2, persisted.Draft.Nodes.Count);
        Assert.Single(persisted.Draft.Edges);
        OrchestrationVersion restoredPublished = Assert.Single(persisted.PublishedVersions);
        Assert.NotNull(restoredPublished.Snapshot);
        Assert.Single(restoredPublished.Snapshot!.Agents);
        Assert.Contains(
            await service.ListPublishedAsync(),
            value => value.OrchestrationId == definition.Id &&
                     value.OrchestrationVersionId == published.Id &&
                     value.Enabled);

        OrchestrationVersion secondPublished = CreateOrchestrationVersion(
            code,
            "2.0.0",
            false);
        OrchestrationDefinition replacement = definition with
        {
            Name = "Orchestration B",
            LogicalRevision = 1,
            Draft = draft with
            {
                Nodes = draft.Nodes.Select(node => node with { TimeoutSeconds = 45 }).ToArray()
            },
            PublishedVersions = [published, secondPublished]
        };
        Assert.True(await service.TryReplaceAsync(replacement, 0));
        Assert.False(await service.TryReplaceAsync(replacement, 0));
        OrchestrationDefinition replaced = Assert.IsType<OrchestrationDefinition>(
            await service.GetByIdAsync(definition.Id));
        Assert.Equal("Orchestration B", replaced.Name);
        Assert.All(replaced.Draft.Nodes, node => Assert.Equal(45, node.TimeoutSeconds));
        Assert.Equal(2, replaced.PublishedVersions.Count);
        Assert.Equal(2, (await service.ListPublishedAsync()).Count(value =>
            value.OrchestrationId == definition.Id));
    }

    private static EvaluationCaseDefinition CreateEvaluationCase(string name) => new(
        Guid.NewGuid(),
        name,
        "What is the Atlas escalation code?",
        Guid.NewGuid(),
        Guid.NewGuid(),
        new RunEvaluationSpecification(
            UnifiedRunStatus.Completed,
            ["ORCHID-7319"],
            ["NOT FOUND"],
            ["knowledge-citation"],
            0,
            120_000));

    private static OrchestrationVersion CreateOrchestrationVersion(
        string code,
        string label,
        bool isDraft)
    {
        Guid versionId = Guid.NewGuid();
        Guid firstAgentId = Guid.NewGuid();
        Guid secondAgentId = Guid.NewGuid();
        OrchestrationNode[] nodes =
        [
            new("node-1", "First", firstAgentId, OrchestrationNodeInputMode.InitialInput,
                string.Empty, 1, 30),
            new("node-2", "Second", secondAgentId, OrchestrationNodeInputMode.PreviousOutput,
                string.Empty, 0, 30)
        ];
        OrchestrationEdge[] edges =
        [
            new("node-1", "node-2", OrchestrationEdgeCondition.Succeeded, string.Empty, 0)
        ];
        OrchestrationVersionSnapshot? snapshot = isDraft
            ? null
            : new OrchestrationVersionSnapshot(
                versionId,
                code,
                "node-1",
                nodes,
                edges,
                [new OrchestrationAgentBinding(firstAgentId, Guid.NewGuid())]);
        return new OrchestrationVersion(
            versionId,
            label,
            isDraft,
            "node-1",
            nodes,
            edges,
            snapshot);
    }

    private static string Hash(char value) => new(value, 64);
}
