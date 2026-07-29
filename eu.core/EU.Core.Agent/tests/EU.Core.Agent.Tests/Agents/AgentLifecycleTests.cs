using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Infrastructure.Persistence;
using Xunit;

namespace EU.Core.Agent.Tests.Agents;

public sealed class AgentLifecycleTests
{
    [Fact]
    public async Task Create_normalizes_code_and_creates_editable_0_1_0_draft()
    {
        var service = new AgentLifecycleService(new InMemoryAgentRepository());

        AgentOperationResult<AgentDefinition> result = await service.CreateAsync(new CreateAgentCommand("  Support__Agent  "));

        Assert.True(result.Succeeded);
        Assert.Equal("support-agent", result.Value!.Code);
        Assert.Equal("0.1.0", result.Value.Draft.Label);
        Assert.True(result.Value.Draft.IsDraft);
        Assert.Empty(result.Value.PublishedVersions);
        Assert.Equal("Server", result.Value.DeploymentTarget);
        Assert.Equal("EU.Core.Agent.Api", result.Value.Host);
    }

    [Fact]
    public async Task Create_and_save_keep_name_and_description_separate_from_immutable_code()
    {
        var service = new AgentLifecycleService(new InMemoryAgentRepository());

        AgentDefinition created = (await service.CreateAsync(new CreateAgentCommand(
            "support-agent", "Support Agent", "Answers customer questions."))).Value!;
        AgentDefinition saved = (await service.SaveDraftAsync(new SaveAgentDraftCommand(
            created.Id, created.LogicalRevision, "Use the support policy.", "qwen", AgentOutputMode.Text, null,
            "Customer Support", "Answers customer questions and escalates incidents."))).Value!;
        AgentListItem listed = Assert.Single(await service.ListAsync(new AgentDefinitionQuery("support")));

        Assert.Equal("support-agent", saved.Code);
        Assert.Equal("Customer Support", saved.Name);
        Assert.Equal("Answers customer questions and escalates incidents.", saved.Description);
        Assert.Equal(saved.Name, listed.Name);
        Assert.Equal(saved.Description, listed.Description);
    }

    [Fact]
    public async Task Create_rejects_duplicate_normalized_code()
    {
        var service = new AgentLifecycleService(new InMemoryAgentRepository());
        await service.CreateAsync(new CreateAgentCommand("support-agent"));

        AgentOperationResult<AgentDefinition> result = await service.CreateAsync(new CreateAgentCommand(" Support_Agent "));

        Assert.False(result.Succeeded);
        Assert.Equal(AgentErrorCodes.CodeConflict, result.Error!.Code);
    }

    [Fact]
    public async Task Save_draft_increments_logical_revision_and_rejects_stale_revision()
    {
        var service = new AgentLifecycleService(new InMemoryAgentRepository());
        AgentDefinition agent = (await service.CreateAsync(new CreateAgentCommand("revision-agent"))).Value!;

        AgentOperationResult<AgentDefinition> saved = await service.SaveDraftAsync(new SaveAgentDraftCommand(
            agent.Id, agent.LogicalRevision, "Follow the policy.", "qwen", AgentOutputMode.Text, null));
        AgentOperationResult<AgentDefinition> stale = await service.SaveDraftAsync(new SaveAgentDraftCommand(
            agent.Id, agent.LogicalRevision, "Different instructions.", "qwen", AgentOutputMode.Text, null));

        Assert.True(saved.Succeeded);
        Assert.Equal(1, saved.Value!.LogicalRevision);
        Assert.Equal("Follow the policy.", saved.Value.Draft.Instructions);
        Assert.False(stale.Succeeded);
        Assert.Equal(AgentErrorCodes.RowVersionConflict, stale.Error!.Code);
    }

    [Fact]
    public async Task Runtime_status_accepts_only_enabled_and_disabled()
    {
        var service = new AgentLifecycleService(new InMemoryAgentRepository());
        AgentDefinition agent = (await service.CreateAsync(new CreateAgentCommand("status-agent"))).Value!;

        AgentOperationResult<AgentDefinition> disabled = await service.SetRuntimeStatusAsync(new SetAgentRuntimeStatusCommand(
            agent.Id, agent.LogicalRevision, AgentRuntimeStatus.Disabled));
        AgentOperationResult<AgentDefinition> invalid = await service.SetRuntimeStatusAsync(new SetAgentRuntimeStatusCommand(
            disabled.Value!.Id, disabled.Value.LogicalRevision, (AgentRuntimeStatus)42));

        Assert.True(disabled.Succeeded);
        Assert.Equal(AgentRuntimeStatus.Disabled, disabled.Value!.RuntimeStatus);
        Assert.False(invalid.Succeeded);
        Assert.Equal(AgentErrorCodes.RuntimeStatusInvalid, invalid.Error!.Code);
    }

