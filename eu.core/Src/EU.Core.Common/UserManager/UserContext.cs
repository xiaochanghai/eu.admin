using EU.Core.Common.Caches;
using EU.Core.Common.Helper;
using EU.Core.Model.Models;

namespace EU.Core.Common.UserManager;

public class UserContext
{
    private static RedisCacheService Redis = new(4);
    /// <summary>
    /// 为了尽量减少redis或Memory读取,保证执行效率,将UserContext注入到DI，
    /// 每个UserContext的属性至多读取一次redis或Memory缓存从而提高查询效率
    /// </summary>
    public static UserContext Current
    {
        get
        {
            //try
            //{
            //    return Context.RequestServices.GetService(typeof(UserContext)) as UserContext;
            //}
            //catch (Exception)
            //{
            //    return new UserContext();
            //}
            return new UserContext();
        }
    }

    public static Microsoft.AspNetCore.Http.HttpContext Context
    {
        get
        {
            return HttpUseContext.Current;
        }
    }

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid? User_Id
    {
        get
        {
            try
            {

                return App.User.ID;
                //if (Context == null)
                //    return null;
                ////string aa = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                //string userId = Context?.User?.Identity?.Name;
                //return string.IsNullOrEmpty(userId) ? null : Guid.Parse(userId);
            }
            catch (Exception)
            {
                return null; //匿名访问
            }
        }
    }

    private SmUsers _userInfo { get; set; }

    public SmUsers UserInfo
    {
        get
        {
            if (_userInfo != null)
            {
                return _userInfo;
            }
            return GetUserInfo(User_Id);
        }
    }

    public SmUsers GetUserInfo(Guid? userId)
    {
        if (_userInfo != null) return _userInfo;
        if (userId is null || userId == Guid.Empty)
        {
            _userInfo = new SmUsers();
            return _userInfo;
        }
        _userInfo = Redis.Get<SmUsers>(userId.ToString());
        if (_userInfo == null)
        {
            string sql = "SELECT A.* FROM SmUsers A WHERE A.IsDeleted='false' AND ID='{0}'";
            sql = string.Format(sql, userId);
            _userInfo = DBHelper.QueryFirst<SmUsers>(sql, null);
            Redis.AddObject(userId.ToString(), _userInfo, new TimeSpan(1, 0, 0));
        }
        return _userInfo ?? new SmUsers();
    }

    /// <summary>
    /// 公司ID
    /// </summary>
    public Guid? CompanyId
    {
        get { return UserInfo.CompanyId ?? Utility.GetCompanyGuidId(); }
    }

    /// <summary>
    /// 集团ID
    /// </summary>
    public Guid? GroupId
    {
        get { return UserInfo.GroupId ?? Utility.GetGroupGuidId(); }
    }
}
