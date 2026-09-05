using EU.Core.IServices.MainAgent;
using EU.Core.Api.Agent.Errors;
using EU.Core.Api.Agent.Security;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EU.Core.IServices;

namespace EU.Core.Api.Agent.Controllers;

// 文件职责：MainAgentController 接口处理

/// <summary>
/// 提供主 Agent 分配管理的 HTTP 接口。
/// </summary>
/// <param name="assignments">用于查询和维护当前主 Agent 分配的服务。</param>
[Route("api/platform/main-agent")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class MainAgentController(IMainAgentAssignmentService assignments) : Base.ControllerBase
{
    private readonly IMainAgentAssignmentService _assignments = assignments ?? throw new ArgumentNullException(nameof(assignments));

    #region 获取（Get）
    /// <summary>
    /// 获取（Get）
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含主 Agent 固定版本分配，失败时包含错误状态和提示。</returns>
    [HttpGet]
    public async Task<ActionResult<ServiceResult<MainAgentAssignment>>> Get(CancellationToken cancellationToken)
    {
        ServiceResult<MainAgentAssignment> result = await _assignments.GetAsync(cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }
    #endregion

    #region 设置（Set）
    /// <summary>
    /// 设置（Set）
    /// </summary>
    /// <param name="request">设置主 Agent 分配所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含主 Agent 固定版本分配，失败时包含错误状态和提示。</returns>
    [HttpPut]
    public async Task<ActionResult<ServiceResult<MainAgentAssignment>>> Set([FromBody] SetMainAgentRequest request, CancellationToken cancellationToken)
    {
        ServiceResult<MainAgentAssignment> result = await _assignments.SetAsync(
            new SetMainAgentCommand(request.AgentId, request.ExpectedLogicalRevision),
            cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }
    #endregion

    #region 转换（FromServiceError）
    /// <summary>
    /// 转换（FromServiceError）
    /// </summary>
    /// <param name="result">操作结果。</param>
    /// <returns>将主 Agent 服务状态转换为业务错误码后生成的失败响应，HTTP 状态由错误解析器确定，未指定时为 500。</returns>
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
    #endregion
}

/// <summary>
/// 设置主 Agent 的请求。
/// </summary>
/// <param name="AgentId">Agent 标识。</param>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
public sealed record SetMainAgentRequest(Guid AgentId, long? ExpectedLogicalRevision);
