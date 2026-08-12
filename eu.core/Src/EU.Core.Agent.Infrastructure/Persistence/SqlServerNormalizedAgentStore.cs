using System.Collections.ObjectModel;
using System.Data;
using EU.Core.Agent.Application.Agents;
using Microsoft.Data.SqlClient;

namespace EU.Core.Agent.Infrastructure.Persistence;

internal sealed class SqlServerNormalizedAgentStore
{
    private const string VersionScope = "Version";
    private const string SnapshotScope = "Snapshot";
    private readonly string _connectionString;

    public SqlServerNormalizedAgentStore(string connectionString)
    {
        _connectionString = SqlServerAgentConnection.Validate(connectionString);
    }

    public async Task<AgentDefinition?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await OpenConnectionAsync(cancellationToken);
        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT Id, Code, Name, Description, RuntimeStatus, LogicalRevision
            FROM AgAgentDefinition
            WHERE Id = @Id;

            SELECT Id, AgentId, Ordinal, Label, IsDraft, Instructions,
                   ModelProfileId, OutputMode, OutputJsonSchema, OutputSchemaSha256
            FROM AgAgentVersion
            WHERE AgentId = @Id
            ORDER BY IsDraft DESC, Ordinal;

            SELECT Snapshot.VersionId, Snapshot.SnapshotVersionId, Snapshot.AgentCode,
                   Snapshot.Instructions, Snapshot.ModelProfileId, Snapshot.OutputMode,
                   Snapshot.OutputJsonSchema, Snapshot.AgentName, Snapshot.AgentDescription
            FROM AgAgentVersionSnapshot AS Snapshot
            INNER JOIN AgAgentVersion AS Version ON Version.Id = Snapshot.VersionId
            WHERE Version.AgentId = @Id;

