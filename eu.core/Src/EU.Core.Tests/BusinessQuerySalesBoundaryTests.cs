using System.Text.Json;
using System.Text.Json.Serialization;
using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;
using EU.Core.Api.MCP.Services.BusinessQuery.Compilation;
using EU.Core.Api.MCP.Services.BusinessQuery.Contracts;
using EU.Core.Api.MCP.Services.BusinessQuery.Policy;
using EU.Core.Api.MCP.Services.BusinessQuery.Security;
using EU.Core.Api.MCP.Services.BusinessQuery.Tooling;
using Xunit;

namespace EU.Core.Tests;

/// <summary>销售主表接入所需的离线边界测试；不启动宿主，不连接数据库、Redis 或审计存储。</summary>
public class BusinessQuerySalesBoundaryTests
{
    private const string Permission = "business.sales.read";
    private static readonly BusinessQueryEvaluationTime EvaluationTime = new(DateTimeOffset.Parse("2026-09-07T00:00:00Z"), "Etc/UTC", null, null);

    [Theory]
    [InlineData("salesOrder.companyId")]
    [InlineData("salesOrder.groupId")]
    [InlineData("")]
    public void Deployment_tenant_never_becomes_company_or_group_scope(string field)
    {
        Assert.Empty(BusinessQueryTrustedScopes.FromTenant(field, "test-tenant"));
    }

    [Fact]
    public void Explicit_tenant_scope_remains_supported()
    {
        Assert.Equal("test-tenant", Assert.Single(BusinessQueryTrustedScopes.FromTenant("order.tenantId", "test-tenant")["order.tenantId"]));
    }

    [Theory]
    [InlineData(BusinessCatalogDialect.SqlServer)]
    [InlineData(BusinessCatalogDialect.MySql)]
    [InlineData(BusinessCatalogDialect.Sqlite)]
    public async Task Required_status_filters_are_parameterized_without_model_filters(BusinessCatalogDialect dialect)
    {
        BusinessCatalogSnapshot catalog = Load(CreateCatalog(dialect));
        BusinessQueryPlan plan = Plan();
        BusinessQueryPolicyDecision decision = await Authorize(catalog, plan);
        Assert.True(decision.Allowed, decision.ErrorCode);
        CompiledBusinessQuery query = new BusinessSqlCompiler().Compile(catalog, plan, decision, EvaluationTime);
        Assert.Contains("(SELECT * FROM", query.CommandText);
        Assert.Contains("IsActive", query.CommandText);
        Assert.Contains("IsDeleted", query.CommandText);
        Assert.Contains(query.Parameters, value => value.DataType == BusinessCatalogDataType.Boolean && Equals(value.Value, true));
        Assert.Contains(query.Parameters, value => value.DataType == BusinessCatalogDataType.Boolean && Equals(value.Value, false));
        Assert.DoesNotContain("SdOrderDetail", query.CommandText);
        Assert.Contains("TaxIncludedAmount", query.CommandText);
        Assert.Contains("HAVING COUNT(*) >=", query.CommandText);
        Assert.Contains(query.Parameters, value => value.DataType == BusinessCatalogDataType.Integer && Equals(value.Value, decision.MinimumGroupSize));
    }

    [Fact]
    public async Task Aggregation_without_currency_dimension_is_denied_before_quota()
    {
        var quota = new MemoryQuota();
        BusinessQueryPolicyDecision decision = await Authorize(Load(CreateCatalog()), Plan() with { Dimensions = [] }, quota);
        Assert.False(decision.Allowed);
        Assert.Equal(0, quota.Reservations);
    }

    [Fact]
    public async Task Currency_filter_does_not_replace_required_output_dimension()
    {
        BusinessQueryPlan plan = Plan() with
        {
            Dimensions = [],
            Filters = [new("salesOrder.currency", BusinessFilterOperator.Equal, JsonSerializer.SerializeToElement("test-currency"))]
        };
        Assert.False((await Authorize(Load(CreateCatalog()), plan)).Allowed);
    }

    [Fact]
    public async Task Missing_business_permission_is_denied()
    {
        BusinessCatalogSnapshot catalog = Load(CreateCatalog());
        var policy = Policy(new MemoryQuota());
        BusinessCallerContext caller = new("test-user", "test-tenant", [], ["test-source"], new Dictionary<string, IReadOnlyList<string>>());
        Assert.False((await policy.AuthorizeAsync(caller, catalog, Plan(), EvaluationTime, default)).Allowed);
    }

