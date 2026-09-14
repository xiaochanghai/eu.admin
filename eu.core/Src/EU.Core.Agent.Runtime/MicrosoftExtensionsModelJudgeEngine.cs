using System.ClientModel;
using EU.Core.IServices.Evaluation;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using OpenAI;

namespace EU.Core.Agent.Runtime;

public sealed class MicrosoftExtensionsModelJudgeEngine(
    AgentRuntimeOptions options,
    IModelCredentialResolver credentials,
    IAgentModelProfileResolver? modelProfiles = null) : IModelJudgeEngine
{
    public async Task<IReadOnlyList<ModelJudgeEngineMetric>> EvaluateAsync(
        string input,
        string output,
        string modelProfileId,
        IReadOnlyList<string> evaluators,
        CancellationToken cancellationToken = default)
    {
        var profile = modelProfiles is null ? null : await modelProfiles.ResolveAsync(modelProfileId, cancellationToken)
            ?? throw new InvalidOperationException("模型配置解析失败，禁止回退到旧凭据。");
        string? apiKey = profile is null
            ? await credentials.ResolveAsync(options.ModelCredentialAlias, cancellationToken)
            : profile.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "The configured model credential alias could not be resolved.");
        }

        var client = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = profile?.Endpoint ?? options.ModelEndpoint });
        using IChatClient chatClient = client
            .GetChatClient(profile?.ModelName ?? modelProfileId)
            .AsIChatClient();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(profile?.Timeout ?? options.ModelTimeout);
        var configuration = new ChatConfiguration(chatClient);
        var results = new List<ModelJudgeEngineMetric>(evaluators.Count);
        foreach (string name in evaluators)
        {
            IEvaluator evaluator = name switch
            {
                ModelJudgeEvaluators.Relevance => new RelevanceEvaluator(),
                ModelJudgeEvaluators.Coherence => new CoherenceEvaluator(),
                _ => throw new InvalidOperationException("The model evaluator is not supported.")
            };
            EvaluationResult evaluation = await evaluator.EvaluateAsync(
                input,
                output,
                configuration,
                additionalContext: null,
                timeout.Token);
            NumericMetric metric = evaluation.Get<NumericMetric>(name);
            results.Add(new ModelJudgeEngineMetric(
                name,
                metric.Value.HasValue
                    ? decimal.Round((decimal)metric.Value.Value, 4)
                    : null,
                metric.Value.HasValue ? [] : ["metric-value-missing"]));
        }

        return results;
    }
}
