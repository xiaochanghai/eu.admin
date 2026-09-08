using EU.Core.Api.MCP.Services.BusinessQuery.Compilation;
using EU.Core.Api.MCP.Services.BusinessQuery.Contracts;
using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;
using SqlSugar;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Presentation;

/// <summary>仅影响展示，原始分组键、金额和结果摘要保持不变。</summary>
public sealed record BusinessQueryPresentationOverrides(string Title,
    IReadOnlyDictionary<string, string> Labels,
    IReadOnlyDictionary<(string Key, string Value), string> Values);

/// <summary>目录驱动的项目展示；只查已返回的 ID，不接收模型指定表或列。</summary>
public static class ProjectBusinessQueryPresentation
{
    public static async Task<BusinessQueryPresentationOverrides> CreateAsync(CompiledBusinessQuery query,
        BusinessQueryResult result, BusinessCatalogPresentation display, ISqlSugarClient database, CancellationToken cancellationToken)
    {
        var values = new Dictionary<(string Key, string Value), string>();
        if (result.Rows.Count > query.MaximumResultRows)
            throw new InvalidOperationException("BUSINESS_QUERY_PRESENTATION_LIMIT_EXCEEDED");
        foreach (var column in query.Columns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!display.Lookups.TryGetValue(column.LogicalField, out var lookup)) continue;
            Guid[] ids = ReadIds(result, column.ResultKey);
            if (ids.Length == 0) continue;
            await ReadNamesAsync(query, result, column.ResultKey, ids, lookup, database, values, cancellationToken);
        }
        return new BusinessQueryPresentationOverrides(display.Title, CreateLabels(query, display), values);
    }

    private static async Task ReadNamesAsync(CompiledBusinessQuery query, BusinessQueryResult result, string key, Guid[] ids,
        BusinessCatalogNameLookup lookup, ISqlSugarClient database, Dictionary<(string Key, string Value), string> values, CancellationToken cancellationToken)
    {
        IBusinessSqlDialect dialect = query.Dialect switch
        {
            BusinessCatalogDialect.SqlServer => new SqlServerBusinessSqlDialect(),
            BusinessCatalogDialect.MySql => new MySqlBusinessSqlDialect(),
            BusinessCatalogDialect.Sqlite => new SqliteBusinessSqlDialect(),
            _ => throw new InvalidOperationException("BUSINESS_QUERY_DIALECT_UNSUPPORTED")
        };
        string Q(string name) => dialect.QuoteIdentifier(name);
        var parameters = new List<SugarParameter>();
        string Parameter(object value)
        {
            string name = dialect.ParameterName(parameters.Count);
            parameters.Add(new SugarParameter(name, value));
            return name;
        }
        string inValues = string.Join(", ", ids.Select(id => Parameter(query.Dialect == BusinessCatalogDialect.SqlServer ? (object)id : id.ToString())));
        string filters = string.Join(" AND ", lookup.RequiredBooleanFilters.OrderBy(item => item.Key, StringComparer.Ordinal)
            .Select(item => $"{Q(item.Key)} = {Parameter(item.Value)}"));
        string limit = Parameter(ids.Length + 1);
        string sql = $"SELECT {(query.Dialect == BusinessCatalogDialect.SqlServer ? $"TOP ({limit}) " : "")}{Q(lookup.KeyColumn)}, {Q(lookup.NameColumn)} FROM {Q(lookup.PhysicalTable)} "
            + $"WHERE {Q(lookup.KeyColumn)} IN ({inValues}) AND {filters}"
            + (query.Dialect == BusinessCatalogDialect.SqlServer ? "" : $" LIMIT {limit}");
        var seen = new HashSet<Guid>();
        database.Ado.CancellationToken = cancellationToken;
        try
        {
            using var dataReader = await database.Ado.GetDataReaderAsync(sql, parameters.ToArray());
            if (dataReader is not System.Data.Common.DbDataReader reader)
                throw new InvalidOperationException("BUSINESS_QUERY_PRESENTATION_RESULT_INVALID");
            while (await reader.ReadAsync(cancellationToken))
            {
                if (!Guid.TryParse(reader.GetValue(0).ToString(), out var id) || !ids.Contains(id) || !seen.Add(id))
                    throw new InvalidOperationException("BUSINESS_QUERY_PRESENTATION_LOOKUP_NOT_UNIQUE");
                AddNames(values, result, key, id, reader.IsDBNull(1) ? null : reader.GetValue(1).ToString());
            }
        }
        finally { database.Ado.RemoveCancellationToken(); }
    }

    public static IReadOnlyDictionary<string, string> CreateLabels(CompiledBusinessQuery query, BusinessCatalogPresentation display)
    {
        var labels = new Dictionary<string, string>();
        if (display.Labels.TryGetValue("rank", out var rank)) labels["rank"] = rank;
        foreach (var column in query.Columns)
        {
            if (display.Labels.TryGetValue(column.LogicalField, out var label)) labels[column.ResultKey] = label;
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
