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
    #region 转换（ToModelSummary）
    /// <summary>
    /// 转换（ToModelSummary）
    /// </summary>
    /// <returns>仅包含成功状态、查询标识、目录版本、行数及截断标志的模型可见 JSON 摘要。</returns>
    public string ToModelSummary() => JsonSerializer.Serialize(new
    {
        status = "succeeded",
        queryId = QueryId,
        catalogRevision = CatalogRevision,
        rowCount = RowCount,
        truncated = Truncated
    });
    #endregion

    #region 转换（ToPersistedContent）
    /// <summary>
    /// 转换（ToPersistedContent）
    /// </summary>
    /// <returns>包含查询标识、凭据、展示数据和完整性摘要的持久化 JSON 文本。</returns>
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
    #endregion

    #region 解析并校验业务查询权威结果（TryParse）
    /// <summary>
    /// 解析并校验业务查询权威结果（TryParse）。
    /// </summary>
    /// <param name="content">业务查询工具返回的 JSON 正文。</param>
    /// <param name="policy">用于校验目录版本、目录摘要和工具结构摘要的业务查询策略。</param>
    /// <param name="result">成功时输出经校验的权威结果，失败时为 null。</param>
    /// <returns>成功解析且回执、策略摘要、展示行数及载荷大小等校验全部通过时返回 true；结构、格式或一致性校验失败时返回 false。</returns>
    public static bool TryParse(string content, BusinessQueryToolPolicy policy, out BusinessQueryAuthoritativeResult? result)
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
    #endregion

    #region 读取业务查询失败错误码（TryReadFailureCode）
    /// <summary>
    /// 读取业务查询失败错误码（TryReadFailureCode）。
    /// </summary>
    /// <param name="content">业务查询工具返回的 JSON 正文。</param>
    /// <param name="errorCode">成功时输出通过格式校验的错误码；失败时为空字符串。</param>
    /// <returns>响应标记为失败且包含符合 BUSINESS_QUERY_ 错误码格式的字符串时返回 true；解析或校验失败时返回 false。</returns>
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
    #endregion

    #region 忽略大小写查找 JSON 属性（TryProperty）
    /// <summary>
    /// 忽略大小写查找 JSON 属性（TryProperty）。
    /// </summary>
    /// <param name="value">待查找属性的 JSON 值。</param>
    /// <param name="name">需要匹配的属性名称，比较时忽略大小写。</param>
    /// <param name="property">找到时输出第一个匹配属性的值；未找到时为默认 JsonElement。</param>
    /// <returns>输入为 JSON 对象且找到同名属性时返回 true；输入不是对象或没有匹配属性时返回 false。</returns>
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
    #endregion

    #region 处理（Property）
    /// <summary>
    /// 处理（Property）
    /// </summary>
    /// <param name="value">需要读取命名属性的 JSON 对象。</param>
    /// <param name="name">对象或字段名称。</param>
    /// <returns>指定名称的 JSON 属性；不存在或无法读取时抛出 JsonException。</returns>
    private static JsonElement Property(JsonElement value, string name) =>
        TryProperty(value, name, out JsonElement property)
            ? property
            : throw new JsonException();
    #endregion

    #region 处理（String）
    /// <summary>
    /// 处理（String）
    /// </summary>
    /// <param name="value">需要读取字符串属性的 JSON 对象。</param>
    /// <param name="name">对象或字段名称。</param>
    /// <returns>指定 JSON 属性的字符串值；JSON null 转为空字符串，属性缺失或类型不符会抛出异常。</returns>
    private static string String(JsonElement value, string name) =>
        Property(value, name).GetString() ?? string.Empty;
    #endregion

    #region 读取 JSON 布尔属性（Boolean）
    /// <summary>
    /// 读取 JSON 布尔属性（Boolean）。
    /// </summary>
    /// <param name="value">包含目标属性的 JSON 对象。</param>
    /// <param name="name">需要读取的属性名称，比较时忽略大小写。</param>
    /// <returns>指定属性实际保存的布尔值；缺少属性或属性不是布尔类型时抛出异常，而不是返回 false。</returns>
    /// <exception cref="JsonException">未找到指定属性。</exception>
    /// <exception cref="InvalidOperationException">属性值不是 JSON 布尔类型。</exception>
    private static bool Boolean(JsonElement value, string name) =>
        Property(value, name).GetBoolean();
    #endregion

    #region 处理（Int32）
    /// <summary>
    /// 处理（Int32）
    /// </summary>
    /// <param name="value">需要读取 32 位整数属性的 JSON 对象。</param>
    /// <param name="name">对象或字段名称。</param>
    /// <returns>指定 JSON 属性的 32 位整数值；属性缺失、类型或范围不符会抛出异常。</returns>
    private static int Int32(JsonElement value, string name) =>
        Property(value, name).GetInt32();
    #endregion

    #region 处理（Int64）
    /// <summary>
    /// 处理（Int64）
    /// </summary>
    /// <param name="value">需要读取 64 位整数属性的 JSON 对象。</param>
    /// <param name="name">对象或字段名称。</param>
    /// <returns>指定 JSON 属性的 64 位整数值；属性缺失、类型或范围不符会抛出异常。</returns>
    private static long Int64(JsonElement value, string name) =>
        Property(value, name).GetInt64();
    #endregion

    #region 处理（GuidValue）
    /// <summary>
    /// 处理（GuidValue）
    /// </summary>
    /// <param name="value">需要读取 GUID 属性的 JSON 对象。</param>
    /// <param name="name">对象或字段名称。</param>
    /// <returns>指定 JSON 属性的 GUID 值；属性缺失、类型或格式不符会抛出异常。</returns>
    private static Guid GuidValue(JsonElement value, string name) =>
        Property(value, name).GetGuid();
    #endregion

    #region 检查是否存在（HashPattern）
    /// <summary>
    /// 检查是否存在（HashPattern）
    /// </summary>
    /// <returns>用于校验业务查询完整性摘要格式的正则表达式。</returns>
    [GeneratedRegex("^[a-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex HashPattern();
    #endregion

    #region 处理（BusinessErrorPattern）
    /// <summary>
    /// 处理（BusinessErrorPattern）
    /// </summary>
    /// <returns>用于识别业务查询错误码格式的正则表达式。</returns>
    [GeneratedRegex("^BUSINESS_QUERY_[A-Z0-9_]{1,96}$", RegexOptions.CultureInvariant)]
    private static partial Regex BusinessErrorPattern();
    #endregion
}

/// <summary>
/// 限制并校验业务查询场景中的 MCP 工具调用。
/// </summary>
/// <param name="inner">业务查询专属检查通过后继续调用的 MCP 调用守卫。</param>
public sealed class BusinessQueryMcpCallGuard(
    IAgentMcpCallGuard inner) : IAgentMcpCallGuard
{
    private int _attempts;

    #region 处理（ReserveAsync）
    /// <summary>
    /// 处理（ReserveAsync）
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>首次调用时的内层预算预留结果；后续调用返回业务查询次数超限的拒绝结果。</returns>
    public ValueTask<AgentMcpCallGuardResult> ReserveAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Increment(ref _attempts) != 1)
        {
            return ValueTask.FromResult(AgentMcpCallGuardResult.Deny(
                UnifiedEntryErrorCodes.BusinessQueryCallLimitExceeded,
                "The controlled business query Agent may call its tool only once."));
        }

        return inner.ReserveAsync(cancellationToken);
    }
    #endregion
}

