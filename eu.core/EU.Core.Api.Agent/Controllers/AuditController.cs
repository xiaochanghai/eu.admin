using EU.Core.Api.Agent.Security;
using EU.Core.IServices;
using EU.Core.IServices.Abstractions.Auditing;
using EU.Core.IServices.Abstractions.Security;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Api.Agent.Controllers;

// 文件职责：AuditController 接口处理

/// <summary>
/// 提供 Agent 操作审计查询的 HTTP 接口。
/// </summary>
/// <param name="repository">用于查询 Agent 操作审计记录的服务。</param>
/// <param name="caller">提供当前调用方身份、租户及权限的上下文。</param>
[Route("api/audit/operations")]
[Authorize(Policy = AgentAuthorizationPolicies.AuditRead)]
public sealed class AuditController(
    IAgAgentOperationAuditServices repository,
    ICallerContext caller) : Base.ControllerBase
{
    #region 查询列表（List）
    /// <summary>
    /// 查询列表（List）
    /// </summary>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 操作审计记录集合，失败时包含错误状态和提示。</returns>
    [HttpGet]
    public async Task<ServiceResult<IReadOnlyList<AgentOperationAuditRecord>>> List([FromQuery] int take = 50, CancellationToken cancellationToken = default) =>
        ServiceResult<IReadOnlyList<AgentOperationAuditRecord>>.QuerySuccess(
            await repository.ListAsync(
                caller.TenantId,
                Math.Clamp(take, 1, 100),
                cancellationToken));
    #endregion
}
