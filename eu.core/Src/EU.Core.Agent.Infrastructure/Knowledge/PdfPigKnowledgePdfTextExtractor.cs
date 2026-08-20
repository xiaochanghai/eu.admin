using System.Text;
using EU.Core.Agent.Application.Knowledge;
using EU.Core.Model;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Exceptions;

namespace EU.Core.Agent.Infrastructure.Knowledge;

public sealed class PdfPigKnowledgePdfTextExtractor : IKnowledgePdfTextExtractor
{
    public Task<KnowledgePdfExtractionResult> ExtractAsync(
        ReadOnlyMemory<byte> content,
        int maximumPages,
        int maximumCharacters,
        CancellationToken cancellationToken = default)
    {
        if (content.IsEmpty || maximumPages < 1 || maximumCharacters < 1)
        {
            return Task.FromResult(KnowledgePdfExtractionResult.Failed(
                KnowledgePdfExtractionFailure.Invalid));
        }

        return Task.Run(
            () => Extract(content, maximumPages, maximumCharacters, cancellationToken),
            cancellationToken);
    }

    private static KnowledgePdfExtractionResult Extract(
        ReadOnlyMemory<byte> content,
        int maximumPages,
        int maximumCharacters,
        CancellationToken cancellationToken)
    {
        try
        {
            using PdfDocument document = PdfDocument.Open(content);
            if (document.IsEncrypted)
            {
                return KnowledgePdfExtractionResult.Failed(
                    KnowledgePdfExtractionFailure.Encrypted);
            }

            if (document.NumberOfPages is < 1 || document.NumberOfPages > maximumPages)
            {
                return KnowledgePdfExtractionResult.Failed(
                    KnowledgePdfExtractionFailure.PageLimitExceeded);
            }

            var builder = new StringBuilder();
            for (int pageNumber = 1; pageNumber <= document.NumberOfPages; pageNumber++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string pageText = ContentOrderTextExtractor.GetText(
                    document.GetPage(pageNumber),
                    addDoubleNewline: true)
                    .Replace("\r\n", "\n", StringComparison.Ordinal)
                    .Replace('\r', '\n')
                    .Trim();
                if (pageText.Length == 0)
                {
                    continue;
                }

                string prefix = builder.Length == 0
                    ? $"[Page {pageNumber}]\n"
                    : $"\n\n[Page {pageNumber}]\n";
                if (builder.Length + prefix.Length + pageText.Length > maximumCharacters)
                {
                    return KnowledgePdfExtractionResult.Failed(
                        KnowledgePdfExtractionFailure.TextLimitExceeded);
                }

                builder.Append(prefix);
                builder.Append(pageText);
            }

            return builder.Length == 0
                ? KnowledgePdfExtractionResult.Failed(
                    KnowledgePdfExtractionFailure.NoExtractableText)
                : KnowledgePdfExtractionResult.Success(
                    builder.ToString(),
                    document.NumberOfPages);
        }
        catch (PdfDocumentEncryptedException)
        {
            return KnowledgePdfExtractionResult.Failed(
                KnowledgePdfExtractionFailure.Encrypted);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return KnowledgePdfExtractionResult.Failed(
                KnowledgePdfExtractionFailure.Invalid);
        }
    }
}
