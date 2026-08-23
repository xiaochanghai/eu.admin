using System.Collections.ObjectModel;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Policy;

public sealed class BusinessQueryPolicyDecision
{
    internal BusinessQueryPolicyDecision(
        bool allowed,
        string? errorCode,
        Guid decisionId,
        long catalogRevision,
        string catalogHash,
        IEnumerable<string> appliedRuleIds,
        int maximumResultRows,
        int minimumGroupSize,
        int complexity,
        int complexityBudget,
        BusinessDataScope dataScope,
        string planHash,
        string evaluationTimeHash,
        Guid? quotaReservationId)
    {
        Allowed = allowed;
        ErrorCode = errorCode;
        DecisionId = decisionId;
        CatalogRevision = catalogRevision;
        CatalogHash = catalogHash;
        AppliedRuleIds = new ReadOnlyCollection<string>(appliedRuleIds.ToArray());
        MaximumResultRows = maximumResultRows;
        MinimumGroupSize = minimumGroupSize;
        Complexity = complexity;
        ComplexityBudget = complexityBudget;
        DataScope = dataScope;
        PlanHash = planHash;
        EvaluationTimeHash = evaluationTimeHash;
        QuotaReservationId = quotaReservationId;
    }

    public bool Allowed { get; }

    public string? ErrorCode { get; }

    public Guid DecisionId { get; }

    public long CatalogRevision { get; }

    public string CatalogHash { get; }

    public IReadOnlyList<string> AppliedRuleIds { get; }

    public int MaximumResultRows { get; }

    public int MinimumGroupSize { get; }

    public int Complexity { get; }

    public int ComplexityBudget { get; }

    public BusinessDataScope DataScope { get; }

    public string PlanHash { get; }

    public string EvaluationTimeHash { get; }

    public Guid? QuotaReservationId { get; }
}
