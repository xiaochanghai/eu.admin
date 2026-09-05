using EU.Core.Api.Agent.Security;
using EU.Core.IServices;
using EU.Core.IServices.Abstractions.Auditing;
using EU.Core.IServices.Abstractions.Security;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Api.Agent.Controllers;

#region 文件职责：AuditController 接口处理

/// <summary>
/// 提供 Agent 操作审计查询的 HTTP 接口。
/// </summary>
[Route("api/audit/operations")]
[Authorize(Policy = AgentAuthorizationPolicies.AuditRead)]
public sealed class AuditController(
    IAgAgentOperationAuditServices repository,
    ICallerContext caller) : Base.ControllerBase
{
    [HttpGet]
    public async Task<ServiceResult<IReadOnlyList<AgentOperationAuditRecord>>> List(
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default) =>
        ServiceResult<IReadOnlyList<AgentOperationAuditRecord>>.QuerySuccess(
            await repository.ListAsync(
                caller.TenantId,
                Math.Clamp(take, 1, 100),
                cancellationToken));
}

#endregion
