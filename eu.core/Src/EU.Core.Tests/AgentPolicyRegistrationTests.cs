#nullable enable
using Autofac;
using EU.Core.Common;
using EU.Core.Extensions;
using EU.Core.IServices.Approvals;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.Runtime;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EU.Core.Tests;

[CollectionDefinition("Agent policy registration", DisableParallelization = true)]
public sealed class AgentPolicyRegistrationCollection { }

/// <summary>使用真实 Autofac 模块，仅解析无 I/O 的策略；不读取配置文件、不连接数据库。</summary>
[Collection("Agent policy registration")]
public sealed class AgentPolicyRegistrationTests
{
    [Theory]
    [InlineData("owner", "tenant", McpToolRisk.Mutating, true)]
    [InlineData("owner", "tenant", McpToolRisk.HighRisk, true)]
    [InlineData("other", "tenant", McpToolRisk.Mutating, false)]
    [InlineData("owner", "other", McpToolRisk.Mutating, false)]
    [InlineData("owner", "tenant", McpToolRisk.ReadOnly, false)]
    public async Task Automatic_policy_registration_preserves_authorization(string user, string tenant, McpToolRisk risk, bool allowed)
    {
        var original = AppSettings.Configuration;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        try
        {
            AppSettings.Configuration = configuration;
            var builder = new ContainerBuilder();
            builder.RegisterModule(new AutofacModuleRegister());
            using var container = builder.Build();
            using var scope = container.BeginLifetimeScope();
            Assert.True(scope.IsRegistered<IToolApprovalConversationResumeService>());
            Assert.False(scope.IsRegistered<EU.Core.Services.ToolApprovalConversationResumeService>());
            var policy = Assert.Single(scope.Resolve<IEnumerable<IToolApprovalExecutionPolicy>>());
            Assert.NotSame(policy, scope.Resolve<IToolApprovalExecutionPolicy>());
            var approval = new ToolApprovalRequestRecord(Guid.NewGuid(), "tenant", "owner",
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                "tool", risk, new string('a', 64), new string('b', 64), "{}", ToolApprovalStatus.Approved,
                1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(5), "", "", null, null, null, "");
            var tool = new PublishedMcpToolReference(approval.McpServerId, "server", "Server", approval.ToolVersionId,
                "tool", "Tool", "{}", risk, approval.ToolSchemaSha256);
            var identity = new AgentExecutionIdentity(user, tenant, [], "offline");
            var result = await policy.RevalidateAsync(approval, tool, identity);
            Assert.Equal(allowed, result.Allowed);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => policy.RevalidateAsync(approval, tool, identity, new CancellationToken(true)));
        }
        finally
        {
            AppSettings.Configuration = original;
            (configuration as IDisposable)?.Dispose();
        }
    }
}
