using EU.Core.IServices;
using EU.Core.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Api.Agent.Controllers;

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
