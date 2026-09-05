#nullable enable

using System.Collections.ObjectModel;

namespace EU.Core.IServices.Skills;

/// <summary>
/// 技能定义的状态。
/// </summary>
public enum SkillStatus
{
    /// <summary>技能处于可用状态。</summary>
    Active,
    /// <summary>技能已归档。</summary>
    Archived
}

/// <summary>
/// 技能草稿中的文件条目。
/// </summary>
/// <param name="Path">技能包内的相对文件路径。</param>
/// <param name="Size">文件大小，单位为字节。</param>
public sealed record SkillFileEntry(string Path, long Size);

/// <summary>
/// 已发布技能文件及其内容摘要。
/// </summary>
/// <param name="Path">技能包内的相对文件路径。</param>
/// <param name="Size">文件大小，单位为字节。</param>
/// <param name="Sha256">内容的 SHA-256 摘要。</param>
public sealed record SkillFileHash(string Path, long Size, string Sha256);

/// <summary>
/// 已发布的技能版本。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="Label">版本标签。</param>
/// <param name="ManifestSha256">技能清单的 SHA-256 摘要。</param>
/// <param name="PublishedAtUtc">版本发布的 UTC 时间。</param>
/// <param name="Files">版本包含的文件集合。</param>
public sealed record SkillVersion(
    Guid Id,
    string Label,
    string ManifestSha256,
    DateTimeOffset PublishedAtUtc,
    IReadOnlyList<SkillFileHash> Files);

/// <summary>
/// 技能定义及其发布版本。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="Code">业务唯一编码或检查项编码。</param>
/// <param name="Name">显示名称或指标名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Category">技能分类。</param>
/// <param name="DraftRevision">当前技能草稿版本。</param>
/// <param name="PublishedVersions">已发布版本集合。</param>
public sealed record SkillDefinition(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string Category,
    long DraftRevision,
    IReadOnlyList<SkillVersion> PublishedVersions)
{
    /// <summary>
    /// 当前状态。
    /// </summary>
    public SkillStatus Status { get; init; } = SkillStatus.Active;
}

/// <summary>
/// 技能定义列表项。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="Code">业务唯一编码或检查项编码。</param>
/// <param name="Name">显示名称或指标名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Category">技能分类。</param>
/// <param name="DraftRevision">当前技能草稿版本。</param>
/// <param name="CurrentPublishedLabel">当前发布版本标签。</param>
/// <param name="CurrentManifestSha256">当前发布版本清单的 SHA-256 摘要。</param>
public sealed record SkillListItem(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string Category,
    long DraftRevision,
    string? CurrentPublishedLabel,
    string? CurrentManifestSha256)
{
    /// <summary>
    /// 当前状态。
    /// </summary>
    public SkillStatus Status { get; init; } = SkillStatus.Active;
}

/// <summary>
/// Agent 运行时引用的已发布技能。
/// </summary>
/// <param name="SkillId">技能定义标识。</param>
/// <param name="VersionId">技能版本标识。</param>
/// <param name="SkillCode">技能业务编码。</param>
/// <param name="SkillName">技能名称。</param>
/// <param name="VersionLabel">发布版本标签。</param>
/// <param name="ManifestSha256">技能清单的 SHA-256 摘要。</param>
public sealed record PublishedSkillReference(
    Guid SkillId,
    Guid VersionId,
    string SkillCode,
    string SkillName,
    string VersionLabel,
    string ManifestSha256);

/// <summary>
/// 运行时加载的已发布技能内容。
/// </summary>
/// <param name="SkillVersionId">技能版本标识。</param>
/// <param name="SkillCode">技能业务编码。</param>
/// <param name="VersionLabel">发布版本标签。</param>
/// <param name="ManifestSha256">技能清单的 SHA-256 摘要。</param>
/// <param name="Instructions">技能运行指令。</param>
public sealed record PublishedSkillContent(
    Guid SkillVersionId,
    string SkillCode,
    string VersionLabel,
    string ManifestSha256,
    string Instructions)
{
    /// <summary>
    /// 技能名称。
    /// </summary>
    public string SkillName { get; init; } = SkillCode;
}

