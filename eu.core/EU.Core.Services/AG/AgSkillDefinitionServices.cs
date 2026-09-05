using EU.Core.IServices.Skills;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

#nullable enable

namespace EU.Core.Services;

// 文件职责：AgSkillDefinitionServices 职责实现

/// <summary>
/// Skill 定义、发布版本和文件清单服务。
/// </summary>
public sealed class AgSkillDefinitionServices :
    BaseServices<AgSkillDefinition>,
    IAgSkillDefinitionServices,
    IPublishedSkillVersionCatalog
{
    private const string DraftAttachmentType = "agent-skill-draft";
    private const string PublishedAttachmentType = "agent-skill-version";
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> Locks = new();
    private readonly ISkillFileStore _fileStore;
    private readonly IPublishedSkillContentStore? _publishedContentStore;

    #region 构造（AgSkillDefinitionServices）
    /// <summary>
    /// 构造（AgSkillDefinitionServices）
    /// </summary>
    /// <param name="dal">当前服务使用的数据访问仓储。</param>
    /// <param name="fileStore">技能文件存储服务。</param>
    /// <param name="publishedContentStore">已发布技能内容存储服务。</param>
    public AgSkillDefinitionServices(
        IBaseRepository<AgSkillDefinition> dal,
        ISkillFileStore fileStore,
        IPublishedSkillContentStore? publishedContentStore = null)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
        _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
        _publishedContentStore = publishedContentStore;
    }
    #endregion

    private static readonly Regex CodePattern = new(
        "^[a-z0-9][a-z0-9-]{0,62}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex VersionPattern = new(
        "^(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    #region 创建（CreateAsync）
    /// <summary>
    /// 创建（CreateAsync）
    /// </summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能定义，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<SkillDefinition>> CreateAsync(CreateSkillCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        string code = command.Code?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!CodePattern.IsMatch(code) ||
            !string.Equals(code, command.Code?.Trim(), StringComparison.Ordinal))
        {
            return Failure(SkillErrorCodes.CodeInvalid, "Skill code must be lowercase kebab-case.");
        }

        var definition = new SkillDefinition(
            Guid.NewGuid(),
            code,
            command.Name?.Trim() ?? string.Empty,
            command.Description ?? string.Empty,
            command.Category?.Trim() ?? string.Empty,
            0,
            SkillContractCloner.ReadOnly(Array.Empty<SkillVersion>()));

        cancellationToken.ThrowIfCancellationRequested();
        bool draftCreated = false;
        await Db.Ado.BeginTranAsync(IsolationLevel.Serializable);
        try
        {
            bool exists = await AnyAsync(value => value.ID == definition.Id || value.Code == definition.Code);
            if (exists)
            {
                await Db.Ado.RollbackTranAsync();
                return Failure(SkillErrorCodes.CodeConflict, "A Skill already uses this code.");
            }

            await Db.Insertable(MapDefinitionEntity(definition)).ExecuteCommandAsync();
            draftCreated = await _fileStore.EnsureDraftAsync(
                code,
                definition.Name,
                definition.Description,
                cancellationToken);
            var draftFiles = await _fileStore.ListDraftAsync(code, cancellationToken);
            await ReconcileAttachmentGroupAsync(
                definition.Id,
                DraftAttachmentType,
                MapDraftAttachments(definition.Id, code, draftFiles));
            cancellationToken.ThrowIfCancellationRequested();
            await Db.Ado.CommitTranAsync();
        }
        catch (Exception exception)
        {
            await Db.Ado.RollbackTranAsync();
            if (!draftCreated)
            {
                throw;
            }

            try
            {
                await _fileStore.RollbackDraftCreationAsync(code, CancellationToken.None);
            }
            catch (Exception compensationException)
            {
                throw new InvalidOperationException(
                    "Skill creation failed and its Draft scaffold could not be rolled back.",
                    new AggregateException(exception, compensationException));
            }

            throw;
        }
        return Success(definition);
    }
    #endregion

    #region 获取（GetAsync）
    /// <summary>
    /// 获取（GetAsync）
    /// </summary>
    /// <param name="id">技能标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>包含已发布版本及文件清单的技能定义；不存在时为 null。</returns>
    public async Task<SkillDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            AgSkillDefinition? definition = await Db.Queryable<AgSkillDefinition>()
                .Where(value => value.ID == id && !value.IsDeleted)
                .FirstAsync();
            SkillDefinition? result = definition is null
                ? null
                : await LoadDefinitionAsync(definition, cancellationToken);
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

    #region 查询列表（ListAsync）
    /// <summary>
    /// 查询列表（ListAsync）
    /// </summary>
    /// <param name="query">查询筛选条件。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>匹配搜索、分类和状态条件的技能摘要集合；未指定状态时排除已归档技能。</returns>
    public async Task<IReadOnlyList<SkillListItem>> ListAsync(SkillQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        string? search = query.Search?.Trim().ToLowerInvariant();
        string? category = query.Category?.Trim().ToLowerInvariant();
        string? requestedStatus = query.Status?.ToString();

        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            List<AgSkillDefinition> definitions = await Db.Queryable<AgSkillDefinition>()
                .Where(value => !value.IsDeleted)
                .WhereIF(
                    requestedStatus is not null,
                    value => value.Status == requestedStatus)
                .WhereIF(
                    requestedStatus is null,
                    value => value.Status != nameof(SkillStatus.Archived))
                .WhereIF(
                    !string.IsNullOrWhiteSpace(search),
                    value =>
                        SqlFunc.ToLower(value.Code).Contains(search!) ||
                        SqlFunc.ToLower(value.Name).Contains(search!) ||
                        SqlFunc.ToLower(value.Description).Contains(search!))
                .WhereIF(
                    !string.IsNullOrWhiteSpace(category),
                    value => SqlFunc.ToLower(value.Category) == category)
                .OrderBy(value => value.Code)
                .OrderBy(value => value.ID)
                .ToListAsync();

            IReadOnlyDictionary<Guid, AgSkillVersion[]> versionsBySkill =
                await LoadVersionsBySkillAsync(definitions.Select(value => value.ID), cancellationToken);
            var result = definitions.Select(definition =>
            {
                AgSkillVersion? current = versionsBySkill
                    .GetValueOrDefault(definition.ID)?
                    .OrderBy(value => value.Ordinal)
                    .LastOrDefault();
                return new SkillListItem(
                    definition.ID,
                    Required(definition.Code, "Code"),
                    Required(definition.Name, "Name"),
                    Required(definition.Description, "Description"),
                    Required(definition.Category, "Category"),
                    Required(definition.DraftRevision, "DraftRevision"),
                    current?.Label,
                    current?.ManifestSha256)
                {
                    Status = ParseStatus(definition.Status)
                };
            }).ToArray();

            cancellationToken.ThrowIfCancellationRequested();
            await Db.Ado.CommitTranAsync();
            return SkillContractCloner.ReadOnly(result);
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #region 更新（UpdateAsync）
    /// <summary>
    /// 更新（UpdateAsync）
    /// </summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能定义，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<SkillDefinition>> UpdateAsync(UpdateSkillCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return await WithLockAsync(command.SkillId, async () =>
        {
            SkillDefinition? existing = await GetAsync(command.SkillId, cancellationToken);
            if (existing is null)
            {
                return Failure(SkillErrorCodes.NotFound, "The Skill was not found.");
            }

            if (existing.DraftRevision != command.ExpectedDraftRevision)
            {
                return RevisionConflict();
            }

            if (existing.Status is SkillStatus.Archived)
            {
                return Failure(
                    SkillErrorCodes.LifecycleTransitionInvalid,
                    "An archived Skill must be restored before it can be edited.");
            }

            SkillDefinition updated = existing with
            {
                Name = command.Name?.Trim() ?? string.Empty,
                Description = command.Description ?? string.Empty,
                Category = command.Category?.Trim() ?? string.Empty,
                DraftRevision = existing.DraftRevision + 1
            };
            return await TryUpdateDefinitionAsync(updated, existing.DraftRevision, cancellationToken)
                ? Success(updated)
                : RevisionConflict();
        }, cancellationToken);
    }
    #endregion

    #region 保存（SaveFileAsync）
    /// <summary>
    /// 保存（SaveFileAsync）
    /// </summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能定义，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<SkillDefinition>> SaveFileAsync(SaveSkillFileCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return await WithLockAsync(command.SkillId, async () =>
        {
            SkillDefinition? existing = await GetAsync(command.SkillId, cancellationToken);
            if (existing is null)
            {
                return Failure(SkillErrorCodes.NotFound, "The Skill was not found.");
            }

            if (existing.DraftRevision != command.ExpectedDraftRevision)
            {
                return RevisionConflict();
            }

            if (existing.Status is SkillStatus.Archived)
            {
                return Failure(
                    SkillErrorCodes.LifecycleTransitionInvalid,
                    "An archived Skill must be restored before its Draft files can be edited.");
            }

            SkillDefinition updated = existing with { DraftRevision = existing.DraftRevision + 1 };
            return await ExecuteDraftMutationAsync(
                existing,
                updated,
                command.RelativePath,
                async token => await _fileStore.WriteDraftTextAsync(
                    existing.Code,
                    command.RelativePath,
                    command.Content,
                    token),
                requireExistingFile: false,
                cancellationToken);
        }, cancellationToken);
    }
    #endregion

    #region 删除（DeleteFileAsync）
    /// <summary>
    /// 删除（DeleteFileAsync）
    /// </summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能定义，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<SkillDefinition>> DeleteFileAsync(DeleteSkillFileCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return await WithLockAsync(command.SkillId, async () =>
        {
            SkillDefinition? existing = await GetAsync(command.SkillId, cancellationToken);
            if (existing is null)
            {
                return Failure(SkillErrorCodes.NotFound, "The Skill was not found.");
            }

            if (existing.DraftRevision != command.ExpectedDraftRevision)
            {
                return RevisionConflict();
            }

            if (existing.Status is SkillStatus.Archived)
            {
                return Failure(
                    SkillErrorCodes.LifecycleTransitionInvalid,
                    "An archived Skill must be restored before its Draft files can be deleted.");
            }

            SkillDefinition updated = existing with { DraftRevision = existing.DraftRevision + 1 };
            return await ExecuteDraftMutationAsync(
                existing,
                updated,
                command.RelativePath,
                async token => await _fileStore.DeleteDraftAsync(
                    existing.Code,
                    command.RelativePath,
                    token),
                requireExistingFile: true,
                cancellationToken);
        }, cancellationToken);
    }
    #endregion

    #region 发布（PublishAsync）
    /// <summary>
    /// 发布（PublishAsync）
    /// </summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能定义，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<SkillDefinition>> PublishAsync(PublishSkillCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        string versionLabel = command.VersionLabel ?? string.Empty;
        if (!VersionPattern.IsMatch(versionLabel))
        {
            return Failure(
                SkillErrorCodes.VersionInvalid,
                "Skill version must be strict SemVer major.minor.patch.");
        }

        return await WithLockAsync(command.SkillId, async () =>
        {
            SkillDefinition? existing = await GetAsync(command.SkillId, cancellationToken);
            if (existing is null)
            {
                return Failure(SkillErrorCodes.NotFound, "The Skill was not found.");
            }

            if (existing.DraftRevision != command.ExpectedDraftRevision)
            {
                return RevisionConflict();
            }

            if (existing.Status is SkillStatus.Archived)
            {
                return Failure(
                    SkillErrorCodes.LifecycleTransitionInvalid,
                    "An archived Skill must be restored before it can be published.");
            }

            if (existing.PublishedVersions.Any(version =>
                string.Equals(version.Label, versionLabel, StringComparison.Ordinal)))
            {
                return Failure(SkillErrorCodes.VersionConflict, "The Skill version already exists.");
            }

            SkillPublishArtifact artifact;
            try
            {
                artifact = await _fileStore.PublishAsync(existing.Code, versionLabel, cancellationToken);
            }
            catch (SkillFileStoreException exception)
            {
                return Failure(exception.Code, exception.Message);
            }

            var version = new SkillVersion(
                Guid.NewGuid(),
                artifact.VersionLabel,
                artifact.ManifestSha256,
                DateTimeOffset.UtcNow,
                artifact.Files);
            SkillDefinition updated = existing with
            {
                DraftRevision = existing.DraftRevision + 1,
                PublishedVersions = SkillContractCloner.ReadOnly(existing.PublishedVersions.Append(version))
            };

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
                int affected = await UpdateDefinitionEntityAsync(updated, existing.DraftRevision);
                if (affected != 1)
                {
                    await Db.Ado.RollbackTranAsync();
                    await _fileStore.DeletePublishedAsync(existing.Code, versionLabel, cancellationToken);
                    return RevisionConflict();
                }

                await Db.Insertable(MapVersionEntity(
                    existing.Id,
                    existing.PublishedVersions.Count,
                    version)).ExecuteCommandAsync();
                List<AgSkillVersionFile> files = MapFileEntities(version);
                if (files.Count > 0)
                {
                    await Db.Insertable(files).ExecuteCommandAsync();
                }
                await ReconcileAttachmentGroupAsync(
                    version.Id,
                    PublishedAttachmentType,
                    MapPublishedAttachments(existing.Code, version));

                cancellationToken.ThrowIfCancellationRequested();
                await Db.Ado.CommitTranAsync();
                return Success(updated);
            }
            catch
            {
                await Db.Ado.RollbackTranAsync();
                await _fileStore.DeletePublishedAsync(existing.Code, versionLabel, CancellationToken.None);
                throw;
            }
        }, cancellationToken);
    }
    #endregion

    #region 查询列表（ListFilesAsync）
    /// <summary>
    /// 查询列表（ListFilesAsync）
    /// </summary>
    /// <param name="id">技能标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能文件条目集合，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<IReadOnlyList<SkillFileEntry>>> ListFilesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        SkillDefinition? definition = await GetAsync(id, cancellationToken);
        if (definition is null)
        {
            return Failure<IReadOnlyList<SkillFileEntry>>(
                SkillErrorCodes.NotFound,
                "The Skill was not found.");
        }

        try
        {
            return Success(
                await _fileStore.ListDraftAsync(definition.Code, cancellationToken));
        }
        catch (SkillFileStoreException exception)
        {
            return Failure<IReadOnlyList<SkillFileEntry>>(exception.Code, exception.Message);
        }
    }
    #endregion

    #region 设置（SetArchivedAsync）
    /// <summary>
    /// 设置（SetArchivedAsync）
    /// </summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能定义，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<SkillDefinition>> SetArchivedAsync(SetSkillArchiveCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return await WithLockAsync(command.SkillId, async () =>
        {
            SkillDefinition? existing = await GetAsync(command.SkillId, cancellationToken);
            if (existing is null)
            {
                return Failure(SkillErrorCodes.NotFound, "The Skill was not found.");
            }

            if (existing.DraftRevision != command.ExpectedDraftRevision)
            {
                return RevisionConflict();
            }

            SkillStatus target = command.Archived ? SkillStatus.Archived : SkillStatus.Active;
            if (existing.Status == target)
            {
                return Failure(
                    SkillErrorCodes.LifecycleTransitionInvalid,
                    command.Archived ? "The Skill is already archived." : "Only an archived Skill can be restored.");
            }

            if (command.Archived)
            {
                var versionIds = existing.PublishedVersions.Select(value => value.Id).ToHashSet();
                string[] blockers = await FindArchiveBlockersAsync(versionIds, cancellationToken);
                if (blockers.Length > 0)
                {
                    return Failure(
                        SkillErrorCodes.ArchiveBlocked,
                        $"The Skill is still referenced by Agent(s): {string.Join(", ", blockers)}.");
                }
            }

            SkillDefinition updated = existing with
            {
                Status = target,
                DraftRevision = existing.DraftRevision + 1
            };
            return await TryUpdateDefinitionAsync(updated, existing.DraftRevision, cancellationToken)
                ? Success(updated)
                : RevisionConflict();
        }, cancellationToken);
    }
    #endregion

    #region 读取（ReadFileAsync）
    /// <summary>
    /// 读取（ReadFileAsync）
    /// </summary>
    /// <param name="id">技能标识。</param>
    /// <param name="relativePath">相对于存储根目录的文件路径。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能文件文本内容，失败时包含错误状态和提示。</returns>
    public async Task<ServiceResult<string>> ReadFileAsync(Guid id, string relativePath, CancellationToken cancellationToken = default)
    {
        SkillDefinition? definition = await GetAsync(id, cancellationToken);
        if (definition is null)
        {
            return Failure<string>(SkillErrorCodes.NotFound, "The Skill was not found.");
        }

        try
        {
            return Success(
                await _fileStore.ReadDraftTextAsync(definition.Code, relativePath, cancellationToken));
        }
        catch (SkillFileStoreException exception)
        {
            return Failure<string>(exception.Code, exception.Message);
        }
    }
    #endregion

    #region 核对并同步（ReconcileFileAttachmentsAsync）
    /// <summary>
    /// 核对并同步（ReconcileFileAttachmentsAsync）
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    public async Task ReconcileFileAttachmentsAsync(CancellationToken cancellationToken = default)
    {
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            List<AgSkillDefinition> definitions = await Db.Queryable<AgSkillDefinition>()
                .Where(value => !value.IsDeleted)
                .OrderBy(value => value.Code)
                .ToListAsync();
            cancellationToken.ThrowIfCancellationRequested();
            Guid[] skillIds = definitions.Select(value => value.ID).ToArray();
            List<AgSkillVersion> versions = skillIds.Length == 0
                ? []
                : await Db.Queryable<AgSkillVersion>()
                    .Where(value =>
                        value.SkillId.HasValue &&
                        skillIds.Contains(value.SkillId.Value) &&
                        !value.IsDeleted)
                    .OrderBy(value => value.SkillId)
                    .OrderBy(value => value.Ordinal)
                    .ToListAsync();
            cancellationToken.ThrowIfCancellationRequested();
            Guid[] versionIds = versions.Select(value => value.ID).ToArray();
            List<AgSkillVersionFile> versionFiles = versionIds.Length == 0
                ? []
                : await Db.Queryable<AgSkillVersionFile>()
                    .Where(value =>
                        value.VersionId.HasValue &&
                        versionIds.Contains(value.VersionId.Value) &&
                        !value.IsDeleted)
                    .OrderBy(value => value.VersionId)
                    .OrderBy(value => value.Ordinal)
                    .ToListAsync();
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyDictionary<Guid, AgSkillVersionFile[]> filesByVersion = versionFiles
                .GroupBy(value => Required(value.VersionId, "VersionFile.VersionId"))
                .ToDictionary(group => group.Key, group => group.ToArray());
            IReadOnlyDictionary<Guid, AgSkillDefinition> definitionsById = definitions
                .ToDictionary(value => value.ID);

            var draftAttachments = new Dictionary<Guid, IReadOnlyList<FileAttachment>>();
            foreach (AgSkillDefinition definition in definitions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string code = Required(definition.Code, "Code");
                IReadOnlyList<SkillFileEntry> draftFiles =
                    await _fileStore.ListDraftAsync(code, cancellationToken);
                draftAttachments[definition.ID] =
                    MapDraftAttachments(definition.ID, code, draftFiles);
            }

            await DeleteStaleAttachmentGroupsAsync(
                DraftAttachmentType,
                skillIds,
                cancellationToken);
            await DeleteStaleAttachmentGroupsAsync(
                PublishedAttachmentType,
                versionIds,
                cancellationToken);
            foreach ((Guid skillId, IReadOnlyList<FileAttachment> attachments) in draftAttachments)
            {
                await ReconcileAttachmentGroupAsync(
                    skillId,
                    DraftAttachmentType,
                    attachments);
            }
            foreach (AgSkillVersion version in versions)
            {
                Guid skillId = Required(version.SkillId, "Version.SkillId");
                AgSkillDefinition definition = definitionsById[skillId];
                SkillVersion mapped = MapVersion(
                    version,
                    filesByVersion.GetValueOrDefault(version.ID) ?? []);
                await ReconcileAttachmentGroupAsync(
                    version.ID,
                    PublishedAttachmentType,
                    MapPublishedAttachments(
                        Required(definition.Code, "Code"),
                        mapped));
            }

            cancellationToken.ThrowIfCancellationRequested();
            await Db.Ado.CommitTranAsync();
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #region 查找（FindArchiveBlockersAsync）
    /// <summary>
    /// 查找（FindArchiveBlockersAsync）
    /// </summary>
    /// <param name="skillVersionIds">技能版本标识集合。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>最多八个引用该技能版本的启用 Agent 编码，检查草稿及最新发布版本。</returns>
    private async Task<string[]> FindArchiveBlockersAsync(IReadOnlySet<Guid> skillVersionIds, CancellationToken cancellationToken)
    {
        if (skillVersionIds.Count == 0)
        {
            return [];
        }

        List<AgAgentDefinition> enabledAgents = await Db.Queryable<AgAgentDefinition>()
            .Where(value =>
                !value.IsDeleted &&
                value.RuntimeStatus == nameof(AgentRuntimeStatus.Enabled))
            .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();
        if (enabledAgents.Count == 0)
        {
            return [];
        }

        Guid[] agentIds = enabledAgents.Select(value => value.ID).ToArray();
        List<AgAgentVersion> agentVersions = await Db.Queryable<AgAgentVersion>()
            .Where(value =>
                !value.IsDeleted &&
                value.AgentId.HasValue &&
                agentIds.Contains(value.AgentId.Value))
            .OrderBy(value => value.AgentId)
            .OrderBy(value => value.Ordinal)
            .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();
        AgAgentVersion[] draftVersions = agentVersions
            .Where(value => value.IsDraft == true)
            .ToArray();
        AgAgentVersion[] latestPublishedVersions = agentVersions
            .Where(value => value.IsDraft == false)
            .GroupBy(value => Required(value.AgentId, "AgentVersion.AgentId"))
            .Select(group => group.Last())
            .ToArray();
        AgAgentVersion[] currentVersions = draftVersions
            .Concat(latestPublishedVersions)
            .ToArray();
        if (currentVersions.Length == 0)
        {
            return [];
        }

        Guid[] currentVersionIds = currentVersions.Select(value => value.ID).ToArray();
        Guid[] referencedSkillVersionIds = skillVersionIds.ToArray();
        List<Guid?> blockingVersionIds = await Db.Queryable<AgAgentVersionBinding>()
            .Where(value =>
                !value.IsDeleted &&
                (value.Scope == "Version" || value.Scope == "Snapshot") &&
                value.BindingType == "Skill" &&
                value.VersionId.HasValue &&
                currentVersionIds.Contains(value.VersionId.Value) &&
                value.ReferenceId.HasValue &&
                referencedSkillVersionIds.Contains(value.ReferenceId.Value))
            .Select(value => value.VersionId)
            .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var agentIdByVersion = currentVersions.ToDictionary(
            value => value.ID,
            value => Required(value.AgentId, "AgentVersion.AgentId"));
        HashSet<Guid> blockingAgentIds = blockingVersionIds
            .Where(value => value.HasValue)
            .Select(value => agentIdByVersion[value!.Value])
            .ToHashSet();
        return enabledAgents
            .Where(value => blockingAgentIds.Contains(value.ID))
            .Select(value => Required(value.Code, "AgentDefinition.Code"))
            .OrderBy(value => value, StringComparer.Ordinal)
            .Take(8)
            .ToArray();
    }
    #endregion

    #region 查询活动技能的版本是否存在（ExistsAsync）
    /// <summary>
    /// 查询活动技能的版本是否存在（ExistsAsync）。
    /// </summary>
    /// <param name="versionId">待查询的技能版本标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定技能版本及所属定义均未删除且技能状态为 Active 时返回 true，否则返回 false。</returns>
    public async Task<bool> ExistsAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Db.Queryable<AgSkillVersion, AgSkillDefinition>(
                (version, definition) => new JoinQueryInfos(
                    JoinType.Inner,
                    version.SkillId == definition.ID))
            .Where((version, definition) =>
                version.ID == versionId &&
                !version.IsDeleted &&
                !definition.IsDeleted &&
                definition.Status == nameof(SkillStatus.Active))
            .AnyAsync();
    }
    #endregion

    #region 查询列表（ListAsync）
    /// <summary>
    /// 查询列表（ListAsync）
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>活动技能的已发布版本引用；配置内容存储时进一步筛除内容不可用的版本。</returns>
    async Task<IReadOnlyList<PublishedSkillReference>> IPublishedSkillVersionCatalog.ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<AgSkillDefinition> definitions = await Db.Queryable<AgSkillDefinition>()
            .Where(value => !value.IsDeleted && value.Status == nameof(SkillStatus.Active))
            .OrderBy(value => value.Code)
            .ToListAsync();
        IReadOnlyDictionary<Guid, AgSkillVersion[]> versionsBySkill =
            await LoadVersionsBySkillAsync(definitions.Select(value => value.ID), cancellationToken);
        IReadOnlyList<PublishedSkillReference> references =
            SkillContractCloner.ReadOnly(definitions.SelectMany(definition =>
            (versionsBySkill.GetValueOrDefault(definition.ID) ?? []).Select(version =>
                new PublishedSkillReference(
                    definition.ID,
                    version.ID,
                    Required(definition.Code, "Code"),
                    Required(definition.Name, "Name"),
                    Required(version.Label, "Version.Label"),
                    Required(version.ManifestSha256, "Version.ManifestSha256")))));
        if (_publishedContentStore is null)
        {
            return references;
        }

        var available = new List<PublishedSkillReference>(references.Count);
        foreach (PublishedSkillReference reference in references)
        {
            PublishedSkillContent? content;
            try
            {
                content = await _publishedContentStore.ReadAsync(reference, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                continue;
            }

            if (content is not null &&
                content.Instructions is not null &&
                content.SkillVersionId == reference.VersionId &&
                string.Equals(content.SkillCode, reference.SkillCode, StringComparison.Ordinal) &&
                string.Equals(content.VersionLabel, reference.VersionLabel, StringComparison.Ordinal) &&
                string.Equals(content.ManifestSha256, reference.ManifestSha256, StringComparison.Ordinal))
            {
                available.Add(reference);
            }
        }

        return SkillContractCloner.ReadOnly(available);
    }
    #endregion

    #region 加载（LoadDefinitionAsync）
    /// <summary>
    /// 加载（LoadDefinitionAsync）
    /// </summary>
    /// <param name="definition">定义记录。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>补齐有序发布版本和各版本文件清单的技能定义。</returns>
    private async Task<SkillDefinition> LoadDefinitionAsync(AgSkillDefinition definition, CancellationToken cancellationToken)
    {
        List<AgSkillVersion> versions = await Db.Queryable<AgSkillVersion>()
            .Where(value => value.SkillId == definition.ID && !value.IsDeleted)
            .OrderBy(value => value.Ordinal)
            .OrderBy(value => value.ID)
            .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();
        Guid[] versionIds = versions.Select(value => value.ID).ToArray();
        List<AgSkillVersionFile> files = versionIds.Length == 0
            ? []
            : await Db.Queryable<AgSkillVersionFile>()
                .Where(value =>
                    value.VersionId.HasValue &&
                    versionIds.Contains(value.VersionId.Value) &&
                    !value.IsDeleted)
                .OrderBy(value => value.VersionId)
                .OrderBy(value => value.Ordinal)
                .OrderBy(value => value.ID)
                .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return MapDefinition(definition, versions, files);
    }
    #endregion

    #region 加载（LoadVersionsBySkillAsync）
    /// <summary>
    /// 加载（LoadVersionsBySkillAsync）
    /// </summary>
    /// <param name="skillIds">技能标识集合。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>按技能标识分组、组内按版本序号及标识排序的未删除版本字典。</returns>
    private async Task<IReadOnlyDictionary<Guid, AgSkillVersion[]>> LoadVersionsBySkillAsync(IEnumerable<Guid> skillIds, CancellationToken cancellationToken)
    {
        Guid[] ids = skillIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, AgSkillVersion[]>();
        }

        List<AgSkillVersion> versions = await Db.Queryable<AgSkillVersion>()
            .Where(value =>
                value.SkillId.HasValue &&
                ids.Contains(value.SkillId.Value) &&
                !value.IsDeleted)
            .OrderBy(value => value.SkillId)
            .OrderBy(value => value.Ordinal)
            .OrderBy(value => value.ID)
            .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return versions
            .GroupBy(value => Required(value.SkillId, "Version.SkillId"))
            .ToDictionary(group => group.Key, group => group.ToArray());
    }
    #endregion

    #region 执行（ExecuteDraftMutationAsync）
    /// <summary>
    /// 执行（ExecuteDraftMutationAsync）
    /// </summary>
    /// <param name="existing">已有数据。</param>
    /// <param name="updated">更新后的数据。</param>
    /// <param name="relativePath">相对于存储根目录的文件路径。</param>
    /// <param name="mutation">用于修改聚合状态的委托。</param>
    /// <param name="requireExistingFile">是否要求目标文件已存在。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能定义，失败时包含错误状态和提示。</returns>
    private async Task<ServiceResult<SkillDefinition>> ExecuteDraftMutationAsync(
        SkillDefinition existing,
        SkillDefinition updated,
        string relativePath,
        Func<CancellationToken, Task> mutation,
        bool requireExistingFile,
        CancellationToken cancellationToken)
    {
        string? previousContent = null;
        try
        {
            previousContent = await _fileStore.ReadDraftTextAsync(
                existing.Code,
                relativePath,
                cancellationToken);
        }
        catch (SkillFileStoreException exception) when (
            !requireExistingFile && exception.Code == SkillErrorCodes.FileMissing)
        {
            // A new Draft file has no content to restore if the transaction fails.
        }
        catch (SkillFileStoreException exception)
        {
            return Failure(exception.Code, exception.Message);
        }

        bool mutationAttempted = false;
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            int affected = await UpdateDefinitionEntityAsync(
                updated,
                existing.DraftRevision);
            if (affected != 1)
            {
                await Db.Ado.RollbackTranAsync();
                return RevisionConflict();
            }

            mutationAttempted = true;
            await mutation(cancellationToken);
            IReadOnlyList<SkillFileEntry> draftFiles =
                await _fileStore.ListDraftAsync(existing.Code, cancellationToken);
            await ReconcileAttachmentGroupAsync(
                existing.Id,
                DraftAttachmentType,
                MapDraftAttachments(existing.Id, existing.Code, draftFiles));
            cancellationToken.ThrowIfCancellationRequested();
            await Db.Ado.CommitTranAsync();
            return Success(updated);
        }
        catch (SkillFileStoreException exception)
        {
            await Db.Ado.RollbackTranAsync();
            if (mutationAttempted)
            {
                await CompensateDraftMutationAsync(
                    existing.Code,
                    relativePath,
                    previousContent,
                    exception);
            }

            return Failure(exception.Code, exception.Message);
        }
        catch (Exception exception)
        {
            await Db.Ado.RollbackTranAsync();
            if (mutationAttempted)
            {
                await CompensateDraftMutationAsync(
                    existing.Code,
                    relativePath,
                    previousContent,
                    exception);
            }

            throw;
        }
    }
    #endregion

    #region 处理（CompensateDraftMutationAsync）
    /// <summary>
    /// 处理（CompensateDraftMutationAsync）
    /// </summary>
    /// <param name="skillCode">技能编码。</param>
    /// <param name="relativePath">相对于存储根目录的文件路径。</param>
    /// <param name="previousContent">先前保存的内容。</param>
    /// <param name="originalException">最初导致失败的异常。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task CompensateDraftMutationAsync(string skillCode, string relativePath, string? previousContent, Exception originalException)
    {
        try
        {
            if (previousContent is null)
            {
                await _fileStore.DeleteDraftAsync(
                    skillCode,
                    relativePath,
                    CancellationToken.None);
            }
            else
            {
                await _fileStore.WriteDraftTextAsync(
                    skillCode,
                    relativePath,
                    previousContent,
                    CancellationToken.None);
            }
        }
        catch (SkillFileStoreException exception) when (
            previousContent is null && exception.Code == SkillErrorCodes.FileMissing)
        {
            // The failed write did not create a file, so there is nothing to remove.
        }
        catch (Exception compensationException)
        {
            throw new InvalidOperationException(
                "The Skill Draft mutation failed and its file change could not be rolled back.",
                new AggregateException(originalException, compensationException));
        }
    }
    #endregion

    #region 删除（DeleteStaleAttachmentGroupsAsync）
    /// <summary>
    /// 删除（DeleteStaleAttachmentGroupsAsync）
    /// </summary>
    /// <param name="attachmentType">文件附件类型。</param>
    /// <param name="retainedMasterIds">需要保留的主记录标识集合。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task DeleteStaleAttachmentGroupsAsync(string attachmentType, IReadOnlyCollection<Guid> retainedMasterIds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<FileAttachment> existing = await Db.Queryable<FileAttachment>()
            .Filter(null, true)
            .Where(value =>
                value.ImageType == attachmentType)
            .ToListAsync();
        Guid[] staleIds = existing
            .Where(value =>
                !value.MasterId.HasValue ||
                !retainedMasterIds.Contains(value.MasterId.Value))
            .Select(value => value.ID)
            .ToArray();
        if (staleIds.Length > 0)
        {
            await Db.Deleteable<FileAttachment>()
                .Where(value => staleIds.Contains(value.ID))
                .ExecuteCommandAsync();
        }
    }
    #endregion

    #region 核对并同步（ReconcileAttachmentGroupAsync）
    /// <summary>
    /// 核对并同步（ReconcileAttachmentGroupAsync）
    /// </summary>
    /// <param name="masterId">主记录标识。</param>
    /// <param name="attachmentType">文件附件类型。</param>
    /// <param name="desired">期望状态。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task ReconcileAttachmentGroupAsync(Guid masterId, string attachmentType, IReadOnlyList<FileAttachment> desired)
    {
        List<FileAttachment> existing = await Db.Queryable<FileAttachment>()
            .Filter(null, true)
            .Where(value =>
                value.MasterId == masterId &&
                value.ImageType == attachmentType)
            .ToListAsync();
        IReadOnlyDictionary<Guid, FileAttachment> desiredById = desired
            .ToDictionary(value => value.ID);
        Guid[] staleIds = existing
            .Where(value => !desiredById.ContainsKey(value.ID))
            .Select(value => value.ID)
            .ToArray();
        if (staleIds.Length > 0)
        {
            await Db.Deleteable<FileAttachment>()
                .Where(value => staleIds.Contains(value.ID))
                .ExecuteCommandAsync();
        }

        IReadOnlyDictionary<Guid, FileAttachment> existingById = existing
            .Where(value => desiredById.ContainsKey(value.ID))
            .ToDictionary(value => value.ID);
        foreach (FileAttachment attachment in desired)
        {
            if (!existingById.TryGetValue(attachment.ID, out FileAttachment? current))
            {
                await Db.Insertable(attachment).ExecuteCommandAsync();
                continue;
            }

            if (AttachmentMatches(current, attachment))
            {
                continue;
            }

            await Db.Updateable(attachment)
                .UpdateColumns(value => new
                {
                    value.MasterId,
                    value.OriginalFileName,
                    value.FileName,
                    value.FileExt,
                    value.Path,
                    value.Length,
                    value.ImageType,
                    value.IsDeleted,
                    value.IsActive
                })
                .Where(value => value.ID == attachment.ID)
                .ExecuteCommandAsync();
        }
    }
    #endregion

    #region 核对技能附件元数据（AttachmentMatches）
    /// <summary>
    /// 核对技能附件元数据（AttachmentMatches）。
    /// </summary>
    /// <param name="current">现有附件记录。</param>
    /// <param name="desired">期望保存的附件元数据；本方法不读取文件内容。</param>
    /// <returns>附件关联标识、文件名、扩展名、路径、长度及图片类型一致，且现有附件未删除并已启用时返回 true，否则返回 false。</returns>
    private static bool AttachmentMatches(FileAttachment current, FileAttachment desired) =>
        current.MasterId == desired.MasterId &&
        string.Equals(current.OriginalFileName, desired.OriginalFileName, StringComparison.Ordinal) &&
        string.Equals(current.FileName, desired.FileName, StringComparison.Ordinal) &&
        string.Equals(current.FileExt, desired.FileExt, StringComparison.Ordinal) &&
        string.Equals(current.Path, desired.Path, StringComparison.Ordinal) &&
        current.Length == desired.Length &&
        string.Equals(current.ImageType, desired.ImageType, StringComparison.Ordinal) &&
        !current.IsDeleted &&
        current.IsActive == true;
    #endregion

    #region 映射（MapDraftAttachments）
    /// <summary>
    /// 映射（MapDraftAttachments）
    /// </summary>
    /// <param name="skillId">技能标识。</param>
    /// <param name="skillCode">技能编码。</param>
    /// <param name="files">文件集合。</param>
    /// <returns>由草稿文件清单映射的附件记录集合。</returns>
    private static IReadOnlyList<FileAttachment> MapDraftAttachments(Guid skillId, string skillCode, IReadOnlyList<SkillFileEntry> files) => files
        .Select(file => MapAttachment(
            skillId,
            DraftAttachmentType,
            skillCode,
            "draft",
            file.Path,
            file.Size))
        .ToArray();
    #endregion

    #region 映射（MapPublishedAttachments）
    /// <summary>
    /// 映射（MapPublishedAttachments）
    /// </summary>
    /// <param name="skillCode">技能编码。</param>
    /// <param name="version">版本记录。</param>
    /// <returns>由发布版本文件清单映射的附件记录集合。</returns>
    private static IReadOnlyList<FileAttachment> MapPublishedAttachments(string skillCode, SkillVersion version) => version.Files
        .Select(file => MapAttachment(
            version.Id,
            PublishedAttachmentType,
            skillCode,
            $"versions/{version.Label}",
            file.Path,
            file.Size))
        .ToArray();
    #endregion

    #region 映射（MapAttachment）
    /// <summary>
    /// 映射（MapAttachment）
    /// </summary>
    /// <param name="masterId">主记录标识。</param>
    /// <param name="attachmentType">文件附件类型。</param>
    /// <param name="skillCode">技能编码。</param>
    /// <param name="scopePath">操作范围路径。</param>
    /// <param name="relativePath">相对于存储根目录的文件路径。</param>
    /// <param name="size">大小限制或数据大小。</param>
    /// <returns>包含确定性标识、规范化目录及文件元数据的附件记录；文件名或扩展名超限时抛出异常。</returns>
    private static FileAttachment MapAttachment(Guid masterId, string attachmentType, string skillCode, string scopePath, string relativePath, long size)
    {
        string normalizedPath = relativePath.Replace('\\', '/').TrimStart('/');
        string fileName = normalizedPath.Split('/').Last();
        string extension = Path.GetExtension(fileName).TrimStart('.');
        if (fileName.Length > 64 || extension.Length > 10)
        {
            throw new SkillFileStoreException(
                SkillErrorCodes.PathInvalid,
                "Skill file names are limited to 64 characters and extensions to 10 characters.");
        }
        string? relativeDirectory = Path.GetDirectoryName(normalizedPath)?
            .Replace('\\', '/')
            .Trim('/');
        string directory = string.IsNullOrEmpty(relativeDirectory)
            ? $"{skillCode}/{scopePath}/"
            : $"{skillCode}/{scopePath}/{relativeDirectory}/";
        return new FileAttachment
        {
            ID = DeterministicAttachmentId(masterId, attachmentType, normalizedPath),
            MasterId = masterId,
            OriginalFileName = fileName,
            FileName = fileName,
            FileExt = extension,
            Path = directory,
            Length = size,
            ImageType = attachmentType,
            IsDeleted = false,
            IsActive = true
        };
    }
    #endregion

    #region 处理（DeterministicAttachmentId）
    /// <summary>
    /// 处理（DeterministicAttachmentId）
    /// </summary>
    /// <param name="masterId">主记录标识。</param>
    /// <param name="attachmentType">文件附件类型。</param>
    /// <param name="relativePath">相对于存储根目录的文件路径。</param>
    /// <returns>根据附件类型、所属记录和相对路径的 SHA-256 前 16 字节构造的稳定标识。</returns>
    private static Guid DeterministicAttachmentId(Guid masterId, string attachmentType, string relativePath)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{attachmentType}\n{masterId:N}\n{relativePath}"));
        return new Guid(hash.AsSpan(0, 16));
    }
    #endregion

    #region 尝试执行（TryUpdateDefinitionAsync）
    /// <summary>
    /// 尝试执行（TryUpdateDefinitionAsync）
    /// </summary>
    /// <param name="definition">定义记录。</param>
    /// <param name="expectedDraftRevision">预期的草稿修订号。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>异步任务，其结果为：操作是否成功；未满足执行条件或更新未生效时返回 false。</returns>
    private async Task<bool> TryUpdateDefinitionAsync(SkillDefinition definition, long expectedDraftRevision, CancellationToken cancellationToken)
    {
        if (expectedDraftRevision == long.MaxValue ||
            definition.DraftRevision != expectedDraftRevision + 1)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await UpdateDefinitionEntityAsync(definition, expectedDraftRevision) == 1;
    }
    #endregion

    #region 更新（UpdateDefinitionEntityAsync）
    /// <summary>
    /// 更新（UpdateDefinitionEntityAsync）
    /// </summary>
    /// <param name="definition">定义记录。</param>
    /// <param name="expectedDraftRevision">预期的草稿修订号。</param>
    /// <returns>按预期草稿版本更新技能定义所影响的行数。</returns>
    private async Task<int> UpdateDefinitionEntityAsync(SkillDefinition definition, long expectedDraftRevision)
    {
        AgSkillDefinition entity = MapDefinitionEntity(definition);
        return await Db.Updateable(entity)
            .UpdateColumns(value => new
            {
                value.Name,
                value.Description,
                value.Category,
                value.Status,
                value.DraftRevision
            })
            .Where(value =>
                value.ID == definition.Id &&
                value.Code == definition.Code &&
                value.DraftRevision == expectedDraftRevision &&
                !value.IsDeleted)
            .ExecuteCommandAsync();
    }
    #endregion

    #region 映射（MapDefinitionEntity）
    /// <summary>
    /// 映射（MapDefinitionEntity）
    /// </summary>
    /// <param name="definition">定义记录。</param>
    /// <returns>由技能定义构造的主表持久化实体。</returns>
    private static AgSkillDefinition MapDefinitionEntity(SkillDefinition definition) =>
        new()
        {
            ID = definition.Id,
            Code = definition.Code,
            DraftRevision = definition.DraftRevision,
            Name = definition.Name,
            Description = definition.Description,
            Category = definition.Category,
            Status = definition.Status.ToString()
        };
    #endregion

    #region 映射（MapVersionEntity）
    /// <summary>
    /// 映射（MapVersionEntity）
    /// </summary>
    /// <param name="skillId">技能标识。</param>
    /// <param name="ordinal">版本在所属定义中的排序序号。</param>
    /// <param name="version">版本记录。</param>
    /// <returns>带有所属技能和版本序号的发布版本实体。</returns>
    private static AgSkillVersion MapVersionEntity(Guid skillId, int ordinal, SkillVersion version) =>
        new()
        {
            ID = version.Id,
            SkillId = skillId,
            Ordinal = ordinal,
            Label = version.Label,
            ManifestSha256 = version.ManifestSha256,
            PublishedAtUtc = version.PublishedAtUtc.UtcDateTime
        };
    #endregion

    #region 映射（MapFileEntities）
    /// <summary>
    /// 映射（MapFileEntities）
    /// </summary>
    /// <param name="version">版本记录。</param>
    /// <returns>保持清单顺序并包含路径、大小和摘要的技能版本文件实体集合。</returns>
    private static List<AgSkillVersionFile> MapFileEntities(SkillVersion version) =>
        version.Files.Select((file, ordinal) => new AgSkillVersionFile
        {
            ID = Guid.NewGuid(),
            VersionId = version.Id,
            Ordinal = ordinal,
            Path = file.Path,
            Size = file.Size,
            Sha256 = file.Sha256
        }).ToList();
    #endregion

    #region 映射（MapDefinition）
    /// <summary>
    /// 映射（MapDefinition）
    /// </summary>
    /// <param name="definition">定义记录。</param>
    /// <param name="versions">版本记录集合。</param>
    /// <param name="files">文件集合。</param>
    /// <returns>包含状态、有序发布版本及文件清单的技能定义。</returns>
    private static SkillDefinition MapDefinition(AgSkillDefinition definition, IReadOnlyList<AgSkillVersion> versions, IReadOnlyList<AgSkillVersionFile> files)
    {
        IReadOnlyDictionary<Guid, AgSkillVersionFile[]> filesByVersion = files
            .GroupBy(value => Required(value.VersionId, "VersionFile.VersionId"))
            .ToDictionary(group => group.Key, group => group.ToArray());
        return new SkillDefinition(
            definition.ID,
            Required(definition.Code, "Code"),
            Required(definition.Name, "Name"),
            Required(definition.Description, "Description"),
            Required(definition.Category, "Category"),
            Required(definition.DraftRevision, "DraftRevision"),
            SkillContractCloner.ReadOnly(versions
                .OrderBy(value => Required(value.Ordinal, "Version.Ordinal"))
                .Select(version => MapVersion(
                    version,
                    filesByVersion.GetValueOrDefault(version.ID) ?? []))))
        {
            Status = ParseStatus(definition.Status)
        };
    }
    #endregion

    #region 映射（MapVersion）
    /// <summary>
    /// 映射（MapVersion）
    /// </summary>
    /// <param name="version">版本记录。</param>
    /// <param name="files">文件集合。</param>
    /// <returns>包含发布时间、清单摘要及有序文件哈希的技能版本。</returns>
    private static SkillVersion MapVersion(AgSkillVersion version, IReadOnlyList<AgSkillVersionFile> files)
    {
        DateTime publishedAtUtc = Required(version.PublishedAtUtc, "Version.PublishedAtUtc");
        return new SkillVersion(
            version.ID,
            Required(version.Label, "Version.Label"),
            Required(version.ManifestSha256, "Version.ManifestSha256"),
            new DateTimeOffset(DateTime.SpecifyKind(publishedAtUtc, DateTimeKind.Utc)),
            SkillContractCloner.ReadOnly(files
                .OrderBy(value => Required(value.Ordinal, "VersionFile.Ordinal"))
                .Select(value => new SkillFileHash(
                    Required(value.Path, "VersionFile.Path"),
                    Required(value.Size, "VersionFile.Size"),
                    Required(value.Sha256, "VersionFile.Sha256")))));
    }
    #endregion

    #region 解析（ParseStatus）
    /// <summary>
    /// 解析并校验持久化枚举值（ParseStatus）。
    /// </summary>
    /// <param name="value">数据库中存储的枚举文本。</param>
    /// <returns>按区分大小写方式解析且已定义的枚举值；无效输入抛出异常。</returns>
    private static SkillStatus ParseStatus(string? value) =>
        Enum.TryParse(value, ignoreCase: false, out SkillStatus status) && Enum.IsDefined(status)
            ? status
            : throw new InvalidDataException($"Skill Status contains unsupported value '{value}'.");
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="name">对象或字段名称。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static string Required(string? value, string name) => value ?? throw new InvalidDataException($"Skill {name} is required.");
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="name">对象或字段名称。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static Guid Required(Guid? value, string name) => value ?? throw new InvalidDataException($"Skill {name} is required.");
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="name">对象或字段名称。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static long Required(long? value, string name) => value ?? throw new InvalidDataException($"Skill {name} is required.");
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="name">对象或字段名称。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static int Required(int? value, string name) => value ?? throw new InvalidDataException($"Skill {name} is required.");
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="name">对象或字段名称。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static DateTime Required(DateTime? value, string name) => value ?? throw new InvalidDataException($"Skill {name} is required.");
    #endregion

    #region 处理（RevisionConflict）
    /// <summary>
    /// 处理（RevisionConflict）
    /// </summary>
    /// <returns>表示记录版本已变化、需要重新加载后重试的失败服务结果。</returns>
    private ServiceResult<SkillDefinition> RevisionConflict() => Failure(
        SkillErrorCodes.RevisionConflict,
        "The Skill Draft changed before this operation completed.");
    #endregion

    #region 处理（Failure）
    /// <summary>
    /// 处理（Failure）
    /// </summary>
    /// <param name="code">对象编码或业务错误码。</param>
    /// <param name="message">消息或提示文本。</param>
    /// <returns>包含对应业务错误状态和提示信息的失败服务结果。</returns>
    private static ServiceResult<SkillDefinition> Failure(string code, string message) =>
        Failure<SkillDefinition>(code, message);
    #endregion

    #region 处理（Failure）
    /// <summary>
    /// 处理（Failure）
    /// </summary>
    /// <typeparam name="T">待处理数据的泛型类型。</typeparam>
    /// <param name="code">对象编码或业务错误码。</param>
    /// <param name="message">消息或提示文本。</param>
    /// <returns>包含对应业务错误状态和提示信息的失败服务结果。</returns>
    private static ServiceResult<T> Failure<T>(string code, string message) =>
        ServiceResult<T>.Failure(SkillServiceStatusCodes.FromErrorCode(code), message);
    #endregion

    #region 处理（WithLockAsync）
    /// <summary>
    /// 处理（WithLockAsync）
    /// </summary>
    /// <typeparam name="T">待处理数据的泛型类型。</typeparam>
    /// <param name="id">技能标识。</param>
    /// <param name="action">需要执行的操作委托。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>在指定技能标识的进程内锁保护下执行委托得到的结果；结束时释放锁。</returns>
    private static async Task<T> WithLockAsync<T>( Guid id, Func<Task<T>> action, CancellationToken cancellationToken)
    {
        SemaphoreSlim gate = Locks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await action();
        }
        finally
        {
            gate.Release();
        }
    }
    #endregion
}
