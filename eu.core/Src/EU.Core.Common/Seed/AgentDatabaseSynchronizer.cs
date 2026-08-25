using System.Data;
using System.Globalization;
using EU.Core.Model;
using EU.Core.Model.Entity;
using SqlSugar;

namespace EU.Core.Common.Seed;

/// <summary>基于 SqlSugar 的 Agent 表结构与数据同步器。</summary>
public static class AgentDatabaseSynchronizer
{
    private const string DraftSkillAttachmentType = "agent-skill-draft";
    private const string PublishedSkillAttachmentType = "agent-skill-version";
    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    private static readonly string[] AgentTableOrder =
    [
        "AgAgentDefinition", "AgAgentVersion", "AgAgentVersionSnapshot", "AgAgentVersionBinding",
        "AgSkillDefinition", "AgSkillVersion", "AgSkillVersionFile",
        "AgMcpServerDefinition", "AgMcpToolVersion", "AgMcpServerArgument",
        "AgKnowledgeBaseDefinition", "AgKnowledgeDocument", "AgKnowledgeChunk",
        "AgOrchestrationDefinition", "AgOrchestrationVersion", "AgOrchestrationNode",
        "AgOrchestrationEdge", "AgOrchestrationAgentBinding", "AgOrchestrationRun",
        "AgOrchestrationRunDetail", "AgOrchestrationRunNode", "AgOrchestrationNodeAttempt",
        "AgOrchestrationToolCall", "AgEvaluationSuite", "AgEvaluationSuiteVersion",
        "AgEvaluationCase", "AgEvaluationCaseRule", "AgEvaluationBatch", "AgEvaluationBatchCase",
        "AgEvaluationBatchCheck", "AgEvaluationBatchObservation", "AgEvaluationModelJudgement",
        "AgEvaluationModelJudgementCase", "AgEvaluationModelJudgementEvaluator",
        "AgEvaluationModelJudgementMetric", "AgEvaluationModelJudgementMinimumScore",
        "AgEvaluationModelJudgementDiagnostic", "AgMainAgentAssignment", "AgAgentRunAudit",
        "AgAgentToolCallAudit", "AgAgentOperationAudit", "AgApiIdempotency",
        "AgToolApprovalRequest", "AgToolApprovalPayload", "AgToolApprovalDecision",
        "AgToolApprovalExecutionResult", "AgChatConversation", "AgChatMessage",
        "AgUnifiedEntryRun", "AgUnifiedAgentRun", "AgUnifiedOrchestrationLink",
        "AgUnifiedToolCall", "AgUnifiedRunEvent", "AgAgentTask", "AgAgentTaskAttempt",
        "AgAgentTaskEvent"
    ];

    /// <summary>
    /// 将 Agent 实体结构及数据从一个已配置连接同步到另一个连接。
    /// 同步器按固定的父表到子表顺序写入，替换数据时按相反顺序清空目标表。
    /// </summary>
    public static Task<AgentDatabaseSyncResult> SyncAsync(
        MyContext myContext,
        AgentDatabaseSyncRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(myContext);
        return SyncAsync(myContext.Db, request, cancellationToken);
    }

    /// <summary>
    /// 将 Agent 实体结构及数据在同一个 SqlSugar 多连接作用域内同步。
    /// </summary>
    public static async Task<AgentDatabaseSyncResult> SyncAsync(
        SqlSugarScope db,
        AgentDatabaseSyncRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(request);

        string sourceConfigId = NormalizeConfigId(request.SourceConfigId, nameof(request.SourceConfigId));
        string targetConfigId = NormalizeConfigId(request.TargetConfigId, nameof(request.TargetConfigId));
        ValidateRequest(request, sourceConfigId, targetConfigId);

        await SyncLock.WaitAsync(cancellationToken);
        try
        {
            return await SyncLockedAsync(
                db,
                request,
                sourceConfigId,
                targetConfigId,
                cancellationToken);
        }
        finally
        {
            SyncLock.Release();
        }
    }

