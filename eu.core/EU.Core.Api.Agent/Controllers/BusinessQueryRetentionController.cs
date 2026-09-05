using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Security;
using EU.Core.IServices.UnifiedEntry;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Controllers;

// 文件职责：BusinessQueryRetentionController 接口处理

/// <summary>
/// 提供业务查询敏感载荷清理的 HTTP 接口。
/// </summary>
/// <param name="repository">用于读取和持久化统一入口会话、运行及事件的仓储。</param>
/// <param name="options">业务查询结果敏感载荷的保留和清理配置。</param>
/// <param name="timeProvider">用于获取当前时间的时间提供器。</param>
[Route("api/business-query-results")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class BusinessQueryRetentionController(
    IUnifiedEntryRepository repository,
    IOptions<BusinessQueryResultRetentionOptions> options,
    TimeProvider timeProvider) : Base.ControllerBase
{
    #region 处理（Cleanup）
    /// <summary>
    /// 处理（Cleanup）
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含过期业务查询结果清理统计，失败时包含错误状态和提示。</returns>
    [HttpPost("cleanup")]
    public async Task<ServiceResult<BusinessQueryCleanupResult>> Cleanup(CancellationToken cancellationToken)
    {
        DateTimeOffset cutoff = timeProvider.GetUtcNow().AddDays(
            -options.Value.RetentionDays);
        BusinessQueryCleanupResult result =
            await repository.RedactExpiredBusinessQueryResultsAsync(
                cutoff, cancellationToken);
        return Success(result);
    }
    #endregion
}
