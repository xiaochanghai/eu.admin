using EU.Core.Extensions.Controllers;
using Microsoft.AspNetCore.Mvc;
namespace EU.Core.Api.Agent.Base;

/// <summary>
/// Agent API 控制器的统一响应边界。
/// </summary>
[ApiController]
public abstract class ControllerBase : ServiceResultControllerBase
{
    protected override string DefaultSuccessMessage => "操作成功";
}
