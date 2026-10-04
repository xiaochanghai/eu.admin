#nullable enable
using System.Runtime.CompilerServices;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Text;
using System.Text.Json;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using EU.Core.Agent.Runtime;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Common;
using EU.Core.Extensions;
using EU.Core.IServices;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.Tasks;
using EU.Core.Model.Entity;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.Repository.Base;
using EU.Core.Repository.UnitOfWorks;
using EU.Core.Services;
using EU.Core.Tests.Service_Test;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Agents.AI;
using OpenAI;
using SqlSugar;
using Xunit;
using AgentRunContext = EU.Core.IServices.Runtime.AgentRunContext;

namespace EU.Core.Tests;

/// <summary>集团公司共享周期配额的隔离测试，不连接部署数据库或外部模型。</summary>
[Collection("Agent policy registration")]
public sealed class AgentUserTokenQuotaTests
{
    private static readonly Guid User = Guid.Parse("aa7602c3-9ff6-4518-b473-832b17ce99a4");
    private static readonly Guid Group = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Company = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static Guid GroupFor(long group) => group == 1 ? Group : Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(null, null, null, true)]
    [InlineData(100L, null, 50L, true)]
    [InlineData(null, 100L, 50L, true)]
    [InlineData(100L, 1000L, 100L, true)]
    [InlineData(0L, null, 1L, false)]
    [InlineData(null, -1L, 1L, false)]
    [InlineData(100L, null, null, false)]
    [InlineData(100L, null, 0L, false)]
    [InlineData(100L, 50L, 100L, false)]
    public void Configuration_requires_explicit_reservation_when_enabled(long? daily, long? monthly, long? reserved, bool valid)
    {
        Assert.Equal(valid, new AgentUserTokenQuotaOptionsValidator().Validate(null,
            new() { DailyTotalTokens = daily, MonthlyTotalTokens = monthly, RequestReservationTokens = reserved }).Succeeded);
        Assert.True(new AgentUserTokenQuotaOptionsValidator().Validate(null,
            new() { DailyTotalTokens = 100, RequestReservationTokens = 50, TimeZoneId = "missing/offline-zone" }).Failed);
        Assert.True(new AgentUserTokenQuotaOptionsValidator().Validate(null, new() { TimeZoneId = "unused/offline-zone" }).Succeeded);
    }

    [Fact]
    public void Period_identity_is_independent_of_agent_and_changes_at_configured_local_boundaries()
    {
        var one = Request(Context());
        var two = Request(Context());
        Assert.Equal(one.Windows, two.Windows);
        Assert.NotEqual(one.RunId, two.RunId);
        var boundary = AgentUserTokenQuotaRequests.Create(Context(), new(2026, 10, 31, 16, 0, 0, TimeSpan.Zero), Zone(), 100, 1000, 50);
        Assert.Equal("20261101@Asia/Shanghai", boundary.Windows[0].Key);
        Assert.Equal("202611@Asia/Shanghai", boundary.Windows[1].Key);
        Assert.Equal(new DateTime(2026, 10, 31, 16, 0, 0, DateTimeKind.Utc), boundary.Windows[0].StartUtc);
        Assert.NotEqual(one.Windows[0].Id, Request(Context(group: 2)).Windows[0].Id);
        Assert.Equal(one.Windows[0].Id, Request(Context(user: Guid.NewGuid())).Windows[0].Id);
        Assert.Throws<AgentRuntimeException>(() => Request(Context() with { ExecutionIdentity = null }));
        Assert.Throws<AgentRuntimeException>(() => Request(Context() with { ExecutionIdentity = new("not-a-user", "1", [], "offline") }));
    }

    [Fact]
    public async Task Different_agents_share_used_and_pending_tokens_and_zero_is_a_valid_settlement()
    {
        using var db = Fixture();
        var service = Service(db);
        var first = Request(Context());
        await service.ReserveAsync(first);
        await service.MarkStartedAsync(first);
        await service.SettleAsync(first, 30);
        var second = Request(Context());
        await service.ReserveAsync(second);
        var periods = await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync();
        Assert.Equal(2, periods.Count);
        Assert.All(periods, x => { Assert.Equal(30, x.UsedTokens); Assert.Equal(50, x.ReservedTokens); });
        await service.MarkStartedAsync(second);
        await service.SettleAsync(second, 0);
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), x => { Assert.Equal(30, x.UsedTokens); Assert.Equal(0, x.ReservedTokens); });
    }

    [Fact]
    public async Task Month_denial_rolls_back_day_preoccupation_and_request_insert()
    {
        using var db = Fixture();
        var service = Service(db);
        var one = Request(Context(), daily: 300, monthly: 100, reserve: 100);
        await service.ReserveAsync(one);
        await service.MarkStartedAsync(one);
        await service.SettleAsync(one, 20);
        var failure = await Assert.ThrowsAsync<AgentRuntimeException>(() => service.ReserveAsync(Request(Context(), 300, 100, 100)));
        Assert.Equal(AgentRunErrorCodes.ModelTokenQuotaExceeded, failure.ErrorCode);
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), x => { Assert.Equal(20, x.UsedTokens); Assert.Equal(0, x.ReservedTokens); });
        Assert.Equal(1, await db.Db.Queryable<AgUserTokenQuotaReservation>().CountAsync());
    }

    [Fact]
    public async Task Unknown_usage_keeps_reservation_and_freezes_period_instead_of_refunding_zero()
    {
        using var db = Fixture();
        var service = Service(db);
        var one = Request(Context());
        await service.ReserveAsync(one);
        await service.MarkStartedAsync(one);
        await service.SettleAsync(one, null);
        await service.SettleAsync(one, null);
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), x => { Assert.True(x.HasUnknownUsage); Assert.Equal(50, x.ReservedTokens); Assert.Equal(0, x.UsedTokens); });
        Assert.Equal(3, (await db.Db.Queryable<AgUserTokenQuotaReservation>().SingleAsync()).State);
        Assert.Equal(AgentRunErrorCodes.ModelTokenUsageUnavailable,
            (await Assert.ThrowsAsync<AgentRuntimeException>(() => Service(db).ReserveAsync(Request(Context())))).ErrorCode);
    }

    [Fact]
    public async Task Settlement_is_idempotent_and_conflicts_or_repeat_dispatch_cannot_double_charge()
    {
        using var db = Fixture();
        var service = Service(db);
        var request = Request(Context());
        await service.ReserveAsync(request);
        await Assert.ThrowsAsync<AgentRuntimeException>(() => service.ReserveAsync(request));
        await service.MarkStartedAsync(request);
        await Assert.ThrowsAsync<AgentRuntimeException>(() => service.MarkStartedAsync(request));
        await service.SettleAsync(request, 40);
        await Service(db).SettleAsync(request, 40);
        await Assert.ThrowsAsync<AgentRuntimeException>(() => service.SettleAsync(request, 41));
        await Assert.ThrowsAsync<AgentRuntimeException>(() => service.SettleAsync(request, null));
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), x => { Assert.Equal(40, x.UsedTokens); Assert.Equal(0, x.ReservedTokens); });
    }

    [Theory]
    [InlineData("group")]
    [InlineData("company")]
    [InlineData("user")]
    [InlineData("run")]
    [InlineData("amount")]
    [InlineData("period")]
    public async Task Cross_owner_or_changed_reservation_cannot_release_or_charge_another_consumers_reservation(string change)
    {
        using var db = Fixture();
        var service = Service(db);
        var original = Request(Context());
        await service.ReserveAsync(original);
        var forged = change switch
        {
            "group" => original with { GroupId = Guid.NewGuid() },
            "company" => original with { CompanyId = Guid.NewGuid() },
            "user" => original with { ConsumerUserId = Guid.NewGuid() },
            "run" => original with { RunId = Guid.NewGuid() },
            "amount" => original with { ReservedTokens = 10 },
            _ => original with { Windows = [original.Windows[0] with { Id = Guid.NewGuid() }, original.Windows[1]] }
        };
        await Assert.ThrowsAsync<AgentRuntimeException>(() => service.MarkStartedAsync(forged));
        await Assert.ThrowsAsync<AgentRuntimeException>(() => service.SettleAsync(forged, null, true));
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), x => Assert.Equal(50, x.ReservedTokens));
        await service.MarkStartedAsync(original);
        await Assert.ThrowsAsync<AgentRuntimeException>(() => service.SettleAsync(forged, 5));
        await service.SettleAsync(original, 5);
        await service.ReserveAsync(Request(Context(group: 2)));
        await service.ReserveAsync(Request(Context(user: Guid.NewGuid())));
        Assert.Equal(4, await db.Db.Queryable<AgUserTokenQuotaPeriod>().CountAsync());
    }

    [Fact]
    public async Task Recreated_service_keeps_pending_reservations_and_settles_original_periods_after_midnight()
    {
        using var db = Fixture();
        var request = Request(Context(), daily: 50, monthly: 50, reserve: 50);
        await Service(db).ReserveAsync(request);
        await Service(db).MarkStartedAsync(request);
        await Assert.ThrowsAsync<AgentRuntimeException>(() => Service(db).ReserveAsync(Request(Context(), 50, 50, 50)));
        var next = AgentUserTokenQuotaRequests.Create(Context(), Now.AddMonths(1), Zone(), 50, 50, 50);
        await Service(db).ReserveAsync(next);
        await Service(db).SettleAsync(request, 20);
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().Where(x => x.ID == request.Windows[0].Id || x.ID == request.Windows[1].Id).ToListAsync(), x => { Assert.Equal(20, x.UsedTokens); Assert.Equal(0, x.ReservedTokens); });
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().Where(x => x.ID == next.Windows[0].Id || x.ID == next.Windows[1].Id).ToListAsync(), x => { Assert.Equal(0, x.UsedTokens); Assert.Equal(50, x.ReservedTokens); });
    }

    [Fact]
    public async Task Release_is_only_for_unstarted_requests_and_precancelled_reservation_does_not_write()
    {
        using var db = Fixture();
        var service = Service(db);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ReserveAsync(Request(Context()), cancellation.Token));
        Assert.Equal(0, await db.Db.Queryable<AgUserTokenQuotaPeriod>().CountAsync());
        var request = Request(Context());
        await service.ReserveAsync(request);
        await service.SettleAsync(request, null, true);
        await service.SettleAsync(request, null, true);
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), x => Assert.Equal(0, x.ReservedTokens));
        var started = Request(Context());
        await service.ReserveAsync(started);
        await service.MarkStartedAsync(started);
        await Assert.ThrowsAsync<AgentRuntimeException>(() => service.SettleAsync(started, null, true));
    }

    [Fact]
    public async Task Concurrent_independent_connections_never_reserve_the_same_remaining_capacity()
    {
        // 唯一测试专属临时文件，仅在本地 SQLite 隔离数据库中验证真实并发事务。
        string path = Path.Combine(Path.GetTempPath(), "eu-agent-quota-test-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            using var setup = FileDatabase(path);
            setup.CodeFirst.InitTables(typeof(AgUserTokenQuotaPeriod), typeof(AgUserTokenQuotaReservation));
            using var one = FileDatabase(path);
            using var two = FileDatabase(path);
            using var gate = new SemaphoreSlim(0);
            async Task<bool> Attempt(SqlSugarScope db)
            {
                await gate.WaitAsync();
                try { await Service(db).ReserveAsync(Request(Context(), 50, 50, 50)); return true; }
                catch (Exception exception) when (exception is AgentRuntimeException || exception.GetType().Name.Contains("Sqlite", StringComparison.Ordinal)) { return false; }
            }
            Task<bool> a = Task.Run(() => Attempt(one));
            Task<bool> b = Task.Run(() => Attempt(two));
            gate.Release(2);
            Assert.Single(await Task.WhenAll(a, b), x => x);
            Assert.All(await setup.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), x => Assert.Equal(50, x.ReservedTokens));
            Assert.Equal(1, await setup.Queryable<AgUserTokenQuotaReservation>().CountAsync());
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(25L)]
    [InlineData(70L)]
    [InlineData(null)]
    [InlineData(-1L)]
    public async Task Streaming_reports_settle_once_and_failures_preserve_real_usage(long? total)
    {
        using var db = Fixture();
        using var host = Host(db);
        var inner = new ScriptedClient(total);
        using var client = new AgentUserTokenQuotaChatClient(inner, host.GetRequiredService<IAgentUserTokenQuota>(), Context());
        var original = new ChatOptions { MaxOutputTokens = 20, Instructions = "retain" };
        var updates = new List<ChatResponseUpdate>();
        var exception = await Record.ExceptionAsync(async () => { await foreach (var x in client.GetStreamingResponseAsync([], original)) updates.Add(x); });
        Assert.Equal(2, updates.Count); // 重复累计快照只结算一次，更新在超限/未知异常之前可见。
        Assert.Equal(20, original.MaxOutputTokens);
        Assert.NotSame(original, inner.Options);
        Assert.Equal(20, inner.Options!.MaxOutputTokens);
        var entry = await db.Db.Queryable<AgUserTokenQuotaReservation>().SingleAsync();
        Assert.Equal(total is >= 0 ? total : null, entry.ActualTokens);
        if (total is null or < 0) Assert.Equal(AgentRunErrorCodes.ModelTokenUsageUnavailable, Assert.IsType<AgentRuntimeException>(exception).ErrorCode);
        else if (total > 50) Assert.Equal(AgentRunErrorCodes.ModelTokenQuotaExceeded, Assert.IsType<AgentRuntimeException>(exception).ErrorCode);
        else Assert.Null(exception);
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), x =>
        {
            Assert.Equal(total is >= 0 ? total.Value : 0, x.UsedTokens);
            Assert.Equal(total is null or < 0 ? 50 : 0, x.ReservedTokens);
        });
    }

    [Theory]
    [InlineData("failure")]
    [InlineData("cancel")]
    [InlineData("early-dispose")]
    public async Task Interrupted_or_disposed_stream_freezes_quota_without_using_cancelled_cleanup_token(string outcome)
    {
        using var db = Fixture();
        using var host = Host(db);
        using var cancellation = new CancellationTokenSource();
        var inner = new ScriptedClient(10, outcome, cancellation);
        using var client = new AgentUserTokenQuotaChatClient(inner, host.GetRequiredService<IAgentUserTokenQuota>(), Context());
        if (outcome == "early-dispose") { await foreach (var x in client.GetStreamingResponseAsync([], cancellationToken: cancellation.Token)) break; }
        else await Assert.ThrowsAnyAsync<Exception>(async () => { await foreach (var x in client.GetStreamingResponseAsync([], cancellationToken: cancellation.Token)) { } });
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), x => { Assert.True(x.HasUnknownUsage); Assert.Equal(50, x.ReservedTokens); });
        Assert.Equal(3, (await db.Db.Queryable<AgUserTokenQuotaReservation>().SingleAsync()).State);
    }

    [Fact]
    public async Task Disabled_provider_does_not_resolve_database_or_require_identity_and_known_calls_share_quota()
    {
        using var disabled = new ServiceCollection().BuildServiceProvider();
        var provider = new AgUserTokenQuotaProvider(disabled.GetRequiredService<IServiceScopeFactory>(), Options.Create(new AgentUserTokenQuotaOptions()), new Clock(), NullLogger<AgUserTokenQuotaProvider>.Instance);
        var unknown = new ScriptedClient(null);
        using (var client = new AgentUserTokenQuotaChatClient(unknown, provider, Context() with { ExecutionIdentity = null }))
            await client.GetResponseAsync([]);
        Assert.Equal(1, unknown.Calls);
        using var db = Fixture();
        using var host = Host(db);
        var quota = host.GetRequiredService<IAgentUserTokenQuota>();
        for (int i = 0; i < 2; i++)
        {
            using var client = new AgentUserTokenQuotaChatClient(new ScriptedClient(30), quota, Context());
            await client.GetResponseAsync([]);
        }
        var blocked = new ScriptedClient(1);
        using var third = new AgentUserTokenQuotaChatClient(blocked, quota, Context());
        Assert.Equal(AgentRunErrorCodes.ModelTokenQuotaExceeded, (await Assert.ThrowsAsync<AgentRuntimeException>(() => third.GetResponseAsync([]))).ErrorCode);
        Assert.Equal(0, blocked.Calls);
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), x => Assert.Equal(60, x.UsedTokens));
    }

    [Fact]
    public async Task Missing_identity_or_database_fails_closed_before_supplier_call_without_leaking_error()
    {
        using var db = Fixture();
        using var host = Host(db);
        var inner = new ScriptedClient(1);
        using var client = new AgentUserTokenQuotaChatClient(inner, host.GetRequiredService<IAgentUserTokenQuota>(), Context() with { ExecutionIdentity = null });
        Assert.Equal(AgentRunErrorCodes.ModelTokenQuotaUnavailable, (await Assert.ThrowsAsync<AgentRuntimeException>(() => client.GetResponseAsync([]))).ErrorCode);
        Assert.Equal(0, inner.Calls);
        using var broken = new ServiceCollection().AddScoped<IAgUserTokenQuotaServices>(_ => throw new IOException("private-database-error"))
            .BuildServiceProvider();
        var provider = new AgUserTokenQuotaProvider(broken.GetRequiredService<IServiceScopeFactory>(), Options.Create(Configuration()), new Clock(), NullLogger<AgUserTokenQuotaProvider>.Instance);
        using var unavailable = new AgentUserTokenQuotaChatClient(inner, provider, Context());
        var error = await Assert.ThrowsAsync<AgentRuntimeException>(() => unavailable.GetResponseAsync([]));
        Assert.Equal(AgentRunErrorCodes.ModelTokenQuotaUnavailable, error.ErrorCode);
        Assert.DoesNotContain("private-database-error", error.ToString());
        Assert.Equal(0, inner.Calls);
    }

    [Fact]
    public async Task Actual_autofac_registration_keeps_database_services_short_lived_and_provider_singleton()
    {
        using var db = Fixture();
        var before = AppSettings.Configuration;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        using var configurationCleanup = configuration as IDisposable;
        try
        {
            AppSettings.Configuration = configuration;
            var services = new ServiceCollection();
            services.AddSingleton<IOptions<AgentUserTokenQuotaOptions>>(Options.Create(Configuration()));
            services.AddSingleton<TimeProvider>(new Clock());
            services.AddSingleton<Microsoft.Extensions.Logging.ILogger<AgUserTokenQuotaProvider>>(NullLogger<AgUserTokenQuotaProvider>.Instance);
            services.AddSingleton<IAgentUserTokenQuota, AgUserTokenQuotaProvider>();
            services.ValidateAgentServiceLifetimes();
            var builder = new ContainerBuilder();
            builder.Populate(services);
            builder.RegisterModule(new AutofacModuleRegister());
            builder.RegisterInstance(db.CreateRepository<AgUserTokenQuotaPeriod>()).As<EU.Core.IRepository.Base.IBaseRepository<AgUserTokenQuotaPeriod>>();
            using var container = builder.Build();
            using var one = container.BeginLifetimeScope();
            using var two = container.BeginLifetimeScope();
            Assert.NotSame(one.Resolve<IAgUserTokenQuotaServices>(), two.Resolve<IAgUserTokenQuotaServices>());
            var provider = one.Resolve<IAgentUserTokenQuota>();
            Assert.Same(provider, two.Resolve<IAgentUserTokenQuota>());
            await using var lease = await provider.ReserveAsync(Context());
            await lease!.MarkStartedAsync();
            await lease.CompleteAsync(20);
            Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), x => Assert.Equal(20, x.UsedTokens));
        }
        finally { AppSettings.Configuration = before; }
    }

    [Fact]
    public async Task Installed_sdk_parent_and_child_share_user_quota_without_double_charging_parent_aggregate()
    {
        using var db = Fixture();
        using var host = Host(db);
        var quota = host.GetRequiredService<IAgentUserTokenQuota>();
        var parentContext = Context();
        var childContext = Context();
        using var handler = new SdkHandler();
        using var http = new HttpClient(handler);
        var providerClient = new OpenAIClient(new ApiKeyCredential("offline-placeholder"), new OpenAIClientOptions
        { Endpoint = new Uri("https://offline.invalid/v1"), Transport = new HttpClientPipelineTransport(http) }).GetChatClient("qwen-offline").AsIChatClient();
        using var client = new AgentTokenBudgetChatClient(new AgentUserTokenQuotaChatClient(providerClient, quota, parentContext), new AgentRuntimeOptions(TimeSpan.FromSeconds(5), MaximumModelOutputTokens: 10));
        int childCalls = 0;
        AITool tool = AIFunctionFactory.Create(async () =>
        {
            using var child = new AgentUserTokenQuotaChatClient(new ScriptedClient(7), quota, childContext);
            await child.GetResponseAsync([]);
            childCalls++;
            return "child completed";
        }, name: "lookup");
        var agent = client.AsAIAgent(new ChatClientAgentOptions
        { ChatOptions = MicrosoftAgentRuntimeEngine.CreateChatOptions("qwen-offline", "instructions", [tool], false) });
        async Task Execute() { await foreach (var update in agent.RunStreamingAsync([new ChatMessage(ChatRole.User, "offline")])) { } }
        await Execute().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, childCalls);
        Assert.Equal(2, handler.Calls);
        var entries = await db.Db.Queryable<AgUserTokenQuotaReservation>().ToListAsync();
        Assert.Equal(3, entries.Count);
        Assert.Equal(2, entries.Count(x => x.RunId == parentContext.RunId));
        Assert.Single(entries, x => x.RunId == childContext.RunId);
        Assert.All(entries, x => Assert.Equal(2, x.State));
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), x => { Assert.Equal(18, x.UsedTokens); Assert.Equal(0, x.ReservedTokens); });
    }

    [Fact]
    public async Task Local_budget_rejection_does_not_create_a_persistent_reservation()
    {
        using var db = Fixture();
        using var host = Host(db);
        var inner = new ScriptedClient(10);
        using var client = new AgentTokenBudgetChatClient(new AgentUserTokenQuotaChatClient(inner, host.GetRequiredService<IAgentUserTokenQuota>(), Context()),
            new AgentRuntimeOptions(TimeSpan.FromSeconds(5), MaximumRunTotalTokens: 10));
        await client.GetResponseAsync([]);
        await Assert.ThrowsAsync<AgentRuntimeException>(() => client.GetResponseAsync([]));
        Assert.Equal(1, inner.Calls);
        Assert.Equal(1, await db.Db.Queryable<AgUserTokenQuotaReservation>().CountAsync());
    }

    [Fact]
    public async Task Unstarted_lease_cancellation_releases_only_the_pending_reservation()
    {
        using var db = Fixture();
        using var host = Host(db);
        var provider = host.GetRequiredService<IAgentUserTokenQuota>();
        using var cancellation = new CancellationTokenSource();
        await using (var lease = await provider.ReserveAsync(Context()))
        {
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => lease!.MarkStartedAsync(cancellation.Token));
        }
        Assert.Equal(4, (await db.Db.Queryable<AgUserTokenQuotaReservation>().SingleAsync()).State);
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), x => { Assert.Equal(0, x.ReservedTokens); Assert.False(x.HasUnknownUsage); });
    }

    [Fact]
    public async Task Settlement_failure_is_safe_and_stream_usage_remains_visible_before_failure()
    {
        using var db = Fixture();
        var service = Service(db);
        var proxy = System.Reflection.DispatchProxy.Create<IAgUserTokenQuotaServices, SettlementFailureProxy>();
        ((SettlementFailureProxy)(object)proxy).Service = service;
        using var host = new ServiceCollection().AddScoped<IAgUserTokenQuotaServices>(_ => proxy).BuildServiceProvider();
        var provider = new AgUserTokenQuotaProvider(host.GetRequiredService<IServiceScopeFactory>(), Options.Create(Configuration()), new Clock(), NullLogger<AgUserTokenQuotaProvider>.Instance);
        using var client = new AgentUserTokenQuotaChatClient(new ScriptedClient(25), provider, Context());
        var updates = new List<ChatResponseUpdate>();
        var failure = await Assert.ThrowsAsync<AgentRuntimeException>(async () => { await foreach (var update in client.GetStreamingResponseAsync([])) updates.Add(update); });
        Assert.Equal(AgentRunErrorCodes.ModelTokenQuotaUnavailable, failure.ErrorCode);
        Assert.DoesNotContain("private-settlement-error", failure.ToString());
        Assert.Equal(2, updates.Count);
        Assert.Equal(1, (await db.Db.Queryable<AgUserTokenQuotaReservation>().SingleAsync()).State);
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), x => Assert.Equal(50, x.ReservedTokens));
    }

    [Fact]
    public async Task Output_truncation_fails_but_known_actual_usage_is_still_persisted()
    {
        using var db = Fixture();
        using var host = Host(db);
        using var client = new AgentUserTokenQuotaChatClient(new ScriptedClient(25, "length"), host.GetRequiredService<IAgentUserTokenQuota>(), Context());
        Assert.Equal(AgentRunErrorCodes.ModelTokenQuotaExceeded,
            (await Assert.ThrowsAsync<AgentRuntimeException>(() => client.GetResponseAsync([]))).ErrorCode);
        Assert.Equal(25, (await db.Db.Queryable<AgUserTokenQuotaReservation>().SingleAsync()).ActualTokens);
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), x => { Assert.Equal(25, x.UsedTokens); Assert.Equal(0, x.ReservedTokens); });
    }

    public class SettlementFailureProxy : System.Reflection.DispatchProxy
    {
        public AgUserTokenQuotaServices Service = null!;
        protected override object? Invoke(System.Reflection.MethodInfo? method, object?[]? arguments)
        {
            if (method!.Name == nameof(IAgUserTokenQuotaServices.SettleAsync)) return Task.FromException(new IOException("private-settlement-error"));
            return method.Invoke(Service, arguments);
        }
    }

    private sealed class SdkHandler : HttpMessageHandler
    {
        public int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            Assert.True(json.RootElement.GetProperty("stream_options").GetProperty("include_usage").GetBoolean());
            bool first = ++Calls == 1;
            string delta = first ? """{"tool_calls":[{"index":0,"id":"lookup-1","type":"function","function":{"name":"lookup","arguments":"{}"}}]}""" : """{"content":"ok"}""";
            string usage = first ? """{"prompt_tokens":3,"completion_tokens":2,"total_tokens":5}""" : """{"prompt_tokens":4,"completion_tokens":2,"total_tokens":6}""";
            string response = $"data: {{\"id\":\"offline-{Calls}\",\"object\":\"chat.completion.chunk\",\"created\":0,\"model\":\"qwen-offline\",\"choices\":[{{\"index\":0,\"delta\":{delta},\"finish_reason\":null}}]}}\n\n" +
                $"data: {{\"id\":\"offline-{Calls}\",\"object\":\"chat.completion.chunk\",\"created\":0,\"model\":\"qwen-offline\",\"choices\":[{{\"index\":0,\"delta\":{{}},\"finish_reason\":\"{(first ? "tool_calls" : "stop")}\"}}]}}\n\n" +
                $"data: {{\"id\":\"offline-{Calls}\",\"object\":\"chat.completion.chunk\",\"created\":0,\"model\":\"qwen-offline\",\"choices\":[],\"usage\":{usage}}}\n\ndata: [DONE]\n\n";
            return new(HttpStatusCode.OK) { Content = new StringContent(response, Encoding.UTF8, "text/event-stream") };
        }
    }

    [Fact]
    public async Task Tenant_claim_does_not_split_group_company_quota()
    {
        using var db = Fixture();
        var service = Service(db);
        var first = Request(Context());
        await service.ReserveAsync(first);
        await service.MarkStartedAsync(first);
        await service.SettleAsync(first, 30);
        var identity = new AgentExecutionIdentity(User.ToString("D"), "legacy-tenant-different", [], "offline") { GroupId = Group, CompanyId = Company };
        var next = Request(Context() with { ExecutionIdentity = identity });
        Assert.Equal(first.Windows, next.Windows);
        await service.ReserveAsync(next);
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), x =>
        { Assert.Equal(Group, x.GroupId); Assert.Equal(Company, x.CompanyId); Assert.Equal(30, x.UsedTokens); Assert.Equal(50, x.ReservedTokens); });
        Assert.Equal(2, await db.Db.Queryable<AgUserTokenQuotaPeriod>().CountAsync());
    }

    [Fact]
    public void Quota_entities_reuse_base_scope_without_tenant_entity_or_tenant_column()
    {
        foreach (var type in new[] { typeof(AgUserTokenQuotaPeriod), typeof(AgUserTokenQuotaReservation), typeof(AgUserTokenQuotaPolicy), typeof(AgUserTokenQuotaAdjustment) })
        {
            Assert.Null(type.GetProperty("TenantId"));
            Assert.DoesNotContain(type.GetInterfaces(), x => x.Name == "ITenantEntity");
            Assert.Equal(typeof(EU.Core.Model.Models.RootTkey.BaseEntity), type.GetProperty("GroupId")!.DeclaringType);
            Assert.Equal(typeof(EU.Core.Model.Models.RootTkey.BaseEntity), type.GetProperty("CompanyId")!.DeclaringType);
        }
        Assert.Null(typeof(AgUserTokenQuotaPeriod).GetProperty("UserId"));
        Assert.Null(typeof(AgUserTokenQuotaPolicy).GetProperty("UserId"));
        Assert.Null(typeof(AgUserTokenQuotaAdjustment).GetProperty("UserId"));
        Assert.Null(typeof(AgUserTokenQuotaReservation).GetProperty("UserId"));
        Assert.NotNull(typeof(AgUserTokenQuotaReservation).GetProperty("ConsumerUserId"));
    }

    [Fact]
    public async Task Deferred_task_persistence_preserves_original_group_company_and_blocks_scope_changed_replay()
    {
        using var db = new AgentPersistenceSqliteFixture(typeof(AgAgentTask), typeof(AgAgentTaskAttempt), typeof(AgAgentTaskEvent));
        var tasks = new AgAgentTaskServices(db.CreateRepository<AgAgentTask>());
        var command = new CreateAgentTaskCommand("1", User.ToString("D"), "offline task", "", "hello", "chat", "", "scope-test", null, 0, 3, Now)
        { GroupId = Group, CompanyId = Company };
        var created = await tasks.CreateAsync(command);
        var restored = await new AgAgentTaskServices(db.CreateRepository<AgAgentTask>()).GetAsync(created.Id, "1", User.ToString("D"));
        Assert.NotNull(restored);
        Assert.Equal(Group, restored.GroupId); Assert.Equal(Company, restored.CompanyId);
        var entity = await db.Db.Queryable<AgAgentTask>().SingleAsync();
        Assert.Equal(Group, entity.GroupId); Assert.Equal(Company, entity.CompanyId);
        await Assert.ThrowsAsync<AgentTaskException>(() => tasks.CreateAsync(command with { CompanyId = Guid.NewGuid() }));
        Assert.Equal(1, await db.Db.Queryable<AgAgentTask>().CountAsync());
        Assert.DoesNotContain("GroupId", JsonSerializer.Serialize(restored));
        Assert.DoesNotContain("CompanyId", JsonSerializer.Serialize(restored));
    }

    [Fact]
    public void Http_scope_uses_existing_user_context_not_query_parameters()
    {
        var user = System.Reflection.DispatchProxy.Create<EU.Core.Common.HttpContextUser.IUser, ScopeUser>();
        var http = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        http.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity([], "offline"));
        http.Request.QueryString = new("?groupId=" + Guid.NewGuid() + "&companyId=" + Guid.NewGuid());
        var caller = new EU.Core.Api.Agent.Security.HttpCallerContext(new Microsoft.AspNetCore.Http.HttpContextAccessor { HttpContext = http }, user);
        Assert.Equal(Group, caller.GroupId); Assert.Equal(Company, caller.CompanyId);
        Assert.Equal(User.ToString("D"), caller.UserId);
    }

    public class ScopeUser : System.Reflection.DispatchProxy
    {
        protected override object? Invoke(System.Reflection.MethodInfo? method, object?[]? args) => method?.Name switch
        {
            "get_ID" => (Guid?)User, "get_TenantId" => 0L, "get_GroupId" => (Guid?)Group, "get_CompanyId" => (Guid?)Company,
            _ => throw new InvalidOperationException("External identity calls are forbidden in this test.")
        };
    }

    private static TimeZoneInfo Zone() => TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");
    private static AgentRunContext Context(long group = 1, Guid? user = null) => new(Guid.NewGuid(), Guid.NewGuid(),
        new AgentVersionSnapshot(Guid.NewGuid(), "offline", "instructions", "offline-profile", AgentOutputMode.Text, null, [], []),
        "hello", "hash", Now, []) { ExecutionIdentity = new((user ?? User).ToString("D"), "1", [], "offline") { GroupId = GroupFor(group), CompanyId = Company } };
    private static AgentUserTokenQuotaRequest Request(AgentRunContext context, long daily = 100, long monthly = 1000, long reserve = 50) =>
        AgentUserTokenQuotaRequests.Create(context, Now, Zone(), daily, monthly, reserve);
    private static AgentPersistenceSqliteFixture Fixture() => new(typeof(AgUserTokenQuotaPeriod), typeof(AgUserTokenQuotaReservation));
    private static AgUserTokenQuotaServices Service(AgentPersistenceSqliteFixture db) => new(db.CreateRepository<AgUserTokenQuotaPeriod>());
    private static AgUserTokenQuotaServices Service(SqlSugarScope db) => new(new BaseRepository<AgUserTokenQuotaPeriod>(new UnitOfWorkManage(db, NullLogger<UnitOfWorkManage>.Instance)));
    private static SqlSugarScope FileDatabase(string path) => new(new ConnectionConfig { ConnectionString = "Data Source=" + path + ";Pooling=False", DbType = DbType.Sqlite, IsAutoCloseConnection = true });
    private static AgentUserTokenQuotaOptions Configuration() => new() { DailyTotalTokens = 100, MonthlyTotalTokens = 1000, RequestReservationTokens = 50 };
    private static ServiceProvider Host(AgentPersistenceSqliteFixture db) => new ServiceCollection()
        .AddScoped<IAgUserTokenQuotaServices>(_ => Service(db))
        .AddSingleton<IOptions<AgentUserTokenQuotaOptions>>(Options.Create(Configuration()))
        .AddSingleton<TimeProvider>(new Clock())
        .AddSingleton<Microsoft.Extensions.Logging.ILogger<AgUserTokenQuotaProvider>>(NullLogger<AgUserTokenQuotaProvider>.Instance)
        .AddSingleton<IAgentUserTokenQuota, AgUserTokenQuotaProvider>().BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class ScriptedClient(long? total, string outcome = "success", CancellationTokenSource? cancellation = null) : IChatClient
    {
        public int Calls;
        public ChatOptions? Options;
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        { Calls++; Options = options; return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")) { Usage = new UsageDetails { TotalTokenCount = total }, FinishReason = outcome == "length" ? ChatFinishReason.Length : ChatFinishReason.Stop }); }
        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Calls++; Options = options;
            yield return Report();
            await Task.Yield();
            if (outcome == "failure") throw new IOException("offline failure");
            if (outcome == "cancel") { cancellation!.Cancel(); cancellationToken.ThrowIfCancellationRequested(); }
            yield return Report();
        }
        private ChatResponseUpdate Report() => new() { ResponseId = "offline-response", FinishReason = ChatFinishReason.Stop,
            Contents = [new UsageContent(new UsageDetails { TotalTokenCount = total })] };
        public object? GetService(Type serviceType, object? serviceKey = null) => serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;
        public void Dispose() { }
    }
}