/// <summary>
/// 创建技能定义的命令。
/// </summary>
/// <param name="Code">业务唯一编码或检查项编码。</param>
/// <param name="Name">显示名称或指标名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Category">技能分类。</param>
public sealed record CreateSkillCommand(
    string Code,
    string Name,
    string Description,
    string Category);

/// <summary>
/// 更新技能定义的命令。
/// </summary>
/// <param name="SkillId">技能定义标识。</param>
/// <param name="ExpectedDraftRevision">用于乐观并发控制的预期草稿版本。</param>
/// <param name="Name">显示名称或指标名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Category">技能分类。</param>
public sealed record UpdateSkillCommand(
    Guid SkillId,
    long ExpectedDraftRevision,
    string Name,
    string Description,
    string Category);

/// <summary>
/// 保存技能草稿文件的命令。
/// </summary>
/// <param name="SkillId">技能定义标识。</param>
/// <param name="ExpectedDraftRevision">用于乐观并发控制的预期草稿版本。</param>
/// <param name="RelativePath">技能包内的相对文件路径。</param>
/// <param name="Content">消息、文件或载荷内容。</param>
public sealed record SaveSkillFileCommand(
    Guid SkillId,
    long ExpectedDraftRevision,
    string RelativePath,
    string Content);

/// <summary>
/// 删除技能草稿文件的命令。
/// </summary>
/// <param name="SkillId">技能定义标识。</param>
/// <param name="ExpectedDraftRevision">用于乐观并发控制的预期草稿版本。</param>
/// <param name="RelativePath">技能包内的相对文件路径。</param>
public sealed record DeleteSkillFileCommand(
    Guid SkillId,
    long ExpectedDraftRevision,
    string RelativePath);

/// <summary>
/// 发布技能版本的命令。
/// </summary>
/// <param name="SkillId">技能定义标识。</param>
/// <param name="ExpectedDraftRevision">用于乐观并发控制的预期草稿版本。</param>
/// <param name="VersionLabel">发布版本标签。</param>
public sealed record PublishSkillCommand(
    Guid SkillId,
    long ExpectedDraftRevision,
    string VersionLabel);

/// <summary>
/// 设置技能归档状态的命令。
/// </summary>
/// <param name="SkillId">技能定义标识。</param>
/// <param name="ExpectedDraftRevision">用于乐观并发控制的预期草稿版本。</param>
/// <param name="Archived">是否设置为归档状态。</param>
public sealed record SetSkillArchiveCommand(
    Guid SkillId,
    long ExpectedDraftRevision,
    bool Archived);

/// <summary>
/// 技能定义的查询条件。
/// </summary>
/// <param name="Search">按编码或名称筛选的搜索文本。</param>
/// <param name="Category">技能分类。</param>
/// <param name="Status">当前运行或生命周期状态。</param>
public sealed record SkillQuery(
    string? Search = null,
    string? Category = null,
    SkillStatus? Status = null);

/// <summary>
/// 定义技能领域错误码。
/// </summary>
public static class SkillErrorCodes
{
    /// <summary>表示 <c>CodeInvalid</c> 场景的错误码。</summary>
    public const string CodeInvalid = "SKILL_CODE_INVALID";
    /// <summary>表示 <c>CodeConflict</c> 场景的错误码。</summary>
    public const string CodeConflict = "SKILL_CODE_CONFLICT";
    /// <summary>表示 <c>NotFound</c> 场景的错误码。</summary>
    public const string NotFound = "SKILL_NOT_FOUND";
    /// <summary>表示 <c>RevisionConflict</c> 场景的错误码。</summary>
    public const string RevisionConflict = "SKILL_DRAFT_REVISION_CONFLICT";
    /// <summary>表示 <c>PathInvalid</c> 场景的错误码。</summary>
    public const string PathInvalid = "SKILL_PATH_INVALID";
    /// <summary>表示 <c>FileTooLarge</c> 场景的错误码。</summary>
    public const string FileTooLarge = "SKILL_FILE_TOO_LARGE";
    /// <summary>表示 <c>FileMissing</c> 场景的错误码。</summary>
    public const string FileMissing = "SKILL_FILE_MISSING";
    /// <summary>表示 <c>VersionInvalid</c> 场景的错误码。</summary>
    public const string VersionInvalid = "SKILL_VERSION_INVALID";
    /// <summary>表示 <c>VersionConflict</c> 场景的错误码。</summary>
    public const string VersionConflict = "SKILL_VERSION_CONFLICT";
    /// <summary>表示 <c>VersionNotPublished</c> 场景的错误码。</summary>
    public const string VersionNotPublished = "SKILL_VERSION_NOT_PUBLISHED";
    /// <summary>表示 <c>PublishInvalid</c> 场景的错误码。</summary>
    public const string PublishInvalid = "SKILL_PUBLISH_INVALID";
    /// <summary>表示 <c>LifecycleTransitionInvalid</c> 场景的错误码。</summary>
    public const string LifecycleTransitionInvalid = "SKILL_LIFECYCLE_TRANSITION_INVALID";
    /// <summary>表示 <c>ArchiveBlocked</c> 场景的错误码。</summary>
    public const string ArchiveBlocked = "SKILL_ARCHIVE_BLOCKED";
}

