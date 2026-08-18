#nullable enable

using EU.Core.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public sealed class JwtBearerAuthenticationSetup_Should
{
    [Fact]
    public async Task Register_shared_schemes_and_host_specific_bearer_options()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJwtBearerAuthentication(
            new JwtBearerAuthenticationSchemes(
                "host-authentication",
                "host-challenge",
                "host-forbid"),
            options =>
            {
                options.Audience = "agent-api";
                options.RequireHttpsMetadata = false;
                options.MapInboundClaims = false;
            });

        using ServiceProvider provider = services.BuildServiceProvider();
        AuthenticationOptions authentication = provider
            .GetRequiredService<IOptions<AuthenticationOptions>>()
            .Value;
        JwtBearerOptions bearer = provider
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get("host-authentication");
        var context = new DefaultHttpContext
        {
            RequestServices = provider
        };
        AuthenticateResult result = await context.AuthenticateAsync(
            "host-authentication");

        Assert.Equal("host-authentication", authentication.DefaultScheme);
        Assert.Equal("host-authentication", authentication.DefaultAuthenticateScheme);
        Assert.Equal("host-challenge", authentication.DefaultChallengeScheme);
        Assert.Equal("host-forbid", authentication.DefaultForbidScheme);
        Assert.Equal("agent-api", bearer.Audience);
        Assert.False(bearer.RequireHttpsMetadata);
        Assert.False(bearer.MapInboundClaims);
        Assert.True(result.None);
    }

    [Fact]
    public void Default_challenge_and_forbid_to_authentication_scheme()
    {
        var schemes = new JwtBearerAuthenticationSchemes("development");

        Assert.Equal("development", schemes.AuthenticateScheme);
        Assert.Equal("development", schemes.ChallengeScheme);
        Assert.Equal("development", schemes.ForbidScheme);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Reject_invalid_authentication_scheme(string scheme)
    {
        Assert.Throws<ArgumentException>(() =>
            new JwtBearerAuthenticationSchemes(scheme));
    }
}
