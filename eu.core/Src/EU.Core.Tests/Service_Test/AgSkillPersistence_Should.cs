using System.Security.Cryptography;
using System.Text;
using EU.Core.Agent.Application.Skills;
using EU.Core.Model.Entity;
using EU.Core.Services;
using Xunit;

#nullable enable

namespace EU.Core.Tests.Service_Test;

public sealed class AgSkillPersistence_Should
{
    [Fact]
    public async Task Persist_draft_revision_published_manifest_and_file_hashes()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgSkillDefinition),
            typeof(AgSkillVersion),
            typeof(AgSkillVersionFile));
        var fileStore = new StubSkillFileStore();
        var service = new AgSkillDefinitionServices(
            fixture.CreateRepository<AgSkillDefinition>(),
            fileStore);
        string code = $"skill-{Guid.NewGuid():N}";

        SkillOperationResult<SkillDefinition> created = await service.CreateAsync(
            new CreateSkillCommand(code, "Skill A", "description", "business"));
        Assert.True(created.Succeeded);
        SkillDefinition initial = Assert.IsType<SkillDefinition>(created.Value);
        Assert.False((await service.CreateAsync(
            new CreateSkillCommand(code, "Duplicate", string.Empty, string.Empty))).Succeeded);
        SkillOperationResult<string> initialFile = await service.ReadFileAsync(
            initial.Id,
            "SKILL.md");
        Assert.Equal("# Skill A\n\ndescription", initialFile.Value);

        SkillOperationResult<SkillDefinition> saved = await service.SaveFileAsync(
            new SaveSkillFileCommand(
                initial.Id,
                0,
                "SKILL.md",
                "# Skill A\n\nAlways return SKILL_OK."));
        Assert.True(saved.Succeeded);
        Assert.False((await service.SaveFileAsync(
            new SaveSkillFileCommand(initial.Id, 0, "SKILL.md", "stale"))).Succeeded);
        SkillDefinition draft = Assert.IsType<SkillDefinition>(saved.Value);
        Assert.Equal(1, draft.DraftRevision);
        Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<SkillFileEntry>>(
            (await service.ListFilesAsync(initial.Id)).Value));

        SkillOperationResult<SkillDefinition> published = await service.PublishAsync(
            new PublishSkillCommand(initial.Id, 1, "1.0.0"));
        Assert.True(published.Succeeded);
        SkillDefinition ready = Assert.IsType<SkillDefinition>(published.Value);
        Assert.Equal(2, ready.DraftRevision);
        SkillVersion version = Assert.Single(ready.PublishedVersions);
        Assert.Equal("1.0.0", version.Label);
        SkillFileHash file = Assert.Single(version.Files);
        Assert.Equal("SKILL.md", file.Path);
        Assert.Equal(fileStore.Draft["SKILL.md"].Length, file.Size);
        Assert.True(await service.ExistsAsync(version.Id));

        PublishedSkillReference reference = Assert.Single(
            await ((IPublishedSkillVersionCatalog)service).ListAsync());
        Assert.Equal(version.Id, reference.VersionId);
        Assert.Equal(code, reference.SkillCode);
        SkillDefinition persisted = Assert.IsType<SkillDefinition>(
            await service.GetAsync(initial.Id));
        Assert.Equal(version.ManifestSha256, Assert.Single(persisted.PublishedVersions).ManifestSha256);
        Assert.Equal("1.0.0", Assert.Single(
            await service.ListAsync(new SkillQuery(Search: "Skill A"))).CurrentPublishedLabel);
    }

    private sealed class StubSkillFileStore : ISkillFileStore
    {
        public Dictionary<string, string> Draft { get; } = new(StringComparer.Ordinal);

        public Task EnsureDraftAsync(
            string skillCode,
            string name,
            string description,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Draft["SKILL.md"] = $"# {name}\n\n{description}";
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SkillFileEntry>> ListDraftAsync(
            string skillCode,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<SkillFileEntry>>(Draft
                .OrderBy(value => value.Key, StringComparer.Ordinal)
                .Select(value => new SkillFileEntry(value.Key, value.Value.Length))
                .ToArray());
        }

        public Task<string> ReadDraftTextAsync(
            string skillCode,
            string relativePath,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Draft[relativePath]);
        }

        public Task WriteDraftTextAsync(
            string skillCode,
            string relativePath,
            string content,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Draft[relativePath] = content;
            return Task.CompletedTask;
        }

        public Task DeleteDraftAsync(
            string skillCode,
            string relativePath,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Draft.Remove(relativePath);
            return Task.CompletedTask;
        }

        public Task<SkillPublishArtifact> PublishAsync(
            string skillCode,
            string versionLabel,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SkillFileHash[] files = Draft
                .OrderBy(value => value.Key, StringComparer.Ordinal)
                .Select(value => new SkillFileHash(
                    value.Key,
                    value.Value.Length,
                    Sha256(value.Value)))
                .ToArray();
            return Task.FromResult(new SkillPublishArtifact(
                versionLabel,
                Sha256(string.Join('|', files.Select(value => value.Sha256))),
                files));
        }

        public Task DeletePublishedAsync(
            string skillCode,
            string versionLabel,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        private static string Sha256(string value) => Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
