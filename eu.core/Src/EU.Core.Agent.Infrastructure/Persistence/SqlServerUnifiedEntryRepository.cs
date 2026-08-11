using System.Globalization;
using System.Text;
using System.Text.Json;
using EU.Core.Agent.Application.UnifiedEntry;
using Microsoft.Data.SqlClient;

namespace EU.Core.Agent.Infrastructure.Persistence;

internal sealed class SqlServerUnifiedEntryRepositoryHooks
{
    public Func<CancellationToken, Task>? BeforeWriteTransactionAsync { get; init; }

    public Func<CancellationToken, Task>? AfterDetailsRootReadAsync { get; init; }

    public Func<CancellationToken, Task>? BeforeRecoveryEventAsync { get; init; }
}

public sealed class SqlServerUnifiedEntryRepository :
    IUnifiedEntryRepository,
    IUnifiedEntryRecovery
{
    // The legacy StateSha256 column now stores only a canonical operation
    // fingerprint. It never derives or validates individual payload hashes.
private readonly string _connectionString;
    private readonly SqlServerUnifiedEntryRepositoryHooks _hooks;

    public SqlServerUnifiedEntryRepository(string connectionString)
        : this(connectionString, new SqlServerUnifiedEntryRepositoryHooks())
    {
    }

    internal SqlServerUnifiedEntryRepository(
        string connectionString,
        SqlServerUnifiedEntryRepositoryHooks hooks)
    {
        ArgumentNullException.ThrowIfNull(hooks);
        _connectionString = SqlServerAgentConnection.Validate(connectionString);
        _hooks = hooks;
    }

    public async Task<ConversationRecord?> GetConversationAsync(
        Guid Id,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Title, CreatedAtUtc, UpdatedAtUtc, TenantId, UserId
            FROM AgChatConversation
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", Format(Id));
        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? ReadConversation(reader)
            : null;
    }

    public async Task<IReadOnlyList<ConversationRecord>> ListConversationsAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Title, CreatedAtUtc, UpdatedAtUtc, TenantId, UserId
            FROM AgChatConversation
            ORDER BY UpdatedAtUtc DESC, Id
            OFFSET 0 ROWS FETCH NEXT @take ROWS ONLY;
            """;
        command.Parameters.AddWithValue("@take", Math.Clamp(take, 1, 100));
        var values = new List<ConversationRecord>();
        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(ReadConversation(reader));
        }

        return UnifiedEntryContractCloner.ReadOnly(values);
    }

    public async Task<IReadOnlyList<ConversationMessageRecord>> ListMessagesAsync(
        Guid conversationId,
        int take = UnifiedEntryReadLimits.DefaultMessageTake,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, ConversationId, Role, Content, ContentSha256,
                   ContentUtf8Bytes, CreatedAtUtc, Kind, BusinessQueryId,
                   BusinessReceiptJson, BusinessPresentationJson,
                   BusinessIntegritySha256
            FROM
            (
                SELECT Id, ConversationId, Role, Content, ContentSha256,
                       ContentUtf8Bytes, CreatedAtUtc, Kind, BusinessQueryId,
                       BusinessReceiptJson, BusinessPresentationJson,
                       BusinessIntegritySha256, Ordinal
                FROM AgChatMessage
                WHERE ConversationId = @conversationId
                ORDER BY Ordinal DESC
                OFFSET 0 ROWS FETCH NEXT @take ROWS ONLY
            ) AS recent_messages
            ORDER BY Ordinal;
            """;
        command.Parameters.AddWithValue("@conversationId", Format(conversationId));
        command.Parameters.AddWithValue(
            "@take",
            Math.Clamp(
                take,
                1,
                UnifiedEntryReadLimits.MaximumMessageTake));
        var values = new List<ConversationMessageRecord>();
        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(ReadMessage(reader));
        }

        return UnifiedEntryContractCloner.ReadOnly(values);
    }

    public async Task<UnifiedEntryRunRecord?> GetRunAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        return await ReadEntryRunAsync(
            connection,
            transaction: null,
            runId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<UnifiedEntryRunRecord>> ListRunsAsync(
        Guid conversationId,
        int take,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            $"""
            {EntryRunSelect}
            WHERE ConversationId = @conversationId
            ORDER BY StartedAtUtc DESC, Id
            OFFSET 0 ROWS FETCH NEXT @take ROWS ONLY;
            """;
        command.Parameters.AddWithValue("@conversationId", Format(conversationId));
        command.Parameters.AddWithValue("@take", Math.Clamp(take, 1, 100));
        var values = new List<UnifiedEntryRunRecord>();
        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(ReadEntryRun(reader));
        }

        return UnifiedEntryContractCloner.ReadOnly(values);
    }

    public async Task<UnifiedRunDetails?> GetDetailsAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlTransaction transaction =
            connection.BeginTransaction();
        try
        {
            UnifiedEntryRunRecord? entry = await ReadEntryRunAsync(
                connection,
                transaction,
                runId,
                cancellationToken);
            if (entry is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return null;
            }

            if (_hooks.AfterDetailsRootReadAsync is not null)
            {
                await _hooks.AfterDetailsRootReadAsync(cancellationToken);
            }

            IReadOnlyList<UnifiedAgentRunRecord> agents =
                await ReadAgentRunsAsync(connection, transaction, runId, cancellationToken);
            IReadOnlyList<UnifiedOrchestrationRunLink> orchestrations =
                await ReadOrchestrationsAsync(connection, transaction, runId, cancellationToken);
            IReadOnlyList<UnifiedToolCallRecord> tools =
                await ReadToolCallsAsync(connection, transaction, runId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new UnifiedRunDetails(entry, agents, orchestrations, tools);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<IReadOnlyList<UnifiedRunEventRecord>> ListEventsAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        return await ReadEventsAsync(
            connection,
            transaction: null,
            runId,
            cancellationToken);
    }

    public async Task<ConversationRecord?> GetConversationForOwnerAsync(
        Guid Id,
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Title, CreatedAtUtc, UpdatedAtUtc, TenantId, UserId
            FROM AgChatConversation
            WHERE Id = @Id AND TenantId = @tenantId AND UserId = @userId;
            """;
        AddOwnerParameters(command, tenantId, userId);
        command.Parameters.AddWithValue("@Id", Format(Id));
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadConversation(reader) : null;
    }

    public async Task<IReadOnlyList<ConversationRecord>> ListConversationsForOwnerAsync(
        string tenantId,
        string userId,
        int take,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Title, CreatedAtUtc, UpdatedAtUtc, TenantId, UserId
            FROM AgChatConversation
            WHERE TenantId = @tenantId AND UserId = @userId
            ORDER BY UpdatedAtUtc DESC, Id
            OFFSET 0 ROWS FETCH NEXT @take ROWS ONLY;
            """;
        AddOwnerParameters(command, tenantId, userId);
        command.Parameters.AddWithValue("@take", Math.Clamp(take, 1, 100));
        var values = new List<ConversationRecord>();
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(ReadConversation(reader));
        }

        return UnifiedEntryContractCloner.ReadOnly(values);
    }

    public async Task<BusinessQueryCleanupResult> RedactExpiredBusinessQueryResultsAsync(
        DateTimeOffset cutoffUtc,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var expired = new List<(Guid MessageId, Guid QueryId, string Receipt, string Integrity)>();
            await using (SqlCommand read = connection.CreateCommand())
            {
                read.Transaction = transaction;
                read.CommandText =
                    "SELECT Id, BusinessQueryId, BusinessReceiptJson, " +
                    "BusinessIntegritySha256 FROM AgChatMessage " +
                    "WHERE Kind = 'BusinessQueryResult' " +
                    "AND BusinessQueryId IS NOT NULL " +
                    "AND BusinessPresentationJson <> '' " +
                    "AND CreatedAtUtc < @cutoff ORDER BY Id;";
                read.Parameters.AddWithValue("@cutoff", Format(cutoffUtc));
                await using SqlDataReader reader =
                    await read.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    expired.Add((
                        Guid.Parse(reader.GetString(0)),
                        Guid.Parse(reader.GetString(1)),
                        reader.GetString(2),
                        reader.GetString(3)));
                }
            }

            int tools = 0;
            int events = 0;
            foreach ((Guid messageId, Guid queryId, string receipt, string integrity)
                     in expired)
            {
                string messageContent = BusinessQueryResultRedaction.CreateContent(
                    queryId, receipt, integrity);
                string redactedPayload = BusinessQueryResultRedaction.RedactedPayload(
                    queryId, integrity);
                await using (SqlCommand message = connection.CreateCommand())
                {
                    message.Transaction = transaction;
                    message.CommandText =
                        "UPDATE AgChatMessage SET Content = @Content, " +
                        "ContentUtf8Bytes = @bytes, BusinessPresentationJson = '' " +
                        "WHERE Id = @Id AND BusinessPresentationJson <> '';";
                    message.Parameters.AddWithValue("@Content", messageContent);
                    message.Parameters.AddWithValue(
                        "@bytes", System.Text.Encoding.UTF8.GetByteCount(messageContent));
                    message.Parameters.AddWithValue("@Id", Format(messageId));
                    await message.ExecuteNonQueryAsync(cancellationToken);
                }

                await using (SqlCommand tool = connection.CreateCommand())
                {
                    tool.Transaction = transaction;
                    tool.CommandText =
                        "UPDATE AgUnifiedToolCall SET ResultContent = @payload " +
                        "WHERE CHARINDEX(@queryId, LOWER(ResultContent)) > 0;";
                    tool.Parameters.AddWithValue("@payload", redactedPayload);
                    tool.Parameters.AddWithValue(
                        "@queryId", queryId.ToString("D").ToLowerInvariant());
                    tools += await tool.ExecuteNonQueryAsync(cancellationToken);
                }

                await using (SqlCommand runEvent = connection.CreateCommand())
                {
                    runEvent.Transaction = transaction;
                    runEvent.CommandText =
                        "UPDATE AgUnifiedRunEvent SET PayloadJson = @payload " +
                        "WHERE CHARINDEX(@queryId, LOWER(PayloadJson)) > 0;";
                    runEvent.Parameters.AddWithValue("@payload", redactedPayload);
                    runEvent.Parameters.AddWithValue(
                        "@queryId", queryId.ToString("D").ToLowerInvariant());
                    events += await runEvent.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return new BusinessQueryCleanupResult(
                expired.Count, tools, events, cutoffUtc);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<IReadOnlyList<ConversationMessageRecord>> ListMessagesForOwnerAsync(
        Guid conversationId,
        string tenantId,
        string userId,
        int take = UnifiedEntryReadLimits.DefaultMessageTake,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, ConversationId, Role, Content, ContentSha256,
                   ContentUtf8Bytes, CreatedAtUtc, Kind, BusinessQueryId,
                   BusinessReceiptJson, BusinessPresentationJson,
                   BusinessIntegritySha256
            FROM (
                SELECT m.Id, m.ConversationId, m.Role, m.Content,
                       m.ContentSha256, m.ContentUtf8Bytes, m.CreatedAtUtc,
                       m.Kind, m.BusinessQueryId, m.BusinessReceiptJson,
                       m.BusinessPresentationJson, m.BusinessIntegritySha256,
                       m.Ordinal
                FROM AgChatMessage AS m
                INNER JOIN AgChatConversation AS c ON c.Id = m.ConversationId
                WHERE m.ConversationId = @conversationId
                  AND c.TenantId = @tenantId AND c.UserId = @userId
                ORDER BY m.Ordinal DESC
                OFFSET 0 ROWS FETCH NEXT @take ROWS ONLY
            ) AS recent_messages
            ORDER BY Ordinal;
            """;
        AddOwnerParameters(command, tenantId, userId);
        command.Parameters.AddWithValue("@conversationId", Format(conversationId));
        command.Parameters.AddWithValue(
            "@take", Math.Clamp(take, 1, UnifiedEntryReadLimits.MaximumMessageTake));
        var values = new List<ConversationMessageRecord>();
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(ReadMessage(reader));
        }

        return UnifiedEntryContractCloner.ReadOnly(values);
    }

    public async Task<UnifiedEntryRunRecord?> GetRunForOwnerAsync(
        Guid runId,
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        return await ReadEntryRunForOwnerAsync(
            connection, null, runId, tenantId, userId, cancellationToken);
    }

    public async Task<IReadOnlyList<UnifiedEntryRunRecord>> ListRunsForOwnerAsync(
        Guid conversationId,
        string tenantId,
        string userId,
        int take,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            $"""
            {EntryRunSelect}
            WHERE ConversationId = @conversationId
              AND TenantId = @tenantId AND UserId = @userId
            ORDER BY StartedAtUtc DESC, Id
            OFFSET 0 ROWS FETCH NEXT @take ROWS ONLY;
            """;
        AddOwnerParameters(command, tenantId, userId);
        command.Parameters.AddWithValue("@conversationId", Format(conversationId));
        command.Parameters.AddWithValue("@take", Math.Clamp(take, 1, 100));
        var values = new List<UnifiedEntryRunRecord>();
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(ReadEntryRun(reader));
        }

        return UnifiedEntryContractCloner.ReadOnly(values);
    }

    public async Task<UnifiedRunDetails?> GetDetailsForOwnerAsync(
        Guid runId,
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlTransaction transaction = connection.BeginTransaction();
        try
        {
            UnifiedEntryRunRecord? entry = await ReadEntryRunForOwnerAsync(
                connection, transaction, runId, tenantId, userId, cancellationToken);
            if (entry is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return null;
            }

            IReadOnlyList<UnifiedAgentRunRecord> agents = await ReadAgentRunsAsync(
                connection, transaction, runId, cancellationToken);
            IReadOnlyList<UnifiedOrchestrationRunLink> orchestrations =
                await ReadOrchestrationsAsync(
                    connection, transaction, runId, cancellationToken);
            IReadOnlyList<UnifiedToolCallRecord> tools = await ReadToolCallsAsync(
                connection, transaction, runId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new UnifiedRunDetails(entry, agents, orchestrations, tools);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<IReadOnlyList<UnifiedRunEventRecord>> ListEventsForOwnerAsync(
        Guid runId,
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        if (await ReadEntryRunForOwnerAsync(
                connection, null, runId, tenantId, userId, cancellationToken) is null)
        {
            return [];
        }

        return await ReadEventsAsync(connection, null, runId, cancellationToken);
    }

    public async Task<UnifiedEntryAggregate?> GetAggregateForOwnerAsync(
        Guid runId,
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlTransaction transaction = connection.BeginTransaction();
        try
        {
            UnifiedEntryRunRecord? entry = await ReadEntryRunForOwnerAsync(
                connection,
                transaction,
                runId,
                tenantId,
                userId,
                cancellationToken);
            if (entry is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return null;
            }

            ConversationRecord conversation;
            await using (SqlCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    """
                    SELECT Id, Title, CreatedAtUtc, UpdatedAtUtc,
                           TenantId, UserId
                    FROM AgChatConversation
                    WHERE Id = @Id AND TenantId = @tenantId AND UserId = @userId;
                    """;
                command.Parameters.AddWithValue("@Id", Format(entry.ConversationId));
                AddOwnerParameters(command, tenantId, userId);
                await using SqlDataReader reader =
                    await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    await transaction.CommitAsync(cancellationToken);
                    return null;
                }

                conversation = ReadConversation(reader);
            }

            var messages = new List<ConversationMessageRecord>();
            await using (SqlCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    """
                    SELECT Id, ConversationId, Role, Content, ContentSha256,
                           ContentUtf8Bytes, CreatedAtUtc, Kind,
                           BusinessQueryId, BusinessReceiptJson,
                           BusinessPresentationJson,
                           BusinessIntegritySha256
                    FROM AgChatMessage
                    WHERE ConversationId = @conversationId
                    ORDER BY Ordinal;
                    """;
                command.Parameters.AddWithValue(
                    "@conversationId",
                    Format(entry.ConversationId));
                await using SqlDataReader reader =
                    await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    messages.Add(ReadMessage(reader));
                }
            }

            IReadOnlyList<UnifiedAgentRunRecord> agents = await ReadAgentRunsAsync(
                connection,
                transaction,
                runId,
                cancellationToken);
            IReadOnlyList<UnifiedOrchestrationRunLink> orchestrations =
                await ReadOrchestrationsAsync(
                    connection,
                    transaction,
                    runId,
                    cancellationToken);
            IReadOnlyList<UnifiedToolCallRecord> tools = await ReadToolCallsAsync(
                connection,
                transaction,
                runId,
                cancellationToken);
            IReadOnlyList<UnifiedRunEventRecord> events = await ReadEventsAsync(
                connection,
                transaction,
                runId,
                cancellationToken);
            PersistenceState? state =
                await ReadPersistenceStateAsync(
                    connection,
                    transaction,
                    runId,
                    cancellationToken);
            if (state is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return null;
            }

            await transaction.CommitAsync(cancellationToken);
            return new UnifiedEntryAggregate(
                conversation,
                messages,
                new UnifiedRunDetails(entry, agents, orchestrations, tools),
                events,
                state.Revision);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<int> RecoverInterruptedAsync(
        DateTimeOffset recoveredAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        await using SqlTransaction transaction =
            connection.BeginTransaction();
        try
        {
            IReadOnlyList<InterruptedRun> interrupted =
                await ReadInterruptedRunsAsync(
                    connection,
                    transaction,
                    cancellationToken);
            foreach (InterruptedRun run in interrupted)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TimeSpan duration = recoveredAtUtc - run.StartedAtUtc;
                if (duration < TimeSpan.Zero)
                {
                    duration = TimeSpan.Zero;
                }

                await TerminalizeInterruptedRowsAsync(
                    connection,
                    transaction,
                    run,
                    recoveredAtUtc,
                    duration,
                    cancellationToken);
                if (_hooks.BeforeRecoveryEventAsync is not null)
                {
                    await _hooks.BeforeRecoveryEventAsync(cancellationToken);
                }

                await InsertInterruptedEventAsync(
                    connection,
                    transaction,
                    run,
                    recoveredAtUtc,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return interrupted.Count;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<UnifiedEntryAggregate> SaveAsync(
        UnifiedEntryAggregate value,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        UnifiedEntryAggregate snapshot = UnifiedEntryContractCloner.Clone(value);
        Validate(snapshot);
        cancellationToken.ThrowIfCancellationRequested();
        string operationFingerprint =
            UnifiedEntryAggregateFingerprint.ComputeSha256(snapshot);

        await using SqlConnection connection = await OpenAsync(cancellationToken);
        if (_hooks.BeforeWriteTransactionAsync is not null)
        {
            await _hooks.BeforeWriteTransactionAsync(cancellationToken);
        }

        await using SqlTransaction transaction =
            connection.BeginTransaction();
        try
        {
            PersistenceState? durable =
                await ReadPersistenceStateAsync(
                connection,
                transaction,
                snapshot.Details.EntryRun.Id,
                cancellationToken);
            if (durable is not null
                && durable.Revision != snapshot.PersistenceRevision)
            {
                bool reconciled =
                    snapshot.PersistenceRevision < long.MaxValue
                    && durable.Revision == snapshot.PersistenceRevision + 1
                    && StringComparer.Ordinal.Equals(
                        durable.Fingerprint,
                        operationFingerprint);
                if (reconciled)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return snapshot.WithPersistenceRevision(
                        durable.Revision);
                }

                throw ConcurrentWriteRejected();
            }

            if (durable is null && snapshot.PersistenceRevision != 0)
            {
                throw ConcurrentWriteRejected();
            }

            long persistedEventSequence = durable?.LastEventSequence ?? 0;
            IReadOnlyList<UnifiedRunEventRecord> eventsToAppend =
                SelectEventsToAppend(
                    snapshot.Events,
                    persistedEventSequence,
                    durable?.LastEventId,
                    durable?.LastEventKind,
                    durable?.LastEventPayloadSha256);

            long nextRevision = checked(snapshot.PersistenceRevision + 1);
            await UpsertConversationAsync(
                connection,
                transaction,
                snapshot.Conversation,
                cancellationToken);
            await PersistMessagesAsync(
                connection,
                transaction,
                snapshot.Conversation.Id,
                snapshot.Messages,
                cancellationToken);

            await UpsertEntryRunAsync(
                connection,
                transaction,
                snapshot.Details.EntryRun,
                rowExists: durable is not null,
                snapshot.PersistenceRevision,
                nextRevision,
                operationFingerprint,
                cancellationToken);
            await DeleteMutableRunChildrenAsync(
                connection,
                transaction,
                snapshot.Details.EntryRun.Id,
                cancellationToken);

            for (int index = 0; index < snapshot.Details.AgentRuns.Count; index++)
            {
                await InsertAgentRunAsync(
                    connection,
                    transaction,
                    snapshot.Details.AgentRuns[index],
                    index,
                    cancellationToken);
            }

            for (int index = 0; index < snapshot.Details.Orchestrations.Count; index++)
            {
                await InsertOrchestrationAsync(
                    connection,
                    transaction,
                    snapshot.Details.Orchestrations[index],
                    index,
                    cancellationToken);
            }

            for (int index = 0; index < snapshot.Details.ToolCalls.Count; index++)
            {
                await InsertToolCallAsync(
                    connection,
                    transaction,
                    snapshot.Details.ToolCalls[index],
                    index,
                    cancellationToken);
            }

            await InsertEventsAsync(
                connection,
                transaction,
                eventsToAppend,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return snapshot.WithPersistenceRevision(nextRevision);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task<IReadOnlyList<InterruptedRun>> ReadInterruptedRunsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT Id, ConversationId, CorrelationId, StartedAtUtc,
                   COALESCE(
                       (SELECT MAX(Sequence)
                        FROM AgUnifiedRunEvent event
                        WHERE event.EntryRunId = AgUnifiedEntryRun.Id),
                       0)
            FROM AgUnifiedEntryRun
            WHERE Status IN ('Pending', 'Running')
            ORDER BY StartedAtUtc, Id;
            """;
        var values = new List<InterruptedRun>();
        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(new InterruptedRun(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                Guid.Parse(reader.GetString(2)),
                ParseDate(reader.GetString(3)),
                reader.GetInt64(4)));
        }

        return values;
    }

    private static async Task TerminalizeInterruptedRowsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        InterruptedRun run,
        DateTimeOffset recoveredAtUtc,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        const string childUpdate =
            """
            UPDATE {0}
            SET Status = 'Failed',
                FinishedAtUtc = @finishedAt,
                DurationTicks = CASE
                    WHEN DATEDIFF_BIG(
                        NANOSECOND,
                        TRY_CONVERT(datetimeoffset(7), StartedAtUtc, 127),
                        TRY_CONVERT(datetimeoffset(7), @finishedAt, 127)) < 0
                    THEN 0
                    ELSE DATEDIFF_BIG(
                        NANOSECOND,
                        TRY_CONVERT(datetimeoffset(7), StartedAtUtc, 127),
                        TRY_CONVERT(datetimeoffset(7), @finishedAt, 127)) / 100
                END,
                ErrorCode = @errorCode
            WHERE EntryRunId = @runId
              AND Status IN ('Pending', 'Running');
            """;
        foreach (string table in new[]
                 {
                     "AgUnifiedAgentRun",
                     "AgUnifiedOrchestrationLink",
                     "AgUnifiedToolCall"
                 })
        {
            await using SqlCommand child = connection.CreateCommand();
            child.Transaction = transaction;
            child.CommandText = string.Format(
                CultureInfo.InvariantCulture,
                childUpdate,
                table);
            AddRecoveryParameters(
                child,
                run.Id,
                recoveredAtUtc,
                duration);
            await child.ExecuteNonQueryAsync(cancellationToken);
        }

        await using SqlCommand root = connection.CreateCommand();
        root.Transaction = transaction;
        root.CommandText =
            """
            UPDATE AgUnifiedEntryRun
            SET Status = 'Failed',
                FinishedAtUtc = @finishedAt,
                DurationTicks = @durationTicks,
                ErrorCode = @errorCode,
                PersistenceRevision = PersistenceRevision + 1,
                StateSha256 = ''
            WHERE Id = @runId
              AND Status IN ('Pending', 'Running');

            UPDATE AgChatConversation
            SET UpdatedAtUtc = @finishedAt
            WHERE Id = @conversationId;
            """;
        AddRecoveryParameters(root, run.Id, recoveredAtUtc, duration);
        root.Parameters.AddWithValue(
            "@conversationId",
            Format(run.ConversationId));
        await root.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertInterruptedEventAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        InterruptedRun run,
        DateTimeOffset recoveredAtUtc,
        CancellationToken cancellationToken)
    {
        string payloadJson = JsonSerializer.Serialize(new
        {
            errorCode = UnifiedEntryErrorCodes.HostInterrupted,
            detail = "The Host restarted before this run reached a terminal state."
        });
        ProtectedUnifiedPayload payload =
            UnifiedEntryPayloadProtector.ProtectInternal(payloadJson);
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO AgUnifiedRunEvent
                (Id, EntryRunId, Sequence, CorrelationId, Kind,
                 OccurredAtUtc, ParentRunId, Depth, PayloadJson,
                 PayloadSha256)
            VALUES
                (@Id, @runId, @Sequence, @correlationId, 'failed',
                 @occurredAt, NULL, 0, @payloadJson, @payloadSha256);
            """;
        command.Parameters.AddWithValue("@Id", Format(Guid.NewGuid()));
        command.Parameters.AddWithValue("@runId", Format(run.Id));
        command.Parameters.AddWithValue("@Sequence", checked(run.LastSequence + 1));
        command.Parameters.AddWithValue(
            "@correlationId",
            Format(run.CorrelationId));
        command.Parameters.AddWithValue("@occurredAt", Format(recoveredAtUtc));
        command.Parameters.AddWithValue("@payloadJson", payload.Content);
        command.Parameters.AddWithValue(
            "@payloadSha256",
            payload.OriginalSha256);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddRecoveryParameters(
        SqlCommand command,
        Guid runId,
        DateTimeOffset recoveredAtUtc,
        TimeSpan duration)
    {
        command.Parameters.AddWithValue("@runId", Format(runId));
        command.Parameters.AddWithValue("@finishedAt", Format(recoveredAtUtc));
        command.Parameters.AddWithValue("@durationTicks", duration.Ticks);
        command.Parameters.AddWithValue(
            "@errorCode",
            UnifiedEntryErrorCodes.HostInterrupted);
    }

    private const string EntryRunSelect =
        """
        SELECT Id, ConversationId, CorrelationId, MainAgentVersionId,
               Status, StartedAtUtc, FinishedAtUtc, DurationTicks,
               InputText, InputSha256, OutputText, OutputSha256, ErrorCode,
               TenantId, UserId
        FROM AgUnifiedEntryRun
        """;

    private static async Task<PersistenceState?> ReadPersistenceStateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid runId,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT run.PersistenceRevision, run.StateSha256,
                   COALESCE(latest.Sequence, 0), latest.Id,
                   latest.Kind, latest.PayloadSha256
            FROM AgUnifiedEntryRun AS run
            OUTER APPLY
            (
                SELECT TOP (1) event.Sequence, event.Id,
                       event.Kind, event.PayloadSha256
                FROM AgUnifiedRunEvent AS event
                WHERE event.EntryRunId = run.Id
                ORDER BY event.Sequence DESC
            ) AS latest
            WHERE run.Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", Format(runId));
        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new PersistenceState(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.GetInt64(2),
            reader.IsDBNull(3) ? null : Guid.Parse(reader.GetString(3)),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetString(5));
    }

    internal static IReadOnlyList<UnifiedRunEventRecord> SelectEventsToAppend(
        IReadOnlyList<UnifiedRunEventRecord> events,
        long lastEventSequence,
        Guid? lastEventId,
        string? lastEventKind,
        string? lastEventPayloadSha256)
    {
        ArgumentNullException.ThrowIfNull(events);
        if (lastEventSequence == 0)
        {
            if (lastEventId is not null
                || lastEventKind is not null
                || lastEventPayloadSha256 is not null)
            {
                throw ConcurrentWriteRejected();
            }

            return events.ToArray();
        }

        if (lastEventSequence < 0
            || lastEventSequence > events.Count
            || lastEventId is not Guid persistedEventId)
        {
            throw ConcurrentWriteRejected();
        }

        UnifiedRunEventRecord expected = events[
            checked((int)lastEventSequence - 1)];
        if (expected.Sequence != lastEventSequence
            || expected.Id != persistedEventId
            || !StringComparer.Ordinal.Equals(expected.Kind, lastEventKind)
            || !StringComparer.Ordinal.Equals(
                expected.PayloadSha256,
                lastEventPayloadSha256))
        {
            throw ConcurrentWriteRejected();
        }

        return events.Skip(checked((int)lastEventSequence)).ToArray();
    }

    private static InvalidOperationException ConcurrentWriteRejected() =>
        new(
            "The unified entry aggregate revision is stale.");

    private static async Task UpsertConversationAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        ConversationRecord value,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            UPDATE AgChatConversation WITH (UPDLOCK, HOLDLOCK)
            SET Title = CASE WHEN @updatedAt >= UpdatedAtUtc THEN @Title ELSE Title END,
                UpdatedAtUtc = CASE WHEN @updatedAt >= UpdatedAtUtc THEN @updatedAt ELSE UpdatedAtUtc END
            WHERE Id = @Id
              AND CreatedAtUtc = @createdAt
              AND TenantId = @tenantId
              AND UserId = @userId;
            IF @@ROWCOUNT = 0 AND NOT EXISTS
                (SELECT 1 FROM AgChatConversation WHERE Id = @Id)
            BEGIN
                INSERT INTO AgChatConversation
                    (Id, Title, CreatedAtUtc, UpdatedAtUtc, TenantId, UserId)
                VALUES
                    (@Id, @Title, @createdAt, @updatedAt, @tenantId, @userId);
            END;
            """;
        command.Parameters.AddWithValue("@Id", Format(value.Id));
        command.Parameters.AddWithValue("@Title", value.Title);
        command.Parameters.AddWithValue("@createdAt", Format(value.CreatedAtUtc));
        command.Parameters.AddWithValue("@updatedAt", Format(value.UpdatedAtUtc));
        command.Parameters.AddWithValue("@tenantId", value.TenantId);
        command.Parameters.AddWithValue("@userId", value.UserId);
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            throw new InvalidOperationException(
                "The conversation identity conflicts with persisted data.");
        }
    }

    private static async Task PersistMessagesAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid conversationId,
        IReadOnlyList<ConversationMessageRecord> values,
        CancellationToken cancellationToken)
    {
        var persisted = new Dictionary<Guid, (ConversationMessageRecord Value, long Ordinal)>();
        long nextOrdinal = 0;
        await using (SqlCommand read = connection.CreateCommand())
        {
            read.Transaction = transaction;
            read.CommandText =
                """
                SELECT Id, ConversationId, Role, Content, ContentSha256,
                       ContentUtf8Bytes, CreatedAtUtc, Kind, BusinessQueryId,
                       BusinessReceiptJson, BusinessPresentationJson,
                       BusinessIntegritySha256, Ordinal
                FROM AgChatMessage
                WHERE ConversationId = @conversationId
                ORDER BY Ordinal;
                """;
            read.Parameters.AddWithValue("@conversationId", Format(conversationId));
            await using SqlDataReader reader =
                await read.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                ConversationMessageRecord message = ReadMessage(reader);
                long Ordinal = reader.GetInt64(12);
                persisted.Add(message.Id, (message, Ordinal));
                nextOrdinal = Math.Max(nextOrdinal, checked(Ordinal + 1));
            }
        }

        long previousOrdinal = -1;
        foreach (ConversationMessageRecord value in values)
        {
            if (persisted.TryGetValue(value.Id, out var existing))
            {
                if (existing.Value != value || existing.Ordinal <= previousOrdinal)
                {
                    throw new InvalidOperationException(
                        "The conversation message identity or supplied ordering conflicts with persisted data.");
                }

                previousOrdinal = existing.Ordinal;
                continue;
            }

            long Ordinal = nextOrdinal;
            nextOrdinal = checked(nextOrdinal + 1);
            if (Ordinal <= previousOrdinal)
            {
                throw new InvalidOperationException(
                    "The supplied conversation message order cannot be appended safely.");
            }

            await InsertMessageAsync(
                connection,
                transaction,
                value,
                Ordinal,
                cancellationToken);
            previousOrdinal = Ordinal;
        }
    }

    private static async Task InsertMessageAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        ConversationMessageRecord value,
        long Ordinal,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO AgChatMessage
                (Id, ConversationId, Ordinal, Role, Content, ContentSha256,
                 ContentUtf8Bytes, CreatedAtUtc, Kind, BusinessQueryId,
                 BusinessReceiptJson, BusinessPresentationJson,
                 BusinessIntegritySha256)
            VALUES
                (@Id, @conversationId, @Ordinal, @Role, @Content, @contentSha256,
                 @contentBytes, @createdAt, @Kind, @businessQueryId,
                 @businessReceipt, @businessPresentation, @businessIntegrity);
            """;
        command.Parameters.AddWithValue("@Id", Format(value.Id));
        command.Parameters.AddWithValue("@conversationId", Format(value.ConversationId));
        command.Parameters.AddWithValue("@Ordinal", Ordinal);
        command.Parameters.AddWithValue("@Role", value.Role.ToString());
        command.Parameters.AddWithValue("@Content", value.Content);
        command.Parameters.AddWithValue("@contentSha256", value.ContentSha256);
        command.Parameters.AddWithValue("@contentBytes", value.ContentUtf8Bytes);
        command.Parameters.AddWithValue("@createdAt", Format(value.CreatedAtUtc));
        command.Parameters.AddWithValue("@Kind", value.Kind.ToString());
        command.Parameters.AddWithValue(
            "@businessQueryId",
            value.BusinessQueryId.HasValue
                ? Format(value.BusinessQueryId.Value)
                : DBNull.Value);
        command.Parameters.AddWithValue("@businessReceipt", value.BusinessQueryReceiptJson);
        command.Parameters.AddWithValue(
            "@businessPresentation", value.BusinessQueryPresentationJson);
        command.Parameters.AddWithValue(
            "@businessIntegrity", value.BusinessQueryIntegritySha256);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpsertEntryRunAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        UnifiedEntryRunRecord value,
        bool rowExists,
        long expectedRevision,
        long nextRevision,
        string operationFingerprint,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = rowExists
            ?
                """
                UPDATE AgUnifiedEntryRun
                SET ConversationId = @conversationId,
                    CorrelationId = @correlationId,
                    MainAgentVersionId = @mainAgentVersionId,
                    Status = @Status,
                    StartedAtUtc = @startedAt,
                    FinishedAtUtc = @finishedAt,
                    DurationTicks = @durationTicks,
                    InputText = @input,
                    InputSha256 = @inputSha256,
                    OutputText = @output,
                    OutputSha256 = @outputSha256,
                    ErrorCode = @errorCode,
                    TenantId = @tenantId,
                    UserId = @userId,
                    PersistenceRevision = @nextRevision,
                    StateSha256 = @operationFingerprint
                WHERE Id = @Id
                  AND PersistenceRevision = @expectedRevision
                  AND TenantId = @tenantId
                  AND UserId = @userId;
                """
            :
                """
                INSERT INTO AgUnifiedEntryRun
                    (Id, ConversationId, CorrelationId, MainAgentVersionId,
                     Status, StartedAtUtc, FinishedAtUtc, DurationTicks,
                     InputText, InputSha256, OutputText, OutputSha256,
                     ErrorCode, TenantId, UserId, PersistenceRevision, StateSha256)
                VALUES
                    (@Id, @conversationId, @correlationId, @mainAgentVersionId,
                     @Status, @startedAt, @finishedAt, @durationTicks,
                     @input, @inputSha256, @output, @outputSha256,
                     @errorCode, @tenantId, @userId, @nextRevision, @operationFingerprint);
                """;
        AddEntryRunParameters(command, value);
        command.Parameters.AddWithValue("@expectedRevision", expectedRevision);
        command.Parameters.AddWithValue("@nextRevision", nextRevision);
        command.Parameters.AddWithValue(
            "@operationFingerprint",
            operationFingerprint);
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            throw ConcurrentWriteRejected();
        }
    }

    private static async Task DeleteMutableRunChildrenAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid runId,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            DELETE FROM AgUnifiedToolCall WHERE EntryRunId = @runId;
            DELETE FROM AgUnifiedOrchestrationLink WHERE EntryRunId = @runId;
            DELETE FROM AgUnifiedAgentRun WHERE EntryRunId = @runId;
            """;
        command.Parameters.AddWithValue("@runId", Format(runId));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertAgentRunAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        UnifiedAgentRunRecord value,
        int Ordinal,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO AgUnifiedAgentRun
                (Id, EntryRunId, Ordinal, ParentRunId, Kind, AgentId,
                 AgentVersionId, Depth, Status, StartedAtUtc, FinishedAtUtc,
                 DurationTicks, InputText, InputSha256, OutputText,
                 OutputSha256, ErrorCode)
            VALUES
                (@Id, @entryRunId, @Ordinal, @parentRunId, @Kind, @agentId,
                 @agentVersionId, @Depth, @Status, @startedAt, @finishedAt,
                 @durationTicks, @input, @inputSha256, @output,
                 @outputSha256, @errorCode);
            """;
        command.Parameters.AddWithValue("@Id", Format(value.Id));
        command.Parameters.AddWithValue("@entryRunId", Format(value.EntryRunId));
        command.Parameters.AddWithValue("@Ordinal", Ordinal);
        command.Parameters.AddWithValue("@parentRunId", Db(value.ParentRunId));
        command.Parameters.AddWithValue("@Kind", value.Kind.ToString());
        command.Parameters.AddWithValue("@agentId", Format(value.AgentId));
        command.Parameters.AddWithValue("@agentVersionId", Format(value.AgentVersionId));
        AddBranchParameters(
            command,
            value.Depth,
            value.Status,
            value.StartedAtUtc,
            value.FinishedAtUtc,
            value.Duration,
            value.Input,
            value.InputSha256,
            value.Output,
            value.OutputSha256,
            value.ErrorCode);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertOrchestrationAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        UnifiedOrchestrationRunLink value,
        int Ordinal,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO AgUnifiedOrchestrationLink
                (Id, EntryRunId, Ordinal, ParentRunId, OrchestrationRunId,
                 OrchestrationVersionId, Depth, Status, StartedAtUtc,
                 FinishedAtUtc, DurationTicks, InputText, InputSha256,
                 OutputText, OutputSha256, ErrorCode)
            VALUES
                (@Id, @entryRunId, @Ordinal, @parentRunId, @orchestrationRunId,
                 @orchestrationVersionId, @Depth, @Status, @startedAt,
                 @finishedAt, @durationTicks, @input, @inputSha256,
                 @output, @outputSha256, @errorCode);
            """;
        command.Parameters.AddWithValue("@Id", Format(value.Id));
        command.Parameters.AddWithValue("@entryRunId", Format(value.EntryRunId));
        command.Parameters.AddWithValue("@Ordinal", Ordinal);
        command.Parameters.AddWithValue("@parentRunId", Format(value.ParentRunId));
        command.Parameters.AddWithValue(
            "@orchestrationRunId",
            Format(value.OrchestrationRunId));
        command.Parameters.AddWithValue(
            "@orchestrationVersionId",
            Format(value.OrchestrationVersionId));
        AddBranchParameters(
            command,
            value.Depth,
            value.Status,
            value.StartedAtUtc,
            value.FinishedAtUtc,
            value.Duration,
            value.Input,
            value.InputSha256,
            value.Output,
            value.OutputSha256,
            value.ErrorCode);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertToolCallAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        UnifiedToolCallRecord value,
        int Ordinal,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO AgUnifiedToolCall
                (Id, EntryRunId, Ordinal, ParentRunId, ToolVersionId,
                 Depth, Status, StartedAtUtc, FinishedAtUtc, DurationTicks,
                 ArgumentsJson, ArgumentsSha256, ResultContent, ResultSha256,
                 ErrorCode)
            VALUES
                (@Id, @entryRunId, @Ordinal, @parentRunId, @toolVersionId,
                 @Depth, @Status, @startedAt, @finishedAt, @durationTicks,
                 @argumentsJson, @argumentsSha256, @resultContent, @resultSha256,
                 @errorCode);
            """;
        command.Parameters.AddWithValue("@Id", Format(value.Id));
        command.Parameters.AddWithValue("@entryRunId", Format(value.EntryRunId));
        command.Parameters.AddWithValue("@Ordinal", Ordinal);
        command.Parameters.AddWithValue("@parentRunId", Format(value.ParentRunId));
        command.Parameters.AddWithValue("@toolVersionId", Format(value.ToolVersionId));
        command.Parameters.AddWithValue("@Depth", value.Depth);
        command.Parameters.AddWithValue("@Status", value.Status.ToString());
        command.Parameters.AddWithValue("@startedAt", Format(value.StartedAtUtc));
        command.Parameters.AddWithValue("@finishedAt", Db(value.FinishedAtUtc));
        command.Parameters.AddWithValue("@durationTicks", Db(value.Duration));
        command.Parameters.AddWithValue("@argumentsJson", value.ArgumentsJson);
        command.Parameters.AddWithValue("@argumentsSha256", value.ArgumentsSha256);
        command.Parameters.AddWithValue("@resultContent", value.ResultContent);
        command.Parameters.AddWithValue("@resultSha256", value.ResultSha256);
        command.Parameters.AddWithValue("@errorCode", value.ErrorCode);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertEventsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        IReadOnlyList<UnifiedRunEventRecord> values,
        CancellationToken cancellationToken)
    {
        const int maximumEventsPerCommand = 100;
        for (int offset = 0; offset < values.Count; offset += maximumEventsPerCommand)
        {
            int count = Math.Min(maximumEventsPerCommand, values.Count - offset);
            await using SqlCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            var sql = new StringBuilder(
                "INSERT INTO AgUnifiedRunEvent " +
                "(Id, EntryRunId, Sequence, CorrelationId, Kind, " +
                "OccurredAtUtc, ParentRunId, Depth, PayloadJson, PayloadSha256) VALUES ");
            for (int index = 0; index < count; index++)
            {
                if (index > 0)
                {
                    sql.Append(',');
                }

                int parameterIndex = offset + index;
                string suffix = parameterIndex.ToString(CultureInfo.InvariantCulture);
                sql.Append("(@Id").Append(suffix)
                    .Append(",@entryRunId").Append(suffix)
                    .Append(",@Sequence").Append(suffix)
                    .Append(",@correlationId").Append(suffix)
                    .Append(",@Kind").Append(suffix)
                    .Append(",@occurredAt").Append(suffix)
                    .Append(",@parentRunId").Append(suffix)
                    .Append(",@Depth").Append(suffix)
                    .Append(",@payloadJson").Append(suffix)
                    .Append(",@payloadSha256").Append(suffix).Append(')');

                UnifiedRunEventRecord value = values[parameterIndex];
                command.Parameters.AddWithValue("@Id" + suffix, Format(value.Id));
                command.Parameters.AddWithValue(
                    "@entryRunId" + suffix,
                    Format(value.EntryRunId));
                command.Parameters.AddWithValue("@Sequence" + suffix, value.Sequence);
                command.Parameters.AddWithValue(
                    "@correlationId" + suffix,
                    Format(value.CorrelationId));
                command.Parameters.AddWithValue("@Kind" + suffix, value.Kind);
                command.Parameters.AddWithValue(
                    "@occurredAt" + suffix,
                    Format(value.OccurredAtUtc));
                command.Parameters.AddWithValue(
                    "@parentRunId" + suffix,
                    Db(value.ParentRunId));
                command.Parameters.AddWithValue("@Depth" + suffix, value.Depth);
                command.Parameters.AddWithValue(
                    "@payloadJson" + suffix,
                    value.PayloadJson);
                command.Parameters.AddWithValue(
                    "@payloadSha256" + suffix,
                    value.PayloadSha256);
            }

            command.CommandText = sql.Append(';').ToString();
            if (await command.ExecuteNonQueryAsync(cancellationToken) != count)
            {
                throw new InvalidOperationException(
                    "The unified entry event batch was not fully persisted.");
            }
        }
    }

    private static async Task<UnifiedEntryRunRecord?> ReadEntryRunAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        Guid runId,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            $"""
            {EntryRunSelect}
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", Format(runId));
        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? ReadEntryRun(reader)
            : null;
    }

    private static async Task<UnifiedEntryRunRecord?> ReadEntryRunForOwnerAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        Guid runId,
        string tenantId,
        string userId,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            $"""
            {EntryRunSelect}
            WHERE Id = @Id AND TenantId = @tenantId AND UserId = @userId;
            """;
        command.Parameters.AddWithValue("@Id", Format(runId));
        AddOwnerParameters(command, tenantId, userId);
        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? ReadEntryRun(reader)
            : null;
    }

    private static async Task<IReadOnlyList<UnifiedAgentRunRecord>> ReadAgentRunsAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        Guid runId,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT Id, EntryRunId, ParentRunId, Kind, AgentId,
                   AgentVersionId, Depth, Status, StartedAtUtc,
                   FinishedAtUtc, DurationTicks, InputText, InputSha256,
                   OutputText, OutputSha256, ErrorCode
            FROM AgUnifiedAgentRun
            WHERE EntryRunId = @runId
            ORDER BY Ordinal;
            """;
        command.Parameters.AddWithValue("@runId", Format(runId));
        var values = new List<UnifiedAgentRunRecord>();
        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(new UnifiedAgentRunRecord(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                ReadNullableGuid(reader, 2),
                ParseEnum<UnifiedAgentRunKind>(reader.GetString(3)),
                Guid.Parse(reader.GetString(4)),
                Guid.Parse(reader.GetString(5)),
                reader.GetInt32(6),
                ParseEnum<UnifiedRunStatus>(reader.GetString(7)),
                ParseDate(reader.GetString(8)),
                ReadNullableDate(reader, 9),
                ReadNullableDuration(reader, 10),
                reader.GetString(11),
                reader.GetString(12),
                reader.GetString(13),
                reader.GetString(14),
                reader.GetString(15)));
        }

        return UnifiedEntryContractCloner.ReadOnly(values);
    }

    private static async Task<IReadOnlyList<UnifiedOrchestrationRunLink>>
        ReadOrchestrationsAsync(
            SqlConnection connection,
            SqlTransaction? transaction,
            Guid runId,
            CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT Id, EntryRunId, ParentRunId, OrchestrationRunId,
                   OrchestrationVersionId, Depth, Status, StartedAtUtc,
                   FinishedAtUtc, DurationTicks, InputText, InputSha256,
                   OutputText, OutputSha256, ErrorCode
            FROM AgUnifiedOrchestrationLink
            WHERE EntryRunId = @runId
            ORDER BY Ordinal;
            """;
        command.Parameters.AddWithValue("@runId", Format(runId));
        var values = new List<UnifiedOrchestrationRunLink>();
        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(new UnifiedOrchestrationRunLink(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                Guid.Parse(reader.GetString(2)),
                Guid.Parse(reader.GetString(3)),
                Guid.Parse(reader.GetString(4)),
                reader.GetInt32(5),
                ParseEnum<UnifiedRunStatus>(reader.GetString(6)),
                ParseDate(reader.GetString(7)),
                ReadNullableDate(reader, 8),
                ReadNullableDuration(reader, 9),
                reader.GetString(10),
                reader.GetString(11),
                reader.GetString(12),
                reader.GetString(13),
                reader.GetString(14)));
        }

        return UnifiedEntryContractCloner.ReadOnly(values);
    }

    private static async Task<IReadOnlyList<UnifiedToolCallRecord>> ReadToolCallsAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        Guid runId,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT Id, EntryRunId, ParentRunId, ToolVersionId, Depth,
                   Status, StartedAtUtc, FinishedAtUtc, DurationTicks,
                   ArgumentsJson, ArgumentsSha256, ResultContent,
                   ResultSha256, ErrorCode
            FROM AgUnifiedToolCall
            WHERE EntryRunId = @runId
            ORDER BY Ordinal;
            """;
        command.Parameters.AddWithValue("@runId", Format(runId));
        var values = new List<UnifiedToolCallRecord>();
        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(new UnifiedToolCallRecord(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                Guid.Parse(reader.GetString(2)),
                Guid.Parse(reader.GetString(3)),
                reader.GetInt32(4),
                ParseEnum<UnifiedRunStatus>(reader.GetString(5)),
                ParseDate(reader.GetString(6)),
                ReadNullableDate(reader, 7),
                ReadNullableDuration(reader, 8),
                reader.GetString(9),
                reader.GetString(10),
                reader.GetString(11),
                reader.GetString(12),
                reader.GetString(13)));
        }

        return UnifiedEntryContractCloner.ReadOnly(values);
    }

    private static async Task<IReadOnlyList<UnifiedRunEventRecord>> ReadEventsAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        Guid runId,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT Id, EntryRunId, Sequence, CorrelationId, Kind,
                   OccurredAtUtc, ParentRunId, Depth, PayloadJson,
                   PayloadSha256
            FROM AgUnifiedRunEvent
            WHERE EntryRunId = @runId
            ORDER BY Sequence;
            """;
        command.Parameters.AddWithValue("@runId", Format(runId));
        var values = new List<UnifiedRunEventRecord>();
        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(new UnifiedRunEventRecord(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                reader.GetInt64(2),
                Guid.Parse(reader.GetString(3)),
                reader.GetString(4),
                ParseDate(reader.GetString(5)),
                ReadNullableGuid(reader, 6),
                reader.GetInt32(7),
                reader.GetString(8),
                reader.GetString(9)));
        }

        return UnifiedEntryContractCloner.ReadOnly(values);
    }

    private static ConversationRecord ReadConversation(SqlDataReader reader) =>
        new(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            ParseDate(reader.GetString(2)),
            ParseDate(reader.GetString(3)))
        {
            TenantId = reader.GetString(4),
            UserId = reader.GetString(5)
        };

    private static ConversationMessageRecord ReadMessage(SqlDataReader reader) =>
        new(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            ParseEnum<ConversationMessageRole>(reader.GetString(2)),
            reader.GetString(3),
            reader.GetString(4),
            Convert.ToInt32(reader.GetValue(5), CultureInfo.InvariantCulture),
            ParseDate(reader.GetString(6)))
        {
            Kind = ParseEnum<ConversationMessageKind>(reader.GetString(7)),
            BusinessQueryId = reader.IsDBNull(8)
                ? null
                : Guid.Parse(reader.GetString(8)),
            BusinessQueryReceiptJson = reader.GetString(9),
            BusinessQueryPresentationJson = reader.GetString(10),
            BusinessQueryIntegritySha256 = reader.GetString(11)
        };

    private static UnifiedEntryRunRecord ReadEntryRun(SqlDataReader reader) =>
        new(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            Guid.Parse(reader.GetString(2)),
            Guid.Parse(reader.GetString(3)),
            ParseEnum<UnifiedRunStatus>(reader.GetString(4)),
            ParseDate(reader.GetString(5)),
            ReadNullableDate(reader, 6),
            ReadNullableDuration(reader, 7),
            reader.GetString(8),
            reader.GetString(9),
            reader.GetString(10),
            reader.GetString(11),
            reader.GetString(12))
        {
            TenantId = reader.GetString(13),
            UserId = reader.GetString(14)
        };

    private static void AddEntryRunParameters(
        SqlCommand command,
        UnifiedEntryRunRecord value)
    {
        command.Parameters.AddWithValue("@Id", Format(value.Id));
        command.Parameters.AddWithValue("@conversationId", Format(value.ConversationId));
        command.Parameters.AddWithValue("@correlationId", Format(value.CorrelationId));
        command.Parameters.AddWithValue("@tenantId", value.TenantId);
        command.Parameters.AddWithValue("@userId", value.UserId);
        command.Parameters.AddWithValue(
            "@mainAgentVersionId",
            Format(value.MainAgentVersionId));
        command.Parameters.AddWithValue("@Status", value.Status.ToString());
        command.Parameters.AddWithValue("@startedAt", Format(value.StartedAtUtc));
        command.Parameters.AddWithValue("@finishedAt", Db(value.FinishedAtUtc));
        command.Parameters.AddWithValue("@durationTicks", Db(value.Duration));
        command.Parameters.AddWithValue("@input", value.Input);
        command.Parameters.AddWithValue("@inputSha256", value.InputSha256);
        command.Parameters.AddWithValue("@output", value.Output);
        command.Parameters.AddWithValue("@outputSha256", value.OutputSha256);
        command.Parameters.AddWithValue("@errorCode", value.ErrorCode);
    }

    private static void AddOwnerParameters(
        SqlCommand command,
        string tenantId,
        string userId)
    {
        command.Parameters.AddWithValue("@tenantId", tenantId);
        command.Parameters.AddWithValue("@userId", userId);
    }

    private static void AddBranchParameters(
        SqlCommand command,
        int Depth,
        UnifiedRunStatus Status,
        DateTimeOffset startedAtUtc,
        DateTimeOffset? finishedAtUtc,
        TimeSpan? duration,
        string input,
        string inputSha256,
        string output,
        string outputSha256,
        string errorCode)
    {
        command.Parameters.AddWithValue("@Depth", Depth);
        command.Parameters.AddWithValue("@Status", Status.ToString());
        command.Parameters.AddWithValue("@startedAt", Format(startedAtUtc));
        command.Parameters.AddWithValue("@finishedAt", Db(finishedAtUtc));
        command.Parameters.AddWithValue("@durationTicks", Db(duration));
        command.Parameters.AddWithValue("@input", input);
        command.Parameters.AddWithValue("@inputSha256", inputSha256);
        command.Parameters.AddWithValue("@output", output);
        command.Parameters.AddWithValue("@outputSha256", outputSha256);
        command.Parameters.AddWithValue("@errorCode", errorCode);
    }

    private static void Validate(UnifiedEntryAggregate value)
    {
        Guid runId = value.Details.EntryRun.Id;
        Guid conversationId = value.Conversation.Id;
        Guid correlationId = value.Details.EntryRun.CorrelationId;
        bool invalid =
            conversationId == Guid.Empty
            || runId == Guid.Empty
            || correlationId == Guid.Empty
            || value.Details.EntryRun.ConversationId != conversationId
            || HasDuplicateIds(value.Messages.Select(item => item.Id))
            || value.Messages.Any(item =>
                item.Id == Guid.Empty
                || item.ConversationId != conversationId
                || item.ContentUtf8Bytes < 0)
            || HasDuplicateIds(value.Details.AgentRuns.Select(item => item.Id))
            || value.Details.AgentRuns.Any(item =>
                item.Id == Guid.Empty
                || item.EntryRunId != runId
                || item.Depth < 0)
            || HasDuplicateIds(value.Details.Orchestrations.Select(item => item.Id))
            || value.Details.Orchestrations.Any(item =>
                item.Id == Guid.Empty
                || item.EntryRunId != runId
                || item.Depth < 0)
            || HasDuplicateIds(value.Details.ToolCalls.Select(item => item.Id))
            || value.Details.ToolCalls.Any(item =>
                item.Id == Guid.Empty
                || item.EntryRunId != runId
                || item.Depth < 0)
            || HasDuplicateIds(value.Events.Select(item => item.Id))
            || value.Events.Any(item =>
                item.Id == Guid.Empty
                || item.EntryRunId != runId
                || item.CorrelationId != correlationId
                || item.Depth < 0)
            || !value.Events.Select(item => item.Sequence)
                .SequenceEqual(Enumerable.Range(1, value.Events.Count).Select(index => (long)index));
        if (invalid)
        {
            throw new ArgumentException(
                "The unified entry aggregate contains invalid or mismatched identities.",
                nameof(value));
        }

        int terminalEventCount = value.Events.Count(item =>
            item.Kind is "completed" or "failed" or "cancelled");
        if (terminalEventCount > 1
            || IsTerminal(value.Details.EntryRun.Status) && terminalEventCount != 1
            || !IsTerminal(value.Details.EntryRun.Status) && terminalEventCount != 0)
        {
            throw new ArgumentException(
                "The unified entry aggregate contains an invalid terminal event history.",
                nameof(value));
        }
    }

    private static bool HasDuplicateIds(IEnumerable<Guid> ids)
    {
        Guid[] values = ids.ToArray();
        return values.Distinct().Count() != values.Length;
    }

    private static bool IsTerminal(UnifiedRunStatus Status) =>
        Status is UnifiedRunStatus.Completed
            or UnifiedRunStatus.Failed
            or UnifiedRunStatus.Cancelled
            or UnifiedRunStatus.Blocked;













    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        return await SqlServerAgentConnection.OpenAsync(_connectionString, cancellationToken);
    }

    private static string Format(Guid value) => value.ToString("D");

    private static string Format(DateTimeOffset value) =>
        value.ToString("O", CultureInfo.InvariantCulture);

    private static object Db(Guid? value) =>
        value is null ? DBNull.Value : Format(value.Value);

    private static object Db(DateTimeOffset? value) =>
        value is null ? DBNull.Value : Format(value.Value);

    private static object Db(TimeSpan? value) =>
        value is null ? DBNull.Value : value.Value.Ticks;

    private static DateTimeOffset ParseDate(string value) =>
        DateTimeOffset.Parse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);

    private static Guid? ReadNullableGuid(SqlDataReader reader, int Ordinal) =>
        reader.IsDBNull(Ordinal) ? null : Guid.Parse(reader.GetString(Ordinal));

    private static DateTimeOffset? ReadNullableDate(
        SqlDataReader reader,
        int Ordinal) =>
        reader.IsDBNull(Ordinal) ? null : ParseDate(reader.GetString(Ordinal));

    private static TimeSpan? ReadNullableDuration(
        SqlDataReader reader,
        int Ordinal) =>
        reader.IsDBNull(Ordinal)
            ? null
            : TimeSpan.FromTicks(reader.GetInt64(Ordinal));

    private sealed record InterruptedRun(
        Guid Id,
        Guid ConversationId,
        Guid CorrelationId,
        DateTimeOffset StartedAtUtc,
        long LastSequence);

    private sealed record PersistenceState(
        long Revision,
        string Fingerprint,
        long LastEventSequence,
        Guid? LastEventId,
        string? LastEventKind,
        string? LastEventPayloadSha256);

    private static T ParseEnum<T>(string value)
        where T : struct, Enum =>
        Enum.TryParse(value, ignoreCase: false, out T parsed)
            ? parsed
            : throw new InvalidDataException(
                $"The SQL Server value '{value}' is not a valid {typeof(T).Name}.");
}
