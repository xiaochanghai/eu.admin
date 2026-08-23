using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using EU.Core.Api.MCP.Services.BusinessQuery.Contracts;
using EU.Core.Api.MCP.Services.BusinessQuery.Errors;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Validation;

public sealed record BusinessQueryPlanError(string Code, string Message);

public sealed record BusinessQueryPlanParseResult(
    BusinessQueryPlan? Plan,
    BusinessQueryPlanError? Error)
{
    public bool Succeeded => Error is null;

    public static BusinessQueryPlanParseResult Success(BusinessQueryPlan plan) =>
        new(plan, null);

    public static BusinessQueryPlanParseResult Failure(
        string code,
        string message) =>
        new(null, new BusinessQueryPlanError(code, message));
}

public sealed partial class BusinessQueryPlanValidator
{
    public const int MaximumUtf8Bytes = 32 * 1024;
    public const int MaximumJsonDepth = 16;
    public const int MaximumDimensions = 8;
    public const int MaximumMeasures = 8;
    public const int MaximumFilters = 16;
    public const int MaximumOrderBy = 4;
    public const int MaximumLimit = 100;
    public const int MaximumInValues = 100;
    public const int MaximumPlanParameters = 256;
    public const int MaximumLogicalNameCharacters = 128;
    public const int MaximumResultKeyCharacters = 64;
    public const int MaximumStringValueCharacters = 1_024;
    public const string GeneratedRankResultKey = "rank";

    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        AllowTrailingCommas = false,
        MaxDepth = MaximumJsonDepth,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters =
        {
            new JsonStringEnumConverter(
                JsonNamingPolicy.CamelCase,
                allowIntegerValues: false)
        }
    };

    public BusinessQueryPlanParseResult Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Failure(
                BusinessQueryErrorCodes.PlanRequired,
                "A business QueryPlan is required.");
        }

        int utf8Bytes;
        try
        {
            utf8Bytes = StrictUtf8.GetByteCount(json);
        }
        catch (EncoderFallbackException)
        {
            return Failure(
                BusinessQueryErrorCodes.PlanInvalidJson,
                "The QueryPlan is not valid UTF-8 text.");
        }

        if (utf8Bytes > MaximumUtf8Bytes)
        {
            return Failure(
                BusinessQueryErrorCodes.PlanTooLarge,
                "The business QueryPlan exceeds the supported size.");
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(
                json,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = MaximumJsonDepth
                });
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Invalid("The QueryPlan root must be an object.");
            }

            if (HasDuplicateProperties(document.RootElement))
            {
                return Failure(
                    BusinessQueryErrorCodes.PlanDuplicateProperty,
                    "The QueryPlan contains a duplicate JSON property.");
            }

            BusinessQueryPlan? plan;
            try
            {
                plan = JsonSerializer.Deserialize<BusinessQueryPlan>(
                    document.RootElement.GetRawText(),
                    SerializerOptions);
            }
            catch (JsonException exception) when (
                exception.Message.Contains(
                    "could not be mapped",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Failure(
                    BusinessQueryErrorCodes.PlanUnknownProperty,
                    "The QueryPlan contains an unsupported property.");
            }

            return plan is null
                ? Invalid("The QueryPlan is invalid.")
                : ValidateAndFreeze(plan);
        }
        catch (JsonException exception) when (
            exception.Message.Contains(
                "could not be mapped",
                StringComparison.OrdinalIgnoreCase))
        {
            return Failure(
                BusinessQueryErrorCodes.PlanUnknownProperty,
                "The QueryPlan contains an unsupported property.");
        }
        catch (JsonException)
        {
            return Failure(
                BusinessQueryErrorCodes.PlanInvalidJson,
                "The QueryPlan is not valid JSON.");
        }
    }

    private static BusinessQueryPlanParseResult ValidateAndFreeze(
        BusinessQueryPlan plan)
    {
        IReadOnlyList<string>? dimensions = plan.Dimensions;
        IReadOnlyList<BusinessMeasure>? measures = plan.Measures;
        IReadOnlyList<BusinessFilter>? filters = plan.Filters;
        IReadOnlyList<BusinessOrder>? orderBy = plan.OrderBy;
        if (!IsLogicalName(plan.Entity, MaximumLogicalNameCharacters)
            || dimensions is null
            || measures is null
            || filters is null
            || orderBy is null)
        {
            return Invalid("The QueryPlan contains a missing or invalid value.");
        }

        if (dimensions.Count > MaximumDimensions
            || measures.Count > MaximumMeasures
            || filters.Count > MaximumFilters
            || orderBy.Count > MaximumOrderBy
            || plan.Limit is < 1 or > MaximumLimit)
        {
            return Failure(
                BusinessQueryErrorCodes.PlanLimitExceeded,
                "The QueryPlan exceeds a structural limit.");
        }

        if (dimensions.Count == 0 && measures.Count == 0)
        {
            return Invalid("The QueryPlan must select a dimension or measure.");
        }

        if (dimensions.Any(value =>
                !IsLogicalName(value, MaximumLogicalNameCharacters))
            || HasDuplicates(dimensions))
        {
            return Invalid("The QueryPlan dimensions are invalid or duplicated.");
        }

        if (measures.Any(value =>
                value is null
                || !IsLogicalName(value.Field, MaximumLogicalNameCharacters)
                || !IsResultKey(value.ResultKey)
                || string.Equals(
                    value.ResultKey,
                    GeneratedRankResultKey,
                    StringComparison.Ordinal)
                || dimensions.Contains(value.ResultKey, StringComparer.Ordinal))
            || HasDuplicates(measures.Select(value => value.ResultKey))
            || HasDuplicates(measures.Select(value =>
                $"{value.Field}\u001f{value.Aggregation}")))
        {
            return Invalid("The QueryPlan measures are invalid or duplicated.");
        }

        if (dimensions.Contains(GeneratedRankResultKey, StringComparer.Ordinal))
        {
            return Invalid("The QueryPlan uses a reserved result key.");
        }

        foreach (BusinessFilter filter in filters)
        {
            if (filter is null
                || !IsLogicalName(filter.Field, MaximumLogicalNameCharacters)
                || !IsValidFilterValue(filter.Operator, filter.Value))
            {
                return Invalid("The QueryPlan contains an invalid filter.");
            }
        }

        int planParameterCount = filters.Sum(EstimateParameterCount)
            + (plan.TimeRange is null ? 0 : 2);
        if (planParameterCount > MaximumPlanParameters)
        {
            return Failure(
                BusinessQueryErrorCodes.PlanLimitExceeded,
                "The QueryPlan exceeds the supported parameter count.");
        }

        if (plan.TimeRange is not null && !IsValidTimeRange(plan.TimeRange))
        {
            return Invalid("The QueryPlan contains an invalid time range.");
        }

        if (orderBy.Any(value =>
                value is null
                || !IsLogicalName(value.Field, MaximumLogicalNameCharacters))
            || HasDuplicates(orderBy.Select(value => value.Field)))
        {
            return Invalid("The QueryPlan ordering is invalid or duplicated.");
        }

        var frozenPlan = plan with
        {
            Dimensions = ReadOnly(dimensions),
            Measures = ReadOnly(measures.Select(value => value with { })),
            Filters = ReadOnly(filters.Select(value => value with
            {
                Value = value.Value.Clone()
            })),
            TimeRange = plan.TimeRange is null
                ? null
                : plan.TimeRange with { },
            OrderBy = ReadOnly(orderBy.Select(value => value with { }))
        };
        return BusinessQueryPlanParseResult.Success(frozenPlan);
    }

    private static bool IsValidTimeRange(BusinessTimeRange value)
    {
        if (!IsLogicalName(value.Field, MaximumLogicalNameCharacters))
        {
            return false;
        }

        bool usesPreset = value.Preset.HasValue;
        bool usesAbsolute = value.Start.HasValue || value.End.HasValue;
        return usesPreset
            ? !usesAbsolute
            : value.Start.HasValue
              && value.End.HasValue
              && value.Start.Value < value.End.Value;
    }

    private static int EstimateParameterCount(BusinessFilter value) =>
        value.Operator switch
        {
            BusinessFilterOperator.In or BusinessFilterOperator.Between =>
                value.Value.GetArrayLength(),
            _ => 1
        };

    private static bool IsValidFilterValue(
        BusinessFilterOperator operation,
        JsonElement value)
    {
        if (operation == BusinessFilterOperator.In)
        {
            return value.ValueKind == JsonValueKind.Array
                && value.GetArrayLength() is >= 1 and <= MaximumInValues
                && IsHomogeneousScalarArray(value);
        }

        if (operation == BusinessFilterOperator.Between)
        {
            return value.ValueKind == JsonValueKind.Array
                && value.GetArrayLength() == 2
                && IsHomogeneousScalarArray(value);
        }

        if (operation == BusinessFilterOperator.Contains)
        {
            return value.ValueKind == JsonValueKind.String
                && IsValidStringValue(value.GetString());
        }

        return IsValidScalar(value);
    }

    private static bool IsHomogeneousScalarArray(JsonElement array)
    {
        JsonValueKind? kind = null;
        foreach (JsonElement item in array.EnumerateArray())
        {
            if (!IsValidScalar(item))
            {
                return false;
            }

            JsonValueKind normalized = item.ValueKind == JsonValueKind.Number
                ? JsonValueKind.Number
                : item.ValueKind;
            if (kind.HasValue && kind.Value != normalized)
            {
                return false;
            }

            kind = normalized;
        }

        return kind.HasValue;
    }

    private static bool IsValidScalar(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.String => IsValidStringValue(value.GetString()),
            JsonValueKind.Number =>
                value.TryGetInt64(out _) || value.TryGetDecimal(out _),
            JsonValueKind.True or JsonValueKind.False => true,
            _ => false
        };

    private static bool IsValidStringValue(string? value) =>
        value is not null
        && value.Length <= MaximumStringValueCharacters
        && !value.Any(char.IsControl);

    private static bool HasDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)
                    || HasDuplicateProperties(property.Value))
                {
                    return true;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in element.EnumerateArray())
            {
                if (HasDuplicateProperties(item))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasDuplicates(IEnumerable<string> values)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        return values.Any(value => !seen.Add(value));
    }

    private static bool IsLogicalName(string? value, int maximumCharacters) =>
        value is not null
        && value.Length <= maximumCharacters
        && LogicalNamePattern().IsMatch(value);

    private static bool IsResultKey(string? value) =>
        value is not null
        && value.Length <= MaximumResultKeyCharacters
        && ResultKeyPattern().IsMatch(value);

    private static ReadOnlyCollection<T> ReadOnly<T>(IEnumerable<T> values) =>
        new(values.ToArray());

    private static BusinessQueryPlanParseResult Invalid(string message) =>
        Failure(BusinessQueryErrorCodes.PlanInvalid, message);

    private static BusinessQueryPlanParseResult Failure(
        string code,
        string message) =>
        BusinessQueryPlanParseResult.Failure(code, message);

    [GeneratedRegex("^[a-z][A-Za-z0-9]*(?:\\.[a-z][A-Za-z0-9]*)*$", RegexOptions.CultureInvariant)]
    private static partial Regex LogicalNamePattern();

    [GeneratedRegex("^[a-z][A-Za-z0-9]*$", RegexOptions.CultureInvariant)]
    private static partial Regex ResultKeyPattern();
}
