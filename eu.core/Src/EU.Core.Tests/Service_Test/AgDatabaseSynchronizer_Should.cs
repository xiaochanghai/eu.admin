using EU.Core.Common.Seed;
using EU.Core.Model;
using EU.Core.Model.Entity;
using SqlSugar;
using Xunit;

#nullable enable

namespace EU.Core.Tests.Service_Test;

[CollectionDefinition(
    AgDatabaseSynchronizerCollection.CollectionName,
    DisableParallelization = true)]
public sealed class AgDatabaseSynchronizerCollection
{
    public const string CollectionName = "Agent database synchronization";
}

[Collection(AgDatabaseSynchronizerCollection.CollectionName)]
public sealed class AgDatabaseSynchronizer_Should
{
    [Fact]
    public async Task Register_and_create_every_agent_entity_table()
    {
        using var db = CreateScope("all");
        SqlSugarScopeProvider source = db.GetConnectionScope("source-all");
        SqlSugarScopeProvider target = db.GetConnectionScope("target-all");
        source.Ado.Open();
        target.Ado.Open();
        Type[] entityTypes = GetAgentEntityTypes();
        source.CodeFirst.InitTables(entityTypes.Append(typeof(FileAttachment)).ToArray());
        string[] missingSourceTables = entityTypes
            .Select(type => source.EntityMaintenance.GetEntityInfo(type).DbTableName)
            .Where(tableName => !source.DbMaintenance.IsAnyTable(tableName, false))
            .OrderBy(tableName => tableName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Assert.True(
            missingSourceTables.Length == 0,
            $"Source Agent tables were not created: {string.Join(", ", missingSourceTables)}");

        AgentDatabaseSyncResult result = await AgentDatabaseSynchronizer.SyncAsync(
            db,
            new AgentDatabaseSyncRequest
            {
                SourceConfigId = "source-all",
                TargetConfigId = "target-all",
                SyncStructure = true,
                ReplaceData = false
            });

        Assert.Equal(entityTypes.Length + 1, result.Tables.Count);
        Assert.Equal(
            entityTypes.Length + 1,
            result.Tables.Select(value => value.TableName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());
        Assert.All(
            result.Tables,
            table => Assert.True(
                target.DbMaintenance.IsAnyTable(table.TableName, false),
                $"Target Agent table was not created: {table.TableName}"));
    }

    [Fact]
    public async Task Replace_skill_attachment_index_without_touching_other_attachments()
    {
        using var db = CreateScope("skill-attachments");
        SqlSugarScopeProvider source = db.GetConnectionScope("source-skill-attachments");
        SqlSugarScopeProvider target = db.GetConnectionScope("target-skill-attachments");
        source.Ado.Open();
        target.Ado.Open();
        source.CodeFirst.InitTables(typeof(AgSkillDefinition), typeof(FileAttachment));
        target.CodeFirst.InitTables(typeof(AgSkillDefinition), typeof(FileAttachment));

        Guid skillId = Guid.NewGuid();
        await source.Insertable(new AgSkillDefinition
        {
            ID = skillId,
            Code = "sync-skill",
            Name = "Synchronized Skill",
            Description = string.Empty,
            Category = string.Empty,
            Status = "Active",
            DraftRevision = 0
        }).ExecuteCommandAsync();
        Guid sourceAttachmentId = Guid.NewGuid();
        await source.Insertable(new FileAttachment
        {
            ID = sourceAttachmentId,
            MasterId = skillId,
            OriginalFileName = "SKILL.md",
            FileName = "SKILL.md",
            FileExt = "md",
            Path = "sync-skill/draft/",
            Length = 12,
            ImageType = "agent-skill-draft"
        }).ExecuteCommandAsync();
        Guid unrelatedAttachmentId = Guid.NewGuid();
        await target.Insertable(new FileAttachment
        {
            ID = unrelatedAttachmentId,
            MasterId = Guid.NewGuid(),
            OriginalFileName = "report.pdf",
            FileName = "report.pdf",
            FileExt = "pdf",
            Path = "upload/report.pdf",
            Length = 24,
            ImageType = "business-document"
        }).ExecuteCommandAsync();
        await target.Insertable(new FileAttachment
        {
            ID = Guid.NewGuid(),
            MasterId = Guid.NewGuid(),
            OriginalFileName = "obsolete.md",
            FileName = "obsolete.md",
            FileExt = "md",
            Path = "obsolete/draft/obsolete.md",
            Length = 1,
            ImageType = "agent-skill-draft"
        }).ExecuteCommandAsync();

        AgentDatabaseSyncResult result = await AgentDatabaseSynchronizer.SyncAsync(
            db,
            new AgentDatabaseSyncRequest
            {
                SourceConfigId = "source-skill-attachments",
                TargetConfigId = "target-skill-attachments",
                Tables = ["AgSkillDefinition"],
                SyncStructure = false,
                ReplaceData = true,
                ConfirmReplaceData = true
            });

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.Tables.Count);
        FileAttachment copied = Assert.Single(
            await target.Queryable<FileAttachment>()
                .Where(value => value.ImageType == "agent-skill-draft")
                .ToListAsync());
        Assert.Equal(sourceAttachmentId, copied.ID);
        Assert.Equal(skillId, copied.MasterId);
        Assert.NotNull(await target.Queryable<FileAttachment>()
            .InSingleAsync(unrelatedAttachmentId));
    }

    [Fact]
    public async Task Create_target_structure_and_replace_selected_table_data()
    {
        using var db = CreateScope("replace");
        SqlSugarScopeProvider source = db.GetConnectionScope("source-replace");
        SqlSugarScopeProvider target = db.GetConnectionScope("target-replace");
        source.Ado.Open();
        target.Ado.Open();
        source.CodeFirst.InitTables(typeof(AgAgentDefinition), typeof(AgAgentVersion));
        target.CodeFirst.InitTables(typeof(AgAgentDefinition));

        Guid agentId = Guid.NewGuid();
        Guid versionId = Guid.NewGuid();
        await source.Insertable(new AgAgentDefinition
        {
            ID = agentId,
            Code = "sync-agent",
            Name = "Source Agent",
            Description = string.Empty,
            RuntimeStatus = "Enabled",
            LogicalRevision = 0
        }).ExecuteCommandAsync();
        await source.Insertable(new AgAgentVersion
        {
            ID = versionId,
            AgentId = agentId,
            Ordinal = 0,
            Label = "0.1.0",
            IsDraft = true,
            Instructions = string.Empty,
            ModelProfileId = string.Empty,
            OutputMode = "Text"
        }).ExecuteCommandAsync();
        await target.Insertable(new AgAgentDefinition
        {
            ID = Guid.NewGuid(),
            Code = "obsolete-agent",
            Name = "Obsolete Agent",
            Description = string.Empty,
            RuntimeStatus = "Disabled",
            LogicalRevision = 0
        }).ExecuteCommandAsync();

        AgentDatabaseSyncResult result = await AgentDatabaseSynchronizer.SyncAsync(
            db,
            new AgentDatabaseSyncRequest
            {
                SourceConfigId = "source-replace",
                TargetConfigId = "target-replace",
                Tables = ["AgAgentDefinition", "AgAgentVersion"],
                SyncStructure = true,
                ReplaceData = true,
                ConfirmReplaceData = true,
                BatchSize = 1
            });

        Assert.True(result.StructureSynchronized);
        Assert.True(result.DataReplaced);
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.Tables.Count);
        AgAgentDefinition copiedDefinition = Assert.Single(
            await target.Queryable<AgAgentDefinition>().ToListAsync());
        AgAgentVersion copiedVersion = Assert.Single(
            await target.Queryable<AgAgentVersion>().ToListAsync());
        Assert.Equal(agentId, copiedDefinition.ID);
        Assert.Equal("Source Agent", copiedDefinition.Name);
        Assert.Equal(versionId, copiedVersion.ID);
        Assert.Equal(agentId, copiedVersion.AgentId);
    }

