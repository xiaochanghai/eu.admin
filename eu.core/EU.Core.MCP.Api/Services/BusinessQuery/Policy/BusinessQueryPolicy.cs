using System.Globalization;
using System.Text.Json;
using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;
using EU.Core.Api.MCP.Services.BusinessQuery.Contracts;
using EU.Core.Api.MCP.Services.BusinessQuery.Security;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Policy;

public sealed class BusinessQueryPolicy
{
    private static readonly string[] AllowRuleIds =
    [
        "caller.identity",
        "catalog.data-source",
        "catalog.permissions",
        "catalog.fields",
        "query.time-range",
        "query.result-limit",
        "query.complexity",
        "scope.trusted-injection",
        "quota.atomic-reservation"
    ];

    private readonly BusinessQueryPolicyOptions _options;
    private readonly IBusinessQueryQuotaStore _quotaStore;

    public BusinessQueryPolicy(
        BusinessQueryPolicyOptions options,
        IBusinessQueryQuotaStore quotaStore)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(quotaStore);
        if (string.IsNullOrWhiteSpace(options.TenantId)
            || string.IsNullOrWhiteSpace(options.DataSourceCode)
            || options.MaximumResultRows is < 1 or > 100
            || options.MinimumGroupSize is < 2 or > 1000
            || options.MaximumDateSpanDays is < 1 or > 3660
            || options.MaximumComplexity < 1
            || string.IsNullOrWhiteSpace(options.ContainsPermission))
        {
            throw new ArgumentException("Business query Policy options are invalid.", nameof(options));
        }

