using EU.Core.Agent.Application.Knowledge;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

#nullable enable

namespace EU.Core.Services;

/// <summary>
/// 鐭ヨ瘑搴撳畾涔夈€佹枃妗ｅ拰妫€绱㈠垎鍧楃殑瑙勮寖鍖栨寔涔呭寲鏈嶅姟銆?
/// </summary>
public sealed class AgKnowledgeBaseDefinitionServices :
    BaseServices<AgKnowledgeBaseDefinition>,
    IAgKnowledgeBaseDefinitionServices,
    IKnowledgeRetriever
{
    public const int MaximumDocumentCharacters = 1_000_000;
    public const int MaximumDocuments = 100;
    public const int MaximumPdfBytes = 10_485_760;
    public const int MaximumPdfPages = 200;

    private readonly Lazy<IAgentDefinitionCatalog>? agents;
    private readonly IKnowledgePdfTextExtractor? pdfTextExtractor;

    public AgKnowledgeBaseDefinitionServices(
        IBaseRepository<AgKnowledgeBaseDefinition> dal,
        Lazy<IAgentDefinitionCatalog>? agents = null,
        IKnowledgePdfTextExtractor? pdfTextExtractor = null)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
        this.agents = agents;
        this.pdfTextExtractor = pdfTextExtractor;
    }

    public async Task<ServiceResult<KnowledgeBaseDefinition>> CreateAsync(
        CreateKnowledgeBaseCommand command, CancellationToken cancellationToken = default)
    {
        string code = (command.Code ?? string.Empty).Trim().ToLowerInvariant();
        if (!Regex.IsMatch(code, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
        {
            return KnowledgeFailure(
                KnowledgeErrorCodes.CodeInvalid, "Knowledge base code must be lowercase kebab-case.");
        }
        var value = new KnowledgeBaseDefinition(
            Guid.NewGuid(), code, command.Name?.Trim() ?? string.Empty,
            command.Description?.Trim() ?? string.Empty, KnowledgeBaseStatus.Enabled,
            0, [], [], null);
        return await TryCreateAsync(value, cancellationToken)
            ? ServiceResult<KnowledgeBaseDefinition>.OprateSuccess(value)
            : KnowledgeFailure(
                KnowledgeErrorCodes.CodeConflict, "A knowledge base already uses this code.");
    }

    public async Task<ServiceResult<KnowledgeBaseDefinition>> UpdateAsync(
        UpdateKnowledgeBaseCommand command, CancellationToken cancellationToken = default)
    {
        KnowledgeBaseDefinition? existing = await GetByIdAsync(command.Id, cancellationToken);
        if (existing is null) return NotFound();
        if (existing.LogicalRevision != command.ExpectedLogicalRevision) return Conflict();
        if (existing.Status is KnowledgeBaseStatus.Archived || command.Status is KnowledgeBaseStatus.Archived)
        {
            return KnowledgeFailure(
                KnowledgeErrorCodes.LifecycleTransitionInvalid,
                "Use the archive operation to archive or restore a knowledge base.");
        }
        if (!Enum.IsDefined(command.Status))
        {
            return KnowledgeFailure(
                KnowledgeErrorCodes.DocumentInvalid, "Knowledge base status is invalid.");
        }
        KnowledgeBaseDefinition updated = existing with
        {
            Name = command.Name?.Trim() ?? string.Empty,
            Description = command.Description?.Trim() ?? string.Empty,
            Status = command.Status,
            LogicalRevision = existing.LogicalRevision + 1
        };
        return await TryReplaceAsync(updated, command.ExpectedLogicalRevision, cancellationToken)
            ? ServiceResult<KnowledgeBaseDefinition>.OprateSuccess(updated)
            : Conflict();
    }

    public async Task<ServiceResult<KnowledgeBaseDefinition>> ImportDocumentAsync(
        ImportKnowledgeDocumentCommand command, CancellationToken cancellationToken = default)
    {
        string mediaType = command.MediaType?.Trim().ToLowerInvariant() ?? string.Empty;
        string content = NormalizeContent(command.Content);
        if (mediaType is not ("text/plain" or "text/markdown"))
        {
            return InvalidDocument(
                $"Only non-empty text/plain and text/markdown documents up to {MaximumDocumentCharacters} characters are accepted by this endpoint.");
        }
        return await PersistDocumentAsync(
            command.KnowledgeBaseId, command.ExpectedLogicalRevision, command.FileName,
            mediaType, content, cancellationToken);
    }

    public async Task<ServiceResult<KnowledgeBaseDefinition>> ImportPdfDocumentAsync(
        ImportPdfKnowledgeDocumentCommand command, CancellationToken cancellationToken = default)
    {
        string fileName = Path.GetFileName(command.FileName?.Trim() ?? string.Empty);
        string mediaType = command.MediaType?.Trim().ToLowerInvariant() ?? string.Empty;
        ReadOnlyMemory<byte> bytes = command.Content;
        if (pdfTextExtractor is null || string.IsNullOrWhiteSpace(fileName)
            || !string.Equals(Path.GetExtension(fileName), ".pdf", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(mediaType, "application/pdf", StringComparison.Ordinal)
            || bytes.Length is 0 or > MaximumPdfBytes || !HasPdfSignature(bytes.Span))
        {
            return InvalidDocument($"Only PDF files up to {MaximumPdfBytes} bytes are accepted by this endpoint.");
        }
        KnowledgeBaseDefinition? target = await GetByIdAsync(command.KnowledgeBaseId, cancellationToken);
        ServiceResult<KnowledgeBaseDefinition>? targetError =
            ValidateImportTarget(target, command.ExpectedLogicalRevision);
        if (targetError is not null) return targetError;
        KnowledgePdfExtractionResult extraction = await pdfTextExtractor.ExtractAsync(
            bytes, MaximumPdfPages, MaximumDocumentCharacters, cancellationToken);
        if (!extraction.Succeeded) return InvalidDocument(PdfFailureMessage(extraction.Failure));
        return await PersistDocumentAsync(
            command.KnowledgeBaseId, command.ExpectedLogicalRevision, fileName,
            mediaType, NormalizeContent(extraction.Content), cancellationToken);
    }

    private async Task<ServiceResult<KnowledgeBaseDefinition>> PersistDocumentAsync(
        Guid knowledgeBaseId, long expectedLogicalRevision, string requestedFileName,
        string mediaType, string content, CancellationToken cancellationToken)
    {
        KnowledgeBaseDefinition? existing = await GetByIdAsync(knowledgeBaseId, cancellationToken);
        ServiceResult<KnowledgeBaseDefinition>? targetError =
            ValidateImportTarget(existing, expectedLogicalRevision);
        if (targetError is not null) return targetError;
        KnowledgeBaseDefinition current = existing!;
        string fileName = Path.GetFileName(requestedFileName?.Trim() ?? string.Empty);
        if (string.IsNullOrWhiteSpace(fileName) || content.Length is 0 or > MaximumDocumentCharacters
            || content.Contains('\0'))
        {
            return InvalidDocument(
                $"The extracted document must contain between 1 and {MaximumDocumentCharacters} safe text characters.");
        }
        Guid documentId = Guid.NewGuid();
        var document = new KnowledgeDocument(
            documentId, fileName, mediaType,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant(),
            content, DateTimeOffset.UtcNow);
        IReadOnlyList<KnowledgeChunk> chunks = KnowledgeTextChunker.Chunk(documentId, content);
        KnowledgeBaseDefinition updated = current with
        {
            LogicalRevision = current.LogicalRevision + 1,
            Documents =   Common.Extensions.CollectionExtensions.ToReadOnlyList(current.Documents.Append(document)),
            Chunks =  Common.Extensions.CollectionExtensions.ToReadOnlyList(current.Chunks.Concat(chunks)),
            IndexedAtUtc = DateTimeOffset.UtcNow
        };
        return await TryReplaceAsync(updated, expectedLogicalRevision, cancellationToken)
            ? ServiceResult<KnowledgeBaseDefinition>.OprateSuccess(updated)
            : Conflict();
    }

    public async Task<ServiceResult<KnowledgeBaseDefinition>> SetArchivedAsync(
        SetKnowledgeBaseArchiveCommand command, CancellationToken cancellationToken = default)
    {
        KnowledgeBaseDefinition? existing = await GetByIdAsync(command.Id, cancellationToken);
        if (existing is null) return NotFound();
        if (existing.LogicalRevision != command.ExpectedLogicalRevision) return Conflict();
        if (command.Archived && existing.Status is not KnowledgeBaseStatus.Disabled)
        {
            return KnowledgeFailure(
                KnowledgeErrorCodes.LifecycleTransitionInvalid,
                "A knowledge base must be disabled before it can be archived.");
        }
        if (!command.Archived && existing.Status is not KnowledgeBaseStatus.Archived)
        {
            return KnowledgeFailure(
                KnowledgeErrorCodes.LifecycleTransitionInvalid,
                "Only an archived knowledge base can be restored.");
        }
        if (command.Archived && agents is not null)
        {
            IReadOnlyList<AgentDefinition> enabledAgents = await agents.Value.ListDefinitionsAsync(
                new AgentDefinitionQuery(RuntimeStatus: AgentRuntimeStatus.Enabled), cancellationToken);
            string[] blockers = enabledAgents
                .Where(value => value.PublishedVersions.LastOrDefault()?.Snapshot?.KnowledgeBases
                    .Any(binding => binding.KnowledgeBaseId == existing.Id) == true)
                .Select(value => value.Code).Take(8).ToArray();
            if (blockers.Length > 0)
            {
                return KnowledgeFailure(
                    KnowledgeErrorCodes.ArchiveBlocked,
                    $"The knowledge base is still referenced by Agent(s): {string.Join(", ", blockers)}.");
            }
        }
        KnowledgeBaseDefinition updated = existing with
        {
            Status = command.Archived ? KnowledgeBaseStatus.Archived : KnowledgeBaseStatus.Disabled,
            LogicalRevision = existing.LogicalRevision + 1
        };
        return await TryReplaceAsync(updated, existing.LogicalRevision, cancellationToken)
            ? ServiceResult<KnowledgeBaseDefinition>.OprateSuccess(updated)
            : Conflict();
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

    private async Task<bool> TryCreateAsync(
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

    private async Task<bool> TryReplaceAsync(
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

    public async Task<IReadOnlyList<PublishedKnowledgeReference>> ListPublishedAsync(
        CancellationToken cancellationToken = default)
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
        return  Common.Extensions.CollectionExtensions.ToReadOnlyList(definitions
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
        return   Common.Extensions.CollectionExtensions.ToReadOnlyList(rows
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
        return  Common.Extensions.CollectionExtensions.ToReadOnlyList(definitions.Select(definition => MapDefinition(
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
         Common.Extensions.CollectionExtensions.ToReadOnlyList(documents
                .OrderBy(value => Required(value.Ordinal, "Document.Ordinal"))
                .ThenBy(value => value.ID)
                .Select(MapDocument)),
        Common.Extensions.CollectionExtensions.ToReadOnlyList(chunks
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

    private static string NormalizeContent(string? content) =>
        (content ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n').Trim();

    private static bool HasPdfSignature(ReadOnlySpan<byte> content) =>
        content.Length >= 5 && content[0] == (byte)'%' && content[1] == (byte)'P'
        && content[2] == (byte)'D' && content[3] == (byte)'F' && content[4] == (byte)'-';

    private static string PdfFailureMessage(KnowledgePdfExtractionFailure failure) => failure switch
    {
        KnowledgePdfExtractionFailure.Encrypted =>
            "Encrypted or password-protected PDF files are not accepted.",
        KnowledgePdfExtractionFailure.PageLimitExceeded =>
            $"The PDF exceeds the {MaximumPdfPages}-page safety limit.",
        KnowledgePdfExtractionFailure.TextLimitExceeded =>
            $"The PDF contains more than {MaximumDocumentCharacters} extracted text characters.",
        KnowledgePdfExtractionFailure.NoExtractableText =>
            "The PDF contains no extractable text; scanned-image OCR is not enabled.",
        _ => "The PDF is malformed or could not be read safely."
    };

    private static ServiceResult<KnowledgeBaseDefinition>? ValidateImportTarget(
        KnowledgeBaseDefinition? existing, long expectedLogicalRevision)
    {
        if (existing is null) return NotFound();
        if (existing.LogicalRevision != expectedLogicalRevision) return Conflict();
        if (existing.Status is KnowledgeBaseStatus.Archived)
        {
            return KnowledgeFailure(
                KnowledgeErrorCodes.LifecycleTransitionInvalid,
                "An archived knowledge base must be restored before documents can be imported.");
        }
        return existing.Documents.Count >= MaximumDocuments
            ? InvalidDocument($"A knowledge base accepts at most {MaximumDocuments} documents.") : null;
    }

    private static ServiceResult<KnowledgeBaseDefinition> KnowledgeFailure(
        string errorCode,
        string message) =>
        ServiceResult<KnowledgeBaseDefinition>.Failure(
            KnowledgeServiceStatusCodes.FromErrorCode(errorCode),
            message);

    private static ServiceResult<KnowledgeBaseDefinition> InvalidDocument(string message) =>
        KnowledgeFailure(KnowledgeErrorCodes.DocumentInvalid, message);

    private static ServiceResult<KnowledgeBaseDefinition> NotFound() =>
        KnowledgeFailure(
            KnowledgeErrorCodes.NotFound, "The knowledge base was not found.");

    private static ServiceResult<KnowledgeBaseDefinition> Conflict() =>
        KnowledgeFailure(
            KnowledgeErrorCodes.RowVersionConflict, "The knowledge base changed; reload and retry.");

    private static class KnowledgeTextChunker
    {
        private const int TargetCharacters = 1200;
        private const int OverlapCharacters = 160;

        public static IReadOnlyList<KnowledgeChunk> Chunk(Guid documentId, string content)
        {
            var values = new List<KnowledgeChunk>();
            int start = 0;
            int sequence = 0;
            while (start < content.Length)
            {
                int end = Math.Min(start + TargetCharacters, content.Length);
                if (end < content.Length)
                {
                    int boundary = content.LastIndexOf('\n', end - 1, end - start);
                    if (boundary > start + TargetCharacters / 2)
                    {
                        end = boundary;
                    }
                }

                string value = content[start..end].Trim();
                if (value.Length > 0)
                {
                    values.Add(new KnowledgeChunk(Guid.NewGuid(), documentId, sequence++, value));
                }

                if (end >= content.Length)
                {
                    break;
                }

                start = Math.Max(start + 1, end - OverlapCharacters);
            }

            return  Common.Extensions.CollectionExtensions.ToReadOnlyList(values);
        }
    }

    private static class KnowledgeLexicalSearch
    {
        public static HashSet<string> Terms(string value)
        {
            string normalized = value.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
            var terms = new HashSet<string>(StringComparer.Ordinal);
            var word = new StringBuilder();
            var cjk = new StringBuilder();

            void FlushWord()
            {
                if (word.Length > 1) terms.Add(word.ToString());
                word.Clear();
            }

            void FlushCjk()
            {
                if (cjk.Length == 1) terms.Add(cjk.ToString());
                for (int i = 0; i + 1 < cjk.Length; i++) terms.Add(cjk.ToString(i, 2));
                cjk.Clear();
            }

            foreach (char character in normalized)
            {
                UnicodeCategory category = char.GetUnicodeCategory(character);
                bool isCjk = character is >= '\u3400' and <= '\u9fff';
                if (isCjk)
                {
                    FlushWord();
                    cjk.Append(character);
                }
                else if (char.IsLetterOrDigit(character) || category == UnicodeCategory.ConnectorPunctuation)
                {
                    FlushCjk();
                    word.Append(character);
                }
                else
                {
                    FlushWord();
                    FlushCjk();
                }
            }

            FlushWord();
            FlushCjk();
            return terms;
        }

        public static double Score(string content, HashSet<string> queryTerms)
        {
            if (queryTerms.Count == 0) return 0;
            HashSet<string> contentTerms = Terms(content);
            int matches = queryTerms.Count(contentTerms.Contains);
            return matches == 0
                ? 0
                : matches / Math.Sqrt(queryTerms.Count * (double)Math.Max(1, contentTerms.Count));
        }
    }

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

