using System.Text.RegularExpressions;
using EU.Core.Api.MCP.Services.BusinessQuery.Compilation;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Execution;

public sealed partial class BusinessQueryReceipt
{
    private BusinessQueryReceipt(
        Guid queryId,
        long catalogRevision,
        string catalogHash,
        string toolSchemaHash,
        string queryPlanHash,
        Guid policyDecisionId,
        DateTimeOffset evaluatedAtUtc,
        string timeZoneId,
        DateTimeOffset? startUtc,
        DateTimeOffset? endUtc,
        int rowCount,
        bool includeBoundaryTies,
        bool truncated,
        string resultHash,
        string terminalStatus)
    {
        QueryId = queryId;
        CatalogRevision = catalogRevision;
        CatalogHash = catalogHash;
        ToolSchemaHash = toolSchemaHash;
        QueryPlanHash = queryPlanHash;
        PolicyDecisionId = policyDecisionId;
        EvaluatedAtUtc = evaluatedAtUtc;
        TimeZoneId = timeZoneId;
        StartUtc = startUtc;
        EndUtc = endUtc;
        RowCount = rowCount;
        IncludeBoundaryTies = includeBoundaryTies;
        Truncated = truncated;
        ResultHash = resultHash;
        TerminalStatus = terminalStatus;
    }

    public Guid QueryId { get; }
    public long CatalogRevision { get; }
    public string CatalogHash { get; }
    public string ToolSchemaHash { get; }
    public string QueryPlanHash { get; }
    public Guid PolicyDecisionId { get; }
    public DateTimeOffset EvaluatedAtUtc { get; }
    public string TimeZoneId { get; }
    public DateTimeOffset? StartUtc { get; }
    public DateTimeOffset? EndUtc { get; }
    public int RowCount { get; }
    public bool IncludeBoundaryTies { get; }
    public bool Truncated { get; }
    public string ResultHash { get; }
    public string TerminalStatus { get; }

    public static BusinessQueryReceipt Create(
        CompiledBusinessQuery query,
        string toolSchemaHash,
        BusinessQueryExecutionResult execution) =>
        Create(Guid.NewGuid(), query, toolSchemaHash, execution);

    public static BusinessQueryReceipt Create(
        Guid queryId,
        CompiledBusinessQuery query,
        string toolSchemaHash,
        BusinessQueryExecutionResult execution)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(execution);
        if (queryId == Guid.Empty
            || !Sha256Pattern().IsMatch(toolSchemaHash ?? string.Empty)
            || !Sha256Pattern().IsMatch(execution.Result.ResultSha256)
            || execution.TerminalStatus != "succeeded"
            || execution.Result.Truncated)
        {
            throw new ArgumentException("Business query Receipt input is invalid.");
        }

        return new BusinessQueryReceipt(
            queryId,
            query.CatalogRevision,
            query.CatalogHash,
            toolSchemaHash!,
            query.PlanHash,
            query.PolicyDecisionId,
            query.EvaluatedAtUtc,
            query.TimeZoneId,
            query.StartUtc,
            query.EndUtc,
            execution.Result.Rows.Count,
            query.IncludeBoundaryTies,
            execution.Result.Truncated,
            execution.Result.ResultSha256,
            execution.TerminalStatus);
    }

    [GeneratedRegex("^[a-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Pattern();
}
