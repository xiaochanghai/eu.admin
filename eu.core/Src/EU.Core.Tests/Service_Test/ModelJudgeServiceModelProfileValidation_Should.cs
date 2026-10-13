#nullable enable

using System.Reflection;
using EU.Core.IServices;
using EU.Core.IServices.Evaluation;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.Services;
using Xunit;

namespace EU.Core.Tests.Service_Test;

/// <summary>验证模型裁判仅接受标准模型配置服务返回的精确模型标识。</summary>
public sealed class ModelJudgeServiceModelProfileValidation_Should
{
    [Fact]
    public async Task Reject_case_insensitive_model_profile_match()
    {
        Guid batchId = Guid.NewGuid();
        var batch = new EvaluationBatchRecord(
            batchId, "tenant", "operator", Guid.NewGuid(), Guid.NewGuid(), "suite-sha256",
            EvaluationBatchStatus.Completed, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            [new EvaluationCaseExecutionRecord(
                Guid.NewGuid(), "case", Guid.NewGuid(), Guid.NewGuid(),
                EvaluationCaseExecutionStatus.Passed, null, null, null, string.Empty)],
            string.Empty);
        IEvaluationBatchRepository batches = Proxy<IEvaluationBatchRepository>((method, _) =>
            method.Name == nameof(IEvaluationBatchRepository.GetAsync)
                ? Task.FromResult<EvaluationBatchRecord?>(batch)
                : throw new InvalidOperationException(method.Name));
        IAgEvaluationSuiteServices suites = Proxy<IAgEvaluationSuiteServices>((method, _) =>
            method.Name == nameof(IAgEvaluationSuiteServices.GetAsync)
                ? Task.FromResult<EvaluationSuiteDefinition?>(null)
                : throw new InvalidOperationException(method.Name));
        IAgModelConfigServices modelConfigs = Proxy<IAgModelConfigServices>((method, _) =>
            method.Name == nameof(IAgModelConfigServices.ListAvailableProfilesAsync)
                ? Task.FromResult<IReadOnlyList<string>>(["available-model"])
                : throw new InvalidOperationException(method.Name));
        var service = new ModelJudgeService(
            batches, suites, null!, null!, modelConfigs, null!,
            new ModelJudgePolicy(true, 1, TimeSpan.FromSeconds(30)));

        ServiceResult<ModelJudgeReport> result = await service.EvaluateAsync(
            batchId,
            "tenant",
            "operator",
            new ModelJudgeSpecification(
                true,
                "AVAILABLE-MODEL",
                [ModelJudgeEvaluators.Relevance],
                new Dictionary<string, decimal> { [ModelJudgeEvaluators.Relevance] = 1m }));

        Assert.False(result.Success);
        Assert.Equal(ModelJudgeServiceStatusCodes.ModelUnavailable, result.Status);
        Assert.Equal("The selected model profile is not available.", result.Message);
    }

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
    {
        var proxy = DispatchProxy.Create<T, DelegateProxy>();
        ((DelegateProxy)(object)proxy).Handler = handler;
        return proxy;
    }

    private class DelegateProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler(targetMethod ?? throw new InvalidOperationException(), args);
    }
}
