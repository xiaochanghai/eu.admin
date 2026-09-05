using System.Collections.ObjectModel;
using System.Text;
using EU.Core.IServices.Evaluation;
using EU.Core.IServices.UnifiedEntry;

#nullable enable

namespace EU.Core.Services;

// 文件职责：RunEvaluationService 职责实现

/// <summary>
/// 根据确定性规则评测统一入口运行结果。
/// </summary>
/// <param name="repository">用于读取和持久化统一入口会话、运行及事件的仓储。</param>
/// <param name="timeProvider">用于获取当前时间的时间提供器；为 null 时使用系统时间提供器。</param>
public sealed class RunEvaluationService(
    IUnifiedEntryRepository repository,
    TimeProvider? timeProvider = null) : IRunEvaluationService
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    #region 处理（EvaluateAsync）
    /// <summary>
    /// 处理（EvaluateAsync）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="specification">评估规范。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>所属身份匹配的运行规则评测报告；运行不存在或不属于指定租户和用户时为 null。</returns>
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
    #endregion

    #region 校验（Validate）
    /// <summary>
    /// 校验（Validate）
    /// </summary>
    /// <param name="runId">运行记录标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="specification">评估规范。</param>
    private static void Validate(Guid runId, string tenantId, string userId, RunEvaluationSpecification specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        if (runId == Guid.Empty || string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(userId))
        {
            throw Invalid();
        }

        RunEvaluationSpecificationValidator.Validate(specification);
    }
    #endregion

    #region 处理（Invalid）
    /// <summary>
    /// 处理（Invalid）
    /// </summary>
    /// <returns>错误码为 SpecificationInvalid 的运行评测异常。</returns>
    private static RunEvaluationException Invalid() => new(
        RunEvaluationErrorCodes.SpecificationInvalid,
        "The run evaluation specification is invalid.");
    #endregion

    #region 添加（Add）
    /// <summary>
    /// 添加（Add）
    /// </summary>
    /// <param name="checks">评估检查项集合。</param>
    /// <param name="code">对象编码或业务错误码。</param>
    /// <param name="passed">校验或评估是否通过。</param>
    /// <param name="expected">期望匹配的值。</param>
    /// <param name="actual">实际取得的值。</param>
    private static void Add(ICollection<RunEvaluationCheck> checks, string code, bool passed, string expected, string actual) =>
        checks.Add(new RunEvaluationCheck(code, passed, expected, actual));
    #endregion
}
