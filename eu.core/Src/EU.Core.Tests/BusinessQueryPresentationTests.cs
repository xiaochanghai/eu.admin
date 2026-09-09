using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;
using EU.Core.Api.MCP.Services.BusinessQuery.Compilation;
using EU.Core.Api.MCP.Services.BusinessQuery.Contracts;
using EU.Core.Api.MCP.Services.BusinessQuery.Presentation;
using Xunit;

namespace EU.Core.Tests;

public sealed class BusinessQueryPresentationTests
{
    [Theory]
    [InlineData("normal")]
    [InlineData("inactive")]
    [InlineData("duplicate")]
    [InlineData("cancelled")]
    [InlineData("deleted-default")]
    [InlineData("deleted-included")]
    [InlineData("deleted-inactive")]
    public async Task Configured_name_lookup_is_bounded_and_preserves_raw_result(string scenario)
    {
        const string id = "11111111-1111-1111-1111-111111111111";
        const string missingId = "22222222-2222-2222-2222-222222222222";
        const string key = "purchase.vendorId";
        var query = new CompiledBusinessQuery("", [], [new(key, key, "d0", BusinessCatalogDataType.String,
            BusinessCatalogFieldKind.Dimension, BusinessCatalogSensitivity.Internal, "", "", null, null)],
            BusinessCatalogDialect.Sqlite, "project", "purchase", "zh-CN", "1.0", 1, "catalog", "plan",
            Guid.NewGuid(), DateTimeOffset.UtcNow, "Asia/Shanghai", null, null, 100, 100, false);
        var result = new BusinessQueryResult([new(key, BusinessQueryValueKind.String, "", "")],
            new[] { id, missingId }.Select(value => new BusinessQueryRow(new Dictionary<string, BusinessQueryValue>
                { [key] = new(BusinessQueryValueKind.String, value, true) })).ToArray(), false, "unchanged");
        bool includeDeleted = scenario is "deleted-included" or "deleted-inactive";
        var lookupFilters = new Dictionary<string, bool> { ["IsActive"] = true };
        if (!includeDeleted) lookupFilters["IsDeleted"] = false;
        var display = new BusinessCatalogPresentation("采购测试", new Dictionary<string, string> { [key] = "供应商" },
            new Dictionary<string, BusinessCatalogNameLookup> { [key] = new("Names", "KeyId", "Caption",
                lookupFilters) { IncludeSoftDeleted = includeDeleted } });
        using var database = new SqlSugar.SqlSugarClient(new SqlSugar.ConnectionConfig
            { DbType = SqlSugar.DbType.Sqlite, ConnectionString = "Data Source=:memory:", IsAutoCloseConnection = false });
        await database.Ado.ExecuteCommandAsync("CREATE TABLE Names(KeyId TEXT, Caption TEXT, IsActive INTEGER, IsDeleted INTEGER)");
        await database.Ado.ExecuteCommandAsync("INSERT INTO Names VALUES (@id, @caption, @active, @deleted)",
            new SqlSugar.SugarParameter("@id", id), new SqlSugar.SugarParameter("@caption", "供应商<测试>|一"),
            new SqlSugar.SugarParameter("@active", scenario is "inactive" or "deleted-inactive" ? 0 : 1),
            new SqlSugar.SugarParameter("@deleted", scenario.StartsWith("deleted-") ? 1 : 0));
        // 数据库中存在额外 ID，但查询只允许读取结果集中的 ID。
        await database.Ado.ExecuteCommandAsync("INSERT INTO Names VALUES ('33333333-3333-3333-3333-333333333333', '不得返回', 1, 0)");
        if (scenario == "duplicate") await database.Ado.ExecuteCommandAsync("INSERT INTO Names SELECT * FROM Names WHERE KeyId=@id", new SqlSugar.SugarParameter("@id", id));
        if (scenario == "cancelled")
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ProjectBusinessQueryPresentation.CreateAsync(query, result, display, database, new CancellationToken(true)));
        else if (scenario == "duplicate")
            await Assert.ThrowsAsync<InvalidOperationException>(() => ProjectBusinessQueryPresentation.CreateAsync(query, result, display, database, default));
        else
        {
            var overrides = await ProjectBusinessQueryPresentation.CreateAsync(query, result, display, database, default);
            var formatted = new BusinessQueryPresentationFormatter().Format(query, result, overrides);
            Assert.Equal(scenario is "normal" or "deleted-included" ? "供应商<测试>|一" : id, formatted.Rows[0][key].DisplayValue);
            Assert.Equal(missingId, formatted.Rows[1][key].DisplayValue);
            Assert.True(formatted.Rows[0][key].UntrustedData);
            Assert.Equal("供应商", formatted.Columns[0].Label);
            Assert.Equal("采购测试", formatted.Title);
            Assert.DoesNotContain("不得返回", formatted.Markdown);
        }
        Assert.Equal(id, result.Rows[0].Values[key].CanonicalValue);
        Assert.Equal("unchanged", result.ResultSha256);
    }

    [Theory]
    [InlineData("totalNetAmount", true)]
    [InlineData("netAmountSum", false)]
    public void Project_display_uses_logical_labels_and_keeps_raw_values(string alias, bool found)
    {
        const string customerKey = "salesOrder.customerId";
        const string customerId = "8d5b0c2f-6e4d-4102-bed6-b7cb07bba62c";
        var query = new CompiledBusinessQuery("", [], [
            new(customerKey, customerKey, "c0", BusinessCatalogDataType.String, BusinessCatalogFieldKind.Dimension,
                BusinessCatalogSensitivity.Internal, "", "", null, null),
            new(alias, "salesOrder.netAmount", "c1", BusinessCatalogDataType.Decimal, BusinessCatalogFieldKind.Measure,
                BusinessCatalogSensitivity.Internal, "amount", "", 20, 2)
        ], BusinessCatalogDialect.SqlServer, "project", "salesOrder", "zh-CN", "1.0", 1, "catalog-hash", "plan-hash",
            Guid.NewGuid(), DateTimeOffset.UtcNow, "Asia/Shanghai", null, null, 100, 100, false);
        var result = new BusinessQueryResult([
            new(customerKey, BusinessQueryValueKind.String, "", ""),
            new(alias, BusinessQueryValueKind.Decimal, "amount", "")
        ], [new BusinessQueryRow(new Dictionary<string, BusinessQueryValue>
        {
            [customerKey] = new(BusinessQueryValueKind.String, customerId, true),
            [alias] = new(BusinessQueryValueKind.Null, "")
        })], false, "original-result-hash");
        var names = new Dictionary<(string Key, string Value), string>();
        if (found) names[(customerKey, customerId)] = "客户<测试>|一";
        var formatted = new BusinessQueryPresentationFormatter().Format(query, result,
            new BusinessQueryPresentationOverrides("销售订单查询结果", ProjectBusinessQueryPresentation.CreateLabels(query,
                new("销售订单查询结果", new Dictionary<string, string> { [customerKey] = "客户", ["salesOrder.netAmount"] = "未税金额" }, new Dictionary<string, BusinessCatalogNameLookup>())), names));
        Assert.Equal("销售订单查询结果", formatted.Title);
        Assert.Equal("客户", formatted.Columns[0].Label);
        Assert.Equal("未税金额", formatted.Columns[1].Label);
        Assert.Empty(formatted.Columns[1].Unit);
        Assert.Equal(found ? "客户<测试>|一" : customerId, formatted.Rows[0][customerKey].DisplayValue);
        Assert.True(formatted.Rows[0][customerKey].UntrustedData);
        Assert.Equal("—", formatted.Rows[0][alias].DisplayValue);
        Assert.Equal(customerId, result.Rows[0].Values[customerKey].CanonicalValue);
        Assert.Equal(BusinessQueryValueKind.Null, result.Rows[0].Values[alias].Kind);
        Assert.Equal("original-result-hash", result.ResultSha256);
        if (found) Assert.Contains("客户&lt;测试&gt;\\|一", formatted.Markdown);
        var generic = new BusinessQueryPresentationFormatter().Format(query, result);
        Assert.Equal(alias, generic.Columns[1].Label);
        Assert.Equal(customerId, generic.Rows[0][customerKey].DisplayValue);
    }
}
