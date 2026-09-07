using EU.Core.Api.MCP.Services.BusinessQuery.Contracts;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Catalog;

public enum BusinessCatalogDialect
{
    Unknown,
    SqlServer,
    Sqlite,
    MySql
}

public enum BusinessCatalogFieldKind
{
    Unknown,
    Dimension,
    Measure,
    Time,
    Scope
}

public enum BusinessCatalogDataType
{
    Unknown,
    String,
    Boolean,
    Integer,
    Decimal,
    Date,
    DateTime
}

public enum BusinessCatalogSensitivity
{
    Unknown,
    Public,
    Internal,
    Confidential,
    Restricted
}

public enum BusinessMeasureAdditivity
{
    Unknown,
    NotApplicable,
    Additive,
    SemiAdditive,
    NonAdditive
}

public enum BusinessNullHandling
{
    Unknown,
    Preserve,
    Exclude,
    Zero
}

public enum BusinessRelationshipCardinality
{
    Unknown,
    OneToOne,
    ManyToOne,
    OneToMany,
    ManyToMany
}

public enum BusinessFanOutPolicy
{
    Unknown,
    NotApplicable,
    Reject,
    PreAggregateSource
}

public sealed record BusinessCatalogField(
    string Name,
    string PhysicalColumn,
    BusinessCatalogFieldKind Kind,
    BusinessCatalogDataType DataType,
    string Description,
    string RequiredPermission,
    IReadOnlyList<BusinessFilterOperator> AllowedOperators,
    IReadOnlyList<BusinessAggregation> AllowedAggregations,
    BusinessCatalogSensitivity Sensitivity,
    BusinessMeasureAdditivity Additivity,
    string Unit,
    string Currency,
    int? Precision,
    int? Scale,
    BusinessNullHandling NullHandling);

public sealed record BusinessCatalogEntity(
    string Name,
    string PhysicalTable,
    string Description,
    string RequiredPermission,
    bool RequiresTimeRange,
    IReadOnlyList<string> Grain,
    string DefaultScopeField,
    IReadOnlyList<BusinessCatalogField> Fields)
{
    /// <summary>受信任目录定义的物理布尔列过滤，不允许查询计划取消或覆盖。</summary>
    public IReadOnlyDictionary<string, bool> RequiredBooleanFilters { get; init; } = new Dictionary<string, bool>();

    /// <summary>聚合时必须保留的维度，例如按订单币别分别统计，禁止跨币别合计。</summary>
    public IReadOnlyList<string> RequiredMeasureDimensions { get; init; } = [];

    /// <summary>EU 项目模块代码；设置后必须通过项目模块权限和公司数据范围校验。</summary>
    public string ProjectModuleCode { get; init; } = string.Empty;
}

public sealed record BusinessCatalogRelationship(
    string Name,
    string FromEntity,
    string ToEntity,
    string FromField,
    string ToField,
    BusinessRelationshipCardinality Cardinality,
    BusinessFanOutPolicy FanOutPolicy);

public sealed record BusinessSemanticCatalog(
    string CatalogId,
    long Revision,
    string DataSourceCode,
    BusinessCatalogDialect Dialect,
    string TimeZoneId,
    string Culture,
    string FormatterVersion,
    bool IncludeBoundaryTies,
    IReadOnlyList<BusinessCatalogEntity> Entities,
    IReadOnlyList<BusinessCatalogRelationship> Relationships);
