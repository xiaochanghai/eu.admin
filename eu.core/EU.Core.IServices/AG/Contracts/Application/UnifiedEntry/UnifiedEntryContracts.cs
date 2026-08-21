#nullable enable

using System.Collections.ObjectModel;

namespace EU.Core.IServices.UnifiedEntry;

public enum UnifiedRunStatus
{
    Pending,
    Running,
    WaitingForApproval,
    Completed,
    Failed,
    Cancelled,
    Blocked
}

public enum UnifiedAgentRunKind
{
    Main,
    Child
}

public enum ConversationMessageRole
{
    User,
    Assistant
}

public sealed record UnifiedEntryLimits
{
    public UnifiedEntryLimits(
        int MaxAgentDepth = 4,
        int MaxChildCalls = 8,
        int MaxOrchestrationCalls = 4,
        int MaxMcpCalls = 20,
        TimeSpan? EntryTimeout = null,
        TimeSpan? ChildTimeout = null,
        int InternalPayloadUtf8Bytes = 32_768,
        int MaxMcpResultUtf8Bytes = 4_194_304)
    {
        this.MaxAgentDepth = MaxAgentDepth;
        this.MaxChildCalls = MaxChildCalls;
        this.MaxOrchestrationCalls = MaxOrchestrationCalls;
        this.MaxMcpCalls = MaxMcpCalls;
        this.EntryTimeout = EntryTimeout ?? TimeSpan.FromSeconds(300);
        this.ChildTimeout = ChildTimeout ?? TimeSpan.FromSeconds(120);
        this.InternalPayloadUtf8Bytes = InternalPayloadUtf8Bytes;
        this.MaxMcpResultUtf8Bytes = MaxMcpResultUtf8Bytes;
    }

    public int MaxAgentDepth { get; init; }

    public int MaxChildCalls { get; init; }

    public int MaxOrchestrationCalls { get; init; }

    public int MaxMcpCalls { get; init; }

    public TimeSpan EntryTimeout { get; init; }

    public TimeSpan ChildTimeout { get; init; }

    public int InternalPayloadUtf8Bytes { get; init; }

    public int MaxMcpResultUtf8Bytes { get; init; }

    public static UnifiedEntryLimits Default { get; } = new();
}