        _options = options;
        _quotaStore = quotaStore;
    }

    public async Task<BusinessQueryPolicyDecision> AuthorizeAsync(
        BusinessCallerContext caller,
        BusinessCatalogSnapshot catalog,
        BusinessQueryPlan plan,
        BusinessQueryEvaluationTime evaluationTime,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(caller);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(evaluationTime);
        cancellationToken.ThrowIfCancellationRequested();

        string planHash = BusinessQueryPlanFingerprint.Compute(plan);
        if (!IsSafeIdentity(caller.UserId) || !IsSafeIdentity(caller.TenantId))
        {
            return Deny(BusinessQueryPolicyErrorCodes.CallerInvalid, catalog, planHash);
        }

        if (!string.Equals(caller.TenantId, _options.TenantId, StringComparison.Ordinal))
        {
            return Deny(BusinessQueryPolicyErrorCodes.TenantMismatch, catalog, planHash);
        }

        if (!string.Equals(catalog.DataSourceCode, _options.DataSourceCode, StringComparison.Ordinal)
            || !caller.AllowedDataSourceCodes.Contains(catalog.DataSourceCode))
        {
            return Deny(BusinessQueryPolicyErrorCodes.DataSourceUnauthorized, catalog, planHash);
        }

        if (!catalog.Entities.TryGetValue(plan.Entity, out BusinessCatalogEntitySnapshot? entity))
        {
            return Deny(BusinessQueryPolicyErrorCodes.EntityUnknown, catalog, planHash);
        }

        if (!caller.Permissions.Contains(entity.RequiredPermission))
        {
            return Deny(BusinessQueryPolicyErrorCodes.PermissionDenied, catalog, planHash);
        }

        var selectedFields = new List<BusinessCatalogFieldSnapshot>();
        foreach (string dimension in plan.Dimensions)
        {
            BusinessCatalogFieldSnapshot? field = ResolveField(catalog, entity, dimension);
            if (field is null || field.Kind != BusinessCatalogFieldKind.Dimension)
            {
                return Deny(FieldError(field), catalog, planHash);
            }

            selectedFields.Add(field);
        }

        foreach (BusinessMeasure measure in plan.Measures)
        {
            if (!entity.Fields.TryGetValue(
                    measure.Field,
                    out BusinessCatalogFieldSnapshot? field)
                || field.Kind != BusinessCatalogFieldKind.Measure
                || !field.AllowedAggregations.Contains(measure.Aggregation))
            {
                return Deny(FieldError(field), catalog, planHash);
            }

            selectedFields.Add(field);
        }

        foreach (BusinessFilter filter in plan.Filters)
        {
            BusinessCatalogFieldSnapshot? field = ResolveField(catalog, entity, filter.Field);
            if (field is null
                || field.Kind == BusinessCatalogFieldKind.Scope
                || !field.AllowedOperators.Contains(filter.Operator)
                || !IsValueCompatible(field.DataType, filter.Operator, filter.Value))
            {
                return Deny(FieldError(field), catalog, planHash);
            }

            if (filter.Operator == BusinessFilterOperator.Contains
                && !caller.Permissions.Contains(_options.ContainsPermission))
            {
                return Deny(BusinessQueryPolicyErrorCodes.ContainsDenied, catalog, planHash);
            }

            selectedFields.Add(field);
        }

        if (!TryValidateTimeRange(catalog, entity, plan, evaluationTime, out string? timeError, out BusinessCatalogFieldSnapshot? timeField))
        {
            return Deny(timeError!, catalog, planHash);
        }

        if (timeField is not null)
        {
            selectedFields.Add(timeField);
        }

        HashSet<string> selectableOrderFields = new(plan.Dimensions, StringComparer.Ordinal);
        selectableOrderFields.UnionWith(plan.Measures.Select(value => value.ResultKey));
        if (plan.OrderBy.Any(value => !selectableOrderFields.Contains(value.Field)))
        {
            return Deny(BusinessQueryPolicyErrorCodes.FieldInvalid, catalog, planHash);
        }

        foreach (BusinessCatalogFieldSnapshot field in selectedFields.DistinctBy(value => value.Name))
        {
            if (!caller.Permissions.Contains(field.RequiredPermission))
            {
                return Deny(BusinessQueryPolicyErrorCodes.PermissionDenied, catalog, planHash);
            }
        }

        if (selectedFields.Any(value =>
                value.Sensitivity is BusinessCatalogSensitivity.Confidential
                    or BusinessCatalogSensitivity.Restricted)
            && plan.Measures.Count == 0)
        {
            return Deny(BusinessQueryPolicyErrorCodes.PermissionDenied, catalog, planHash);
        }

        if (plan.Limit > _options.MaximumResultRows)
        {
            return Deny(BusinessQueryPolicyErrorCodes.ResultLimitExceeded, catalog, planHash);
        }

        if (!TryBuildScope(caller, catalog, entity, out BusinessDataScope? scope, out string? scopeError))
        {
            return Deny(scopeError!, catalog, planHash);
        }

        int complexity = CalculateComplexity(plan, catalog, entity, evaluationTime);
        if (complexity > _options.MaximumComplexity)
        {
            return Deny(
                BusinessQueryPolicyErrorCodes.ComplexityExceeded,
                catalog,
                planHash,
                complexity);
        }

        BusinessQueryQuotaReservationResult reservation = await _quotaStore
            .TryReserveAsync(
                new BusinessQueryQuotaRequest(
                    caller.UserId,
                    caller.TenantId,
                    planHash,
                    complexity,
                    evaluationTime.EvaluatedAtUtc),
                cancellationToken)
            .ConfigureAwait(false);
        if (!reservation.Accepted
            || !reservation.ReservationId.HasValue
            || reservation.ReservationId.Value == Guid.Empty)
        {
            return Deny(
                BusinessQueryPolicyErrorCodes.QuotaExceeded,
                catalog,
                planHash,
                complexity);
        }

        return new BusinessQueryPolicyDecision(
            true,
            null,
            Guid.NewGuid(),
            catalog.Revision,
            catalog.Sha256,
            AllowRuleIds,
            _options.MaximumResultRows,
            _options.MinimumGroupSize,
            complexity,
            _options.MaximumComplexity,
            scope!,
            planHash,
            BusinessQueryEvaluationTimeFingerprint.Compute(evaluationTime),
            reservation.ReservationId);
    }

    private static BusinessCatalogFieldSnapshot? ResolveField(
        BusinessCatalogSnapshot catalog,
        BusinessCatalogEntitySnapshot root,
        string logicalName)
    {
        BusinessCatalogEntitySnapshot? owner = catalog.Entities.Values.SingleOrDefault(
            value => value.Fields.ContainsKey(logicalName));
        if (owner is null || !IsReachable(catalog, root.Name, owner.Name))
        {
            return null;
        }

        return owner.Fields[logicalName];
    }

    private static bool IsReachable(
        BusinessCatalogSnapshot catalog,
        string start,
        string target)
    {
        if (string.Equals(start, target, StringComparison.Ordinal))
        {
            return true;
        }

        var pending = new Queue<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal) { start };
        pending.Enqueue(start);
        while (pending.TryDequeue(out string? current))
        {
            foreach (string next in catalog.Relationships
                         .Where(value => value.FromEntity == current)
                         .Select(value => value.ToEntity))
            {
                if (next == target)
                {
                    return true;
                }

                if (seen.Add(next))
                {
                    pending.Enqueue(next);
                }
            }
        }

        return false;
    }

    private bool TryValidateTimeRange(
        BusinessCatalogSnapshot catalog,
        BusinessCatalogEntitySnapshot entity,
        BusinessQueryPlan plan,
        BusinessQueryEvaluationTime evaluationTime,
        out string? error,
        out BusinessCatalogFieldSnapshot? timeField)
    {
        timeField = null;
        if (!string.Equals(evaluationTime.TimeZoneId, catalog.TimeZoneId, StringComparison.Ordinal)
            || evaluationTime.EvaluatedAtUtc.Offset != TimeSpan.Zero)
        {
            error = BusinessQueryPolicyErrorCodes.TimeRangeInvalid;
            return false;
        }

        if (plan.TimeRange is null)
        {
            error = entity.RequiresTimeRange
                ? BusinessQueryPolicyErrorCodes.TimeRangeRequired
                : null;
            return !entity.RequiresTimeRange;
        }

        timeField = ResolveField(catalog, entity, plan.TimeRange.Field);
        if (timeField is null
            || timeField.Kind != BusinessCatalogFieldKind.Time
            || !evaluationTime.StartUtc.HasValue
            || !evaluationTime.EndUtc.HasValue
            || evaluationTime.StartUtc.Value.Offset != TimeSpan.Zero
            || evaluationTime.EndUtc.Value.Offset != TimeSpan.Zero
            || evaluationTime.StartUtc >= evaluationTime.EndUtc)
        {
            error = BusinessQueryPolicyErrorCodes.TimeRangeInvalid;
            return false;
        }

        if (plan.TimeRange.Preset is null
            && (plan.TimeRange.Start?.ToUniversalTime() != evaluationTime.StartUtc
                || plan.TimeRange.End?.ToUniversalTime() != evaluationTime.EndUtc))
        {
            error = BusinessQueryPolicyErrorCodes.TimeRangeInvalid;
            return false;
        }

        if (evaluationTime.EndUtc.Value - evaluationTime.StartUtc.Value
            > TimeSpan.FromDays(_options.MaximumDateSpanDays))
        {
            error = BusinessQueryPolicyErrorCodes.TimeRangeExceeded;
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryBuildScope(
        BusinessCallerContext caller,
        BusinessCatalogSnapshot catalog,
        BusinessCatalogEntitySnapshot entity,
        out BusinessDataScope? scope,
        out string? error)
    {
        if (string.IsNullOrEmpty(entity.DefaultScopeField))
        {
            scope = BusinessDataScope.Empty;
            error = null;
            return true;
        }

        BusinessCatalogFieldSnapshot? field = catalog.FindField(entity.DefaultScopeField);
        if (field is null
            || field.Kind != BusinessCatalogFieldKind.Scope
            || !caller.Permissions.Contains(field.RequiredPermission)
            || !caller.DataScopes.TryGetValue(field.Name, out IReadOnlyList<string>? values)
            || values.Count == 0
            || values.Count > 100
            || values.Any(value => string.IsNullOrWhiteSpace(value) || value.Length > 128)
            || values.Distinct(StringComparer.Ordinal).Count() != values.Count)
        {
            scope = null;
            error = BusinessQueryPolicyErrorCodes.ScopeRequired;
            return false;
        }

        if (field.Name.EndsWith(".tenantId", StringComparison.Ordinal)
            && (values.Count != 1
                || !string.Equals(values[0], caller.TenantId, StringComparison.Ordinal)))
        {
            scope = null;
            error = BusinessQueryPolicyErrorCodes.TenantMismatch;
            return false;
        }

        scope = new BusinessDataScope([new BusinessScopeConstraint(field.Name, values)]);
        error = null;
        return true;
    }

    private static int CalculateComplexity(
        BusinessQueryPlan plan,
        BusinessCatalogSnapshot catalog,
        BusinessCatalogEntitySnapshot entity,
        BusinessQueryEvaluationTime evaluationTime)
    {
        int filterCost = plan.Filters.Sum(value => value.Operator switch
        {
            BusinessFilterOperator.Contains => 20,
            BusinessFilterOperator.In => 3 + value.Value.GetArrayLength(),
            BusinessFilterOperator.Between => 5,
            _ => 2
        });
        int joinedEntities = plan.Dimensions
            .Concat(plan.Measures.Select(value => value.Field))
            .Concat(plan.Filters.Select(value => value.Field))
            .Select(field => catalog.Entities.Values.Single(value => value.Fields.ContainsKey(field)).Name)
            .Where(owner => owner != entity.Name)
            .Distinct(StringComparer.Ordinal)
            .Count();
        int dayCost = evaluationTime.StartUtc.HasValue && evaluationTime.EndUtc.HasValue
            ? Math.Max(1, (int)Math.Ceiling(
                (evaluationTime.EndUtc.Value - evaluationTime.StartUtc.Value).TotalDays / 31D))
            : 1;
        return 1
            + (plan.Dimensions.Count * 3)
            + (plan.Measures.Count * 5)
            + filterCost
            + (plan.OrderBy.Count * 2)
            + (joinedEntities * 10)
            + dayCost
            + plan.Limit;
    }

    private static bool IsValueCompatible(
        BusinessCatalogDataType type,
        BusinessFilterOperator operation,
        JsonElement value)
    {
        if (operation is BusinessFilterOperator.In or BusinessFilterOperator.Between)
        {
            return value.ValueKind == JsonValueKind.Array
                && value.EnumerateArray().All(item => IsScalarCompatible(type, item));
        }

        return IsScalarCompatible(type, value);
    }

    private static bool IsScalarCompatible(BusinessCatalogDataType type, JsonElement value) =>
        type switch
        {
            BusinessCatalogDataType.String => value.ValueKind == JsonValueKind.String,
            BusinessCatalogDataType.Boolean => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            BusinessCatalogDataType.Integer => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _),
            BusinessCatalogDataType.Decimal => value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out _),
            BusinessCatalogDataType.Date or BusinessCatalogDataType.DateTime =>
                value.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(
                    value.GetString(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out _),
            _ => false
        };

    private static bool IsSafeIdentity(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= 128
        && !value.Any(char.IsControl);

    private static string FieldError(BusinessCatalogFieldSnapshot? field) =>
        field?.Kind == BusinessCatalogFieldKind.Scope
            ? BusinessQueryPolicyErrorCodes.ScopeConflict
            : BusinessQueryPolicyErrorCodes.FieldInvalid;

    private BusinessQueryPolicyDecision Deny(
        string errorCode,
        BusinessCatalogSnapshot catalog,
        string planHash,
        int complexity = 0) =>
        new(
            false,
            errorCode,
            Guid.NewGuid(),
            catalog.Revision,
            catalog.Sha256,
            [],
            _options.MaximumResultRows,
            _options.MinimumGroupSize,
            complexity,
            _options.MaximumComplexity,
            BusinessDataScope.Empty,
            planHash,
            string.Empty,
            null);
}
