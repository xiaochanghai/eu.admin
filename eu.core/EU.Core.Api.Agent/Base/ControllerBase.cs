using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Mvc;
namespace EU.Core.Api.Agent.Base;

/// <summary>
/// Agent API 控制器的统一响应边界。
/// </summary>
[ApiController]
public abstract class ControllerBase : Microsoft.AspNetCore.Mvc.ControllerBase
{
    protected static ServiceResult<T> Success<T>(T data, string message = "操作成功") =>
        ServiceResult<T>.OprateSuccess(data, message);
}