public static class UnifiedEntryErrorCodes
{
    public const string PayloadLimitExceeded = "UNIFIED_ENTRY_PAYLOAD_LIMIT_EXCEEDED";
    public const string PayloadInvalidEncoding = "UNIFIED_ENTRY_PAYLOAD_INVALID_ENCODING";
    public const string SequenceExhausted = "UNIFIED_ENTRY_SEQUENCE_EXHAUSTED";
    public const string AgentCycleDetected = "UNIFIED_ENTRY_AGENT_CYCLE_DETECTED";
    public const string AgentDepthLimitExceeded = "UNIFIED_ENTRY_AGENT_DEPTH_LIMIT_EXCEEDED";
    public const string ChildCallLimitExceeded = "UNIFIED_ENTRY_CHILD_CALL_LIMIT_EXCEEDED";
    public const string ChildCatalogInvalid = "UNIFIED_ENTRY_CHILD_CATALOG_INVALID";
    public const string OrchestrationCallLimitExceeded =
        "UNIFIED_ENTRY_ORCHESTRATION_CALL_LIMIT_EXCEEDED";
    public const string McpCallLimitExceeded = "UNIFIED_ENTRY_MCP_CALL_LIMIT_EXCEEDED";
    public const string McpResultLimitExceeded =
        "UNIFIED_ENTRY_MCP_RESULT_LIMIT_EXCEEDED";
    public const string InvalidState = "UNIFIED_ENTRY_INVALID_STATE";
    public const string InternalArgumentsInvalid =
        "UNIFIED_ENTRY_INTERNAL_ARGUMENTS_INVALID";
    public const string AgentVersionUnauthorized =
        "UNIFIED_ENTRY_AGENT_VERSION_UNAUTHORIZED";
    public const string OrchestrationVersionUnauthorized =
        "UNIFIED_ENTRY_ORCHESTRATION_VERSION_UNAUTHORIZED";
    public const string SkillVersionUnauthorized =
        "UNIFIED_ENTRY_SKILL_VERSION_UNAUTHORIZED";
    public const string KnowledgeBaseUnauthorized =
        KnowledgeAccessDenied;
    public const string KnowledgeAccessDenied = "KNOWLEDGE_ACCESS_DENIED";
    public const string EntryTimeout = "UNIFIED_ENTRY_TIMEOUT";
    public const string ChildTimeout = "UNIFIED_ENTRY_CHILD_TIMEOUT";
    public const string Cancelled = "UNIFIED_ENTRY_CANCELLED";
    public const string ChildExecutionFailed =
        "UNIFIED_ENTRY_CHILD_EXECUTION_FAILED";
    public const string OrchestrationExecutionFailed =
        "UNIFIED_ENTRY_ORCHESTRATION_EXECUTION_FAILED";
    public const string OrchestrationDetailsMissing =
        "UNIFIED_ENTRY_ORCHESTRATION_DETAILS_MISSING";
    public const string ConversationNotFound =
        "UNIFIED_ENTRY_CONVERSATION_NOT_FOUND";
    public const string PersistenceFailed =
        "UNIFIED_ENTRY_PERSISTENCE_FAILED";
    public const string BusinessQueryEvidenceRequired =
        "BUSINESS_QUERY_EVIDENCE_REQUIRED";
    public const string BusinessQueryCallLimitExceeded =
        "BUSINESS_QUERY_CALL_LIMIT_EXCEEDED";
    public const string BusinessQueryResultLimitExceeded =
        "BUSINESS_QUERY_RESULT_LIMIT_EXCEEDED";
    public const string HostInterrupted =
        "UNIFIED_ENTRY_HOST_INTERRUPTED";
}

public sealed class UnifiedEntryException(string errorCode, string message)
    : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}

public sealed record ConversationRecord(
    Guid Id,
    string Title,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public string TenantId { get; init; } = UnifiedEntryOwnership.LegacyUnowned;
    public string UserId { get; init; } = UnifiedEntryOwnership.LegacyUnowned;
}

public sealed record ConversationMessageRecord(
    Guid Id,
    Guid ConversationId,
    ConversationMessageRole Role,
    string Content,
    string ContentSha256,
    int ContentUtf8Bytes,
    DateTimeOffset CreatedAtUtc)
{
    public ConversationMessageKind Kind { get; init; } = ConversationMessageKind.Legacy;
    public Guid? BusinessQueryId { get; init; }
    public string BusinessQueryReceiptJson { get; init; } = string.Empty;
    public string BusinessQueryPresentationJson { get; init; } = string.Empty;
    public string BusinessQueryIntegritySha256 { get; init; } = string.Empty;
}

public enum ConversationMessageKind
{
    Legacy,
    UserInput,
    AssistantNarrative,
    BusinessQueryResult
}

public sealed record UnifiedEntryRunRecord(
    Guid Id,
    Guid ConversationId,
    Guid CorrelationId,
    Guid MainAgentVersionId,
    UnifiedRunStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    TimeSpan? Duration,
    string Input,
    string InputSha256,
    string Output,
    string OutputSha256,
    string ErrorCode)
{
    public string TenantId { get; init; } = UnifiedEntryOwnership.LegacyUnowned;
    public string UserId { get; init; } = UnifiedEntryOwnership.LegacyUnowned;
}

public static class UnifiedEntryOwnership
{
    public const string LegacyUnowned = "__legacy_unowned__";
}

public sealed record BusinessQueryCleanupResult(
    int MessagesRedacted,
    int ToolCallsRedacted,
    int EventsRedacted,
    DateTimeOffset CutoffUtc);

