using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;
using EU.Core.Api.MCP.Services.BusinessQuery.Contracts;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Time;

public sealed record BusinessQueryTimeResolutionError(
    string Code,
    string Message);

public sealed record BusinessQueryTimeResolutionResult(
    BusinessQueryEvaluationTime? EvaluationTime,
    BusinessQueryTimeResolutionError? Error)
{
    public bool Succeeded => Error is null;
}

public sealed class BusinessQueryTimeRangeResolver
{
    public const string TimeRangeInvalid = "BUSINESS_QUERY_TIME_RANGE_INVALID";
    public const string TimeZoneUnavailable = "BUSINESS_QUERY_TIME_ZONE_UNAVAILABLE";

    private readonly TimeProvider _timeProvider;

    public BusinessQueryTimeRangeResolver(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public BusinessQueryTimeResolutionResult Resolve(
        BusinessQueryPlan plan,
        BusinessCatalogSnapshot catalog)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(catalog);

        DateTimeOffset evaluatedAtUtc = _timeProvider.GetUtcNow().ToUniversalTime();
        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(catalog.TimeZoneId);
        }
        catch (Exception exception) when (
            exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return Failure(
                TimeZoneUnavailable,
                "The configured business time zone is unavailable.");
        }

        if (plan.TimeRange is null)
        {
            return Success(new BusinessQueryEvaluationTime(
                evaluatedAtUtc,
                catalog.TimeZoneId,
                null,
                null));
        }

        if (plan.TimeRange.Preset == BusinessTimePreset.PreviousYear)
        {
            DateTimeOffset localNow = TimeZoneInfo.ConvertTime(
                evaluatedAtUtc,
                timeZone);
            if (localNow.Year <= 1)
            {
                return Failure(
                    TimeRangeInvalid,
                    "The previous business year cannot be represented.");
            }

            int year = localNow.Year - 1;
            DateTime localStart = DateTime.SpecifyKind(
                new DateTime(year, 1, 1),
                DateTimeKind.Unspecified);
            DateTime localEnd = DateTime.SpecifyKind(
                new DateTime(year + 1, 1, 1),
                DateTimeKind.Unspecified);
            if (timeZone.IsInvalidTime(localStart)
                || timeZone.IsInvalidTime(localEnd)
                || timeZone.IsAmbiguousTime(localStart)
                || timeZone.IsAmbiguousTime(localEnd))
            {
                return Failure(
                    TimeRangeInvalid,
                    "The business time range crosses an invalid local time.");
            }

            return Success(new BusinessQueryEvaluationTime(
                evaluatedAtUtc,
                catalog.TimeZoneId,
                new DateTimeOffset(
                    TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone),
                    TimeSpan.Zero),
                new DateTimeOffset(
                    TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone),
                    TimeSpan.Zero)));
        }

        if (plan.TimeRange.Preset is null
            && plan.TimeRange.Start.HasValue
            && plan.TimeRange.End.HasValue)
        {
            DateTimeOffset start = plan.TimeRange.Start.Value.ToUniversalTime();
            DateTimeOffset end = plan.TimeRange.End.Value.ToUniversalTime();
            if (start >= end)
            {
                return Failure(
                    TimeRangeInvalid,
                    "The business time range is invalid.");
            }

            return Success(new BusinessQueryEvaluationTime(
                evaluatedAtUtc,
                catalog.TimeZoneId,
                start,
                end));
        }

        return Failure(
            TimeRangeInvalid,
            "The business time range is invalid.");
    }

    private static BusinessQueryTimeResolutionResult Success(
        BusinessQueryEvaluationTime value) =>
        new(value, null);

    private static BusinessQueryTimeResolutionResult Failure(
        string code,
        string message) =>
        new(null, new BusinessQueryTimeResolutionError(code, message));
}
