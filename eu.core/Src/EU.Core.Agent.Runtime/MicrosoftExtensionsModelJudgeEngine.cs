using System.ClientModel;
using EU.Core.IServices.Evaluation;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using OpenAI;

namespace EU.Core.Agent.Runtime;

public sealed class MicrosoftExtensionsModelJudgeEngine(
    AgentRuntimeOptions options,
    IModelCredentialResolver credentials) : IModelJudgeEngine
{
    public async Task<IReadOnlyList<ModelJudgeEngineMetric>> EvaluateAsync(
        string input,
        string output,
        string modelProfileId,
        IReadOnlyList<string> evaluators,
        CancellationToken cancellationToken = default)
    {
        string? apiKey = await credentials.ResolveAsync(
            options.ModelCredentialAlias, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "The configured model credential alias could not be resolved.");
        }

        var client = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = options.ModelEndpoint });
        using IChatClient chatClient = client
            .GetChatClient(modelProfileId)
            .AsIChatClient();
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
                cancellationToken);
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
