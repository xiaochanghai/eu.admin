using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using EU.Core.Api.MCP.Services.BusinessQuery.Contracts;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Catalog;

public sealed record BusinessCatalogValidationError(
    string Code,
    string Message);

public sealed record BusinessCatalogValidationResult(
    BusinessCatalogSnapshot? Snapshot,
    BusinessCatalogValidationError? Error)
{
    public bool Succeeded => Error is null;
}

public sealed partial class BusinessSemanticCatalogValidator
{
    public const int MaximumEntities = 32;
    public const int MaximumFieldsPerEntity = 64;
    public const int MaximumTotalFields = 256;
    public const int MaximumRelationships = 128;
    public const int MaximumGrainFields = 8;
    public const int MaximumDescriptionCharacters = 512;
    public const int MaximumPermissionCharacters = 128;
    public const int MaximumGeneratedToolSchemaCharacters = 65_536;

    public const string CatalogInvalid = "BUSINESS_CATALOG_INVALID";
    public const string CatalogHashMismatch = "BUSINESS_CATALOG_HASH_MISMATCH";
    public const string CatalogSchemaTooLarge = "BUSINESS_CATALOG_SCHEMA_TOO_LARGE";

    public BusinessCatalogValidationResult Validate(
        BusinessSemanticCatalog? catalog,
        string canonicalJson,
        string sha256)
    {
        if (catalog is null
            || !SafeCodePattern().IsMatch(catalog.CatalogId ?? string.Empty)
            || catalog.Revision <= 0
            || !SafeCodePattern().IsMatch(catalog.DataSourceCode ?? string.Empty)
            || catalog.Dialect == BusinessCatalogDialect.Unknown
            || !IsValidTimeZone(catalog.TimeZoneId)
            || !IsValidCulture(catalog.Culture)
            || !SafeVersionPattern().IsMatch(catalog.FormatterVersion ?? string.Empty)
            || !catalog.IncludeBoundaryTies
            || catalog.Entities is null
            || catalog.Relationships is null)
        {
            return Invalid("The semantic Catalog header is invalid.");
        }

        if (catalog.Entities.Count is < 1 or > MaximumEntities
            || catalog.Relationships.Count > MaximumRelationships
            || catalog.Entities.Sum(entity => entity?.Fields?.Count ?? 0)
                > MaximumTotalFields)
        {
            return Invalid("The semantic Catalog exceeds its structural limits.");
        }

        var entitySources = new Dictionary<string, BusinessCatalogEntity>(
            StringComparer.Ordinal);
        var fieldOwners = new Dictionary<string, string>(StringComparer.Ordinal);
        var snapshots = new Dictionary<string, BusinessCatalogEntitySnapshot>(
            StringComparer.Ordinal);
        foreach (BusinessCatalogEntity entity in catalog.Entities)
        {
            if (!TryValidateEntity(entity, out string? error)
                || !entitySources.TryAdd(entity.Name, entity))
            {
                return Invalid(error ?? "Catalog entity names must be unique.");
            }

            var fields = new Dictionary<string, BusinessCatalogFieldSnapshot>(
                StringComparer.Ordinal);
            var physicalColumns = new HashSet<string>(StringComparer.Ordinal);
            foreach (BusinessCatalogField field in entity.Fields)
            {
                if (!TryValidateField(field, out error)
                    || !fields.TryAdd(field.Name, FreezeField(field))
                    || !physicalColumns.Add(field.PhysicalColumn!)
                    || !fieldOwners.TryAdd(field.Name, entity.Name))
                {
                    return Invalid(error ?? "Catalog logical and physical fields must be unique.");
                }
            }

            if (entity.Grain.Any(name =>
                    !fields.TryGetValue(name, out BusinessCatalogFieldSnapshot? field)
                    || field.Kind is not (
                        BusinessCatalogFieldKind.Dimension
                        or BusinessCatalogFieldKind.Scope))
                || (!string.IsNullOrEmpty(entity.DefaultScopeField)
                    && (!fields.TryGetValue(
                            entity.DefaultScopeField,
                            out BusinessCatalogFieldSnapshot? scope)
                        || scope.Kind != BusinessCatalogFieldKind.Scope)))
            {
                return Invalid("Entity Grain or default scope fields are invalid.");
            }

            snapshots.Add(
                entity.Name,
                new BusinessCatalogEntitySnapshot(
                    entity.Name,
                    entity.PhysicalTable,
                    entity.Description,
                    entity.RequiredPermission,
                    entity.RequiresTimeRange,
                    entity.Grain,
                    entity.DefaultScopeField,
                    fields));
        }

        var relationships = new List<BusinessCatalogRelationshipSnapshot>();
        var relationshipNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (BusinessCatalogRelationship relationship in catalog.Relationships)
        {
            if (!TryValidateRelationship(
                    relationship,
                    snapshots,
                    fieldOwners,
                    out string? error)
                || !relationshipNames.Add(relationship.Name))
            {
                return Invalid(error ?? "Catalog relationship names must be unique.");
            }

            relationships.Add(new BusinessCatalogRelationshipSnapshot(
                relationship.Name,
                relationship.FromEntity,
                relationship.ToEntity,
                relationship.FromField,
                relationship.ToField,
                relationship.Cardinality,
                relationship.FanOutPolicy));
        }

        if (HasRelationshipCycle(snapshots.Keys, relationships))
        {
            return Invalid("Catalog relationships cannot contain a cycle.");
        }

        if (HasAmbiguousRelationshipPath(snapshots.Keys, relationships))
        {
            return Invalid("Catalog relationships cannot contain ambiguous paths.");
        }

        int schemaBudget = EstimateToolSchemaCharacters(catalog);
        if (schemaBudget > MaximumGeneratedToolSchemaCharacters)
        {
            return new BusinessCatalogValidationResult(
                null,
                new BusinessCatalogValidationError(
                    CatalogSchemaTooLarge,
                    "The model-visible Catalog exceeds the tool Schema budget."));
        }

        return new BusinessCatalogValidationResult(
            new BusinessCatalogSnapshot(
                catalog.CatalogId!,
                catalog.Revision,
                catalog.DataSourceCode!,
                catalog.Dialect,
                catalog.TimeZoneId,
                catalog.Culture,
                catalog.FormatterVersion!,
                catalog.IncludeBoundaryTies,
                canonicalJson,
                sha256,
                schemaBudget,
                snapshots,
                relationships),
            null);
    }

