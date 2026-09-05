using EU.Core.IServices;
using EU.Core.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Api.Agent.Controllers;

#region 文件职责：SessionController 接口处理

/// <summary>
/// 提供当前登录会话信息的 HTTP 接口。
/// </summary>
[Route("api/session")]
public sealed class SessionController(ISmUsersServices usersServices) : Base.ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public Task<ServiceResult<LoginReturn>> Login(
        [FromBody] LoginRequest request) => usersServices.LoginAsync(request);

    [HttpPost("logout")]
    public ServiceResult Logout() => usersServices.LogOutAsync();
}

#endregion
