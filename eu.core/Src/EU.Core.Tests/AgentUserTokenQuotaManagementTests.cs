#nullable enable
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Diagnostics.Metrics;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Controllers;
using EU.Core.Api.Agent.Errors;
using EU.Core.Api.Agent.Observability;
using EU.Core.Api.Agent.Security;
using EU.Core.Common.HttpContextUser;
using EU.Core.IServices;
using EU.Core.IServices.Runtime;
using EU.Core.Model;
using EU.Core.Model.Entity;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.Services;
using EU.Core.Tests.Service_Test;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EU.Core.Tests;

/// <summary>真实业务服务 + 隔离 SQLite，不连接部署数据库、用户库或供应商。</summary>
[Collection("Agent policy registration")]
public sealed class AgentUserTokenQuotaManagementTests
{
    private static readonly Guid User = Guid.Parse("50c61a38-f65d-482a-8742-263c27024e63");
    private static readonly Guid Group = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Company = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static Guid GroupFor(long group) => group == 1 ? Group : Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Operator = Guid.Parse("57bbd788-f370-4620-bc18-c4a460494feb");
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Default_policy_read_does_not_create_records()
    {
        using var db = Fixture();
        var policy = await Service(db).GetPolicyAsync(Group, Company);
        Assert.True(policy.UseDefaults); Assert.Equal(0, policy.Revision);
        Assert.Equal(0, await db.Db.Queryable<AgUserTokenQuotaPolicy>().CountAsync());
    }

    [Fact]
    public async Task Policy_save_is_versioned_idempotent_and_audited_with_exact_long_values()
    {
        using var db = Fixture(); var service = Service(db);
        var input = Policy(daily: long.MaxValue);
        var first = await service.SavePolicyAsync(Group, Company, Operator, input);
        Assert.Equal(first, await service.SavePolicyAsync(Group, Company, Operator, input));
        Assert.Equal(1, first.Revision); Assert.Equal(long.MaxValue, first.DailyTotalTokens);
        var audit = await db.Db.Queryable<AgUserTokenQuotaAdjustment>().SingleAsync();
        Assert.Equal(Operator, audit.OperatorId); Assert.Equal(Group, audit.GroupId); Assert.Equal(Company, audit.CompanyId);
        Assert.Equal("Policy", audit.Kind); Assert.Equal("核实配置", audit.Reason);
        Assert.Contains("9223372036854775807", audit.ResultJson);
        Assert.Equal(1, await db.Db.Queryable<AgUserTokenQuotaPolicy>().CountAsync());
    }

    [Theory]
    [InlineData("changed-input")]
    [InlineData("different-group")]
    [InlineData("different-company")]
    [InlineData("different-operator")]
    public async Task Operation_id_cannot_be_reused_for_another_command_or_tenant(string scenario)
    {
        using var db = Fixture(); var service = Service(db); var input = Policy();
        await service.SavePolicyAsync(Group, Company, Operator, input);
        var error = await Assert.ThrowsAsync<AgentRuntimeException>(() => service.SavePolicyAsync(scenario == "different-group" ? Guid.NewGuid() : Group,
            scenario == "different-company" ? Guid.NewGuid() : Company,
            scenario == "different-operator" ? Guid.NewGuid() : Operator,
            scenario == "changed-input" ? Policy(input.OperationId, daily: 200) : input));
        Assert.Equal(AgentUserTokenQuotaManagementErrors.Conflict, error.ErrorCode);
        Assert.Equal(1, await db.Db.Queryable<AgUserTokenQuotaAdjustment>().CountAsync());
    }

    [Fact]
    public async Task Stale_policy_save_and_stale_reservation_are_rejected_without_mutation()
    {
        using var db = Fixture(); var service = Service(db);
        await service.SavePolicyAsync(Group, Company, Operator, Policy());
        var error = await Assert.ThrowsAsync<AgentRuntimeException>(() => service.SavePolicyAsync(Group, Company, Operator, Policy()));
        Assert.Equal(AgentUserTokenQuotaManagementErrors.Conflict, error.ErrorCode);
        var request = Request() with { PolicyRevision = 0 };
        error = await Assert.ThrowsAsync<AgentRuntimeException>(() => service.ReserveAsync(request));
        Assert.Equal(AgentUserTokenQuotaManagementErrors.Conflict, error.ErrorCode);
        Assert.Equal(0, await db.Db.Queryable<AgUserTokenQuotaPeriod>().CountAsync());
        Assert.Equal(0, await db.Db.Queryable<AgUserTokenQuotaReservation>().CountAsync());
    }

