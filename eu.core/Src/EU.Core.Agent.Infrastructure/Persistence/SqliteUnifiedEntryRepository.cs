using System.Globalization;
using System.Text.Json;
using EU.Core.Agent.Application.UnifiedEntry;
using Microsoft.Data.Sqlite;

namespace EU.Core.Agent.Infrastructure.Persistence;

internal sealed class SqliteUnifiedEntryRepositoryHooks
{
    public Func<CancellationToken, Task>? BeforeWriteTransactionAsync { get; init; }

    public Func<CancellationToken, Task>? AfterDetailsRootReadAsync { get; init; }

    public Func<CancellationToken, Task>? BeforeRecoveryEventAsync { get; init; }
}

public sealed class SqliteUnifiedEntryRepository :
    IUnifiedEntryRepository,
    IUnifiedEntryRecovery
{
    // The legacy state_sha256 column now stores only a canonical operation
    // fingerprint. It never derives or validates individual payload hashes.
    private const string CreateSchemaSql =
        """
        CREATE TABLE IF NOT EXISTS chat_conversations
        (
            id             TEXT NOT NULL PRIMARY KEY,
            title          TEXT NOT NULL,
            created_at_utc TEXT NOT NULL,
            updated_at_utc TEXT NOT NULL,
            tenant_id      TEXT NOT NULL DEFAULT '__legacy_unowned__',
            user_id        TEXT NOT NULL DEFAULT '__legacy_unowned__'
        ) WITHOUT ROWID;

        CREATE TABLE IF NOT EXISTS chat_messages
        (
            id                 TEXT    NOT NULL PRIMARY KEY,
            conversation_id    TEXT    NOT NULL,
            ordinal            INTEGER NOT NULL CHECK (ordinal >= 0),
            role               TEXT    NOT NULL,
            content            TEXT    NOT NULL,
            content_sha256     TEXT    NOT NULL,
            content_utf8_bytes INTEGER NOT NULL CHECK (content_utf8_bytes >= 0),
            created_at_utc     TEXT    NOT NULL,
            kind               TEXT    NOT NULL DEFAULT 'Legacy',
            business_query_id  TEXT,
            business_receipt_json TEXT NOT NULL DEFAULT '',
            business_presentation_json TEXT NOT NULL DEFAULT '',
            business_integrity_sha256 TEXT NOT NULL DEFAULT '',
            UNIQUE (conversation_id, ordinal),
            FOREIGN KEY (conversation_id) REFERENCES chat_conversations(id)
        ) WITHOUT ROWID;

        CREATE TABLE IF NOT EXISTS unified_entry_runs
        (
            id                    TEXT    NOT NULL PRIMARY KEY,
            conversation_id       TEXT    NOT NULL,
            correlation_id        TEXT    NOT NULL,
            main_agent_version_id TEXT    NOT NULL,
            status                TEXT    NOT NULL,
            started_at_utc        TEXT    NOT NULL,
            finished_at_utc       TEXT,
            duration_ticks        INTEGER,
            input_text            TEXT    NOT NULL,
            input_sha256          TEXT    NOT NULL,
            output_text           TEXT    NOT NULL,
            output_sha256         TEXT    NOT NULL,
            error_code            TEXT    NOT NULL,
            tenant_id             TEXT    NOT NULL DEFAULT '__legacy_unowned__',
            user_id               TEXT    NOT NULL DEFAULT '__legacy_unowned__',
            persistence_revision  INTEGER NOT NULL CHECK (persistence_revision >= 0),
            state_sha256          TEXT    NOT NULL DEFAULT '',
            FOREIGN KEY (conversation_id) REFERENCES chat_conversations(id)
        ) WITHOUT ROWID;

        CREATE TABLE IF NOT EXISTS unified_agent_runs
        (
            id               TEXT    NOT NULL PRIMARY KEY,
            entry_run_id     TEXT    NOT NULL,
            ordinal          INTEGER NOT NULL CHECK (ordinal >= 0),
            parent_run_id    TEXT,
            kind             TEXT    NOT NULL,
            agent_id         TEXT    NOT NULL,
            agent_version_id TEXT    NOT NULL,
            depth            INTEGER NOT NULL CHECK (depth >= 0),
            status           TEXT    NOT NULL,
            started_at_utc   TEXT    NOT NULL,
            finished_at_utc  TEXT,
            duration_ticks   INTEGER,
            input_text       TEXT    NOT NULL,
            input_sha256     TEXT    NOT NULL,
            output_text      TEXT    NOT NULL,
            output_sha256    TEXT    NOT NULL,
            error_code       TEXT    NOT NULL,
            UNIQUE (entry_run_id, ordinal),
            FOREIGN KEY (entry_run_id) REFERENCES unified_entry_runs(id) ON DELETE CASCADE
        ) WITHOUT ROWID;

        CREATE TABLE IF NOT EXISTS unified_orchestration_links
        (
            id                       TEXT    NOT NULL PRIMARY KEY,
            entry_run_id             TEXT    NOT NULL,
            ordinal                  INTEGER NOT NULL CHECK (ordinal >= 0),
            parent_run_id            TEXT    NOT NULL,
            orchestration_run_id     TEXT    NOT NULL,
            orchestration_version_id TEXT    NOT NULL,
            depth                    INTEGER NOT NULL CHECK (depth >= 0),
            status                   TEXT    NOT NULL,
            started_at_utc           TEXT    NOT NULL,
            finished_at_utc          TEXT,
            duration_ticks           INTEGER,
            input_text               TEXT    NOT NULL,
            input_sha256             TEXT    NOT NULL,
            output_text              TEXT    NOT NULL,
            output_sha256            TEXT    NOT NULL,
            error_code               TEXT    NOT NULL,
            UNIQUE (entry_run_id, ordinal),
            FOREIGN KEY (entry_run_id) REFERENCES unified_entry_runs(id) ON DELETE CASCADE
        ) WITHOUT ROWID;

        CREATE TABLE IF NOT EXISTS unified_tool_calls
        (
            id                 TEXT    NOT NULL PRIMARY KEY,
            entry_run_id       TEXT    NOT NULL,
            ordinal            INTEGER NOT NULL CHECK (ordinal >= 0),
            parent_run_id      TEXT    NOT NULL,
            tool_version_id    TEXT    NOT NULL,
            depth              INTEGER NOT NULL CHECK (depth >= 0),
            status             TEXT    NOT NULL,
            started_at_utc     TEXT    NOT NULL,
            finished_at_utc    TEXT,
            duration_ticks     INTEGER,
            arguments_json     TEXT    NOT NULL,
            arguments_sha256   TEXT    NOT NULL,
            result_content     TEXT    NOT NULL,
            result_sha256      TEXT    NOT NULL,
            error_code         TEXT    NOT NULL,
            UNIQUE (entry_run_id, ordinal),
            FOREIGN KEY (entry_run_id) REFERENCES unified_entry_runs(id) ON DELETE CASCADE
        ) WITHOUT ROWID;

        CREATE TABLE IF NOT EXISTS unified_run_events
        (
            id               TEXT    NOT NULL PRIMARY KEY,
            entry_run_id     TEXT    NOT NULL,
            sequence         INTEGER NOT NULL CHECK (sequence > 0),
            correlation_id   TEXT    NOT NULL,
            kind             TEXT    NOT NULL,
            occurred_at_utc  TEXT    NOT NULL,
            parent_run_id    TEXT,
            depth            INTEGER NOT NULL CHECK (depth >= 0),
            payload_json     TEXT    NOT NULL,
            payload_sha256   TEXT    NOT NULL,
            UNIQUE (entry_run_id, sequence),
            FOREIGN KEY (entry_run_id) REFERENCES unified_entry_runs(id) ON DELETE CASCADE
        ) WITHOUT ROWID;

        CREATE INDEX IF NOT EXISTS ix_chat_conversations_updated
            ON chat_conversations (updated_at_utc DESC, id);
        CREATE INDEX IF NOT EXISTS ix_unified_entry_runs_conversation_started
            ON unified_entry_runs (conversation_id, started_at_utc DESC, id);
        """;

    private const string CreateRevisionValidationSql =
        """
        CREATE TRIGGER IF NOT EXISTS unified_entry_revision_nonnegative_insert
        BEFORE INSERT ON unified_entry_runs
        WHEN NEW.persistence_revision < 0
        BEGIN
            SELECT RAISE(ABORT, 'persistence_revision must be nonnegative');
        END;

        CREATE TRIGGER IF NOT EXISTS unified_entry_revision_nonnegative_update
        BEFORE UPDATE OF persistence_revision ON unified_entry_runs
        WHEN NEW.persistence_revision < 0
        BEGIN
            SELECT RAISE(ABORT, 'persistence_revision must be nonnegative');
        END;
        """;

    private readonly string _connectionString;
    private readonly SqliteUnifiedEntryRepositoryHooks _hooks;

    public SqliteUnifiedEntryRepository(string databasePath)
        : this(databasePath, new SqliteUnifiedEntryRepositoryHooks())
    {
    }

    internal SqliteUnifiedEntryRepository(
        string databasePath,
        SqliteUnifiedEntryRepositoryHooks hooks)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentNullException.ThrowIfNull(hooks);
        string fullPath = Path.GetFullPath(databasePath);
        string? directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException(
                "The SQLite database path must have a parent directory.",
                nameof(databasePath));
        }

        Directory.CreateDirectory(directory);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Private,
            Pooling = false,
            DefaultTimeout = 5
        }.ToString();
        _hooks = hooks;
        EnsureCreated();
    }

    public async Task<ConversationRecord?> GetConversationAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, title, created_at_utc, updated_at_utc, tenant_id, user_id
            FROM chat_conversations
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", Format(id));
        await using SqliteDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? ReadConversation(reader)
            : null;
    }

    public async Task<IReadOnlyList<ConversationRecord>> ListConversationsAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, title, created_at_utc, updated_at_utc, tenant_id, user_id
            FROM chat_conversations
            ORDER BY updated_at_utc DESC, id
            LIMIT $take;
            """;
        command.Parameters.AddWithValue("$take", Math.Clamp(take, 1, 100));
        var values = new List<ConversationRecord>();
        await using SqliteDataReader reader =
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
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, conversation_id, role, content, content_sha256,
                   content_utf8_bytes, created_at_utc, kind, business_query_id,
                   business_receipt_json, business_presentation_json,
                   business_integrity_sha256
            FROM
            (
                SELECT id, conversation_id, role, content, content_sha256,
                       content_utf8_bytes, created_at_utc, kind, business_query_id,
                       business_receipt_json, business_presentation_json,
                       business_integrity_sha256, ordinal
                FROM chat_messages
                WHERE conversation_id = $conversationId
                ORDER BY ordinal DESC
                LIMIT $take
            )
            ORDER BY ordinal;
            """;
        command.Parameters.AddWithValue("$conversationId", Format(conversationId));
        command.Parameters.AddWithValue(
            "$take",
            Math.Clamp(
                take,
                1,
                UnifiedEntryReadLimits.MaximumMessageTake));
        var values = new List<ConversationMessageRecord>();
        await using SqliteDataReader reader =
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
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
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
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            $"""
            {EntryRunSelect}
            WHERE conversation_id = $conversationId
            ORDER BY started_at_utc DESC, id
            LIMIT $take;
            """;
        command.Parameters.AddWithValue("$conversationId", Format(conversationId));
        command.Parameters.AddWithValue("$take", Math.Clamp(take, 1, 100));
        var values = new List<UnifiedEntryRunRecord>();
        await using SqliteDataReader reader =
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
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteTransaction transaction =
            connection.BeginTransaction(deferred: true);
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
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        return await ReadEventsAsync(
            connection,
            transaction: null,
            runId,
            cancellationToken);
    }

    public async Task<ConversationRecord?> GetConversationForOwnerAsync(
        Guid id,
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, title, created_at_utc, updated_at_utc, tenant_id, user_id
            FROM chat_conversations
            WHERE id = $id AND tenant_id = $tenantId AND user_id = $userId;
            """;
        AddOwnerParameters(command, tenantId, userId);
        command.Parameters.AddWithValue("$id", Format(id));
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadConversation(reader) : null;
    }

    public async Task<IReadOnlyList<ConversationRecord>> ListConversationsForOwnerAsync(
        string tenantId,
        string userId,
        int take,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, title, created_at_utc, updated_at_utc, tenant_id, user_id
            FROM chat_conversations
            WHERE tenant_id = $tenantId AND user_id = $userId
            ORDER BY updated_at_utc DESC, id
            LIMIT $take;
            """;
        AddOwnerParameters(command, tenantId, userId);
        command.Parameters.AddWithValue("$take", Math.Clamp(take, 1, 100));
        var values = new List<ConversationRecord>();
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
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
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteTransaction transaction =
            (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var expired = new List<(Guid MessageId, Guid QueryId, string Receipt, string Integrity)>();
            await using (SqliteCommand read = connection.CreateCommand())
            {
                read.Transaction = transaction;
                read.CommandText =
                    "SELECT id, business_query_id, business_receipt_json, " +
                    "business_integrity_sha256 FROM chat_messages " +
                    "WHERE kind = 'BusinessQueryResult' " +
                    "AND business_query_id IS NOT NULL " +
                    "AND business_presentation_json <> '' " +
                    "AND created_at_utc < $cutoff ORDER BY id;";
                read.Parameters.AddWithValue("$cutoff", Format(cutoffUtc));
                await using SqliteDataReader reader =
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
                await using (SqliteCommand message = connection.CreateCommand())
                {
                    message.Transaction = transaction;
                    message.CommandText =
                        "UPDATE chat_messages SET content = $content, " +
                        "content_utf8_bytes = $bytes, business_presentation_json = '' " +
                        "WHERE id = $id AND business_presentation_json <> '';";
                    message.Parameters.AddWithValue("$content", messageContent);
                    message.Parameters.AddWithValue(
                        "$bytes", System.Text.Encoding.UTF8.GetByteCount(messageContent));
                    message.Parameters.AddWithValue("$id", Format(messageId));
                    await message.ExecuteNonQueryAsync(cancellationToken);
                }

                await using (SqliteCommand tool = connection.CreateCommand())
                {
                    tool.Transaction = transaction;
                    tool.CommandText =
                        "UPDATE unified_tool_calls SET result_content = $payload " +
                        "WHERE instr(lower(result_content), $queryId) > 0;";
                    tool.Parameters.AddWithValue("$payload", redactedPayload);
                    tool.Parameters.AddWithValue(
                        "$queryId", queryId.ToString("D").ToLowerInvariant());
                    tools += await tool.ExecuteNonQueryAsync(cancellationToken);
                }

                await using (SqliteCommand runEvent = connection.CreateCommand())
                {
                    runEvent.Transaction = transaction;
                    runEvent.CommandText =
                        "UPDATE unified_run_events SET payload_json = $payload " +
                        "WHERE instr(lower(payload_json), $queryId) > 0;";
                    runEvent.Parameters.AddWithValue("$payload", redactedPayload);
                    runEvent.Parameters.AddWithValue(
                        "$queryId", queryId.ToString("D").ToLowerInvariant());
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
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, conversation_id, role, content, content_sha256,
                   content_utf8_bytes, created_at_utc, kind, business_query_id,
                   business_receipt_json, business_presentation_json,
                   business_integrity_sha256
            FROM (
                SELECT m.id, m.conversation_id, m.role, m.content,
                       m.content_sha256, m.content_utf8_bytes, m.created_at_utc,
                       m.kind, m.business_query_id, m.business_receipt_json,
                       m.business_presentation_json, m.business_integrity_sha256,
                       m.ordinal
                FROM chat_messages AS m
                INNER JOIN chat_conversations AS c ON c.id = m.conversation_id
                WHERE m.conversation_id = $conversationId
                  AND c.tenant_id = $tenantId AND c.user_id = $userId
                ORDER BY m.ordinal DESC
                LIMIT $take
            )
            ORDER BY ordinal;
            """;
        AddOwnerParameters(command, tenantId, userId);
        command.Parameters.AddWithValue("$conversationId", Format(conversationId));
        command.Parameters.AddWithValue(
            "$take", Math.Clamp(take, 1, UnifiedEntryReadLimits.MaximumMessageTake));
        var values = new List<ConversationMessageRecord>();
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
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
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
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
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            $"""
            {EntryRunSelect}
            WHERE conversation_id = $conversationId
              AND tenant_id = $tenantId AND user_id = $userId
            ORDER BY started_at_utc DESC, id
            LIMIT $take;
            """;
        AddOwnerParameters(command, tenantId, userId);
        command.Parameters.AddWithValue("$conversationId", Format(conversationId));
        command.Parameters.AddWithValue("$take", Math.Clamp(take, 1, 100));
        var values = new List<UnifiedEntryRunRecord>();
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
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
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteTransaction transaction = connection.BeginTransaction(deferred: true);
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
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
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
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteTransaction transaction = connection.BeginTransaction(deferred: true);
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
            await using (SqliteCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    """
                    SELECT id, title, created_at_utc, updated_at_utc,
                           tenant_id, user_id
                    FROM chat_conversations
                    WHERE id = $id AND tenant_id = $tenantId AND user_id = $userId;
                    """;
                command.Parameters.AddWithValue("$id", Format(entry.ConversationId));
                AddOwnerParameters(command, tenantId, userId);
                await using SqliteDataReader reader =
                    await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    await transaction.CommitAsync(cancellationToken);
                    return null;
                }

                conversation = ReadConversation(reader);
            }

            var messages = new List<ConversationMessageRecord>();
            await using (SqliteCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    """
                    SELECT id, conversation_id, role, content, content_sha256,
                           content_utf8_bytes, created_at_utc, kind,
                           business_query_id, business_receipt_json,
                           business_presentation_json,
                           business_integrity_sha256
                    FROM chat_messages
                    WHERE conversation_id = $conversationId
                    ORDER BY ordinal;
                    """;
                command.Parameters.AddWithValue(
                    "$conversationId",
                    Format(entry.ConversationId));
                await using SqliteDataReader reader =
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
            (long Revision, string Fingerprint)? state =
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
                state.Value.Revision);
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
        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        await using SqliteTransaction transaction =
            connection.BeginTransaction(deferred: false);
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

        await using SqliteConnection connection = await OpenAsync(cancellationToken);
        if (_hooks.BeforeWriteTransactionAsync is not null)
        {
            await _hooks.BeforeWriteTransactionAsync(cancellationToken);
        }

        await using SqliteTransaction transaction =
            connection.BeginTransaction(deferred: false);
        try
        {
            (long Revision, string Fingerprint)? durable =
                await ReadPersistenceStateAsync(
                connection,
                transaction,
                snapshot.Details.EntryRun.Id,
                cancellationToken);
            if (durable.HasValue
                && durable.Value.Revision != snapshot.PersistenceRevision)
            {
                bool reconciled =
                    snapshot.PersistenceRevision < long.MaxValue
                    && durable.Value.Revision == snapshot.PersistenceRevision + 1
                    && StringComparer.Ordinal.Equals(
                        durable.Value.Fingerprint,
                        operationFingerprint);
                if (reconciled)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return snapshot.WithPersistenceRevision(
                        durable.Value.Revision);
                }

                throw ConcurrentWriteRejected();
            }

            if (!durable.HasValue && snapshot.PersistenceRevision != 0)
            {
                throw ConcurrentWriteRejected();
            }

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
                rowExists: durable.HasValue,
                snapshot.PersistenceRevision,
                nextRevision,
                operationFingerprint,
                cancellationToken);
            await DeleteRunChildrenAsync(
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

            foreach (UnifiedRunEventRecord runEvent in snapshot.Events)
            {
                await InsertEventAsync(
                    connection,
                    transaction,
                    runEvent,
                    cancellationToken);
            }

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
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT id, conversation_id, correlation_id, started_at_utc,
                   COALESCE(
                       (SELECT MAX(sequence)
                        FROM unified_run_events event
                        WHERE event.entry_run_id = unified_entry_runs.id),
                       0)
            FROM unified_entry_runs
            WHERE status IN ('Pending', 'Running')
            ORDER BY started_at_utc, id;
            """;
        var values = new List<InterruptedRun>();
        await using SqliteDataReader reader =
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
        SqliteConnection connection,
        SqliteTransaction transaction,
        InterruptedRun run,
        DateTimeOffset recoveredAtUtc,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        const string childUpdate =
            """
            UPDATE {0}
            SET status = 'Failed',
                finished_at_utc = $finishedAt,
                duration_ticks = MAX(
                    0,
                    CAST(
                        (julianday($finishedAt) - julianday(started_at_utc))
                        * 864000000000
                        AS INTEGER)),
                error_code = $errorCode
            WHERE entry_run_id = $runId
              AND status IN ('Pending', 'Running');
            """;
        foreach (string table in new[]
                 {
                     "unified_agent_runs",
                     "unified_orchestration_links",
                     "unified_tool_calls"
                 })
        {
            await using SqliteCommand child = connection.CreateCommand();
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

        await using SqliteCommand root = connection.CreateCommand();
        root.Transaction = transaction;
        root.CommandText =
            """
            UPDATE unified_entry_runs
            SET status = 'Failed',
                finished_at_utc = $finishedAt,
                duration_ticks = $durationTicks,
                error_code = $errorCode,
                persistence_revision = persistence_revision + 1,
                state_sha256 = ''
            WHERE id = $runId
              AND status IN ('Pending', 'Running');

            UPDATE chat_conversations
            SET updated_at_utc = $finishedAt
            WHERE id = $conversationId;
            """;
        AddRecoveryParameters(root, run.Id, recoveredAtUtc, duration);
        root.Parameters.AddWithValue(
            "$conversationId",
            Format(run.ConversationId));
        await root.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertInterruptedEventAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
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
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO unified_run_events
                (id, entry_run_id, sequence, correlation_id, kind,
                 occurred_at_utc, parent_run_id, depth, payload_json,
                 payload_sha256)
            VALUES
                ($id, $runId, $sequence, $correlationId, 'failed',
                 $occurredAt, NULL, 0, $payloadJson, $payloadSha256);
            """;
        command.Parameters.AddWithValue("$id", Format(Guid.NewGuid()));
        command.Parameters.AddWithValue("$runId", Format(run.Id));
        command.Parameters.AddWithValue("$sequence", checked(run.LastSequence + 1));
        command.Parameters.AddWithValue(
            "$correlationId",
            Format(run.CorrelationId));
        command.Parameters.AddWithValue("$occurredAt", Format(recoveredAtUtc));
        command.Parameters.AddWithValue("$payloadJson", payload.Content);
        command.Parameters.AddWithValue(
            "$payloadSha256",
            payload.OriginalSha256);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddRecoveryParameters(
        SqliteCommand command,
        Guid runId,
        DateTimeOffset recoveredAtUtc,
        TimeSpan duration)
    {
        command.Parameters.AddWithValue("$runId", Format(runId));
        command.Parameters.AddWithValue("$finishedAt", Format(recoveredAtUtc));
        command.Parameters.AddWithValue("$durationTicks", duration.Ticks);
        command.Parameters.AddWithValue(
            "$errorCode",
            UnifiedEntryErrorCodes.HostInterrupted);
    }

    private const string EntryRunSelect =
        """
        SELECT id, conversation_id, correlation_id, main_agent_version_id,
               status, started_at_utc, finished_at_utc, duration_ticks,
               input_text, input_sha256, output_text, output_sha256, error_code,
               tenant_id, user_id
        FROM unified_entry_runs
        """;

    private static async Task<(long Revision, string Fingerprint)?> ReadPersistenceStateAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid runId,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT persistence_revision, state_sha256
            FROM unified_entry_runs
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", Format(runId));
        await using SqliteDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return (reader.GetInt64(0), reader.GetString(1));
    }

    private static InvalidOperationException ConcurrentWriteRejected() =>
        new(
            "The unified entry aggregate revision is stale.");

    private static async Task UpsertConversationAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        ConversationRecord value,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO chat_conversations
                (id, title, created_at_utc, updated_at_utc, tenant_id, user_id)
            VALUES
                ($id, $title, $createdAt, $updatedAt, $tenantId, $userId)
            ON CONFLICT(id) DO UPDATE SET
                title = CASE
                    WHEN excluded.updated_at_utc >= chat_conversations.updated_at_utc
                    THEN excluded.title
                    ELSE chat_conversations.title
                END,
                updated_at_utc = MAX(chat_conversations.updated_at_utc, excluded.updated_at_utc)
            WHERE chat_conversations.created_at_utc = excluded.created_at_utc
              AND chat_conversations.tenant_id = excluded.tenant_id
              AND chat_conversations.user_id = excluded.user_id;
            """;
        command.Parameters.AddWithValue("$id", Format(value.Id));
        command.Parameters.AddWithValue("$title", value.Title);
        command.Parameters.AddWithValue("$createdAt", Format(value.CreatedAtUtc));
        command.Parameters.AddWithValue("$updatedAt", Format(value.UpdatedAtUtc));
        command.Parameters.AddWithValue("$tenantId", value.TenantId);
        command.Parameters.AddWithValue("$userId", value.UserId);
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            throw new InvalidOperationException(
                "The conversation identity conflicts with persisted data.");
        }
    }

    private static async Task PersistMessagesAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid conversationId,
        IReadOnlyList<ConversationMessageRecord> values,
        CancellationToken cancellationToken)
    {
        var persisted = new Dictionary<Guid, (ConversationMessageRecord Value, long Ordinal)>();
        long nextOrdinal = 0;
        await using (SqliteCommand read = connection.CreateCommand())
        {
            read.Transaction = transaction;
            read.CommandText =
                """
                SELECT id, conversation_id, role, content, content_sha256,
                       content_utf8_bytes, created_at_utc, kind, business_query_id,
                       business_receipt_json, business_presentation_json,
                       business_integrity_sha256, ordinal
                FROM chat_messages
                WHERE conversation_id = $conversationId
                ORDER BY ordinal;
                """;
            read.Parameters.AddWithValue("$conversationId", Format(conversationId));
            await using SqliteDataReader reader =
                await read.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                ConversationMessageRecord message = ReadMessage(reader);
                long ordinal = reader.GetInt64(12);
                persisted.Add(message.Id, (message, ordinal));
                nextOrdinal = Math.Max(nextOrdinal, checked(ordinal + 1));
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

            long ordinal = nextOrdinal;
            nextOrdinal = checked(nextOrdinal + 1);
            if (ordinal <= previousOrdinal)
            {
                throw new InvalidOperationException(
                    "The supplied conversation message order cannot be appended safely.");
            }

            await InsertMessageAsync(
                connection,
                transaction,
                value,
                ordinal,
                cancellationToken);
            previousOrdinal = ordinal;
        }
    }

    private static async Task InsertMessageAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        ConversationMessageRecord value,
        long ordinal,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO chat_messages
                (id, conversation_id, ordinal, role, content, content_sha256,
                 content_utf8_bytes, created_at_utc, kind, business_query_id,
                 business_receipt_json, business_presentation_json,
                 business_integrity_sha256)
            VALUES
                ($id, $conversationId, $ordinal, $role, $content, $contentSha256,
                 $contentBytes, $createdAt, $kind, $businessQueryId,
                 $businessReceipt, $businessPresentation, $businessIntegrity);
            """;
        command.Parameters.AddWithValue("$id", Format(value.Id));
        command.Parameters.AddWithValue("$conversationId", Format(value.ConversationId));
        command.Parameters.AddWithValue("$ordinal", ordinal);
        command.Parameters.AddWithValue("$role", value.Role.ToString());
        command.Parameters.AddWithValue("$content", value.Content);
        command.Parameters.AddWithValue("$contentSha256", value.ContentSha256);
        command.Parameters.AddWithValue("$contentBytes", value.ContentUtf8Bytes);
        command.Parameters.AddWithValue("$createdAt", Format(value.CreatedAtUtc));
        command.Parameters.AddWithValue("$kind", value.Kind.ToString());
        command.Parameters.AddWithValue(
            "$businessQueryId",
            value.BusinessQueryId.HasValue
                ? Format(value.BusinessQueryId.Value)
                : DBNull.Value);
        command.Parameters.AddWithValue("$businessReceipt", value.BusinessQueryReceiptJson);
        command.Parameters.AddWithValue(
            "$businessPresentation", value.BusinessQueryPresentationJson);
        command.Parameters.AddWithValue(
            "$businessIntegrity", value.BusinessQueryIntegritySha256);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpsertEntryRunAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        UnifiedEntryRunRecord value,
        bool rowExists,
        long expectedRevision,
        long nextRevision,
        string operationFingerprint,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = rowExists
            ?
                """
                UPDATE unified_entry_runs
                SET conversation_id = $conversationId,
                    correlation_id = $correlationId,
                    main_agent_version_id = $mainAgentVersionId,
                    status = $status,
                    started_at_utc = $startedAt,
                    finished_at_utc = $finishedAt,
                    duration_ticks = $durationTicks,
                    input_text = $input,
                    input_sha256 = $inputSha256,
                    output_text = $output,
                    output_sha256 = $outputSha256,
                    error_code = $errorCode,
                    tenant_id = $tenantId,
                    user_id = $userId,
                    persistence_revision = $nextRevision,
                    state_sha256 = $operationFingerprint
                WHERE id = $id
                  AND persistence_revision = $expectedRevision
                  AND tenant_id = $tenantId
                  AND user_id = $userId;
                """
            :
                """
                INSERT INTO unified_entry_runs
                    (id, conversation_id, correlation_id, main_agent_version_id,
                     status, started_at_utc, finished_at_utc, duration_ticks,
                     input_text, input_sha256, output_text, output_sha256,
                     error_code, tenant_id, user_id, persistence_revision, state_sha256)
                VALUES
                    ($id, $conversationId, $correlationId, $mainAgentVersionId,
                     $status, $startedAt, $finishedAt, $durationTicks,
                     $input, $inputSha256, $output, $outputSha256,
                     $errorCode, $tenantId, $userId, $nextRevision, $operationFingerprint);
                """;
        AddEntryRunParameters(command, value);
        command.Parameters.AddWithValue("$expectedRevision", expectedRevision);
        command.Parameters.AddWithValue("$nextRevision", nextRevision);
        command.Parameters.AddWithValue(
            "$operationFingerprint",
            operationFingerprint);
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            throw ConcurrentWriteRejected();
        }
    }

    private static async Task DeleteRunChildrenAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid runId,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            DELETE FROM unified_run_events WHERE entry_run_id = $runId;
            DELETE FROM unified_tool_calls WHERE entry_run_id = $runId;
            DELETE FROM unified_orchestration_links WHERE entry_run_id = $runId;
            DELETE FROM unified_agent_runs WHERE entry_run_id = $runId;
            """;
        command.Parameters.AddWithValue("$runId", Format(runId));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertAgentRunAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        UnifiedAgentRunRecord value,
        int ordinal,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO unified_agent_runs
                (id, entry_run_id, ordinal, parent_run_id, kind, agent_id,
                 agent_version_id, depth, status, started_at_utc, finished_at_utc,
                 duration_ticks, input_text, input_sha256, output_text,
                 output_sha256, error_code)
            VALUES
                ($id, $entryRunId, $ordinal, $parentRunId, $kind, $agentId,
                 $agentVersionId, $depth, $status, $startedAt, $finishedAt,
                 $durationTicks, $input, $inputSha256, $output,
                 $outputSha256, $errorCode);
            """;
        command.Parameters.AddWithValue("$id", Format(value.Id));
        command.Parameters.AddWithValue("$entryRunId", Format(value.EntryRunId));
        command.Parameters.AddWithValue("$ordinal", ordinal);
        command.Parameters.AddWithValue("$parentRunId", Db(value.ParentRunId));
        command.Parameters.AddWithValue("$kind", value.Kind.ToString());
        command.Parameters.AddWithValue("$agentId", Format(value.AgentId));
        command.Parameters.AddWithValue("$agentVersionId", Format(value.AgentVersionId));
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
        SqliteConnection connection,
        SqliteTransaction transaction,
        UnifiedOrchestrationRunLink value,
        int ordinal,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO unified_orchestration_links
                (id, entry_run_id, ordinal, parent_run_id, orchestration_run_id,
                 orchestration_version_id, depth, status, started_at_utc,
                 finished_at_utc, duration_ticks, input_text, input_sha256,
                 output_text, output_sha256, error_code)
            VALUES
                ($id, $entryRunId, $ordinal, $parentRunId, $orchestrationRunId,
                 $orchestrationVersionId, $depth, $status, $startedAt,
                 $finishedAt, $durationTicks, $input, $inputSha256,
                 $output, $outputSha256, $errorCode);
            """;
        command.Parameters.AddWithValue("$id", Format(value.Id));
        command.Parameters.AddWithValue("$entryRunId", Format(value.EntryRunId));
        command.Parameters.AddWithValue("$ordinal", ordinal);
        command.Parameters.AddWithValue("$parentRunId", Format(value.ParentRunId));
        command.Parameters.AddWithValue(
            "$orchestrationRunId",
            Format(value.OrchestrationRunId));
        command.Parameters.AddWithValue(
            "$orchestrationVersionId",
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
        SqliteConnection connection,
        SqliteTransaction transaction,
        UnifiedToolCallRecord value,
        int ordinal,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO unified_tool_calls
                (id, entry_run_id, ordinal, parent_run_id, tool_version_id,
                 depth, status, started_at_utc, finished_at_utc, duration_ticks,
                 arguments_json, arguments_sha256, result_content, result_sha256,
                 error_code)
            VALUES
                ($id, $entryRunId, $ordinal, $parentRunId, $toolVersionId,
                 $depth, $status, $startedAt, $finishedAt, $durationTicks,
                 $argumentsJson, $argumentsSha256, $resultContent, $resultSha256,
                 $errorCode);
            """;
        command.Parameters.AddWithValue("$id", Format(value.Id));
        command.Parameters.AddWithValue("$entryRunId", Format(value.EntryRunId));
        command.Parameters.AddWithValue("$ordinal", ordinal);
        command.Parameters.AddWithValue("$parentRunId", Format(value.ParentRunId));
        command.Parameters.AddWithValue("$toolVersionId", Format(value.ToolVersionId));
        command.Parameters.AddWithValue("$depth", value.Depth);
        command.Parameters.AddWithValue("$status", value.Status.ToString());
        command.Parameters.AddWithValue("$startedAt", Format(value.StartedAtUtc));
        command.Parameters.AddWithValue("$finishedAt", Db(value.FinishedAtUtc));
        command.Parameters.AddWithValue("$durationTicks", Db(value.Duration));
        command.Parameters.AddWithValue("$argumentsJson", value.ArgumentsJson);
        command.Parameters.AddWithValue("$argumentsSha256", value.ArgumentsSha256);
        command.Parameters.AddWithValue("$resultContent", value.ResultContent);
        command.Parameters.AddWithValue("$resultSha256", value.ResultSha256);
        command.Parameters.AddWithValue("$errorCode", value.ErrorCode);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertEventAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        UnifiedRunEventRecord value,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO unified_run_events
                (id, entry_run_id, sequence, correlation_id, kind,
                 occurred_at_utc, parent_run_id, depth, payload_json,
                 payload_sha256)
            VALUES
                ($id, $entryRunId, $sequence, $correlationId, $kind,
                 $occurredAt, $parentRunId, $depth, $payloadJson,
                 $payloadSha256);
            """;
        command.Parameters.AddWithValue("$id", Format(value.Id));
        command.Parameters.AddWithValue("$entryRunId", Format(value.EntryRunId));
        command.Parameters.AddWithValue("$sequence", value.Sequence);
        command.Parameters.AddWithValue("$correlationId", Format(value.CorrelationId));
        command.Parameters.AddWithValue("$kind", value.Kind);
        command.Parameters.AddWithValue("$occurredAt", Format(value.OccurredAtUtc));
        command.Parameters.AddWithValue("$parentRunId", Db(value.ParentRunId));
        command.Parameters.AddWithValue("$depth", value.Depth);
        command.Parameters.AddWithValue("$payloadJson", value.PayloadJson);
        command.Parameters.AddWithValue("$payloadSha256", value.PayloadSha256);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<UnifiedEntryRunRecord?> ReadEntryRunAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid runId,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            $"""
            {EntryRunSelect}
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", Format(runId));
        await using SqliteDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? ReadEntryRun(reader)
            : null;
    }

    private static async Task<UnifiedEntryRunRecord?> ReadEntryRunForOwnerAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid runId,
        string tenantId,
        string userId,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            $"""
            {EntryRunSelect}
            WHERE id = $id AND tenant_id = $tenantId AND user_id = $userId;
            """;
        command.Parameters.AddWithValue("$id", Format(runId));
        AddOwnerParameters(command, tenantId, userId);
        await using SqliteDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? ReadEntryRun(reader)
            : null;
    }

    private static async Task<IReadOnlyList<UnifiedAgentRunRecord>> ReadAgentRunsAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid runId,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT id, entry_run_id, parent_run_id, kind, agent_id,
                   agent_version_id, depth, status, started_at_utc,
                   finished_at_utc, duration_ticks, input_text, input_sha256,
                   output_text, output_sha256, error_code
            FROM unified_agent_runs
            WHERE entry_run_id = $runId
            ORDER BY ordinal;
            """;
        command.Parameters.AddWithValue("$runId", Format(runId));
        var values = new List<UnifiedAgentRunRecord>();
        await using SqliteDataReader reader =
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
            SqliteConnection connection,
            SqliteTransaction? transaction,
            Guid runId,
            CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT id, entry_run_id, parent_run_id, orchestration_run_id,
                   orchestration_version_id, depth, status, started_at_utc,
                   finished_at_utc, duration_ticks, input_text, input_sha256,
                   output_text, output_sha256, error_code
            FROM unified_orchestration_links
            WHERE entry_run_id = $runId
            ORDER BY ordinal;
            """;
        command.Parameters.AddWithValue("$runId", Format(runId));
        var values = new List<UnifiedOrchestrationRunLink>();
        await using SqliteDataReader reader =
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
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid runId,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT id, entry_run_id, parent_run_id, tool_version_id, depth,
                   status, started_at_utc, finished_at_utc, duration_ticks,
                   arguments_json, arguments_sha256, result_content,
                   result_sha256, error_code
            FROM unified_tool_calls
            WHERE entry_run_id = $runId
            ORDER BY ordinal;
            """;
        command.Parameters.AddWithValue("$runId", Format(runId));
        var values = new List<UnifiedToolCallRecord>();
        await using SqliteDataReader reader =
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
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid runId,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT id, entry_run_id, sequence, correlation_id, kind,
                   occurred_at_utc, parent_run_id, depth, payload_json,
                   payload_sha256
            FROM unified_run_events
            WHERE entry_run_id = $runId
            ORDER BY sequence;
            """;
        command.Parameters.AddWithValue("$runId", Format(runId));
        var values = new List<UnifiedRunEventRecord>();
        await using SqliteDataReader reader =
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

    private static ConversationRecord ReadConversation(SqliteDataReader reader) =>
        new(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            ParseDate(reader.GetString(2)),
            ParseDate(reader.GetString(3)))
        {
            TenantId = reader.GetString(4),
            UserId = reader.GetString(5)
        };

    private static ConversationMessageRecord ReadMessage(SqliteDataReader reader) =>
        new(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            ParseEnum<ConversationMessageRole>(reader.GetString(2)),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetInt32(5),
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

    private static UnifiedEntryRunRecord ReadEntryRun(SqliteDataReader reader) =>
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
        SqliteCommand command,
        UnifiedEntryRunRecord value)
    {
        command.Parameters.AddWithValue("$id", Format(value.Id));
        command.Parameters.AddWithValue("$conversationId", Format(value.ConversationId));
        command.Parameters.AddWithValue("$correlationId", Format(value.CorrelationId));
        command.Parameters.AddWithValue("$tenantId", value.TenantId);
        command.Parameters.AddWithValue("$userId", value.UserId);
        command.Parameters.AddWithValue(
            "$mainAgentVersionId",
            Format(value.MainAgentVersionId));
        command.Parameters.AddWithValue("$status", value.Status.ToString());
        command.Parameters.AddWithValue("$startedAt", Format(value.StartedAtUtc));
        command.Parameters.AddWithValue("$finishedAt", Db(value.FinishedAtUtc));
        command.Parameters.AddWithValue("$durationTicks", Db(value.Duration));
        command.Parameters.AddWithValue("$input", value.Input);
        command.Parameters.AddWithValue("$inputSha256", value.InputSha256);
        command.Parameters.AddWithValue("$output", value.Output);
        command.Parameters.AddWithValue("$outputSha256", value.OutputSha256);
        command.Parameters.AddWithValue("$errorCode", value.ErrorCode);
    }

    private static void AddOwnerParameters(
        SqliteCommand command,
        string tenantId,
        string userId)
    {
        command.Parameters.AddWithValue("$tenantId", tenantId);
        command.Parameters.AddWithValue("$userId", userId);
    }

    private static void AddBranchParameters(
        SqliteCommand command,
        int depth,
        UnifiedRunStatus status,
        DateTimeOffset startedAtUtc,
        DateTimeOffset? finishedAtUtc,
        TimeSpan? duration,
        string input,
        string inputSha256,
        string output,
        string outputSha256,
        string errorCode)
    {
        command.Parameters.AddWithValue("$depth", depth);
        command.Parameters.AddWithValue("$status", status.ToString());
        command.Parameters.AddWithValue("$startedAt", Format(startedAtUtc));
        command.Parameters.AddWithValue("$finishedAt", Db(finishedAtUtc));
        command.Parameters.AddWithValue("$durationTicks", Db(duration));
        command.Parameters.AddWithValue("$input", input);
        command.Parameters.AddWithValue("$inputSha256", inputSha256);
        command.Parameters.AddWithValue("$output", output);
        command.Parameters.AddWithValue("$outputSha256", outputSha256);
        command.Parameters.AddWithValue("$errorCode", errorCode);
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

    private static bool IsTerminal(UnifiedRunStatus status) =>
        status is UnifiedRunStatus.Completed
            or UnifiedRunStatus.Failed
            or UnifiedRunStatus.Cancelled
            or UnifiedRunStatus.Blocked;

    private void EnsureCreated()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using (SqliteCommand pragma = connection.CreateCommand())
        {
            pragma.CommandText =
                """
                PRAGMA busy_timeout = 5000;
                PRAGMA foreign_keys = ON;
                PRAGMA journal_mode = WAL;
                """;
            pragma.ExecuteNonQuery();
        }

        using SqliteTransaction transaction =
            connection.BeginTransaction(deferred: false);
        try
        {
            using (SqliteCommand schema = connection.CreateCommand())
            {
                schema.Transaction = transaction;
                schema.CommandText = CreateSchemaSql;
                schema.ExecuteNonQuery();
            }

            UpgradeEntryPersistenceRevision(connection, transaction);
            UpgradeOwnership(connection, transaction);
            using (SqliteCommand revisionValidation = connection.CreateCommand())
            {
                revisionValidation.Transaction = transaction;
                revisionValidation.CommandText = CreateRevisionValidationSql;
                revisionValidation.ExecuteNonQuery();
            }

            UpgradeMessageOrdinals(connection, transaction);
            UpgradeMessageKinds(connection, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void UpgradeOwnership(
        SqliteConnection connection,
        SqliteTransaction transaction)
    {
        EnsureOwnershipColumns(connection, transaction, "chat_conversations");
        EnsureOwnershipColumns(connection, transaction, "unified_entry_runs");
        using SqliteCommand indexes = connection.CreateCommand();
        indexes.Transaction = transaction;
        indexes.CommandText =
            """
            CREATE INDEX IF NOT EXISTS ix_chat_conversations_owner_updated
                ON chat_conversations (tenant_id, user_id, updated_at_utc DESC, id);
            CREATE INDEX IF NOT EXISTS ix_unified_entry_runs_owner_started
                ON unified_entry_runs (tenant_id, user_id, started_at_utc DESC, id);
            """;
        indexes.ExecuteNonQuery();
    }

    private static void EnsureOwnershipColumns(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string table)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (SqliteCommand columns = connection.CreateCommand())
        {
            columns.Transaction = transaction;
            columns.CommandText = $"PRAGMA table_info({table});";
            using SqliteDataReader reader = columns.ExecuteReader();
            while (reader.Read())
            {
                names.Add(reader.GetString(1));
            }
        }

        foreach (string column in new[] { "tenant_id", "user_id" })
        {
            if (names.Contains(column))
            {
                continue;
            }

            using SqliteCommand alter = connection.CreateCommand();
            alter.Transaction = transaction;
            alter.CommandText =
                $"ALTER TABLE {table} ADD COLUMN {column} TEXT NOT NULL DEFAULT '__legacy_unowned__';";
            alter.ExecuteNonQuery();
        }
    }

    private static void UpgradeEntryPersistenceRevision(
        SqliteConnection connection,
        SqliteTransaction transaction)
    {
        bool hasPersistenceRevision = false;
        using (SqliteCommand columns = connection.CreateCommand())
        {
            columns.Transaction = transaction;
            columns.CommandText = "PRAGMA table_info(unified_entry_runs);";
            using SqliteDataReader reader = columns.ExecuteReader();
            while (reader.Read())
            {
                hasPersistenceRevision |= StringComparer.OrdinalIgnoreCase.Equals(
                    reader.GetString(1),
                    "persistence_revision");
            }
        }

        if (!hasPersistenceRevision)
        {
            using SqliteCommand alter = connection.CreateCommand();
            alter.Transaction = transaction;
            alter.CommandText =
                """
                ALTER TABLE unified_entry_runs
                ADD COLUMN persistence_revision INTEGER NOT NULL DEFAULT 0;
                """;
            alter.ExecuteNonQuery();
        }

        using SqliteCommand validate = connection.CreateCommand();
        validate.Transaction = transaction;
        validate.CommandText =
            """
            SELECT EXISTS(
                SELECT 1
                FROM unified_entry_runs
                WHERE persistence_revision < 0);
            """;
        if (Convert.ToInt64(validate.ExecuteScalar()) != 0)
        {
            throw new InvalidDataException(
                "The unified entry persistence revision cannot be negative.");
        }
    }

    private static void UpgradeMessageOrdinals(
        SqliteConnection connection,
        SqliteTransaction transaction)
    {
        bool hasOrdinal = false;
        using (SqliteCommand columns = connection.CreateCommand())
        {
            columns.Transaction = transaction;
            columns.CommandText = "PRAGMA table_info(chat_messages);";
            using SqliteDataReader reader = columns.ExecuteReader();
            while (reader.Read())
            {
                hasOrdinal |= StringComparer.OrdinalIgnoreCase.Equals(
                    reader.GetString(1),
                    "ordinal");
            }
        }

        if (!hasOrdinal)
        {
            using SqliteCommand rebuild = connection.CreateCommand();
            rebuild.Transaction = transaction;
            rebuild.CommandText =
                """
                ALTER TABLE chat_messages RENAME TO chat_messages_without_ordinal;

                CREATE TABLE chat_messages
                (
                    id                 TEXT    NOT NULL PRIMARY KEY,
                    conversation_id    TEXT    NOT NULL,
                    ordinal            INTEGER NOT NULL CHECK (ordinal >= 0),
                    role               TEXT    NOT NULL,
                    content            TEXT    NOT NULL,
                    content_sha256     TEXT    NOT NULL,
                    content_utf8_bytes INTEGER NOT NULL CHECK (content_utf8_bytes >= 0),
                    created_at_utc     TEXT    NOT NULL,
                    UNIQUE (conversation_id, ordinal),
                    FOREIGN KEY (conversation_id) REFERENCES chat_conversations(id)
                ) WITHOUT ROWID;

                INSERT INTO chat_messages
                    (id, conversation_id, ordinal, role, content, content_sha256,
                     content_utf8_bytes, created_at_utc)
                SELECT
                    target.id,
                    target.conversation_id,
                    (
                        SELECT COUNT(*) - 1
                        FROM chat_messages_without_ordinal AS preceding
                        WHERE preceding.conversation_id = target.conversation_id
                          AND
                          (
                              preceding.created_at_utc < target.created_at_utc
                              OR
                              (
                                  preceding.created_at_utc = target.created_at_utc
                                  AND preceding.id <= target.id
                              )
                          )
                    ),
                    target.role,
                    target.content,
                    target.content_sha256,
                    target.content_utf8_bytes,
                    target.created_at_utc
                FROM chat_messages_without_ordinal AS target;

                DROP TABLE chat_messages_without_ordinal;
                """;
            rebuild.ExecuteNonQuery();
        }

        using SqliteCommand index = connection.CreateCommand();
        index.Transaction = transaction;
        index.CommandText =
            """
            CREATE UNIQUE INDEX IF NOT EXISTS ux_chat_messages_conversation_ordinal
                ON chat_messages (conversation_id, ordinal);
            """;
        index.ExecuteNonQuery();
    }

    private static void UpgradeMessageKinds(
        SqliteConnection connection,
        SqliteTransaction transaction)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (SqliteCommand read = connection.CreateCommand())
        {
            read.Transaction = transaction;
            read.CommandText = "PRAGMA table_info(chat_messages);";
            using SqliteDataReader reader = read.ExecuteReader();
            while (reader.Read())
            {
                columns.Add(reader.GetString(1));
            }
        }

        (string Name, string Sql)[] additions =
        [
            ("kind", "ALTER TABLE chat_messages ADD COLUMN kind TEXT NOT NULL DEFAULT 'Legacy';"),
            ("business_query_id", "ALTER TABLE chat_messages ADD COLUMN business_query_id TEXT;"),
            ("business_receipt_json", "ALTER TABLE chat_messages ADD COLUMN business_receipt_json TEXT NOT NULL DEFAULT '';"),
            ("business_presentation_json", "ALTER TABLE chat_messages ADD COLUMN business_presentation_json TEXT NOT NULL DEFAULT '';"),
            ("business_integrity_sha256", "ALTER TABLE chat_messages ADD COLUMN business_integrity_sha256 TEXT NOT NULL DEFAULT '';")
        ];
        foreach ((string name, string sql) in additions)
        {
            if (columns.Contains(name))
            {
                continue;
            }

            using SqliteCommand alter = connection.CreateCommand();
            alter.Transaction = transaction;
            alter.CommandText = sql;
            alter.ExecuteNonQuery();
        }

        using SqliteCommand index = connection.CreateCommand();
        index.Transaction = transaction;
        index.CommandText =
            "CREATE INDEX IF NOT EXISTS ix_chat_messages_business_query " +
            "ON chat_messages (business_query_id) WHERE business_query_id IS NOT NULL;";
        index.ExecuteNonQuery();
    }

    private async Task<SqliteConnection> OpenAsync(
        CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            await using SqliteCommand pragma = connection.CreateCommand();
            pragma.CommandText =
                """
                PRAGMA foreign_keys = ON;
                PRAGMA busy_timeout = 5000;
                """;
            await pragma.ExecuteNonQueryAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
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

    private static Guid? ReadNullableGuid(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Guid.Parse(reader.GetString(ordinal));

    private static DateTimeOffset? ReadNullableDate(
        SqliteDataReader reader,
        int ordinal) =>
        reader.IsDBNull(ordinal) ? null : ParseDate(reader.GetString(ordinal));

    private static TimeSpan? ReadNullableDuration(
        SqliteDataReader reader,
        int ordinal) =>
        reader.IsDBNull(ordinal)
            ? null
            : TimeSpan.FromTicks(reader.GetInt64(ordinal));

    private sealed record InterruptedRun(
        Guid Id,
        Guid ConversationId,
        Guid CorrelationId,
        DateTimeOffset StartedAtUtc,
        long LastSequence);

    private static T ParseEnum<T>(string value)
        where T : struct, Enum =>
        Enum.TryParse(value, ignoreCase: false, out T parsed)
            ? parsed
            : throw new InvalidDataException(
                $"The SQLite value '{value}' is not a valid {typeof(T).Name}.");
}
