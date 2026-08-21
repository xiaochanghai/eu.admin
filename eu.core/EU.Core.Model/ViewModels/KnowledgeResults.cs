#nullable enable

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

public static class KnowledgeServiceStatusCodes
{
    public const int NotFound = 640001;
    public const int CodeInvalid = 640002;
    public const int CodeConflict = 640003;
    public const int RowVersionConflict = 640004;
    public const int DocumentInvalid = 640005;
    public const int DocumentNotFound = 640006;
    public const int Unavailable = 640007;
    public const int LifecycleTransitionInvalid = 640008;
    public const int ArchiveBlocked = 640009;

    public static int FromErrorCode(string errorCode) => errorCode switch
    {
        KnowledgeErrorCodes.NotFound => NotFound,
        KnowledgeErrorCodes.CodeInvalid => CodeInvalid,
        KnowledgeErrorCodes.CodeConflict => CodeConflict,
        KnowledgeErrorCodes.RowVersionConflict => RowVersionConflict,
        KnowledgeErrorCodes.DocumentInvalid => DocumentInvalid,
        KnowledgeErrorCodes.DocumentNotFound => DocumentNotFound,
        KnowledgeErrorCodes.Unavailable => Unavailable,
        KnowledgeErrorCodes.LifecycleTransitionInvalid => LifecycleTransitionInvalid,
        KnowledgeErrorCodes.ArchiveBlocked => ArchiveBlocked,
        _ => Unavailable
    };

    public static string ToErrorCode(int status) => status switch
    {
        NotFound => KnowledgeErrorCodes.NotFound,
        CodeInvalid => KnowledgeErrorCodes.CodeInvalid,
        CodeConflict => KnowledgeErrorCodes.CodeConflict,
        RowVersionConflict => KnowledgeErrorCodes.RowVersionConflict,
        DocumentInvalid => KnowledgeErrorCodes.DocumentInvalid,
        DocumentNotFound => KnowledgeErrorCodes.DocumentNotFound,
        Unavailable => KnowledgeErrorCodes.Unavailable,
        LifecycleTransitionInvalid => KnowledgeErrorCodes.LifecycleTransitionInvalid,
        ArchiveBlocked => KnowledgeErrorCodes.ArchiveBlocked,
        _ => KnowledgeErrorCodes.Unavailable
    };
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
