using System.Collections.ObjectModel;

namespace EU.Core.Agent.Application.Skills;

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
    IReadOnlyList<SkillVersion> PublishedVersions);

public sealed record SkillListItem(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string Category,
    long DraftRevision,
    string? CurrentPublishedLabel,
    string? CurrentManifestSha256);

public sealed record PublishedSkillReference(
    Guid SkillId,
    Guid VersionId,
    string SkillCode,
    string SkillName,
    string VersionLabel,
    string ManifestSha256);

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

public sealed record PublishSkillCommand(
    Guid SkillId,
    long ExpectedDraftRevision,
    string VersionLabel);

public sealed record SkillQuery(string? Search = null, string? Category = null);

public sealed record SkillError(string Code, string Message);

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
}

public sealed record SkillOperationResult<T>(T? Value, SkillError? Error)
{
    public bool Succeeded => Error is null;

    public static SkillOperationResult<T> Success(T value) => new(value, null);

    public static SkillOperationResult<T> Failure(string code, string message) =>
        new(default, new SkillError(code, message));
}

public interface ISkillRepository
{
    Task<SkillDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SkillDefinition?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SkillDefinition>> ListAsync(
        SkillQuery query,
        CancellationToken cancellationToken = default);

    Task<bool> TryCreateAsync(
        SkillDefinition definition,
        CancellationToken cancellationToken = default);

    Task<bool> TryReplaceAsync(
        SkillDefinition definition,
        long expectedDraftRevision,
        CancellationToken cancellationToken = default);
}

public interface IPublishedSkillVersionCatalog
{
    Task<bool> ExistsAsync(Guid versionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PublishedSkillReference>> ListAsync(
        CancellationToken cancellationToken = default);
}

public interface ISkillFileStore
{
    Task EnsureDraftAsync(string skillCode, string name, string description, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SkillFileEntry>> ListDraftAsync(string skillCode, CancellationToken cancellationToken = default);

    Task<string> ReadDraftTextAsync(string skillCode, string relativePath, CancellationToken cancellationToken = default);

    Task WriteDraftTextAsync(string skillCode, string relativePath, string content, CancellationToken cancellationToken = default);

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
    public static SkillDefinition Clone(SkillDefinition definition) =>
        definition with
        {
            PublishedVersions = ReadOnly(definition.PublishedVersions.Select(Clone))
        };

    public static SkillVersion Clone(SkillVersion version) =>
        version with { Files = ReadOnly(version.Files.Select(file => file with { })) };

    public static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) =>
        new ReadOnlyCollection<T>(values.ToArray());

    public static bool PreservesPublishedHistory(
        SkillDefinition existing,
        SkillDefinition updated)
    {
        if (updated.PublishedVersions.Count < existing.PublishedVersions.Count ||
            updated.PublishedVersions.Count > existing.PublishedVersions.Count + 1)
        {
            return false;
        }

        for (int index = 0; index < existing.PublishedVersions.Count; index++)
        {
            SkillVersion left = existing.PublishedVersions[index];
            SkillVersion right = updated.PublishedVersions[index];
            if (left.Id != right.Id ||
                !string.Equals(left.Label, right.Label, StringComparison.Ordinal) ||
                !string.Equals(left.ManifestSha256, right.ManifestSha256, StringComparison.Ordinal) ||
                left.PublishedAtUtc != right.PublishedAtUtc ||
                left.Files.Count != right.Files.Count)
            {
                return false;
            }

            for (int fileIndex = 0; fileIndex < left.Files.Count; fileIndex++)
            {
                if (left.Files[fileIndex] != right.Files[fileIndex])
                {
                    return false;
                }
            }
        }

        return updated.PublishedVersions
            .Select(version => version.Id)
            .Distinct()
            .Count() == updated.PublishedVersions.Count &&
               updated.PublishedVersions
                   .Select(version => version.Label)
                   .Distinct(StringComparer.Ordinal)
                   .Count() == updated.PublishedVersions.Count;
    }
}
