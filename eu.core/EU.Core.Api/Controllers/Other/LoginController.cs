using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EU.Core.AuthHelper;
using EU.Core.AuthHelper.OverWrite;
using EU.Core.Common.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;


namespace EU.Core.Controllers
{
    /// <summary>
    /// 登录管理【无权限】
    /// </summary>
    [Produces("application/json")]
    [Route("api/Login"), ApiExplorerSettings(GroupName = Grouping.GroupName_Other)]
    [AllowAnonymous]
    public class LoginController : BaseApiController
    {
        readonly PermissionRequirement _requirement;
        private readonly ILogger<LoginController> _logger;
        readonly ISmUsersServices _smUsersServices;

        /// <summary>
        /// 构造函数注入
        /// </summary>  
        /// <param name="smUsersServices"></param> 
        /// <param name="requirement"></param> 
        /// <param name="logger"></param>
        public LoginController(ISmUsersServices smUsersServices, PermissionRequirement requirement, ILogger<LoginController> logger)
        {
            this._smUsersServices = smUsersServices;
            _requirement = requirement;
            _logger = logger;
        }


        #region 获取token的第1种方法

        /// <summary>
        /// 获取JWT的方法1
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("Token")]
        public async Task<ServiceResult<string>> GetJwtStr(string name, string pass)
        {
            string jwtStr = string.Empty;
            bool suc = false;
            //这里就是用户登陆以后，通过数据库去调取数据，分配权限的操作

            //var user = await _sysUserInfoServices.GetUserRoleNameStr(name, MD5Helper.MD5Encrypt32(pass));
            //if (user != null)
            //{
            //    TokenModelJwt tokenModel = new TokenModelJwt { Uid = 1, Role = user };

            //    jwtStr = JwtHelper.IssueJwt(tokenModel);
            //    suc = true;
            //}
            //else
            //    jwtStr = "login fail!!!";

            return new ServiceResult<string>()
            {
                Success = suc,
                Message = suc ? "获取成功" : "获取失败",
                Data = jwtStr
            };
        }


