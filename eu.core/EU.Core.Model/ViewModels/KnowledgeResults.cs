namespace EU.Core.Model;

public enum KnowledgePdfExtractionFailure
{
    None,
    Invalid,
    Encrypted,
    PageLimitExceeded,
    TextLimitExceeded,
    NoExtractableText
}

public sealed record KnowledgePdfExtractionResult(
    string Content,
    int PageCount,
    KnowledgePdfExtractionFailure Failure)
{
    public bool Succeeded => Failure == KnowledgePdfExtractionFailure.None;

    public static KnowledgePdfExtractionResult Success(string content, int pageCount) =>
        new(content, pageCount, KnowledgePdfExtractionFailure.None);

    public static KnowledgePdfExtractionResult Failed(KnowledgePdfExtractionFailure failure) =>
        new(string.Empty, 0, failure);
}

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
    public const string DocumentNotFound = "KNOWLEDGE_DOCUMENT_NOT_FOUND";
    public const string Unavailable = "KNOWLEDGE_BASE_UNAVAILABLE";
    public const string LifecycleTransitionInvalid = "KNOWLEDGE_BASE_LIFECYCLE_TRANSITION_INVALID";
    public const string ArchiveBlocked = "KNOWLEDGE_BASE_ARCHIVE_BLOCKED";
}