/// <summary>
/// 将技能领域错误映射为服务状态码。
/// </summary>
public static class SkillServiceStatusCodes
{
    /// <summary>表示 <c>NotFound</c> 场景映射的服务状态码。</summary>
    public const int NotFound = 620001;
    /// <summary>表示 <c>CodeInvalid</c> 场景映射的服务状态码。</summary>
    public const int CodeInvalid = 620002;
    /// <summary>表示 <c>CodeConflict</c> 场景映射的服务状态码。</summary>
    public const int CodeConflict = 620003;
    /// <summary>表示 <c>RevisionConflict</c> 场景映射的服务状态码。</summary>
    public const int RevisionConflict = 620004;
    /// <summary>表示 <c>PathInvalid</c> 场景映射的服务状态码。</summary>
    public const int PathInvalid = 620005;
    /// <summary>表示 <c>FileTooLarge</c> 场景映射的服务状态码。</summary>
    public const int FileTooLarge = 620006;
    /// <summary>表示 <c>FileMissing</c> 场景映射的服务状态码。</summary>
    public const int FileMissing = 620007;
    /// <summary>表示 <c>VersionInvalid</c> 场景映射的服务状态码。</summary>
    public const int VersionInvalid = 620008;
    /// <summary>表示 <c>VersionConflict</c> 场景映射的服务状态码。</summary>
    public const int VersionConflict = 620009;
    /// <summary>表示 <c>VersionNotPublished</c> 场景映射的服务状态码。</summary>
    public const int VersionNotPublished = 620010;
    /// <summary>表示 <c>PublishInvalid</c> 场景映射的服务状态码。</summary>
    public const int PublishInvalid = 620011;
    /// <summary>表示 <c>LifecycleTransitionInvalid</c> 场景映射的服务状态码。</summary>
    public const int LifecycleTransitionInvalid = 620012;
    /// <summary>表示 <c>ArchiveBlocked</c> 场景映射的服务状态码。</summary>
    public const int ArchiveBlocked = 620013;

