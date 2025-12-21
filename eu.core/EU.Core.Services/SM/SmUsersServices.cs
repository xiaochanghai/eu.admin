/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmUsers.cs
*
*功 能： N / A
* 类 名： SmUsers
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/18 14:58:12  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

using EU.Core.AuthHelper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EU.Core.Services;

/// <summary>
/// 系统用户 (服务)
/// </summary>
public class SmUsersServices : BaseServices<SmUsers, SmUsersDto, InsertSmUsersInput, EditSmUsersInput>, ISmUsersServices
{
    // TODO: 考虑将静态字段改为依赖注入，以提高可测试性和避免潜在的并发问题
    private static readonly RedisCacheService TokenRedis = new();
    private static readonly RedisCacheService Redis = new(4);

    private const string AvatarDirectory = "files/userAvatar";
    private const string AvatarFileExtension = "png";
    private static readonly TimeSpan UserCacheExpiration = new(1, 0, 0); // 1小时

    private readonly IBaseRepository<SmUsers> _dal;
    private readonly PermissionRequirement _requirement;
    private readonly IWebHostEnvironment _hostingEnvironment;

    public SmUsersServices(IBaseRepository<SmUsers> dal, PermissionRequirement requirement, IWebHostEnvironment hostingEnvironment)
    {
        this._dal = dal;
        base.BaseDal = dal;
        _requirement = requirement;
        _hostingEnvironment = hostingEnvironment;
    }

    #region 上传头像
    /// <summary>
    /// 上传头像
    /// </summary>
    /// <param name="file">文件流</param>
    /// <returns>上传结果，包含文件附件ID</returns>
    public async Task<ServiceResult<Guid>> UploadAvatarAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return Failed<Guid>("文件不能为空");

        try
        {
            var wwwrootPath = Path.Combine(Environment.CurrentDirectory, "wwwroot");
            var relativePath = Path.Combine("files", "userAvatar");
            var fullPath = Path.Combine(wwwrootPath, relativePath);

            // 确保目录存在
            FileHelper.CreateDirectory(fullPath);

            var fileId = Utility.GetSysID();
            var fileName = $"{fileId}.{AvatarFileExtension}";
            var filePath = Path.Combine(fullPath, fileName);

            // 保存文件
            using (var stream = File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            // 创建文件附件记录
            var fileAttachment = new FileAttachment
            {
                OriginalFileName = file.FileName,
                CreatedBy = App.User.ID,
                CreatedTime = Utility.GetSysDate(),
                FileName = fileName,
                FileExt = AvatarFileExtension,
                MasterId = UserId,
                Length = file.Length,
                Path = "\\files\\userAvatar\\"
            };

            await Db.Insertable(fileAttachment).ExecuteCommandAsync();
            return Success(fileAttachment.ID, "上传成功！");
        }
        catch (Exception ex)
        {
            return Failed<Guid>($"上传头像失败: {ex.Message}");
        }
    }
    #endregion

    #region 查询
    /// <summary>
    /// 根据ID查询一条数据
    /// </summary>
    /// <param name="objId">对象ID</param>
    /// <param name="blnUseCache">是否使用缓存</param>
    /// <returns>用户DTO对象</returns>
    public override async Task<SmUsersDto> QueryDto(object objId, bool blnUseCache = false)
    {
        var userId = objId.ObjToString();
        var data = Redis.Get<SmUsers>(userId);

        if (data == null)
        {
            data = await Query(objId, blnUseCache);
            if (data != null)
            {
                SetUserCache(userId, data);
            }
        }

        return Mapper.Map(data).ToANew<SmUsersDto>();
    }
    #endregion

    #region 更新
    /// <summary>
    /// 更新用户信息并刷新缓存
    /// </summary>
    /// <param name="Id">用户ID</param>
    /// <param name="entity">数据</param>
    /// <returns>更新是否成功</returns>
    public override async Task<bool> Update(Guid Id, object entity)
    {
        var result = await base.Update(Id, entity);

        if (result)
        {
            var user = await Query(Id);
            if (user != null)
            {
                RefreshUserCache(Id, user);
            }
        }

        return result;
    }
    #endregion

