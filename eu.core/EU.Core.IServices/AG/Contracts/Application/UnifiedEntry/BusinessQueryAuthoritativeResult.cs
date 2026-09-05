#nullable enable

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.Runtime;

namespace EU.Core.IServices.UnifiedEntry;

/// <summary>
/// 业务查询结果的载荷限制。
/// </summary>
/// <param name="MaximumResultBytes">单次业务查询结果允许的最大字节数。</param>
/// <param name="MaximumConversationBytes">会话可保留的业务查询结果最大总字节数。</param>
public sealed record BusinessQueryResultLimits(
    int MaximumResultBytes,
    int MaximumConversationBytes)
{
    /// <summary>
    /// 获取默认的业务查询结果载荷限制。
    /// </summary>
    public static BusinessQueryResultLimits Default { get; } =
        new(1_048_576, 10_485_760);
}

/// <summary>
/// 表示经过校验、可审计和可持久化的业务查询权威结果。
/// </summary>
/// <param name="QueryId">业务查询标识。</param>
/// <param name="CatalogRevision">执行查询时使用的工具目录修订号。</param>
/// <param name="CatalogHash">执行查询时使用的工具目录摘要。</param>
/// <param name="ToolSchemaHash">执行查询时使用的工具输入架构摘要。</param>
/// <param name="QueryPlanHash">查询计划摘要。</param>
/// <param name="PolicyDecisionId">授权策略决策标识。</param>
/// <param name="RowCount">结果行数。</param>
/// <param name="Truncated">结果是否被截断。</param>
/// <param name="TerminalStatus">查询终态。</param>
/// <param name="FormatterVersion">结果格式化器版本。</param>
/// <param name="ReceiptJson">可审计的查询回执 JSON。</param>
/// <param name="PresentationJson">供展示使用的查询结果 JSON。</param>
/// <param name="IntegritySha256">回执和展示结果的完整性摘要。</param>
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

/// <summary>
/// 限制并校验业务查询场景中的 MCP 工具调用。
/// </summary>
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

/// <summary>
/// 集中定义业务查询 MCP 工具的调用限制。
/// </summary>
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

/// <summary>
/// 清理业务查询结果中不应持久化的敏感内容。
/// </summary>
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
