using EU.Core.IServices.MainAgent;
using EU.Core.Model.Entity;
using EU.Core.Services;
using Xunit;

#nullable enable

namespace EU.Core.Tests.Service_Test;

public sealed class AgMainAgentAssignmentPersistence_Should
{
    [Fact]
    public async Task Create_and_replace_assignment_with_optimistic_revision()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgMainAgentAssignment));
        var service = new AgMainAgentAssignmentServices(
            fixture.CreateRepository<AgMainAgentAssignment>());
        DateTimeOffset now = DateTimeOffset.Parse("2026-08-16T09:00:00Z");
        var initial = new MainAgentAssignment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            0,
            now);

        Assert.True(await service.TryReplaceAsync(initial, null));
        Assert.False(await service.TryReplaceAsync(initial, null));
        Assert.Equal(initial, await service.GetAsync());

        MainAgentAssignment replacement = initial with
        {
            AgentId = Guid.NewGuid(),
            AgentVersionId = Guid.NewGuid(),
            LogicalRevision = 1,
            UpdatedAtUtc = now.AddMinutes(1)
        };
        Assert.False(await service.TryReplaceAsync(replacement, 1));
        Assert.True(await service.TryReplaceAsync(replacement, 0));
        Assert.Equal(replacement, await service.GetAsync());

        MainAgentAssignment stale = replacement with
        {
            LogicalRevision = 2,
            UpdatedAtUtc = now.AddMinutes(2)
        };
        Assert.False(await service.TryReplaceAsync(stale, 0));
        Assert.Equal(replacement, await service.GetAsync());
    }
}
