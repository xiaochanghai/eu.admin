using EU.Core.Agent.Application.Knowledge;
using EU.Core.Model.Entity;
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

    private static string Hash(char value) => new(value, 64);
}
