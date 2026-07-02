using EU.Core.Model;
using EU.Core.Model.Entity;
using System.Security.Claims;

namespace EU.Core.Common.HttpContextUser;

public interface IUser
{
    string Name { get; }
    Guid? ID { get; }
    SmUsers UserInfo { get; }
    Guid? CompanyId { get; }
    Guid? GroupId { get; }
    long TenantId { get; }
    long? SessionId { get; }
    bool IsAuthenticated();
    IEnumerable<Claim> GetClaimsIdentity();
    List<string> GetClaimValueByType(string ClaimType);

    string GetToken();
    string GetPlatform();
    List<string> GetUserInfoFromToken(string ClaimType);

    ServiceResult<string> MessageModel { get; set; }
}
