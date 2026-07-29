using System.Globalization;
using System.Text;
using EU.Core.Agent.Application.Knowledge;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class InMemoryKnowledgeBaseRepository :
    IKnowledgeBaseRepository,
    IPublishedKnowledgeCatalog,
    IKnowledgeRetriever
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, KnowledgeBaseDefinition> _values = [];

    public Task<KnowledgeBaseDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(_values.TryGetValue(id, out KnowledgeBaseDefinition? value)
                ? KnowledgeContractCloner.Clone(value) : null);
        }
    }

    public Task<KnowledgeBaseDefinition?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            KnowledgeBaseDefinition? value = _values.Values.FirstOrDefault(
                candidate => string.Equals(candidate.Code, code, StringComparison.Ordinal));
            return Task.FromResult(value is null ? null : KnowledgeContractCloner.Clone(value));
        }
    }

    public Task<IReadOnlyList<KnowledgeBaseDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(KnowledgeContractCloner.ReadOnly(_values.Values
                .OrderBy(value => value.Code, StringComparer.Ordinal)
                .Select(KnowledgeContractCloner.Clone)));
        }
    }

    public Task<bool> TryCreateAsync(KnowledgeBaseDefinition value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_values.ContainsKey(value.Id) || _values.Values.Any(
                    existing => string.Equals(existing.Code, value.Code, StringComparison.Ordinal)))
            {
                return Task.FromResult(false);
            }

            _values[value.Id] = KnowledgeContractCloner.Clone(value);
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryReplaceAsync(
        KnowledgeBaseDefinition value,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_values.TryGetValue(value.Id, out KnowledgeBaseDefinition? existing) ||
                existing.LogicalRevision != expectedLogicalRevision ||
                value.LogicalRevision != expectedLogicalRevision + 1 ||
                !string.Equals(existing.Code, value.Code, StringComparison.Ordinal))
            {
                return Task.FromResult(false);
            }

            _values[value.Id] = KnowledgeContractCloner.Clone(value);
            return Task.FromResult(true);
        }
    }

    Task<IReadOnlyList<PublishedKnowledgeReference>> IPublishedKnowledgeCatalog.ListAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(KnowledgeContractCloner.ReadOnly(_values.Values
                .Where(value => value.Status == KnowledgeBaseStatus.Enabled && value.Chunks.Count > 0)
                .OrderBy(value => value.Code, StringComparer.Ordinal)
                .Select(value => new PublishedKnowledgeReference(
                    value.Id, value.Code, value.Name, value.LogicalRevision))));
        }
    }

    public Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        IReadOnlyList<Guid> knowledgeBaseIds,
        string query,
        int take,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            HashSet<string> terms = KnowledgeLexicalSearch.Terms(query);
            var results = new List<KnowledgeSearchResult>();
            foreach (KnowledgeBaseDefinition knowledgeBase in _values.Values.Where(value =>
                         value.Status == KnowledgeBaseStatus.Enabled &&
                         knowledgeBaseIds.Contains(value.Id)))
            {
                IReadOnlyDictionary<Guid, KnowledgeDocument> documents =
                    knowledgeBase.Documents.ToDictionary(document => document.Id);
                foreach (KnowledgeChunk chunk in knowledgeBase.Chunks)
                {
                    double score = KnowledgeLexicalSearch.Score(chunk.Content, terms);
                    if (score <= 0 || !documents.TryGetValue(chunk.DocumentId, out KnowledgeDocument? document))
                    {
                        continue;
                    }

                    results.Add(new KnowledgeSearchResult(
                        knowledgeBase.Id, knowledgeBase.Code, document.Id, document.FileName,
                        chunk.Id, chunk.Sequence, chunk.Content, score));
                }
            }

            return Task.FromResult(KnowledgeContractCloner.ReadOnly(results
                .OrderByDescending(value => value.Score)
                .ThenBy(value => value.KnowledgeBaseCode, StringComparer.Ordinal)
                .ThenBy(value => value.FileName, StringComparer.Ordinal)
                .ThenBy(value => value.ChunkSequence)
                .Take(Math.Clamp(take, 1, 20))));
        }
    }

    internal void Load(IEnumerable<KnowledgeBaseDefinition> values)
    {
        lock (_gate)
        {
            foreach (KnowledgeBaseDefinition value in values)
            {
                _values[value.Id] = KnowledgeContractCloner.Clone(value);
            }
        }
    }
}

public static class KnowledgeLexicalSearch
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
        return matches == 0 ? 0 : matches / Math.Sqrt(queryTerms.Count * (double)Math.Max(1, contentTerms.Count));
    }
}
