using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EU.Core.Api.MCP.Services.BusinessQuery.Auditing;
using EU.Core.Api.MCP.Services.BusinessQuery.Configuration;
using EU.Core.Api.MCP.Services.BusinessQuery.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace EU.Core.Tests;

public sealed class BusinessQueryAuthenticationTests
{
    private const string TestKey = "offline-test-signing-key-not-a-real-secret-123456789";
    private const string ServiceToken = "offline-test-service-token-not-a-real-secret";

    [Theory]
    [InlineData("jwt", true)]
    [InlineData("lowercase", true)]
    [InlineData("service", true)]
    [InlineData("expired", false)]
    [InlineData("issuer", false)]
    [InlineData("audience", false)]
    [InlineData("signature", false)]
    [InlineData("malformed", false)]
    [InlineData("missing", false)]
    [InlineData("basic", false)]
    public async Task Accepts_service_token_or_validated_project_jwt(string kind, bool accepted)
    {
        await VerifyAsync(kind, accepted, false);
    }

    [Fact]
    public async Task Rejection_fails_closed_when_audit_is_unavailable() =>
        await VerifyAsync("malformed", false, true);

    private static async Task VerifyAsync(string kind, bool accepted, bool auditFails)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey)),
                ValidateIssuer = true,
                ValidIssuer = "offline-project",
                ValidateAudience = true,
                ValidAudience = "offline-api",
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.Zero
            };
        });
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Path = "/mcp/business-query/controller";
        context.Response.Body = new MemoryStream();
        // An unrelated principal must not bypass the registered JWT handler.
        context.User = new ClaimsPrincipal(new ClaimsIdentity("untrusted-test-principal"));
        string token = kind switch
        {
            "service" => ServiceToken,
            "malformed" => "not-a-valid-token",
            "missing" => string.Empty,
            _ => CreateJwt(kind)
        };
        context.Request.Headers.Authorization = kind switch
        {
            "missing" => string.Empty,
            "basic" => $"Basic {token}",
            "lowercase" => $"bearer {token}",
            _ => $"Bearer {token}"
        };
        var audit = new AuditSpy(auditFails);
        bool invoked = false;
        var middleware = new BusinessQueryAuthenticationMiddleware(
            _ => { invoked = true; return Task.CompletedTask; },
            new BusinessQueryServiceTokenResolver(
                Options.Create(new BusinessQueryOptions { DevelopmentServiceToken = ServiceToken }),
                new HostingEnvironment { EnvironmentName = Environments.Development }),
            audit,
            TimeProvider.System);

        await middleware.InvokeAsync(context);

        Assert.Equal(accepted, invoked);
        Assert.Equal(accepted ? 200 : auditFails ? 503 : 401, context.Response.StatusCode);
        Assert.Equal(accepted ? 0 : 1, audit.Rejections);
        if (accepted && kind != "service")
            Assert.Equal("offline-user", context.User.FindFirst("jti")?.Value);
    }

    private static string CreateJwt(string kind)
    {
        var now = DateTime.UtcNow;
        var jwt = new JwtSecurityToken(
            issuer: kind == "issuer" ? "wrong-issuer" : "offline-project",
            audience: kind == "audience" ? "wrong-audience" : "offline-api",
            claims: [new Claim("jti", "offline-user")],
            notBefore: now.AddMinutes(-10),
            expires: kind == "expired" ? now.AddMinutes(-1) : now.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(kind == "signature" ? TestKey + "wrong" : TestKey)),
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private sealed class AuditSpy(bool fails) : IBusinessQueryAuditRepository
    {
        public int Rejections { get; private set; }
        public Task WriteTerminalAsync(BusinessQueryAuditRecord record, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Query execution is outside authentication tests.");
        public Task WriteSecurityRejectionAsync(BusinessQuerySecurityAuditRecord record, CancellationToken cancellationToken)
        {
            Rejections++;
            Assert.False(cancellationToken.CanBeCanceled);
            Assert.Equal("AUTHENTICATION_REQUIRED", record.ErrorCode);
            return fails ? Task.FromException(new InvalidOperationException("offline-audit-failure")) : Task.CompletedTask;
        }
    }
}
