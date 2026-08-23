namespace EU.Core.Api.MCP.Services.BusinessQuery.Policy;

public sealed record BusinessQueryPolicyOptions
{
    public required string TenantId { get; init; }

    public required string DataSourceCode { get; init; }

    public int MaximumResultRows { get; init; } = 100;

    public int MinimumGroupSize { get; init; } = 5;

    public int MaximumDateSpanDays { get; init; } = 366;

    public int MaximumComplexity { get; init; } = 250;

    public string ContainsPermission { get; init; } = "business.query.contains";
}
