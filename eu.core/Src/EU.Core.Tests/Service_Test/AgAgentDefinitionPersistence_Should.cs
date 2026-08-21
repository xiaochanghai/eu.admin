using EU.Core.IServices.Agents;
using EU.Core.IServices.MainAgent;
using EU.Core.IServices.Orchestration;
using EU.Core.Model;
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

        ServiceResult<AgentDefinition> saved = await service.SaveDraftAsync(
            new SaveAgentDraftCommand(
                initial.Id,
                0,
                "Always return AGENT_OK.",
                modelProfileId,
                AgentOutputMode.Text,
                null,
                "Agent A updated",
                "updated description"));
        Assert.True(saved.Success);
        AgentDefinition draft = Assert.IsType<AgentDefinition>(saved.Data);
        Assert.Equal(1, draft.LogicalRevision);
        Assert.Equal("Always return AGENT_OK.", draft.Draft.Instructions);
        ServiceResult<AgentDefinition> staleSave = await service.SaveDraftAsync(
            new SaveAgentDraftCommand(
                initial.Id,
                0,
                "stale",
                modelProfileId,
                AgentOutputMode.Text,
                null));
        Assert.False(staleSave.Success);
        Assert.Equal(500, staleSave.Status);
        Assert.Contains("changed before this operation completed", staleSave.Message);

        ServiceResult<AgentDefinition> published = await service.PublishAsync(
            new PublishAgentCommand(initial.Id, 1));
        Assert.True(published.Success);
        AgentDefinition publishedDefinition = Assert.IsType<AgentDefinition>(published.Data);
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

        ServiceResult<AgentDefinition> disabled = await service.SetRuntimeStatusAsync(
            new SetAgentRuntimeStatusCommand(initial.Id, 2, AgentRuntimeStatus.Disabled));
        Assert.True(disabled.Success);
        ServiceResult<AgentDefinition> archived = await service.SetRuntimeStatusAsync(
            new SetAgentRuntimeStatusCommand(initial.Id, 3, AgentRuntimeStatus.Archived));
        Assert.True(archived.Success);
        Assert.Empty(await service.ListAsync(new AgentDefinitionQuery(Search: code)));
        AgentListItem archivedItem = Assert.Single(await service.ListAsync(
            new AgentDefinitionQuery(code, AgentRuntimeStatus.Archived)));
        Assert.Equal(4, archivedItem.LogicalRevision);
    }

    [Fact]
    public async Task Block_archive_while_an_enabled_parent_agent_references_the_agent()
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

        Guid childId = (await service.CreateAsync(
            new CreateAgentCommand(
                $"child-{Guid.NewGuid():N}",
                "Child Agent"))).Data;
        AgentDefinition childDraft = Assert.IsType<AgentDefinition>((await service.SaveDraftAsync(
            new SaveAgentDraftCommand(
                childId,
                0,
                "Return CHILD_OK.",
                modelProfileId,
                AgentOutputMode.Text,
                null))).Data);
        AgentDefinition childPublished = Assert.IsType<AgentDefinition>((await service.PublishAsync(
            new PublishAgentCommand(childId, childDraft.LogicalRevision))).Data);
        Guid childVersionId = Assert.Single(childPublished.PublishedVersions).Id;

        Guid parentId = (await service.CreateAsync(
            new CreateAgentCommand(
                $"parent-{Guid.NewGuid():N}",
                "Parent Agent"))).Data;
        var saveParent = new SaveAgentDraftCommand(
            parentId,
            0,
            "Delegate to the child Agent.",
            modelProfileId,
            AgentOutputMode.Text,
            null)
        {
            ChildAgentIds = [childId],
            ChildAgentPins =
            [
                new AgentChildBindingSnapshot(childId, childVersionId)
                {
                    AgentCode = childPublished.Code,
                    AgentName = childPublished.Name,
                    AgentDescription = childPublished.Description
                }
            ]
        };
        AgentDefinition parentDraft = Assert.IsType<AgentDefinition>(
            (await service.SaveDraftAsync(saveParent)).Data);
        AgentDefinition parentPublished = Assert.IsType<AgentDefinition>((await service.PublishAsync(
            new PublishAgentCommand(parentId, parentDraft.LogicalRevision))).Data);

        AgentDefinition childDisabled = Assert.IsType<AgentDefinition>((await service.SetRuntimeStatusAsync(
            new SetAgentRuntimeStatusCommand(
                childId,
                childPublished.LogicalRevision,
                AgentRuntimeStatus.Disabled))).Data);
        ServiceResult<AgentDefinition> blocked = await service.SetRuntimeStatusAsync(
            new SetAgentRuntimeStatusCommand(
                childId,
                childDisabled.LogicalRevision,
                AgentRuntimeStatus.Archived));

        Assert.False(blocked.Success);
        Assert.Equal(500, blocked.Status);
        Assert.Contains(parentPublished.Code, blocked.Message);

        Assert.True((await service.SetRuntimeStatusAsync(
            new SetAgentRuntimeStatusCommand(
                parentId,
                parentPublished.LogicalRevision,
                AgentRuntimeStatus.Disabled))).Success);
        ServiceResult<AgentDefinition> archived = await service.SetRuntimeStatusAsync(
            new SetAgentRuntimeStatusCommand(
                childId,
                childDisabled.LogicalRevision,
                AgentRuntimeStatus.Archived));
        Assert.True(archived.Success);
        Assert.Equal(AgentRuntimeStatus.Archived, archived.Data.RuntimeStatus);
    }

    [Fact]
    public async Task Advance_main_agent_assignment_when_its_new_version_is_published()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgAgentDefinition),
            typeof(AgAgentVersion),
            typeof(AgAgentVersionBinding),
            typeof(AgAgentVersionSnapshot),
            typeof(AgMainAgentAssignment));
        var assignments = new AgMainAgentAssignmentServices(
            fixture.CreateRepository<AgMainAgentAssignment>());
        const string modelProfileId = "test-model";
        var service = new AgAgentDefinitionServices(
            fixture.CreateRepository<AgAgentDefinition>(),
            mainAgentAssignments: assignments,
            modelProfiles: new PublicModelProfileCatalog([modelProfileId]));

        Guid agentId = (await service.CreateAsync(new CreateAgentCommand(
            $"main-{Guid.NewGuid():N}",
            "Main Agent"))).Data;
        AgentDefinition firstDraft = Assert.IsType<AgentDefinition>((await service.SaveDraftAsync(
            new SaveAgentDraftCommand(
                agentId,
                0,
                "Return VERSION_1.",
                modelProfileId,
                AgentOutputMode.Text,
                null))).Data);
        AgentDefinition firstPublished = Assert.IsType<AgentDefinition>((await service.PublishAsync(
            new PublishAgentCommand(agentId, firstDraft.LogicalRevision))).Data);
        Guid firstVersionId = Assert.Single(firstPublished.PublishedVersions).Id;
        Assert.True(await assignments.TryReplaceAsync(
            new MainAgentAssignment(agentId, firstVersionId, 0, DateTimeOffset.UtcNow),
            null));

        AgentDefinition secondDraft = Assert.IsType<AgentDefinition>((await service.SaveDraftAsync(
            new SaveAgentDraftCommand(
                agentId,
                firstPublished.LogicalRevision,
                "Return VERSION_2.",
                modelProfileId,
                AgentOutputMode.Text,
                null))).Data);
        AgentDefinition secondPublished = Assert.IsType<AgentDefinition>((await service.PublishAsync(
            new PublishAgentCommand(agentId, secondDraft.LogicalRevision))).Data);

        MainAgentAssignment assignment = Assert.IsType<MainAgentAssignment>(
            await assignments.GetAsync());
        Assert.Equal(secondPublished.PublishedVersions[^1].Id, assignment.AgentVersionId);
        Assert.Equal(1, assignment.LogicalRevision);
    }

    [Fact]
    public async Task Block_archive_while_agent_is_main_assignment_or_used_by_orchestration()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgAgentDefinition),
            typeof(AgAgentVersion),
            typeof(AgAgentVersionBinding),
            typeof(AgAgentVersionSnapshot),
            typeof(AgMainAgentAssignment),
            typeof(AgOrchestrationDefinition),
            typeof(AgOrchestrationVersion),
            typeof(AgOrchestrationNode),
            typeof(AgOrchestrationEdge),
            typeof(AgOrchestrationAgentBinding));
        var assignments = new AgMainAgentAssignmentServices(
            fixture.CreateRepository<AgMainAgentAssignment>());
        var orchestrations = new AgOrchestrationDefinitionServices(
            fixture.CreateRepository<AgOrchestrationDefinition>());
        const string modelProfileId = "test-model";
        var service = new AgAgentDefinitionServices(
            fixture.CreateRepository<AgAgentDefinition>(),
            orchestrations: orchestrations,
            mainAgentAssignments: assignments,
            modelProfiles: new PublicModelProfileCatalog([modelProfileId]));
        Guid agentId = (await service.CreateAsync(
            new CreateAgentCommand(
                $"referenced-{Guid.NewGuid():N}",
                "Referenced Agent"))).Data;
        AgentDefinition draft = Assert.IsType<AgentDefinition>((await service.SaveDraftAsync(
            new SaveAgentDraftCommand(
                agentId,
                0,
                "Return REFERENCED_OK.",
                modelProfileId,
                AgentOutputMode.Text,
                null))).Data);
        AgentDefinition published = Assert.IsType<AgentDefinition>((await service.PublishAsync(
            new PublishAgentCommand(agentId, draft.LogicalRevision))).Data);
        Guid agentVersionId = Assert.Single(published.PublishedVersions).Id;
        Assert.True(await assignments.TryReplaceAsync(
            new MainAgentAssignment(
                agentId,
                agentVersionId,
                0,
                DateTimeOffset.Parse("2026-08-16T15:00:00Z")),
            null));

        Guid orchestrationId = Guid.NewGuid();
        Guid orchestrationVersionId = Guid.NewGuid();
        const string orchestrationCode = "agent-consumer-flow";
        OrchestrationNode[] nodes =
        [
            new(
                "node-1",
                "Referenced Agent Node",
                agentId,
                OrchestrationNodeInputMode.InitialInput,
                string.Empty,
                0,
                30)
        ];
        var orchestrationSnapshot = new OrchestrationVersionSnapshot(
            orchestrationVersionId,
            orchestrationCode,
            "node-1",
            nodes,
            [],
            [new OrchestrationAgentBinding(agentId, agentVersionId)]);
        var orchestration = new OrchestrationDefinition(
            orchestrationId,
            orchestrationCode,
            "Agent Consumer Flow",
            string.Empty,
            OrchestrationStatus.Enabled,
            0,
            new OrchestrationVersion(
                Guid.NewGuid(),
                "draft",
                true,
                "node-1",
                nodes,
                [],
                null),
            [
                new OrchestrationVersion(
                    orchestrationVersionId,
                    "1.0.0",
                    false,
                    "node-1",
                    nodes,
                    [],
                    orchestrationSnapshot)
            ]);
        Assert.True(await orchestrations.TryCreateAsync(orchestration));

        AgentDefinition disabled = Assert.IsType<AgentDefinition>((await service.SetRuntimeStatusAsync(
            new SetAgentRuntimeStatusCommand(
                agentId,
                published.LogicalRevision,
                AgentRuntimeStatus.Disabled))).Data);
        ServiceResult<AgentDefinition> blocked = await service.SetRuntimeStatusAsync(
            new SetAgentRuntimeStatusCommand(
                agentId,
                disabled.LogicalRevision,
                AgentRuntimeStatus.Archived));

        Assert.False(blocked.Success);
        Assert.Equal(500, blocked.Status);
        Assert.Contains("Main Agent assignment", blocked.Message);
        Assert.Contains(orchestrationCode, blocked.Message);

        Assert.True(await assignments.TryReplaceAsync(
            new MainAgentAssignment(
                Guid.NewGuid(),
                Guid.NewGuid(),
                1,
                DateTimeOffset.Parse("2026-08-16T15:01:00Z")),
            0));
        Assert.True(await orchestrations.TryReplaceAsync(
            orchestration with
            {
                Status = OrchestrationStatus.Disabled,
                LogicalRevision = 1
            },
            0));
        ServiceResult<AgentDefinition> archived = await service.SetRuntimeStatusAsync(
            new SetAgentRuntimeStatusCommand(
                agentId,
                disabled.LogicalRevision,
                AgentRuntimeStatus.Archived));
        Assert.True(archived.Success);
        Assert.Equal(AgentRuntimeStatus.Archived, archived.Data.RuntimeStatus);
    }
}