    #region 辅助方法
    /// <summary>
    /// 设置用户缓存
    /// </summary>
    private void SetUserCache(string userId, SmUsers user)
    {
        Redis.AddObject(userId, user, UserCacheExpiration);
    }

    /// <summary>
    /// 设置用户缓存（Guid重载）
    /// </summary>
    private void SetUserCache(Guid userId, SmUsers user)
    {
        SetUserCache(userId.ToString(), user);
    }

    /// <summary>
    /// 刷新用户缓存
    /// </summary>
    private void RefreshUserCache(Guid userId, SmUsers user)
    {
        Redis.Remove(userId);
        SetUserCache(userId, user);
    }

    /// <summary>
    /// 验证密码 (使用MD5加密)
    /// WARNING: MD5是弱加密算法，建议迁移到BCrypt、PBKDF2或Argon2等更安全的哈希算法
    /// </summary>
    private string HashPassword(string password)
    {
        return MD5Helper.MD5Encrypt32(password);
    }
    #endregion

    #region 用户登录
    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="request">登录请求</param>
    /// <returns>登录结果，包含Token和用户信息</returns>
    public async Task<ServiceResult<LoginReturn>> LoginAsync(LoginRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UserAccount) || string.IsNullOrWhiteSpace(request.PassWord))
            return ServiceResult<LoginReturn>.OprateFailed("用户账号或密码不能为空");

        try
        {
            var hashedPassword = HashPassword(request.PassWord);
            var user = await QuerySingle(x => x.IsDeleted == false && x.UserAccount == request.UserAccount && x.PassWord == hashedPassword);

            if (user == null)
                return ServiceResult<LoginReturn>.OprateFailed(ResponseText.LOGIN_USER_PWD_FAIL);

            // 更新用户缓存
            RefreshUserCache(user.ID, user);

            // 生成会话ID和Token
            var sessionId = Utility.SnowID().ObjToString();
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Jti, user.ID.ObjToString()),
                //new Claim("TenantId", user.FirstOrDefault().Id.ToString()),
                new("SessionId", sessionId),
                new(JwtRegisteredClaimNames.Iat, DateTime.Now.DateToTimeStamp()),
                new(ClaimTypes.Expiration, DateTime.Now.AddSeconds(_requirement.Expiration.TotalSeconds).ToString())
            };
            var token = JwtToken.BuildJwtToken(claims.ToArray(), _requirement, sessionId);

            var result = new LoginReturn
            {
                Token = token.token,
                UserId = user.ID,
                Modules = new List<SmModules>(),
                UserInfo = new CurrentUser
                {
                    UserName = user.UserName,
                    UserId = user.ID,
                    AvatarFileId = user.AvatarFileId,
                    UserType = user.UserType,
                    WeekName = DateTime.Now.GetWeekNameOfDay()
                }
            };

            #region 记录用户登录日志
            var isDevelopment = _hostingEnvironment.IsDevelopment();
            var platform = App.User.GetPlatform();


            // 使用Task.Run代替new Task，更简洁且自动启动
            _ = Task.Run(() => Utility.RecordEntryLog(Db, user.ID, platform ?? "Web"));

            #endregion

            return Success(result, ResponseText.LOGIN_SUCCESS);
        }
        catch (Exception ex)
        {
            return ServiceResult<LoginReturn>.OprateFailed($"登录失败: {ex.Message}");
        }
    }
    #endregion

    #region 获取用户信息
    /// <summary>
    /// 获取当前登录用户信息
    /// </summary>
    /// <returns>当前用户信息</returns>
    public async Task<ServiceResult<CurrentUser>> CurrentUserAsync()
    {
        if (UserId == null || UserId == Guid.Empty)
            return ServiceResult<CurrentUser>.OprateFailed(ResponseText.LOGIN_USER_PWD_FAIL);

        try
        {
            var user = Redis.Get<SmUsers>(UserId);

            if (user == null)
            {
                user = await QueryDto(UserId);
                if (user != null)
                {
                    SetUserCache(UserId.ObjToString(), user);
                }
                else
                {
                    return ServiceResult<CurrentUser>.OprateFailed("用户不存在");
                }
            }

            var result = new CurrentUser
            {
                UserType = user.UserType,
                UserName = user.UserName,
                UserId = user.ID,
                AvatarFileId = user.AvatarFileId,
                WeekName = DateTime.Now.GetWeekNameOfDay()
            };

            return Success(result, ResponseText.QUERY_SUCCESS);
        }
        catch (Exception ex)
        {
            return ServiceResult<CurrentUser>.OprateFailed($"获取用户信息失败: {ex.Message}");
        }
    }
    #endregion

    #region 修改密码
    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="password">密码修改请求</param>
    /// <returns>修改结果</returns>
    public async Task<ServiceResult> RestPasswordAsync(RestPassword password)
    {
        if (password == null || string.IsNullOrWhiteSpace(password.oldPassword) || string.IsNullOrWhiteSpace(password.newPassword))
            return ServiceResult.OprateFailed("密码不能为空");

        if (UserId == null || UserId == Guid.Empty)
            return ServiceResult.OprateFailed("用户未登录");

        try
        {
            var user = await Query(UserId);
            if (user == null)
                return ServiceResult.OprateFailed("用户不存在");

            var hashedOldPassword = HashPassword(password.oldPassword);
            if (user.PassWord != hashedOldPassword)
                return ServiceResult.OprateFailed("旧密码输入错误");

            // 更新密码
            user.PassWord = HashPassword(password.newPassword);
            await Update(user, ["PassWord"]);

            return Success(ResponseText.UPDATE_SUCCESS);
        }
        catch (Exception ex)
        {
            return ServiceResult.OprateFailed($"修改密码失败: {ex.Message}");
        }
    }
    #endregion

    #region 退出登录
    /// <summary>
    /// 退出登录，清除Token缓存
    /// </summary>
    /// <returns>操作结果</returns>
    public ServiceResult LogOutAsync()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(App.User.SessionId.ObjToString()))
            {
                TokenRedis.Remove(App.User.SessionId.ObjToString());
            }

            return Success(ResponseText.EXECUTE_SUCCESS);
        }
        catch (Exception ex)
        {
            return ServiceResult.OprateFailed($"退出登录失败: {ex.Message}");
        }
    }
    #endregion

    #region 获取token
    /// <summary>
    /// 获取token
    /// </summary>
    /// <param name="request">获取token</param> 
    public async Task<ServiceResult<LoginReturn>> GetAccessToken()
    {
        try
        {
            var user = await QuerySingle(x => x.IsDeleted == false && x.UserAccount == "Admin");

            if (user == null)
                return ServiceResult<LoginReturn>.OprateFailed(ResponseText.LOGIN_USER_PWD_FAIL);

            // 更新用户缓存
            RefreshUserCache(user.ID, user);

            // 生成会话ID和Token
            var sessionId = Utility.SnowID().ObjToString();
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Jti, user.ID.ObjToString()),
                //new Claim("TenantId", user.FirstOrDefault().Id.ToString()),
                new("SessionId", sessionId),
                new(JwtRegisteredClaimNames.Iat, DateTime.Now.DateToTimeStamp()),
                new(ClaimTypes.Expiration, "2099/12/31 23:59:59")
            };
            var token = JwtToken.BuildJwtToken(claims.ToArray(), _requirement, sessionId);

            var result = new LoginReturn
            {
                Token = token.token,
                UserId = user.ID,
                //Modules = new List<SmModules>(),
                //UserInfo = new CurrentUser
                //{
                //    UserName = user.UserName,
                //    UserId = user.ID,
                //    AvatarFileId = user.AvatarFileId,
                //    UserType = user.UserType,
                //    WeekName = DateTime.Now.GetWeekNameOfDay()
                //}
            }; 

            return Success(result, ResponseText.LOGIN_SUCCESS);
        }
        catch (Exception ex)
        {
            return ServiceResult<LoginReturn>.OprateFailed($"登录失败: {ex.Message}");
        }
    }
    #endregion
}