    #region 转换（FromErrorCode）
    /// <summary>
    /// 转换（FromErrorCode）
    /// </summary>
    /// <param name="errorCode">操作失败对应的业务错误码。</param>
    /// <returns>技能错误码对应的服务状态值；未知错误码抛出 ArgumentOutOfRangeException。</returns>
    public static int FromErrorCode(string errorCode) => errorCode switch
    {
        SkillErrorCodes.NotFound => NotFound,
        SkillErrorCodes.CodeInvalid => CodeInvalid,
        SkillErrorCodes.CodeConflict => CodeConflict,
        SkillErrorCodes.RevisionConflict => RevisionConflict,
        SkillErrorCodes.PathInvalid => PathInvalid,
        SkillErrorCodes.FileTooLarge => FileTooLarge,
        SkillErrorCodes.FileMissing => FileMissing,
        SkillErrorCodes.VersionInvalid => VersionInvalid,
        SkillErrorCodes.VersionConflict => VersionConflict,
        SkillErrorCodes.VersionNotPublished => VersionNotPublished,
        SkillErrorCodes.PublishInvalid => PublishInvalid,
        SkillErrorCodes.LifecycleTransitionInvalid => LifecycleTransitionInvalid,
        SkillErrorCodes.ArchiveBlocked => ArchiveBlocked,
        _ => throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, null)
    };
    #endregion

    #region 转换（ToErrorCode）
    /// <summary>
    /// 转换（ToErrorCode）
    /// </summary>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <returns>服务状态值对应的技能错误码；未知状态抛出 ArgumentOutOfRangeException。</returns>
    public static string ToErrorCode(int status) => status switch
    {
        NotFound => SkillErrorCodes.NotFound,
        CodeInvalid => SkillErrorCodes.CodeInvalid,
        CodeConflict => SkillErrorCodes.CodeConflict,
        RevisionConflict => SkillErrorCodes.RevisionConflict,
        PathInvalid => SkillErrorCodes.PathInvalid,
        FileTooLarge => SkillErrorCodes.FileTooLarge,
        FileMissing => SkillErrorCodes.FileMissing,
        VersionInvalid => SkillErrorCodes.VersionInvalid,
        VersionConflict => SkillErrorCodes.VersionConflict,
        VersionNotPublished => SkillErrorCodes.VersionNotPublished,
        PublishInvalid => SkillErrorCodes.PublishInvalid,
        LifecycleTransitionInvalid => SkillErrorCodes.LifecycleTransitionInvalid,
        ArchiveBlocked => SkillErrorCodes.ArchiveBlocked,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
    #endregion
}

/// <summary>
/// 定义 Agent 运行时可用的已发布技能版本目录。
/// </summary>
public interface IPublishedSkillVersionCatalog
{
    #region 查询活动技能的版本是否存在（ExistsAsync）
    /// <summary>
    /// 查询活动技能的版本是否存在（ExistsAsync）。
    /// </summary>
    /// <param name="versionId">待查询的技能版本标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定技能版本及所属定义均未删除且技能状态为 Active 时返回 true，否则返回 false。</returns>
    Task<bool> ExistsAsync(Guid versionId, CancellationToken cancellationToken = default);
    #endregion

    #region 查询已发布技能版本列表。
    /// <summary>查询已发布技能版本列表。</summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>可供 Agent 绑定的活动技能已发布版本引用集合。</returns>
    Task<IReadOnlyList<PublishedSkillReference>> ListAsync(CancellationToken cancellationToken = default);
    #endregion
}

/// <summary>
/// 定义已发布技能内容的读取边界。
/// </summary>
public interface IPublishedSkillContentStore
{
    #region 读取已发布技能内容。
    /// <summary>读取已发布技能内容。</summary>
    /// <param name="reference">需要读取内容的已发布技能引用。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定已发布技能的指令内容及完整性信息；没有可用内容时为 null。</returns>
    Task<PublishedSkillContent?> ReadAsync(PublishedSkillReference reference, CancellationToken cancellationToken = default);
    #endregion
}

/// <summary>
/// 定义技能草稿文件及发布产物的存储边界。
/// </summary>
public interface ISkillFileStore
{
    #region 确保技能草稿及初始说明文件存在（EnsureDraftAsync）
    /// <summary>
    /// 确保技能草稿及初始说明文件存在（EnsureDraftAsync）。
    /// </summary>
    /// <param name="skillCode">技能编码。</param>
    /// <param name="name">生成初始说明标题的技能名称，空白时使用技能编码。</param>
    /// <param name="description">生成初始说明的描述，空白时使用默认描述。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>新建草稿 SKILL.md 时返回 true；该文件已存在且未覆盖时返回 false；路径校验或写入失败通过异常报告。</returns>
    Task<bool> EnsureDraftAsync(string skillCode, string name, string description, CancellationToken cancellationToken = default);
    #endregion

    #region 回滚新建的技能草稿目录。
    /// <summary>回滚新建的技能草稿目录。</summary>
    /// <param name="skillCode">技能编码。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示操作完成的异步任务。</returns>
    Task RollbackDraftCreationAsync(string skillCode, CancellationToken cancellationToken = default);
    #endregion