public sealed record UnifiedAgentRunRecord(
    Guid Id,
    Guid EntryRunId,
    Guid? ParentRunId,
    UnifiedAgentRunKind Kind,
    Guid AgentId,
    Guid AgentVersionId,
    int Depth,
    UnifiedRunStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    TimeSpan? Duration,
    string Input,
    string InputSha256,
    string Output,
    string OutputSha256,
    string ErrorCode);

public sealed record UnifiedOrchestrationRunLink(
    Guid Id,
    Guid EntryRunId,
    Guid ParentRunId,
    Guid OrchestrationRunId,
    Guid OrchestrationVersionId,
    int Depth,
    UnifiedRunStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    TimeSpan? Duration,
    string Input,
    string InputSha256,
    string Output,
    string OutputSha256,
    string ErrorCode);

public sealed record UnifiedToolCallRecord(
    Guid Id,
    Guid EntryRunId,
    Guid ParentRunId,
    Guid ToolVersionId,
    int Depth,
    UnifiedRunStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    TimeSpan? Duration,
    string ArgumentsJson,
    string ArgumentsSha256,
    string ResultContent,
    string ResultSha256,
    string ErrorCode);

public sealed record UnifiedRunEventRecord(
    Guid Id,
    Guid EntryRunId,
    long Sequence,
    Guid CorrelationId,
    string Kind,
    DateTimeOffset OccurredAtUtc,
    Guid? ParentRunId,
    int Depth,
    string PayloadJson,
    string PayloadSha256);

public sealed record UnifiedRunDetails
{
    public UnifiedRunDetails(
        UnifiedEntryRunRecord entryRun,
        IReadOnlyList<UnifiedAgentRunRecord> agentRuns,
        IReadOnlyList<UnifiedOrchestrationRunLink> orchestrations,
        IReadOnlyList<UnifiedToolCallRecord> toolCalls)
    {
        EntryRun = entryRun with { };
        AgentRuns = UnifiedEntryContractCloner.ReadOnly(
            agentRuns.Select(value => value with { }));
        Orchestrations = UnifiedEntryContractCloner.ReadOnly(
            orchestrations.Select(value => value with { }));
        ToolCalls = UnifiedEntryContractCloner.ReadOnly(
            toolCalls.Select(value => value with { }));
    }

    public UnifiedEntryRunRecord EntryRun { get; }

    public IReadOnlyList<UnifiedAgentRunRecord> AgentRuns { get; }

    public IReadOnlyList<UnifiedOrchestrationRunLink> Orchestrations { get; }

    public IReadOnlyList<UnifiedToolCallRecord> ToolCalls { get; }

    public UnifiedRunDetails WithEntryRun(UnifiedEntryRunRecord entryRun) =>
        new(entryRun, AgentRuns, Orchestrations, ToolCalls);
}

public sealed record UnifiedEntryAggregate
{
    public UnifiedEntryAggregate(
        ConversationRecord conversation,
        IReadOnlyList<ConversationMessageRecord> messages,
        UnifiedRunDetails details,
        IReadOnlyList<UnifiedRunEventRecord> events,
        long persistenceRevision = 0)
    {
        if (persistenceRevision < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(persistenceRevision),
                "The persistence revision cannot be negative.");
        }

        Conversation = conversation with { };
        Messages = UnifiedEntryContractCloner.ReadOnly(
            messages.Select(value => value with { }));
        Details = UnifiedEntryContractCloner.Clone(details);
        Events = UnifiedEntryContractCloner.ReadOnly(
            events.Select(value => value with { }));
        PersistenceRevision = persistenceRevision;
    }

    public ConversationRecord Conversation { get; }

    public IReadOnlyList<ConversationMessageRecord> Messages { get; }

    public UnifiedRunDetails Details { get; }

    public IReadOnlyList<UnifiedRunEventRecord> Events { get; }

    public long PersistenceRevision { get; }

    public UnifiedEntryAggregate WithDetails(UnifiedRunDetails details) =>
        new(Conversation, Messages, details, Events, PersistenceRevision);

    public UnifiedEntryAggregate WithMessage(ConversationMessageRecord message) =>
        new(
            Conversation,
            Messages.Append(message).ToArray(),
            Details,
            Events,
            PersistenceRevision);

    public UnifiedEntryAggregate WithEvent(UnifiedRunEventRecord value) =>
        new(
            Conversation,
            Messages,
            Details,
            Events.Append(value).ToArray(),
            PersistenceRevision);

    public UnifiedEntryAggregate WithPersistenceRevision(long value) =>
        new(Conversation, Messages, Details, Events, value);
}

