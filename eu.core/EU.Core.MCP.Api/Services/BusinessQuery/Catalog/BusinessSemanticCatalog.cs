using EU.Core.Api.MCP.Services.BusinessQuery.Contracts;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Catalog;

public enum BusinessCatalogDialect
{
    Unknown,
    SqlServer,
    Sqlite,
    MySql,
    Auto
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
    BusinessNullHandling NullHandling)
{
    /// <summary>可选业务日期派生维度；只允许 year 或 yearMonth，不接受 SQL 表达式。</summary>
    public BusinessCalendarDimension? CalendarDimension { get; init; }
}

public sealed record BusinessCalendarDimension(string SourceField, string Part);

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

    /// <summary>可选的单明细源：按主表唯一粒度预汇总后 LEFT JOIN；未配置时直接读取主表。</summary>
    public BusinessDetailAggregate? DetailAggregate { get; init; }

    /// <summary>可选展示配置；不改变原始查询结果及分组键。</summary>
    public BusinessCatalogPresentation? Presentation { get; init; }
}

/// <summary>标题、逻辑字段标签及名称查找配置；仅受信任目录可定义。</summary>
public sealed record BusinessCatalogPresentation(string Title, IReadOnlyDictionary<string, string> Labels,
    IReadOnlyDictionary<string, BusinessCatalogNameLookup> Lookups);

/// <summary>同数据库的 GUID 名称映射；只查已返回的 ID，缺失名称回退原 ID。</summary>
public sealed record BusinessCatalogNameLookup(string PhysicalTable, string KeyColumn, string NameColumn,
    IReadOnlyDictionary<string, bool> RequiredBooleanFilters)
{
    /// <summary>仅供历史报表名称展示包含软删除记录，默认关闭；不放宽事实查询或启用条件。</summary>
    public bool IncludeSoftDeleted { get; init; }
}

/// <summary>受信任的明细预汇总配置，不允许 SQL 表达式；当前仅支持数值求和与空值补零。</summary>
public sealed record BusinessDetailAggregate(
    string PhysicalTable,
    string ParentKeyField,
    string ForeignKeyColumn,
    IReadOnlyDictionary<string, bool> RequiredBooleanFilters,
    IReadOnlyDictionary<string, string> Measures);

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
