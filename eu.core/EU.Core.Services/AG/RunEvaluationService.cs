using System.Collections.ObjectModel;
using System.Text;
using EU.Core.IServices.Evaluation;
using EU.Core.IServices.UnifiedEntry;

#nullable enable

namespace EU.Core.Services;

public sealed class RunEvaluationService(
    IUnifiedEntryRepository repository,
    TimeProvider? timeProvider = null) : IRunEvaluationService
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