    private static async Task<AgentDatabaseSyncResult> SyncLockedAsync(
        SqlSugarScope db,
        AgentDatabaseSyncRequest request,
        string sourceConfigId,
        string targetConfigId,
        CancellationToken cancellationToken)
    {
        if (!db.IsAnyConnection(sourceConfigId) ||
            !db.IsAnyConnection(targetConfigId))
        {
            throw new InvalidOperationException("源数据库或目标数据库未配置为已启用的 SqlSugar 连接。");
        }

        SqlSugarScopeProvider source = db.GetConnectionScope(sourceConfigId);
        SqlSugarScopeProvider target = db.GetConnectionScope(targetConfigId);
        IReadOnlyDictionary<string, Type> entityTypes = GetAgentEntityTypes(source);
        IReadOnlyList<(string TableName, Type EntityType)> tables = ResolveAgentTables(
            request.Tables ?? [],
            entityTypes);
        AttachmentSyncPlan attachmentPlan = ResolveAttachmentSyncPlan(tables);
        ValidateSourceTables(source, tables, cancellationToken);
        ValidateAttachmentSource(source, attachmentPlan);

        if (request.SyncStructure)
        {
            SyncTargetStructure(target, tables, attachmentPlan);
        }
        ValidateTargetTables(target, tables);
        ValidateAttachmentTarget(target, attachmentPlan);

        if (!request.ReplaceData)
        {
            var structureResults = tables
                .Select(table => new AgentDatabaseSyncTableResult(
                    table.TableName,
                    CountRows(source, table.TableName),
                    CountRows(target, table.TableName)))
                .ToList();
            AddAttachmentResult(structureResults, source, target, attachmentPlan);
            return new AgentDatabaseSyncResult(
                sourceConfigId,
                targetConfigId,
                request.SyncStructure,
                false,
                0,
                structureResults);
        }

        await target.Ado.BeginTranAsync();
        try
        {
            DeleteTargetAttachments(target, attachmentPlan);
            foreach ((string tableName, _) in tables.Reverse())
            {
                cancellationToken.ThrowIfCancellationRequested();
                target.Ado.ExecuteCommand($"DELETE FROM {QuoteTable(target, tableName)}");
            }

            var results = new List<AgentDatabaseSyncTableResult>(tables.Count);
            long totalRows = 0;
            foreach ((string tableName, _) in tables)
            {
                cancellationToken.ThrowIfCancellationRequested();
                DataTable sourceData = source.Ado.GetDataTable(
                    $"SELECT * FROM {QuoteTable(source, tableName)}");
                CopyBatches(target, tableName, sourceData, request.BatchSize, cancellationToken);

                long sourceRows = sourceData.Rows.Count;
                long targetRows = CountRows(target, tableName);
                if (targetRows != sourceRows)
                {
                    throw new InvalidOperationException(
                        $"Agent 表 {tableName} 同步后行数不一致：源 {sourceRows}，目标 {targetRows}。");
                }
                totalRows = checked(totalRows + sourceRows);
                results.Add(new AgentDatabaseSyncTableResult(tableName, sourceRows, targetRows));
            }
            totalRows = checked(totalRows + CopyAttachments(
                source,
                target,
                attachmentPlan,
                request.BatchSize,
                results,
                cancellationToken));

            await target.Ado.CommitTranAsync();
            return new AgentDatabaseSyncResult(
                sourceConfigId,
                targetConfigId,
                request.SyncStructure,
                true,
                totalRows,
                results);
        }
        catch
        {
            await target.Ado.RollbackTranAsync();
            throw;
        }
    }

