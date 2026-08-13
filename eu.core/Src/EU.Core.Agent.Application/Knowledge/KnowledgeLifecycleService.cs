using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using EU.Core.Agent.Application.Agents;
using EU.Core.Model.ViewModels.Extend;

namespace EU.Core.Agent.Application.Knowledge;

public sealed class KnowledgeLifecycleService(
    IKnowledgeBaseRepository repository,
    IKnowledgeRetriever retriever,
    IAgentRepository? agents = null,
    IKnowledgePdfTextExtractor? pdfTextExtractor = null)
{
    public const int MaximumDocumentCharacters = 1_000_000;
    public const int MaximumDocuments = 100;
    public const int MaximumPdfBytes = 10_485_760;
    public const int MaximumPdfPages = 200;

    public async Task<KnowledgeOperationResult<KnowledgeBaseDefinition>> CreateAsync(
        CreateKnowledgeBaseCommand command,
        CancellationToken cancellationToken = default)
    {
        string code = (command.Code ?? string.Empty).Trim().ToLowerInvariant();
        if (!Regex.IsMatch(code, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
        {
            return KnowledgeOperationResult<KnowledgeBaseDefinition>.Failure(
                KnowledgeErrorCodes.CodeInvalid,
                "Knowledge base code must be lowercase kebab-case.");
        }

        var value = new KnowledgeBaseDefinition(
            Guid.NewGuid(),
            code,
            command.Name?.Trim() ?? string.Empty,
            command.Description?.Trim() ?? string.Empty,
            KnowledgeBaseStatus.Enabled,
            0,
            KnowledgeContractCloner.ReadOnly(Array.Empty<KnowledgeDocument>()),
            KnowledgeContractCloner.ReadOnly(Array.Empty<KnowledgeChunk>()),
            null);
        return await repository.TryCreateAsync(value, cancellationToken)
            ? KnowledgeOperationResult<KnowledgeBaseDefinition>.Success(value)
            : KnowledgeOperationResult<KnowledgeBaseDefinition>.Failure(
                KnowledgeErrorCodes.CodeConflict,
                "A knowledge base already uses this code.");
    }

    public async Task<KnowledgeOperationResult<KnowledgeBaseDefinition>> UpdateAsync(
        UpdateKnowledgeBaseCommand command,
        CancellationToken cancellationToken = default)
    {
        KnowledgeBaseDefinition? existing =
            await repository.GetByIdAsync(command.Id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        if (existing.LogicalRevision != command.ExpectedLogicalRevision)
        {
            return Conflict();
        }

        if (existing.Status is KnowledgeBaseStatus.Archived ||
            command.Status is KnowledgeBaseStatus.Archived)
        {
            return KnowledgeOperationResult<KnowledgeBaseDefinition>.Failure(
                KnowledgeErrorCodes.LifecycleTransitionInvalid,
                "Use the archive operation to archive or restore a knowledge base.");
        }

        if (!Enum.IsDefined(command.Status))
        {
            return KnowledgeOperationResult<KnowledgeBaseDefinition>.Failure(
                KnowledgeErrorCodes.DocumentInvalid,
                "Knowledge base status is invalid.");
        }

        KnowledgeBaseDefinition updated = existing with
        {
            Name = command.Name?.Trim() ?? string.Empty,
            Description = command.Description?.Trim() ?? string.Empty,
            Status = command.Status,
            LogicalRevision = existing.LogicalRevision + 1
        };
        return await repository.TryReplaceAsync(updated, command.ExpectedLogicalRevision, cancellationToken)
            ? KnowledgeOperationResult<KnowledgeBaseDefinition>.Success(updated)
            : Conflict();
    }

    public async Task<KnowledgeOperationResult<KnowledgeBaseDefinition>> ImportDocumentAsync(
        ImportKnowledgeDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        string mediaType = command.MediaType?.Trim().ToLowerInvariant() ?? string.Empty;
        string content = NormalizeContent(command.Content);
        if (mediaType is not ("text/plain" or "text/markdown"))
        {
            return InvalidDocument(
                $"Only non-empty text/plain and text/markdown documents up to {MaximumDocumentCharacters} characters are accepted by this endpoint.");
        }

        return await PersistDocumentAsync(
            command.KnowledgeBaseId,
            command.ExpectedLogicalRevision,
            command.FileName,
            mediaType,
            content,
            cancellationToken);
    }

    public async Task<KnowledgeOperationResult<KnowledgeBaseDefinition>> ImportPdfDocumentAsync(
        ImportPdfKnowledgeDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        string fileName = Path.GetFileName(command.FileName?.Trim() ?? string.Empty);
        string mediaType = command.MediaType?.Trim().ToLowerInvariant() ?? string.Empty;
        ReadOnlyMemory<byte> bytes = command.Content;
        if (pdfTextExtractor is null
            || string.IsNullOrWhiteSpace(fileName)
            || !string.Equals(Path.GetExtension(fileName), ".pdf", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(mediaType, "application/pdf", StringComparison.Ordinal)
            || bytes.Length is 0 or > MaximumPdfBytes
            || !HasPdfSignature(bytes.Span))
        {
            return InvalidDocument(
                $"Only PDF files up to {MaximumPdfBytes} bytes are accepted by this endpoint.");
        }

        KnowledgeBaseDefinition? target =
            await repository.GetByIdAsync(command.KnowledgeBaseId, cancellationToken);
        KnowledgeOperationResult<KnowledgeBaseDefinition>? targetError =
            ValidateImportTarget(target, command.ExpectedLogicalRevision);
        if (targetError is not null)
        {
            return targetError;
        }
        KnowledgePdfExtractionResult extraction = await pdfTextExtractor.ExtractAsync(
            bytes,
            MaximumPdfPages,
            MaximumDocumentCharacters,
            cancellationToken);
        if (!extraction.Succeeded)
        {
            return InvalidDocument(PdfFailureMessage(extraction.Failure));
        }

        return await PersistDocumentAsync(
            command.KnowledgeBaseId,
            command.ExpectedLogicalRevision,
            fileName,
            mediaType,
            NormalizeContent(extraction.Content),
            cancellationToken);
    }

    private async Task<KnowledgeOperationResult<KnowledgeBaseDefinition>> PersistDocumentAsync(
        Guid knowledgeBaseId,
        long expectedLogicalRevision,
        string requestedFileName,
        string mediaType,
        string content,
        CancellationToken cancellationToken)
    {
        KnowledgeBaseDefinition? existing =
            await repository.GetByIdAsync(knowledgeBaseId, cancellationToken);
        KnowledgeOperationResult<KnowledgeBaseDefinition>? targetError =
            ValidateImportTarget(existing, expectedLogicalRevision);
        if (targetError is not null)
        {
            return targetError;
        }
        KnowledgeBaseDefinition current = existing!;

        string fileName = Path.GetFileName(requestedFileName?.Trim() ?? string.Empty);
        if (string.IsNullOrWhiteSpace(fileName)
            || content.Length is 0 or > MaximumDocumentCharacters
            || content.Contains('\0'))
        {
            return InvalidDocument(
                $"The extracted document must contain between 1 and {MaximumDocumentCharacters} safe text characters.");
        }

        Guid documentId = Guid.NewGuid();
        var document = new KnowledgeDocument(
            documentId,
            fileName,
            mediaType,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant(),
            content,
            DateTimeOffset.UtcNow);
        IReadOnlyList<KnowledgeChunk> chunks = KnowledgeTextChunker.Chunk(documentId, content);
        KnowledgeBaseDefinition updated = current with
        {
            LogicalRevision = current.LogicalRevision + 1,
            Documents = KnowledgeContractCloner.ReadOnly(current.Documents.Append(document)),
            Chunks = KnowledgeContractCloner.ReadOnly(current.Chunks.Concat(chunks)),
            IndexedAtUtc = DateTimeOffset.UtcNow
        };
        return await repository.TryReplaceAsync(updated, expectedLogicalRevision, cancellationToken)
            ? KnowledgeOperationResult<KnowledgeBaseDefinition>.Success(updated)
            : Conflict();
    }

    public async Task<IReadOnlyList<KnowledgeBaseListItem>> ListAsync(
        KnowledgeBaseQuery query,
        CancellationToken cancellationToken = default) =>
        KnowledgeContractCloner.ReadOnly((await repository.ListAsync(query, cancellationToken)).Select(value =>
            new KnowledgeBaseListItem(
                value.Id, value.Code, value.Name, value.Description, value.Status,
                value.LogicalRevision, value.Documents.Count, value.Chunks.Count, value.IndexedAtUtc)));

    public Task<KnowledgeBaseDefinition?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        Guid id,
        string query,
        int take,
        CancellationToken cancellationToken = default) =>
        retriever.SearchAsync([id], query, Math.Clamp(take, 1, 20), cancellationToken);

    public async Task<KnowledgeOperationResult<KnowledgeBaseDefinition>> SetArchivedAsync(
        SetKnowledgeBaseArchiveCommand command,
        CancellationToken cancellationToken = default)
    {
        KnowledgeBaseDefinition? existing = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        if (existing.LogicalRevision != command.ExpectedLogicalRevision)
        {
            return Conflict();
        }

        if (command.Archived && existing.Status is not KnowledgeBaseStatus.Disabled)
        {
            return KnowledgeOperationResult<KnowledgeBaseDefinition>.Failure(
                KnowledgeErrorCodes.LifecycleTransitionInvalid,
                "A knowledge base must be disabled before it can be archived.");
        }

        if (!command.Archived && existing.Status is not KnowledgeBaseStatus.Archived)
        {
            return KnowledgeOperationResult<KnowledgeBaseDefinition>.Failure(
                KnowledgeErrorCodes.LifecycleTransitionInvalid,
                "Only an archived knowledge base can be restored.");
        }

        if (command.Archived && agents is not null)
        {
            IReadOnlyList<AgentDefinition> enabledAgents = await agents.ListAsync(
                new AgentDefinitionQuery(RuntimeStatus: AgentRuntimeStatus.Enabled),
                cancellationToken);
            string[] blockers = enabledAgents
                .Where(value => value.PublishedVersions.LastOrDefault()?.Snapshot?.KnowledgeBases
                    .Any(binding => binding.KnowledgeBaseId == existing.Id) == true)
                .Select(value => value.Code)
                .Take(8)
                .ToArray();
            if (blockers.Length > 0)
            {
                return KnowledgeOperationResult<KnowledgeBaseDefinition>.Failure(
                    KnowledgeErrorCodes.ArchiveBlocked,
                    $"The knowledge base is still referenced by Agent(s): {string.Join(", ", blockers)}.");
            }
        }

        KnowledgeBaseDefinition updated = existing with
        {
            Status = command.Archived ? KnowledgeBaseStatus.Archived : KnowledgeBaseStatus.Disabled,
            LogicalRevision = existing.LogicalRevision + 1
        };
        return await repository.TryReplaceAsync(updated, existing.LogicalRevision, cancellationToken)
            ? KnowledgeOperationResult<KnowledgeBaseDefinition>.Success(updated)
            : Conflict();
    }

    private static string NormalizeContent(string? content) =>
        (content ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n').Trim();

    private static KnowledgeOperationResult<KnowledgeBaseDefinition>? ValidateImportTarget(
        KnowledgeBaseDefinition? existing,
        long expectedLogicalRevision)
    {
        if (existing is null)
        {
            return NotFound();
        }

        if (existing.LogicalRevision != expectedLogicalRevision)
        {
            return Conflict();
        }

        if (existing.Status is KnowledgeBaseStatus.Archived)
        {
            return KnowledgeOperationResult<KnowledgeBaseDefinition>.Failure(
                KnowledgeErrorCodes.LifecycleTransitionInvalid,
                "An archived knowledge base must be restored before documents can be imported.");
        }

        return existing.Documents.Count >= MaximumDocuments
            ? InvalidDocument($"A knowledge base accepts at most {MaximumDocuments} documents.")
            : null;
    }

    private static bool HasPdfSignature(ReadOnlySpan<byte> content) =>
        content.Length >= 5
        && content[0] == (byte)'%'
        && content[1] == (byte)'P'
        && content[2] == (byte)'D'
        && content[3] == (byte)'F'
        && content[4] == (byte)'-';

    private static string PdfFailureMessage(KnowledgePdfExtractionFailure failure) =>
        failure switch
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

    private static KnowledgeOperationResult<KnowledgeBaseDefinition> InvalidDocument(
        string message) =>
        KnowledgeOperationResult<KnowledgeBaseDefinition>.Failure(
            KnowledgeErrorCodes.DocumentInvalid,
            message);

    private static KnowledgeOperationResult<KnowledgeBaseDefinition> NotFound() =>
        KnowledgeOperationResult<KnowledgeBaseDefinition>.Failure(
            KnowledgeErrorCodes.NotFound, "The knowledge base was not found.");

    private static KnowledgeOperationResult<KnowledgeBaseDefinition> Conflict() =>
        KnowledgeOperationResult<KnowledgeBaseDefinition>.Failure(
            KnowledgeErrorCodes.RowVersionConflict, "The knowledge base changed; reload and retry.");
}

public static class KnowledgeTextChunker
{
    public const int TargetCharacters = 1200;
    public const int OverlapCharacters = 160;

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

        return KnowledgeContractCloner.ReadOnly(values);
    }
}
