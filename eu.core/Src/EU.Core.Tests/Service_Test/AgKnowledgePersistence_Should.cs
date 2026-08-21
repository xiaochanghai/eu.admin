using EU.Core.Model;
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
        string code = $"knowledge-{Guid.NewGuid():N}";
        ServiceResult<KnowledgeBaseDefinition> created = await service.CreateAsync(
            new CreateKnowledgeBaseCommand(code, "Atlas Knowledge", "description"));
        Assert.True(created.Success);
        ServiceResult<KnowledgeBaseDefinition> duplicate = await service.CreateAsync(
            new CreateKnowledgeBaseCommand(code, "Duplicate", string.Empty));
        Assert.False(duplicate.Success);
        Assert.Equal(KnowledgeServiceStatusCodes.CodeConflict, duplicate.Status);

        ServiceResult<KnowledgeBaseDefinition> imported = await service.ImportDocumentAsync(
            new ImportKnowledgeDocumentCommand(
                created.Data!.Id,
                created.Data.LogicalRevision,
                "atlas.txt",
                "text/plain",
                "Atlas service escalation code is ORCHID-7319."));
        Assert.True(imported.Success);
        KnowledgeBaseDefinition definition = imported.Data!;
        KnowledgeBaseDefinition persisted = Assert.IsType<KnowledgeBaseDefinition>(
            await service.GetByCodeAsync(definition.Code));
        Assert.Equal("Atlas service escalation code is ORCHID-7319.", Assert.Single(persisted.Documents).Content);
        Assert.NotEmpty(persisted.Chunks);
        Assert.Contains(
            await service.ListPublishedAsync(),
            value => value.KnowledgeBaseId == definition.Id);

        IReadOnlyList<KnowledgeSearchResult> results = await service.SearchAsync(
            [definition.Id],
            "Atlas ORCHID-7319",
            10);
        KnowledgeSearchResult first = Assert.Single(results);
        Assert.Contains("ORCHID-7319", first.Content);
        Assert.True(first.Score > 0);

        ServiceResult<KnowledgeBaseDefinition> disabled = await service.UpdateAsync(
            new UpdateKnowledgeBaseCommand(
                definition.Id,
                definition.LogicalRevision,
                "Atlas Knowledge Updated",
                definition.Description,
                KnowledgeBaseStatus.Disabled));
        Assert.True(disabled.Success);
        ServiceResult<KnowledgeBaseDefinition> stale = await service.UpdateAsync(
            new UpdateKnowledgeBaseCommand(
                definition.Id,
                definition.LogicalRevision,
                "Stale update",
                definition.Description,
                KnowledgeBaseStatus.Disabled));
        Assert.False(stale.Success);
        Assert.Equal(KnowledgeServiceStatusCodes.RowVersionConflict, stale.Status);
        Assert.Empty(await service.SearchAsync([definition.Id], "Atlas", 10));
        Assert.DoesNotContain(
            await service.ListPublishedAsync(),
            value => value.KnowledgeBaseId == definition.Id);
        Assert.Single(await service.ListAsync(
            new KnowledgeBaseQuery(KnowledgeBaseStatus.Disabled)));

        KnowledgeBaseDefinition unchanged = Assert.IsType<KnowledgeBaseDefinition>(
            await service.GetByIdAsync(definition.Id));
        Assert.Single(unchanged.Documents);
        Assert.NotEmpty(unchanged.Chunks);
    }

    [Fact]
    public async Task Block_archive_while_an_enabled_agent_references_the_knowledge_base()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgKnowledgeBaseDefinition),
            typeof(AgKnowledgeDocument),
            typeof(AgKnowledgeChunk));
        StubAgentDefinitionCatalog? agents = null;
        var service = new AgKnowledgeBaseDefinitionServices(
            fixture.CreateRepository<AgKnowledgeBaseDefinition>(),
            new Lazy<IAgentDefinitionCatalog>(() => agents!));
        ServiceResult<KnowledgeBaseDefinition> created = await service.CreateAsync(
            new CreateKnowledgeBaseCommand(
                $"knowledge-{Guid.NewGuid():N}",
                "Referenced Knowledge",
                string.Empty));
        Assert.True(created.Success);
        Guid knowledgeBaseId = created.Data!.Id;
        ServiceResult<KnowledgeBaseDefinition> disabled = await service.UpdateAsync(
            new UpdateKnowledgeBaseCommand(
                knowledgeBaseId,
                created.Data.LogicalRevision,
                created.Data.Name,
                created.Data.Description,
                KnowledgeBaseStatus.Disabled));
        Assert.True(disabled.Success);
        agents = new StubAgentDefinitionCatalog(
            [CreateReferencingAgent(knowledgeBaseId)]);

        ServiceResult<KnowledgeBaseDefinition> blocked =
            await service.SetArchivedAsync(
                new SetKnowledgeBaseArchiveCommand(
                    knowledgeBaseId,
                    disabled.Data!.LogicalRevision,
                    true));

        Assert.False(blocked.Success);
        Assert.Equal(KnowledgeServiceStatusCodes.ArchiveBlocked, blocked.Status);
        Assert.Contains("knowledge-consumer", blocked.Message);

        agents.Definitions = [];
        ServiceResult<KnowledgeBaseDefinition> archived =
            await service.SetArchivedAsync(
                new SetKnowledgeBaseArchiveCommand(
                    knowledgeBaseId,
                    disabled.Data.LogicalRevision,
                    true));
        Assert.True(archived.Success);
        Assert.Equal(KnowledgeBaseStatus.Archived, archived.Data?.Status);
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

}