    [Fact]
    public async Task Require_explicit_confirmation_before_replacing_target_data()
    {
        using var db = CreateScope("confirmation");

        ArgumentException error = await Assert.ThrowsAsync<ArgumentException>(() =>
            AgentDatabaseSynchronizer.SyncAsync(
                db,
                new AgentDatabaseSyncRequest
                {
                    SourceConfigId = "source-confirmation",
                    TargetConfigId = "target-confirmation",
                    ReplaceData = true,
                    ConfirmReplaceData = false
                }));

        Assert.Contains("ConfirmReplaceData", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reject_unregistered_and_duplicate_table_selections()
    {
        using var db = CreateScope("table-validation");

        ArgumentException unregistered = await Assert.ThrowsAsync<ArgumentException>(() =>
            AgentDatabaseSynchronizer.SyncAsync(
                db,
                new AgentDatabaseSyncRequest
                {
                    SourceConfigId = "source-table-validation",
                    TargetConfigId = "target-table-validation",
                    Tables = ["NotAnAgentTable"]
                }));
        Assert.Contains("不是已登记的 Agent 实体表", unregistered.Message, StringComparison.Ordinal);

        ArgumentException duplicate = await Assert.ThrowsAsync<ArgumentException>(() =>
            AgentDatabaseSynchronizer.SyncAsync(
                db,
                new AgentDatabaseSyncRequest
                {
                    SourceConfigId = "source-table-validation",
                    TargetConfigId = "target-table-validation",
                    Tables = ["AgAgentDefinition", "agagentdefinition"]
                }));
        Assert.Contains("被重复指定", duplicate.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reject_the_same_source_and_target_connection()
    {
        using var db = CreateScope("same-connection");

        ArgumentException error = await Assert.ThrowsAsync<ArgumentException>(() =>
            AgentDatabaseSynchronizer.SyncAsync(
                db,
                new AgentDatabaseSyncRequest
                {
                    SourceConfigId = " SOURCE-SAME-CONNECTION ",
                    TargetConfigId = "source-same-connection"
                }));

        Assert.Contains("源数据库与目标数据库不能相同", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10001)]
    public async Task Reject_batch_sizes_outside_the_safe_range(int batchSize)
    {
        using var db = CreateScope($"batch-{batchSize}");

        ArgumentOutOfRangeException error =
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                AgentDatabaseSynchronizer.SyncAsync(
                    db,
                    new AgentDatabaseSyncRequest
                    {
                        SourceConfigId = $"source-batch-{batchSize}",
                        TargetConfigId = $"target-batch-{batchSize}",
                        BatchSize = batchSize
                    }));

        Assert.Equal("BatchSize", error.ParamName);
    }

    [Fact]
    public async Task Serialize_concurrent_replacements_in_the_same_application_instance()
    {
        using var db = CreateScope("concurrent");
        SqlSugarScopeProvider source = db.GetConnectionScope("source-concurrent");
        SqlSugarScopeProvider target = db.GetConnectionScope("target-concurrent");
        source.Ado.Open();
        target.Ado.Open();
        source.CodeFirst.InitTables(typeof(AgAgentDefinition));
        target.CodeFirst.InitTables(typeof(AgAgentDefinition));
        Guid agentId = Guid.NewGuid();
        await source.Insertable(new AgAgentDefinition
        {
            ID = agentId,
            Code = "concurrent-agent",
            Name = "Concurrent Agent",
            Description = string.Empty,
            RuntimeStatus = "Enabled",
            LogicalRevision = 0
        }).ExecuteCommandAsync();
        var request = new AgentDatabaseSyncRequest
        {
            SourceConfigId = "source-concurrent",
            TargetConfigId = "target-concurrent",
            Tables = ["AgAgentDefinition"],
            SyncStructure = false,
            ReplaceData = true,
            ConfirmReplaceData = true
        };

        await Task.WhenAll(
            AgentDatabaseSynchronizer.SyncAsync(db, request),
            AgentDatabaseSynchronizer.SyncAsync(db, request));

        AgAgentDefinition copied = Assert.Single(
            await target.Queryable<AgAgentDefinition>().ToListAsync());
        Assert.Equal(agentId, copied.ID);
    }

    [Fact]
    public async Task Roll_back_target_changes_when_a_later_table_copy_fails()
    {
        using var db = CreateScope("rollback");
        SqlSugarScopeProvider source = db.GetConnectionScope("source-rollback");
        SqlSugarScopeProvider target = db.GetConnectionScope("target-rollback");
        source.Ado.Open();
        target.Ado.Open();
        source.CodeFirst.InitTables(typeof(AgAgentDefinition), typeof(AgAgentVersion));
        target.CodeFirst.InitTables(typeof(AgAgentDefinition));
        target.Ado.ExecuteCommand(
            "CREATE TABLE [AgAgentVersion] ([ID] TEXT NOT NULL PRIMARY KEY)");

        Guid sourceAgentId = Guid.NewGuid();
        await source.Insertable(new AgAgentDefinition
        {
            ID = sourceAgentId,
            Code = "source-agent",
            Name = "Source Agent",
            Description = string.Empty,
            RuntimeStatus = "Enabled",
            LogicalRevision = 0
        }).ExecuteCommandAsync();
        await source.Insertable(new AgAgentVersion
        {
            ID = Guid.NewGuid(),
            AgentId = sourceAgentId,
            Ordinal = 0,
            Label = "0.1.0",
            IsDraft = true,
            Instructions = string.Empty,
            ModelProfileId = string.Empty,
            OutputMode = "Text"
        }).ExecuteCommandAsync();
        Guid existingTargetId = Guid.NewGuid();
        await target.Insertable(new AgAgentDefinition
        {
            ID = existingTargetId,
            Code = "existing-agent",
            Name = "Existing Agent",
            Description = string.Empty,
            RuntimeStatus = "Disabled",
            LogicalRevision = 0
        }).ExecuteCommandAsync();

        await Assert.ThrowsAnyAsync<Exception>(() => AgentDatabaseSynchronizer.SyncAsync(
            db,
            new AgentDatabaseSyncRequest
            {
                SourceConfigId = "source-rollback",
                TargetConfigId = "target-rollback",
                Tables = ["AgAgentDefinition", "AgAgentVersion"],
                SyncStructure = false,
                ReplaceData = true,
                ConfirmReplaceData = true
            }));

        AgAgentDefinition preserved = Assert.Single(
            await target.Queryable<AgAgentDefinition>().ToListAsync());
        Assert.Equal(existingTargetId, preserved.ID);
        Assert.Equal("Existing Agent", preserved.Name);
    }

    private static Type[] GetAgentEntityTypes() => typeof(AgAgentDefinition).Assembly
        .GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false } &&
                       string.Equals(
                           type.Namespace,
                           "EU.Core.Model.Entity",
                           StringComparison.Ordinal) &&
                       type.Name.StartsWith("Ag", StringComparison.Ordinal))
        .OrderBy(type => type.Name, StringComparer.Ordinal)
        .ToArray();

    private static SqlSugarScope CreateScope(string scopeName)
    {
        string databaseToken = Guid.NewGuid().ToString("N");
        return new SqlSugarScope(
            new List<ConnectionConfig>
            {
                new()
                {
                    ConfigId = $"source-{scopeName}",
                    ConnectionString =
                        $"Data Source=agent-sync-source-{databaseToken};Mode=Memory;Cache=Shared",
                    DbType = DbType.Sqlite,
                    IsAutoCloseConnection = false
                },
                new()
                {
                    ConfigId = $"target-{scopeName}",
                    ConnectionString =
                        $"Data Source=agent-sync-target-{databaseToken};Mode=Memory;Cache=Shared",
                    DbType = DbType.Sqlite,
                    IsAutoCloseConnection = false
                }
            });
    }
}
