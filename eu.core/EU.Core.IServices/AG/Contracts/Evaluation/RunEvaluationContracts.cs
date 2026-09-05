#nullable enable

using System.Collections.ObjectModel;
using System.Text;
using EU.Core.IServices.UnifiedEntry;

namespace EU.Core.IServices.Evaluation;

/// <summary>
/// 定义确定性运行评测错误码。
/// </summary>
public static class RunEvaluationErrorCodes
{
    /// <summary>表示 <c>SpecificationInvalid</c> 场景的错误码。</summary>
    public const string SpecificationInvalid = "RUN_EVALUATION_SPECIFICATION_INVALID";
    /// <summary>表示 <c>RunNotFound</c> 场景的错误码。</summary>
    public const string RunNotFound = "RUN_EVALUATION_RUN_NOT_FOUND";
}

/// <summary>
/// 运行结果的确定性评测规则。
/// </summary>
/// <param name="ExpectedStatus">期望的运行终态。</param>
/// <param name="OutputContains">输出必须包含的文本集合。</param>
/// <param name="OutputExcludes">输出不得包含的文本集合。</param>
/// <param name="RequiredEventKinds">运行必须产生的事件类型集合。</param>
/// <param name="MaximumToolCalls">允许的最大工具调用次数。</param>
/// <param name="MaximumDurationMilliseconds">允许的最大运行时长，单位为毫秒。</param>
public sealed record RunEvaluationSpecification(
    UnifiedRunStatus? ExpectedStatus,
    IReadOnlyList<string> OutputContains,
    IReadOnlyList<string> OutputExcludes,
    IReadOnlyList<string> RequiredEventKinds,
    int? MaximumToolCalls,
    long? MaximumDurationMilliseconds);

/// <summary>
/// 单项运行评测检查结果。
/// </summary>
/// <param name="Code">业务唯一编码或检查项编码。</param>
/// <param name="Passed">检查项或评测是否通过。</param>
/// <param name="Expected">检查项的期望值。</param>
/// <param name="Actual">检查项的实际值。</param>
public sealed record RunEvaluationCheck(
    string Code,
    bool Passed,
    string Expected,
    string Actual);

/// <summary>
/// 运行结果的确定性评测报告。
/// </summary>
/// <param name="RunId">运行标识。</param>
/// <param name="EvaluatedAtUtc">评测完成的 UTC 时间。</param>
/// <param name="Passed">检查项或评测是否通过。</param>
/// <param name="Score">模型裁判或综合评测分数。</param>
/// <param name="OutputSha256">输出内容的 SHA-256 摘要。</param>
/// <param name="OutputUtf8Bytes">输出按 UTF-8 编码后的字节数。</param>
/// <param name="Checks">逐项评测检查结果集合。</param>
public sealed record RunEvaluationReport(
    Guid RunId,
    DateTimeOffset EvaluatedAtUtc,
    bool Passed,
    decimal Score,
    string OutputSha256,
    int OutputUtf8Bytes,
    IReadOnlyList<RunEvaluationCheck> Checks);

/// <summary>
/// 定义统一入口运行结果的确定性评测服务。
/// </summary>
public interface IRunEvaluationService
{
    /// <summary>执行运行结果评测。</summary>
    Task<RunEvaluationReport?> EvaluateAsync(
        Guid runId,
        string tenantId,
        string userId,
        RunEvaluationSpecification specification,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 表示运行评测过程中的领域异常。
/// </summary>
public sealed class RunEvaluationException(string errorCode, string message)
    : Exception(message)
{
    /// <summary>
    /// 获取领域异常对应的错误码。
    /// </summary>
    public string ErrorCode { get; } = errorCode;
}

/// <summary>
/// 校验并规范化运行评测规则。
/// </summary>
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
