#nullable enable

using System.Security.Claims;
using EU.Core.Api.Agent.Security;
using EU.Core.Common.HttpContextUser;
using EU.Core.IServices.Runtime;
using EU.Core.Model;
using EU.Core.Model.Entity;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public sealed class HttpCallerContext_Should
{
    private static readonly Guid UserId = Guid.Parse("879beff4-716f-4c18-b952-92f60a9e71d9");

    [Fact]
    public void Build_caller_context_from_the_shared_api_user()
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-1",
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, UserId.ToString("D"))],
                "Bearer"))
        };
        var caller = new HttpCallerContext(
            new HttpContextAccessor { HttpContext = context },
            new TestUser(UserId, 7));

        Assert.Equal(UserId.ToString("D"), caller.UserId);
        Assert.Equal("7", caller.TenantId);
        Assert.Equal("business.project.query", Assert.Single(caller.Permissions));
        Assert.Equal("trace-1", caller.CorrelationId);

        var identity = new AgentExecutionIdentity(
            caller.UserId,
            caller.TenantId,
            caller.Permissions,
            caller.CorrelationId);
        Assert.Equal("business.project.query", Assert.Single(identity.Permissions));
    }

    [Fact]
    public void Reject_a_caller_without_the_shared_api_user_id()
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-1",
            User = new ClaimsPrincipal(new ClaimsIdentity([], "Bearer"))
        };

        Assert.Throws<InvalidOperationException>(() =>
            new HttpCallerContext(
                new HttpContextAccessor { HttpContext = context },
                new TestUser(null, 0)));
    }

    [Fact]
    public void Reject_unauthenticated_user_even_when_user_id_is_available()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "trace-1" };
        Assert.Throws<InvalidOperationException>(() => new HttpCallerContext(
            new HttpContextAccessor { HttpContext = context }, new TestUser(UserId, 7)));
    }

    [Fact]
    public void Does_not_promote_arbitrary_permission_claims_to_business_authorization()
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-1",
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("permission", "business.sales.admin")], "Bearer"))
        };
        var caller = new HttpCallerContext(new HttpContextAccessor { HttpContext = context }, new TestUser(UserId, 7));
        Assert.Equal("business.project.query", Assert.Single(caller.Permissions));
    }

    private sealed class TestUser(Guid? id, long tenantId) : IUser
    {
        public string Name => id?.ToString("D") ?? string.Empty;
        public Guid? ID => id;
        public SmUsers UserInfo => new();
        public Guid? CompanyId => null;
        public Guid? GroupId => null;
        public long TenantId => tenantId;
        public long? SessionId => null;
        public ServiceResult<string> MessageModel { get; set; } = new();
        public bool IsAuthenticated() => id is not null;
        public IEnumerable<Claim> GetClaimsIdentity() => [];
        public List<string> GetClaimValueByType(string ClaimType) => [];
        public string GetToken() => string.Empty;
        public string GetPlatform() => string.Empty;
        public List<string> GetUserInfoFromToken(string ClaimType) => [];
    }
}
