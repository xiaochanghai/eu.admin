using EU.Core.Agent.Application.UnifiedEntry;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class InMemoryUnifiedEntryRepository : IUnifiedEntryRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, ConversationRecord> _conversations = [];
    private readonly Dictionary<Guid, ConversationMessageRecord> _messages = [];
    private readonly Dictionary<Guid, List<Guid>> _messageOrder = [];
    private readonly Dictionary<Guid, long> _messageOrdinals = [];
    private readonly Dictionary<Guid, UnifiedRunDetails> _details = [];
    private readonly Dictionary<Guid, IReadOnlyList<UnifiedRunEventRecord>> _events = [];
    private readonly Dictionary<Guid, UnifiedEntryAggregate> _aggregates = [];

    public Task<ConversationRecord?> GetConversationAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(
                _conversations.TryGetValue(id, out ConversationRecord? value)
                    ? value with { }
                    : null);
        }
    }

    public Task<IReadOnlyList<ConversationRecord>> ListConversationsAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int boundedTake = Math.Clamp(take, 1, 100);
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<ConversationRecord>>(
                UnifiedEntryContractCloner.ReadOnly(
                    _conversations.Values
                        .OrderByDescending(value => value.UpdatedAtUtc)
                        .ThenBy(value => value.Id)
                        .Take(boundedTake)
                        .Select(value => value with { })));
        }
    }

    public Task<IReadOnlyList<ConversationMessageRecord>> ListMessagesAsync(
        Guid conversationId,
        int take = UnifiedEntryReadLimits.DefaultMessageTake,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int boundedTake = Math.Clamp(
            take,
            1,
            UnifiedEntryReadLimits.MaximumMessageTake);
        lock (_gate)
        {
            if (!_messageOrder.TryGetValue(
                    conversationId,
                    out List<Guid>? orderedIds))
            {
                return Task.FromResult<IReadOnlyList<ConversationMessageRecord>>(
                    []);
            }

            int start = Math.Max(0, orderedIds.Count - boundedTake);
            return Task.FromResult<IReadOnlyList<ConversationMessageRecord>>(
                UnifiedEntryContractCloner.ReadOnly(
                    orderedIds
                        .Skip(start)
                        .Select(id => _messages[id] with { })));
        }
    }

    public Task<UnifiedEntryRunRecord?> GetRunAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(
                _details.TryGetValue(runId, out UnifiedRunDetails? value)
                    ? value.EntryRun with { }
                    : null);
        }
    }

    public Task<IReadOnlyList<UnifiedEntryRunRecord>> ListRunsAsync(
        Guid conversationId,
        int take,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int boundedTake = Math.Clamp(take, 1, 100);
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<UnifiedEntryRunRecord>>(
                UnifiedEntryContractCloner.ReadOnly(
                    _details.Values
                        .Select(value => value.EntryRun)
                        .Where(value => value.ConversationId == conversationId)
                        .OrderByDescending(value => value.StartedAtUtc)
                        .ThenBy(value => value.Id)
                        .Take(boundedTake)
                        .Select(value => value with { })));
        }
    }

    public Task<UnifiedRunDetails?> GetDetailsAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(
                _details.TryGetValue(runId, out UnifiedRunDetails? value)
                    ? UnifiedEntryContractCloner.Clone(value)
                    : null);
        }
    }

    public Task<IReadOnlyList<UnifiedRunEventRecord>> ListEventsAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<UnifiedRunEventRecord>>(
                _events.TryGetValue(runId, out IReadOnlyList<UnifiedRunEventRecord>? values)
                    ? UnifiedEntryContractCloner.ReadOnly(
                        values.Select(value => value with { }))
                    : []);
        }
    }

    public Task<BusinessQueryCleanupResult> RedactExpiredBusinessQueryResultsAsync(
        DateTimeOffset cutoffUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            ConversationMessageRecord[] expired = _messages.Values
                .Where(value => value.Kind == ConversationMessageKind.BusinessQueryResult
                    && value.BusinessQueryId.HasValue
                    && value.BusinessQueryPresentationJson.Length > 0
                    && value.CreatedAtUtc < cutoffUtc)
                .ToArray();
            var queryIds = expired
                .ToDictionary(
                    value => value.BusinessQueryId!.Value,
                    value => value.BusinessQueryIntegritySha256);
            foreach (ConversationMessageRecord value in expired)
            {
                _messages[value.Id] = BusinessQueryResultRedaction.Redact(value);
            }

            int tools = 0;
            int events = 0;
            foreach ((Guid runId, UnifiedRunDetails details) in _details.ToArray())
            {
                UnifiedToolCallRecord[] projected = details.ToolCalls
                    .Select(value =>
                    {
                        Guid? queryId = MatchingQueryId(value.ResultContent, queryIds.Keys);
                        if (!queryId.HasValue)
                        {
                            return value;
                        }

                        tools++;
                        return value with
                        {
                            ResultContent = BusinessQueryResultRedaction.RedactedPayload(
                                queryId.Value, queryIds[queryId.Value])
                        };
                    })
                    .ToArray();
                _details[runId] = new UnifiedRunDetails(
                    details.EntryRun,
                    details.AgentRuns,
                    details.Orchestrations,
                    projected);
            }

            foreach ((Guid runId, IReadOnlyList<UnifiedRunEventRecord> values)
                     in _events.ToArray())
            {
                _events[runId] = UnifiedEntryContractCloner.ReadOnly(values.Select(value =>
                {
                    Guid? queryId = MatchingQueryId(value.PayloadJson, queryIds.Keys);
                    if (!queryId.HasValue)
                    {
                        return value;
                    }

                    events++;
                    return value with
                    {
                        PayloadJson = BusinessQueryResultRedaction.RedactedPayload(
                            queryId.Value, queryIds[queryId.Value])
                    };
                }));
            }

            foreach ((Guid runId, UnifiedEntryAggregate aggregate) in _aggregates.ToArray())
            {
                ConversationMessageRecord[] messages = aggregate.Messages
                    .Select(value => _messages.TryGetValue(
                        value.Id, out ConversationMessageRecord? updated)
                            ? updated
                            : value)
                    .ToArray();
                _aggregates[runId] = new UnifiedEntryAggregate(
                    aggregate.Conversation,
                    messages,
                    _details[runId],
                    _events[runId],
                    aggregate.PersistenceRevision);
            }

            return Task.FromResult(new BusinessQueryCleanupResult(
                expired.Length, tools, events, cutoffUtc));
        }
    }

    public Task<ConversationRecord?> GetConversationForOwnerAsync(
        Guid id,
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(
                _conversations.TryGetValue(id, out ConversationRecord? value)
                && Owned(value.TenantId, value.UserId, tenantId, userId)
                    ? value with { }
                    : null);
        }
    }

    public Task<IReadOnlyList<ConversationRecord>> ListConversationsForOwnerAsync(
        string tenantId,
        string userId,
        int take,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<ConversationRecord>>(
                UnifiedEntryContractCloner.ReadOnly(_conversations.Values
                    .Where(value => Owned(value.TenantId, value.UserId, tenantId, userId))
                    .OrderByDescending(value => value.UpdatedAtUtc)
                    .ThenBy(value => value.Id)
                    .Take(Math.Clamp(take, 1, 100))
                    .Select(value => value with { })));
        }
    }

    public async Task<IReadOnlyList<ConversationMessageRecord>> ListMessagesForOwnerAsync(
        Guid conversationId,
        string tenantId,
        string userId,
        int take = UnifiedEntryReadLimits.DefaultMessageTake,
        CancellationToken cancellationToken = default) =>
        await GetConversationForOwnerAsync(
            conversationId, tenantId, userId, cancellationToken) is null
                ? []
                : await ListMessagesAsync(conversationId, take, cancellationToken);

    public Task<UnifiedEntryRunRecord?> GetRunForOwnerAsync(
        Guid runId,
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(
                _details.TryGetValue(runId, out UnifiedRunDetails? value)
                && Owned(
                    value.EntryRun.TenantId,
                    value.EntryRun.UserId,
                    tenantId,
                    userId)
                    ? value.EntryRun with { }
                    : null);
        }
    }

    public Task<IReadOnlyList<UnifiedEntryRunRecord>> ListRunsForOwnerAsync(
        Guid conversationId,
        string tenantId,
        string userId,
        int take,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<UnifiedEntryRunRecord>>(
                UnifiedEntryContractCloner.ReadOnly(_details.Values
                    .Select(value => value.EntryRun)
                    .Where(value => value.ConversationId == conversationId
                        && Owned(value.TenantId, value.UserId, tenantId, userId))
                    .OrderByDescending(value => value.StartedAtUtc)
                    .ThenBy(value => value.Id)
                    .Take(Math.Clamp(take, 1, 100))
                    .Select(value => value with { })));
        }
    }

    public Task<UnifiedRunDetails?> GetDetailsForOwnerAsync(
        Guid runId,
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(
                _details.TryGetValue(runId, out UnifiedRunDetails? value)
                && Owned(
                    value.EntryRun.TenantId,
                    value.EntryRun.UserId,
                    tenantId,
                    userId)
                    ? UnifiedEntryContractCloner.Clone(value)
                    : null);
        }
    }

    public async Task<IReadOnlyList<UnifiedRunEventRecord>> ListEventsForOwnerAsync(
        Guid runId,
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default) =>
        await GetRunForOwnerAsync(runId, tenantId, userId, cancellationToken) is null
            ? []
            : await ListEventsAsync(runId, cancellationToken);

    public Task<UnifiedEntryAggregate?> GetAggregateForOwnerAsync(
        Guid runId,
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            UnifiedEntryAggregate? value = _aggregates.GetValueOrDefault(runId);
            return Task.FromResult(
                value is not null
                && Owned(
                    value.Details.EntryRun.TenantId,
                    value.Details.EntryRun.UserId,
                    tenantId,
                    userId)
                    ? UnifiedEntryContractCloner.Clone(value)
                    : null);
        }
    }

    public Task<UnifiedEntryAggregate> SaveAsync(
        UnifiedEntryAggregate value,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();
        UnifiedEntryAggregate copy = UnifiedEntryContractCloner.Clone(value);
        Validate(copy);
        lock (_gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guid runId = copy.Details.EntryRun.Id;
            if (_aggregates.TryGetValue(runId, out UnifiedEntryAggregate? durable))
            {
                if (copy.PersistenceRevision != durable.PersistenceRevision)
                {
                    bool immediatelyFollowing =
                        copy.PersistenceRevision < long.MaxValue
                        && durable.PersistenceRevision
                            == copy.PersistenceRevision + 1;
                    if (immediatelyFollowing
                        && StringComparer.Ordinal.Equals(
                            UnifiedEntryAggregateFingerprint.ComputeSha256(durable),
                            UnifiedEntryAggregateFingerprint.ComputeSha256(copy)))
                    {
                        return Task.FromResult(
                            UnifiedEntryContractCloner.Clone(durable));
                    }

                    throw new InvalidOperationException(
                        "The unified entry aggregate persistence revision is stale.");
                }
            }
            else if (copy.PersistenceRevision != 0)
            {
                throw new InvalidOperationException(
                    "The unified entry aggregate persistence revision is stale.");
            }

            UnifiedEntryAggregate saved = copy.WithPersistenceRevision(
                checked(copy.PersistenceRevision + 1));
            PersistMessagesLocked(
                saved.Conversation.Id,
                saved.Messages);
            _conversations[saved.Conversation.Id] = saved.Conversation with { };
            _details[runId] = UnifiedEntryContractCloner.Clone(saved.Details);
            _events[runId] = UnifiedEntryContractCloner.ReadOnly(
                saved.Events.Select(item => item with { }));
            _aggregates[runId] = UnifiedEntryContractCloner.Clone(saved);
            return Task.FromResult(UnifiedEntryContractCloner.Clone(saved));
        }
    }

    private static bool Owned(
        string storedTenant,
        string storedUser,
        string tenantId,
        string userId) =>
        string.Equals(storedTenant, tenantId, StringComparison.Ordinal)
        && string.Equals(storedUser, userId, StringComparison.Ordinal);

    private static Guid? MatchingQueryId(
        string content,
        IEnumerable<Guid> queryIds) =>
        queryIds.FirstOrDefault(value => content.Contains(
            value.ToString("D"), StringComparison.OrdinalIgnoreCase)) is Guid match
            && match != Guid.Empty
                ? match
                : null;

    private void PersistMessagesLocked(
        Guid conversationId,
        IReadOnlyList<ConversationMessageRecord> values)
    {
        _messageOrder.TryGetValue(
            conversationId,
            out List<Guid>? existingOrder);
        long nextOrdinal = existingOrder?.Count ?? 0;
        long previousOrdinal = -1;
        var additions =
            new List<(ConversationMessageRecord Message, long Ordinal)>();

        foreach (ConversationMessageRecord value in values)
        {
            long ordinal;
            if (_messages.TryGetValue(
                    value.Id,
                    out ConversationMessageRecord? existing))
            {
                if (existing != value
                    || !_messageOrdinals.TryGetValue(value.Id, out ordinal)
                    || ordinal <= previousOrdinal)
                {
                    throw new InvalidOperationException(
                        "The conversation message identity or supplied ordering conflicts with persisted data.");
                }
            }
            else
            {
                ordinal = nextOrdinal;
                nextOrdinal = checked(nextOrdinal + 1);
                if (ordinal <= previousOrdinal)
                {
                    throw new InvalidOperationException(
                        "The supplied conversation message order cannot be appended safely.");
                }

                additions.Add((value with { }, ordinal));
            }

            previousOrdinal = ordinal;
        }

        if (additions.Count == 0)
        {
            return;
        }

        existingOrder ??= [];
        foreach ((ConversationMessageRecord message, long ordinal) in additions)
        {
            _messages.Add(message.Id, message);
            _messageOrdinals.Add(message.Id, ordinal);
            existingOrder.Add(message.Id);
        }

        _messageOrder[conversationId] = existingOrder;
    }

    private static void Validate(UnifiedEntryAggregate value)
    {
        if (value.Conversation.Id == Guid.Empty
            || value.Details.EntryRun.Id == Guid.Empty
            || value.Details.EntryRun.ConversationId != value.Conversation.Id
            || value.Messages.Any(message =>
                message.Id == Guid.Empty
                || message.ConversationId != value.Conversation.Id)
            || value.Messages.Select(message => message.Id).Distinct().Count()
                != value.Messages.Count
            || value.Events.Any(runEvent =>
                runEvent.Id == Guid.Empty
                || runEvent.EntryRunId != value.Details.EntryRun.Id)
            || value.Events.Select(runEvent => runEvent.Id).Distinct().Count()
                != value.Events.Count)
        {
            throw new ArgumentException(
                "The unified entry aggregate contains mismatched identities.",
                nameof(value));
        }
    }
}
