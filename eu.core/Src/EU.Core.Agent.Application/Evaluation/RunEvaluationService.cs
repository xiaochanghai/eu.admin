using System.Collections.ObjectModel;
using System.Text;
using EU.Core.Agent.Application.UnifiedEntry;

namespace EU.Core.Agent.Application.Evaluation;

public static class RunEvaluationErrorCodes
{
    public const string SpecificationInvalid = "RUN_EVALUATION_SPECIFICATION_INVALID";
    public const string RunNotFound = "RUN_EVALUATION_RUN_NOT_FOUND";
}

public sealed record RunEvaluationSpecification(
    UnifiedRunStatus? ExpectedStatus,
    IReadOnlyList<string> OutputContains,
    IReadOnlyList<string> OutputExcludes,
    IReadOnlyList<string> RequiredEventKinds,
    int? MaximumToolCalls,
    long? MaximumDurationMilliseconds);

public sealed record RunEvaluationCheck(
    string Code,
    bool Passed,
    string Expected,
    string Actual);

public sealed record RunEvaluationReport(
    Guid RunId,
    DateTimeOffset EvaluatedAtUtc,
    bool Passed,
    decimal Score,
    string OutputSha256,
    int OutputUtf8Bytes,
    IReadOnlyList<RunEvaluationCheck> Checks);

public sealed class RunEvaluationException(string errorCode, string message)
    : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}

public static class RunEvaluationSpecificationValidator
{
    private const int MaximumRulesPerGroup = 20;
    private const int MaximumRuleUtf8Bytes = 200;
    private const int MaximumTotalRuleUtf8Bytes = 4096;

    public static void Validate(RunEvaluationSpecification specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ValidateRules(specification.OutputContains);
        ValidateRules(specification.OutputExcludes);
        ValidateRules(specification.RequiredEventKinds);
        int totalBytes = specification.OutputContains
            .Concat(specification.OutputExcludes)
            .Concat(specification.RequiredEventKinds)
            .Sum(value => Encoding.UTF8.GetByteCount(value));
        if (totalBytes > MaximumTotalRuleUtf8Bytes
            || specification.MaximumToolCalls is < 0 or > 1000
            || specification.MaximumDurationMilliseconds is < 1 or > 3_600_000)
        {
            throw Invalid();
        }

        int checkCount = (specification.ExpectedStatus.HasValue ? 1 : 0)
            + specification.OutputContains.Count
            + specification.OutputExcludes.Count
            + specification.RequiredEventKinds.Count
            + (specification.MaximumToolCalls.HasValue ? 1 : 0)
            + (specification.MaximumDurationMilliseconds.HasValue ? 1 : 0);
        if (checkCount == 0)
        {
            throw Invalid();
        }
    }

    private static void ValidateRules(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count > MaximumRulesPerGroup)
        {
            throw Invalid();
        }

        foreach (string value in values)
        {
            if (string.IsNullOrWhiteSpace(value)
                || Encoding.UTF8.GetByteCount(value) > MaximumRuleUtf8Bytes)
            {
                throw Invalid();
            }
        }
    }

    private static RunEvaluationException Invalid() => new(
        RunEvaluationErrorCodes.SpecificationInvalid,
        "The run evaluation specification is invalid.");
}

public sealed class RunEvaluationService(
    IUnifiedEntryRepository repository,
    TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<RunEvaluationReport?> EvaluateAsync(
        Guid runId,
        string tenantId,
        string userId,
        RunEvaluationSpecification specification,
        CancellationToken cancellationToken = default)
    {
        Validate(runId, tenantId, userId, specification);

        UnifiedRunDetails? details = await repository.GetDetailsForOwnerAsync(
            runId,
            tenantId,
            userId,
            cancellationToken);
        if (details is null)
        {
            return null;
        }

        IReadOnlyList<UnifiedRunEventRecord> events =
            specification.RequiredEventKinds.Count == 0
                ? Array.Empty<UnifiedRunEventRecord>()
                : await repository.ListEventsForOwnerAsync(
                    runId,
                    tenantId,
                    userId,
                    cancellationToken);

        var checks = new List<RunEvaluationCheck>();
        UnifiedEntryRunRecord run = details.EntryRun;
        if (specification.ExpectedStatus.HasValue)
        {
            Add(
                checks,
                "status",
                run.Status == specification.ExpectedStatus.Value,
                specification.ExpectedStatus.Value.ToString(),
                run.Status.ToString());
        }

        foreach (string expected in specification.OutputContains)
        {
            bool found = run.Output.Contains(expected, StringComparison.OrdinalIgnoreCase);
            Add(checks, "output-contains", found, "present", found ? "present" : "absent");
        }

        foreach (string forbidden in specification.OutputExcludes)
        {
            bool found = run.Output.Contains(forbidden, StringComparison.OrdinalIgnoreCase);
            Add(checks, "output-excludes", !found, "absent", found ? "present" : "absent");
        }

        foreach (string eventKind in specification.RequiredEventKinds)
        {
            bool found = events.Any(value => string.Equals(
                value.Kind,
                eventKind,
                StringComparison.OrdinalIgnoreCase));
            Add(checks, "event-kind", found, "present", found ? "present" : "absent");
        }

        if (specification.MaximumToolCalls.HasValue)
        {
            Add(
                checks,
                "tool-call-count",
                details.ToolCalls.Count <= specification.MaximumToolCalls.Value,
                $"<= {specification.MaximumToolCalls.Value}",
                details.ToolCalls.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (specification.MaximumDurationMilliseconds.HasValue)
        {
            long? duration = run.Duration is null
                ? null
                : checked((long)Math.Ceiling(run.Duration.Value.TotalMilliseconds));
            Add(
                checks,
                "duration",
                duration.HasValue && duration.Value <= specification.MaximumDurationMilliseconds.Value,
                $"<= {specification.MaximumDurationMilliseconds.Value} ms",
                duration.HasValue ? $"{duration.Value} ms" : "unavailable");
        }

        int passed = checks.Count(value => value.Passed);
        decimal score = decimal.Round(
            passed / (decimal)checks.Count,
            4,
            MidpointRounding.AwayFromZero);
        return new RunEvaluationReport(
            runId,
            _timeProvider.GetUtcNow().ToUniversalTime(),
            passed == checks.Count,
            score,
            run.OutputSha256,
            Encoding.UTF8.GetByteCount(run.Output),
            new ReadOnlyCollection<RunEvaluationCheck>(checks));
    }

    private static void Validate(
        Guid runId,
        string tenantId,
        string userId,
        RunEvaluationSpecification specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        if (runId == Guid.Empty || string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(userId))
        {
            throw Invalid();
        }

        RunEvaluationSpecificationValidator.Validate(specification);
    }

    private static RunEvaluationException Invalid() => new(
        RunEvaluationErrorCodes.SpecificationInvalid,
        "The run evaluation specification is invalid.");

    private static void Add(
        ICollection<RunEvaluationCheck> checks,
        string code,
        bool passed,
        string expected,
        string actual) =>
        checks.Add(new RunEvaluationCheck(code, passed, expected, actual));
}
