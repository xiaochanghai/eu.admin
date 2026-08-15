using EU.Core.Agent.Application.Knowledge;

#nullable enable

namespace EU.Core.Services;

/// <summary>
/// 知识库定义、文档和检索分块的规范化持久化服务。
/// </summary>
public sealed class AgKnowledgeBaseDefinitionServices :
    BaseServices<AgKnowledgeBaseDefinition>,
    IAgKnowledgeBaseDefinitionServices,
    IKnowledgeBaseRepository,
    IPublishedKnowledgeCatalog,
    IKnowledgeRetriever
{
    public AgKnowledgeBaseDefinitionServices(IBaseRepository<AgKnowledgeBaseDefinition> dal)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
    }

    public async Task<KnowledgeBaseDefinition?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AgKnowledgeBaseDefinition? definition = await Db.Queryable<AgKnowledgeBaseDefinition>()
            .Where(value => value.ID == id && !value.IsDeleted)
            .FirstAsync();
        return definition is null
            ? null
            : await LoadDefinitionAsync(definition, cancellationToken);
    }

    public async Task<KnowledgeBaseDefinition?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AgKnowledgeBaseDefinition? definition = await Db.Queryable<AgKnowledgeBaseDefinition>()
            .Where(value => value.Code == code && !value.IsDeleted)
            .FirstAsync();
        return definition is null
            ? null
            : await LoadDefinitionAsync(definition, cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeBaseDefinition>> ListAsync(
        KnowledgeBaseQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        var expression = Db.Queryable<AgKnowledgeBaseDefinition>()
            .Where(value => !value.IsDeleted);
        if (query.Status.HasValue)
        {
            string status = query.Status.Value.ToString();
            expression = expression.Where(value => value.Status == status);
        }
        else
        {
            expression = expression.Where(value => value.Status != nameof(KnowledgeBaseStatus.Archived));
        }

        List<AgKnowledgeBaseDefinition> definitions = await expression
            .OrderBy(value => value.Code)
            .OrderBy(value => value.ID)
            .ToListAsync();
        return await LoadDefinitionsAsync(definitions, cancellationToken);
    }

    public async Task<bool> TryCreateAsync(
        KnowledgeBaseDefinition value,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            bool exists = await Db.Queryable<AgKnowledgeBaseDefinition>()
                .Where(candidate =>
                    !candidate.IsDeleted &&
                    (candidate.ID == value.Id || candidate.Code == value.Code))
                .AnyAsync();
            if (exists)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            await Db.Insertable(MapDefinitionEntity(value)).ExecuteCommandAsync();
            await InsertDocumentsAndChunksAsync(value, cancellationToken);
            await Db.Ado.CommitTranAsync();
            return true;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<bool> TryReplaceAsync(
        KnowledgeBaseDefinition value,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (expectedLogicalRevision == long.MaxValue ||
            value.LogicalRevision != expectedLogicalRevision + 1)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            HashSet<Guid> existingDocumentIds = (await Db.Queryable<AgKnowledgeDocument>()
                    .Where(candidate => candidate.KnowledgeBaseId == value.Id)
                    .Select(candidate => candidate.ID)
                    .ToListAsync())
                .ToHashSet();
            HashSet<Guid> existingChunkIds = (await Db.Queryable<AgKnowledgeChunk>()
                    .Where(candidate => candidate.KnowledgeBaseId == value.Id)
                    .Select(candidate => candidate.ID)
                    .ToListAsync())
                .ToHashSet();
            HashSet<Guid> requestedDocumentIds = value.Documents
                .Select(document => document.Id)
                .ToHashSet();
            HashSet<Guid> requestedChunkIds = value.Chunks
                .Select(chunk => chunk.Id)
                .ToHashSet();
            if (!existingDocumentIds.IsSubsetOf(requestedDocumentIds) ||
                !existingChunkIds.IsSubsetOf(requestedChunkIds))
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            AgKnowledgeBaseDefinition entity = MapDefinitionEntity(value);
            int updated = await Db.Updateable(entity)
                .UpdateColumns(candidate => new
                {
                    candidate.Name,
                    candidate.Description,
                    candidate.Status,
                    candidate.LogicalRevision,
                    candidate.IndexedAtUtc
                })
                .Where(candidate =>
                    candidate.ID == value.Id &&
                    candidate.Code == value.Code &&
                    candidate.LogicalRevision == expectedLogicalRevision &&
                    !candidate.IsDeleted)
                .ExecuteCommandAsync();
            if (updated != 1)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            await InsertDocumentsAndChunksAsync(
                value,
                cancellationToken,
                existingDocumentIds,
                existingChunkIds);
            await Db.Ado.CommitTranAsync();
            return true;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    async Task<IReadOnlyList<PublishedKnowledgeReference>> IPublishedKnowledgeCatalog.ListAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<AgKnowledgeBaseDefinition> definitions = await Db.Queryable<AgKnowledgeBaseDefinition>()
            .Where(definition =>
                !definition.IsDeleted &&
                definition.Status == nameof(KnowledgeBaseStatus.Enabled))
            .OrderBy(definition => definition.Code)
            .OrderBy(definition => definition.ID)
            .ToListAsync();
        if (definitions.Count == 0)
        {
            return [];
        }

        Guid[] definitionIds = definitions.Select(value => value.ID).ToArray();
        List<Guid> populatedIds = await Db.Queryable<AgKnowledgeChunk>()
            .Where(chunk =>
                chunk.KnowledgeBaseId.HasValue &&
                definitionIds.Contains(chunk.KnowledgeBaseId.Value) &&
                !chunk.IsDeleted)
            .Select(chunk => chunk.KnowledgeBaseId!.Value)
            .Distinct()
            .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();
        HashSet<Guid> populated = populatedIds.ToHashSet();
        return KnowledgeContractCloner.ReadOnly(definitions
            .Where(definition => populated.Contains(definition.ID))
            .Select(definition =>
            new PublishedKnowledgeReference(
                definition.ID,
                Required(definition.Code, "Code"),
                Required(definition.Name, "Name"),
                Required(definition.LogicalRevision, "LogicalRevision"))));
    }

    public async Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        IReadOnlyList<Guid> knowledgeBaseIds,
        string query,
        int take,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(knowledgeBaseIds);
        cancellationToken.ThrowIfCancellationRequested();
        if (knowledgeBaseIds.Count == 0)
        {
            return [];
        }

        Guid[] ids = knowledgeBaseIds.Distinct().ToArray();
        var rows = await Db
            .Queryable<AgKnowledgeBaseDefinition, AgKnowledgeDocument, AgKnowledgeChunk>(
                (definition, document, chunk) => new JoinQueryInfos(
                    JoinType.Inner, definition.ID == document.KnowledgeBaseId,
                    JoinType.Inner,
                    document.ID == chunk.DocumentId &&
                    definition.ID == chunk.KnowledgeBaseId))
            .Where((definition, document, chunk) =>
                ids.Contains(definition.ID) &&
                definition.Status == nameof(KnowledgeBaseStatus.Enabled) &&
                !definition.IsDeleted &&
                !document.IsDeleted &&
                !chunk.IsDeleted)
            .Select((definition, document, chunk) => new KnowledgeSearchRow
            {
                KnowledgeBaseId = definition.ID,
                KnowledgeBaseCode = definition.Code,
                DocumentId = document.ID,
                FileName = document.FileName,
                ChunkId = chunk.ID,
                ChunkSequence = chunk.Sequence,
                Content = chunk.Content
            })
            .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();

        HashSet<string> terms = KnowledgeLexicalSearch.Terms(query);
        return KnowledgeContractCloner.ReadOnly(rows
            .Select(row => new
            {
                Row = row,
                Score = KnowledgeLexicalSearch.Score(Required(row.Content, "Chunk.Content"), terms)
            })
            .Where(value => value.Score > 0)
            .Select(value => new KnowledgeSearchResult(
                value.Row.KnowledgeBaseId,
                Required(value.Row.KnowledgeBaseCode, "KnowledgeBase.Code"),
                value.Row.DocumentId,
                Required(value.Row.FileName, "Document.FileName"),
                value.Row.ChunkId,
                Required(value.Row.ChunkSequence, "Chunk.Sequence"),
                Required(value.Row.Content, "Chunk.Content"),
                value.Score))
            .OrderByDescending(value => value.Score)
            .ThenBy(value => value.KnowledgeBaseCode, StringComparer.Ordinal)
            .ThenBy(value => value.FileName, StringComparer.Ordinal)
            .ThenBy(value => value.ChunkSequence)
            .Take(Math.Clamp(take, 1, 20)));
    }

    private async Task<KnowledgeBaseDefinition> LoadDefinitionAsync(
        AgKnowledgeBaseDefinition definition,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<KnowledgeBaseDefinition> values = await LoadDefinitionsAsync(
            [definition],
            cancellationToken);
        return values[0];
    }

    private async Task<IReadOnlyList<KnowledgeBaseDefinition>> LoadDefinitionsAsync(
        IReadOnlyList<AgKnowledgeBaseDefinition> definitions,
        CancellationToken cancellationToken)
    {
        if (definitions.Count == 0)
        {
            return [];
        }

        Guid[] ids = definitions.Select(value => value.ID).ToArray();
        List<AgKnowledgeDocument> documents = await Db.Queryable<AgKnowledgeDocument>()
            .Where(value =>
                value.KnowledgeBaseId.HasValue &&
                ids.Contains(value.KnowledgeBaseId.Value) &&
                !value.IsDeleted)
            .OrderBy(value => value.KnowledgeBaseId)
            .OrderBy(value => value.Ordinal)
            .OrderBy(value => value.ID)
            .ToListAsync();
        List<AgKnowledgeChunk> chunks = await Db.Queryable<AgKnowledgeChunk>()
            .Where(value =>
                value.KnowledgeBaseId.HasValue &&
                ids.Contains(value.KnowledgeBaseId.Value) &&
                !value.IsDeleted)
            .OrderBy(value => value.KnowledgeBaseId)
            .OrderBy(value => value.DocumentId)
            .OrderBy(value => value.Sequence)
            .OrderBy(value => value.ID)
            .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyDictionary<Guid, AgKnowledgeDocument[]> documentsByDefinition = documents
            .GroupBy(value => Required(value.KnowledgeBaseId, "Document.KnowledgeBaseId"))
            .ToDictionary(group => group.Key, group => group.ToArray());
        IReadOnlyDictionary<Guid, AgKnowledgeChunk[]> chunksByDefinition = chunks
            .GroupBy(value => Required(value.KnowledgeBaseId, "Chunk.KnowledgeBaseId"))
            .ToDictionary(group => group.Key, group => group.ToArray());
        return KnowledgeContractCloner.ReadOnly(definitions.Select(definition => MapDefinition(
            definition,
            documentsByDefinition.GetValueOrDefault(definition.ID) ?? [],
            chunksByDefinition.GetValueOrDefault(definition.ID) ?? [])));
    }

    private async Task InsertDocumentsAndChunksAsync(
        KnowledgeBaseDefinition definition,
        CancellationToken cancellationToken,
        IReadOnlySet<Guid>? existingDocumentIds = null,
        IReadOnlySet<Guid>? existingChunkIds = null)
    {
        List<AgKnowledgeDocument> documents = definition.Documents
            .Select((document, ordinal) => MapDocumentEntity(definition.Id, document, ordinal))
            .Where(document => existingDocumentIds is null || !existingDocumentIds.Contains(document.ID))
            .ToList();
        if (documents.Count > 0)
        {
            await Db.Insertable(documents).ExecuteCommandAsync();
        }

        List<AgKnowledgeChunk> chunks = definition.Chunks
            .Select(chunk => MapChunkEntity(definition.Id, chunk))
            .Where(chunk => existingChunkIds is null || !existingChunkIds.Contains(chunk.ID))
            .ToList();
        if (chunks.Count > 0)
        {
            await Db.Insertable(chunks).ExecuteCommandAsync();
        }
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static KnowledgeBaseDefinition MapDefinition(
        AgKnowledgeBaseDefinition definition,
        IReadOnlyList<AgKnowledgeDocument> documents,
        IReadOnlyList<AgKnowledgeChunk> chunks)
    {
        IReadOnlyDictionary<Guid, int> documentOrdinals = documents.ToDictionary(
            value => value.ID,
            value => Required(value.Ordinal, "Document.Ordinal"));
        return new(
            definition.ID,
            Required(definition.Code, "Code"),
            Required(definition.Name, "Name"),
            Required(definition.Description, "Description"),
            ParseStatus(definition.Status),
            Required(definition.LogicalRevision, "LogicalRevision"),
            KnowledgeContractCloner.ReadOnly(documents
                .OrderBy(value => Required(value.Ordinal, "Document.Ordinal"))
                .ThenBy(value => value.ID)
                .Select(MapDocument)),
            KnowledgeContractCloner.ReadOnly(chunks
                .OrderBy(value => documentOrdinals.GetValueOrDefault(
                    Required(value.DocumentId, "Chunk.DocumentId"),
                    int.MaxValue))
                .ThenBy(value => Required(value.Sequence, "Chunk.Sequence"))
                .ThenBy(value => value.ID)
                .Select(MapChunk)),
            ToDateTimeOffset(definition.IndexedAtUtc));
    }

    private static AgKnowledgeBaseDefinition MapDefinitionEntity(KnowledgeBaseDefinition value) =>
        new()
        {
            ID = value.Id,
            Code = value.Code,
            Name = value.Name,
            Description = value.Description,
            Status = value.Status.ToString(),
            LogicalRevision = value.LogicalRevision,
            IndexedAtUtc = value.IndexedAtUtc?.UtcDateTime,
            IsDeleted = false,
            IsActive = true
        };

    private static KnowledgeDocument MapDocument(AgKnowledgeDocument value) =>
        new(
            value.ID,
            Required(value.FileName, "Document.FileName"),
            Required(value.MediaType, "Document.MediaType"),
            Required(value.Sha256, "Document.Sha256"),
            Required(value.Content, "Document.Content"),
            RequiredDateTimeOffset(value.ImportedAtUtc, "Document.ImportedAtUtc"));

    private static AgKnowledgeDocument MapDocumentEntity(
        Guid knowledgeBaseId,
        KnowledgeDocument value,
        int ordinal) =>
        new()
        {
            ID = value.Id,
            KnowledgeBaseId = knowledgeBaseId,
            Ordinal = ordinal,
            FileName = value.FileName,
            MediaType = value.MediaType,
            Sha256 = value.Sha256,
            Content = value.Content,
            ImportedAtUtc = value.ImportedAtUtc.UtcDateTime,
            IsDeleted = false,
            IsActive = true
        };

    private static KnowledgeChunk MapChunk(AgKnowledgeChunk value) =>
        new(
            value.ID,
            Required(value.DocumentId, "Chunk.DocumentId"),
            Required(value.Sequence, "Chunk.Sequence"),
            Required(value.Content, "Chunk.Content"));

    private static AgKnowledgeChunk MapChunkEntity(Guid knowledgeBaseId, KnowledgeChunk value) =>
        new()
        {
            ID = value.Id,
            KnowledgeBaseId = knowledgeBaseId,
            DocumentId = value.DocumentId,
            Sequence = value.Sequence,
            Content = value.Content,
            IsDeleted = false,
            IsActive = true
        };

    private static KnowledgeBaseStatus ParseStatus(string? value) =>
        Enum.TryParse(value, ignoreCase: false, out KnowledgeBaseStatus status) && Enum.IsDefined(status)
            ? status
            : throw new InvalidDataException($"Knowledge base status '{value}' is invalid.");

    private static DateTimeOffset? ToDateTimeOffset(DateTime? value) =>
        value.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
            : null;

    private static DateTimeOffset RequiredDateTimeOffset(DateTime? value, string field) =>
        value.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
            : throw new InvalidDataException($"Knowledge base field '{field}' is missing.");

    private static T Required<T>(T? value, string field) where T : struct =>
        value ?? throw new InvalidDataException($"Knowledge base field '{field}' is missing.");

    private static string Required(string? value, string field) =>
        value ?? throw new InvalidDataException($"Knowledge base field '{field}' is missing.");

    private sealed class KnowledgeSearchRow
    {
        public Guid KnowledgeBaseId { get; set; }
        public string? KnowledgeBaseCode { get; set; }
        public Guid DocumentId { get; set; }
        public string? FileName { get; set; }
        public Guid ChunkId { get; set; }
        public int? ChunkSequence { get; set; }
        public string? Content { get; set; }
    }
}
