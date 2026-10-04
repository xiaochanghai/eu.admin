#nullable enable
using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Controllers;
using EU.Core.Api.Agent.Errors;
using EU.Core.Api.Agent.Security;
using EU.Core.Common;
using EU.Core.Common.HttpContextUser;
using EU.Core.IServices;
using EU.Core.IServices.Abstractions.Security;
using EU.Core.IServices.Runtime;
using EU.Core.Model;
using EU.Core.Model.Entity;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.Services;
using EU.Core.Tests.Service_Test;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EU.Core.Tests;

/// <summary>余额接口和真实业务服务的隔离回归，不访问部署数据库或外部网络。</summary>
[Collection("Agent policy registration")]
public sealed class AgentUserTokenQuotaBalanceTests
{
    private static readonly Guid User = Guid.Parse("bbc7dc4b-a2bd-4d1e-b70a-f18d76f66e39");
    private static readonly Guid Group = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Company = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static Guid GroupFor(long group) => group == 1 ? Group : Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Reading_unused_periods_does_not_create_ledger_or_reservations()
    {
        using var db = Fixture();
        using var host = Host(db);
        var result = await Provider(host).GetBalanceAsync(Identity());
        Assert.True(result.Enabled);
        Assert.True(result.CanReserve);
        Assert.Equal(2, result.Periods.Count);
        Assert.All(result.Periods, x => { Assert.Equal(0, x.UsedTokens); Assert.Equal(0, x.ReservedTokens); Assert.Equal(x.LimitTokens, x.RemainingTokens); });
        Assert.Equal(0, await db.Db.Queryable<AgUserTokenQuotaPeriod>().CountAsync());
        Assert.Equal(0, await db.Db.Queryable<AgUserTokenQuotaReservation>().CountAsync());
    }

    [Fact]
    public async Task Current_balance_includes_known_settlement_and_pending_reservation_without_mutation()
    {
        using var db = Fixture();
        using var host = Host(db);
        var service = Service(db);
        var first = Request();
        await service.ReserveAsync(first);
        await service.MarkStartedAsync(first);
        await service.SettleAsync(first, 30);
        await service.ReserveAsync(Request());
        var before = JsonSerializer.Serialize(await db.Db.Queryable<AgUserTokenQuotaPeriod>().OrderBy(x => x.PeriodKind).ToListAsync());
        var balance = await Provider(host).GetBalanceAsync(Identity());
        Assert.False(balance.CanReserve); // daily 100 - 30 - 50 = 20, less than the next reservation
        Assert.Equal(20, balance.Periods[0].RemainingTokens);
        Assert.Equal(920, balance.Periods[1].RemainingTokens);
        Assert.All(balance.Periods, x => { Assert.Equal(30, x.UsedTokens); Assert.Equal(50, x.ReservedTokens); });
        Assert.Equal(before, JsonSerializer.Serialize(await db.Db.Queryable<AgUserTokenQuotaPeriod>().OrderBy(x => x.PeriodKind).ToListAsync()));
    }

    [Fact]
    public async Task Unknown_usage_is_frozen_not_a_positive_balance()
    {
        using var db = Fixture();
        using var host = Host(db);
        var service = Service(db);
        var request = Request();
        await service.ReserveAsync(request);
        await service.MarkStartedAsync(request);
        await service.SettleAsync(request, null);
        var result = await Provider(host).GetBalanceAsync(Identity());
        Assert.False(result.CanReserve);
        Assert.All(result.Periods, x => { Assert.True(x.HasUnknownUsage); Assert.Null(x.RemainingTokens); Assert.False(x.CanReserve); Assert.Equal(50, x.ReservedTokens); });
    }

    [Fact]
    public async Task Overspent_balance_cannot_overflow_into_available_tokens()
    {
        using var db = Fixture();
        using var host = Host(db);
        var service = Service(db);
        var request = Request();
        await service.ReserveAsync(request);
        await service.MarkStartedAsync(request);
        await service.SettleAsync(request, long.MaxValue);
        var result = await Provider(host).GetBalanceAsync(Identity());
        Assert.False(result.CanReserve);
        Assert.All(result.Periods, x => { Assert.Equal(long.MaxValue, x.UsedTokens); Assert.Equal(0, x.RemainingTokens); Assert.False(x.CanReserve); });
    }

