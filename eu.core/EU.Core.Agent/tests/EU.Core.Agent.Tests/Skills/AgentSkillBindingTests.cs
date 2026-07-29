using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Application.Skills;
using EU.Core.Agent.Infrastructure.Persistence;
using Xunit;

namespace EU.Core.Agent.Tests.Skills;

public sealed class AgentSkillBindingTests
{
    [Fact]
    public async Task Agent_draft_accepts_only_published_skill_versions_and_publish_freezes_binding()
    {
        var skills = new InMemorySkillRepository();
        Guid versionId = Guid.NewGuid();
        var version = new SkillVersion(
            versionId,
            "1.0.0",
            new string('a', 64),
            DateTimeOffset.UtcNow,
            SkillContractCloner.ReadOnly(Array.Empty<SkillFileHash>()));
        var skill = new SkillDefinition(
            Guid.NewGuid(),
            "support-skill",
            "Support",
            "Support answers",
            "Service",
            0,
            SkillContractCloner.ReadOnly([version]));
        Assert.True(await skills.TryCreateAsync(skill));

        var lifecycle = new AgentLifecycleService(
            new InMemoryAgentRepository(),
            skillVersions: skills);
        AgentDefinition created = Successful(await lifecycle.CreateAsync(
            new CreateAgentCommand("support-agent")));
        AgentDefinition saved = Successful(await lifecycle.SaveDraftAsync(
            new SaveAgentDraftCommand(
                created.Id,
                created.LogicalRevision,
                "Answer support questions.",
                "qwen-safe",
                AgentOutputMode.Text,
                null,
                SkillVersionIds: [versionId])));
        AgentDefinition published = Successful(await lifecycle.PublishAsync(
            new PublishAgentCommand(saved.Id, saved.LogicalRevision)));

        Assert.Equal(versionId, Assert.Single(saved.Draft.SkillVersionIds));
        Assert.Equal(
            versionId,
            Assert.Single(Assert.Single(published.PublishedVersions).Snapshot!.Skills).SkillVersionId);

        AgentOperationResult<AgentDefinition> missing = await lifecycle.SaveDraftAsync(
            new SaveAgentDraftCommand(
                published.Id,
                published.LogicalRevision,
                "Answer support questions.",
                "qwen-safe",
                AgentOutputMode.Text,
                null,
                SkillVersionIds: [Guid.NewGuid()]));
        Assert.Equal(AgentErrorCodes.SkillVersionNotPublished, missing.Error?.Code);
    }

    [Fact]
    public async Task Agent_package_round_trip_preserves_published_skill_version_references()
    {
        var skills = new InMemorySkillRepository();
        Guid versionId = Guid.NewGuid();
        var skill = new SkillDefinition(
            Guid.NewGuid(),
            "package-skill",
            "Package Skill",
            "Package reference",
            "General",
            0,
            SkillContractCloner.ReadOnly([
                new SkillVersion(
                    versionId,
                    "1.0.0",
                    new string('b', 64),
                    DateTimeOffset.UtcNow,
                    SkillContractCloner.ReadOnly(Array.Empty<SkillFileHash>()))
            ]));
        Assert.True(await skills.TryCreateAsync(skill));
        var models = new PublicModelProfileCatalog(["qwen-safe"]);
        var sourceRepository = new InMemoryAgentRepository();
        var sourceLifecycle = new AgentLifecycleService(
            sourceRepository,
            skillVersions: skills);
        AgentDefinition created = Successful(await sourceLifecycle.CreateAsync(
            new CreateAgentCommand("package-agent")));
        AgentDefinition saved = Successful(await sourceLifecycle.SaveDraftAsync(
            new SaveAgentDraftCommand(
                created.Id,
                created.LogicalRevision,
                "Use published skills.",
                "qwen-safe",
                AgentOutputMode.Text,
                null,
                SkillVersionIds: [versionId])));
        var sourcePackages = new AgentPackageService(
            sourceRepository,
            sourceLifecycle,
            models,
            skillVersions: skills);

        AgentOperationResult<string> exported = await sourcePackages.ExportAsync(saved.Id);
        Assert.True(exported.Succeeded, exported.Error?.Message);

        var targetRepository = new InMemoryAgentRepository();
        var targetLifecycle = new AgentLifecycleService(
            targetRepository,
            skillVersions: skills);
        var targetPackages = new AgentPackageService(
            targetRepository,
            targetLifecycle,
            models,
            skillVersions: skills);
        AgentOperationResult<AgentDefinition> imported =
            await targetPackages.ImportAsync(exported.Value!);

        AgentDefinition restored = Successful(imported);
        Assert.Equal(versionId, Assert.Single(restored.Draft.SkillVersionIds));
    }

    private static AgentDefinition Successful(AgentOperationResult<AgentDefinition> result)
    {
        Assert.True(result.Succeeded, result.Error?.Message);
        return Assert.IsType<AgentDefinition>(result.Value);
    }
}
