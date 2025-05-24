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
*│　版权所有：苏州一优信息技术有限公司                                │
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
    private RedisCacheService Redis = new(4);
    private readonly IBaseRepository<SmUsers> _dal;
    private readonly PermissionRequirement _requirement;
    private readonly IWebHostEnvironment _hostingEnvironment;
    public SmUsersServices(IBaseRepository<SmUsers> dal, DataContext context, PermissionRequirement requirement, IWebHostEnvironment hostingEnvironment)
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
    /// <returns></returns>
    public async Task<ServiceResult<Guid>> UploadAvatarAsync(IFormFile file)
    {
        var directory = $"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}wwwroot";
        var path = $"{$"{Path.DirectorySeparatorChar}files{Path.DirectorySeparatorChar}userAvatar{Path.DirectorySeparatorChar}"}";

        FileHelper.CreateDirectory(directory + path);
        var ID = Utility.GetSysID();
        var fileName = $"{ID}.png";

        using (var stream = File.Create(directory + path + fileName))
        {
            await file.CopyToAsync(stream);
        }

        var fileAttachment = new FileAttachment();
        fileAttachment.OriginalFileName = file.FileName;
        fileAttachment.CreatedBy = App.User.ID;
        fileAttachment.CreatedTime = Utility.GetSysDate();
        fileAttachment.FileName = ID;
        fileAttachment.FileExt = "png";
        fileAttachment.MasterId = UserId;
        fileAttachment.Length = file.Length;
        fileAttachment.Path = path;

        await Db.Insertable(fileAttachment).ExecuteCommandAsync();
        return Success(fileAttachment.ID, "上传成功！");
    }
    #endregion

    #region 查询
    /// <summary>
    /// 根据ID查询查询一条数据
    /// </summary>
    /// <param name="objId"></param>
    /// <param name="blnUseCache"></param>
    /// <returns></returns>
    public override async Task<SmUsersDto> QueryDto(object objId, bool blnUseCache = false)
    {
        var data = Redis.Get<SmUsers>(objId.ObjToString());
        if (data is null)
            data = await Query(objId, blnUseCache);

        return Mapper.Map(data).ToANew<SmUsersDto>();
    }
    #endregion

    #region 更新
    /// <summary>
    /// 更新
    /// </summary>
    /// <param name="Id">用户ID</param>
    /// <param name="entity">数据</param>
    /// <returns></returns>
    public override async Task<bool> Update(Guid Id, object entity)
    {
        var result = await base.Update(Id, entity);
        var user = await Query(Id);
        Redis.Remove(Id);
        Redis.AddObject(Id, user, new(1, 0, 0));
        return result;
    }
    #endregion

    #region 用户登录
    public async Task<ServiceResult<LoginReturn>> LoginAsync(LoginRequest request)
    {
        var result = new LoginReturn();

        var user = await QuerySingle(x => x.IsDeleted == false && x.UserAccount == request.UserAccount && x.PassWord == MD5Helper.MD5Encrypt32(request.PassWord));
        if (user != null)
        {
            var User = user;
            Redis.Remove(User.ID);
            Redis.AddObject(User.ID, User, new(1, 0, 0));

            var claims = new List<Claim>
            {
                new (ClaimTypes.Name,  user.ID.ToString()),
                new(JwtRegisteredClaimNames.Jti, user.ID.ToString()),
                //new Claim("TenantId", user.FirstOrDefault().Id.ToString()),
                new ("TenantId", "0"),
                new (JwtRegisteredClaimNames.Iat, DateTime.Now.DateToTimeStamp()),
                new (ClaimTypes.Expiration, DateTime.Now.AddSeconds(_requirement.Expiration.TotalSeconds).ToString())
            };
            var token = JwtToken.BuildJwtToken(claims.ToArray(), _requirement);
            result.Token = token.token;
            result.UserId = user.ID;

            //string sql = @"SELECT DISTINCT C.*
            //                    FROM SmRoleModule A
            //                         JOIN SmUserRole_V B
            //                            ON     A.SmRoleId = B.SmRoleId
            //                               AND B.SmUserId = '{0}'
            //                         JOIN SmModules C ON A.SmModuleId = C.ID AND C.IsDeleted = 'false'
            //                    WHERE A.IsDeleted = 'false'";
            //sql = string.Format(sql, user.ID);
            //var roleModule = DBHelper.Instance.QueryList<SmModule>(sql);
            result.Modules = new List<SmModules>();

            result.UserInfo = new CurrentUser()
            {
                UserName = user.UserName,
                UserId = user.ID,
                AvatarFileId = user.AvatarFileId,
                WeekName = DateTime.Now.GetWeekNameOfDay()
            };

            #region 记录用户登录日志
            var isDevelopment = _hostingEnvironment.IsDevelopment();

            var platform = App.User.GetPlatform();
            if (!isDevelopment)
            {
                Task task = new Task(() => Utility.RecordEntryLog(User.ID, platform ?? "Web"));
                task.Start();
            }
            #endregion

            return Success(result, ResponseText.QUERY_SUCCESS);
        }
        else
            return ServiceResult<LoginReturn>.OprateFailed(ResponseText.LOGIN_USER_PWD_FAIL);
    }
    #endregion

    #region 获取用户信息
    /// <summary>
    /// 当前用户
    /// </summary>
    /// <returns></returns>
    public async Task<ServiceResult<CurrentUser>> CurrentUserAsync()
    {
        if (UserId != null && UserId != Guid.Empty)
        {
            var result = new CurrentUser();
            var user = Redis.Get<SmUsers>(UserId);
            if (user == null)
            {
                user = await QueryDto(UserId);
                Redis.AddObject(UserId, user, new(1, 0, 0));
            }
            result.UserName = user.UserName;
            result.UserId = user.ID;
            result.AvatarFileId = user.AvatarFileId;
            result.WeekName = DateTime.Now.GetWeekNameOfDay();
            return Success(result, ResponseText.QUERY_SUCCESS);
        }
        else
            return ServiceResult<CurrentUser>.OprateFailed(ResponseText.LOGIN_USER_PWD_FAIL);
    }
    #endregion

    #region 修改密码
    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="password"></param>
    /// <returns></returns>
    public async Task<ServiceResult> RestPasswordAsync(RestPassword password)
    {
        var user = await Query(UserId);
        if (user.PassWord != MD5Helper.MD5Encrypt32(password.oldPassword))
            return ServiceResult.OprateFailed("旧密码输入错误");
        else
        {
            user.PassWord = MD5Helper.MD5Encrypt32(password.newPassword);
            await Update(user, ["PassWord"]);
        }
        return Success(ResponseText.UPDATE_SUCCESS);
    }
    #endregion

    #region 退出登录
    /// <summary>
    /// 退出登录
    /// </summary>
    /// <returns></returns>
    public async Task<ServiceResult> LogOutAsync()
    {
        await Query(UserId);
        return Success(ResponseText.EXECUTE_SUCCESS);
    }
    #endregion
}