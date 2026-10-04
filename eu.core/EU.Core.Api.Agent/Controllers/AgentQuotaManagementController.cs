using EU.Core.Api.Agent.Errors;
using EU.Core.Api.Agent.Security;
using EU.Core.IServices;
using EU.Core.IServices.Abstractions.Security;
using EU.Core.IServices.Runtime;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Api.Agent.Controllers;

/// <summary>共享额度专用管理命令，不能通过普通 CRUD 修改账本或覆盖审计。</summary>
/// <param name="service">现有作用域额度业务服务。</param>
/// <param name="caller">可信认证上下文。</param>
/// <param name="logger">只记录固定错误码及异常类型。</param>
[Route("api/agent-usage/management")]
[Authorize(Policy = AgentAuthorizationPolicies.QuotaManage)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AgentQuotaManagementController(IAgUserTokenQuotaServices service, ICallerContext caller, ILogger<AgentQuotaManagementController> logger) : Base.ControllerBase
{
    #region 读取租户额度设置（GetPolicy）
    /// <summary>只查询认证上下文中的集团、公司，无客户端归属参数。</summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>设置快照。</returns>
    [HttpGet("policy")]
    public Task<ActionResult<ServiceResult<AgentUserTokenQuotaPolicy>>> GetPolicy(CancellationToken cancellationToken) =>
        ExecuteAsync((group, company, _) => service.GetPolicyAsync(group, company, cancellationToken), cancellationToken);
    #endregion

    #region 修改租户额度设置（SavePolicy）
    /// <summary>仅修改配置上限，降低上限不退款；版本或幂等冲突返回 409。</summary>
    /// <param name="input">有原因和版本的命令。</param>
    /// <param name="cancellationToken">提交前取消令牌。</param>
    /// <returns>提交后设置。</returns>
    [HttpPut("policy")]
    public Task<ActionResult<ServiceResult<AgentUserTokenQuotaPolicy>>> SavePolicy([FromBody] AgentUserTokenQuotaPolicyInput input, CancellationToken cancellationToken) =>
        ExecuteAsync((group, company, actor) => service.SavePolicyAsync(group, company, actor, input ?? throw Invalid(), cancellationToken), cancellationToken);
    #endregion

    #region 读取待核查预占（GetPending）
    /// <summary>返回有界列表，不能据活跃状态自动退款。</summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>最多 100 条。</returns>
    [HttpGet("pending")]
    public Task<ActionResult<ServiceResult<IReadOnlyList<AgentUserTokenQuotaPending>>>> GetPending(CancellationToken cancellationToken) =>
        ExecuteAsync((group, company, _) => service.GetPendingAsync(group, company, cancellationToken), cancellationToken);
    #endregion

    #region 人工对账（Reconcile）
    /// <summary>保留原周期及追加式审计，只有核实的实际零用量才能填写 0。</summary>
    /// <param name="reservationId">原预占记录。</param>
    /// <param name="input">确认、实际用量和依据。</param>
    /// <param name="cancellationToken">提交前取消令牌。</param>
    /// <returns>原子对账回执。</returns>
    [HttpPost("reservations/{reservationId:guid}/reconcile")]
    public Task<ActionResult<ServiceResult<AgentUserTokenQuotaReconciliation>>> Reconcile(Guid reservationId, [FromBody] AgentUserTokenQuotaReconcileInput input, CancellationToken cancellationToken) =>
        ExecuteAsync((group, company, actor) => service.ReconcileAsync(group, company, actor, reservationId, input ?? throw Invalid(), cancellationToken), cancellationToken);
    #endregion

    #region 可信所有者与安全响应（ExecuteAsync）
    /// <summary>集团、公司及操作者仅从认证上下文读取，不回传底层异常和部分账本。</summary>
    /// <typeparam name="T">标准响应类型。</typeparam>
    /// <param name="action">业务服务命令。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>标准 ServiceResult。</returns>
    private async Task<ActionResult<ServiceResult<T>>> ExecuteAsync<T>(Func<Guid, Guid, Guid, Task<T>> action, CancellationToken cancellationToken)
    {
        try
        {
            if (caller.GroupId is not { } group || group == Guid.Empty ||
                caller.CompanyId is not { } company || company == Guid.Empty ||
                !Guid.TryParse(caller.UserId, out var actor) || actor == Guid.Empty) throw Invalid();
            cancellationToken.ThrowIfCancellationRequested();
            return ServiceResult<T>.QuerySuccess(await action(group, company, actor));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            string code = exception is AgentRuntimeException runtime && runtime.ErrorCode is
                AgentUserTokenQuotaManagementErrors.Invalid or AgentUserTokenQuotaManagementErrors.Conflict or AgentUserTokenQuotaManagementErrors.NotFound
                ? runtime.ErrorCode : AgentRunErrorCodes.ModelTokenQuotaUnavailable;
            logger.LogWarning("Quota management failed: {ErrorCode}; exception type: {ExceptionType}", code, exception.GetType().Name);
            var descriptor = AgentApiErrorResolver.Resolve(HttpContext, code);
            string message = code == AgentUserTokenQuotaManagementErrors.Conflict ? "额度状态已变化，请重新读取；不要重复执行已提交的操作。" :
                code == AgentUserTokenQuotaManagementErrors.Invalid ? "额度管理参数无效，请检查版本、金额、原因和对账确认。" :
                code == AgentUserTokenQuotaManagementErrors.NotFound ? "未找到当前集团、公司的预占记录。" : "额度管理暂时不可用，操作结果请通过幂等标识核实。";
            return new JsonResult(ServiceResult<AgentApiErrorData>.Failure(descriptor.Status, message, new AgentApiErrorData(code, HttpContext.TraceIdentifier))) { StatusCode = descriptor.HttpStatus };
        }
    }
    #endregion

    #region 无效命令（Invalid）
    /// <summary>创建固定错误，不泄漏可信身份细节。</summary>
    /// <returns>管理参数错误。</returns>
    private static AgentRuntimeException Invalid() => new(AgentUserTokenQuotaManagementErrors.Invalid, "Invalid quota command.");
    #endregion
}
