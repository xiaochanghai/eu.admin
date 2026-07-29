using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Infrastructure.Persistence;
using Xunit;

namespace EU.Core.Agent.Tests.Agents;

public sealed class SqliteAgentRepositoryTests
{
    [Fact]
    public async Task Repository_persists_draft_and_published_versions_across_instances()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            string databasePath = Path.Combine(directory, "agents.db");
            var lifecycle = new AgentLifecycleService(new SqliteAgentRepository(databasePath));
            AgentDefinition created = Successful(
                await lifecycle.CreateAsync(
                    new CreateAgentCommand("support-agent", "Support", "Persistent Agent")));
            AgentDefinition saved = Successful(
                await lifecycle.SaveDraftAsync(
                    new SaveAgentDraftCommand(
                        created.Id,
                        created.LogicalRevision,
                        "Answer support questions.",
                        "qwen-safe",
                        AgentOutputMode.Text,
                        null)));
            AgentDefinition published = Successful(
                await lifecycle.PublishAsync(
                    new PublishAgentCommand(saved.Id, saved.LogicalRevision)));

            var reopened = new SqliteAgentRepository(databasePath);
            AgentDefinition? restored = await reopened.GetByIdAsync(created.Id);

            Assert.NotNull(restored);
            Assert.Equal("support-agent", restored.Code);
            Assert.Equal("Support", restored.Name);
            Assert.Equal(published.LogicalRevision, restored.LogicalRevision);
            AgentVersion version = Assert.Single(restored.PublishedVersions);
            Assert.Equal("1.0.0", version.Label);
            Assert.Equal("Answer support questions.", version.Snapshot?.Instructions);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Repository_enforces_unique_code_and_atomic_logical_revision()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            var repository = new SqliteAgentRepository(
                Path.Combine(directory, "agents.db"));
            AgentDefinition original = Definition("concurrency-agent");
            AgentDefinition duplicateCode = Definition("concurrency-agent");

            Assert.True(await repository.TryCreateAsync(original));
            Assert.False(await repository.TryCreateAsync(duplicateCode));

            AgentDefinition firstUpdate = original with
            {
                Name = "First",
                LogicalRevision = 1
            };
            AgentDefinition staleUpdate = original with
            {
                Name = "Stale",
                LogicalRevision = 1
            };

            Assert.True(await repository.TryReplaceAsync(firstUpdate, 0));
            Assert.False(await repository.TryReplaceAsync(staleUpdate, 0));
            Assert.Equal("First", (await repository.GetByIdAsync(original.Id))?.Name);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Repository_preserves_list_search_and_status_filters()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            var repository = new SqliteAgentRepository(
                Path.Combine(directory, "agents.db"));
            AgentDefinition enabled = Definition(
                "billing-agent",
                "Billing",
                "Handles invoices",
                AgentRuntimeStatus.Enabled);
            AgentDefinition disabled = Definition(
                "shipping-agent",
                "Shipping",
                "Tracks parcels",
                AgentRuntimeStatus.Disabled);
            Assert.True(await repository.TryCreateAsync(enabled));
            Assert.True(await repository.TryCreateAsync(disabled));

            IReadOnlyList<AgentDefinition> searched = await repository.ListAsync(
                new AgentDefinitionQuery("invoice"));
            IReadOnlyList<AgentDefinition> filtered = await repository.ListAsync(
                new AgentDefinitionQuery(RuntimeStatus: AgentRuntimeStatus.Disabled));

            Assert.Equal(enabled.Id, Assert.Single(searched).Id);
            Assert.Equal(disabled.Id, Assert.Single(filtered).Id);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static AgentDefinition Definition(
        string code,
        string name = "",
        string description = "",
        AgentRuntimeStatus status = AgentRuntimeStatus.Enabled)
    {
        var draft = new AgentVersion(
            Guid.NewGuid(),
            "0.1.0",
            true,
            string.Empty,
            string.Empty,
            AgentOutputMode.Text,
            null,
            null,
            null);
        return new AgentDefinition(
            Guid.NewGuid(),
            code,
            name,
            description,
            status,
            0,
            draft,
            AgentContractCloner.ReadOnly(Array.Empty<AgentVersion>()));
    }

    private static AgentDefinition Successful(
        AgentOperationResult<AgentDefinition> result)
    {
        Assert.True(result.Succeeded, result.Error?.Message);
        return Assert.IsType<AgentDefinition>(result.Value);
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"eu-core-agent-sqlite-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
