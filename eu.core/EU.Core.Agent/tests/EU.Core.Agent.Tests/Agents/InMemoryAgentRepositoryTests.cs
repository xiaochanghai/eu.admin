using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Infrastructure.Persistence;
using Xunit;

namespace EU.Core.Agent.Tests.Agents;

public sealed class InMemoryAgentRepositoryTests
{
    [Fact]
    public async Task Repository_clones_boundaries_and_sorts_filtered_queries_deterministically()
    {
        var repository = new InMemoryAgentRepository();
        var service = new AgentLifecycleService(repository);
        AgentDefinition zulu = (await service.CreateAsync(new CreateAgentCommand("zulu"))).Value!;
        AgentDefinition alpha = (await service.CreateAsync(new CreateAgentCommand("alpha"))).Value!;
        AgentDefinition mutableCopy = (await repository.GetByIdAsync(alpha.Id))!;

        AgentDefinition disabled = (await service.SetRuntimeStatusAsync(new SetAgentRuntimeStatusCommand(
            zulu.Id, zulu.LogicalRevision, AgentRuntimeStatus.Disabled))).Value!;
        IReadOnlyList<AgentListItem> all = await service.ListAsync(new AgentDefinitionQuery());
        IReadOnlyList<AgentListItem> filtered = await service.ListAsync(new AgentDefinitionQuery("lu", AgentRuntimeStatus.Disabled));
        AgentDefinition storedAgain = (await repository.GetByIdAsync(alpha.Id))!;

        Assert.NotSame(mutableCopy, storedAgain);
        Assert.Equal("alpha", storedAgain.Code);
        Assert.Equal(["alpha", "zulu"], all.Select(item => item.Code));
        AgentListItem only = Assert.Single(filtered);
        Assert.Equal(disabled.Id, only.Id);
    }

    [Fact]
    public async Task Repository_creation_and_expected_revision_updates_are_atomic()
    {
        var repository = new InMemoryAgentRepository();
        var service = new AgentLifecycleService(repository);

        AgentOperationResult<AgentDefinition>[] creations = await Task.WhenAll(Enumerable.Range(0, 16)
            .Select(_ => service.CreateAsync(new CreateAgentCommand("atomic-agent"))));
        AgentDefinition created = Assert.Single(creations, result => result.Succeeded).Value!;
        AgentOperationResult<AgentDefinition>[] updates = await Task.WhenAll(Enumerable.Range(0, 16)
            .Select(index => service.SaveDraftAsync(new SaveAgentDraftCommand(
                created.Id, created.LogicalRevision, $"instructions-{index}", "qwen", AgentOutputMode.Text, null))));

        Assert.Equal(1, creations.Count(result => result.Succeeded));
        Assert.Equal(1, updates.Count(result => result.Succeeded));
        Assert.All(updates.Where(result => !result.Succeeded), result => Assert.Equal(AgentErrorCodes.RowVersionConflict, result.Error!.Code));
    }

    [Fact]
    public async Task Repository_deep_clones_nested_snapshot_collections_and_keeps_returned_collections_read_only()
    {
        var repository = new InMemoryAgentRepository();
        var sourceSkills = new List<AgentSkillBindingSnapshot> { new(Guid.NewGuid()) };
        var sourceTools = new List<AgentToolBindingSnapshot> { new(Guid.NewGuid()) };
        var snapshot = new AgentVersionSnapshot(Guid.NewGuid(), "copy-agent", "instructions", "qwen", AgentOutputMode.Text, null, sourceSkills, sourceTools);
        var published = new List<AgentVersion> { new(snapshot.VersionId, "1.0.0", false, "instructions", "qwen", AgentOutputMode.Text, null, null, snapshot) };
        var definition = new AgentDefinition(
            Guid.NewGuid(), "copy-agent", "Copy agent", "Tests deep cloning.", AgentRuntimeStatus.Enabled, 0,
            new AgentVersion(Guid.NewGuid(), "0.1.0", true, string.Empty, string.Empty, AgentOutputMode.Text, null, null, null),
            published);

        Assert.True(await repository.TryCreateAsync(definition));
        sourceSkills.Clear();
        sourceTools.Clear();
        published.Clear();
        AgentDefinition returned = (await repository.GetByIdAsync(definition.Id))!;
        AgentVersionSnapshot returnedSnapshot = Assert.Single(returned.PublishedVersions).Snapshot!;

        Assert.Single(returnedSnapshot.Skills);
        Assert.Single(returnedSnapshot.Tools);
        Assert.Throws<NotSupportedException>(() => ((IList<AgentVersion>)returned.PublishedVersions).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<AgentSkillBindingSnapshot>)returnedSnapshot.Skills).Clear());
        Assert.Single((await repository.GetByIdAsync(definition.Id))!.PublishedVersions);
    }

    [Fact]
    public async Task Repository_rejects_replacement_that_does_not_advance_revision_by_exactly_one()
    {
        var repository = new InMemoryAgentRepository();
        var service = new AgentLifecycleService(repository);
        AgentDefinition created = (await service.CreateAsync(new CreateAgentCommand("revision-guard"))).Value!;

        bool replaced = await repository.TryReplaceAsync(created with { LogicalRevision = 2 }, created.LogicalRevision);

        Assert.False(replaced);
        Assert.Equal(0, (await repository.GetByIdAsync(created.Id))!.LogicalRevision);
    }
}
