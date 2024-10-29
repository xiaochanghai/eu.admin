using System.Security.Claims;
using EU.Core.Model;

namespace EU.Core.Common.HttpContextUser;

public interface IUser
{
    string Name { get; }
    Guid? ID { get; }
    long TenantId { get; }
    bool IsAuthenticated();
    IEnumerable<Claim> GetClaimsIdentity();
    List<string> GetClaimValueByType(string ClaimType);

    string GetToken();
    List<string> GetUserInfoFromToken(string ClaimType);

    ServiceResult<string> MessageModel { get; set; }
}