    [Theory]
    [InlineData(2, true, 0)]
    [InlineData(1, false, 50)]
    public async Task Only_group_and_company_define_the_shared_balance_owner(long group, bool sameUser, long expectedReservedTokens)
    {
        using var db = Fixture();
        using var host = Host(db);
        await Service(db).ReserveAsync(Request());
        var other = await Provider(host).GetBalanceAsync(Identity(group, sameUser ? User : Guid.NewGuid()));
        Assert.True(other.CanReserve);
        Assert.All(other.Periods, x => { Assert.Equal(0, x.UsedTokens); Assert.Equal(expectedReservedTokens, x.ReservedTokens); });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Deleted_or_inconsistent_period_is_unavailable_not_unused(bool deleted)
    {
        using var db = Fixture();
        using var host = Host(db);
        await Service(db).ReserveAsync(Request());
        var row = await db.Db.Queryable<AgUserTokenQuotaPeriod>().FirstAsync();
        row.IsDeleted = deleted;
        if (!deleted) row.UsedTokens = -1;
        await db.Db.Updateable(row).ExecuteCommandAsync();
        var failure = await Assert.ThrowsAsync<AgentRuntimeException>(() => Provider(host).GetBalanceAsync(Identity()));
        Assert.Equal(AgentRunErrorCodes.ModelTokenQuotaUnavailable, failure.ErrorCode);
    }

    [Fact]
    public async Task Next_month_uses_new_windows_without_clearing_old_ledger()
    {
        using var db = Fixture();
        using var host = Host(db);
        await Service(db).ReserveAsync(Request());
        var clock = (Clock)host.GetRequiredService<TimeProvider>();
        clock.Value = new(2026, 10, 31, 16, 0, 0, TimeSpan.Zero);
        var balance = await Provider(host).GetBalanceAsync(Identity());
        Assert.Equal(clock.Value, balance.EvaluatedAtUtc);
        Assert.True(balance.CanReserve);
        Assert.All(balance.Periods, x => Assert.Equal(0, x.ReservedTokens));
        Assert.Equal(2, await db.Db.Queryable<AgUserTokenQuotaPeriod>().CountAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Single_enabled_period_is_returned_without_the_other(bool daily)
    {
        using var db = Fixture();
        using var host = Host(db, new() { DailyTotalTokens = daily ? 100 : null, MonthlyTotalTokens = daily ? null : 1000, RequestReservationTokens = 50 });
        var period = Assert.Single((await Provider(host).GetBalanceAsync(Identity())).Periods);
        Assert.Equal(daily ? "Daily" : "Monthly", period.Kind);
    }

    [Fact]
    public async Task Disabled_balance_does_not_resolve_database_or_unused_time_zone()
    {
        using var host = new ServiceCollection().BuildServiceProvider();
        var provider = new AgUserTokenQuotaProvider(host.GetRequiredService<IServiceScopeFactory>(), Options.Create(new AgentUserTokenQuotaOptions { TimeZoneId = "unused/offline-zone" }), new Clock(), NullLogger<AgUserTokenQuotaProvider>.Instance);
        var result = await provider.GetBalanceAsync(Identity());
        Assert.False(result.Enabled);
        Assert.Null(result.CanReserve);
        Assert.Null(result.RequestReservationTokens);
        Assert.Empty(result.Periods);
    }

    [Fact]
    public async Task Invalid_identity_and_cancelled_read_do_not_access_database()
    {
        using var host = new ServiceCollection().AddScoped<IAgUserTokenQuotaServices>(_ => throw new Xunit.Sdk.XunitException("Database must not resolve")).BuildServiceProvider();
        var provider = Provider(host);
        Assert.Equal(AgentRunErrorCodes.ModelTokenQuotaUnavailable, (await Assert.ThrowsAsync<AgentRuntimeException>(() =>
            provider.GetBalanceAsync(new("not-a-user", "1", [], "offline")))).ErrorCode);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GetBalanceAsync(Identity(), cancellation.Token));
    }

    [Fact]
    public async Task Controller_uses_trusted_caller_even_when_query_contains_other_owner()
    {
        using var db = Fixture();
        using var host = Host(db);
        await Service(db).ReserveAsync(Request());
        var controller = Controller(Provider(host));
        controller.HttpContext.Request.QueryString = new("?groupId=" + Guid.NewGuid() + "&companyId=" + Guid.NewGuid() + "&userId=" + Guid.NewGuid());
        var response = await controller.GetQuota(CancellationToken.None);
        Assert.True(response.Value!.Success);
        Assert.Equal(50, response.Value.Data.Periods[0].ReservedTokens);
        Assert.Null(response.Value.MessageDev);
        Assert.True(controller.GetType().GetMethod("GetQuota")!.GetCustomAttribute<ResponseCacheAttribute>()!.NoStore);
        Assert.Equal(typeof(CancellationToken), Assert.Single(controller.GetType().GetMethod("GetQuota")!.GetParameters()).ParameterType);
    }

    [Fact]
    public async Task Normal_mvc_json_keeps_pascal_contract_and_exact_long_token_strings()
    {
        using var db = Fixture();
        using var host = Host(db, new() { DailyTotalTokens = long.MaxValue, MonthlyTotalTokens = null, RequestReservationTokens = 50 });
        var result = await Controller(Provider(host)).GetQuota(CancellationToken.None);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
        var root = document.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal("50", data.GetProperty("RequestReservationTokens").GetString());
        var period = data.GetProperty("Periods")[0];
        Assert.Equal("9223372036854775807", period.GetProperty("LimitTokens").GetString());
        Assert.Equal("0", period.GetProperty("UsedTokens").GetString());
        Assert.Equal("9223372036854775807", period.GetProperty("RemainingTokens").GetString());
        Assert.EndsWith("+00:00", period.GetProperty("StartUtc").GetString());
        Assert.False(data.TryGetProperty("UserId", out _));
        Assert.False(data.TryGetProperty("TenantId", out _));
    }

    [Fact]
    public async Task Database_failure_returns_standard_safe_503_without_zero_quota()
    {
        using var host = new ServiceCollection().AddScoped<IAgUserTokenQuotaServices>(_ => throw new IOException("private-db-error-with-sql-or-credentials")).BuildServiceProvider();
        var response = await Controller(Provider(host)).GetQuota(CancellationToken.None);
        var result = Assert.IsType<JsonResult>(response.Result);
        Assert.Equal(503, result.StatusCode);
        var error = Assert.IsType<ServiceResult<AgentApiErrorData>>(result.Value);
        Assert.False(error.Success);
        Assert.Equal(660043, error.Status);
        Assert.Equal(AgentRunErrorCodes.ModelTokenQuotaUnavailable, error.Data.ErrorCode);
        Assert.Null(error.MessageDev);
        Assert.DoesNotContain("private-db", JsonSerializer.Serialize(error));
        Assert.DoesNotContain("Periods", JsonSerializer.Serialize(error));
    }

    [Fact]
    public async Task Endpoint_requires_the_existing_authenticated_history_policy()
    {
        var auth = typeof(AgentUsageController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.Equal(AgentAuthorizationPolicies.HistoryRead, auth?.Policy);
        Assert.Null(typeof(AgentUsageController).GetCustomAttribute<AllowAnonymousAttribute>());
        var previous = AppSettings.Configuration;
        try
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Audience:Secret"] = "offline-only-signing-placeholder-32-chars",
                ["Audience:Issuer"] = "offline", ["Audience:Audience"] = "offline"
            }).Build();
            _ = new AppSettings(configuration);
            var services = new ServiceCollection().AddLogging();
            // HistoryRead 只含认证要求。保留生产处理器，并用禁止调用的替身补齐构造依赖，不能访问 Redis/用户库。
            services.AddSingleton(DispatchProxy.Create<ISmUsersServices, UnusedDependency>());
            services.AddSingleton(DispatchProxy.Create<IUser, UnusedDependency>());
            typeof(AgentUsageController).Assembly.GetType("EU.Core.Api.Agent.Security.AgentApiSecurityServiceCollectionExtensions", true)!
                .GetMethod("AddAgentApiHttpSecurity", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [services, configuration]);
            using var provider = services.BuildServiceProvider();
            var authorization = provider.GetRequiredService<IAuthorizationService>();
            Assert.False((await authorization.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null, auth!.Policy!)).Succeeded);
            Assert.True((await authorization.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity([], "offline-authenticated")), null, auth.Policy!)).Succeeded);
        }
        finally { AppSettings.Configuration = previous; }
    }

    private static AgentExecutionIdentity Identity(long group = 1, Guid? user = null) => new((user ?? User).ToString("D"), "1", [], "offline") { GroupId = GroupFor(group), CompanyId = Company };
    private static AgentUserTokenQuotaRequest Request()
    {
        var query = AgentUserTokenQuotaRequests.CreateQuery(Identity(), Now, TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai"), 100, 1000, 50);
        return new(Guid.NewGuid(), Guid.NewGuid(), query.GroupId, query.CompanyId, User, 50, query.Windows);
    }
    [Theory]
    [InlineData("missing-group")]
    [InlineData("empty-group")]
    [InlineData("missing-company")]
    [InlineData("empty-company")]
    public async Task Missing_business_scope_is_rejected_before_database_access(string missing)
    {
        using var host = new ServiceCollection().AddScoped<IAgUserTokenQuotaServices>(_ => throw new Xunit.Sdk.XunitException("Database must not resolve")).BuildServiceProvider();
        var identity = new AgentExecutionIdentity(User.ToString("D"), "1", [], "offline")
        {
            GroupId = missing == "missing-group" ? null : missing == "empty-group" ? Guid.Empty : Group,
            CompanyId = missing == "missing-company" ? null : missing == "empty-company" ? Guid.Empty : Company
        };
        Assert.Equal(AgentRunErrorCodes.ModelTokenQuotaUnavailable,
            (await Assert.ThrowsAsync<AgentRuntimeException>(() => Provider(host).GetBalanceAsync(identity))).ErrorCode);
    }

    [Fact]
    public async Task Same_group_and_user_different_company_has_an_independent_balance()
    {
        using var db = Fixture();
        using var host = Host(db);
        var first = Request();
        await Service(db).ReserveAsync(first);
        var other = new AgentExecutionIdentity(User.ToString("D"), "1", [], "offline") { GroupId = Group, CompanyId = Guid.NewGuid() };
        Assert.All((await Provider(host).GetBalanceAsync(other)).Periods, x => Assert.Equal(0, x.ReservedTokens));
        Assert.All((await Provider(host).GetBalanceAsync(Identity())).Periods, x => Assert.Equal(50, x.ReservedTokens));
    }

    private static AgentPersistenceSqliteFixture Fixture() => new(typeof(AgUserTokenQuotaPeriod), typeof(AgUserTokenQuotaReservation));
    private static AgUserTokenQuotaServices Service(AgentPersistenceSqliteFixture db) => new(db.CreateRepository<AgUserTokenQuotaPeriod>());
    private static ServiceProvider Host(AgentPersistenceSqliteFixture db, AgentUserTokenQuotaOptions? options = null) => new ServiceCollection()
        .AddScoped<IAgUserTokenQuotaServices>(_ => Service(db))
        .AddSingleton<IOptions<AgentUserTokenQuotaOptions>>(Options.Create(options ?? Configuration()))
        .AddSingleton<TimeProvider>(new Clock()).BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    private static AgentUserTokenQuotaOptions Configuration() => new() { DailyTotalTokens = 100, MonthlyTotalTokens = 1000, RequestReservationTokens = 50 };
    private static AgUserTokenQuotaProvider Provider(ServiceProvider host) => new(host.GetRequiredService<IServiceScopeFactory>(),
        host.GetService<IOptions<AgentUserTokenQuotaOptions>>() ?? Options.Create(Configuration()), host.GetService<TimeProvider>() ?? new Clock(), NullLogger<AgUserTokenQuotaProvider>.Instance);
    private static AgentUsageController Controller(IAgentUserTokenQuota provider) => new(provider, new Caller())
    { ControllerContext = new() { HttpContext = new DefaultHttpContext() } };
    private sealed class Clock : TimeProvider { public DateTimeOffset Value = Now; public override DateTimeOffset GetUtcNow() => Value; }
    private sealed class Caller : ICallerContext
    {
        public string UserId => User.ToString("D");
        public string TenantId => "1";
        public Guid? GroupId => Group;
        public Guid? CompanyId => Company;
        public IReadOnlySet<string> Permissions { get; } = new HashSet<string>();
        public string CorrelationId => "offline";
    }
    public class UnusedDependency : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new Xunit.Sdk.XunitException("HistoryRead must not invoke external permission dependencies.");
    }
}
