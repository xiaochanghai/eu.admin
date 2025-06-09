﻿using System.Security.Claims;
using EU.Core.Model;

namespace EU.Core.Common.HttpContextUser;

public interface IUser
{
    string Name { get; }
    Guid? ID { get; }
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