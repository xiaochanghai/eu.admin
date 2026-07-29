using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace EU.Core.Agent.Application.Knowledge;

public sealed class KnowledgeLifecycleService(
    IKnowledgeBaseRepository repository,
    IKnowledgeRetriever retriever)
{
    public const int MaximumDocumentCharacters = 1_000_000;
    public const int MaximumDocuments = 100;

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
        KnowledgeBaseDefinition? existing =
            await repository.GetByIdAsync(command.KnowledgeBaseId, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        if (existing.LogicalRevision != command.ExpectedLogicalRevision)
        {
            return Conflict();
        }

        string fileName = Path.GetFileName(command.FileName?.Trim() ?? string.Empty);
        string mediaType = command.MediaType?.Trim().ToLowerInvariant() ?? string.Empty;
        string content = NormalizeContent(command.Content);
        if (string.IsNullOrWhiteSpace(fileName) ||
            mediaType is not ("text/plain" or "text/markdown") ||
            content.Length is 0 or > MaximumDocumentCharacters ||
            content.Contains('\0') ||
            existing.Documents.Count >= MaximumDocuments)
        {
            return KnowledgeOperationResult<KnowledgeBaseDefinition>.Failure(
                KnowledgeErrorCodes.DocumentInvalid,
                $"Only non-empty text/plain and text/markdown documents up to {MaximumDocumentCharacters} characters are accepted.");
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
        KnowledgeBaseDefinition updated = existing with
        {
            LogicalRevision = existing.LogicalRevision + 1,
            Documents = KnowledgeContractCloner.ReadOnly(existing.Documents.Append(document)),
            Chunks = KnowledgeContractCloner.ReadOnly(existing.Chunks.Concat(chunks)),
            IndexedAtUtc = DateTimeOffset.UtcNow
        };
        return await repository.TryReplaceAsync(updated, command.ExpectedLogicalRevision, cancellationToken)
            ? KnowledgeOperationResult<KnowledgeBaseDefinition>.Success(updated)
            : Conflict();
    }

    public async Task<IReadOnlyList<KnowledgeBaseListItem>> ListAsync(
        CancellationToken cancellationToken = default) =>
        KnowledgeContractCloner.ReadOnly((await repository.ListAsync(cancellationToken)).Select(value =>
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

    private static string NormalizeContent(string? content) =>
        (content ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n').Trim();

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
