using System.Collections.ObjectModel;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Catalog;

public sealed record BusinessCatalogFieldSnapshot(
    string Name,
    string PhysicalColumn,
    BusinessCatalogFieldKind Kind,
    BusinessCatalogDataType DataType,
    string Description,
    string RequiredPermission,
    IReadOnlySet<Contracts.BusinessFilterOperator> AllowedOperators,
    IReadOnlySet<Contracts.BusinessAggregation> AllowedAggregations,
    BusinessCatalogSensitivity Sensitivity,
    BusinessMeasureAdditivity Additivity,
    string Unit,
    string Currency,
    int? Precision,
    int? Scale,
    BusinessNullHandling NullHandling);

public sealed record BusinessCatalogEntitySnapshot
{
    public BusinessCatalogEntitySnapshot(
        string name,
        string physicalTable,
        string description,
        string requiredPermission,
        bool requiresTimeRange,
        IReadOnlyList<string> grain,
        string defaultScopeField,
        IReadOnlyDictionary<string, BusinessCatalogFieldSnapshot> fields)
    {
        Name = name;
        PhysicalTable = physicalTable;
        Description = description;
        RequiredPermission = requiredPermission;
        RequiresTimeRange = requiresTimeRange;
        Grain = new ReadOnlyCollection<string>(grain.ToArray());
        DefaultScopeField = defaultScopeField;
        Fields = new ReadOnlyDictionary<string, BusinessCatalogFieldSnapshot>(
            fields.ToDictionary(
                item => item.Key,
                item => item.Value with { },
                StringComparer.Ordinal));
    }

    public string Name { get; }

    public string PhysicalTable { get; }

    public string Description { get; }

    public string RequiredPermission { get; }

    public bool RequiresTimeRange { get; }

    public IReadOnlyList<string> Grain { get; }

    public string DefaultScopeField { get; }

    public IReadOnlyDictionary<string, BusinessCatalogFieldSnapshot> Fields { get; }
}

public sealed record BusinessCatalogRelationshipSnapshot(
    string Name,
    string FromEntity,
    string ToEntity,
    string FromField,
    string ToField,
    BusinessRelationshipCardinality Cardinality,
    BusinessFanOutPolicy FanOutPolicy);

public sealed record BusinessCatalogSnapshot
{
    public BusinessCatalogSnapshot(
        string catalogId,
        long revision,
        string dataSourceCode,
        BusinessCatalogDialect dialect,
        string timeZoneId,
        string culture,
        string formatterVersion,
        bool includeBoundaryTies,
        string canonicalJson,
        string sha256,
        int modelSchemaBudgetCharacters,
        IReadOnlyDictionary<string, BusinessCatalogEntitySnapshot> entities,
        IReadOnlyList<BusinessCatalogRelationshipSnapshot> relationships)
    {
        CatalogId = catalogId;
        Revision = revision;
        DataSourceCode = dataSourceCode;
        Dialect = dialect;
        TimeZoneId = timeZoneId;
        Culture = culture;
        FormatterVersion = formatterVersion;
        IncludeBoundaryTies = includeBoundaryTies;
        CanonicalJson = canonicalJson;
        Sha256 = sha256;
        ModelSchemaBudgetCharacters = modelSchemaBudgetCharacters;
        Entities = new ReadOnlyDictionary<string, BusinessCatalogEntitySnapshot>(
            entities.ToDictionary(
                item => item.Key,
                item => item.Value,
                StringComparer.Ordinal));
        Relationships = new ReadOnlyCollection<BusinessCatalogRelationshipSnapshot>(
            relationships.Select(value => value with { }).ToArray());
    }

    public string CatalogId { get; }

    public long Revision { get; }

    public string DataSourceCode { get; }

    public BusinessCatalogDialect Dialect { get; }

    public string TimeZoneId { get; }

    public string Culture { get; }

    public string FormatterVersion { get; }

    public bool IncludeBoundaryTies { get; }

    public string CanonicalJson { get; }

    public string Sha256 { get; }

    public int ModelSchemaBudgetCharacters { get; }

    public IReadOnlyDictionary<string, BusinessCatalogEntitySnapshot> Entities { get; }

    public IReadOnlyList<BusinessCatalogRelationshipSnapshot> Relationships { get; }

    public BusinessCatalogFieldSnapshot? FindField(string logicalName) =>
        Entities.Values
            .SelectMany(entity => entity.Fields.Values)
            .SingleOrDefault(field => string.Equals(
                field.Name,
                logicalName,
                StringComparison.Ordinal));
}
