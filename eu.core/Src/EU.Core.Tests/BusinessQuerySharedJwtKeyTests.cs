#nullable enable
using System.Security.Cryptography;
using System.Text;
using EU.Core.Agent.Infrastructure.Mcp;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.MCP.Services.BusinessQuery.Configuration;
using EU.Core.Api.MCP.Services.BusinessQuery.Persistence;
using EU.Core.Api.MCP.Services.BusinessQuery.Security;
using EU.Core.Api.MCP.Services.BusinessQuery.Tooling;
using EU.Core.Extensions;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace EU.Core.Tests;

/// <summary>仅使用测试 JWT 配置和隔离的临时 SQLite 文件，不访问业务数据库。</summary>
public sealed class BusinessQuerySharedJwtKeyTests
{
    private const string Secret = "offline-jwt-key-not-a-real-secret-1234567890";

    private static ServiceProvider Provider(string? secret = Secret)
    {
        var services = new ServiceCollection();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure(options =>
        {
            if (secret is not null)
                options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
        });
        return services.BuildServiceProvider();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("too-short")]
    public async Task Missing_or_weak_project_key_fails_closed(string? secret)
    {
        using var provider = Provider(secret);
        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await new DevelopmentBusinessQuerySigningKeyResolver(options).ResolveAsync("alias:ignored"));
        Assert.Throws<InvalidOperationException>(() => new BusinessQueryExecutionContextKeyResolver(options).ResolveVerificationKeys());
    }

    [Fact]
    public async Task Both_hosts_derive_same_key_without_mutating_jwt_key()
    {
        using var provider = Provider();
        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var agent = await new DevelopmentBusinessQuerySigningKeyResolver(options).ResolveAsync("alias:ignored");
        var mcp = Assert.Single(new BusinessQueryExecutionContextKeyResolver(options).ResolveVerificationKeys());
        Assert.Equal(agent, mcp);
        Assert.NotEqual(Encoding.ASCII.GetBytes(Secret), agent);
        Assert.NotEqual(agent, JwtPurposeSigningKey.Derive(options, "different-purpose"));
        CryptographicOperations.ZeroMemory(agent);
        CryptographicOperations.ZeroMemory(mcp);
        Assert.Equal(Encoding.ASCII.GetBytes(Secret), ((SymmetricSecurityKey)options.Get("Bearer").TokenValidationParameters.IssuerSigningKey).Key);
    }

    [Theory]
    [InlineData("success")]
    [InlineData("success-zero")]
    [InlineData("zero-versus-development")]
    [InlineData("wrong-key")]
    [InlineData("tenant")]
    [InlineData("catalog")]
    [InlineData("expired")]
    [InlineData("tampered")]
    public async Task Agent_token_is_verified_by_mcp_with_security_boundaries(string scenario)
    {
        using var agentProvider = Provider();
        using var mcpProvider = Provider(scenario == "wrong-key" ? Secret + "-other" : Secret);
        var agentKeys = new DevelopmentBusinessQuerySigningKeyResolver(agentProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>());
        var mcpKeys = new BusinessQueryExecutionContextKeyResolver(mcpProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>());
        var now = new FixedTime(DateTimeOffset.UtcNow);
        var policy = new BusinessQueryToolPolicy("business-query", "query_business_data", new Uri("https://localhost"),
            "eu-agent", "eu-mcp", "alias:project-jwt", 1, new string('a', 64), new string('b', 64), TimeSpan.FromSeconds(45), false);
        string tenantId = scenario is "success-zero" or "zero-versus-development" ? "0" : "7";
        bool shouldSucceed = scenario is "success" or "success-zero";
        var identity = new AgentExecutionIdentity(Guid.NewGuid().ToString("D"), tenantId, ["business.project.query"], "test-trace");
        var tool = new PublishedMcpToolReference(Guid.NewGuid(), "business-query", "Business Query", Guid.NewGuid(),
            "query_business_data", "test", "{}", McpToolRisk.ReadOnly, new string('b', 64));
        string token = await new BusinessQueryContextTokenProvider(agentKeys, now).CreateAsync(new McpInvocationContext(identity, Guid.NewGuid()), policy, tool);
        if (scenario == "tampered")
        {
            string[] parts = token.Split('.');
            parts[2] = (parts[2][0] == 'A' ? "B" : "A") + parts[2][1..];
            token = string.Join('.', parts);
        }

        string root = Path.Combine(Path.GetTempPath(), "eu-bq-jwt-" + Guid.NewGuid().ToString("N"));
        var configuration = Options.Create(new BusinessQueryOptions
        {
            TenantId = scenario == "tenant" ? "8" : scenario == "zero-versus-development" ? "development" : tenantId,
            ServerCode = "business-query",
            ExecutionContextIssuer = "eu-agent", ExecutionContextAudience = "eu-mcp", AuditDatabasePath = "audit.db"
        });
        var store = new BusinessQueryStorePath(configuration, new HostingEnvironment { ContentRootPath = root });
        try
        {
            var clock = scenario == "expired" ? new FixedTime(now.GetUtcNow().AddMinutes(2)) : now;
            var replay = new SqliteBusinessQueryReplayRepository(store, configuration, clock);
            var definition = new BusinessQueryToolDefinition("query_business_data", "test", "{}", new string('b', 64),
                scenario == "catalog" ? 2 : 1, new string('a', 64));
            var verifier = new BusinessQueryExecutionContextVerifier(configuration, definition, mcpKeys, replay,
                new BusinessQueryExecutionContextAccessor(), null!, clock);
            var result = await verifier.ValidateAsync(token, definition.Name, CancellationToken.None);
            Assert.Equal(shouldSucceed, result.Succeeded);
            if (shouldSucceed)
            {
                Assert.Equal(identity.UserId, result.Context!.UserId);
                Assert.Equal(tenantId, result.Context.TenantId);
                var duplicate = await verifier.ValidateAsync(token, definition.Name, CancellationToken.None);
                Assert.Equal("BUSINESS_QUERY_EXECUTION_CONTEXT_REPLAYED", duplicate.ErrorCode);
            }
            else Assert.Equal("BUSINESS_QUERY_EXECUTION_CONTEXT_INVALID", result.ErrorCode);
        }
        finally
        {
            using var connection = new SqliteConnection(store.ConnectionString);
            SqliteConnection.ClearPool(connection);
            // Only this test's unique temporary directory is removed.
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