            SELECT Binding.VersionId, Binding.Scope, Binding.BindingType, Binding.Ordinal,
                   Binding.ReferenceId, Binding.ReferenceVersionId, Binding.LogicalRevision,
                   Binding.ReferenceCode, Binding.ReferenceName, Binding.ReferenceDescription
            FROM AgAgentVersionBinding AS Binding
            INNER JOIN AgAgentVersion AS Version ON Version.Id = Binding.VersionId
            WHERE Version.AgentId = @Id
            ORDER BY Binding.VersionId, Binding.Scope, Binding.BindingType, Binding.Ordinal;
            """;
        AddGuid(command, "@Id", id);

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            await reader.DisposeAsync();
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        Guid definitionId = ReadGuid(reader, 0);
        string code = reader.GetString(1);
        string name = reader.GetString(2);
        string description = reader.GetString(3);
        AgentRuntimeStatus runtimeStatus = ParseEnum<AgentRuntimeStatus>(reader.GetString(4), "RuntimeStatus");
        long logicalRevision = reader.GetInt64(5);

        var versions = new Dictionary<Guid, MutableVersion>();
        await reader.NextResultAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var version = new MutableVersion(
                ReadGuid(reader, 0),
                reader.GetInt32(2),
                reader.GetString(3),
                reader.GetBoolean(4),
                reader.GetString(5),
                reader.GetString(6),
                ParseEnum<AgentOutputMode>(reader.GetString(7), "OutputMode"),
                ReadNullableString(reader, 8),
                ReadNullableString(reader, 9));
            versions.Add(version.Id, version);
        }

        await reader.NextResultAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            Guid versionId = ReadGuid(reader, 0);
            if (!versions.TryGetValue(versionId, out MutableVersion? version))
            {
                throw new InvalidDataException("An Agent snapshot references a missing version.");
            }

            version.Snapshot = new MutableSnapshot(
                ReadGuid(reader, 1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                ParseEnum<AgentOutputMode>(reader.GetString(5), "Snapshot.OutputMode"),
                ReadNullableString(reader, 6),
                ReadNullableString(reader, 7),
                ReadNullableString(reader, 8));
        }

        await reader.NextResultAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            Guid versionId = ReadGuid(reader, 0);
            if (!versions.TryGetValue(versionId, out MutableVersion? version))
            {
                throw new InvalidDataException("An Agent binding references a missing version.");
            }

            string scope = reader.GetString(1);
            string bindingType = reader.GetString(2);
            Guid referenceId = ReadGuid(reader, 4);
            Guid? referenceVersionId = ReadNullableGuid(reader, 5);
            long? referenceRevision = reader.IsDBNull(6) ? null : reader.GetInt64(6);
            string? referenceCode = ReadNullableString(reader, 7);
            string? referenceName = ReadNullableString(reader, 8);
            string? referenceDescription = ReadNullableString(reader, 9);
            AddBinding(version, scope, bindingType, referenceId, referenceVersionId,
                referenceRevision, referenceCode, referenceName, referenceDescription);
        }

        MutableVersion draft = versions.Values.SingleOrDefault(value => value.IsDraft)
            ?? throw new InvalidDataException("The Agent does not have exactly one Draft version.");
        IReadOnlyList<AgentVersion> published = versions.Values
            .Where(value => !value.IsDraft)
            .OrderBy(value => value.Ordinal)
            .Select(BuildVersion)
            .ToArray();

        var definition = new AgentDefinition(
            definitionId,
            code,
            name,
            description,
            runtimeStatus,
            logicalRevision,
            BuildVersion(draft),
            new ReadOnlyCollection<AgentVersion>(published.ToArray()));
        await reader.DisposeAsync();
        await transaction.CommitAsync(cancellationToken);
        return definition;
    }

    public async Task<AgentDefinition?> GetByCodeAsync(
        string normalizedCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedCode);
        await using SqlConnection connection = await OpenConnectionAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT Id FROM AgAgentDefinition WHERE Code = @Code;";
        command.Parameters.Add("@Code", SqlDbType.NVarChar, 128).Value = normalizedCode;
        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : await GetByIdAsync((Guid)value, cancellationToken);
    }

    public async Task<IReadOnlyList<AgentDefinition>> ListAsync(
        AgentDefinitionQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        await using SqlConnection connection = await OpenConnectionAsync(cancellationToken);
        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SET NOCOUNT ON;

            SELECT Id INTO #SelectedAgentIds
            FROM AgAgentDefinition WITH (HOLDLOCK)
            WHERE (@RuntimeStatus IS NULL AND RuntimeStatus <> 'Archived'
                   OR @RuntimeStatus IS NOT NULL AND RuntimeStatus = @RuntimeStatus)
              AND (@Search IS NULL
                   OR Code COLLATE Latin1_General_100_CI_AS LIKE @Search ESCAPE '\'
                   OR Name COLLATE Latin1_General_100_CI_AS LIKE @Search ESCAPE '\'
                   OR Description COLLATE Latin1_General_100_CI_AS LIKE @Search ESCAPE '\');

            SELECT Definition.Id, Definition.Code, Definition.Name, Definition.Description,
                   Definition.RuntimeStatus, Definition.LogicalRevision
            FROM AgAgentDefinition AS Definition
            INNER JOIN #SelectedAgentIds AS Selected ON Selected.Id = Definition.Id
            ORDER BY Definition.Code, Definition.Id;

            SELECT Version.Id, Version.AgentId, Version.Ordinal, Version.Label, Version.IsDraft,
                   Version.Instructions, Version.ModelProfileId, Version.OutputMode,
                   Version.OutputJsonSchema, Version.OutputSchemaSha256
            FROM AgAgentVersion AS Version
            INNER JOIN #SelectedAgentIds AS Selected ON Selected.Id = Version.AgentId
            ORDER BY Version.AgentId, Version.IsDraft DESC, Version.Ordinal;

            SELECT Snapshot.VersionId, Snapshot.SnapshotVersionId, Snapshot.AgentCode,
                   Snapshot.Instructions, Snapshot.ModelProfileId, Snapshot.OutputMode,
                   Snapshot.OutputJsonSchema, Snapshot.AgentName, Snapshot.AgentDescription,
                   Version.AgentId
            FROM AgAgentVersionSnapshot AS Snapshot
            INNER JOIN AgAgentVersion AS Version ON Version.Id = Snapshot.VersionId
            INNER JOIN #SelectedAgentIds AS Selected ON Selected.Id = Version.AgentId
            ORDER BY Version.AgentId, Snapshot.VersionId;

            SELECT Binding.VersionId, Binding.Scope, Binding.BindingType, Binding.Ordinal,
                   Binding.ReferenceId, Binding.ReferenceVersionId, Binding.LogicalRevision,
                   Binding.ReferenceCode, Binding.ReferenceName, Binding.ReferenceDescription,
                   Version.AgentId
            FROM AgAgentVersionBinding AS Binding
            INNER JOIN AgAgentVersion AS Version ON Version.Id = Binding.VersionId
            INNER JOIN #SelectedAgentIds AS Selected ON Selected.Id = Version.AgentId
            ORDER BY Version.AgentId, Binding.VersionId, Binding.Scope,
                     Binding.BindingType, Binding.Ordinal;
            """;
        command.Parameters.Add("@RuntimeStatus", SqlDbType.VarChar, 32).Value =
            query.RuntimeStatus is null ? DBNull.Value : query.RuntimeStatus.Value.ToString();
        command.Parameters.Add("@Search", SqlDbType.NVarChar, 520).Value =
            string.IsNullOrWhiteSpace(query.Search)
                ? DBNull.Value
                : $"%{EscapeLike(query.Search.Trim())}%";