    [Fact]
    public void Invalid_physical_filter_column_is_rejected()
    {
        BusinessSemanticCatalog catalog = CreateCatalog();
        catalog = catalog with { Entities = [catalog.Entities[0] with { RequiredBooleanFilters = new Dictionary<string, bool> { ["IsDeleted; DROP TABLE SdOrder"] = false } }] };
        Assert.False(Parse(catalog).Succeeded);
    }

    [Fact]
    public void Explicit_null_rules_are_rejected_instead_of_disabling_protection()
    {
        BusinessSemanticCatalog catalog = CreateCatalog();
        Assert.False(Parse(catalog with { Entities = [catalog.Entities[0] with { RequiredBooleanFilters = null }] }).Succeeded);
        Assert.False(Parse(catalog with { Entities = [catalog.Entities[0] with { RequiredMeasureDimensions = null }] }).Succeeded);
    }

    [Fact]
    public void Rules_are_frozen_and_included_in_catalog_hash()
    {
        BusinessSemanticCatalog source = CreateCatalog();
        BusinessCatalogSnapshot snapshot = Load(source);
        ((Dictionary<string, bool>)source.Entities[0].RequiredBooleanFilters)["IsDeleted"] = true;
        Assert.False(snapshot.Entities["salesOrder"].RequiredBooleanFilters["IsDeleted"]);
        Assert.NotEqual(snapshot.Sha256, Load(source).Sha256);
    }

    [Fact]
    public async Task Conflicting_model_filter_cannot_remove_mandatory_filter()
    {
        BusinessSemanticCatalog source = CreateCatalog();
        BusinessCatalogEntity entity = source.Entities[0];
        BusinessCatalogField state = entity.Fields[0] with
        {
            Name = "salesOrder.deleted", PhysicalColumn = "IsDeleted", DataType = BusinessCatalogDataType.Boolean,
            Unit = "boolean"
        };
        BusinessCatalogSnapshot catalog = Load(source with { Entities = [entity with { Fields = [.. entity.Fields, state] }] });
        BusinessQueryPlan plan = Plan() with { Filters = [new(state.Name, BusinessFilterOperator.Equal, JsonSerializer.SerializeToElement(true))] };
        BusinessQueryPolicyDecision decision = await Authorize(catalog, plan);
        Assert.True(decision.Allowed, decision.ErrorCode);
        var query = new BusinessSqlCompiler().Compile(catalog, plan, decision, EvaluationTime);
        Assert.Contains(query.Parameters, parameter => parameter.DataType == BusinessCatalogDataType.Boolean && Equals(parameter.Value, false));
        Assert.Equal(2, query.CommandText.Split("IsDeleted").Length - 1);
    }

    [Fact]
    public async Task Joined_entity_is_filtered_before_left_join()
    {
        BusinessSemanticCatalog source = CreateCatalog();
        BusinessCatalogField id = source.Entities[0].Fields[0] with { Name = "currency.id", PhysicalColumn = "ID" };
        BusinessCatalogEntity currency = new("currency", "BdCurrency", "币别", Permission, false, [id.Name], "",
            [id, id with { Name = "currency.name", PhysicalColumn = "CurrencyName" }])
        {
            RequiredBooleanFilters = new Dictionary<string, bool> { ["IsDeleted"] = false }
        };
        BusinessCatalogSnapshot catalog = Load(source with
        {
            Entities = [.. source.Entities, currency],
            Relationships = [new("orderCurrency", "salesOrder", "currency", "salesOrder.currency", "currency.id",
                BusinessRelationshipCardinality.ManyToOne, BusinessFanOutPolicy.NotApplicable)]
        });
        BusinessQueryPlan plan = Plan() with { Dimensions = ["salesOrder.currency", "currency.name"] };
        BusinessQueryPolicyDecision decision = await Authorize(catalog, plan);
        Assert.True(decision.Allowed, decision.ErrorCode);
        var query = new BusinessSqlCompiler().Compile(catalog, plan, decision, EvaluationTime);
        Assert.Contains("LEFT JOIN (SELECT * FROM [BdCurrency] WHERE [IsDeleted] = @", query.CommandText);
    }

    [Fact]
    public void Unknown_required_dimension_is_rejected()
    {
        BusinessSemanticCatalog catalog = CreateCatalog();
        catalog = catalog with { Entities = [catalog.Entities[0] with { RequiredMeasureDimensions = ["salesOrder.unknown"] }] };
        Assert.False(Parse(catalog).Succeeded);
    }

