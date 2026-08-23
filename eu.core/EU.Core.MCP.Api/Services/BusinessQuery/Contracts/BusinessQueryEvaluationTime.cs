namespace EU.Core.Api.MCP.Services.BusinessQuery.Contracts;

public sealed record BusinessQueryEvaluationTime(
    DateTimeOffset EvaluatedAtUtc,
    string TimeZoneId,
    DateTimeOffset? StartUtc,
    DateTimeOffset? EndUtc);