    [Theory]
    [InlineData("", "qwen", AgentOutputMode.Text, null, AgentErrorCodes.VersionNotPublishable)]
    [InlineData("instructions", "", AgentOutputMode.Text, null, AgentErrorCodes.VersionNotPublishable)]
    [InlineData("instructions", "qwen", AgentOutputMode.Text, "{}", AgentErrorCodes.OutputSchemaInvalid)]
    [InlineData("instructions", "qwen", AgentOutputMode.Structured, null, AgentErrorCodes.OutputSchemaInvalid)]
    [InlineData("instructions", "qwen", AgentOutputMode.Structured, "{not-json}", AgentErrorCodes.OutputSchemaInvalid)]
    public async Task Publish_rejects_missing_or_invalid_draft_data(
        string instructions,
        string modelProfileId,
        AgentOutputMode outputMode,
        string? outputJsonSchema,
        string expectedErrorCode)
    {
        var service = new AgentLifecycleService(new InMemoryAgentRepository());
        AgentDefinition agent = (await service.CreateAsync(new CreateAgentCommand("invalid-publish-agent"))).Value!;
        AgentDefinition saved = (await service.SaveDraftAsync(new SaveAgentDraftCommand(
            agent.Id, agent.LogicalRevision, instructions, modelProfileId, outputMode, outputJsonSchema))).Value!;

        AgentOperationResult<AgentDefinition> result = await service.PublishAsync(new PublishAgentCommand(saved.Id, saved.LogicalRevision));

        Assert.False(result.Succeeded);
        Assert.Equal(expectedErrorCode, result.Error!.Code);
    }

    [Fact]
    public async Task Publish_creates_major_versions_and_preserves_immutable_snapshots()
    {
        var service = new AgentLifecycleService(new InMemoryAgentRepository());
        AgentDefinition created = (await service.CreateAsync(new CreateAgentCommand("publisher"))).Value!;
        AgentDefinition firstDraft = (await service.SaveDraftAsync(new SaveAgentDraftCommand(
            created.Id, created.LogicalRevision, "First instructions", "qwen", AgentOutputMode.Text, null))).Value!;
        AgentDefinition firstPublished = (await service.PublishAsync(new PublishAgentCommand(firstDraft.Id, firstDraft.LogicalRevision))).Value!;
        AgentDefinition secondDraft = (await service.SaveDraftAsync(new SaveAgentDraftCommand(
            firstPublished.Id, firstPublished.LogicalRevision, "Second instructions", "deepseek", AgentOutputMode.Text, null))).Value!;
        AgentDefinition secondPublished = (await service.PublishAsync(new PublishAgentCommand(secondDraft.Id, secondDraft.LogicalRevision))).Value!;

        Assert.Collection(secondPublished.PublishedVersions.OrderBy(version => version.Label, StringComparer.Ordinal),
            version =>
            {
                Assert.Equal("1.0.0", version.Label);
                Assert.Equal("First instructions", version.Snapshot!.Instructions);
                Assert.Empty(version.Snapshot.Skills);
                Assert.Empty(version.Snapshot.Tools);
            },
            version =>
            {
                Assert.Equal("2.0.0", version.Label);
                Assert.Equal("Second instructions", version.Snapshot!.Instructions);
            });
        Assert.Equal("Second instructions", secondPublished.Draft.Instructions);
    }

    [Fact]
    public async Task Publish_rejects_a_client_revision_that_became_stale_before_publish()
    {
        var service = new AgentLifecycleService(new InMemoryAgentRepository());
        AgentDefinition created = (await service.CreateAsync(new CreateAgentCommand("stale-publish"))).Value!;
        AgentDefinition reviewed = (await service.SaveDraftAsync(new SaveAgentDraftCommand(
            created.Id, created.LogicalRevision, "Reviewed instructions", "qwen", AgentOutputMode.Text, null))).Value!;
        AgentDefinition newer = (await service.SaveDraftAsync(new SaveAgentDraftCommand(
            reviewed.Id, reviewed.LogicalRevision, "Newer instructions", "qwen", AgentOutputMode.Text, null))).Value!;

        AgentOperationResult<AgentDefinition> result = await service.PublishAsync(new PublishAgentCommand(reviewed.Id, reviewed.LogicalRevision));

        Assert.False(result.Succeeded);
        Assert.Equal(AgentErrorCodes.RowVersionConflict, result.Error!.Code);
        Assert.Empty(newer.PublishedVersions);
    }

    [Fact]
    public async Task Concurrent_publish_requests_with_the_same_revision_allow_only_one_snapshot()
    {
        var service = new AgentLifecycleService(new InMemoryAgentRepository());
        AgentDefinition created = (await service.CreateAsync(new CreateAgentCommand("concurrent-publish"))).Value!;
        AgentDefinition reviewed = (await service.SaveDraftAsync(new SaveAgentDraftCommand(
            created.Id, created.LogicalRevision, "Publish exactly once", "qwen", AgentOutputMode.Text, null))).Value!;

        AgentOperationResult<AgentDefinition>[] results = await Task.WhenAll(
            service.PublishAsync(new PublishAgentCommand(reviewed.Id, reviewed.LogicalRevision)),
            service.PublishAsync(new PublishAgentCommand(reviewed.Id, reviewed.LogicalRevision)));

        AgentDefinition published = Assert.Single(results, result => result.Succeeded).Value!;
        Assert.Equal(AgentErrorCodes.RowVersionConflict, Assert.Single(results, result => !result.Succeeded).Error!.Code);
        Assert.Single(published.PublishedVersions);
    }
}
