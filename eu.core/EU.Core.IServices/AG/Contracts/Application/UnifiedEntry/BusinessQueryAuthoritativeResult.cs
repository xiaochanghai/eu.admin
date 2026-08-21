#nullable enable

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.Runtime;

namespace EU.Core.IServices.UnifiedEntry;

public sealed record BusinessQueryResultLimits(
    int MaximumResultBytes,
    int MaximumConversationBytes)
{
    public static BusinessQueryResultLimits Default { get; } =
        new(1_048_576, 10_485_760);
}

public sealed partial record BusinessQueryAuthoritativeResult(
    Guid QueryId,
    long CatalogRevision,
    string CatalogHash,
    string ToolSchemaHash,
    string QueryPlanHash,
    Guid PolicyDecisionId,
    int RowCount,
    bool Truncated,
    string TerminalStatus,
    string FormatterVersion,
    string ReceiptJson,
    string PresentationJson,
    string IntegritySha256)
{
    public string ToModelSummary() => JsonSerializer.Serialize(new
    {
        status = "succeeded",
        queryId = QueryId,
        catalogRevision = CatalogRevision,
        rowCount = RowCount,
        truncated = Truncated
    });

    public string ToPersistedContent()
    {
        using JsonDocument receipt = JsonDocument.Parse(ReceiptJson);
        using JsonDocument presentation = JsonDocument.Parse(PresentationJson);
        return JsonSerializer.Serialize(new
        {
            kind = "business-query-result",
            queryId = QueryId,
            receipt = receipt.RootElement,
            presentation = presentation.RootElement,
            integritySha256 = IntegritySha256
        });
    }

    public static bool TryParse(
        string content,
        BusinessQueryToolPolicy policy,
        out BusinessQueryAuthoritativeResult? result)
    {
        result = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(content, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 32
            });
            JsonElement root = document.RootElement;
            if (!Boolean(root, "succeeded")
                || !TryProperty(root, "receipt", out JsonElement receipt)
                || !TryProperty(root, "presentation", out JsonElement presentation)
                || !TryProperty(root, "result", out JsonElement queryResult)
                || receipt.ValueKind != JsonValueKind.Object
                || presentation.ValueKind != JsonValueKind.Object
                || queryResult.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            Guid queryId = GuidValue(receipt, "queryId");
            long catalogRevision = Int64(receipt, "catalogRevision");
            string catalogHash = String(receipt, "catalogHash");
            string toolSchemaHash = String(receipt, "toolSchemaHash");
            string queryPlanHash = String(receipt, "queryPlanHash");
            Guid policyDecisionId = GuidValue(receipt, "policyDecisionId");
            int rowCount = Int32(receipt, "rowCount");
            bool truncated = Boolean(receipt, "truncated");
            string terminalStatus = String(receipt, "terminalStatus");
            string resultHash = String(receipt, "resultHash");
            string actualResultHash = String(queryResult, "resultSha256");
            JsonElement rows = Property(presentation, "rows");
            string formatterVersion = String(presentation, "formatterVersion");
            if (queryId == Guid.Empty
                || policyDecisionId == Guid.Empty
                || catalogRevision != policy.CatalogRevision
                || !string.Equals(catalogHash, policy.CatalogHash, StringComparison.Ordinal)
                || !string.Equals(toolSchemaHash, policy.ToolSchemaHash, StringComparison.Ordinal)
                || !HashPattern().IsMatch(queryPlanHash)
                || !HashPattern().IsMatch(resultHash)
                || !string.Equals(resultHash, actualResultHash, StringComparison.Ordinal)
                || rowCount < 0
                || truncated
                || !string.Equals(terminalStatus, "succeeded", StringComparison.Ordinal)
                || rows.ValueKind != JsonValueKind.Array
                || rows.GetArrayLength() != rowCount
                || string.IsNullOrWhiteSpace(formatterVersion)
                || formatterVersion.Length > 64)
            {
                return false;
            }

            string receiptJson = receipt.GetRawText();
            string presentationJson = presentation.GetRawText();
            if (Encoding.UTF8.GetByteCount(receiptJson) > 32_768
                || Encoding.UTF8.GetByteCount(presentationJson) > 1_048_576)
            {
                return false;
            }

            string integrity = Convert.ToHexStringLower(SHA256.HashData(
                Encoding.UTF8.GetBytes(receiptJson + "\n" + presentationJson)));
            result = new BusinessQueryAuthoritativeResult(
                queryId,
                catalogRevision,
                catalogHash,
                toolSchemaHash,
                queryPlanHash,
                policyDecisionId,
                rowCount,
                truncated,
                terminalStatus,
                formatterVersion,
                receiptJson,
                presentationJson,
                integrity);
            return true;
        }
        catch (Exception exception) when (exception is JsonException
            or InvalidOperationException
            or FormatException
            or OverflowException)
        {
            return false;
        }
    }

    public static bool TryReadFailureCode(string content, out string errorCode)
    {
        errorCode = string.Empty;
        try
        {
            using JsonDocument document = JsonDocument.Parse(content);
            JsonElement root = document.RootElement;
            if (Boolean(root, "succeeded")
                || !TryProperty(root, "errorCode", out JsonElement error)
                || error.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            string value = error.GetString() ?? string.Empty;
            if (!BusinessErrorPattern().IsMatch(value))
            {
                return false;
            }

            errorCode = value;
            return true;
        }
        catch (Exception exception) when (exception is JsonException
            or InvalidOperationException)
        {
            return false;
        }
    }

    private static bool TryProperty(JsonElement value, string name, out JsonElement property)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty candidate in value.EnumerateObject())
            {
                if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    property = candidate.Value;
                    return true;
                }
            }
        }

        property = default;
        return false;
    }

    private static JsonElement Property(JsonElement value, string name) =>
        TryProperty(value, name, out JsonElement property)
            ? property
            : throw new JsonException();

    private static string String(JsonElement value, string name) =>
        Property(value, name).GetString() ?? string.Empty;

    private static bool Boolean(JsonElement value, string name) =>
        Property(value, name).GetBoolean();

    private static int Int32(JsonElement value, string name) =>
        Property(value, name).GetInt32();

    private static long Int64(JsonElement value, string name) =>
        Property(value, name).GetInt64();

    private static Guid GuidValue(JsonElement value, string name) =>
        Property(value, name).GetGuid();

    [GeneratedRegex("^[a-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex HashPattern();

    [GeneratedRegex("^BUSINESS_QUERY_[A-Z0-9_]{1,96}$", RegexOptions.CultureInvariant)]
    private static partial Regex BusinessErrorPattern();
}

public sealed class BusinessQueryMcpCallGuard(
    IAgentMcpCallGuard inner) : IAgentMcpCallGuard
{
    private int _attempts;

    public ValueTask<AgentMcpCallGuardResult> ReserveAsync(
        CancellationToken cancellationToken = default)
    {
        if (Interlocked.Increment(ref _attempts) != 1)
        {
            return ValueTask.FromResult(AgentMcpCallGuardResult.Deny(
                UnifiedEntryErrorCodes.BusinessQueryCallLimitExceeded,
                "The controlled business query Agent may call its tool only once."));
        }

        return inner.ReserveAsync(cancellationToken);
    }
}

public static class BusinessQueryMcpToolCallLimits
{
    public static IReadOnlyList<AgentMcpToolCallLimit> Create(
        BusinessQueryToolPolicy? policy,
        IReadOnlyList<PublishedMcpToolReference> tools) =>
        policy is null
            ? Array.Empty<AgentMcpToolCallLimit>()
            : tools
                .Where(policy.Matches)
                .Select(tool => new AgentMcpToolCallLimit(
                    tool.ToolVersionId,
                    1,
                    UnifiedEntryErrorCodes.BusinessQueryCallLimitExceeded,
                    "The controlled business query tool may be called only once per run."))
                .ToArray();
}

public static class BusinessQueryResultRedaction
{
    public static string CreateContent(
        Guid queryId,
        string receiptJson,
        string integritySha256)
    {
        using JsonDocument receipt = JsonDocument.Parse(receiptJson);
        return JsonSerializer.Serialize(new
        {
            kind = "business-query-result",
            queryId,
            receipt = receipt.RootElement,
            presentationRedacted = true,
            integritySha256
        });
    }

    public static ConversationMessageRecord Redact(ConversationMessageRecord value)
    {
        if (value.Kind != ConversationMessageKind.BusinessQueryResult
            || value.BusinessQueryId is not Guid queryId)
        {
            return value with { };
        }

        string content = CreateContent(
            queryId,
            value.BusinessQueryReceiptJson,
            value.BusinessQueryIntegritySha256);
        return value with
        {
            Content = content,
            ContentUtf8Bytes = Encoding.UTF8.GetByteCount(content),
            BusinessQueryPresentationJson = string.Empty
        };
    }

    public static string RedactedPayload(Guid queryId, string integritySha256) =>
        JsonSerializer.Serialize(new
        {
            kind = "business-query-result",
            queryId,
            presentationRedacted = true,
            integritySha256
        });
}
