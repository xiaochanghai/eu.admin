using EU.Core.Api.Agent.Errors;
using EU.Core.Api.Agent.Security;
using EU.Core.IServices.Abstractions.Security;
using EU.Core.IServices.Runtime;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Api.Agent.Controllers;

/// <summary>当前登录用户的共享 Token 额度只读入口，不提供所有者选择或账本修改。</summary>
/// <param name="quota">宿主额度适配器。</param>
/// <param name="caller">当前认证用户及租户。</param>
[Route("api/agent-usage")]
[Authorize(Policy = AgentAuthorizationPolicies.HistoryRead)]
public sealed class AgentUsageController(IAgentUserTokenQuota quota, ICallerContext caller) : Base.ControllerBase
{
    #region 查询额度管理入口权限（GetManagementAccess）
    /// <summary>客户端只消费服务端授权结果，不根据前端角色猜测管理权限。</summary>
    /// <param name="authorization">现有授权服务。</param>
    /// <returns>是否允许当前用户进入额度管理。</returns>
    [HttpGet("management/access")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<ServiceResult<AgentQuotaManagementAccess>>> GetManagementAccess([FromServices] IAuthorizationService authorization) =>
        ServiceResult<AgentQuotaManagementAccess>.QuerySuccess(new((await authorization.AuthorizeAsync(User, null, AgentAuthorizationPolicies.QuotaManage)).Succeeded));
    #endregion
    #region 查询当前租户额度（GetQuota）
    /// <summary>始终从认证上下文取集团、公司，客户端不能查询其他租户。</summary>
    /// <param name="cancellationToken">当前请求取消令牌。</param>
    /// <returns>标准服务结果，失败时不返回伪造的零用量。</returns>
    [HttpGet("quota")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<ServiceResult<AgentUserTokenQuotaBalance>>> GetQuota(CancellationToken cancellationToken)
    {
        try
        {
            var identity = new AgentExecutionIdentity(caller.UserId, caller.TenantId, caller.Permissions, caller.CorrelationId) { GroupId = caller.GroupId, CompanyId = caller.CompanyId };
            return ServiceResult<AgentUserTokenQuotaBalance>.QuerySuccess(await quota.GetBalanceAsync(identity, cancellationToken));
        }
        catch (AgentRuntimeException)
        {
            // 不将账本/身份异常消息回传给浏览器。
            const string errorCode = AgentRunErrorCodes.ModelTokenQuotaUnavailable;
            var descriptor = AgentApiErrorResolver.Resolve(HttpContext, errorCode);
            return new JsonResult(ServiceResult<AgentApiErrorData>.Failure(descriptor.Status, "租户 Token 额度暂时无法读取。",
                new AgentApiErrorData(errorCode, HttpContext.TraceIdentifier))) { StatusCode = descriptor.HttpStatus };
        }
    }
    #endregion
}

/// <summary>当前认证用户的额度管理入口权限，不是数据库修改授权凭证。</summary>
/// <param name="CanManage">当前是否通过服务端管理策略。</param>
public sealed record AgentQuotaManagementAccess(bool CanManage);
