using System.Collections.ObjectModel;

namespace EU.Core.Agent.Application.Knowledge;

public enum KnowledgeBaseStatus
{
    Enabled,
    Disabled
}

public sealed record KnowledgeDocument(
    Guid Id,
    string FileName,
    string MediaType,
    string Sha256,
    string Content,
    DateTimeOffset ImportedAtUtc);

public sealed record KnowledgeChunk(
    Guid Id,
    Guid DocumentId,
    int Sequence,
    string Content);

public sealed record KnowledgeBaseDefinition(
    Guid Id,
    string Code,
    string Name,
    string Description,
    KnowledgeBaseStatus Status,
    long LogicalRevision,
    IReadOnlyList<KnowledgeDocument> Documents,
    IReadOnlyList<KnowledgeChunk> Chunks,
    DateTimeOffset? IndexedAtUtc);

public sealed record KnowledgeBaseListItem(
    Guid Id,
    string Code,
    string Name,
    string Description,
    KnowledgeBaseStatus Status,
    long LogicalRevision,
    int DocumentCount,
    int ChunkCount,
    DateTimeOffset? IndexedAtUtc);

public sealed record PublishedKnowledgeReference(
    Guid KnowledgeBaseId,
    string Code,
    string Name,
    long LogicalRevision);

public sealed record KnowledgeSearchResult(
    Guid KnowledgeBaseId,
    string KnowledgeBaseCode,
    Guid DocumentId,
    string FileName,
    Guid ChunkId,
    int ChunkSequence,
    string Content,
    double Score);

public sealed record CreateKnowledgeBaseCommand(string Code, string Name, string Description);

public sealed record UpdateKnowledgeBaseCommand(
    Guid Id,
    long ExpectedLogicalRevision,
    string Name,
    string Description,
    KnowledgeBaseStatus Status);

public sealed record ImportKnowledgeDocumentCommand(
    Guid KnowledgeBaseId,
    long ExpectedLogicalRevision,
    string FileName,
    string MediaType,
    string Content);

public sealed record KnowledgeError(string Code, string Message);

public sealed record KnowledgeOperationResult<T>(T? Value, KnowledgeError? Error)
{
    public bool Succeeded => Error is null;
    public static KnowledgeOperationResult<T> Success(T value) => new(value, null);
    public static KnowledgeOperationResult<T> Failure(string code, string message) =>
        new(default, new KnowledgeError(code, message));
}

public static class KnowledgeErrorCodes
{
    public const string NotFound = "KNOWLEDGE_BASE_NOT_FOUND";
    public const string CodeInvalid = "KNOWLEDGE_BASE_CODE_INVALID";
    public const string CodeConflict = "KNOWLEDGE_BASE_CODE_CONFLICT";
    public const string RowVersionConflict = "KNOWLEDGE_BASE_ROW_VERSION_CONFLICT";
    public const string DocumentInvalid = "KNOWLEDGE_DOCUMENT_INVALID";
    public const string Unavailable = "KNOWLEDGE_BASE_UNAVAILABLE";
}

public interface IKnowledgeBaseRepository
{
    Task<KnowledgeBaseDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<KnowledgeBaseDefinition?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KnowledgeBaseDefinition>> ListAsync(CancellationToken cancellationToken = default);
    Task<bool> TryCreateAsync(KnowledgeBaseDefinition value, CancellationToken cancellationToken = default);
    Task<bool> TryReplaceAsync(
        KnowledgeBaseDefinition value,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default);
}

public interface IPublishedKnowledgeCatalog
{
    Task<IReadOnlyList<PublishedKnowledgeReference>> ListAsync(
        CancellationToken cancellationToken = default);
}

public interface IKnowledgeRetriever
{
    Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        IReadOnlyList<Guid> knowledgeBaseIds,
        string query,
        int take,
        CancellationToken cancellationToken = default);
}

public static class KnowledgeContractCloner
{
    public static KnowledgeBaseDefinition Clone(KnowledgeBaseDefinition value) =>
        value with
        {
            Documents = ReadOnly(value.Documents.Select(document => document with { })),
            Chunks = ReadOnly(value.Chunks.Select(chunk => chunk with { }))
        };

    public static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) =>
        new ReadOnlyCollection<T>(values.ToArray());
}
