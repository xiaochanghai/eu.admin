#nullable enable

using System.Collections.ObjectModel;

namespace EU.Core.IServices.Skills;

public enum SkillStatus
{
    Active,
    Archived
}

public sealed record SkillFileEntry(string Path, long Size);

public sealed record SkillFileHash(string Path, long Size, string Sha256);

public sealed record SkillVersion(
    Guid Id,
    string Label,
    string ManifestSha256,
    DateTimeOffset PublishedAtUtc,
    IReadOnlyList<SkillFileHash> Files);

public sealed record SkillDefinition(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string Category,
    long DraftRevision,
    IReadOnlyList<SkillVersion> PublishedVersions)
{
    public SkillStatus Status { get; init; } = SkillStatus.Active;
}

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
    public SkillStatus Status { get; init; } = SkillStatus.Active;
}

public sealed record PublishedSkillReference(
    Guid SkillId,
    Guid VersionId,
    string SkillCode,
    string SkillName,
    string VersionLabel,
    string ManifestSha256);

public sealed record PublishedSkillContent(
    Guid SkillVersionId,
    string SkillCode,
    string VersionLabel,
    string ManifestSha256,
    string Instructions)
{
    public string SkillName { get; init; } = SkillCode;
}

public sealed record CreateSkillCommand(
    string Code,
    string Name,
    string Description,
    string Category);

public sealed record UpdateSkillCommand(
    Guid SkillId,
    long ExpectedDraftRevision,
    string Name,
    string Description,
    string Category);

public sealed record SaveSkillFileCommand(
    Guid SkillId,
    long ExpectedDraftRevision,
    string RelativePath,
    string Content);

public sealed record DeleteSkillFileCommand(
    Guid SkillId,
    long ExpectedDraftRevision,
    string RelativePath);

public sealed record PublishSkillCommand(
    Guid SkillId,
    long ExpectedDraftRevision,
    string VersionLabel);

public sealed record SetSkillArchiveCommand(
    Guid SkillId,
    long ExpectedDraftRevision,
    bool Archived);

public sealed record SkillQuery(
    string? Search = null,
    string? Category = null,
    SkillStatus? Status = null);

public static class SkillErrorCodes
{
    public const string CodeInvalid = "SKILL_CODE_INVALID";
    public const string CodeConflict = "SKILL_CODE_CONFLICT";
    public const string NotFound = "SKILL_NOT_FOUND";
    public const string RevisionConflict = "SKILL_DRAFT_REVISION_CONFLICT";
    public const string PathInvalid = "SKILL_PATH_INVALID";
    public const string FileTooLarge = "SKILL_FILE_TOO_LARGE";
    public const string FileMissing = "SKILL_FILE_MISSING";
    public const string VersionInvalid = "SKILL_VERSION_INVALID";
    public const string VersionConflict = "SKILL_VERSION_CONFLICT";
    public const string VersionNotPublished = "SKILL_VERSION_NOT_PUBLISHED";
    public const string PublishInvalid = "SKILL_PUBLISH_INVALID";
    public const string LifecycleTransitionInvalid = "SKILL_LIFECYCLE_TRANSITION_INVALID";
    public const string ArchiveBlocked = "SKILL_ARCHIVE_BLOCKED";
}

public static class SkillServiceStatusCodes
{
    public const int NotFound = 620001;
    public const int CodeInvalid = 620002;
    public const int CodeConflict = 620003;
    public const int RevisionConflict = 620004;
    public const int PathInvalid = 620005;
    public const int FileTooLarge = 620006;
    public const int FileMissing = 620007;
    public const int VersionInvalid = 620008;
    public const int VersionConflict = 620009;
    public const int VersionNotPublished = 620010;
    public const int PublishInvalid = 620011;
    public const int LifecycleTransitionInvalid = 620012;
    public const int ArchiveBlocked = 620013;

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
}

public interface IPublishedSkillVersionCatalog
{
    Task<bool> ExistsAsync(Guid versionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PublishedSkillReference>> ListAsync(
        CancellationToken cancellationToken = default);
}

public interface IPublishedSkillContentStore
{
    Task<PublishedSkillContent?> ReadAsync(
        PublishedSkillReference reference,
        CancellationToken cancellationToken = default);
}

public interface ISkillFileStore
{
    Task<bool> EnsureDraftAsync(string skillCode, string name, string description, CancellationToken cancellationToken = default);

    Task RollbackDraftCreationAsync(string skillCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SkillFileEntry>> ListDraftAsync(string skillCode, CancellationToken cancellationToken = default);

    Task<string> ReadDraftTextAsync(string skillCode, string relativePath, CancellationToken cancellationToken = default);

    Task WriteDraftTextAsync(string skillCode, string relativePath, string content, CancellationToken cancellationToken = default);

    Task DeleteDraftAsync(string skillCode, string relativePath, CancellationToken cancellationToken = default);

    Task<SkillPublishArtifact> PublishAsync(string skillCode, string versionLabel, CancellationToken cancellationToken = default);

    Task DeletePublishedAsync(string skillCode, string versionLabel, CancellationToken cancellationToken = default);
}

public sealed record SkillPublishArtifact(
    string VersionLabel,
    string ManifestSha256,
    IReadOnlyList<SkillFileHash> Files);

public sealed class SkillFileStoreException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public static class SkillContractCloner
{
    public static PublishedSkillContent Clone(PublishedSkillContent content) =>
        content with { };

    public static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) =>
        new ReadOnlyCollection<T>(values.ToArray());
}
