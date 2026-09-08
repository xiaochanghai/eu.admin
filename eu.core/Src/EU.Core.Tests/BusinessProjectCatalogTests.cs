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
        Assert.Equal("BusinessQuery/catalog/project.json", mcp.GetProperty("CatalogPath").GetString());
        Assert.Equal("Auto", mcp.GetProperty("Dialect").GetString());
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Detail_totals_do_not_duplicate_orders_and_zero_missing_or_null_amounts(bool useOtherTables)
    {
        // 隔离内存数据库：执行实际编译 SQL，不连接项目数据库。
        string json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ProjectCatalogs", "project.json"));
        var node = System.Text.Json.Nodes.JsonNode.Parse(json)!;
        node["dialect"] = "sqlite";
        if (useOtherTables)
        {
            var entity = node["entities"]![1]!;
            entity["physicalTable"] = "PurchaseHeader";
            entity["projectModuleCode"] = "TEST_PURCHASE_MNG";
            entity["detailAggregate"]!["physicalTable"] = "PurchaseLines";
            entity["detailAggregate"]!["foreignKeyColumn"] = "HeaderId";
            entity["detailAggregate"]!["measures"]!["salesOrder.grossAmount"] = "LineTotal";
        }
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
        if (useOtherTables)
        {
            seed.CommandText = seed.CommandText.Replace("SdOrderDetail", "PurchaseLines").Replace("SdOrder", "PurchaseHeader")
                .Replace("OrderId", "HeaderId").Replace("TaxIncludedAmount", "LineTotal");
            Assert.DoesNotContain("SdOrder", query.CommandText);
            Assert.Contains("HeaderId", query.CommandText);
            Assert.Contains("LineTotal", query.CommandText);
        }
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

    [Theory]
    [InlineData("table")]
    [InlineData("foreign-key")]
    [InlineData("parent-key")]
    [InlineData("dimension")]
    [InlineData("null-measures")]
    [InlineData("empty-measures")]
    [InlineData("column-expression")]
    [InlineData("deleted-filter")]
    [InlineData("null-filters")]
    [InlineData("aggregation")]
    [InlineData("null-handling")]
    [InlineData("unknown-property")]
    public void Unsafe_or_unsupported_detail_configuration_is_rejected(string kind)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ProjectCatalogs", "project.json")))!;
        var entity = node["entities"]![1]!;
        var source = entity["detailAggregate"]!;
        switch (kind)
        {
            case "table": source["physicalTable"] = "SdOrderDetail; DROP TABLE SdOrder"; break;
            case "foreign-key": source["foreignKeyColumn"] = "OrderId OR 1=1"; break;
            case "parent-key": source["parentKeyField"] = "salesOrder.customerId"; break;
            case "dimension": source["measures"]!["salesOrder.customerId"] = "CustomerId"; break;
            case "null-measures": source["measures"] = null; break;
            case "empty-measures": source["measures"] = new System.Text.Json.Nodes.JsonObject(); break;
            case "column-expression": source["measures"]!["salesOrder.netAmount"] = "SUM(Amount)"; break;
            case "deleted-filter": source["requiredBooleanFilters"]!["IsDeleted"] = true; break;
            case "null-filters": source["requiredBooleanFilters"] = null; break;
            case "unknown-property": source["sql"] = "SELECT 1"; break;
            default:
                var measure = entity["fields"]!.AsArray().Single(field => field!["name"]!.GetValue<string>() == "salesOrder.netAmount")!;
                if (kind == "aggregation") measure["allowedAggregations"] = new System.Text.Json.Nodes.JsonArray("average");
                else measure["nullHandling"] = "preserve";
                break;
        }
        Assert.False(new BusinessSemanticCatalogLoader().Load(node.ToJsonString(), runtimeDialect: BusinessCatalogDialect.SqlServer).Succeeded);
    }

    [Fact]
    public void Detail_configuration_is_frozen_and_changes_the_catalog_hash()
    {
        var original = Load("sqlserver");
        var detail = original.Entities["salesOrder"].DetailAggregate!;
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, string>)detail.Measures)["salesOrder.netAmount"] = "OtherAmount");
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, bool>)detail.RequiredBooleanFilters)["IsDeleted"] = true);
        var node = System.Text.Json.Nodes.JsonNode.Parse(original.CanonicalJson)!;
        node["entities"]![1]!["detailAggregate"]!["measures"]!["salesOrder.netAmount"] = "OtherAmount";
        var changed = new BusinessSemanticCatalogLoader().Load(node.ToJsonString(), runtimeDialect: BusinessCatalogDialect.SqlServer);
        Assert.True(changed.Succeeded);
        Assert.NotEqual(original.Sha256, changed.Snapshot!.Sha256);
    }

    [Fact]
    public async Task Omitted_detail_configuration_keeps_direct_table_compilation_and_original_hash()
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(Load("sqlserver").CanonicalJson)!;
        node["entities"]![1]!.AsObject().Remove("detailAggregate");
        var loaded = new BusinessSemanticCatalogLoader().Load(node.ToJsonString(), runtimeDialect: BusinessCatalogDialect.SqlServer);
        Assert.True(loaded.Succeeded);
        var catalog = loaded.Snapshot!;
        Assert.NotEqual(Load("sqlserver").Sha256, catalog.Sha256);
        var plan = Plan("salesOrder");
        var time = new BusinessQueryEvaluationTime(DateTimeOffset.UtcNow, catalog.TimeZoneId, null, null);
        var query = new BusinessSqlCompiler().Compile(catalog, plan, await Authorize(catalog, plan, time), time);
        Assert.Contains("FROM [SdOrder]", query.CommandText);
        Assert.DoesNotContain("SdOrderDetail", query.CommandText);
        Assert.DoesNotContain("LEFT JOIN", query.CommandText);
    }

    [Fact]
    public void Automatic_catalog_has_one_hash_and_tool_schema_for_both_databases()
    {
        var sqlServer = Load("sqlserver");
        var mySql = Load("mysql");
        Assert.Equal(sqlServer.Sha256, mySql.Sha256);
        Assert.Equal(new BusinessQueryToolSchemaBuilder().Build(sqlServer).ToolVersionHash,
            new BusinessQueryToolSchemaBuilder().Build(mySql).ToolVersionHash);
        Assert.NotEqual(sqlServer.Dialect, mySql.Dialect);
        var loader = new BusinessSemanticCatalogLoader();
        Assert.False(loader.Load(sqlServer.CanonicalJson).Succeeded);
        Assert.False(loader.Load(sqlServer.CanonicalJson, runtimeDialect: BusinessCatalogDialect.Auto).Succeeded);
        Assert.False(loader.Load(sqlServer.CanonicalJson, new string('0', 64), BusinessCatalogDialect.SqlServer).Succeeded);
        var explicitCatalog = System.Text.Json.Nodes.JsonNode.Parse(sqlServer.CanonicalJson)!;
        explicitCatalog["dialect"] = "sqlServer";
        Assert.True(loader.Load(explicitCatalog.ToJsonString()).Succeeded);
        Assert.False(loader.Load(explicitCatalog.ToJsonString(), runtimeDialect: BusinessCatalogDialect.MySql).Succeeded);
    }

    [Theory]
    [InlineData(SqlSugar.DbType.SqlServer, "Auto", false, false, true)]
    [InlineData(SqlSugar.DbType.MySql, "Auto", false, false, true)]
    [InlineData(SqlSugar.DbType.SqlServer, "MySql", false, false, false)]
    [InlineData(SqlSugar.DbType.Oracle, "Auto", false, false, false)]
    [InlineData(SqlSugar.DbType.Sqlite, "Auto", true, true, false)]
    [InlineData(SqlSugar.DbType.Sqlite, "Sqlite", false, true, false)]
    [InlineData(SqlSugar.DbType.Sqlite, "Sqlite", true, false, false)]
    [InlineData(SqlSugar.DbType.Sqlite, "Sqlite", true, true, true)]
    public void Runtime_dialect_uses_connection_type_and_preserves_sqlite_opt_in(SqlSugar.DbType type, string configured, bool development, bool allowSqlite, bool allowed)
    {
        if (allowed)
            Assert.Equal(type.ToString(), EU.Core.Api.MCP.Services.BusinessQuery.Configuration.BusinessQueryDatabaseDialect.Resolve(type, configured, development, allowSqlite).ToString());
        else
            Assert.Throws<InvalidOperationException>(() => EU.Core.Api.MCP.Services.BusinessQuery.Configuration.BusinessQueryDatabaseDialect.Resolve(type, configured, development, allowSqlite));
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
            Path.Combine(AppContext.BaseDirectory, "ProjectCatalogs", "project.json")), runtimeDialect: dialect == "mysql" ? BusinessCatalogDialect.MySql : BusinessCatalogDialect.SqlServer);
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
