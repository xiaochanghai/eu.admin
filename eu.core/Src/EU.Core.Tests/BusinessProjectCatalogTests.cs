using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;
using EU.Core.Api.MCP.Services.BusinessQuery.Contracts;
using EU.Core.Api.MCP.Services.BusinessQuery.Compilation;
using EU.Core.Api.MCP.Services.BusinessQuery.Policy;
using EU.Core.Api.MCP.Services.BusinessQuery.Tooling;
using EU.Core.Api.MCP.Services.BusinessQuery.Security;
using Xunit;
using Xunit.Abstractions;

namespace EU.Core.Tests;

public sealed class BusinessProjectCatalogTests(ITestOutputHelper output)
{
    [Fact]
    public void Host_configuration_pins_the_same_unified_catalog_and_tool()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EU.Core.sln")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        var options = new System.Text.Json.JsonDocumentOptions { CommentHandling = System.Text.Json.JsonCommentHandling.Skip };
        using var mcpDocument = System.Text.Json.JsonDocument.Parse(File.ReadAllText(
            Path.Combine(directory.FullName, "EU.Core.MCP.Api", "appsettings.json")), options);
        using var agentDocument = System.Text.Json.JsonDocument.Parse(File.ReadAllText(
            Path.Combine(directory.FullName, "EU.Core.Api.Agent", "appsettings.json")), options);
        var mcp = mcpDocument.RootElement.GetProperty("BusinessQuery");
        var agent = agentDocument.RootElement.GetProperty("BusinessQueryForwarding");
        var catalog = Load("sqlserver");
        var tool = new BusinessQueryToolSchemaBuilder().Build(catalog);
        Assert.Equal("BusinessQuery/catalog/project.sqlserver.json", mcp.GetProperty("CatalogPath").GetString());
        // SmUsersServices.GenerateJwtToken currently issues TenantId=0 for project logins.
        Assert.Equal("0", mcp.GetProperty("TenantId").GetString());
        Assert.Equal(catalog.Sha256, mcp.GetProperty("ExpectedCatalogHash").GetString());
        Assert.Equal(catalog.Sha256, agent.GetProperty("CatalogHash").GetString());
        Assert.Equal(catalog.Revision, agent.GetProperty("CatalogRevision").GetInt64());
        Assert.Equal(tool.ToolVersionHash, agent.GetProperty("ToolSchemaHash").GetString());
    }

    [Theory]
    [InlineData("sqlserver")]
    [InlineData("mysql")]
    public void Unified_catalog_exposes_both_entities_in_one_tool(string dialect)
    {
        var catalog = Load(dialect);
        Assert.Equal(2, catalog.Entities.Count);
        Assert.Contains("supplier", catalog.Entities.Keys);
        Assert.Contains("salesOrder", catalog.Entities.Keys);
        Assert.Empty(catalog.Relationships);
        var tool = new BusinessQueryToolSchemaBuilder().Build(catalog);
        Assert.Equal("query_business_data", tool.Name);
        Assert.Contains("supplier.taxRate", tool.InputSchemaJson);
        Assert.Contains("salesOrder.grossAmount", tool.InputSchemaJson);
        foreach (var entity in catalog.Entities.Values)
        {
            Assert.Equal("business.project.query", entity.RequiredPermission);
            Assert.All(entity.Fields.Values, field => Assert.Equal("business.project.query", field.RequiredPermission));
            Assert.True(entity.RequiredBooleanFilters["IsActive"]);
            Assert.False(entity.RequiredBooleanFilters["IsDeleted"]);
        }
        output.WriteLine($"{dialect}: CatalogHash={catalog.Sha256}; ToolSchemaHash={tool.ToolVersionHash}");
    }

    [Theory]
    [InlineData("sqlserver", "supplier")]
    [InlineData("mysql", "supplier")]
    [InlineData("sqlserver", "salesOrder")]
    [InlineData("mysql", "salesOrder")]
    public async Task Each_entity_compiles_independently_with_project_capability(string dialect, string entity)
    {
        var catalog = Load(dialect);
        var plan = Plan(entity);
        var time = new BusinessQueryEvaluationTime(DateTimeOffset.UtcNow, catalog.TimeZoneId, null, null);
        var decision = await Authorize(catalog, plan, time);
        Assert.True(decision.Allowed, decision.ErrorCode);
        var query = new BusinessSqlCompiler().Compile(catalog, plan, decision, time);
        Assert.Contains(entity == "supplier" ? "BdSupplier" : "SdOrder", query.CommandText);
        Assert.DoesNotContain(entity == "supplier" ? "SdOrder" : "BdSupplier", query.CommandText);
        Assert.Contains("IsActive", query.CommandText);
        Assert.Contains("IsDeleted", query.CommandText);
        Assert.Empty(decision.DataScope.Constraints);
        Assert.DoesNotContain("HAVING", query.CommandText);
        Assert.DoesNotContain("COUNT(", query.CommandText);
        Assert.DoesNotContain(query.Parameters, value => value.DataType == BusinessCatalogDataType.Integer && Equals(value.Value, decision.MinimumGroupSize));
        if (entity == "salesOrder")
        {
            Assert.Contains("CurrencyId", query.CommandText);
            Assert.Contains("SdOrderDetail", query.CommandText);
            Assert.Contains("LEFT JOIN", query.CommandText);
            Assert.Contains("SUM(COALESCE(", query.CommandText);
            Assert.Contains("detailTotals", query.CommandText);
        }
        else Assert.DoesNotContain("SdOrderDetail", query.CommandText);
    }

    [Theory]
    [InlineData("mixed-field")]
    [InlineData("mixed-measure")]
    [InlineData("missing-currency")]
    [InlineData("sum-tax-rate")]
    [InlineData("unconfigured-purchase")]
    public async Task Unified_catalog_does_not_allow_invalid_cross_entity_queries(string kind)
    {
        var catalog = Load("sqlserver");
        var plan = kind switch
        {
            "mixed-field" => Plan("salesOrder") with { Dimensions = ["salesOrder.currencyId", "supplier.taxType"] },
            "mixed-measure" => Plan("salesOrder") with { Measures = [new("supplier.taxRate", BusinessAggregation.Average, "rate")] },
            "missing-currency" => Plan("salesOrder") with { Dimensions = [] },
            "sum-tax-rate" => Plan("supplier") with { Measures = [new("supplier.taxRate", BusinessAggregation.Sum, "rate")] },
            _ => Plan("supplier") with { Entity = "purchaseOrder" }
        };
        var decision = await Authorize(catalog, plan,
            new BusinessQueryEvaluationTime(DateTimeOffset.UtcNow, catalog.TimeZoneId, null, null));
        Assert.False(decision.Allowed);
    }

    [Fact]
    public async Task Detail_totals_do_not_duplicate_orders_and_zero_missing_or_null_amounts()
    {
        // 隔离内存数据库：执行实际编译 SQL，不连接项目数据库。
        string json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ProjectCatalogs", "project.sqlserver.json"));
        var node = System.Text.Json.Nodes.JsonNode.Parse(json)!;
        node["dialect"] = "sqlite";
        var catalog = new BusinessSemanticCatalogLoader().Load(node.ToJsonString()).Snapshot!;
        var plan = Plan("salesOrder") with { Dimensions = ["salesOrder.customerId", "salesOrder.currencyId"],
            Measures = [new("salesOrder.netAmount", BusinessAggregation.Sum, "net"),
                new("salesOrder.taxAmount", BusinessAggregation.Sum, "tax"), new("salesOrder.grossAmount", BusinessAggregation.Sum, "gross")] };
        var time = new BusinessQueryEvaluationTime(DateTimeOffset.UtcNow, catalog.TimeZoneId, null, null);
        var query = new BusinessSqlCompiler().Compile(catalog, plan, await Authorize(catalog, plan, time), time);
        await using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var seed = connection.CreateCommand();
        seed.CommandText = """
            CREATE TABLE SdOrder(ID TEXT, OrderNo TEXT, CustomerId TEXT, CurrencyId TEXT, CompanyId TEXT,
                SalesOrderStatus TEXT, AuditStatus TEXT, IsActive INTEGER, IsDeleted INTEGER,
                NoTaxAmount NUMERIC, TaxAmount NUMERIC, TaxIncludedAmount NUMERIC);
            CREATE TABLE SdOrderDetail(OrderId TEXT, IsActive INTEGER, IsDeleted INTEGER,
                NoTaxAmount NUMERIC, TaxAmount NUMERIC, TaxIncludedAmount NUMERIC);
            INSERT INTO SdOrder(ID,CustomerId,CurrencyId,IsActive,IsDeleted,TaxIncludedAmount) VALUES
                ('1','a','x',1,0,999), ('2','a','x',1,0,999), ('3','b','x',1,0,999),
                ('4','c','x',1,0,999), ('5','a','x',0,0,999), ('6','a','x',1,1,999);
            INSERT INTO SdOrderDetail VALUES
                ('1',1,0,10,1,11), ('1',1,0,20,2,22), ('2',1,0,30,3,33),
                ('1',0,0,100,100,100), ('1',1,1,100,100,100), ('4',1,0,NULL,NULL,NULL),
                ('5',1,0,100,100,100), ('6',1,0,100,100,100);
            """;
        await seed.ExecuteNonQueryAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = query.CommandText;
        foreach (var parameter in query.Parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        await using var reader = await command.ExecuteReaderAsync();
        int count = 0;
        while (await reader.ReadAsync())
        {
            bool hasAmounts = reader.GetString(0) == "a";
            Assert.Equal(hasAmounts ? 60m : 0m, Convert.ToDecimal(reader.GetValue(2)));
            Assert.Equal(hasAmounts ? 6m : 0m, Convert.ToDecimal(reader.GetValue(3)));
            Assert.Equal(hasAmounts ? 66m : 0m, Convert.ToDecimal(reader.GetValue(4)));
            count++;
        }
        Assert.Equal(3, count);
    }

    private static BusinessQueryPlan Plan(string entity) => entity == "supplier"
        ? new(entity, ["supplier.taxType"], [new("supplier.taxRate", BusinessAggregation.Average, "averageRate")], [], null, [], 10)
        : new(entity, ["salesOrder.currencyId"], [new("salesOrder.grossAmount", BusinessAggregation.Sum, "grossTotal")], [], null, [], 10);

    private static Task<BusinessQueryPolicyDecision> Authorize(BusinessCatalogSnapshot catalog, BusinessQueryPlan plan, BusinessQueryEvaluationTime time) =>
        new BusinessQueryPolicy(new() { TenantId = "7", DataSourceCode = catalog.DataSourceCode }, new Quota())
            .AuthorizeAsync(new BusinessCallerContext(Guid.NewGuid().ToString(), "7", ["business.project.query"], [catalog.DataSourceCode], new Dictionary<string, IReadOnlyList<string>>()), catalog, plan, time, default);

    private static BusinessCatalogSnapshot Load(string dialect)
    {
        var result = new BusinessSemanticCatalogLoader().Load(File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "ProjectCatalogs", $"project.{dialect}.json")));
        Assert.True(result.Succeeded, result.Error?.Message);
        return result.Snapshot!;
    }

    private sealed class Quota : IBusinessQueryQuotaStore
    {
        public Task<BusinessQueryQuotaReservationResult> TryReserveAsync(BusinessQueryQuotaRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(BusinessQueryQuotaReservationResult.Allow(Guid.NewGuid()));
        public Task SettleAsync(Guid reservationId, BusinessQueryQuotaOutcome outcome, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