        /// <summary>
        /// 获取JWT的方法2：给Nuxt提供
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("GetTokenNuxt")]
        public ServiceResult<string> GetJwtStrForNuxt(string name, string pass)
        {
            string jwtStr = string.Empty;
            bool suc = false;
            //这里就是用户登陆以后，通过数据库去调取数据，分配权限的操作
            //这里直接写死了
            if (name == "admins" && pass == "admins")
            {
                TokenModelJwt tokenModel = new TokenModelJwt
                {
                    Uid = 1,
                    Role = "Admin"
                };

                jwtStr = JwtHelper.IssueJwt(tokenModel);
                suc = true;
            }
            else
                jwtStr = "login fail!!!";

            //var result = new
            //{
            //    data = new { success = suc, token = jwtStr }
            //};

            return new ServiceResult<string>()
            {
                Success = suc,
                Message = suc ? "获取成功" : "获取失败",
                Data = jwtStr
            };
        }

        #endregion


        /// <summary>
        /// 获取JWT的方法3：整个系统主要方法
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("JWTToken3.0")]
        public async Task<ServiceResult<TokenInfoViewModel>> GetJwtToken3(string name = "", string pass = "")
        {
            string jwtStr = string.Empty;

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(pass))
                return Failed<TokenInfoViewModel>("用户名或密码不能为空");

            pass = MD5Helper.MD5Encrypt32(pass);

            var user = await _smUsersServices.QuerySingle(d => d.UserAccount == name && d.PassWord == pass);
            if (user != null)
            {
                //var userRoles = await _smUsersServices.GetUserRoleNameStr(name, pass);
                //如果是基于用户的授权策略，这里要添加用户;如果是基于角色的授权策略，这里要添加角色
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name,  user.ID.ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, user.ID.ToString()),
                    //new Claim("TenantId", user.FirstOrDefault().Id.ToString()),
                    new Claim("TenantId", "0"),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTime.Now.DateToTimeStamp()),
                    new Claim(ClaimTypes.Expiration,
                        DateTime.Now.AddSeconds(_requirement.Expiration.TotalSeconds).ToString())
                };
                //claims.AddRange(userRoles.Split(',').Select(s => new Claim(ClaimTypes.Role, s)));


                // ids4和jwt切换
                // jwt
                if (!Permissions.IsUseIds4)
                {
                    //var data = await _roleModulePermissionServices.RoleModuleMaps();
                    //var list = (from item in data
                    //            where item.IsDeleted == false
                    //            orderby item.Id
                    //            select new PermissionItem
                    //            {
                    //                Url = item.Module?.LinkUrl,
                    //                Role = item.Role?.Name.ObjToString(),
                    //            }).ToList();

                    //_requirement.Permissions = list;
                }

                var token = JwtToken.BuildJwtToken(claims.ToArray(), _requirement);

                //var result = _jwtApp.Create(user, _requirement);


                return Success(token, "获取成功");
            }
            else
                return Failed<TokenInfoViewModel>("认证失败");
        }

        [HttpGet]
        [Route("GetJwtTokenSecret")]
        public async Task<ServiceResult<TokenInfoViewModel>> GetJwtTokenSecret(string name = "", string pass = "")
        {
            var rlt = await GetJwtToken3(name, pass);
            return rlt;
        }

        /// <summary>
        /// 请求刷新Token（以旧换新）
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("RefreshToken")]
        public async Task<ServiceResult<TokenInfoViewModel>> RefreshToken(string token = "")
        {
            string jwtStr = string.Empty;

            if (string.IsNullOrEmpty(token))
                return Failed<TokenInfoViewModel>("token无效，请重新登录！");
            var tokenModel = JwtHelper.SerializeJwt(token);
            if (tokenModel != null && JwtHelper.customSafeVerify(token) && tokenModel.Uid > 0)
            {
                //var user = await _sysUserInfoServices.QueryById(tokenModel.Uid);
                //var value = User.Claims.SingleOrDefault(s => s.Type == JwtRegisteredClaimNames.Iat)?.Value;
                //if (value != null && user.CriticalModifyTime > value.ObjToDate())
                //    return Failed<TokenInfoViewModel>("很抱歉,授权已失效,请重新授权！");

                //if (user != null && !(value != null && user.CriticalModifyTime > value.ObjToDate()))
                //{
                //    var userRoles = await _sysUserInfoServices.GetUserRoleNameStr(user.LoginName, user.LoginPWD);
                //    //如果是基于用户的授权策略，这里要添加用户;如果是基于角色的授权策略，这里要添加角色
                //    var claims = new List<Claim>
                //    {
                //        new Claim(ClaimTypes.Name, user.LoginName),
                //        new Claim(JwtRegisteredClaimNames.Jti, tokenModel.Uid.ObjToString()),
                //        new Claim(JwtRegisteredClaimNames.Iat, DateTime.Now.DateToTimeStamp()),
                //        new Claim(ClaimTypes.Expiration,
                //            DateTime.Now.AddSeconds(_requirement.Expiration.TotalSeconds).ToString())
                //    };
                //    claims.AddRange(userRoles.Split(',').Select(s => new Claim(ClaimTypes.Role, s)));

                //    //用户标识
                //    var identity = new ClaimsIdentity(JwtBearerDefaults.AuthenticationScheme);
                //    identity.AddClaims(claims);

                //    var refreshToken = JwtToken.BuildJwtToken(claims.ToArray(), _requirement);
                //    return Success(refreshToken, "获取成功");
                //}
            }

            return Failed<TokenInfoViewModel>("认证失败！");
        }

        /// <summary>
        /// 获取JWT的方法4：给 JSONP 测试
        /// </summary>
        /// <param name="callBack"></param>
        /// <param name="id"></param>
        /// <param name="sub"></param>
        /// <param name="expiresSliding"></param>
        /// <param name="expiresAbsoulute"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("jsonp")]
        public void Getjsonp(string callBack, long id = 1, string sub = "Admin", int expiresSliding = 30, int expiresAbsoulute = 30)
        {
            TokenModelJwt tokenModel = new TokenModelJwt
            {
                Uid = id,
                Role = sub
            };

            string jwtStr = JwtHelper.IssueJwt(tokenModel);

            string response = string.Format("\"value\":\"{0}\"", jwtStr);
            string call = callBack + "({" + response + "})";
            Response.WriteAsync(call);
        }


        /// <summary>
        /// 测试 MD5 加密字符串
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("Md5Password")]
        public string Md5Password(string password = "")
        {
            return MD5Helper.MD5Encrypt32(password);
        }

        /// <summary>
        /// swagger登录
        /// </summary>
        /// <param name="loginRequest"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("/api/Login/swgLogin")]
        public async Task<dynamic> SwgLogin([FromBody] SwaggerLoginRequest loginRequest)
        {
            if (loginRequest is null)
                return new { result = false };

            try
            {
                var result = await GetJwtToken3(loginRequest.name, loginRequest.pwd);
                if (result.Success)
                {
                    HttpContext.SuccessSwagger();
                    HttpContext.SuccessSwaggerJwt(result.Data.token);
                    return new { result = true };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Swagger登录异常");
            }

            return new { result = false };
        }

        /// <summary>
        /// weixin登录
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("wxLogin")]
        public dynamic WxLogin(string g = "", string token = "")
        {
            return new { g, token };
        }
    }

    public class SwaggerLoginRequest
    {
        public string name { get; set; }
        public string pwd { get; set; }
    }
}