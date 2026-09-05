using System.Text.Json.Serialization;
using EU.Core.Api.Agent.Security;
using EU.Core.Api.Agent.Errors;
using EU.Core.IServices.Abstractions.Security;
using EU.Core.IServices.Evaluation;
using EU.Core.IServices.UnifiedEntry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;

namespace EU.Core.Api.Agent.Controllers;

// 文件职责：RunEvaluationsController 接口处理

/// <summary>
/// 提供运行结果评测的 HTTP 接口。
/// </summary>
/// <param name="service">用于按评测规范检查运行结果的服务。</param>
/// <param name="caller">提供当前调用方身份、租户及权限的上下文。</param>
[Route("api/evaluations/runs")]
[Authorize(Policy = AgentAuthorizationPolicies.Debug)]
public sealed class RunEvaluationsController(
    IRunEvaluationService service,
    ICallerContext caller) : Base.ControllerBase
{
    #region 处理（Evaluate）
    /// <summary>
    /// 处理（Evaluate）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="request">运行规则评测请求，包含预期终态、输出规则及资源限制。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含运行规则评测报告，失败时包含错误状态和提示。</returns>
    [HttpPost("{runId}")]
    public async Task<ActionResult<ServiceResult<RunEvaluationReport>>> Evaluate(
        string runId,
        [FromBody] EvaluateRunRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(runId, out Guid id) || id == Guid.Empty)
        {
            return FromError(RunEvaluationErrorCodes.SpecificationInvalid, "The run evaluation request is invalid.");
        }

        if (request.AdditionalProperties is { Count: > 0 }
            || !TryStatus(request.ExpectedStatus, out UnifiedRunStatus? expectedStatus))
        {
            return FromError(RunEvaluationErrorCodes.SpecificationInvalid, "The run evaluation request is invalid.");
        }

        try
        {
            RunEvaluationReport? report = await service.EvaluateAsync(
                id,
                caller.TenantId,
                caller.UserId,
                new RunEvaluationSpecification(
                    expectedStatus,
                    request.OutputContains ?? [],
                    request.OutputExcludes ?? [],
                    request.RequiredEventKinds ?? [],
                    request.MaximumToolCalls,
                    request.MaximumDurationMilliseconds),
                cancellationToken);
            return report is null
                ? FromError(RunEvaluationErrorCodes.RunNotFound, "The run was not found.")
                : Success(report);
        }
        catch (RunEvaluationException exception)
        {
            return FromError(exception.ErrorCode, exception.Message);
        }
    }
    #endregion

    #region 解析可选期望运行状态（TryStatus）
    /// <summary>
    /// 忽略大小写解析已定义的运行状态，空文本表示不限定状态（TryStatus）。
    /// </summary>
    /// <param name="value">可选的期望运行状态文本。</param>
    /// <param name="status">成功解析的状态；文本为空或解析失败时为 null。</param>
    /// <returns>文本为空或可解析为已定义状态时返回 true；非法状态返回 false。</returns>
    private static bool TryStatus(string? value, out UnifiedRunStatus? status)
    {
        status = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!Enum.TryParse(value, true, out UnifiedRunStatus parsed)
            || !Enum.IsDefined(parsed))
        {
            return false;
        }

        status = parsed;
        return true;
    }
    #endregion

    #region 转换（FromError）
    /// <summary>
    /// 转换（FromError）
    /// </summary>
    /// <param name="errorCode">操作失败对应的业务错误码。</param>
    /// <param name="message">消息或提示文本。</param>
    /// <returns>包含运行评测错误码和请求跟踪标识的失败响应，HTTP 状态由错误解析器确定，未指定时为 500。</returns>
    private JsonResult FromError(string errorCode, string message)
    {
        AgentApiErrorDescriptor descriptor = AgentApiErrorResolver.Resolve(HttpContext, errorCode);
        return new JsonResult(
            ServiceResult<AgentApiErrorData>.Failure(
                descriptor.Status,
                message,
                new AgentApiErrorData(errorCode, HttpContext.TraceIdentifier)))
        { StatusCode = descriptor.HttpStatus ?? StatusCodes.Status500InternalServerError };
    }
    #endregion
}

/// <summary>
/// 运行评测规则的请求。
/// </summary>
/// <param name="ExpectedStatus">期望的运行终态。</param>
/// <param name="OutputContains">输出必须包含的文本集合。</param>
/// <param name="OutputExcludes">输出不得包含的文本集合。</param>
/// <param name="RequiredEventKinds">运行必须产生的事件类型集合。</param>
/// <param name="MaximumToolCalls">允许的最大工具调用次数。</param>
/// <param name="MaximumDurationMilliseconds">允许的最大运行时长，单位为毫秒。</param>
public sealed record EvaluateRunRequest(
    string? ExpectedStatus,
    IReadOnlyList<string>? OutputContains,
    IReadOnlyList<string>? OutputExcludes,
    IReadOnlyList<string>? RequiredEventKinds,
    int? MaximumToolCalls,
    long? MaximumDurationMilliseconds)
{
    [JsonExtensionData]
    /// <summary>
    /// 未识别的附加字段，用于严格输入校验。
    /// </summary>
    public Dictionary<string, object?>? AdditionalProperties { get; init; }
}
