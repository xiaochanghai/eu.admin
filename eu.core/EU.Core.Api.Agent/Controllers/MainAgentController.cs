using EU.Core.IServices.MainAgent;
using EU.Core.Api.Agent.Errors;
using EU.Core.Api.Agent.Security;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EU.Core.IServices;

namespace EU.Core.Api.Agent.Controllers;

#region 文件职责：MainAgentController 接口处理

/// <summary>
/// 提供主 Agent 分配管理的 HTTP 接口。
/// </summary>
[Route("api/platform/main-agent")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class MainAgentController(IMainAgentAssignmentService assignments) : Base.ControllerBase
{
    private readonly IMainAgentAssignmentService _assignments = assignments ?? throw new ArgumentNullException(nameof(assignments));

    [HttpGet]
    public async Task<ActionResult<ServiceResult<MainAgentAssignment>>> Get(CancellationToken cancellationToken)
    {
        ServiceResult<MainAgentAssignment> result = await _assignments.GetAsync(cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }

    [HttpPut]
    public async Task<ActionResult<ServiceResult<MainAgentAssignment>>> Set([FromBody] SetMainAgentRequest request, CancellationToken cancellationToken)
    {
        ServiceResult<MainAgentAssignment> result = await _assignments.SetAsync(
            new SetMainAgentCommand(request.AgentId, request.ExpectedLogicalRevision),
            cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }

    private JsonResult FromServiceError(ServiceResult<MainAgentAssignment> result)
    {
        string errorCode = MainAgentServiceStatusCodes.ToErrorCode(result.Status);
        AgentApiErrorDescriptor descriptor = AgentApiErrorResolver.Resolve(HttpContext, errorCode);
        return new JsonResult(
            ServiceResult<AgentApiErrorData>.Failure(
                descriptor.Status,
                result.Message,
                new AgentApiErrorData(errorCode, HttpContext.TraceIdentifier)))
        {
            StatusCode = descriptor.HttpStatus ?? StatusCodes.Status500InternalServerError
        };
    }
}

/// <summary>
/// 设置主 Agent 的请求。
/// </summary>
/// <param name="AgentId">Agent 标识。</param>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <summary>
/// 设置主 Agent 的请求。
/// </summary>
/// <param name="AgentId">Agent 标识。</param>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
public sealed record SetMainAgentRequest(Guid AgentId, long? ExpectedLogicalRevision);

#endregion
