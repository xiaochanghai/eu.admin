using EU.Core.IServices;
using EU.Core.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Api.Agent.Controllers;

// 文件职责：SessionController 接口处理

/// <summary>
/// 提供当前登录会话信息的 HTTP 接口。
/// </summary>
/// <param name="usersServices">用于处理用户登录及会话身份信息的服务。</param>
[Route("api/session")]
public sealed class SessionController(ISmUsersServices usersServices) : Base.ControllerBase
{
    #region 处理（Login）
    /// <summary>
    /// 处理（Login）
    /// </summary>
    /// <param name="request">登录凭据及用户服务要求的登录参数。</param>
    /// <returns>用户服务返回的登录结果，成功时携带登录数据，失败时携带错误提示。</returns>
    [AllowAnonymous]
    [HttpPost("login")]
    public Task<ServiceResult<LoginReturn>> Login([FromBody] LoginRequest request) => usersServices.LoginAsync(request);
    #endregion

    #region 处理（Logout）
    /// <summary>
    /// 处理（Logout）
    /// </summary>
    /// <returns>用户服务返回的注销操作结果。</returns>
    [HttpPost("logout")]
    public ServiceResult Logout() => usersServices.LogOutAsync();
    #endregion
}
