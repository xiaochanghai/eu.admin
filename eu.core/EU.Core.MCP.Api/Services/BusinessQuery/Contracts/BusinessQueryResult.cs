using System.Collections.ObjectModel;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Contracts;

public enum BusinessQueryValueKind
{
    Null,
    String,
    Boolean,
    Integer,
    Decimal,
    Date,
    DateTime
}

public sealed record BusinessQueryColumn(
    string Key,
    BusinessQueryValueKind Kind,
    string Unit,
    string Currency);

public sealed record BusinessQueryValue(
    BusinessQueryValueKind Kind,
    string CanonicalValue,
    bool UntrustedData = false);

public sealed record BusinessQueryRow
{
    public BusinessQueryRow(
        IReadOnlyDictionary<string, BusinessQueryValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Values = new ReadOnlyDictionary<string, BusinessQueryValue>(
            values.ToDictionary(
                item => item.Key,
                item => item.Value with { },
                StringComparer.Ordinal));
    }

    public IReadOnlyDictionary<string, BusinessQueryValue> Values { get; }
}

public sealed record BusinessQueryResult
{
    public BusinessQueryResult(
        IReadOnlyList<BusinessQueryColumn> columns,
        IReadOnlyList<BusinessQueryRow> rows,
        bool truncated,
        string resultSha256)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);
        Columns = new ReadOnlyCollection<BusinessQueryColumn>(
            columns.Select(value => value with { }).ToArray());
        Rows = new ReadOnlyCollection<BusinessQueryRow>(
            rows.Select(value => new BusinessQueryRow(value.Values)).ToArray());
        Truncated = truncated;
        ResultSha256 = resultSha256 ?? string.Empty;
    }

    public IReadOnlyList<BusinessQueryColumn> Columns { get; }

    public IReadOnlyList<BusinessQueryRow> Rows { get; }

    public bool Truncated { get; }

    public string ResultSha256 { get; }
}
