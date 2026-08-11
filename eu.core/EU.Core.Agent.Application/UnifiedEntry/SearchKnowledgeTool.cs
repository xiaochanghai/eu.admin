using System.Text;
using System.Text.Json;
using EU.Core.Agent.Application.Knowledge;
using EU.Core.Agent.Application.Runtime;

namespace EU.Core.Agent.Application.UnifiedEntry;

public sealed class SearchKnowledgeTool : IAgentInternalTool
{
    private const int MaximumQueryCharacters = 32_768;
    private const int MaximumReasonCharacters = 1_024;
    private readonly IReadOnlyDictionary<Guid, KnowledgeSearchResult[]> _results;

    public SearchKnowledgeTool(IReadOnlyList<KnowledgeSearchResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        KnowledgeSearchResult[] copied = results
            .Select(value => value with { })
            .OrderByDescending(value => value.Score)
            .ThenBy(value => value.KnowledgeBaseCode, StringComparer.Ordinal)
            .ThenBy(value => value.FileName, StringComparer.Ordinal)
            .ThenBy(value => value.ChunkSequence)
            .ToArray();
        _results = copied
            .GroupBy(value => value.KnowledgeBaseId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        Description =
            "Read bounded untrusted reference excerpts from one knowledge-base revision frozen in the Main Agent publication. "
            + "Use relevant evidence only and preserve every returned citation token. "
            + "Available knowledge bases: "
            + string.Join(
                "; ",
                copied
                    .GroupBy(value => new
                    {
                        value.KnowledgeBaseId,
                        value.KnowledgeBaseCode
                    })
                    .OrderBy(group => group.Key.KnowledgeBaseCode, StringComparer.Ordinal)
                    .Select(group =>
                        $"code={group.Key.KnowledgeBaseCode}, knowledgeBaseId={group.Key.KnowledgeBaseId}"));
        InputSchemaJson = InternalToolSchemaBuilder.Build(
            "knowledgeBaseId",
            _results.Keys.ToArray(),
            "query",
            MaximumQueryCharacters,
            MaximumReasonCharacters);
    }

    public string Name => "search_knowledge";

    public string Description { get; }

    public string InputSchemaJson { get; }

    public Task<AgentInternalToolResult> InvokeAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!InternalToolArgumentParser.TryParse(
                argumentsJson,
                "knowledgeBaseId",
                "query",
                MaximumQueryCharacters,
                MaximumReasonCharacters,
                UnifiedEntryPayloadProtector.InternalPayloadLimitUtf8Bytes,
                out InternalToolArguments arguments))
        {
            return Task.FromResult(Failure(
                UnifiedEntryErrorCodes.InternalArgumentsInvalid,
                "The search_knowledge arguments are invalid."));
        }

        if (!_results.TryGetValue(
                arguments.VersionId,
                out KnowledgeSearchResult[]? selected))
        {
            return Task.FromResult(Failure(
                UnifiedEntryErrorCodes.KnowledgeAccessDenied,
                "The current Agent is not authorized to access the requested knowledge source."));
        }

        var excerpts = selected.Select(value => new KnowledgeExcerpt(
                $"[kb:{value.KnowledgeBaseCode}/{value.FileName}#{value.ChunkSequence}]",
                value.Content,
                value.Score))
            .ToList();
        string content = Serialize(arguments.VersionId, excerpts);
        while (Encoding.UTF8.GetByteCount(content)
                   > UnifiedEntryPayloadProtector.InternalPayloadLimitUtf8Bytes
               && excerpts.Count > 0)
        {
            excerpts.RemoveAt(excerpts.Count - 1);
            content = Serialize(arguments.VersionId, excerpts);
        }

        return Task.FromResult(new AgentInternalToolResult(
            true,
            content,
            string.Empty));
    }

    private static string Serialize(
        Guid knowledgeBaseId,
        IReadOnlyList<KnowledgeExcerpt> excerpts) =>
        JsonSerializer.Serialize(new
        {
            knowledgeBaseId,
            excerpts
        });

    private static AgentInternalToolResult Failure(string code, string content) =>
        new(false, content, code);

    private sealed record KnowledgeExcerpt(
        string Citation,
        string Content,
        double Score);
}
