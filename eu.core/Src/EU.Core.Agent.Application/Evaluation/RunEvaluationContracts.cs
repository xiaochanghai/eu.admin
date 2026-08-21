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
