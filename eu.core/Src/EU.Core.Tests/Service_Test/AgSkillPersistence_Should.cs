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
    public async Task Roll_back_created_draft_scaffold_when_database_work_fails()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgSkillDefinition),
            typeof(AgSkillVersion),
            typeof(AgSkillVersionFile),
            typeof(FileAttachment));
        var fileStore = new StubSkillFileStore { FailNextList = true };
        var service = new AgSkillDefinitionServices(
            fixture.CreateRepository<AgSkillDefinition>(),
            fileStore);

        await Assert.ThrowsAsync<SkillFileStoreException>(() => service.CreateAsync(
            new CreateSkillCommand(
                $"skill-{Guid.NewGuid():N}",
                "Failed Skill",
                string.Empty,
                string.Empty)));

        Assert.Empty(fileStore.Draft);
        Assert.Equal(0, await fixture.Db.Queryable<AgSkillDefinition>().CountAsync());
    }

    [Fact]
    public async Task Persist_draft_revision_published_manifest_and_file_hashes()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgSkillDefinition),
            typeof(AgSkillVersion),
            typeof(AgSkillVersionFile),
            typeof(FileAttachment));
        var fileStore = new StubSkillFileStore();
        var service = new AgSkillDefinitionServices(
            fixture.CreateRepository<AgSkillDefinition>(),
            fileStore);
        string code = $"skill-{Guid.NewGuid():N}";

        SkillOperationResult<SkillDefinition> created = await service.CreateAsync(
            new CreateSkillCommand(code, "Skill A", "description", "business"));
        Assert.True(created.Succeeded);
        SkillDefinition initial = Assert.IsType<SkillDefinition>(created.Value);
        FileAttachment initialAttachment = Assert.Single(
            await fixture.Db.Queryable<FileAttachment>()
                .Where(value =>
                    value.MasterId == initial.Id &&
                    value.ImageType == "agent-skill-draft")
                .ToListAsync());
        Assert.Equal("SKILL.md", initialAttachment.OriginalFileName);
        Assert.Equal($"{code}/draft/", initialAttachment.Path);
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
        FileAttachment savedAttachment = Assert.Single(
            await fixture.Db.Queryable<FileAttachment>()
                .Where(value =>
                    value.MasterId == initial.Id &&
                    value.ImageType == "agent-skill-draft")
                .ToListAsync());
        Assert.Equal(initialAttachment.ID, savedAttachment.ID);
        Assert.Equal(fileStore.Draft["SKILL.md"].Length, savedAttachment.Length);
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
        FileAttachment publishedAttachment = Assert.Single(
            await fixture.Db.Queryable<FileAttachment>()
                .Where(value =>
                    value.MasterId == version.Id &&
                    value.ImageType == "agent-skill-version")
                .ToListAsync());
        Assert.Equal($"{code}/versions/1.0.0/", publishedAttachment.Path);
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

        await fixture.Db.Deleteable<FileAttachment>().ExecuteCommandAsync();
        await service.ReconcileFileAttachmentsAsync();
        Assert.Equal(
            2,
            await fixture.Db.Queryable<FileAttachment>()
                .Where(value =>
                    value.ImageType == "agent-skill-draft" ||
                    value.ImageType == "agent-skill-version")
                .CountAsync());

        FileAttachment draftAttachment = await fixture.Db.Queryable<FileAttachment>()
            .Where(value => value.ImageType == "agent-skill-draft")
            .FirstAsync();
        await fixture.Db.Updateable<FileAttachment>()
            .SetColumns(value => value.IsDeleted == true)
            .Where(value => value.ID == draftAttachment.ID)
            .ExecuteCommandAsync();
        await service.ReconcileFileAttachmentsAsync();
        FileAttachment restoredAttachment = await fixture.Db.Queryable<FileAttachment>()
            .Filter(null, true)
            .Where(value => value.ID == draftAttachment.ID)
            .SingleAsync();
        Assert.False(restoredAttachment.IsDeleted);
        Assert.True(restoredAttachment.IsActive);
    }

    [Fact]
    public async Task Roll_back_file_when_attachment_index_update_fails()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgSkillDefinition),
            typeof(AgSkillVersion),
            typeof(AgSkillVersionFile),
            typeof(FileAttachment));
        var fileStore = new StubSkillFileStore();
        var service = new AgSkillDefinitionServices(
            fixture.CreateRepository<AgSkillDefinition>(),
            fileStore);
        SkillDefinition definition = Assert.IsType<SkillDefinition>((await service.CreateAsync(
            new CreateSkillCommand(
                $"skill-{Guid.NewGuid():N}",
                "Rollback Skill",
                string.Empty,
                string.Empty))).Value);
        string original = fileStore.Draft["SKILL.md"];
        fileStore.FailNextList = true;

        SkillOperationResult<SkillDefinition> result = await service.SaveFileAsync(
            new SaveSkillFileCommand(definition.Id, 0, "SKILL.md", "changed"));

        Assert.False(result.Succeeded);
        Assert.Equal(SkillErrorCodes.PathInvalid, result.Error?.Code);
        Assert.Equal(original, fileStore.Draft["SKILL.md"]);
        Assert.Equal(0, (await service.GetAsync(definition.Id))!.DraftRevision);
    }

    [Fact]
    public async Task Reject_file_name_that_cannot_be_represented_by_attachment_schema()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgSkillDefinition),
            typeof(AgSkillVersion),
            typeof(AgSkillVersionFile),
            typeof(FileAttachment));
        var fileStore = new StubSkillFileStore();
        var service = new AgSkillDefinitionServices(
            fixture.CreateRepository<AgSkillDefinition>(),
            fileStore);
        SkillDefinition definition = Assert.IsType<SkillDefinition>((await service.CreateAsync(
            new CreateSkillCommand(
                $"skill-{Guid.NewGuid():N}",
                "Path Skill",
                string.Empty,
                string.Empty))).Value);
        string longName = $"references/{new string('a', 61)}.txt";

        SkillOperationResult<SkillDefinition> result = await service.SaveFileAsync(
            new SaveSkillFileCommand(definition.Id, 0, longName, "content"));

        Assert.False(result.Succeeded);
        Assert.Equal(SkillErrorCodes.PathInvalid, result.Error?.Code);
        Assert.DoesNotContain(longName, fileStore.Draft.Keys);
        Assert.Equal(0, (await service.GetAsync(definition.Id))!.DraftRevision);
    }

    [Fact]
    public async Task Track_nested_draft_attachment_and_remove_it_on_delete()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgSkillDefinition),
            typeof(AgSkillVersion),
            typeof(AgSkillVersionFile),
            typeof(FileAttachment));
        var fileStore = new StubSkillFileStore();
        var service = new AgSkillDefinitionServices(
            fixture.CreateRepository<AgSkillDefinition>(),
            fileStore);
        string code = $"skill-{Guid.NewGuid():N}";
        SkillDefinition definition = Assert.IsType<SkillDefinition>((await service.CreateAsync(
            new CreateSkillCommand(code, "Nested Skill", string.Empty, string.Empty))).Value);

        SkillOperationResult<SkillDefinition> saved = await service.SaveFileAsync(
            new SaveSkillFileCommand(
                definition.Id,
                0,
                "references/guide.md",
                "nested content"));
        Assert.True(saved.Succeeded);
        FileAttachment nested = Assert.Single(
            await fixture.Db.Queryable<FileAttachment>()
                .Where(value =>
                    value.MasterId == definition.Id &&
                    value.FileName == "guide.md")
                .ToListAsync());
        Assert.Equal($"{code}/draft/references/", nested.Path);

        SkillOperationResult<SkillDefinition> deleted = await service.DeleteFileAsync(
            new DeleteSkillFileCommand(
                definition.Id,
                1,
                "references/guide.md"));
        Assert.True(deleted.Succeeded);
        Assert.False(await fixture.Db.Queryable<FileAttachment>()
            .Where(value => value.ID == nested.ID)
            .AnyAsync());
        Assert.Single(await fixture.Db.Queryable<FileAttachment>()
            .Where(value =>
                value.MasterId == definition.Id &&
                value.ImageType == "agent-skill-draft")
            .ToListAsync());
    }

    [Fact]
    public async Task Block_archive_while_an_enabled_agent_references_the_published_version()
    {
        using var fixture = new AgentPersistenceSqliteFixture(
            typeof(AgSkillDefinition),
            typeof(AgSkillVersion),
            typeof(AgSkillVersionFile),
            typeof(FileAttachment),
            typeof(AgAgentDefinition),
            typeof(AgAgentVersion),
            typeof(AgAgentVersionBinding));
        var service = new AgSkillDefinitionServices(
            fixture.CreateRepository<AgSkillDefinition>(),
            new StubSkillFileStore());
        SkillDefinition definition = Assert.IsType<SkillDefinition>((await service.CreateAsync(
            new CreateSkillCommand(
                $"skill-{Guid.NewGuid():N}",
                "Referenced Skill",
                string.Empty,
                string.Empty))).Value);
        SkillDefinition published = Assert.IsType<SkillDefinition>((await service.PublishAsync(
            new PublishSkillCommand(definition.Id, 0, "1.0.0"))).Value);
        Guid skillVersionId = Assert.Single(published.PublishedVersions).Id;
        Guid agentId = Guid.NewGuid();
        Guid agentVersionId = Guid.NewGuid();
        await fixture.Db.Insertable(new AgAgentDefinition
        {
            ID = agentId,
            Code = "skill-consumer",
            Name = "Skill Consumer",
            Description = string.Empty,
            RuntimeStatus = "Enabled",
            LogicalRevision = 0
        }).ExecuteCommandAsync();
        await fixture.Db.Insertable(new AgAgentVersion
        {
            ID = agentVersionId,
            AgentId = agentId,
            Ordinal = 0,
            Label = "draft",
            IsDraft = true,
            Instructions = string.Empty,
            ModelProfileId = string.Empty,
            OutputMode = "Text"
        }).ExecuteCommandAsync();
        await fixture.Db.Insertable(new AgAgentVersionBinding
        {
            ID = Guid.NewGuid(),
            VersionId = agentVersionId,
            Scope = "Version",
            BindingType = "Skill",
            Ordinal = 0,
            ReferenceId = skillVersionId,
            ReferenceCode = definition.Code,
            ReferenceName = definition.Name,
            ReferenceDescription = string.Empty
        }).ExecuteCommandAsync();

        SkillOperationResult<SkillDefinition> blocked = await service.SetArchivedAsync(
            new SetSkillArchiveCommand(definition.Id, published.DraftRevision, true));

        Assert.False(blocked.Succeeded);
        Assert.Equal(SkillErrorCodes.ArchiveBlocked, blocked.Error?.Code);
        Assert.Contains("skill-consumer", blocked.Error?.Message);

        await fixture.Db.Updateable<AgAgentDefinition>()
            .SetColumns(value => value.RuntimeStatus == "Disabled")
            .Where(value => value.ID == agentId)
            .ExecuteCommandAsync();
        SkillOperationResult<SkillDefinition> archived = await service.SetArchivedAsync(
            new SetSkillArchiveCommand(definition.Id, published.DraftRevision, true));
        Assert.True(archived.Succeeded);
        Assert.Equal(SkillStatus.Archived, archived.Value?.Status);
    }

    private sealed class StubSkillFileStore : ISkillFileStore
    {
        public Dictionary<string, string> Draft { get; } = new(StringComparer.Ordinal);

        public bool FailNextList { get; set; }

        public Task<bool> EnsureDraftAsync(
            string skillCode,
            string name,
            string description,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Draft["SKILL.md"] = $"# {name}\n\n{description}";
            return Task.FromResult(true);
        }

        public Task RollbackDraftCreationAsync(
            string skillCode,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Draft.Clear();
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SkillFileEntry>> ListDraftAsync(
            string skillCode,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (FailNextList)
            {
                FailNextList = false;
                throw new SkillFileStoreException(
                    SkillErrorCodes.PathInvalid,
                    "Simulated attachment indexing failure.");
            }

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
            if (!Draft.TryGetValue(relativePath, out string? content))
            {
                throw new SkillFileStoreException(
                    SkillErrorCodes.FileMissing,
                    "The Skill file was not found.");
            }

            return Task.FromResult(content);
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
