using EU.Core.Api.Agent.Errors;
using EU.Core.Api.Agent.Security;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace EU.Core.Api.Agent.Base;

/// <summary>
/// Agent API 控制器的统一响应边界。
/// </summary>
[ApiController, Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public abstract class ControllerBase : Microsoft.AspNetCore.Mvc.ControllerBase
{
    protected IActionResult QuerySuccess<T>(T value) =>
        new JsonResult(ServiceResult<T>.QuerySuccess(value))
        {
            StatusCode = StatusCodes.Status200OK
        };

    protected IActionResult OperationSuccess<T>(
        T value,
        int httpStatus = StatusCodes.Status200OK) =>
        new JsonResult(ServiceResult<T>.OprateSuccess(value))
        {
            StatusCode = httpStatus
        };

    protected IActionResult FromServiceError<T>(
        ServiceResult<T> result,
        Func<int, string> errorCodeResolver)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(errorCodeResolver);
        return FromError(errorCodeResolver(result.Status), result.Message);
    }

    protected IActionResult FromError(string errorCode, string message)
    {
        AgentApiErrorDescriptor descriptor = AgentApiErrorResolver.Resolve(HttpContext, errorCode);
        return new JsonResult(
            ServiceResult<AgentApiErrorData>.Failure(
                descriptor.Status,
                message,
                new AgentApiErrorData(errorCode, HttpContext.TraceIdentifier)))
        {
            StatusCode = descriptor.HttpStatus ?? StatusCodes.Status500InternalServerError
        };
    }
}
