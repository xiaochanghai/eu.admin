using EU.Core.Model;

namespace EU.Core.Agent.Application.Knowledge;

public interface IKnowledgeRetriever
{
    Task<IReadOnlyList<PublishedKnowledgeReference>> ListPublishedAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        IReadOnlyList<Guid> knowledgeBaseIds,
        string query,
        int take,
        CancellationToken cancellationToken = default);
}
