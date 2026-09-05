#nullable enable

using EU.Core.Model;

namespace EU.Core.IServices.Knowledge;

/// <summary>
/// Agent 运行时所需的已发布知识库目录和检索能力。
/// </summary>
public interface IKnowledgeRetriever
{
    #region 查询已发布知识库列表。
    /// <summary>查询已发布知识库列表。</summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>已启用且具有可检索分块的知识库发布引用集合。</returns>
    Task<IReadOnlyList<PublishedKnowledgeReference>> ListPublishedAsync(CancellationToken cancellationToken = default);
    #endregion

    #region 在已发布知识库中执行相关性检索。
    /// <summary>在已发布知识库中执行相关性检索。</summary>
    /// <param name="knowledgeBaseIds">知识库标识集合。</param>
    /// <param name="query">查询筛选条件。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定知识库中匹配查询词的相关分块及评分集合。</returns>
    Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        IReadOnlyList<Guid> knowledgeBaseIds,
        string query,
        int take,
        CancellationToken cancellationToken = default);
    #endregion
}