    [Fact]
    public async Task Lowering_limits_and_restoring_defaults_never_reset_usage()
    {
        using var db = Fixture(); var service = Service(db); var request = Request();
        await service.ReserveAsync(request); await service.MarkStartedAsync(request); await service.SettleAsync(request, 70);
        await service.SavePolicyAsync(Group, Company, Operator, Policy(daily: 50));
        var before = JsonSerializer.Serialize(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync());
        await service.SavePolicyAsync(Group, Company, Operator, new() { OperationId = Guid.NewGuid(), ExpectedRevision = 1, UseDefaults = true, Reason = "恢复默认" });
        Assert.Equal(before, JsonSerializer.Serialize(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync()));
        Assert.True((await service.GetPolicyAsync(Group, Company)).UseDefaults);
    }

    [Fact]
    public async Task Tenant_policy_applies_to_all_users_and_is_business_scope_scoped()
    {
        using var db = Fixture(); var service = Service(db);
        await service.SavePolicyAsync(Group, Company, Operator, Policy(daily: 100));
        using var host = new ServiceCollection().AddScoped<IAgUserTokenQuotaServices>(_ => Service(db)).BuildServiceProvider();
        var provider = new AgUserTokenQuotaProvider(host.GetRequiredService<IServiceScopeFactory>(), Options.Create(new AgentUserTokenQuotaOptions { ManagementEnabled = true }),
            new Clock(), NullLogger<AgUserTokenQuotaProvider>.Instance);
        var balance = await provider.GetBalanceAsync(Identity());
        Assert.True(balance.Enabled); Assert.Equal(100, balance.Periods[0].LimitTokens);
        Assert.False((await provider.GetBalanceAsync(Identity(2))).Enabled);
        Assert.True((await provider.GetBalanceAsync(Identity(1, Guid.NewGuid()))).Enabled);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_or_null_actual_tokens_are_rejected_before_reconciliation_without_releasing_quota(bool explicitNull)
    {
        using var db = Fixture(); var service = Service(db); var request = Request();
        await service.ReserveAsync(request); await service.MarkStartedAsync(request); await service.SettleAsync(request, null);
        var body = JsonSerializer.SerializeToElement(Reconcile(0)).EnumerateObject()
            .Where(property => property.Name != nameof(AgentUserTokenQuotaReconcileInput.ActualTokens))
            .ToDictionary(property => property.Name, property => property.Value);
        if (explicitNull) body[nameof(AgentUserTokenQuotaReconcileInput.ActualTokens)] = JsonSerializer.SerializeToElement<object?>(null);
        var json = JsonSerializer.Serialize(body);
        await Assert.ThrowsAsync<JsonException>(async () =>
        {
            var input = JsonSerializer.Deserialize<AgentUserTokenQuotaReconcileInput>(json) ?? throw new JsonException();
            await service.ReconcileAsync(Group, Company, Operator, request.Id, input);
        });
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), row =>
        { Assert.Equal(0, row.UsedTokens); Assert.Equal(50, row.ReservedTokens); Assert.True(row.HasUnknownUsage); });
        Assert.Equal(3, (await service.GetPendingAsync(Group, Company)).Single().State);
        Assert.Equal(0, await db.Db.Queryable<AgUserTokenQuotaAdjustment>().CountAsync());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"ActualTokens\":null}")]
    [InlineData("{\"actualTokens\":null}")]
    public async Task Mvc_binding_rejects_missing_or_null_actual_tokens_with_standard_400(string body)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "application/json";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
        httpContext.Request.Body = stream;
        var modelState = new ModelStateDictionary();
        var metadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(AgentUserTokenQuotaReconcileInput));
        var context = new InputFormatterContext(httpContext, "input", modelState, metadata, (input, encoding) => new StreamReader(input, encoding));
        var jsonOptions = new JsonOptions();
        // Same MVC setup as the host: PascalCase output, case-insensitive input and System.Text.Json.
        jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = null;
        var formatter = new SystemTextJsonInputFormatter(jsonOptions, NullLogger<SystemTextJsonInputFormatter>.Instance);
        var result = await formatter.ReadRequestBodyAsync(context, Encoding.UTF8);
        Assert.True(result.HasError);
        Assert.Null(result.Model);
        Assert.False(modelState.IsValid);
        var response = Assert.IsType<JsonResult>(AgentApiValidationResultFilter.InvalidModelState(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), modelState)));
        Assert.Equal(400, response.StatusCode);
        var error = Assert.IsType<ServiceResult<AgentApiErrorData>>(response.Value);
        Assert.False(error.Success);
        Assert.Equal("REQUEST_INVALID", error.Data.ErrorCode);
        Assert.Null(error.MessageDev);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Explicit_json_zero_is_valid_as_number_or_exact_string(bool stringValue)
    {
        using var db = Fixture(); var service = Service(db); var request = Request();
        await service.ReserveAsync(request); await service.MarkStartedAsync(request); await service.SettleAsync(request, null);
        var body = JsonSerializer.SerializeToElement(Reconcile(0)).EnumerateObject().ToDictionary(property => property.Name, property => property.Value);
        if (stringValue) body[nameof(AgentUserTokenQuotaReconcileInput.ActualTokens)] = JsonSerializer.SerializeToElement("0");
        var input = JsonSerializer.Deserialize<AgentUserTokenQuotaReconcileInput>(JsonSerializer.Serialize(body)) ?? throw new JsonException();
        Assert.Equal(0, input.ActualTokens);
        var receipt = await service.ReconcileAsync(Group, Company, Operator, request.Id, input);
        Assert.Equal(0, receipt.ActualTokens);
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), row =>
        { Assert.Equal(0, row.UsedTokens); Assert.Equal(0, row.ReservedTokens); Assert.False(row.HasUnknownUsage); });
        Assert.Equal(1, await db.Db.Queryable<AgUserTokenQuotaAdjustment>().CountAsync());
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(35L)]
    public async Task Unknown_reconciliation_charges_real_usage_once_and_allows_safe_late_cleanup(long actual)
    {
        using var db = Fixture(); var service = Service(db); var request = Request();
        await service.ReserveAsync(request); await service.MarkStartedAsync(request); await service.SettleAsync(request, null);
        var input = Reconcile(actual);
        var first = await service.ReconcileAsync(Group, Company, Operator, request.Id, input);
        Assert.Equal(first, await service.ReconcileAsync(Group, Company, Operator, request.Id, input));
        await service.SettleAsync(request, null); await service.SettleAsync(request, actual);
        foreach (var period in await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync())
        { Assert.Equal(actual, period.UsedTokens); Assert.Equal(0, period.ReservedTokens); Assert.False(period.HasUnknownUsage); }
        Assert.Equal(5, (await db.Db.Queryable<AgUserTokenQuotaReservation>().SingleAsync()).State);
        Assert.Empty(await service.GetPendingAsync(Group, Company));
        Assert.Equal(1, await db.Db.Queryable<AgUserTokenQuotaAdjustment>().CountAsync());
        await Assert.ThrowsAsync<AgentRuntimeException>(() => service.SettleAsync(request, actual + 1));
        await Assert.ThrowsAsync<AgentRuntimeException>(() => service.ReconcileAsync(Group, Company, Operator, request.Id, Reconcile(actual)));
    }

    [Fact]
    public async Task Last_unknown_reconciliation_unfreezes_only_original_periods()
    {
        using var db = Fixture(); var service = Service(db); var first = Request(); var second = Request();
        await service.ReserveAsync(first); await service.ReserveAsync(second);
        await service.MarkStartedAsync(first); await service.MarkStartedAsync(second);
        await service.SettleAsync(first, null); await service.SettleAsync(second, null);
        await service.ReconcileAsync(Group, Company, Operator, first.Id, Reconcile(10));
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), row => { Assert.True(row.HasUnknownUsage); Assert.Equal(50, row.ReservedTokens); Assert.Equal(10, row.UsedTokens); });
        await service.ReconcileAsync(Group, Company, Operator, second.Id, Reconcile(20));
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), row => { Assert.False(row.HasUnknownUsage); Assert.Equal(0, row.ReservedTokens); Assert.Equal(30, row.UsedTokens); Assert.Contains("202610", row.PeriodKey); });
    }

    [Theory]
    [InlineData(0, "Running")]
    [InlineData(1, "Running")]
    [InlineData(1, "WaitingForApproval")]
    [InlineData(1, "Completed")]
    public async Task Active_paused_and_recent_terminal_runs_cannot_be_reconciled(int state, string status)
    {
        using var db = Fixture(); var service = Service(db); var request = Request();
        await service.ReserveAsync(request); if (state == 1) await service.MarkStartedAsync(request);
        await db.Db.Insertable(new AgAgentRunAudit { ID = request.RunId, Status = status, FinishedAtUtc = DateTime.UtcNow }).ExecuteCommandAsync();
        var error = await Assert.ThrowsAsync<AgentRuntimeException>(() => service.ReconcileAsync(Group, Company, Operator, request.Id, Reconcile(0)));
        Assert.Equal(AgentUserTokenQuotaManagementErrors.Conflict, error.ErrorCode);
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), row => Assert.Equal(50, row.ReservedTokens));
    }

    [Theory]
    [InlineData(0, 0L)]
    [InlineData(1, 25L)]
    public async Task Old_persisted_terminal_audit_allows_orphan_reconciliation(int state, long actual)
    {
        using var db = Fixture(); var service = Service(db); var request = Request();
        await service.ReserveAsync(request); if (state == 1) await service.MarkStartedAsync(request);
        await db.Db.Insertable(new AgAgentRunAudit { ID = request.RunId, Status = "Failed", FinishedAtUtc = DateTime.UtcNow.AddMinutes(-10) }).ExecuteCommandAsync();
        await service.ReconcileAsync(Group, Company, Operator, request.Id, Reconcile(actual));
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), row => { Assert.Equal(actual, row.UsedTokens); Assert.Equal(0, row.ReservedTokens); });
    }

    [Theory]
    [InlineData("group")]
    [InlineData("company")]
    public async Task Pending_and_reconciliation_never_cross_owner_boundaries(string scope)
    {
        using var db = Fixture(); var service = Service(db); var request = Request();
        await service.ReserveAsync(request); await service.MarkStartedAsync(request); await service.SettleAsync(request, null);
        var group = scope == "group" ? Guid.NewGuid() : Group; var company = scope == "company" ? Guid.NewGuid() : Company;
        Assert.Empty(await service.GetPendingAsync(group, company));
        var error = await Assert.ThrowsAsync<AgentRuntimeException>(() => service.ReconcileAsync(group, company, Operator, request.Id, Reconcile(0)));
        Assert.Equal(AgentUserTokenQuotaManagementErrors.NotFound, error.ErrorCode);
        Assert.True((await service.GetPendingAsync(Group, Company)).Single().State == 3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Audit_insertion_failure_rolls_back_every_mutation(bool reconcile)
    {
        using var db = Fixture(); var service = Service(db); var request = Request();
        if (reconcile) { await service.ReserveAsync(request); await service.MarkStartedAsync(request); await service.SettleAsync(request, null); }
        await db.Db.Ado.ExecuteCommandAsync("CREATE TRIGGER fail_adjustment BEFORE INSERT ON AgUserTokenQuotaAdjustment BEGIN SELECT RAISE(ABORT, 'offline failure'); END;");
        if (reconcile)
        {
            await Assert.ThrowsAnyAsync<Exception>(() => service.ReconcileAsync(Group, Company, Operator, request.Id, Reconcile(10)));
            Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), row => { Assert.Equal(0, row.UsedTokens); Assert.Equal(50, row.ReservedTokens); Assert.True(row.HasUnknownUsage); });
            Assert.Equal(3, (await service.GetPendingAsync(Group, Company)).Single().State);
        }
        else
        {
            await Assert.ThrowsAnyAsync<Exception>(() => service.SavePolicyAsync(Group, Company, Operator, Policy()));
            Assert.Equal(0, (await service.GetPolicyAsync(Group, Company)).Revision);
        }
        Assert.Equal(0, await db.Db.Queryable<AgUserTokenQuotaAdjustment>().CountAsync());
    }

    [Fact]
    public async Task Cancelled_management_commands_do_not_write()
    {
        using var db = Fixture(); var service = Service(db); using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.SavePolicyAsync(Group, Company, Operator, Policy(), cts.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ReconcileAsync(Group, Company, Operator, Guid.NewGuid(), Reconcile(0), cts.Token));
        Assert.Equal(0, await db.Db.Queryable<AgUserTokenQuotaAdjustment>().CountAsync());
    }

    [Theory]
    [InlineData(-1L, 20L, "reason")]
    [InlineData(10L, 20L, "reason")]
    [InlineData(100L, 20L, "")]
    [InlineData(100L, 20L, "bad\nreason")]
    public async Task Invalid_policy_input_cannot_create_ledger(long daily, long reserved, string reason)
    {
        using var db = Fixture(); var service = Service(db);
        var error = await Assert.ThrowsAsync<AgentRuntimeException>(() => service.SavePolicyAsync(Group, Company, Operator,
            new() { OperationId = Guid.NewGuid(), DailyTotalTokens = daily, RequestReservationTokens = reserved, Reason = reason }));
        Assert.Equal(AgentUserTokenQuotaManagementErrors.Invalid, error.ErrorCode);
        Assert.Equal(0, await db.Db.Queryable<AgUserTokenQuotaPolicy>().CountAsync());
    }

    [Theory]
    [InlineData(true, true, 1, true)]
    [InlineData(false, true, 1, false)]
    [InlineData(true, false, 1, false)]
    [InlineData(true, true, 2, false)]
    public async Task Quota_management_requires_authenticated_explicit_same_group_administrator(bool enabled, bool authenticated, long group, bool expected)
    {
        var user = DispatchProxy.Create<IUser, UserProxy>(); ((UserProxy)user).GroupValue = GroupFor(group);
        var options = Options.Create(new AgentUserTokenQuotaOptions { ManagementEnabled = enabled, Administrators = [new() { GroupId = Group, CompanyId = Company, UserId = Operator }] });
        var requirement = new AgentQuotaManagementRequirement();
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(new ClaimsIdentity([], authenticated ? "offline" : null)), null);
        await new AgentQuotaManagementAuthorizationHandler(user, options).HandleAsync(context);
        Assert.Equal(expected, context.HasSucceeded);
        Assert.Equal(AgentAuthorizationPolicies.QuotaManage, typeof(AgentQuotaManagementController).GetCustomAttribute<AuthorizeAttribute>()?.Policy);
        Assert.Null(typeof(AgentQuotaManagementController).GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void Quota_metrics_have_only_bounded_labels_and_invalid_enum_is_ignored()
    {
        using var metrics = new AgentMetrics();
        metrics.RecordUserQuota(AgentUserQuotaSignal.Frozen); metrics.RecordUserQuota((AgentUserQuotaSignal)999);
        string rendered = metrics.RenderPrometheus();
        Assert.Contains("agent_user_token_quota_signals_total{signal=\"Frozen\"} 1", rendered);
        Assert.DoesNotContain("999", rendered); Assert.DoesNotContain(User.ToString(), rendered);
    }

    [Fact]
    public async Task Broken_metric_listener_cannot_turn_committed_unknown_settlement_into_failure()
    {
        using var db = Fixture(); using var metrics = new AgentMetrics();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meter) => { if (instrument.Name == "agent.user_token_quota.signals") meter.EnableMeasurementEvents(instrument); };
        listener.SetMeasurementEventCallback<long>((_, _, _, _) => throw new InvalidOperationException("offline listener failure"));
        listener.Start();
        using var host = new ServiceCollection().AddScoped<IAgUserTokenQuotaServices>(_ => Service(db)).BuildServiceProvider();
        var provider = new AgUserTokenQuotaProvider(host.GetRequiredService<IServiceScopeFactory>(), Options.Create(new AgentUserTokenQuotaOptions { DailyTotalTokens = 100, RequestReservationTokens = 50 }),
            new Clock(), NullLogger<AgUserTokenQuotaProvider>.Instance, metrics);
        var context = new AgentRunContext(Guid.NewGuid(), Guid.NewGuid(), new AgentVersionSnapshot(Guid.NewGuid(), "offline", "instructions", "offline-profile", AgentOutputMode.Text, null, [], []),
            "hello", "hash", Now, []) { ExecutionIdentity = Identity() };
        var lease = await provider.ReserveAsync(context) ?? throw new InvalidOperationException();
        await lease.MarkStartedAsync(); await lease.CompleteAsync(null); await lease.DisposeAsync();
        Assert.Equal(3, (await db.Db.Queryable<AgUserTokenQuotaReservation>().SingleAsync()).State);
        Assert.True((await db.Db.Queryable<AgUserTokenQuotaPeriod>().SingleAsync()).HasUnknownUsage);
    }

    [Fact]
    public async Task Soft_deleted_unknown_reservation_does_not_release_another_unknown_freeze()
    {
        using var db = Fixture(); var service = Service(db); var first = Request(); var second = Request();
        await service.ReserveAsync(first); await service.ReserveAsync(second);
        await service.MarkStartedAsync(first); await service.MarkStartedAsync(second);
        await service.SettleAsync(first, null); await service.SettleAsync(second, null);
        await db.Db.Updateable<AgUserTokenQuotaReservation>().SetColumns(x => x.IsDeleted == true).Where(x => x.ID == second.Id).ExecuteCommandAsync();
        await service.ReconcileAsync(Group, Company, Operator, first.Id, Reconcile(10));
        Assert.All(await db.Db.Queryable<AgUserTokenQuotaPeriod>().ToListAsync(), row => Assert.True(row.HasUnknownUsage));
    }

    [Fact]
    public async Task Company_specific_policy_does_not_leak_to_same_user_in_other_company()
    {
        using var db = Fixture(); var service = Service(db);
        await service.SavePolicyAsync(Group, Company, Operator, Policy());
        Assert.True((await service.GetPolicyAsync(Group, Guid.NewGuid())).UseDefaults);
        Assert.False((await service.GetPolicyAsync(Group, Company)).UseDefaults);
    }

    [Theory]
    [InlineData("other-company")]
    [InlineData("missing-group")]
    [InlineData("empty-company")]
    public async Task Administrator_requires_both_business_scope_components(string scope)
    {
        var user = DispatchProxy.Create<IUser, UserProxy>();
        ((UserProxy)user).GroupValue = scope == "missing-group" ? null : Group;
        ((UserProxy)user).CompanyValue = scope == "empty-company" ? Guid.Empty : scope == "other-company" ? Guid.NewGuid() : Company;
        var options = Options.Create(new AgentUserTokenQuotaOptions { ManagementEnabled = true, Administrators = [new() { GroupId = Group, CompanyId = Company, UserId = Operator }] });
        var context = new AuthorizationHandlerContext([new AgentQuotaManagementRequirement()], new ClaimsPrincipal(new ClaimsIdentity([], "offline")), null);
        await new AgentQuotaManagementAuthorizationHandler(user, options).HandleAsync(context);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public void Administrator_configuration_requires_complete_distinct_business_scope()
    {
        var admin = new AgentUserTokenQuotaAdministrator { GroupId = Group, CompanyId = Company, UserId = Operator };
        var validator = new AgentUserTokenQuotaOptionsValidator();
        Assert.True(validator.Validate(null, new() { Administrators = [admin, admin] }).Failed);
        Assert.True(validator.Validate(null, new() { Administrators = [new() { GroupId = Group, UserId = Operator }] }).Failed);
        Assert.True(validator.Validate(null, new() { Administrators = [admin, new() { GroupId = Group, CompanyId = Guid.NewGuid(), UserId = Operator }] }).Succeeded);
    }

    private static AgentPersistenceSqliteFixture Fixture() => new(typeof(AgUserTokenQuotaPeriod), typeof(AgUserTokenQuotaReservation), typeof(AgUserTokenQuotaPolicy), typeof(AgUserTokenQuotaAdjustment), typeof(AgAgentRunAudit));
    private static AgUserTokenQuotaServices Service(AgentPersistenceSqliteFixture db) => new(db.CreateRepository<AgUserTokenQuotaPeriod>());
    private static AgentExecutionIdentity Identity(long group = 1, Guid? user = null) => new((user ?? User).ToString("D"), "1", [], "offline") { GroupId = GroupFor(group), CompanyId = Company };
    private static AgentUserTokenQuotaRequest Request()
    {
        var query = AgentUserTokenQuotaRequests.CreateQuery(Identity(), Now, TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai"), 100, 1000, 50);
        return new(Guid.NewGuid(), Guid.NewGuid(), Group, Company, User, 50, query.Windows);
    }
    private static AgentUserTokenQuotaPolicyInput Policy(Guid? id = null, long daily = 100) => new() { OperationId = id ?? Guid.NewGuid(), DailyTotalTokens = daily, RequestReservationTokens = 50, Reason = "核实配置" };
    private static AgentUserTokenQuotaReconcileInput Reconcile(long actual) => new() { OperationId = Guid.NewGuid(), ActualTokens = actual, Confirmed = true, Reason = "核实用量", EvidenceReference = "offline-provider-record" };
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => Now; }
    public class UserProxy : DispatchProxy
    {
        public Guid? GroupValue = Group;
        public Guid? CompanyValue = Company;
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method?.Name switch
        { "get_ID" => (Guid?)Operator, "get_GroupId" => GroupValue, "get_CompanyId" => CompanyValue, _ => throw new InvalidOperationException("No external identity calls allowed.") };
    }
}
