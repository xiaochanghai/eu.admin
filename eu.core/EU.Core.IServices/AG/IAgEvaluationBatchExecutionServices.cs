using EU.Core.IServices.Evaluation;
using EU.Core.IServices.Runtime;
using EU.Core.Model;

#nullable enable

namespace EU.Core.IServices;

// 文件职责：IAgEvaluationBatchExecutionServices 服务契约

/// <summary>
/// 定义评测批次执行与恢复服务。
/// </summary>
public interface IAgEvaluationBatchExecutionServices
{
    #region 获取评测批次。
    /// <summary>获取评测批次。</summary>
    /// <param name="id">评测批次标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户下包含用例明细的评测批次；不存在时为 null。</returns>
    Task<EvaluationBatchRecord?> GetAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);
    #endregion

    #region 查询评测批次列表。
    /// <summary>查询评测批次列表。</summary>
    /// <param name="suiteId">评估套件标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定租户和套件下的评测批次，按开始时间及标识倒序排列，最多 100 条。</returns>
    Task<IReadOnlyList<EvaluationBatchRecord>> ListAsync(Guid suiteId, string tenantId, int take, CancellationToken cancellationToken = default);
    #endregion

    #region 启动并执行评测批次。
    /// <summary>启动并执行评测批次。</summary>
    /// <param name="suiteId">评估套件标识。</param>
    /// <param name="suiteVersionId">评估套件版本标识。</param>
    /// <param name="identity">当前操作使用的执行身份。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含评测批次记录，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<EvaluationBatchRecord>> RunAsync(
        Guid suiteId,
        Guid suiteVersionId,
        AgentExecutionIdentity identity,
        CancellationToken cancellationToken = default);
    #endregion
}
