using EU.Core.Agent.Application.Agents;
using EU.Core.Model.Entity;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.Services;
using Xunit;

#nullable enable

namespace EU.Core.Tests.Service_Test;

public sealed class AgAgentDefinitionPersistence_Should
{
    [Fact]
    public async Task Persist_draft_publication_snapshot_and_lifecycle_revisions()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgAgentDefinition),
            typeof(AgAgentVersion),
            typeof(AgAgentVersionBinding),
            typeof(AgAgentVersionSnapshot));
        const string modelProfileId = "test-model";
        var service = new AgAgentDefinitionServices(
            fixture.CreateRepository<AgAgentDefinition>(),
            modelProfiles: new PublicModelProfileCatalog([modelProfileId]));
        string code = $"agent-{Guid.NewGuid():N}";

        var created = await service.CreateAsync(
            new CreateAgentCommand(code, "Agent A", "description"));
        Assert.True(created.Success);
        Assert.False((await service.CreateAsync(
            new CreateAgentCommand(code, "Duplicate"))).Success);

        AgentDefinition initial = Assert.IsType<AgentDefinition>(
            await service.GetDefinitionAsync(created.Data, CancellationToken.None));
        Assert.Equal(0, initial.LogicalRevision);
        Assert.True(initial.Draft.IsDraft);
        Assert.Empty(initial.PublishedVersions);

        AgentOperationResult<AgentDefinition> saved = await service.SaveDraftAsync(
            new SaveAgentDraftCommand(
                initial.Id,
                0,
                "Always return AGENT_OK.",
                modelProfileId,
                AgentOutputMode.Text,
                null,
                "Agent A updated",
                "updated description"));
        Assert.True(saved.Succeeded);
        AgentDefinition draft = Assert.IsType<AgentDefinition>(saved.Value);
        Assert.Equal(1, draft.LogicalRevision);
        Assert.Equal("Always return AGENT_OK.", draft.Draft.Instructions);
        AgentOperationResult<AgentDefinition> staleSave = await service.SaveDraftAsync(
            new SaveAgentDraftCommand(
                initial.Id,
                0,
                "stale",
                modelProfileId,
                AgentOutputMode.Text,
                null));
        Assert.False(staleSave.Succeeded);
        Assert.Equal(AgentErrorCodes.RowVersionConflict, staleSave.Error?.Code);

        AgentOperationResult<AgentDefinition> published = await service.PublishAsync(
            new PublishAgentCommand(initial.Id, 1));
        Assert.True(published.Succeeded);
        AgentDefinition publishedDefinition = Assert.IsType<AgentDefinition>(published.Value);
        Assert.Equal(2, publishedDefinition.LogicalRevision);
        AgentVersion version = Assert.Single(publishedDefinition.PublishedVersions);
        Assert.Equal("1.0.0", version.Label);
        AgentVersionSnapshot snapshot = Assert.IsType<AgentVersionSnapshot>(version.Snapshot);
        Assert.Equal(code, snapshot.AgentCode);
        Assert.Equal("Agent A updated", snapshot.AgentName);
        Assert.Equal(modelProfileId, snapshot.ModelProfileId);

        AgentDefinition persisted = Assert.IsType<AgentDefinition>(
            await service.GetDefinitionAsync(initial.Id, CancellationToken.None));
        Assert.Equal(version.Id, Assert.Single(persisted.PublishedVersions).Id);
        Assert.Equal(version.Id, persisted.PublishedVersions[0].Snapshot?.VersionId);
        Assert.Equal("1.0.0", Assert.Single(
            await service.ListAsync(new AgentDefinitionQuery(Search: "Agent A updated")))
            .CurrentPublishedLabel);
        Assert.NotNull(await service.QueryAgent(initial.Id));
        Assert.Single(await service.QueryAgentList(search: code));

        AgentOperationResult<AgentDefinition> disabled = await service.SetRuntimeStatusAsync(
            new SetAgentRuntimeStatusCommand(initial.Id, 2, AgentRuntimeStatus.Disabled));
        Assert.True(disabled.Succeeded);
        AgentOperationResult<AgentDefinition> archived = await service.SetRuntimeStatusAsync(
            new SetAgentRuntimeStatusCommand(initial.Id, 3, AgentRuntimeStatus.Archived));
        Assert.True(archived.Succeeded);
        Assert.Empty(await service.ListAsync(new AgentDefinitionQuery(Search: code)));
        AgentListItem archivedItem = Assert.Single(await service.ListAsync(
            new AgentDefinitionQuery(code, AgentRuntimeStatus.Archived)));
        Assert.Equal(4, archivedItem.LogicalRevision);
    }
}
