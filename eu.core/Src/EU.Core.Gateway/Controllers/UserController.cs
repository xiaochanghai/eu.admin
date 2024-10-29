using EU.Core.Common.HttpContextUser;
using EU.Core.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Gateway.Controllers;

[Authorize(AuthenticationSchemes = Permissions.GWName)]
[Route("/gateway/[controller]/[action]")]
public class UserController : ControllerBase
{
    private readonly IUser _user;

    public UserController(IUser user)
    {
        _user = user;
    }

    [HttpGet]
    public ServiceResult<List<ClaimDto>> MyClaims()
    {
        return new ServiceResult<List<ClaimDto>>()
        {
            Success = true,
            Data = (_user.GetClaimsIdentity().ToList()).Select(d =>
                new ClaimDto
                {
                    Type = d.Type,
                    Value = d.Value
                }
            ).ToList()
        };
    }
}
public class ClaimDto
{
    public string Type { get; set; }
    public string Value { get; set; }
}
