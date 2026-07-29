using EU.Core.Agent.Application.Skills;
using EU.Core.Agent.Infrastructure.Persistence;
using EU.Core.Agent.Infrastructure.Skills;
using Xunit;

namespace EU.Core.Agent.Tests.Skills;

public sealed class SkillPublishingTests
{
    [Fact]
    public async Task Publish_creates_an_immutable_hashed_version_and_survives_repository_restart()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            string database = Path.Combine(directory, "agents.db");
            string files = Path.Combine(directory, "skills");
            var repository = new SqliteSkillRepository(database);
            var service = new SkillLifecycleService(
                repository,
                new ControlledSkillFileStore(files));
            SkillDefinition created = Successful(await service.CreateAsync(
                new CreateSkillCommand(
                    "employee-handbook",
                    "Employee Handbook",
                    "Answers employee questions",
                    "HR")));
            SkillDefinition saved = Successful(await service.SaveFileAsync(
                new SaveSkillFileCommand(
                    created.Id,
                    created.DraftRevision,
                    "references/pay.md",
                    "# Pay")));
            SkillDefinition published = Successful(await service.PublishAsync(
                new PublishSkillCommand(saved.Id, saved.DraftRevision, "1.0.0")));

            SkillVersion version = Assert.Single(published.PublishedVersions);
            Assert.Equal(64, version.ManifestSha256.Length);
            Assert.Equal(2, version.Files.Count);
            Assert.All(
                Directory.EnumerateFiles(
                    Path.Combine(files, "employee-handbook", "versions", "1.0.0"),
                    "*",
                    SearchOption.AllDirectories),
                path => Assert.True((File.GetAttributes(path) & FileAttributes.ReadOnly) != 0));

            SkillDefinition tampered = published with
            {
                DraftRevision = published.DraftRevision + 1,
                PublishedVersions = SkillContractCloner.ReadOnly([
                    version with { ManifestSha256 = new string('f', 64) }
                ])
            };
            Assert.False(await repository.TryReplaceAsync(tampered, published.DraftRevision));

            var reopened = new SqliteSkillRepository(database);
            SkillDefinition? restored = await reopened.GetByIdAsync(created.Id);
            Assert.Equal(version.ManifestSha256, Assert.Single(restored!.PublishedVersions).ManifestSha256);
        }
        finally
        {
            ClearReadOnly(directory);
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Publish_rejects_missing_front_matter_duplicate_version_and_stale_revision()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            var service = new SkillLifecycleService(
                new InMemorySkillRepository(),
                new ControlledSkillFileStore(directory));
            SkillDefinition created = Successful(await service.CreateAsync(
                new CreateSkillCommand("safe-skill", "Safe", "Safe", "General")));
            SkillDefinition invalid = Successful(await service.SaveFileAsync(
                new SaveSkillFileCommand(
                    created.Id,
                    created.DraftRevision,
                    "SKILL.md",
                    "# no front matter")));

            SkillOperationResult<SkillDefinition> invalidPublish = await service.PublishAsync(
                new PublishSkillCommand(invalid.Id, invalid.DraftRevision, "1.0.0"));
            Assert.Equal(SkillErrorCodes.PublishInvalid, invalidPublish.Error?.Code);

            SkillDefinition repaired = Successful(await service.SaveFileAsync(
                new SaveSkillFileCommand(
                    invalid.Id,
                    invalid.DraftRevision,
                    "SKILL.md",
                    "---\nname: safe-skill\ndescription: Safe\n---\n\n# Safe")));
            SkillDefinition published = Successful(await service.PublishAsync(
                new PublishSkillCommand(repaired.Id, repaired.DraftRevision, "1.0.0")));

            SkillOperationResult<SkillDefinition> duplicate = await service.PublishAsync(
                new PublishSkillCommand(published.Id, published.DraftRevision, "1.0.0"));
            SkillOperationResult<SkillDefinition> stale = await service.SaveFileAsync(
                new SaveSkillFileCommand(
                    published.Id,
                    repaired.DraftRevision,
                    "references/stale.md",
                    "stale"));

            Assert.Equal(SkillErrorCodes.VersionConflict, duplicate.Error?.Code);
            Assert.Equal(SkillErrorCodes.RevisionConflict, stale.Error?.Code);
        }
        finally
        {
            ClearReadOnly(directory);
            Directory.Delete(directory, recursive: true);
        }
    }

    private static SkillDefinition Successful(SkillOperationResult<SkillDefinition> result)
    {
        Assert.True(result.Succeeded, result.Error?.Message);
        return Assert.IsType<SkillDefinition>(result.Value);
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"eu-core-agent-skill-publish-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void ClearReadOnly(string root)
    {
        foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
        }
    }
}
