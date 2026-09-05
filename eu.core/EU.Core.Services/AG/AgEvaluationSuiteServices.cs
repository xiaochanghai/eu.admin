using EU.Core.IServices.Evaluation;
using EU.Core.IServices.UnifiedEntry;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

#nullable enable

namespace EU.Core.Services;

// 文件职责：AgEvaluationSuiteServices 职责实现

/// <summary>
/// 评测套件、版本、用例和规则的规范化持久化服务。
/// </summary>
public sealed partial class AgEvaluationSuiteServices :
    BaseServices<AgEvaluationSuite>,
    IAgEvaluationSuiteServices
{
    private const string OutputContainsRule = "OutputContains";
    private const string OutputExcludesRule = "OutputExcludes";
    private const string RequiredEventKindRule = "RequiredEventKind";
    private readonly IAgentDefinitionCatalog? agents;
    private readonly TimeProvider timeProvider;

    #region 构造（AgEvaluationSuiteServices）
    /// <summary>
    /// 构造（AgEvaluationSuiteServices）
    /// </summary>
    /// <param name="dal">当前服务使用的数据访问仓储。</param>
    /// <param name="agents">Agent 定义集合。</param>
    /// <param name="timeProvider">用于读取当前时间的时间提供器。</param>
    public AgEvaluationSuiteServices(IBaseRepository<AgEvaluationSuite> dal, IAgentDefinitionCatalog? agents = null, TimeProvider? timeProvider = null)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
        this.agents = agents;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }
    #endregion

    #region 检查评测目标版本是否已发布（IsPublishedAsync）
    /// <summary>
    /// 检查指定 Agent 版本是否已发布且具有可供评测使用的快照（IsPublishedAsync）。
    /// </summary>
    /// <param name="agentId">Agent 定义标识。</param>
    /// <param name="agentVersionId">Agent 版本标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>异步查询结果：指定版本属于该 Agent 的已发布版本且具有非空快照时返回 true，否则返回 false；未配置 Agent 目录时也返回 false。</returns>
    public async Task<bool> IsPublishedAsync(Guid agentId, Guid agentVersionId, CancellationToken cancellationToken = default)
    {
        AgentDefinition? agent = agents is null ? null : await agents.GetDefinitionAsync(agentId, cancellationToken);
        return agent?.PublishedVersions.Any(version => version.Id == agentVersionId && version.Snapshot is not null) == true;
    }
    #endregion

    #region 获取（GetAsync）
    /// <summary>
    /// 获取（GetAsync）
    /// </summary>
    /// <param name="id">评测套件标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户下包含草稿、发布版本及用例的评测套件；不存在时为 null。</returns>
    public async Task<EvaluationSuiteDefinition?> GetAsync(Guid id, string tenantId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(IsolationLevel.RepeatableRead);
        try
        {
            var suite = await Db.Queryable<AgEvaluationSuite>()
                .Where(x =>
                    x.ID == id &&
                    x.TenantId == tenantId)
                .FirstAsync();
            EvaluationSuiteDefinition? result = suite is null
                ? null
                : await LoadSuiteAsync(suite, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await Db.Ado.CommitTranAsync();
            return result;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #region 查询列表（ListPersistedAsync）
    /// <summary>
    /// 查询列表（ListPersistedAsync）
    /// </summary>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户未删除的完整评测套件，按编码及标识升序排列。</returns>
    private async Task<IReadOnlyList<EvaluationSuiteDefinition>> ListPersistedAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            List<AgEvaluationSuite> suites = await Db.Queryable<AgEvaluationSuite>()
                .Where(value => value.TenantId == tenantId && !value.IsDeleted)
                .OrderBy(value => value.Code)
                .OrderBy(value => value.ID)
                .ToListAsync();
            IReadOnlyList<EvaluationSuiteDefinition> result = await LoadSuitesAsync(
                suites,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await Db.Ado.CommitTranAsync();
            return result;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #region 尝试执行（TryCreateAsync）
    /// <summary>
    /// 尝试执行（TryCreateAsync）
    /// </summary>
    /// <param name="value">本次操作使用的评测套件定义。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>异步任务，其结果为：操作是否成功；未满足执行条件或更新未生效时返回 false。</returns>
    public async Task<bool> TryCreateAsync(EvaluationSuiteDefinition value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            bool exists = await Db.Queryable<AgEvaluationSuite>()
                .Where(candidate =>
                    !candidate.IsDeleted &&
                    (candidate.ID == value.Id ||
                     (candidate.TenantId == value.TenantId && candidate.Code == value.Code)))
                .AnyAsync();
            if (exists)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            await Db.Insertable(MapSuiteEntity(value)).ExecuteCommandAsync();
            Guid draftVersionId = Guid.NewGuid();
            await Db.Insertable(MapDraftVersionEntity(value.Id, draftVersionId))
                .ExecuteCommandAsync();
            await InsertCasesAsync(
                value.Id,
                draftVersionId,
                value.Draft.Cases,
                cancellationToken);
            for (int index = 0; index < value.PublishedVersions.Count; index++)
            {
                await InsertPublishedVersionAsync(
                    value.Id,
                    value.PublishedVersions[index],
                    index + 1,
                    cancellationToken);
            }

            await Db.Ado.CommitTranAsync();
            return true;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #region 尝试执行（TryReplaceAsync）
    /// <summary>
    /// 尝试执行（TryReplaceAsync）
    /// </summary>
    /// <param name="value">本次操作使用的评测套件定义。</param>
    /// <param name="expectedLogicalRevision">并发更新要求匹配的逻辑修订号。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>异步任务，其结果为：操作是否成功；未满足执行条件或更新未生效时返回 false。</returns>
    public async Task<bool> TryReplaceAsync(EvaluationSuiteDefinition value, long expectedLogicalRevision, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (expectedLogicalRevision == long.MaxValue ||
            value.LogicalRevision != expectedLogicalRevision + 1)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            List<AgEvaluationSuiteVersion> existingVersions =
                await Db.Queryable<AgEvaluationSuiteVersion>()
                    .Where(candidate => candidate.SuiteId == value.Id && !candidate.IsDeleted)
                    .OrderBy(candidate => candidate.Ordinal)
                    .OrderBy(candidate => candidate.ID)
                    .ToListAsync();
            AgEvaluationSuiteVersion? draft = existingVersions.SingleOrDefault(
                candidate => candidate.IsDraft == true);
            HashSet<Guid> existingPublishedIds = existingVersions
                .Where(candidate => candidate.IsDraft == false)
                .Select(candidate => candidate.ID)
                .ToHashSet();
            HashSet<Guid> requestedPublishedIds = value.PublishedVersions
                .Select(version => version.Id)
                .ToHashSet();
            if (draft is null ||
                !existingPublishedIds.IsSubsetOf(requestedPublishedIds) ||
                value.PublishedVersions.Count != requestedPublishedIds.Count)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            AgEvaluationSuite entity = MapSuiteEntity(value);
            int updated = await Db.Updateable(entity)
                .UpdateColumns(candidate => new
                {
                    candidate.Name,
                    candidate.Description,
                    candidate.Status,
                    candidate.LogicalRevision,
                    candidate.UpdatedAtUtc,
                    candidate.UpdatedByUserId
                })
                .Where(candidate =>
                    candidate.ID == value.Id &&
                    candidate.TenantId == value.TenantId &&
                    candidate.Code == value.Code &&
                    candidate.LogicalRevision == expectedLogicalRevision &&
                    !candidate.IsDeleted)
                .ExecuteCommandAsync();
            if (updated != 1)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            await DeleteVersionCasesAsync(draft.ID);
            await InsertCasesAsync(
                value.Id,
                draft.ID,
                value.Draft.Cases,
                cancellationToken);

            for (int index = 0; index < value.PublishedVersions.Count; index++)
            {
                PublishedEvaluationSuiteVersion version = value.PublishedVersions[index];
                if (!existingPublishedIds.Contains(version.Id))
                {
                    await InsertPublishedVersionAsync(
                        value.Id,
                        version,
                        index + 1,
                        cancellationToken);
                }
            }

            await Db.Ado.CommitTranAsync();
            return true;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #region 加载（LoadSuiteAsync）
    /// <summary>
    /// 加载（LoadSuiteAsync）
    /// </summary>
    /// <param name="suite">评估套件。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>补齐草稿、发布版本、用例及规则的评测套件定义。</returns>
    private async Task<EvaluationSuiteDefinition> LoadSuiteAsync(AgEvaluationSuite suite, CancellationToken cancellationToken)
    {
        IReadOnlyList<EvaluationSuiteDefinition> values = await LoadSuitesAsync(
            [suite],
            cancellationToken);
        return values[0];
    }
    #endregion

    #region 加载（LoadSuitesAsync）
    /// <summary>
    /// 加载（LoadSuitesAsync）
    /// </summary>
    /// <param name="suites">评估套件集合。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>保持输入顺序并补齐各版本用例及规则的评测套件集合。</returns>
    private async Task<IReadOnlyList<EvaluationSuiteDefinition>> LoadSuitesAsync(IReadOnlyList<AgEvaluationSuite> suites, CancellationToken cancellationToken)
    {
        if (suites.Count == 0)
        {
            return [];
        }

        Guid[] suiteIds = suites.Select(value => value.ID).ToArray();
        List<AgEvaluationSuiteVersion> versions = await Db.Queryable<AgEvaluationSuiteVersion>()
            .Where(value =>
                value.SuiteId.HasValue &&
                suiteIds.Contains(value.SuiteId.Value) &&
                !value.IsDeleted)
            .OrderBy(value => value.SuiteId)
            .OrderBy(value => value.Ordinal)
            .OrderBy(value => value.ID)
            .ToListAsync();
        Guid[] versionIds = versions.Select(value => value.ID).ToArray();
        List<AgEvaluationCase> cases = versionIds.Length == 0
            ? []
            : await Db.Queryable<AgEvaluationCase>()
                .Where(value =>
                    value.VersionId.HasValue &&
                    versionIds.Contains(value.VersionId.Value) &&
                    !value.IsDeleted)
                .OrderBy(value => value.VersionId)
                .OrderBy(value => value.Ordinal)
                .OrderBy(value => value.ID)
                .ToListAsync();
        Guid[] caseRowIds = cases.Select(value => value.ID).ToArray();
        List<AgEvaluationCaseRule> rules = caseRowIds.Length == 0
            ? []
            : await Db.Queryable<AgEvaluationCaseRule>()
                .Where(value =>
                    value.EvaluationCaseId.HasValue &&
                    caseRowIds.Contains(value.EvaluationCaseId.Value) &&
                    !value.IsDeleted)
                .OrderBy(value => value.EvaluationCaseId)
                .OrderBy(value => value.RuleType)
                .OrderBy(value => value.Ordinal)
                .OrderBy(value => value.ID)
                .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyDictionary<Guid, AgEvaluationCaseRule[]> rulesByCase = rules
            .GroupBy(value => Required(value.EvaluationCaseId, "Rule.EvaluationCaseId"))
            .ToDictionary(group => group.Key, group => group.ToArray());
        IReadOnlyDictionary<Guid, AgEvaluationCase[]> casesByVersion = cases
            .GroupBy(value => Required(value.VersionId, "Case.VersionId"))
            .ToDictionary(group => group.Key, group => group.ToArray());
        IReadOnlyDictionary<Guid, AgEvaluationSuiteVersion[]> versionsBySuite = versions
            .GroupBy(value => Required(value.SuiteId, "Version.SuiteId"))
            .ToDictionary(group => group.Key, group => group.ToArray());

        return EvaluationSuiteContractCloner.ReadOnly(suites.Select(suite => MapSuite(
            suite,
            versionsBySuite.GetValueOrDefault(suite.ID) ?? [],
            casesByVersion,
            rulesByCase)));
    }
    #endregion

    #region 映射（MapSuite）
    /// <summary>
    /// 映射（MapSuite）
    /// </summary>
    /// <param name="suite">评估套件。</param>
    /// <param name="versions">版本记录集合。</param>
    /// <param name="casesByVersion">按版本分组的评估用例。</param>
    /// <param name="rulesByCase">按用例分组的评估规则。</param>
    /// <returns>包含唯一草稿和有序发布版本的评测套件；没有唯一草稿时抛出异常。</returns>
    private static EvaluationSuiteDefinition MapSuite(
        AgEvaluationSuite suite,
        IReadOnlyList<AgEvaluationSuiteVersion> versions,
        IReadOnlyDictionary<Guid, AgEvaluationCase[]> casesByVersion,
        IReadOnlyDictionary<Guid, AgEvaluationCaseRule[]> rulesByCase)
    {
        AgEvaluationSuiteVersion draft = versions.SingleOrDefault(value => value.IsDraft == true)
            ?? throw new InvalidDataException(
                $"Evaluation suite '{suite.Code}' does not have exactly one draft version.");
        #region 映射（MapCases）
        IReadOnlyList<EvaluationCaseDefinition> MapCases(Guid versionId) =>
            Array.AsReadOnly((casesByVersion.GetValueOrDefault(versionId) ?? [])
                .OrderBy(value => Required(value.Ordinal, "Case.Ordinal"))
                .ThenBy(value => value.ID)
                .Select(value => MapCase(value, rulesByCase.GetValueOrDefault(value.ID) ?? []))
                .ToArray());
        #endregion

        return new EvaluationSuiteDefinition(
            suite.ID,
            Required(suite.TenantId, "TenantId"),
            Required(suite.Code, "Code"),
            Required(suite.Name, "Name"),
            Required(suite.Description, "Description"),
            Required(suite.LogicalRevision, "LogicalRevision"),
            ToOffset(Required(suite.CreatedAtUtc, "CreatedAtUtc")),
            ToOffset(Required(suite.UpdatedAtUtc, "UpdatedAtUtc")),
            Required(suite.CreatedByUserId, "CreatedByUserId"),
            Required(suite.UpdatedByUserId, "UpdatedByUserId"),
            new EvaluationSuiteDraft(MapCases(draft.ID)),
            Array.AsReadOnly(versions
                .Where(value => value.IsDraft == false)
                .OrderBy(value => Required(value.Ordinal, "Version.Ordinal"))
                .ThenBy(value => value.ID)
                .Select(value => new PublishedEvaluationSuiteVersion(
                    value.ID,
                    Required(value.Label, "Version.Label"),
                    Required(value.ContentSha256, "Version.ContentSha256"),
                    ToOffset(Required(value.PublishedAtUtc, "Version.PublishedAtUtc")),
                    Required(value.PublishedByUserId, "Version.PublishedByUserId"),
                    MapCases(value.ID)))
                .ToArray()))
        {
            Status = ParseSuiteStatus(suite.Status)
        };
    }
    #endregion

    #region 映射（MapCase）
    /// <summary>
    /// 映射（MapCase）
    /// </summary>
    /// <param name="value">本次操作使用的评测用例实体。</param>
    /// <param name="rules">评估规则集合。</param>
    /// <returns>包含目标 Agent 版本及规则评测要求的用例定义。</returns>
    private static EvaluationCaseDefinition MapCase(AgEvaluationCase value, IReadOnlyList<AgEvaluationCaseRule> rules)
    {
        #region 处理（RuleValues）
        string[] RuleValues(string type) => rules
            .Where(rule => string.Equals(rule.RuleType, type, StringComparison.Ordinal))
            .OrderBy(rule => Required(rule.Ordinal, "Rule.Ordinal"))
            .ThenBy(rule => rule.ID)
            .Select(rule => Required(rule.Value, "Rule.Value"))
            .ToArray();
        #endregion
        return new EvaluationCaseDefinition(
            Required(value.CaseId, "Case.CaseId"),
            Required(value.Name, "Case.Name"),
            Required(value.Input, "Case.Input"),
            Required(value.TargetAgentId, "Case.TargetAgentId"),
            Required(value.TargetAgentVersionId, "Case.TargetAgentVersionId"),
            new RunEvaluationSpecification(
                ParseExpectedStatus(value.ExpectedStatus),
                RuleValues(OutputContainsRule),
                RuleValues(OutputExcludesRule),
                RuleValues(RequiredEventKindRule),
                value.MaximumToolCalls,
                value.MaximumDurationMilliseconds));
    }
    #endregion

    #region 新增（InsertPublishedVersionAsync）
    /// <summary>
    /// 新增（InsertPublishedVersionAsync）
    /// </summary>
    /// <param name="suiteId">评估套件标识。</param>
    /// <param name="version">版本记录。</param>
    /// <param name="ordinal">版本在所属定义中的排序序号。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task InsertPublishedVersionAsync(Guid suiteId, PublishedEvaluationSuiteVersion version, int ordinal, CancellationToken cancellationToken)
    {
        await Db.Insertable(new AgEvaluationSuiteVersion
        {
            ID = version.Id,
            SuiteId = suiteId,
            Ordinal = ordinal,
            Label = version.Label,
            IsDraft = false,
            ContentSha256 = version.ContentSha256,
            PublishedAtUtc = version.PublishedAtUtc.UtcDateTime,
            PublishedByUserId = version.PublishedBy,
            IsDeleted = false,
            IsActive = true
        }).ExecuteCommandAsync();
        await InsertCasesAsync(suiteId, version.Id, version.Cases, cancellationToken);
    }
    #endregion

    #region 新增（InsertCasesAsync）
    /// <summary>
    /// 新增（InsertCasesAsync）
    /// </summary>
    /// <param name="suiteId">评估套件标识。</param>
    /// <param name="versionId">版本标识。</param>
    /// <param name="cases">评估用例集合。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task InsertCasesAsync(Guid suiteId, Guid versionId, IReadOnlyList<EvaluationCaseDefinition> cases, CancellationToken cancellationToken)
    {
        for (int ordinal = 0; ordinal < cases.Count; ordinal++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EvaluationCaseDefinition value = cases[ordinal];
            Guid rowId = Guid.NewGuid();
            await Db.Insertable(new AgEvaluationCase
            {
                ID = rowId,
                SuiteId = suiteId,
                VersionId = versionId,
                Ordinal = ordinal,
                CaseId = value.Id,
                Name = value.Name,
                Input = value.Input,
                TargetAgentId = value.TargetAgentId,
                TargetAgentVersionId = value.TargetAgentVersionId,
                ExpectedStatus = value.Specification.ExpectedStatus?.ToString(),
                MaximumToolCalls = value.Specification.MaximumToolCalls,
                MaximumDurationMilliseconds = value.Specification.MaximumDurationMilliseconds,
                IsDeleted = false,
                IsActive = true
            }).ExecuteCommandAsync();
            await InsertRulesAsync(
                suiteId,
                versionId,
                rowId,
                OutputContainsRule,
                value.Specification.OutputContains);
            await InsertRulesAsync(
                suiteId,
                versionId,
                rowId,
                OutputExcludesRule,
                value.Specification.OutputExcludes);
            await InsertRulesAsync(
                suiteId,
                versionId,
                rowId,
                RequiredEventKindRule,
                value.Specification.RequiredEventKinds);
        }
    }
    #endregion

    #region 新增（InsertRulesAsync）
    /// <summary>
    /// 新增（InsertRulesAsync）
    /// </summary>
    /// <param name="suiteId">评估套件标识。</param>
    /// <param name="versionId">版本标识。</param>
    /// <param name="caseRowId">评估用例行标识。</param>
    /// <param name="ruleType">评估规则类型。</param>
    /// <param name="values">指定规则类型下需要按序持久化的规则值。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task InsertRulesAsync(Guid suiteId, Guid versionId, Guid caseRowId, string ruleType, IReadOnlyList<string> values)
    {
        if (values.Count == 0)
        {
            return;
        }

        List<AgEvaluationCaseRule> rows = values.Select((value, ordinal) =>
            new AgEvaluationCaseRule
            {
                ID = Guid.NewGuid(),
                SuiteId = suiteId,
                VersionId = versionId,
                EvaluationCaseId = caseRowId,
                RuleType = ruleType,
                Ordinal = ordinal,
                Value = value,
                IsDeleted = false,
                IsActive = true
            }).ToList();
        await Db.Insertable(rows).ExecuteCommandAsync();
    }
    #endregion

    #region 删除（DeleteVersionCasesAsync）
    /// <summary>
    /// 删除（DeleteVersionCasesAsync）
    /// </summary>
    /// <param name="versionId">版本标识。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task DeleteVersionCasesAsync(Guid versionId)
    {
        Guid[] caseIds = await Db.Queryable<AgEvaluationCase>()
            .Where(value => value.VersionId == versionId)
            .Select(value => value.ID)
            .ToArrayAsync();
        if (caseIds.Length > 0)
        {
            await Db.Deleteable<AgEvaluationCaseRule>()
                .Where(value =>
                    value.EvaluationCaseId.HasValue &&
                    caseIds.Contains(value.EvaluationCaseId.Value))
                .ExecuteCommandAsync();
        }
        await Db.Deleteable<AgEvaluationCase>()
            .Where(value => value.VersionId == versionId)
            .ExecuteCommandAsync();
    }
    #endregion

    #region 映射（MapSuiteEntity）
    /// <summary>
    /// 映射（MapSuiteEntity）
    /// </summary>
    /// <param name="value">本次操作使用的评测套件定义。</param>
    /// <returns>由评测套件定义构造的套件主表实体。</returns>
    private static AgEvaluationSuite MapSuiteEntity(EvaluationSuiteDefinition value) =>
        new()
        {
            ID = value.Id,
            TenantId = value.TenantId,
            Code = value.Code,
            Name = value.Name,
            Description = value.Description,
            Status = value.Status.ToString(),
            LogicalRevision = value.LogicalRevision,
            CreatedAtUtc = value.CreatedAtUtc.UtcDateTime,
            UpdatedAtUtc = value.UpdatedAtUtc.UtcDateTime,
            CreatedByUserId = value.CreatedBy,
            UpdatedByUserId = value.UpdatedBy,
            IsDeleted = false,
            IsActive = true
        };
    #endregion

    #region 映射（MapDraftVersionEntity）
    /// <summary>
    /// 映射（MapDraftVersionEntity）
    /// </summary>
    /// <param name="suiteId">评估套件标识。</param>
    /// <param name="draftVersionId">草稿版本标识。</param>
    /// <returns>序号为零、尚未发布的评测套件草稿版本实体。</returns>
    private static AgEvaluationSuiteVersion MapDraftVersionEntity(Guid suiteId, Guid draftVersionId) =>
        new()
        {
            ID = draftVersionId,
            SuiteId = suiteId,
            Ordinal = 0,
            Label = "draft",
            IsDraft = true,
            ContentSha256 = string.Empty,
            PublishedAtUtc = null,
            PublishedByUserId = string.Empty,
            IsDeleted = false,
            IsActive = true
        };
    #endregion

    #region 解析（ParseSuiteStatus）
    /// <summary>
    /// 解析并校验持久化枚举值（ParseSuiteStatus）。
    /// </summary>
    /// <param name="value">数据库中存储的枚举文本。</param>
    /// <returns>按区分大小写方式解析且已定义的枚举值；无效输入抛出异常。</returns>
    private static EvaluationSuiteStatus ParseSuiteStatus(string? value) =>
        Enum.TryParse(value, ignoreCase: false, out EvaluationSuiteStatus result) &&
        Enum.IsDefined(result)
            ? result
            : throw new InvalidDataException(
                $"Evaluation suite Status contains unsupported value '{value}'.");
    #endregion

    #region 解析（ParseExpectedStatus）
    /// <summary>
    /// 解析并校验持久化枚举值（ParseExpectedStatus）。
    /// </summary>
    /// <param name="value">数据库中存储的枚举文本。</param>
    /// <returns>按区分大小写方式解析且已定义的枚举值；空字符串或 null 返回 null，无效输入抛出异常。</returns>
    private static UnifiedRunStatus? ParseExpectedStatus(string? value) =>
        string.IsNullOrEmpty(value)
            ? null
            : Enum.TryParse(value, ignoreCase: false, out UnifiedRunStatus result) &&
              Enum.IsDefined(result)
                ? result
                : throw new InvalidDataException(
                    $"Evaluation case ExpectedStatus contains unsupported value '{value}'.");
    #endregion

    #region 转换（ToOffset）
    /// <summary>
    /// 将数据库时间还原为 UTC 时间（ToOffset）。
    /// </summary>
    /// <param name="value">按 UTC 语义存储的数据库时间。</param>
    /// <returns>将输入时间视为 UTC 后构造的零偏移时间。</returns>
    private static DateTimeOffset ToOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <typeparam name="T">必填字段的值类型。</typeparam>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="field">字段名称，用于校验和错误提示。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static T Required<T>(T? value, string field) where T : struct =>
        value ?? throw new InvalidDataException($"Evaluation suite field '{field}' is missing.");
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="field">字段名称，用于校验和错误提示。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static string Required(string? value, string field) =>
        value ?? throw new InvalidDataException($"Evaluation suite field '{field}' is missing.");
    #endregion

    private static readonly JsonSerializerOptions HashJsonOptions = new(JsonSerializerDefaults.Web);

    #region 创建（CreateAsync）
    /// <summary>
    /// 创建（CreateAsync）
    /// </summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含评测套件定义，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<EvaluationSuiteDefinition>> CreateAsync(CreateEvaluationSuiteCommand command, CancellationToken cancellationToken = default)
    {
        if (!ValidIdentity(command.TenantId, command.ActorUserId))
        {
            return Invalid("Trusted tenant and actor are required.");
        }

        string code = (command.Code ?? string.Empty).Trim().ToLowerInvariant();
        if (!Regex.IsMatch(code, "^[a-z0-9]+(?:-[a-z0-9]+)*$") || code.Length > 80)
        {
            return Failure(EvaluationSuiteErrorCodes.CodeInvalid, "Code must be lowercase kebab-case.");
        }

        if (!ValidMetadata(command.Name, command.Description))
        {
            return Invalid("Suite name or description exceeds its limit.");
        }

        DateTimeOffset now = timeProvider.GetUtcNow().ToUniversalTime();
        var value = new EvaluationSuiteDefinition(
            Guid.NewGuid(), command.TenantId, code, command.Name.Trim(),
            command.Description?.Trim() ?? string.Empty, 0, now, now,
            command.ActorUserId, command.ActorUserId, new EvaluationSuiteDraft([]), []);
        return await TryCreateAsync(value, cancellationToken)
            ? Success(value)
            : Failure(EvaluationSuiteErrorCodes.CodeConflict, "An evaluation suite already uses this code.");
    }
    #endregion

    #region 查询列表（ListAsync）
    /// <summary>
    /// 查询列表（ListAsync）
    /// </summary>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户下匹配状态的评测套件；未指定状态时排除已归档套件。</returns>
    public async Task<IReadOnlyList<EvaluationSuiteDefinition>> ListAsync(
        string tenantId,
        EvaluationSuiteStatus? status = null,
        CancellationToken cancellationToken = default) =>
        EvaluationSuiteContractCloner.ReadOnly((await ListPersistedAsync(tenantId, cancellationToken))
            .Where(value => status.HasValue
                ? value.Status == status.Value
                : value.Status is not EvaluationSuiteStatus.Archived));
    #endregion

    #region 保存（SaveDraftAsync）
    /// <summary>
    /// 保存（SaveDraftAsync）
    /// </summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含评测套件定义，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<EvaluationSuiteDefinition>> SaveDraftAsync(
        SaveEvaluationSuiteDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        EvaluationSuiteDefinition? existing = await GetAsync(command.Id, command.TenantId, cancellationToken);
        if (existing is null) return Failure(EvaluationSuiteErrorCodes.NotFound, "The evaluation suite was not found.");
        if (existing.LogicalRevision != command.ExpectedLogicalRevision) return Conflict();
        if (existing.Status is EvaluationSuiteStatus.Archived)
        {
            return Failure(EvaluationSuiteErrorCodes.LifecycleTransitionInvalid,
                "An archived evaluation suite must be restored before its Draft can be edited.");
        }
        if (!ValidIdentity(command.TenantId, command.ActorUserId)
            || !ValidMetadata(command.Name, command.Description)
            || !TryValidateCases(command.Cases, allowEmpty: true))
        {
            return Invalid("The evaluation suite Draft is invalid.");
        }

        EvaluationSuiteDefinition updated = existing with
        {
            Name = command.Name.Trim(),
            Description = command.Description?.Trim() ?? string.Empty,
            LogicalRevision = existing.LogicalRevision + 1,
            UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime(),
            UpdatedBy = command.ActorUserId,
            Draft = new EvaluationSuiteDraft(CloneCases(command.Cases))
        };
        return await TryReplaceAsync(updated, command.ExpectedLogicalRevision, cancellationToken)
            ? Success(updated) : Conflict();
    }
    #endregion

    #region 发布（PublishAsync）
    /// <summary>
    /// 发布（PublishAsync）
    /// </summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含评测套件定义，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<EvaluationSuiteDefinition>> PublishAsync(
        PublishEvaluationSuiteCommand command,
        CancellationToken cancellationToken = default)
    {
        EvaluationSuiteDefinition? existing = await GetAsync(command.Id, command.TenantId, cancellationToken);
        if (existing is null) return Failure(EvaluationSuiteErrorCodes.NotFound, "The evaluation suite was not found.");
        if (existing.LogicalRevision != command.ExpectedLogicalRevision) return Conflict();
        if (existing.Status is EvaluationSuiteStatus.Archived)
        {
            return Failure(EvaluationSuiteErrorCodes.LifecycleTransitionInvalid,
                "An archived evaluation suite must be restored before it can be published.");
        }
        if (!ValidIdentity(command.TenantId, command.ActorUserId)
            || !TryValidateCases(existing.Draft.Cases, allowEmpty: false))
        {
            return Invalid("A valid non-empty Draft is required before publication.");
        }

        foreach ((Guid agentId, Guid versionId) in existing.Draft.Cases
            .Select(value => (value.TargetAgentId, value.TargetAgentVersionId)).Distinct())
        {
            if (!await IsPublishedAsync(agentId, versionId, cancellationToken))
            {
                return Failure(EvaluationSuiteErrorCodes.TargetUnavailable,
                    "Every evaluation target must reference an existing published Agent version.");
            }
        }

        DateTimeOffset now = timeProvider.GetUtcNow().ToUniversalTime();
        IReadOnlyList<EvaluationCaseDefinition> cases = CloneCases(existing.Draft.Cases);
        var version = new PublishedEvaluationSuiteVersion(Guid.NewGuid(),
            $"{existing.PublishedVersions.Count + 1}.0.0", ComputeContentHash(cases),
            now, command.ActorUserId, cases);
        EvaluationSuiteDefinition updated = existing with
        {
            LogicalRevision = existing.LogicalRevision + 1,
            UpdatedAtUtc = now,
            UpdatedBy = command.ActorUserId,
            PublishedVersions = new ReadOnlyCollection<PublishedEvaluationSuiteVersion>(
                existing.PublishedVersions.Append(version).ToArray())
        };
        return await TryReplaceAsync(updated, command.ExpectedLogicalRevision, cancellationToken)
            ? Success(updated) : Conflict();
    }
    #endregion

    #region 设置（SetArchivedAsync）
    /// <summary>
    /// 设置（SetArchivedAsync）
    /// </summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含评测套件定义，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<EvaluationSuiteDefinition>> SetArchivedAsync(
        SetEvaluationSuiteArchiveCommand command,
        CancellationToken cancellationToken = default)
    {
        EvaluationSuiteDefinition? existing = await GetAsync(command.Id, command.TenantId, cancellationToken);
        if (existing is null) return Failure(EvaluationSuiteErrorCodes.NotFound, "The evaluation suite was not found.");
        if (existing.LogicalRevision != command.ExpectedLogicalRevision) return Conflict();
        if (!ValidIdentity(command.TenantId, command.ActorUserId)) return Invalid("Trusted tenant and actor are required.");

        EvaluationSuiteStatus target = command.Archived ? EvaluationSuiteStatus.Archived : EvaluationSuiteStatus.Active;
        if (existing.Status == target)
        {
            return Failure(EvaluationSuiteErrorCodes.LifecycleTransitionInvalid,
                command.Archived ? "The evaluation suite is already archived."
                    : "Only an archived evaluation suite can be restored.");
        }

        EvaluationSuiteDefinition updated = existing with
        {
            Status = target,
            LogicalRevision = existing.LogicalRevision + 1,
            UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime(),
            UpdatedBy = command.ActorUserId
        };
        return await TryReplaceAsync(updated, command.ExpectedLogicalRevision, cancellationToken)
            ? Success(updated) : Conflict();
    }
    #endregion

    #region 尝试执行（TryValidateCases）
    /// <summary>
    /// 尝试执行（TryValidateCases）
    /// </summary>
    /// <param name="cases">评估用例集合。</param>
    /// <param name="allowEmpty">是否允许空值。</param>
    /// <returns>操作是否成功；未满足执行条件或更新未生效时返回 false。</returns>
    private static bool TryValidateCases(IReadOnlyList<EvaluationCaseDefinition>? cases, bool allowEmpty)
    {
        if (cases is null || cases.Count > 100 || (!allowEmpty && cases.Count == 0)
            || cases.Select(value => value.Id).Distinct().Count() != cases.Count) return false;
        foreach (EvaluationCaseDefinition value in cases)
        {
            if (value.Id == Guid.Empty || value.TargetAgentId == Guid.Empty
                || value.TargetAgentVersionId == Guid.Empty || string.IsNullOrWhiteSpace(value.Name)
                || value.Name.Trim().Length > 120 || string.IsNullOrWhiteSpace(value.Input)
                || Encoding.UTF8.GetByteCount(value.Input) > 32_768 || value.Specification is null) return false;
            try { RunEvaluationSpecificationValidator.Validate(value.Specification); }
            catch (RunEvaluationException) { return false; }
        }
        return true;
    }
    #endregion

    #region 复制（CloneCases）
    /// <summary>
    /// 复制（CloneCases）
    /// </summary>
    /// <param name="cases">评估用例集合。</param>
    /// <returns>复制规则集合并去除名称、输入首尾空白后的只读用例集合。</returns>
    private static IReadOnlyList<EvaluationCaseDefinition> CloneCases(IEnumerable<EvaluationCaseDefinition> cases) =>
        new ReadOnlyCollection<EvaluationCaseDefinition>(cases.Select(value => value with
        {
            Name = value.Name.Trim(),
            Input = value.Input.Trim(),
            Specification = value.Specification with
            {
                OutputContains = value.Specification.OutputContains.ToArray(),
                OutputExcludes = value.Specification.OutputExcludes.ToArray(),
                RequiredEventKinds = value.Specification.RequiredEventKinds.ToArray()
            }
        }).ToArray());
    #endregion

    #region 计算（ComputeContentHash）
    /// <summary>
    /// 计算（ComputeContentHash）
    /// </summary>
    /// <param name="cases">评估用例集合。</param>
    /// <returns>用例集合按指定 JSON 选项序列化后的 SHA-256 小写十六进制摘要。</returns>
    private static string ComputeContentHash(IReadOnlyList<EvaluationCaseDefinition> cases) =>
        Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(cases, HashJsonOptions)));
    #endregion

    #region 校验评测操作的租户和用户标识（ValidIdentity）
    /// <summary>
    /// 校验评测操作的租户和用户标识（ValidIdentity）。
    /// </summary>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="actorUserId">执行当前操作的用户标识。</param>
    /// <returns>租户及用户均非空白，且原始长度分别不超过 128 和 256 字符时返回 true，否则返回 false。</returns>
    private static bool ValidIdentity(string tenantId, string actorUserId) =>
        !string.IsNullOrWhiteSpace(tenantId) && tenantId.Length <= 128
        && !string.IsNullOrWhiteSpace(actorUserId) && actorUserId.Length <= 256;
    #endregion

    #region 校验评测套件名称和描述（ValidMetadata）
    /// <summary>
    /// 校验评测套件名称和描述（ValidMetadata）。
    /// </summary>
    /// <param name="name">待校验的评测套件名称。</param>
    /// <param name="description">可选的评测套件描述。</param>
    /// <returns>名称非空白且修剪后不超过 120 字符，描述为空或修剪后不超过 1000 字符时返回 true，否则返回 false。</returns>
    private static bool ValidMetadata(string? name, string? description) =>
        !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= 120
        && (description?.Trim().Length ?? 0) <= 1000;
    #endregion

    #region 处理（Invalid）
    /// <summary>
    /// 处理（Invalid）
    /// </summary>
    /// <param name="message">消息或提示文本。</param>
    /// <returns>表示评测套件定义无效并携带指定提示的失败服务结果。</returns>
    private static ServiceResult<EvaluationSuiteDefinition> Invalid(string message) =>
        Failure(EvaluationSuiteErrorCodes.DefinitionInvalid, message);
    #endregion

    #region 处理（Conflict）
    /// <summary>
    /// 处理（Conflict）
    /// </summary>
    /// <returns>表示记录版本已变化、需要重新加载后重试的失败服务结果。</returns>
    private static ServiceResult<EvaluationSuiteDefinition> Conflict() =>
        Failure(EvaluationSuiteErrorCodes.RowVersionConflict, "The evaluation suite changed; reload and retry.");
    #endregion

    #region 处理（Failure）
    /// <summary>
    /// 处理（Failure）
    /// </summary>
    /// <param name="code">对象编码或业务错误码。</param>
    /// <param name="message">消息或提示文本。</param>
    /// <returns>包含对应业务错误状态和提示信息的失败服务结果。</returns>
    private static ServiceResult<EvaluationSuiteDefinition> Failure(string code, string message) =>
        ServiceResult<EvaluationSuiteDefinition>.Failure(
            EvaluationSuiteServiceStatusCodes.FromErrorCode(code), message);
    #endregion
}