    private static void ValidateRequest(
        AgentDatabaseSyncRequest request,
        string sourceConfigId,
        string targetConfigId)
    {
        if (string.Equals(sourceConfigId, targetConfigId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("源数据库与目标数据库不能相同。", nameof(request));
        }
        if (!request.SyncStructure && !request.ReplaceData)
        {
            throw new ArgumentException("至少需要启用结构同步或数据替换。", nameof(request));
        }
        if (request.ReplaceData && !request.ConfirmReplaceData)
        {
            throw new ArgumentException("替换目标数据前必须显式确认 ConfirmReplaceData。", nameof(request));
        }
        if (request.BatchSize is < 1 or > 10000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.BatchSize),
                "BatchSize 必须介于 1 和 10000 之间。");
        }
    }

    private static void ValidateSourceTables(
        SqlSugarScopeProvider source,
        IReadOnlyList<(string TableName, Type EntityType)> tables,
        CancellationToken cancellationToken)
    {
        foreach ((string tableName, Type entityType) in tables)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!source.DbMaintenance.IsAnyTable(tableName))
            {
                throw new InvalidOperationException($"源数据库缺少 Agent 表 {tableName}。");
            }
            if (source.EntityMaintenance.GetEntityInfo(entityType).Columns.Any(column => column.IsIdentity))
            {
                throw new NotSupportedException(
                    $"Agent 表 {tableName} 包含自增列，当前同步器不会隐式改变自增键语义。");
            }
        }
    }

    private static void ValidateTargetTables(
        SqlSugarScopeProvider target,
        IReadOnlyList<(string TableName, Type EntityType)> tables)
    {
        foreach ((string tableName, _) in tables)
        {
            if (!target.DbMaintenance.IsAnyTable(tableName))
            {
                throw new InvalidOperationException(
                    $"目标数据库缺少 Agent 表 {tableName}，请启用 SyncStructure。");
            }
        }
    }

    private static void ValidateAttachmentSource(
        SqlSugarScopeProvider source,
        AttachmentSyncPlan plan)
    {
        if (plan.Enabled && !source.DbMaintenance.IsAnyTable(nameof(FileAttachment)))
        {
            throw new InvalidOperationException(
                $"源数据库缺少 Skill 附件索引表 {nameof(FileAttachment)}。");
        }
    }

    private static void ValidateAttachmentTarget(
        SqlSugarScopeProvider target,
        AttachmentSyncPlan plan)
    {
        if (plan.Enabled && !target.DbMaintenance.IsAnyTable(nameof(FileAttachment)))
        {
            throw new InvalidOperationException(
                $"目标数据库缺少 Skill 附件索引表 {nameof(FileAttachment)}，请启用 SyncStructure。");
        }
    }

    private static void SyncTargetStructure(
        SqlSugarScopeProvider target,
        IReadOnlyList<(string TableName, Type EntityType)> tables,
        AttachmentSyncPlan attachmentPlan)
    {
        ConnectionConfig config = target.CurrentConnectionConfig;
        ConnMoreSettings originalSettings = config.MoreSettings;
        bool forceSqlServerVarchar = config.DbType is SqlSugar.DbType.SqlServer;
        bool originalSqlServerCodeFirstNvarchar =
            originalSettings?.SqlServerCodeFirstNvarchar ?? false;
        if (forceSqlServerVarchar)
        {
            config.MoreSettings ??= new ConnMoreSettings();
            config.MoreSettings.SqlServerCodeFirstNvarchar = false;
        }

        try
        {
            Type[] types = attachmentPlan.Enabled
                ? tables.Select(table => table.EntityType).Append(typeof(FileAttachment)).ToArray()
                : tables.Select(table => table.EntityType).ToArray();
            target.CodeFirst.InitTables(types);
        }
        finally
        {
            if (forceSqlServerVarchar)
            {
                if (originalSettings is null)
                {
                    config.MoreSettings = null;
                }
                else
                {
                    originalSettings.SqlServerCodeFirstNvarchar =
                        originalSqlServerCodeFirstNvarchar;
                    config.MoreSettings = originalSettings;
                }
            }
        }
    }

    private static AttachmentSyncPlan ResolveAttachmentSyncPlan(
        IReadOnlyList<(string TableName, Type EntityType)> tables)
    {
        bool includeDraft = tables.Any(table => string.Equals(
            table.TableName,
            nameof(AgSkillDefinition),
            StringComparison.OrdinalIgnoreCase));
        bool includePublished = tables.Any(table =>
            string.Equals(
                table.TableName,
                nameof(AgSkillVersion),
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                table.TableName,
                nameof(AgSkillVersionFile),
                StringComparison.OrdinalIgnoreCase));
        return new AttachmentSyncPlan(includeDraft, includePublished);
    }

    private static void AddAttachmentResult(
        ICollection<AgentDatabaseSyncTableResult> results,
        SqlSugarScopeProvider source,
        SqlSugarScopeProvider target,
        AttachmentSyncPlan plan)
    {
        if (!plan.Enabled)
        {
            return;
        }

        results.Add(new AgentDatabaseSyncTableResult(
            nameof(FileAttachment),
            CountAttachments(source, plan),
            CountAttachments(target, plan)));
    }

    private static void DeleteTargetAttachments(
        SqlSugarScopeProvider target,
        AttachmentSyncPlan plan)
    {
        if (!plan.Enabled)
        {
            return;
        }

        target.Deleteable<FileAttachment>()
            .Where(BuildAttachmentPredicate(plan))
            .ExecuteCommand();
    }

    private static long CopyAttachments(
        SqlSugarScopeProvider source,
        SqlSugarScopeProvider target,
        AttachmentSyncPlan plan,
        int batchSize,
        ICollection<AgentDatabaseSyncTableResult> results,
        CancellationToken cancellationToken)
    {
        if (!plan.Enabled)
        {
            return 0;
        }

        cancellationToken.ThrowIfCancellationRequested();
        DataTable sourceData = source.Queryable<FileAttachment>()
            .Where(BuildAttachmentPredicate(plan))
            .ToDataTable();
        CopyBatches(
            target,
            nameof(FileAttachment),
            sourceData,
            batchSize,
            cancellationToken);
        long sourceRows = sourceData.Rows.Count;
        long targetRows = CountAttachments(target, plan);
        if (targetRows != sourceRows)
        {
            throw new InvalidOperationException(
                $"Skill 附件索引同步后行数不一致：源 {sourceRows}，目标 {targetRows}。");
        }

        results.Add(new AgentDatabaseSyncTableResult(
            nameof(FileAttachment),
            sourceRows,
            targetRows));
        return sourceRows;
    }

    private static long CountAttachments(
        SqlSugarScopeProvider provider,
        AttachmentSyncPlan plan) => provider.Queryable<FileAttachment>()
        .Where(BuildAttachmentPredicate(plan))
        .Count();

    private static System.Linq.Expressions.Expression<Func<FileAttachment, bool>>
        BuildAttachmentPredicate(AttachmentSyncPlan plan)
    {
        if (plan.IncludeDraft && plan.IncludePublished)
        {
            return value =>
                value.ImageType == DraftSkillAttachmentType ||
                value.ImageType == PublishedSkillAttachmentType;
        }
        if (plan.IncludeDraft)
        {
            return value => value.ImageType == DraftSkillAttachmentType;
        }
        return value => value.ImageType == PublishedSkillAttachmentType;
    }

    private static void CopyBatches(
        SqlSugarScopeProvider target,
        string tableName,
        DataTable sourceData,
        int batchSize,
        CancellationToken cancellationToken)
    {
        for (int offset = 0; offset < sourceData.Rows.Count; offset += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DataTable batch = sourceData.Clone();
            int end = Math.Min(offset + batchSize, sourceData.Rows.Count);
            for (int index = offset; index < end; index++)
            {
                batch.ImportRow(sourceData.Rows[index]);
            }
            target.Fastest<DataTable>().AS(tableName).BulkCopy(batch);
        }
    }

    private static string NormalizeConfigId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("数据库连接配置标识不能为空。", parameterName);
        }
        return value.Trim().ToLowerInvariant();
    }

    private static IReadOnlyDictionary<string, Type> GetAgentEntityTypes(SqlSugarScopeProvider source) =>
        typeof(AgAgentDefinition).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } &&
                           string.Equals(type.Namespace, "EU.Core.Model.Entity", StringComparison.Ordinal) &&
                           type.Name.StartsWith("Ag", StringComparison.Ordinal))
            .ToDictionary(
                type => source.EntityMaintenance.GetEntityInfo(type).DbTableName,
                type => type,
                StringComparer.OrdinalIgnoreCase);

    private static IReadOnlyList<(string TableName, Type EntityType)> ResolveAgentTables(
        IReadOnlyList<string> requestedTables,
        IReadOnlyDictionary<string, Type> entityTypes)
    {
        string[] unregisteredTables = entityTypes.Keys
            .Except(AgentTableOrder, StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (unregisteredTables.Length > 0)
        {
            throw new InvalidOperationException(
                $"以下 Agent 实体表尚未配置同步顺序：{string.Join(", ", unregisteredTables)}。");
        }

        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (requestedTables.Count == 0)
        {
            selected.UnionWith(AgentTableOrder);
        }
        else
        {
            foreach (string name in requestedTables)
            {
                string requestedName = name?.Trim() ?? string.Empty;
                if (!entityTypes.ContainsKey(requestedName))
                {
                    throw new ArgumentException(
                        $"{requestedName} 不是已登记的 Agent 实体表。",
                        nameof(requestedTables));
                }
                if (!selected.Add(requestedName))
                {
                    throw new ArgumentException(
                        $"Agent 表 {requestedName} 被重复指定。",
                        nameof(requestedTables));
                }
            }
        }

        return AgentTableOrder
            .Where(selected.Contains)
            .Select(tableName => (tableName, entityTypes[tableName]))
            .ToArray();
    }

    private static string QuoteTable(SqlSugarScopeProvider provider, string tableName) =>
        provider.CurrentConnectionConfig.DbType switch
        {
            SqlSugar.DbType.MySql => $"`{tableName}`",
            SqlSugar.DbType.Oracle or SqlSugar.DbType.PostgreSQL => $"\"{tableName}\"",
            _ => $"[{tableName}]"
        };

    private static long CountRows(SqlSugarScopeProvider provider, string tableName) =>
        Convert.ToInt64(
            provider.Ado.GetScalar($"SELECT COUNT(*) FROM {QuoteTable(provider, tableName)}"),
            CultureInfo.InvariantCulture);

    private sealed record AttachmentSyncPlan(
        bool IncludeDraft,
        bool IncludePublished)
    {
        public bool Enabled => IncludeDraft || IncludePublished;
    }
}