    [Fact]
    public async Task Omitted_optional_rules_keep_legacy_catalog_loadable()
    {
        BusinessSemanticCatalog catalog = CreateCatalog();
        catalog = catalog with { Entities = [catalog.Entities[0] with { RequiredBooleanFilters = new Dictionary<string, bool>(), RequiredMeasureDimensions = [] }] };
        string json = Serialize(catalog).Replace(",\"requiredBooleanFilters\":{}", "").Replace(",\"requiredMeasureDimensions\":[]", "");
        BusinessCatalogLoadResult loaded = new BusinessSemanticCatalogLoader().Load(json);
        Assert.True(loaded.Succeeded, loaded.Error?.Message);
        BusinessQueryPlan plan = Plan() with { Dimensions = [] };
        Assert.True((await Authorize(loaded.Snapshot!, plan)).Allowed);
    }

    [Fact]
    public void Tool_guidance_exposes_grouping_requirement_but_not_physical_filters()
    {
        var tool = new BusinessQueryToolSchemaBuilder().Build(Load(CreateCatalog()));
        Assert.Contains("dimensions must include salesOrder.currency", tool.Description);
        Assert.DoesNotContain("IsDeleted", tool.InputSchemaJson);
        Assert.DoesNotContain("IsActive", tool.InputSchemaJson);
    }

    private static BusinessSemanticCatalog CreateCatalog(BusinessCatalogDialect dialect = BusinessCatalogDialect.SqlServer)
    {
        BusinessCatalogField dimension = new("salesOrder.id", "ID", BusinessCatalogFieldKind.Dimension, BusinessCatalogDataType.String,
            "订单主键", Permission, [BusinessFilterOperator.Equal], [], BusinessCatalogSensitivity.Internal,
            BusinessMeasureAdditivity.NotApplicable, "identifier", "", null, null, BusinessNullHandling.Preserve);
        BusinessCatalogEntity entity = new("salesOrder", "SdOrder", "销售订单主表金额", Permission, false, [dimension.Name], "",
            [dimension, dimension with { Name = "salesOrder.currency", PhysicalColumn = "CurrencyId" },
                new("salesOrder.grossAmount", "TaxIncludedAmount", BusinessCatalogFieldKind.Measure, BusinessCatalogDataType.Decimal,
                    "主表含税金额，按币别分别统计", Permission, [], [BusinessAggregation.Sum], BusinessCatalogSensitivity.Internal,
                    BusinessMeasureAdditivity.Additive, "amount", "", 20, 2, BusinessNullHandling.Preserve)])
        {
            RequiredBooleanFilters = new Dictionary<string, bool> { ["IsActive"] = true, ["IsDeleted"] = false },
            RequiredMeasureDimensions = ["salesOrder.currency"]
        };
        return new("test-sales", 1, "test-source", dialect, "Etc/UTC", "zh-CN", "1.0.0", true, [entity], []);
    }

    private static BusinessQueryPlan Plan() => new("salesOrder", ["salesOrder.currency"],
        [new("salesOrder.grossAmount", BusinessAggregation.Sum, "totalAmount")], [], null, [], 10);

    private static string Serialize(BusinessSemanticCatalog catalog) => JsonSerializer.Serialize(catalog, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    });

    private static BusinessCatalogLoadResult Parse(BusinessSemanticCatalog catalog) => new BusinessSemanticCatalogLoader().Load(Serialize(catalog));

    private static BusinessCatalogSnapshot Load(BusinessSemanticCatalog catalog)
    {
        BusinessCatalogLoadResult loaded = Parse(catalog);
        Assert.True(loaded.Succeeded, loaded.Error?.Message);
        return loaded.Snapshot!;
    }

    private static BusinessQueryPolicy Policy(MemoryQuota quota) => new(new BusinessQueryPolicyOptions
    {
        TenantId = "test-tenant", DataSourceCode = "test-source"
    }, quota);

    private static Task<BusinessQueryPolicyDecision> Authorize(BusinessCatalogSnapshot catalog, BusinessQueryPlan plan, MemoryQuota quota = null) =>
        Policy(quota ?? new MemoryQuota()).AuthorizeAsync(new BusinessCallerContext("test-user", "test-tenant", [Permission],
            ["test-source"], new Dictionary<string, IReadOnlyList<string>>()), catalog, plan, EvaluationTime, default);

    private sealed class MemoryQuota : IBusinessQueryQuotaStore
    {
        public int Reservations { get; private set; }
        public Task<BusinessQueryQuotaReservationResult> TryReserveAsync(BusinessQueryQuotaRequest request, CancellationToken cancellationToken)
        {
            Reservations++;
            return Task.FromResult(BusinessQueryQuotaReservationResult.Allow(Guid.NewGuid()));
        }
        public Task SettleAsync(Guid reservationId, BusinessQueryQuotaOutcome outcome, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