/// <summary>
/// 集中定义业务查询 MCP 工具的调用限制。
/// </summary>
public static class BusinessQueryMcpToolCallLimits
{
    #region 创建（Create）
    /// <summary>
    /// 创建（Create）
    /// </summary>
    /// <param name="policy">受控业务查询工具策略。</param>
    /// <param name="tools">工具集合。</param>
    /// <returns>与业务查询策略匹配的工具单次调用限制集合；未配置策略时为空集合。</returns>
    public static IReadOnlyList<AgentMcpToolCallLimit> Create(BusinessQueryToolPolicy? policy, IReadOnlyList<PublishedMcpToolReference> tools) =>
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
    #endregion
}

/// <summary>
/// 清理业务查询结果中不应持久化的敏感内容。
/// </summary>
public static class BusinessQueryResultRedaction
{
    #region 创建（CreateContent）
    /// <summary>
    /// 创建（CreateContent）
    /// </summary>
    /// <param name="queryId">业务查询标识。</param>
    /// <param name="receiptJson">业务查询回执的 JSON 内容。</param>
    /// <param name="integritySha256">查询回执和展示内容的完整性 SHA-256 摘要。</param>
    /// <returns>保留查询凭据和完整性摘要、标记展示内容已脱敏的 JSON 文本。</returns>
    public static string CreateContent(Guid queryId, string receiptJson, string integritySha256)
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
    #endregion

    #region 处理（Redact）
    /// <summary>
    /// 处理（Redact）
    /// </summary>
    /// <param name="value">本次操作使用的会话消息记录。</param>
    /// <returns>移除业务查询展示内容并更新内容字节数的消息副本；其他消息仅复制记录。</returns>
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
    #endregion

    #region 处理（RedactedPayload）
    /// <summary>
    /// 处理（RedactedPayload）
    /// </summary>
    /// <param name="queryId">业务查询标识。</param>
    /// <param name="integritySha256">查询回执和展示内容的完整性 SHA-256 摘要。</param>
    /// <returns>仅包含结果类型、查询标识、展示脱敏标志及完整性摘要的 JSON 载荷。</returns>
    public static string RedactedPayload(Guid queryId, string integritySha256) =>
        JsonSerializer.Serialize(new
        {
            kind = "business-query-result",
            queryId,
            presentationRedacted = true,
            integritySha256
        });
    #endregion
}
