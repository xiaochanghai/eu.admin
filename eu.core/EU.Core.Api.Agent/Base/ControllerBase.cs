using EU.Core.Api.Agent.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace EU.Core.Api.Agent.Base;

/// <summary>
/// Agent API 控制器的统一响应边界。
/// </summary>
[ApiController, Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public abstract class ControllerBase : Microsoft.AspNetCore.Mvc.ControllerBase
{
}
