using System.Text.Json;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Contracts;

public enum BusinessFilterOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    In,
    Between,
    Contains
}

public enum BusinessAggregation
{
    Sum,
    Count,
    CountDistinct,
    Average,
    Minimum,
    Maximum
}

public enum BusinessSortDirection
{
    Ascending,
    Descending
}

public enum BusinessTimePreset
{
    PreviousYear
}

public sealed record BusinessMeasure(
    string Field,
    BusinessAggregation Aggregation,
    string ResultKey);

public sealed record BusinessFilter(
    string Field,
    BusinessFilterOperator Operator,
    JsonElement Value);

public sealed record BusinessTimeRange(
    string Field,
    BusinessTimePreset? Preset,
    DateTimeOffset? Start,
    DateTimeOffset? End);

public sealed record BusinessOrder(
    string Field,
    BusinessSortDirection Direction);

public sealed record BusinessQueryPlan(
    string Entity,
    IReadOnlyList<string> Dimensions,
    IReadOnlyList<BusinessMeasure> Measures,
    IReadOnlyList<BusinessFilter> Filters,
    BusinessTimeRange? TimeRange,
    IReadOnlyList<BusinessOrder> OrderBy,
    int Limit);
