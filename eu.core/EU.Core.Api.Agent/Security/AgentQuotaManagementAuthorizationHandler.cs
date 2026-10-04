using EU.Core.Api.Agent.Configuration;
using EU.Core.Common.HttpContextUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Security;

/// <summary>额度管理专用要求，不改变其他 Agent 接口的既有授权。</summary>
public sealed class AgentQuotaManagementRequirement : IAuthorizationRequirement;

/// <summary>同一集团、公司内仅显式配置的管理员可以调整额度或人工对账。</summary>
public sealed class AgentQuotaManagementAuthorizationHandler(IUser user, IOptions<AgentUserTokenQuotaOptions> options) : AuthorizationHandler<AgentQuotaManagementRequirement>
{
    #region 校验管理权限（HandleRequirementAsync）
    /// <summary>复用当前认证身份；配置关闭、匿名、未知身份、其他集团/公司均拒绝。</summary>
    /// <param name="context">授权上下文。</param>
    /// <param name="requirement">额度管理要求。</param>
    /// <returns>授权检查任务，不访问额度数据库。</returns>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AgentQuotaManagementRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true && options.Value.ManagementEnabled &&
            user.ID is { } id && id != Guid.Empty &&
            user.GroupId is { } group && group != Guid.Empty && user.CompanyId is { } company && company != Guid.Empty &&
            options.Value.Administrators.Any(x => x.GroupId == group && x.CompanyId == company && x.UserId == id))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
    #endregion
}
