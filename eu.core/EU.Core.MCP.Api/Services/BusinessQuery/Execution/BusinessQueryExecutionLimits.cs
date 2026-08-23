namespace EU.Core.Api.MCP.Services.BusinessQuery.Execution;

public sealed record BusinessQueryExecutionLimits
{
    public int CommandTimeoutSeconds { get; init; } = 30;

    public int MaximumColumns { get; init; } = 24;

    public int MaximumRows { get; init; } = 100;

    public int MaximumCellUtf8Bytes { get; init; } = 16_384;

    public int MaximumPayloadUtf8Bytes { get; init; } = 262_144;

    public void Validate()
    {
        if (CommandTimeoutSeconds is < 1 or > 300
            || MaximumColumns is < 1 or > 128
            || MaximumRows is < 1 or > 10_000
            || MaximumCellUtf8Bytes is < 1 or > 1_048_576
            || MaximumPayloadUtf8Bytes < MaximumCellUtf8Bytes
            || MaximumPayloadUtf8Bytes > 16_777_216)
        {
            throw new ArgumentException("Business query execution limits are invalid.");
        }
    }
}
