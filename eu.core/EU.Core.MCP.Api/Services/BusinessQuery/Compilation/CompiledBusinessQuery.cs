using System.Collections.ObjectModel;
using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Compilation;

public sealed record BusinessSqlParameter(
    string Name,
    BusinessCatalogDataType DataType,
    object Value);

public sealed record CompiledBusinessQueryColumn(
    string ResultKey,
    string LogicalField,
    string SqlAlias,
    BusinessCatalogDataType DataType,
    BusinessCatalogFieldKind Kind,
    BusinessCatalogSensitivity Sensitivity,
    string Unit,
    string Currency,
    int? Precision,
    int? Scale);

public sealed class CompiledBusinessQuery
{
    public const string BoundaryRankColumnAlias = "__rank";

    public CompiledBusinessQuery(
        string commandText,
        IEnumerable<BusinessSqlParameter> parameters,
        IEnumerable<CompiledBusinessQueryColumn> columns,
        BusinessCatalogDialect dialect,
        string dataSourceCode,
        string entity,
        string culture,
        string formatterVersion,
        long catalogRevision,
        string catalogHash,
        string planHash,
        Guid policyDecisionId,
        DateTimeOffset evaluatedAtUtc,
        string timeZoneId,
        DateTimeOffset? startUtc,
        DateTimeOffset? endUtc,
        int requestedRankLimit,
        int maximumResultRows,
        bool includeBoundaryTies)
    {
        CommandText = commandText;
        Parameters = new ReadOnlyCollection<BusinessSqlParameter>(
            parameters.Select(value => value with { }).ToArray());
        Columns = new ReadOnlyCollection<CompiledBusinessQueryColumn>(
            columns.Select(value => value with { }).ToArray());
        Dialect = dialect;
        DataSourceCode = dataSourceCode;
        Entity = entity;
        Culture = culture;
        FormatterVersion = formatterVersion;
        CatalogRevision = catalogRevision;
        CatalogHash = catalogHash;
        PlanHash = planHash;
        PolicyDecisionId = policyDecisionId;
        EvaluatedAtUtc = evaluatedAtUtc;
        TimeZoneId = timeZoneId;
        StartUtc = startUtc;
        EndUtc = endUtc;
        RequestedRankLimit = requestedRankLimit;
        MaximumResultRows = maximumResultRows;
        IncludeBoundaryTies = includeBoundaryTies;
    }

    public string CommandText { get; }

    public IReadOnlyList<BusinessSqlParameter> Parameters { get; }

    public IReadOnlyList<CompiledBusinessQueryColumn> Columns { get; }

    public BusinessCatalogDialect Dialect { get; }

    public string DataSourceCode { get; }

    public string Entity { get; }

    public string Culture { get; }

    public string FormatterVersion { get; }

    public long CatalogRevision { get; }

    public string CatalogHash { get; }

    public string PlanHash { get; }

    public Guid PolicyDecisionId { get; }

    public DateTimeOffset EvaluatedAtUtc { get; }

    public string TimeZoneId { get; }

    public DateTimeOffset? StartUtc { get; }

    public DateTimeOffset? EndUtc { get; }

    public int RequestedRankLimit { get; }

    public int MaximumResultRows { get; }

    public bool IncludeBoundaryTies { get; }
}
