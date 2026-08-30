using EU.Core.Model;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Extensions.Controllers;

/// <summary>
/// 提供统一 ServiceResult 响应辅助方法的 Controller 基类。
/// </summary>
public abstract class ServiceResultControllerBase : ControllerBase
{
    protected virtual string DefaultSuccessMessage => "成功";

    [NonAction]
    public ServiceResult<T> Success<T>(T data, string message = null) => new()
    {
        Success = true,
        Message = message ?? DefaultSuccessMessage,
        Data = data
    };

    [NonAction]
    public ServiceResult Success(string message = null) => new()
    {
        Success = true,
        Message = message ?? DefaultSuccessMessage,
        Data = null
    };

    [NonAction]
    public ServiceResult<string> Failed(string message = "失败", int status = 500) => new()
    {
        Success = false,
        Status = status,
        Message = message,
        Data = null
    };

    [NonAction]
    public ServiceResult<T> Failed<T>(string message = "失败", int status = 500) => new()
    {
        Success = false,
        Status = status,
        Message = message,
        Data = default
    };

    [NonAction]
    public ServiceResult<PageModel<T>> SuccessPage<T>(
        int page,
        int dataCount,
        int pageSize,
        List<T> data,
        int pageCount,
        string message = "获取成功") => new()
    {
        Success = true,
        Message = message,
        Data = new PageModel<T>(page, dataCount, pageSize, data)
    };

    [NonAction]
    public ServiceResult<PageModel<T>> SuccessPage<T>(
        PageModel<T> pageModel,
        string message = "获取成功") => new()
    {
        Success = true,
        Message = message,
        Data = pageModel
    };
}
