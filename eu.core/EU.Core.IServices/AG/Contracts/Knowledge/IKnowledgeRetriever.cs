#nullable enable

using EU.Core.Model;

namespace EU.Core.IServices.Knowledge;

/// <summary>
/// Agent 运行时所需的已发布知识库目录和检索能力。
/// </summary>
public interface IKnowledgeRetriever
{
    /// <summary>查询已发布知识库列表。</summary>
    Task<IReadOnlyList<PublishedKnowledgeReference>> ListPublishedAsync(
        CancellationToken cancellationToken = default);

    /// <summary>在已发布知识库中执行相关性检索。</summary>
    Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        IReadOnlyList<Guid> knowledgeBaseIds,
        string query,
        int take,
        CancellationToken cancellationToken = default);
}
