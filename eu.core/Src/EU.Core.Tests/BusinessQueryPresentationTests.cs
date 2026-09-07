using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;
using EU.Core.Api.MCP.Services.BusinessQuery.Compilation;
using EU.Core.Api.MCP.Services.BusinessQuery.Contracts;
using EU.Core.Api.MCP.Services.BusinessQuery.Presentation;
using Xunit;

namespace EU.Core.Tests;

public sealed class BusinessQueryPresentationTests
{
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
            new BusinessQueryPresentationOverrides("销售订单查询结果", ProjectBusinessQueryPresentation.CreateLabels(query), names));
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
