using EU.Core.Common.HttpContextUser;
using Microsoft.Extensions.Logging;

namespace EU.Core.Extensions.Authorizations.Behaviors;

public class UserBehaviorService : IUserBehaviorService
{
    private readonly IUser _user; 
    private readonly ILogger<UserBehaviorService> _logger;
    private readonly string _uid;
    private readonly string _token;

    public UserBehaviorService(IUser user 
        , ILogger<UserBehaviorService> logger)
    {
        _user = user; 
        _logger = logger;
        _uid = _user.ID.ObjToString();
        _token = _user.GetToken();
    }


    public Task<bool> CheckTokenIsNormal()
    {
        throw new System.NotImplementedException();
    }

    public Task<bool> CheckUserIsNormal()
    {
        throw new System.NotImplementedException();
    }

    public Task<bool> CreateOrUpdateUserAccessByUid()
    {
        throw new System.NotImplementedException();
    }

    public Task<bool> RemoveAllUserAccessByUid()
    {
        throw new System.NotImplementedException();
    }
}
