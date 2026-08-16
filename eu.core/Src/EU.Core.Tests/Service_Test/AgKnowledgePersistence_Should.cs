using EU.Core.Agent.Application.Knowledge;
using EU.Core.Model.Entity;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.Services;
using Xunit;

#nullable enable

namespace EU.Core.Tests.Service_Test;

public sealed class AgKnowledgePersistence_Should
{
    [Fact]
    public async Task Persist_documents_chunks_catalog_and_lexical_search()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgKnowledgeBaseDefinition),
            typeof(AgKnowledgeDocument),
            typeof(AgKnowledgeChunk));
        var service = new AgKnowledgeBaseDefinitionServices(
            fixture.CreateRepository<AgKnowledgeBaseDefinition>());
        DateTimeOffset now = DateTimeOffset.Parse("2026-08-16T14:00:00Z");
        Guid documentId = Guid.NewGuid();
        var document = new KnowledgeDocument(
            documentId,
            "atlas.txt",
            "text/plain",
            Hash('a'),
            "Atlas service escalation code is ORCHID-7319.",
            now);
        var relevantChunk = new KnowledgeChunk(
            Guid.NewGuid(),
            documentId,
            0,
            "Atlas escalation code ORCHID-7319");
        var unrelatedChunk = new KnowledgeChunk(
            Guid.NewGuid(),
            documentId,
            1,
            "General service documentation");
        var definition = new KnowledgeBaseDefinition(
            Guid.NewGuid(),
            $"knowledge-{Guid.NewGuid():N}",
            "Atlas Knowledge",
            "description",
            KnowledgeBaseStatus.Enabled,
            0,
            [document],
            [relevantChunk, unrelatedChunk],
            now);

        Assert.True(await service.TryCreateAsync(definition));
        Assert.False(await service.TryCreateAsync(definition with { Id = Guid.NewGuid() }));
        KnowledgeBaseDefinition persisted = Assert.IsType<KnowledgeBaseDefinition>(
            await service.GetByCodeAsync(definition.Code));
        Assert.Equal(document.Content, Assert.Single(persisted.Documents).Content);
        Assert.Equal(2, persisted.Chunks.Count);
        Assert.Contains(
            await ((IPublishedKnowledgeCatalog)service).ListAsync(),
            value => value.KnowledgeBaseId == definition.Id);

        IReadOnlyList<KnowledgeSearchResult> results = await service.SearchAsync(
            [definition.Id],
            "Atlas ORCHID-7319",
            10);
        KnowledgeSearchResult first = Assert.Single(results);
        Assert.Equal(relevantChunk.Id, first.ChunkId);
        Assert.True(first.Score > 0);

        KnowledgeBaseDefinition disabled = definition with
        {
            Name = "Atlas Knowledge Updated",
            Status = KnowledgeBaseStatus.Disabled,
            LogicalRevision = 1
        };
        Assert.True(await service.TryReplaceAsync(disabled, 0));
        Assert.False(await service.TryReplaceAsync(disabled, 0));
        Assert.Empty(await service.SearchAsync([definition.Id], "Atlas", 10));
        Assert.DoesNotContain(
            await ((IPublishedKnowledgeCatalog)service).ListAsync(),
            value => value.KnowledgeBaseId == definition.Id);
        Assert.Single(await service.ListAsync(
            new KnowledgeBaseQuery(KnowledgeBaseStatus.Disabled)));

        KnowledgeBaseDefinition removalAttempt = disabled with
        {
            LogicalRevision = 2,
            Documents = [],
            Chunks = []
        };
        Assert.False(await service.TryReplaceAsync(removalAttempt, 1));
        KnowledgeBaseDefinition unchanged = Assert.IsType<KnowledgeBaseDefinition>(
            await service.GetByIdAsync(definition.Id));
        Assert.Single(unchanged.Documents);
        Assert.Equal(2, unchanged.Chunks.Count);
    }

    [Fact]
    public async Task Block_archive_while_an_enabled_agent_references_the_knowledge_base()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgKnowledgeBaseDefinition),
            typeof(AgKnowledgeDocument),
            typeof(AgKnowledgeChunk));
        var repository = new AgKnowledgeBaseDefinitionServices(
            fixture.CreateRepository<AgKnowledgeBaseDefinition>());
        Guid knowledgeBaseId = Guid.NewGuid();
        var definition = new KnowledgeBaseDefinition(
            knowledgeBaseId,
            $"knowledge-{Guid.NewGuid():N}",
            "Referenced Knowledge",
            string.Empty,
            KnowledgeBaseStatus.Disabled,
            0,
            [],
            [],
            null);
        Assert.True(await repository.TryCreateAsync(definition));
        var agents = new StubAgentDefinitionCatalog(
            [CreateReferencingAgent(knowledgeBaseId)]);
        var lifecycle = new KnowledgeLifecycleService(repository, repository, agents);

        KnowledgeOperationResult<KnowledgeBaseDefinition> blocked =
            await lifecycle.SetArchivedAsync(
                new SetKnowledgeBaseArchiveCommand(knowledgeBaseId, 0, true));

        Assert.False(blocked.Succeeded);
        Assert.Equal(KnowledgeErrorCodes.ArchiveBlocked, blocked.Error?.Code);
        Assert.Contains("knowledge-consumer", blocked.Error?.Message);

        agents.Definitions = [];
        KnowledgeOperationResult<KnowledgeBaseDefinition> archived =
            await lifecycle.SetArchivedAsync(
                new SetKnowledgeBaseArchiveCommand(knowledgeBaseId, 0, true));
        Assert.True(archived.Succeeded);
        Assert.Equal(KnowledgeBaseStatus.Archived, archived.Value?.Status);
    }

    private static AgentDefinition CreateReferencingAgent(Guid knowledgeBaseId)
    {
        Guid agentId = Guid.NewGuid();
        Guid versionId = Guid.NewGuid();
        var snapshot = new AgentVersionSnapshot(
            versionId,
            "knowledge-consumer",
            string.Empty,
            string.Empty,
            AgentOutputMode.Text,
            null,
            [],
            [])
        {
            KnowledgeBases = [new AgentKnowledgeBindingSnapshot(knowledgeBaseId, 0)]
        };
        var draft = new AgentVersion(
            Guid.NewGuid(),
            "draft",
            true,
            string.Empty,
            string.Empty,
            AgentOutputMode.Text,
            null,
            null,
            null);
        var published = new AgentVersion(
            versionId,
            "1.0.0",
            false,
            string.Empty,
            string.Empty,
            AgentOutputMode.Text,
            null,
            null,
            snapshot);
        return new AgentDefinition(
            agentId,
            "knowledge-consumer",
            "Knowledge Consumer",
            string.Empty,
            AgentRuntimeStatus.Enabled,
            0,
            draft,
            [published]);
    }

    private static string Hash(char value) => new(value, 64);
}