    #region 查询技能草稿文件列表。
    /// <summary>查询技能草稿文件列表。</summary>
    /// <param name="skillCode">技能编码。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定技能草稿目录中的文件及其路径和大小清单。</returns>
    Task<IReadOnlyList<SkillFileEntry>> ListDraftAsync(string skillCode, CancellationToken cancellationToken = default);
    #endregion

    #region 读取技能草稿文本文件。
    /// <summary>读取技能草稿文本文件。</summary>
    /// <param name="skillCode">技能编码。</param>
    /// <param name="relativePath">相对于存储根目录的文件路径。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定技能草稿相对路径下的文本文件内容。</returns>
    Task<string> ReadDraftTextAsync(string skillCode, string relativePath, CancellationToken cancellationToken = default);
    #endregion

    #region 写入技能草稿文本文件。
    /// <summary>写入技能草稿文本文件。</summary>
    /// <param name="skillCode">技能编码。</param>
    /// <param name="relativePath">相对于存储根目录的文件路径。</param>
    /// <param name="content">写入技能草稿文件的完整文本内容。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示操作完成的异步任务。</returns>
    Task WriteDraftTextAsync(string skillCode, string relativePath, string content, CancellationToken cancellationToken = default);
    #endregion

    #region 删除技能草稿文件。
    /// <summary>删除技能草稿文件。</summary>
    /// <param name="skillCode">技能编码。</param>
    /// <param name="relativePath">相对于存储根目录的文件路径。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示操作完成的异步任务。</returns>
    Task DeleteDraftAsync(string skillCode, string relativePath, CancellationToken cancellationToken = default);
    #endregion

    #region 发布技能文件。
    /// <summary>发布技能文件。</summary>
    /// <param name="skillCode">技能编码。</param>
    /// <param name="versionLabel">技能发布版本标签。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>发布后的技能文件清单及清单摘要等制品信息。</returns>
    Task<SkillPublishArtifact> PublishAsync(string skillCode, string versionLabel, CancellationToken cancellationToken = default);
    #endregion

    #region 删除指定的已发布技能产物。
    /// <summary>删除指定的已发布技能产物。</summary>
    /// <param name="skillCode">技能编码。</param>
    /// <param name="versionLabel">技能发布版本标签。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示操作完成的异步任务。</returns>
    Task DeletePublishedAsync(string skillCode, string versionLabel, CancellationToken cancellationToken = default);
    #endregion
}

/// <summary>
/// 技能发布过程生成的版本产物。
/// </summary>
/// <param name="VersionLabel">发布版本标签。</param>
/// <param name="ManifestSha256">技能清单的 SHA-256 摘要。</param>
/// <param name="Files">版本包含的文件集合。</param>
public sealed record SkillPublishArtifact(
    string VersionLabel,
    string ManifestSha256,
    IReadOnlyList<SkillFileHash> Files);

/// <summary>
/// 表示技能文件存储过程中的领域异常。
/// </summary>
/// <param name="code">用于标识技能文件存储失败原因的错误码。</param>
/// <param name="message">描述异常原因的错误消息。</param>
public sealed class SkillFileStoreException(string code, string message) : Exception(message)
{
    /// <summary>
    /// 获取领域异常对应的错误码。
    /// </summary>
    public string Code { get; } = code;
}

/// <summary>
/// 提供技能契约对象的防御性复制。
/// </summary>
public static class SkillContractCloner
{
    #region 复制（Clone）
    /// <summary>
    /// 复制（Clone）
    /// </summary>
    /// <param name="content">需要复制的已发布技能内容记录。</param>
    /// <returns>已发布技能内容记录的副本。</returns>
    public static PublishedSkillContent Clone(PublishedSkillContent content) =>
        content with { };
    #endregion

    #region 读取（ReadOnly）
    /// <summary>
    /// 读取（ReadOnly）
    /// </summary>
    /// <param name="values">按原顺序枚举并复制为只读集合的源数据。</param>
    /// <typeparam name="T">待处理数据的泛型类型。</typeparam>
    /// <returns>按枚举顺序复制到新数组并包装为只读的集合；元素本身不作深复制。</returns>
    public static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) =>
        new ReadOnlyCollection<T>(values.ToArray());
    #endregion
}
