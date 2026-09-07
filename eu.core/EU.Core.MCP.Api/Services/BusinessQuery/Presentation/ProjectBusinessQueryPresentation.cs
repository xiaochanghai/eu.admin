using EU.Core.Api.MCP.Services.BusinessQuery.Compilation;
using EU.Core.Api.MCP.Services.BusinessQuery.Contracts;
using EU.Core.Model.Entity;
using SqlSugar;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Presentation;

/// <summary>仅影响展示，原始分组键、金额和结果摘要保持不变。</summary>
public sealed record BusinessQueryPresentationOverrides(string Title,
    IReadOnlyDictionary<string, string> Labels,
    IReadOnlyDictionary<(string Key, string Value), string> Values);

/// <summary>销售主表查询的固定名称映射；只查已返回的 ID，不接收模型指定表或列。</summary>
public static class ProjectBusinessQueryPresentation
{
    public static async Task<BusinessQueryPresentationOverrides> CreateAsync(CompiledBusinessQuery query,
        BusinessQueryResult result, ISqlSugarClient database, CancellationToken cancellationToken)
    {
        var values = new Dictionary<(string Key, string Value), string>();
        Guid[] customerIds = ReadIds(result, "salesOrder.customerId");
        Guid[] currencyIds = ReadIds(result, "salesOrder.currencyId");
        // 使用同一请求已选定的数据库；公司权限当前按项目要求停用，不跨库查询名称。
        if (customerIds.Length > 0)
        {
            var customers = await database.Queryable<BdCustomer>()
                .Where(x => customerIds.Contains(x.ID) && x.IsActive == true && x.IsDeleted == false)
                .Select(x => new { x.ID, Name = x.CustomerName }).ToListAsync(cancellationToken);
            foreach (var item in customers) AddNames(values, result, "salesOrder.customerId", item.ID, item.Name);
        }
        if (currencyIds.Length > 0)
        {
            var currencies = await database.Queryable<BdCurrency>()
                .Where(x => currencyIds.Contains(x.ID) && x.IsActive == true && x.IsDeleted == false)
                .Select(x => new { x.ID, Name = x.CurrencyName }).ToListAsync(cancellationToken);
            foreach (var item in currencies) AddNames(values, result, "salesOrder.currencyId", item.ID, item.Name);
        }
        return new BusinessQueryPresentationOverrides("销售订单查询结果", CreateLabels(query), values);
    }

    public static IReadOnlyDictionary<string, string> CreateLabels(CompiledBusinessQuery query)
    {
        var labels = new Dictionary<string, string> { ["rank"] = "排名" };
        foreach (var column in query.Columns)
        {
            string? label = column.LogicalField switch
            {
                "salesOrder.customerId" => "客户",
                "salesOrder.currencyId" => "币别",
                "salesOrder.id" => "订单标识",
                "salesOrder.orderNo" => "订单编号",
                "salesOrder.netAmount" => "未税金额",
                "salesOrder.taxAmount" => "税额",
                "salesOrder.grossAmount" => "含税金额",
                "salesOrder.status" => "订单状态",
                "salesOrder.auditStatus" => "审核状态",
                _ => null
            };
            if (label is not null) labels[column.ResultKey] = label;
        }
        return labels;
    }

    private static Guid[] ReadIds(BusinessQueryResult result, string key) => result.Rows
        .Select(row => row.Values.TryGetValue(key, out var value) && Guid.TryParse(value.CanonicalValue, out var id) ? id : Guid.Empty)
        .Where(id => id != Guid.Empty).Distinct().ToArray();

    private static void AddNames(Dictionary<(string Key, string Value), string> names, BusinessQueryResult result,
        string key, Guid id, string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        string safeName = new(name.Trim().Where(c => !char.IsControl(c)).Take(256).ToArray());
        if (safeName.Length == 0) return;
        foreach (var row in result.Rows)
            if (row.Values.TryGetValue(key, out var value) && Guid.TryParse(value.CanonicalValue, out var rowId) && rowId == id)
                names[(key, value.CanonicalValue)] = safeName;
    }
}