        var definitions = new Dictionary<Guid, MutableDefinition>();
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            Guid id = ReadGuid(reader, 0);
            definitions.Add(id, new MutableDefinition(
                id, reader.GetString(1), reader.GetString(2), reader.GetString(3),
                ParseEnum<AgentRuntimeStatus>(reader.GetString(4), "RuntimeStatus"),
                reader.GetInt64(5)));
        }

        await reader.NextResultAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            Guid agentId = ReadGuid(reader, 1);
            if (!definitions.TryGetValue(agentId, out MutableDefinition? definition))
            {
                throw new InvalidDataException("An Agent version references a missing definition.");
            }
            var version = new MutableVersion(
                ReadGuid(reader, 0), reader.GetInt32(2), reader.GetString(3),
                reader.GetBoolean(4), reader.GetString(5), reader.GetString(6),
                ParseEnum<AgentOutputMode>(reader.GetString(7), "OutputMode"),
                ReadNullableString(reader, 8), ReadNullableString(reader, 9));
            definition.Versions.Add(version.Id, version);
        }

        await reader.NextResultAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            Guid agentId = ReadGuid(reader, 9);
            Guid versionId = ReadGuid(reader, 0);
            if (!definitions.TryGetValue(agentId, out MutableDefinition? definition) ||
                !definition.Versions.TryGetValue(versionId, out MutableVersion? version))
            {
                throw new InvalidDataException("An Agent snapshot references a missing version.");
            }
            version.Snapshot = new MutableSnapshot(
                ReadGuid(reader, 1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
                ParseEnum<AgentOutputMode>(reader.GetString(5), "Snapshot.OutputMode"),
                ReadNullableString(reader, 6), ReadNullableString(reader, 7),
                ReadNullableString(reader, 8));
        }

        await reader.NextResultAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            Guid agentId = ReadGuid(reader, 10);
            Guid versionId = ReadGuid(reader, 0);
            if (!definitions.TryGetValue(agentId, out MutableDefinition? definition) ||
                !definition.Versions.TryGetValue(versionId, out MutableVersion? version))
            {
                throw new InvalidDataException("An Agent binding references a missing version.");
            }
            AddBinding(version, reader.GetString(1), reader.GetString(2), ReadGuid(reader, 4),
                ReadNullableGuid(reader, 5), reader.IsDBNull(6) ? null : reader.GetInt64(6),
                ReadNullableString(reader, 7), ReadNullableString(reader, 8),
                ReadNullableString(reader, 9));
        }

        var result = definitions.Values.Select(BuildDefinition).ToArray();
        await reader.DisposeAsync();
        await transaction.CommitAsync(cancellationToken);
        return new ReadOnlyCollection<AgentDefinition>(result);
    }

    public async Task<bool> TryCreateAsync(
        AgentDefinition definition,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definition);
        await using SqlConnection connection = await OpenConnectionAsync(cancellationToken);
        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            await using SqlCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO AgAgentDefinition
                    (Id, Code, Name, Description, RuntimeStatus, LogicalRevision)
                SELECT @Id, @Code, @Name, @Description, @RuntimeStatus, @LogicalRevision
                WHERE NOT EXISTS
                (
                    SELECT 1 FROM AgAgentDefinition WITH (UPDLOCK, HOLDLOCK)
                    WHERE Id = @Id OR Code = @Code
                );
                """;
            AddDefinitionParameters(command, definition);
            if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await WriteVersionsAsync(connection, transaction, definition, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<bool> TryReplaceAsync(
        AgentDefinition definition,
        long expectedLogicalRevision,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (expectedLogicalRevision == long.MaxValue ||
            definition.LogicalRevision != expectedLogicalRevision + 1)
        {
            return false;
        }

        await using SqlConnection connection = await OpenConnectionAsync(cancellationToken);
        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            await using SqlCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                UPDATE AgAgentDefinition
                SET Name = @Name,
                    Description = @Description,
                    RuntimeStatus = @RuntimeStatus,
                    LogicalRevision = @LogicalRevision
                WHERE Id = @Id
                  AND Code = @Code
                  AND LogicalRevision = @ExpectedLogicalRevision;
                """;
            AddDefinitionParameters(command, definition);
            command.Parameters.Add("@ExpectedLogicalRevision", SqlDbType.BigInt).Value = expectedLogicalRevision;
            if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            IReadOnlyList<Guid> existingPublishedIds = await ReadPublishedVersionIdsAsync(
                connection, transaction, definition.Id, cancellationToken);
            if (existingPublishedIds.Count > definition.PublishedVersions.Count ||
                existingPublishedIds.Where((id, index) =>
                    definition.PublishedVersions[index].Id != id).Any())
            {
                throw new InvalidDataException(
                    "Published Agent versions are immutable and must remain an ordered prefix.");
            }

            await using SqlCommand delete = connection.CreateCommand();
            delete.Transaction = transaction;
            delete.CommandText =
                "DELETE FROM AgAgentVersion WHERE AgentId = @AgentId AND IsDraft = 1;";
            AddGuid(delete, "@AgentId", definition.Id);
            await delete.ExecuteNonQueryAsync(cancellationToken);

            await WriteVersionAsync(connection, transaction, definition.Id, 0,
                definition.Draft, cancellationToken);
            for (int index = existingPublishedIds.Count;
                 index < definition.PublishedVersions.Count;
                 index++)
            {
                await WriteVersionAsync(connection, transaction, definition.Id, index,
                    definition.PublishedVersions[index], cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task<IReadOnlyList<Guid>> ReadPublishedVersionIdsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid agentId,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT Id
            FROM AgAgentVersion WITH (UPDLOCK, HOLDLOCK)
            WHERE AgentId = @AgentId AND IsDraft = 0
            ORDER BY Ordinal;
            """;
        AddGuid(command, "@AgentId", agentId);
        var ids = new List<Guid>();
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ids.Add(ReadGuid(reader, 0));
        }
        return new ReadOnlyCollection<Guid>(ids);
    }

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken) =>
        await SqlServerAgentConnection.OpenAsync(_connectionString, cancellationToken);

    private static async Task WriteVersionsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        AgentDefinition definition,
        CancellationToken cancellationToken)
    {
        await WriteVersionAsync(connection, transaction, definition.Id, 0, definition.Draft, cancellationToken);
        for (int index = 0; index < definition.PublishedVersions.Count; index++)
        {
            await WriteVersionAsync(connection, transaction, definition.Id, index,
                definition.PublishedVersions[index], cancellationToken);
        }
    }

    private static async Task WriteVersionAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid agentId,
        int ordinal,
        AgentVersion version,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO AgAgentVersion
                (Id, AgentId, Ordinal, Label, IsDraft, Instructions, ModelProfileId,
                 OutputMode, OutputJsonSchema, OutputSchemaSha256)
            VALUES
                (@Id, @AgentId, @Ordinal, @Label, @IsDraft, @Instructions, @ModelProfileId,
                 @OutputMode, @OutputJsonSchema, @OutputSchemaSha256);
            """;
        AddGuid(command, "@Id", version.Id);
        AddGuid(command, "@AgentId", agentId);
        command.Parameters.Add("@Ordinal", SqlDbType.Int).Value = ordinal;
        command.Parameters.Add("@Label", SqlDbType.NVarChar, 128).Value = version.Label;
        command.Parameters.Add("@IsDraft", SqlDbType.Bit).Value = version.IsDraft;
        command.Parameters.Add("@Instructions", SqlDbType.NVarChar, -1).Value = version.Instructions;
        command.Parameters.Add("@ModelProfileId", SqlDbType.NVarChar, 256).Value = version.ModelProfileId;
        command.Parameters.Add("@OutputMode", SqlDbType.VarChar, 32).Value = version.OutputMode.ToString();
        command.Parameters.Add("@OutputJsonSchema", SqlDbType.NVarChar, -1).Value = DbValue(version.OutputJsonSchema);
        command.Parameters.Add("@OutputSchemaSha256", SqlDbType.Char, 64).Value = DbValue(version.OutputSchemaSha256);
        await command.ExecuteNonQueryAsync(cancellationToken);

        await WriteVersionBindingsAsync(connection, transaction, version, cancellationToken);
        if (version.Snapshot is not null)
        {
            await WriteSnapshotAsync(connection, transaction, version.Id, version.Snapshot, cancellationToken);
        }
    }

    private static async Task WriteVersionBindingsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        AgentVersion version,
        CancellationToken cancellationToken)
    {
        await WriteSimpleBindingsAsync(connection, transaction, version.Id, VersionScope, "Skill",
            version.SkillVersionIds, cancellationToken);
        await WriteSimpleBindingsAsync(connection, transaction, version.Id, VersionScope, "Tool",
            version.ToolVersionIds, cancellationToken);
        await WriteSimpleBindingsAsync(connection, transaction, version.Id, VersionScope, "KnowledgeBase",
            version.KnowledgeBaseIds, cancellationToken);

        IReadOnlyDictionary<Guid, AgentChildBindingSnapshot> childPins =
            version.ChildAgentPins.ToDictionary(value => value.AgentId);
        for (int index = 0; index < version.ChildAgentIds.Count; index++)
        {
            Guid id = version.ChildAgentIds[index];
            childPins.TryGetValue(id, out AgentChildBindingSnapshot? pin);
            await WriteBindingAsync(connection, transaction, version.Id, VersionScope, "ChildAgent",
                index, id, pin?.AgentVersionId, null, pin?.AgentCode, pin?.AgentName,
                pin?.AgentDescription, cancellationToken);
        }

        IReadOnlyDictionary<Guid, AgentOrchestrationBindingSnapshot> orchestrationPins =
            version.OrchestrationPins.ToDictionary(value => value.OrchestrationId);
        for (int index = 0; index < version.OrchestrationIds.Count; index++)
        {
            Guid id = version.OrchestrationIds[index];
            orchestrationPins.TryGetValue(id, out AgentOrchestrationBindingSnapshot? pin);
            await WriteBindingAsync(connection, transaction, version.Id, VersionScope, "Orchestration",
                index, id, pin?.OrchestrationVersionId, null, null, null, null, cancellationToken);
        }
    }

    private static async Task WriteSnapshotAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid versionId,
        AgentVersionSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO AgAgentVersionSnapshot
                (ID, VersionId, SnapshotVersionId, AgentCode, Instructions, ModelProfileId,
                 OutputMode, OutputJsonSchema, AgentName, AgentDescription)
            VALUES
                (@ID, @VersionId, @SnapshotVersionId, @AgentCode, @Instructions, @ModelProfileId,
                 @OutputMode, @OutputJsonSchema, @AgentName, @AgentDescription);
            """;
        AddGuid(command, "@ID", versionId);
        AddGuid(command, "@VersionId", versionId);
        AddGuid(command, "@SnapshotVersionId", snapshot.VersionId);
        command.Parameters.Add("@AgentCode", SqlDbType.NVarChar, 128).Value = snapshot.AgentCode;
        command.Parameters.Add("@Instructions", SqlDbType.NVarChar, -1).Value = snapshot.Instructions;
        command.Parameters.Add("@ModelProfileId", SqlDbType.NVarChar, 256).Value = snapshot.ModelProfileId;
        command.Parameters.Add("@OutputMode", SqlDbType.VarChar, 32).Value = snapshot.OutputMode.ToString();
        command.Parameters.Add("@OutputJsonSchema", SqlDbType.NVarChar, -1).Value = DbValue(snapshot.OutputJsonSchema);
        command.Parameters.Add("@AgentName", SqlDbType.NVarChar, 256).Value = DbValue(snapshot.AgentName);
        command.Parameters.Add("@AgentDescription", SqlDbType.NVarChar, -1).Value = DbValue(snapshot.AgentDescription);
        await command.ExecuteNonQueryAsync(cancellationToken);

        await WriteSimpleBindingsAsync(connection, transaction, versionId, SnapshotScope, "Skill",
            snapshot.Skills.Select(value => value.SkillVersionId).ToArray(), cancellationToken);
        await WriteSimpleBindingsAsync(connection, transaction, versionId, SnapshotScope, "Tool",
            snapshot.Tools.Select(value => value.ToolVersionId).ToArray(), cancellationToken);
        for (int index = 0; index < snapshot.KnowledgeBases.Count; index++)
        {
            AgentKnowledgeBindingSnapshot value = snapshot.KnowledgeBases[index];
            await WriteBindingAsync(connection, transaction, versionId, SnapshotScope, "KnowledgeBase",
                index, value.KnowledgeBaseId, null, value.LogicalRevision, null, null, null, cancellationToken);
        }
        for (int index = 0; index < snapshot.ChildAgents.Count; index++)
        {
            AgentChildBindingSnapshot value = snapshot.ChildAgents[index];
            await WriteBindingAsync(connection, transaction, versionId, SnapshotScope, "ChildAgent",
                index, value.AgentId, value.AgentVersionId, null, value.AgentCode, value.AgentName,
                value.AgentDescription, cancellationToken);
        }
        for (int index = 0; index < snapshot.Orchestrations.Count; index++)
        {
            AgentOrchestrationBindingSnapshot value = snapshot.Orchestrations[index];
            await WriteBindingAsync(connection, transaction, versionId, SnapshotScope, "Orchestration",
                index, value.OrchestrationId, value.OrchestrationVersionId, null,
                null, null, null, cancellationToken);
        }
    }

    private static async Task WriteSimpleBindingsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid versionId,
        string scope,
        string bindingType,
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken)
    {
        for (int index = 0; index < ids.Count; index++)
        {
            await WriteBindingAsync(connection, transaction, versionId, scope, bindingType,
                index, ids[index], null, null, null, null, null, cancellationToken);
        }
    }

    private static async Task WriteBindingAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid versionId,
        string scope,
        string bindingType,
        int ordinal,
        Guid referenceId,
        Guid? referenceVersionId,
        long? logicalRevision,
        string? referenceCode,
        string? referenceName,
        string? referenceDescription,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO AgAgentVersionBinding
                (ID, VersionId, Scope, BindingType, Ordinal, ReferenceId, ReferenceVersionId,
                 LogicalRevision, ReferenceCode, ReferenceName, ReferenceDescription)
            VALUES
                (@ID, @VersionId, @Scope, @BindingType, @Ordinal, @ReferenceId, @ReferenceVersionId,
                 @LogicalRevision, @ReferenceCode, @ReferenceName, @ReferenceDescription);
            """;
        AddGuid(command, "@ID", Guid.NewGuid());
        AddGuid(command, "@VersionId", versionId);
        command.Parameters.Add("@Scope", SqlDbType.VarChar, 16).Value = scope;
        command.Parameters.Add("@BindingType", SqlDbType.VarChar, 32).Value = bindingType;
        command.Parameters.Add("@Ordinal", SqlDbType.Int).Value = ordinal;
        AddGuid(command, "@ReferenceId", referenceId);
        command.Parameters.Add("@ReferenceVersionId", SqlDbType.UniqueIdentifier).Value = DbValue(referenceVersionId);
        command.Parameters.Add("@LogicalRevision", SqlDbType.BigInt).Value = DbValue(logicalRevision);
        command.Parameters.Add("@ReferenceCode", SqlDbType.NVarChar, 128).Value = DbValue(referenceCode);
        command.Parameters.Add("@ReferenceName", SqlDbType.NVarChar, 256).Value = DbValue(referenceName);
        command.Parameters.Add("@ReferenceDescription", SqlDbType.NVarChar, -1).Value = DbValue(referenceDescription);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddBinding(
        MutableVersion version,
        string scope,
        string bindingType,
        Guid referenceId,
        Guid? referenceVersionId,
        long? logicalRevision,
        string? referenceCode,
        string? referenceName,
        string? referenceDescription)
    {
        if (scope == VersionScope)
        {
            switch (bindingType)
            {
                case "Skill": version.SkillVersionIds.Add(referenceId); break;
                case "Tool": version.ToolVersionIds.Add(referenceId); break;
                case "KnowledgeBase": version.KnowledgeBaseIds.Add(referenceId); break;
                case "ChildAgent":
                    version.ChildAgentIds.Add(referenceId);
                    if (referenceVersionId is Guid childVersionId)
                    {
                        version.ChildAgentPins.Add(new AgentChildBindingSnapshot(referenceId, childVersionId)
                        {
                            AgentCode = referenceCode ?? string.Empty,
                            AgentName = referenceName,
                            AgentDescription = referenceDescription
                        });
                    }
                    break;
                case "Orchestration":
                    version.OrchestrationIds.Add(referenceId);
                    if (referenceVersionId is Guid orchestrationVersionId)
                    {
                        version.OrchestrationPins.Add(
                            new AgentOrchestrationBindingSnapshot(referenceId, orchestrationVersionId));
                    }
                    break;
                default: throw new InvalidDataException($"Unknown Agent binding type '{bindingType}'.");
            }
            return;
        }

        if (scope != SnapshotScope || version.Snapshot is null)
        {
            throw new InvalidDataException($"Unknown Agent binding scope '{scope}'.");
        }

        switch (bindingType)
        {
            case "Skill": version.Snapshot.Skills.Add(new AgentSkillBindingSnapshot(referenceId)); break;
            case "Tool": version.Snapshot.Tools.Add(new AgentToolBindingSnapshot(referenceId)); break;
            case "KnowledgeBase":
                version.Snapshot.KnowledgeBases.Add(new AgentKnowledgeBindingSnapshot(
                    referenceId,
                    logicalRevision ?? throw new InvalidDataException("A snapshot knowledge binding requires a revision.")));
                break;
            case "ChildAgent":
                version.Snapshot.ChildAgents.Add(new AgentChildBindingSnapshot(
                    referenceId,
                    referenceVersionId ?? throw new InvalidDataException("A snapshot child binding requires a version."))
                {
                    AgentCode = referenceCode ?? string.Empty,
                    AgentName = referenceName,
                    AgentDescription = referenceDescription
                });
                break;
            case "Orchestration":
                version.Snapshot.Orchestrations.Add(new AgentOrchestrationBindingSnapshot(
                    referenceId,
                    referenceVersionId ?? throw new InvalidDataException("A snapshot orchestration binding requires a version.")));
                break;
            default: throw new InvalidDataException($"Unknown Agent snapshot binding type '{bindingType}'.");
        }
    }

    private static AgentDefinition BuildDefinition(MutableDefinition value)
    {
        MutableVersion draft = value.Versions.Values.SingleOrDefault(version => version.IsDraft)
            ?? throw new InvalidDataException("The Agent does not have exactly one Draft version.");
        AgentVersion[] published = value.Versions.Values
            .Where(version => !version.IsDraft)
            .OrderBy(version => version.Ordinal)
            .Select(BuildVersion)
            .ToArray();
        return new AgentDefinition(
            value.Id, value.Code, value.Name, value.Description, value.RuntimeStatus,
            value.LogicalRevision, BuildVersion(draft),
            new ReadOnlyCollection<AgentVersion>(published));
    }

    private static AgentVersion BuildVersion(MutableVersion value) =>
        new(value.Id, value.Label, value.IsDraft, value.Instructions, value.ModelProfileId,
            value.OutputMode, value.OutputJsonSchema, value.OutputSchemaSha256,
            value.Snapshot is null ? null : BuildSnapshot(value.Snapshot))
        {
            SkillVersionIds = AgentContractCloner.ReadOnly(value.SkillVersionIds),
            ToolVersionIds = AgentContractCloner.ReadOnly(value.ToolVersionIds),
            KnowledgeBaseIds = AgentContractCloner.ReadOnly(value.KnowledgeBaseIds),
            ChildAgentIds = AgentContractCloner.ReadOnly(value.ChildAgentIds),
            OrchestrationIds = AgentContractCloner.ReadOnly(value.OrchestrationIds),
            ChildAgentPins = AgentContractCloner.ReadOnly(value.ChildAgentPins),
            OrchestrationPins = AgentContractCloner.ReadOnly(value.OrchestrationPins)
        };

    private static AgentVersionSnapshot BuildSnapshot(MutableSnapshot value) =>
        new(value.VersionId, value.AgentCode, value.Instructions, value.ModelProfileId,
            value.OutputMode, value.OutputJsonSchema,
            AgentContractCloner.ReadOnly(value.Skills), AgentContractCloner.ReadOnly(value.Tools))
        {
            AgentName = value.AgentName,
            AgentDescription = value.AgentDescription,
            KnowledgeBases = AgentContractCloner.ReadOnly(value.KnowledgeBases),
            ChildAgents = AgentContractCloner.ReadOnly(value.ChildAgents),
            Orchestrations = AgentContractCloner.ReadOnly(value.Orchestrations)
        };

    private static void AddDefinitionParameters(SqlCommand command, AgentDefinition definition)
    {
        AddGuid(command, "@Id", definition.Id);
        command.Parameters.Add("@Code", SqlDbType.NVarChar, 128).Value = definition.Code;
        command.Parameters.Add("@Name", SqlDbType.NVarChar, 256).Value = definition.Name;
        command.Parameters.Add("@Description", SqlDbType.NVarChar, -1).Value = definition.Description;
        command.Parameters.Add("@RuntimeStatus", SqlDbType.VarChar, 32).Value = definition.RuntimeStatus.ToString();
        command.Parameters.Add("@LogicalRevision", SqlDbType.BigInt).Value = definition.LogicalRevision;
    }

    private static T ParseEnum<T>(string value, string columnName) where T : struct, Enum =>
        Enum.TryParse(value, false, out T parsed)
            ? parsed
            : throw new InvalidDataException($"The Agent column {columnName} contains unsupported value '{value}'.");

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal);

    private static object DbValue(object? value) => value ?? DBNull.Value;

    private static void AddGuid(SqlCommand command, string name, Guid value) =>
        command.Parameters.Add(name, SqlDbType.UniqueIdentifier).Value = value;

    private static string? ReadNullableString(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static Guid? ReadNullableGuid(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : ReadGuid(reader, ordinal);

    private static Guid ReadGuid(SqlDataReader reader, int ordinal) => reader.GetValue(ordinal) switch
    {
        Guid guid => guid,
        string text when Guid.TryParse(text, out Guid guid) => guid,
        _ => throw new InvalidDataException(
            $"The SQL Server Agent ID in column {reader.GetName(ordinal)} is not a valid GUID.")
    };

    private sealed class MutableDefinition(
        Guid id,
        string code,
        string name,
        string description,
        AgentRuntimeStatus runtimeStatus,
        long logicalRevision)
    {
        public Guid Id { get; } = id;
        public string Code { get; } = code;
        public string Name { get; } = name;
        public string Description { get; } = description;
        public AgentRuntimeStatus RuntimeStatus { get; } = runtimeStatus;
        public long LogicalRevision { get; } = logicalRevision;
        public Dictionary<Guid, MutableVersion> Versions { get; } = [];
    }

    private sealed class MutableVersion(
        Guid id,
        int ordinal,
        string label,
        bool isDraft,
        string instructions,
        string modelProfileId,
        AgentOutputMode outputMode,
        string? outputJsonSchema,
        string? outputSchemaSha256)
    {
        public Guid Id { get; } = id;
        public int Ordinal { get; } = ordinal;
        public string Label { get; } = label;
        public bool IsDraft { get; } = isDraft;
        public string Instructions { get; } = instructions;
        public string ModelProfileId { get; } = modelProfileId;
        public AgentOutputMode OutputMode { get; } = outputMode;
        public string? OutputJsonSchema { get; } = outputJsonSchema;
        public string? OutputSchemaSha256 { get; } = outputSchemaSha256;
        public MutableSnapshot? Snapshot { get; set; }
        public List<Guid> SkillVersionIds { get; } = [];
        public List<Guid> ToolVersionIds { get; } = [];
        public List<Guid> KnowledgeBaseIds { get; } = [];
        public List<Guid> ChildAgentIds { get; } = [];
        public List<Guid> OrchestrationIds { get; } = [];
        public List<AgentChildBindingSnapshot> ChildAgentPins { get; } = [];
        public List<AgentOrchestrationBindingSnapshot> OrchestrationPins { get; } = [];
    }

    private sealed class MutableSnapshot(
        Guid versionId,
        string agentCode,
        string instructions,
        string modelProfileId,
        AgentOutputMode outputMode,
        string? outputJsonSchema,
        string? agentName,
        string? agentDescription)
    {
        public Guid VersionId { get; } = versionId;
        public string AgentCode { get; } = agentCode;
        public string Instructions { get; } = instructions;
        public string ModelProfileId { get; } = modelProfileId;
        public AgentOutputMode OutputMode { get; } = outputMode;
        public string? OutputJsonSchema { get; } = outputJsonSchema;
        public string? AgentName { get; } = agentName;
        public string? AgentDescription { get; } = agentDescription;
        public List<AgentSkillBindingSnapshot> Skills { get; } = [];
        public List<AgentToolBindingSnapshot> Tools { get; } = [];
        public List<AgentKnowledgeBindingSnapshot> KnowledgeBases { get; } = [];
        public List<AgentChildBindingSnapshot> ChildAgents { get; } = [];
        public List<AgentOrchestrationBindingSnapshot> Orchestrations { get; } = [];
    }
}
