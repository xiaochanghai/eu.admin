using EU.Core.AuthHelper;
using EU.Core.Common.Caches;

namespace EU.Core.Controllers;

/// <summary>
/// 登录管理【无权限】
/// </summary>
[Produces("application/json")]
[Route("api/[controller]/[action]")]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_Auth)]
public class AuthorizeController : BaseApiController
{
    readonly ISmUsersServices _smUsersServices;
    readonly PermissionRequirement _requirement;
    private readonly ILogger<AuthorizeController> _logger;
    private RedisCacheService Redis = new(4);

    /// <summary>
    /// 构造函数注入
    /// </summary>
    /// <param name="smUsersServices"></param> 
    /// <param name="requirement"></param>
    /// <param name="logger"></param>
    public AuthorizeController(ISmUsersServices smUsersServices, PermissionRequirement requirement, ILogger<AuthorizeController> logger)
    {
        _smUsersServices = smUsersServices;
        _requirement = requirement;
        _logger = logger;
    }

    #region 用户认证-认证授权 
    /// <summary>
    /// 用户认证-认证授权
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [AllowAnonymous, HttpPost]
    public async Task<ServiceResult<LoginReturn>> Login([FromBody] LoginRequest request) => await _smUsersServices.LoginAsync(request);
    #endregion

    #region 获取用户信息
    /// <summary>
    /// 获取用户信息
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ServiceResult<CurrentUser>> CurrentUser() => await _smUsersServices.CurrentUserAsync();
    #endregion

    #region 修改密码
    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="password"></param>
    /// <returns></returns>
    [HttpPut]
    public async Task<ServiceResult> RestPassword([FromBody] RestPassword password) => await _smUsersServices.RestPasswordAsync(password);
    #endregion

    /// <summary>
    /// 退出登录
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ServiceResult> LogOut() => await _smUsersServices.LogOutAsync();

    ///// <summary>
    ///// 获取token
    ///// </summary>
    ///// <returns></returns>
    //[HttpGet]
    //public IActionResult GetAccessToken()
    //{
    //    dynamic obj = new ExpandoObject();
    //    string status = "error";
    //    string message = string.Empty;

    //    try
    //    {
    //        string token = this.HttpContext.Request.Headers["Authorization"].ToString();
    //        if (!string.IsNullOrEmpty(token) && token.StartsWith("Bearer"))
    //        {
    //            DateTime ExpirationTime = DateTime.Parse(User.Claims.SingleOrDefault(x => x.Type == ClaimTypes.Expiration).Value);
    //            DateTime NowTime = DateTime.UtcNow;

    //            TimeSpan timeSpan = ExpirationTime.Subtract(NowTime);
    //            if (timeSpan.Minutes < 10)//过期时间最后的十分钟刷新token
    //            {
    //                //var dto = _context.Set<SmUsers>().Where(x => x.ID == Guid.Parse(User.Identity.Name)).FirstOrDefault();
    //                //var result = _jwtApp.RefreshAsync(token, dto).Result;
    //                //if (result.Success != false)
    //                //{
    //                //    _jwtApp.DeactivateAsync(token);//停用刷新前的token
    //                //    obj.token = result.Token;
    //                //}
    //            }
    //        }
    //        status = "ok";
    //    }
    //    catch (Exception E)
    //    {
    //        message = E.Message;
    //    }

    //    obj.status = status;
    //    obj.message = message;
    //    return Ok(obj);
    //}
}