    private static bool TryValidateEntity(
        BusinessCatalogEntity? entity,
        out string? error)
    {
        if (entity is null
            || !LogicalNamePattern().IsMatch(entity.Name ?? string.Empty)
            || !PhysicalIdentifierPattern().IsMatch(
                entity.PhysicalTable ?? string.Empty)
            || !IsDescription(entity.Description)
            || !IsPermission(entity.RequiredPermission)
            || entity.Grain is null
            || entity.Grain.Count is < 1 or > MaximumGrainFields
            || entity.Grain.Any(name =>
                !LogicalNamePattern().IsMatch(name ?? string.Empty))
            || HasDuplicates(entity.Grain)
            || (entity.DefaultScopeField?.Length > 0
                && !LogicalNamePattern().IsMatch(entity.DefaultScopeField))
            || entity.Fields is null
            || entity.Fields.Count is < 1 or > MaximumFieldsPerEntity)
        {
            error = "A Catalog entity is invalid.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryValidateField(
        BusinessCatalogField? field,
        out string? error)
    {
        if (field is null
            || !LogicalNamePattern().IsMatch(field.Name ?? string.Empty)
            || !PhysicalSegmentPattern().IsMatch(field.PhysicalColumn ?? string.Empty)
            || !IsDescription(field.Description)
            || !IsPermission(field.RequiredPermission)
            || field.Kind == BusinessCatalogFieldKind.Unknown
            || field.DataType == BusinessCatalogDataType.Unknown
            || field.Sensitivity == BusinessCatalogSensitivity.Unknown
            || field.NullHandling == BusinessNullHandling.Unknown
            || field.AllowedOperators is null
            || field.AllowedAggregations is null
            || HasDuplicates(field.AllowedOperators)
            || HasDuplicates(field.AllowedAggregations)
            || (field.Kind == BusinessCatalogFieldKind.Scope
                && (field.AllowedOperators.Count > 0
                    || field.AllowedAggregations.Count > 0))
            || field.AllowedOperators.Any(value =>
                !IsCompatibleOperator(field.DataType, value))
            || field.AllowedAggregations.Any(value =>
                !IsCompatibleAggregation(field.DataType, value)))
        {
            error = "A Catalog field is invalid.";
            return false;
        }

        bool isMeasure = field.Kind == BusinessCatalogFieldKind.Measure;
        if (isMeasure != (field.AllowedAggregations.Count > 0)
            || (isMeasure
                && field.Additivity is BusinessMeasureAdditivity.Unknown
                    or BusinessMeasureAdditivity.NotApplicable)
            || (!isMeasure
                && field.Additivity != BusinessMeasureAdditivity.NotApplicable)
            || (field.Additivity == BusinessMeasureAdditivity.NonAdditive
                && field.AllowedAggregations.Contains(BusinessAggregation.Sum))
            || (field.Kind == BusinessCatalogFieldKind.Time
                && field.DataType is not (
                    BusinessCatalogDataType.Date
                    or BusinessCatalogDataType.DateTime)))
        {
            error = "Catalog aggregation or additivity metadata is invalid.";
            return false;
        }

        bool isDecimal = field.DataType == BusinessCatalogDataType.Decimal;
        if (isDecimal != (field.Precision.HasValue && field.Scale.HasValue)
            || (isDecimal
                && (field.Precision is < 1 or > 38
                    || field.Scale < 0
                    || field.Scale > field.Precision)))
        {
            error = "Catalog Decimal precision and scale are invalid.";
            return false;
        }

        bool isCurrency = string.Equals(
            field.Unit,
            "currency",
            StringComparison.Ordinal);
        if ((isCurrency && !CurrencyPattern().IsMatch(field.Currency ?? string.Empty))
            || (isCurrency && field.DataType != BusinessCatalogDataType.Decimal)
            || (!isCurrency && !string.IsNullOrEmpty(field.Currency))
            || !UnitPattern().IsMatch(field.Unit ?? string.Empty)
            || (field.NullHandling == BusinessNullHandling.Zero
                && field.DataType is not (
                    BusinessCatalogDataType.Integer
                    or BusinessCatalogDataType.Decimal)))
        {
            error = "Catalog unit or currency metadata is invalid.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryValidateRelationship(
        BusinessCatalogRelationship? relationship,
        IReadOnlyDictionary<string, BusinessCatalogEntitySnapshot> entities,
        IReadOnlyDictionary<string, string> fieldOwners,
        out string? error)
    {
        if (relationship is null
            || !LogicalNamePattern().IsMatch(relationship.Name ?? string.Empty)
            || relationship.FromEntity == relationship.ToEntity
            || !entities.TryGetValue(
                relationship.FromEntity ?? string.Empty,
                out BusinessCatalogEntitySnapshot? from)
            || !entities.TryGetValue(
                relationship.ToEntity ?? string.Empty,
                out BusinessCatalogEntitySnapshot? to)
            || !from.Fields.TryGetValue(
                relationship.FromField ?? string.Empty,
                out BusinessCatalogFieldSnapshot? fromField)
            || !to.Fields.TryGetValue(
                relationship.ToField ?? string.Empty,
                out BusinessCatalogFieldSnapshot? toField)
            || fieldOwners[relationship.FromField!] != relationship.FromEntity
            || fieldOwners[relationship.ToField!] != relationship.ToEntity
            || fromField.DataType != toField.DataType
            || fromField.Kind is not (
                BusinessCatalogFieldKind.Dimension
                or BusinessCatalogFieldKind.Scope)
            || toField.Kind is not (
                BusinessCatalogFieldKind.Dimension
                or BusinessCatalogFieldKind.Scope)
            || relationship.Cardinality == BusinessRelationshipCardinality.Unknown)
        {
            error = "A Catalog relationship is invalid.";
            return false;
        }

        bool canFanOut = relationship.Cardinality is
            BusinessRelationshipCardinality.OneToMany
            or BusinessRelationshipCardinality.ManyToMany;
        if ((canFanOut
                && relationship.FanOutPolicy is not (
                    BusinessFanOutPolicy.Reject
                    or BusinessFanOutPolicy.PreAggregateSource))
            || (!canFanOut
                && relationship.FanOutPolicy
                    != BusinessFanOutPolicy.NotApplicable))
        {
            error = "Catalog relationship fan-out policy is invalid.";
            return false;
        }

        error = null;
        return true;
    }

    private static BusinessCatalogFieldSnapshot FreezeField(
        BusinessCatalogField field) =>
        new(
            field.Name,
            field.PhysicalColumn,
            field.Kind,
            field.DataType,
            field.Description,
            field.RequiredPermission,
            new ReadOnlySet<BusinessFilterOperator>(
                new HashSet<BusinessFilterOperator>(field.AllowedOperators)),
            new ReadOnlySet<BusinessAggregation>(
                new HashSet<BusinessAggregation>(field.AllowedAggregations)),
            field.Sensitivity,
            field.Additivity,
            field.Unit,
            field.Currency,
            field.Precision,
            field.Scale,
            field.NullHandling);

    private static bool HasRelationshipCycle(
        IEnumerable<string> entityNames,
        IEnumerable<BusinessCatalogRelationshipSnapshot> relationships)
    {
        Dictionary<string, string[]> graph = entityNames.ToDictionary(
            name => name,
            name => relationships
                .Where(value => value.FromEntity == name)
                .Select(value => value.ToEntity)
                .ToArray(),
            StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);

        bool Visit(string current)
        {
            if (!visiting.Add(current))
            {
                return true;
            }

            if (visited.Contains(current))
            {
                visiting.Remove(current);
                return false;
            }

            if (graph[current].Any(Visit))
            {
                return true;
            }

            visiting.Remove(current);
            visited.Add(current);
            return false;
        }

        return graph.Keys.Any(Visit);
    }

    private static bool HasAmbiguousRelationshipPath(
        IEnumerable<string> entityNames,
        IEnumerable<BusinessCatalogRelationshipSnapshot> relationships)
    {
        Dictionary<string, string[]> graph = entityNames.ToDictionary(
            name => name,
            name => relationships
                .Where(value => value.FromEntity == name)
                .Select(value => value.ToEntity)
                .ToArray(),
            StringComparer.Ordinal);

        foreach (string start in graph.Keys)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            bool Visit(string current)
            {
                foreach (string next in graph[current])
                {
                    counts[next] = counts.GetValueOrDefault(next) + 1;
                    if (counts[next] > 1 || Visit(next))
                    {
                        return true;
                    }
                }

                return false;
            }

            if (Visit(start))
            {
                return true;
            }
        }

        return false;
    }

    private static int EstimateToolSchemaCharacters(BusinessSemanticCatalog value)
    {
        const int fixedEnvelope = 4_096;
        int logicalText = value.Entities.Sum(entity =>
            entity.Name.Length
            + entity.Description.Length
            + entity.RequiredPermission.Length
            + entity.Fields.Sum(field =>
                field.Name.Length
                + field.Description.Length
                + field.RequiredPermission.Length
                + 96));
        return fixedEnvelope + (logicalText * 2);
    }

    private static bool IsCompatibleOperator(
        BusinessCatalogDataType type,
        BusinessFilterOperator value) =>
        value switch
        {
            BusinessFilterOperator.Contains =>
                type == BusinessCatalogDataType.String,
            BusinessFilterOperator.GreaterThan
                or BusinessFilterOperator.GreaterThanOrEqual
                or BusinessFilterOperator.LessThan
                or BusinessFilterOperator.LessThanOrEqual
                or BusinessFilterOperator.Between =>
                type is BusinessCatalogDataType.Integer
                    or BusinessCatalogDataType.Decimal
                    or BusinessCatalogDataType.Date
                    or BusinessCatalogDataType.DateTime,
            _ => true
        };

    private static bool IsCompatibleAggregation(
        BusinessCatalogDataType type,
        BusinessAggregation value) =>
        value switch
        {
            BusinessAggregation.Count or BusinessAggregation.CountDistinct => true,
            BusinessAggregation.Sum or BusinessAggregation.Average =>
                type is BusinessCatalogDataType.Integer
                    or BusinessCatalogDataType.Decimal,
            BusinessAggregation.Minimum or BusinessAggregation.Maximum =>
                type is BusinessCatalogDataType.Integer
                    or BusinessCatalogDataType.Decimal
                    or BusinessCatalogDataType.Date
                    or BusinessCatalogDataType.DateTime,
            _ => false
        };

    private static bool IsDescription(string? value) =>
        value is not null
        && value.Length <= MaximumDescriptionCharacters
        && !value.Any(char.IsControl);

    private static bool IsPermission(string? value) =>
        value is not null
        && value.Length <= MaximumPermissionCharacters
        && PermissionPattern().IsMatch(value);

    private static bool IsValidTimeZone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !IanaTimeZonePattern().IsMatch(value))
        {
            return false;
        }

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(value);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    private static bool IsValidCulture(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(value);
            return !string.IsNullOrEmpty(culture.Name)
                && string.Equals(culture.Name, value, StringComparison.Ordinal);
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    private static bool HasDuplicates<T>(IEnumerable<T> values)
        where T : notnull
    {
        var seen = new HashSet<T>();
        return values.Any(value => !seen.Add(value));
    }

    private static BusinessCatalogValidationResult Invalid(string message) =>
        new(
            null,
            new BusinessCatalogValidationError(CatalogInvalid, message));

    [GeneratedRegex("^[a-z][a-z0-9-]{1,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeCodePattern();

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*(?:\\.[A-Za-z_][A-Za-z0-9_]*)?$", RegexOptions.CultureInvariant)]
    private static partial Regex PhysicalIdentifierPattern();

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex PhysicalSegmentPattern();

    [GeneratedRegex("^[a-z][A-Za-z0-9]*(?:\\.[a-z][A-Za-z0-9]*)*$", RegexOptions.CultureInvariant)]
    private static partial Regex LogicalNamePattern();

    [GeneratedRegex("^[A-Z]{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex CurrencyPattern();

    [GeneratedRegex("^(?:|count|currency|percentage|[a-z][a-z0-9-]{0,31})$", RegexOptions.CultureInvariant)]
    private static partial Regex UnitPattern();

    [GeneratedRegex("^[0-9]+(?:\\.[0-9]+){0,3}$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeVersionPattern();

    [GeneratedRegex("^[A-Za-z_+-]+/[A-Za-z0-9_+.-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex IanaTimeZonePattern();

    [GeneratedRegex("^[a-z][a-z0-9]*(?:[.-][a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex PermissionPattern();
}
