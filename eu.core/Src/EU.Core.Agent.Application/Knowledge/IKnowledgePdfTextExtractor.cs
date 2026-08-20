using EU.Core.Model;

namespace EU.Core.Agent.Application.Knowledge;

public interface IKnowledgePdfTextExtractor
{
    Task<KnowledgePdfExtractionResult> ExtractAsync(
        ReadOnlyMemory<byte> content,
        int maximumPages,
        int maximumCharacters,
        CancellationToken cancellationToken = default);
}
