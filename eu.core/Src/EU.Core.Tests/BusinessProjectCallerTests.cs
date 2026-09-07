#nullable enable
using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;
using EU.Core.Api.MCP.Services.BusinessQuery.Security;
using EU.Core.Api.MCP.Services.BusinessQuery.Policy;
using EU.Core.Api.MCP.Services.BusinessQuery.Contracts;
using EU.Core.Api.MCP.Services.BusinessQuery.Compilation;
using SqlSugar;
using Xunit;

namespace EU.Core.Tests;

public sealed class BusinessProjectCallerTests
{
    private static readonly Guid Company = Guid.Parse("f659597e-18d1-4ff7-8993-4f57a112f718");
    private static BusinessQueryExecutionContext Context(string tenant = "7", string[]? permissions = null) =>
        new(Guid.NewGuid().ToString("D"), tenant, permissions ?? ["business.project.query"], "test-trace", Guid.NewGuid(), Guid.NewGuid(), "test-jti");

    [Theory]
    [InlineData("sqlserver")]
    [InlineData("mysql")]
    public async Task Sales_catalog_temporarily_compiles_without_company_scope(string dialect)
    {
        var catalog = Catalog(dialect);
        var reader = new FakeReader(new(true, [Company]));
        var caller = await new BusinessProjectCallerResolver(reader).ResolveAsync(Context(), catalog.Entities["salesOrder"],
            "7", catalog.DataSourceCode, null!, default);
        Assert.NotNull(caller);
        Assert.Equal(0, reader.Calls);
        Assert.Empty(caller.DataScopes);
        var plan = new BusinessQueryPlan("salesOrder", ["salesOrder.currencyId"],
            [new("salesOrder.grossAmount", BusinessAggregation.Sum, "grossTotal")], [], null, [], 10);
        var time = new BusinessQueryEvaluationTime(DateTimeOffset.UtcNow, catalog.TimeZoneId, null, null);
        var policy = new BusinessQueryPolicy(new() { TenantId = "7", DataSourceCode = catalog.DataSourceCode }, new Quota());
        var decision = await policy.AuthorizeAsync(caller, catalog, plan, time, default);
        Assert.True(decision.Allowed, decision.ErrorCode);
        Assert.Contains("scope.project-disabled", decision.AppliedRuleIds);
        Assert.DoesNotContain("scope.trusted-injection", decision.AppliedRuleIds);
        var query = new BusinessSqlCompiler().Compile(catalog, plan, decision, time);
        Assert.DoesNotContain("CompanyId", query.CommandText);
        Assert.DoesNotContain(query.Parameters, item => item.Value.ToString() == Company.ToString("D"));
        Assert.Contains("IsActive", query.CommandText);
        Assert.Contains("IsDeleted", query.CommandText);
        Assert.DoesNotContain(Company.ToString("D"), query.CommandText);
        Assert.Contains("TaxIncludedAmount", query.CommandText);
        Assert.DoesNotContain("SdOrderDetail", query.CommandText);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    [InlineData(true, 101)]
    public async Task Module_and_company_permissions_are_temporarily_not_read(bool moduleAllowed, int companies)
    {
        var reader = new FakeReader(new(moduleAllowed, Enumerable.Range(0, companies).Select(_ => Guid.NewGuid()).ToArray()));
        Assert.NotNull(await Resolve(reader, Context()));
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task Wrong_tenant_is_denied_before_database_access()
    {
        var reader = new FakeReader(new(true, [Company]));
        Assert.Null(await Resolve(reader, Context("8")));
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task Missing_capability_is_denied_before_database_access()
    {
        var reader = new FakeReader(new(true, [Company]));
        Assert.Null(await Resolve(reader, Context(permissions: [])));
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task Invalid_user_id_is_denied_before_database_access()
    {
        var reader = new FakeReader(new(true, [Company]));
        var context = new BusinessQueryExecutionContext("not-a-guid", "7", ["business.project.query"], "trace", Guid.NewGuid(), Guid.NewGuid(), "jti");
        Assert.Null(await Resolve(reader, context));
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task Disabled_permission_lookup_does_not_access_permission_store()
    {
        var reader = new FakeReader(new(true, [Company])) { Throw = true };
        var caller = await Resolve(reader, Context());
        Assert.NotNull(caller);
        Assert.Empty(caller.DataScopes);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public void Project_capability_without_module_binding_is_invalid()
    {
        string json = File.ReadAllText(CatalogPath("sqlserver")).Replace("\"projectModuleCode\": \"SD_SALES_ORDER_MNG\"", "\"projectModuleCode\": \"\"");
        Assert.False(new BusinessSemanticCatalogLoader().Load(json).Succeeded);
    }

    [Fact]
    public void Project_catalog_cannot_remove_company_scope()
    {
        string json = File.ReadAllText(CatalogPath("sqlserver")).Replace("\"defaultScopeField\": \"salesOrder.companyId\"", "\"defaultScopeField\": \"\"");
        Assert.False(new BusinessSemanticCatalogLoader().Load(json).Succeeded);
    }

    [Fact]
    public async Task Cancellation_is_propagated_before_permission_lookup()
    {
        var reader = new FakeReader(new(true, [Company]));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new BusinessProjectCallerResolver(reader)
            .ResolveAsync(Context(), Catalog("sqlserver").Entities["salesOrder"], "7", "eu-core-main", null!, cancellation.Token));
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task Another_business_module_reuses_the_same_caller_resolver()
    {
        // 虚构模块仅用于离线测试，不新增业务配置或数据库记录。
        string json = File.ReadAllText(CatalogPath("sqlserver"))
            .Replace("salesOrder", "testDocument", StringComparison.Ordinal)
            .Replace("SdOrder", "TestDocument", StringComparison.Ordinal)
            .Replace("SD_SALES_ORDER_MNG", "TEST_DOCUMENT_MNG", StringComparison.Ordinal);
        var loaded = new BusinessSemanticCatalogLoader().Load(json);
        Assert.True(loaded.Succeeded, loaded.Error?.Message);
        var reader = new FakeReader(new(true, [Company]));
        var caller = await new BusinessProjectCallerResolver(reader).ResolveAsync(Context(),
            loaded.Snapshot!.Entities["testDocument"], "7", "eu-core-main", null!, default);

        Assert.NotNull(caller);
        Assert.Equal(0, reader.Calls);
        Assert.Empty(caller.DataScopes);
        Assert.False(caller.DataScopes.ContainsKey("salesOrder.companyId"));
        var tool = new EU.Core.Api.MCP.Services.BusinessQuery.Tooling.BusinessQueryToolSchemaBuilder().Build(loaded.Snapshot);
        Assert.Equal("query_business_data", tool.Name);
    }

    private static Task<BusinessCallerContext?> Resolve(FakeReader reader, BusinessQueryExecutionContext context) =>
        new BusinessProjectCallerResolver(reader).ResolveAsync(context, Catalog("sqlserver").Entities["salesOrder"], "7", "eu-core-main", null!, default);

    private static string CatalogPath(string dialect) => Path.Combine(AppContext.BaseDirectory, "ProjectCatalogs", $"sales-order.{dialect}.json");
    private static BusinessCatalogSnapshot Catalog(string dialect)
    {
        var loaded = new BusinessSemanticCatalogLoader().Load(File.ReadAllText(CatalogPath(dialect)));
        Assert.True(loaded.Succeeded, loaded.Error?.Message);
        return loaded.Snapshot!;
    }

    private sealed class FakeReader(BusinessProjectAccess result) : IBusinessProjectAccessReader
    {
        public int Calls { get; private set; }
        public string? Module { get; private set; }
        public string? Table { get; private set; }
        public bool Throw { get; init; }
        public Task<BusinessProjectAccess> ReadAsync(ISqlSugarClient database, Guid userId, string moduleCode, string physicalTable, CancellationToken cancellationToken)
        {
            Calls++;
            Module = moduleCode;
            Table = physicalTable;
            if (Throw) throw new InvalidOperationException("test unavailable");
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(result);
        }
    }

    private sealed class Quota : IBusinessQueryQuotaStore
    {
        public Task<BusinessQueryQuotaReservationResult> TryReserveAsync(BusinessQueryQuotaRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(BusinessQueryQuotaReservationResult.Allow(Guid.NewGuid()));
        public Task SettleAsync(Guid reservationId, BusinessQueryQuotaOutcome outcome, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
