namespace EU.Core.Model;

public enum KnowledgeBaseStatus
{
    Enabled,
    Disabled,
    Archived
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