public static class UnifiedEntryReadLimits
{
    public const int DefaultMessageTake = 100;
    public const int MaximumMessageTake = 500;
}

public interface IUnifiedEntryRepository
{
    Task<ConversationRecord?> GetConversationAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationRecord>> ListConversationsAsync(
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationMessageRecord>> ListMessagesAsync(
        Guid conversationId,
        int take = UnifiedEntryReadLimits.DefaultMessageTake,
        CancellationToken cancellationToken = default);

    Task<UnifiedEntryRunRecord?> GetRunAsync(
        Guid runId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UnifiedEntryRunRecord>> ListRunsAsync(
        Guid conversationId,
        int take,
        CancellationToken cancellationToken = default);

    Task<UnifiedRunDetails?> GetDetailsAsync(
        Guid runId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UnifiedRunEventRecord>> ListEventsAsync(
        Guid runId,
        CancellationToken cancellationToken = default);

    Task<BusinessQueryCleanupResult> RedactExpiredBusinessQueryResultsAsync(
        DateTimeOffset cutoffUtc,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new BusinessQueryCleanupResult(0, 0, 0, cutoffUtc));

    Task<ConversationRecord?> GetConversationForOwnerAsync(
        Guid id,
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default) =>
        GetOwnedConversationFallbackAsync(this, id, tenantId, userId, cancellationToken);

    Task<IReadOnlyList<ConversationRecord>> ListConversationsForOwnerAsync(
        string tenantId,
        string userId,
        int take,
        CancellationToken cancellationToken = default) =>
        ListOwnedConversationsFallbackAsync(this, tenantId, userId, take, cancellationToken);

    Task<IReadOnlyList<ConversationMessageRecord>> ListMessagesForOwnerAsync(
        Guid conversationId,
        string tenantId,
        string userId,
        int take = UnifiedEntryReadLimits.DefaultMessageTake,
        CancellationToken cancellationToken = default) =>
        ListOwnedMessagesFallbackAsync(
            this, conversationId, tenantId, userId, take, cancellationToken);

    Task<UnifiedEntryRunRecord?> GetRunForOwnerAsync(
        Guid runId,
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default) =>
        GetOwnedRunFallbackAsync(this, runId, tenantId, userId, cancellationToken);

    Task<IReadOnlyList<UnifiedEntryRunRecord>> ListRunsForOwnerAsync(
        Guid conversationId,
        string tenantId,
        string userId,
        int take,
        CancellationToken cancellationToken = default) =>
        ListOwnedRunsFallbackAsync(
            this, conversationId, tenantId, userId, take, cancellationToken);

    Task<UnifiedRunDetails?> GetDetailsForOwnerAsync(
        Guid runId,
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default) =>
        GetOwnedDetailsFallbackAsync(this, runId, tenantId, userId, cancellationToken);

    Task<IReadOnlyList<UnifiedRunEventRecord>> ListEventsForOwnerAsync(
        Guid runId,
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default) =>
        ListOwnedEventsFallbackAsync(this, runId, tenantId, userId, cancellationToken);

    Task<UnifiedEntryAggregate?> GetAggregateForOwnerAsync(
        Guid runId,
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<UnifiedEntryAggregate?>(null);

    Task<UnifiedEntryAggregate> SaveAsync(
        UnifiedEntryAggregate value,
        CancellationToken cancellationToken = default);

    private static async Task<ConversationRecord?> GetOwnedConversationFallbackAsync(
        IUnifiedEntryRepository repository,
        Guid id,
        string tenantId,
        string userId,
        CancellationToken cancellationToken)
    {
        ConversationRecord? value = await repository.GetConversationAsync(id, cancellationToken);
        return value is not null && Owned(value.TenantId, value.UserId, tenantId, userId)
            ? value
            : null;
    }

    private static async Task<IReadOnlyList<ConversationRecord>>
        ListOwnedConversationsFallbackAsync(
            IUnifiedEntryRepository repository,
            string tenantId,
            string userId,
            int take,
            CancellationToken cancellationToken) =>
        (await repository.ListConversationsAsync(take, cancellationToken))
            .Where(value => Owned(value.TenantId, value.UserId, tenantId, userId))
            .ToArray();

    private static async Task<IReadOnlyList<ConversationMessageRecord>>
        ListOwnedMessagesFallbackAsync(
            IUnifiedEntryRepository repository,
            Guid conversationId,
            string tenantId,
            string userId,
            int take,
            CancellationToken cancellationToken) =>
        await GetOwnedConversationFallbackAsync(
            repository, conversationId, tenantId, userId, cancellationToken) is null
                ? []
                : await repository.ListMessagesAsync(
                    conversationId, take, cancellationToken);

    private static async Task<UnifiedEntryRunRecord?> GetOwnedRunFallbackAsync(
        IUnifiedEntryRepository repository,
        Guid runId,
        string tenantId,
        string userId,
        CancellationToken cancellationToken)
    {
        UnifiedEntryRunRecord? value = await repository.GetRunAsync(runId, cancellationToken);
        return value is not null && Owned(value.TenantId, value.UserId, tenantId, userId)
            ? value
            : null;
    }

    private static async Task<IReadOnlyList<UnifiedEntryRunRecord>>
        ListOwnedRunsFallbackAsync(
            IUnifiedEntryRepository repository,
            Guid conversationId,
            string tenantId,
            string userId,
            int take,
            CancellationToken cancellationToken) =>
        (await repository.ListRunsAsync(conversationId, take, cancellationToken))
            .Where(value => Owned(value.TenantId, value.UserId, tenantId, userId))
            .ToArray();

    private static async Task<UnifiedRunDetails?> GetOwnedDetailsFallbackAsync(
        IUnifiedEntryRepository repository,
        Guid runId,
        string tenantId,
        string userId,
        CancellationToken cancellationToken) =>
        await GetOwnedRunFallbackAsync(repository, runId, tenantId, userId, cancellationToken)
            is null
                ? null
                : await repository.GetDetailsAsync(runId, cancellationToken);

    private static async Task<IReadOnlyList<UnifiedRunEventRecord>>
        ListOwnedEventsFallbackAsync(
            IUnifiedEntryRepository repository,
            Guid runId,
            string tenantId,
            string userId,
            CancellationToken cancellationToken) =>
        await GetOwnedRunFallbackAsync(repository, runId, tenantId, userId, cancellationToken)
            is null
                ? []
                : await repository.ListEventsAsync(runId, cancellationToken);

    private static bool Owned(
        string storedTenant,
        string storedUser,
        string tenantId,
        string userId) =>
        string.Equals(storedTenant, tenantId, StringComparison.Ordinal)
        && string.Equals(storedUser, userId, StringComparison.Ordinal);
}

public interface IUnifiedEntryRecovery
{
    Task<int> RecoverInterruptedAsync(
        DateTimeOffset recoveredAtUtc,
        CancellationToken cancellationToken = default);
}

public static class UnifiedEntryContractCloner
{
    public static UnifiedEntryAggregate Clone(UnifiedEntryAggregate value) =>
        new(
            value.Conversation,
            value.Messages,
            value.Details,
            value.Events,
            value.PersistenceRevision);

    public static UnifiedRunDetails Clone(UnifiedRunDetails value) =>
        new(value.EntryRun, value.AgentRuns, value.Orchestrations, value.ToolCalls);

    public static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) =>
        new ReadOnlyCollection<T>(values.ToArray());
}
