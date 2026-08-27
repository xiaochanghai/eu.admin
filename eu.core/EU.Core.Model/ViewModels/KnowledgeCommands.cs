namespace EU.Core.Model;

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

public sealed record ImportPdfKnowledgeDocumentCommand(
    Guid KnowledgeBaseId,
    long ExpectedLogicalRevision,
    string FileName,
    string MediaType,
    ReadOnlyMemory<byte> Content);

public sealed record DeleteKnowledgeDocumentCommand(
    Guid KnowledgeBaseId,
    Guid DocumentId,
    long ExpectedLogicalRevision);

public sealed record SetKnowledgeBaseArchiveCommand(
    Guid Id,
    long ExpectedLogicalRevision,
    bool Archived);

public sealed record KnowledgeBaseQuery(KnowledgeBaseStatus? Status = null);
