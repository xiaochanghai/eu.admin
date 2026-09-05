#nullable enable

using EU.Core.IServices.UnifiedEntry;

using System.Text.Json;
using System.Text.Json.Nodes;

namespace EU.Core.Services;

// 文件职责：BusinessQueryResultProjector 职责实现

/// <summary>
/// 将业务查询结果投影为统一入口可持久化的结构。
/// </summary>
public static class BusinessQueryResultProjector
{
    #region 结果投影与脱敏

    #region 处理（ProjectMessages）
    /// <summary>
    /// 处理（ProjectMessages）
    /// </summary>
    /// <param name="values">需要按展示权限投影的会话消息集合。</param>
    /// <param name="includePresentation">是否包含用于展示的结果内容。</param>
    /// <returns>消息记录副本集合；未获准展示时移除业务查询展示内容。</returns>
    public static IReadOnlyList<ConversationMessageRecord> ProjectMessages(IReadOnlyList<ConversationMessageRecord> values, bool includePresentation) =>
        includePresentation
            ? UnifiedEntryContractCloner.ReadOnly(values.Select(value => value with { }))
            : UnifiedEntryContractCloner.ReadOnly(values.Select(value =>
                value.Kind == ConversationMessageKind.BusinessQueryResult
                    ? BusinessQueryResultRedaction.Redact(value)
                    : value with { }));
    #endregion

    #region 处理（ProjectDetails）
    /// <summary>
    /// 处理（ProjectDetails）
    /// </summary>
    /// <param name="value">本次操作使用的统一入口运行详情。</param>
    /// <param name="includePresentation">是否包含用于展示的结果内容。</param>
    /// <returns>运行详情副本；未获准展示时脱敏工具结果中的业务查询展示内容。</returns>
    public static UnifiedRunDetails ProjectDetails(UnifiedRunDetails value, bool includePresentation) =>
        includePresentation
            ? UnifiedEntryContractCloner.Clone(value)
            : new UnifiedRunDetails(
                value.EntryRun,
                value.AgentRuns,
                value.Orchestrations,
                value.ToolCalls.Select(call => call with
                {
                    ResultContent = ProjectPayload(call.ResultContent)
                }).ToArray());
    #endregion

    #region 处理（ProjectEvents）
    /// <summary>
    /// 处理（ProjectEvents）
    /// </summary>
    /// <param name="values">需要按展示权限投影的运行事件集合。</param>
    /// <param name="includePresentation">是否包含用于展示的结果内容。</param>
    /// <returns>事件记录副本集合；未获准展示时脱敏事件载荷中的业务查询展示内容。</returns>
    public static IReadOnlyList<UnifiedRunEventRecord> ProjectEvents(IReadOnlyList<UnifiedRunEventRecord> values, bool includePresentation) =>
        includePresentation
            ? UnifiedEntryContractCloner.ReadOnly(values.Select(value => value with { }))
            : UnifiedEntryContractCloner.ReadOnly(values.Select(value => value with
            {
                PayloadJson = ProjectPayload(value.PayloadJson)
            }));
    #endregion

    #region 处理（ProjectPayload）
    /// <summary>
    /// 处理（ProjectPayload）
    /// </summary>
    /// <param name="payload">待按展示权限检查和脱敏的事件或工具结果载荷。</param>
    /// <returns>移除业务查询展示内容后的载荷；无需处理时原样返回，疑似展示载荷解析失败时返回脱敏占位文本。</returns>
    public static string ProjectPayload(string payload)
    {
        if (string.IsNullOrEmpty(payload)
            || !payload.Contains("presentation", StringComparison.OrdinalIgnoreCase))
        {
            return payload;
        }

        try
        {
            JsonNode? node = JsonNode.Parse(payload, documentOptions: new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 64
            });
            bool changed = RedactNode(node);
            return changed ? node!.ToJsonString() : payload;
        }
        catch (JsonException)
        {
            return "[BUSINESS_QUERY_PRESENTATION_REDACTED]";
        }
    }
    #endregion

    #region 递归移除业务查询展示载荷（RedactNode）
    /// <summary>
    /// 递归移除业务查询展示载荷（RedactNode）。
    /// </summary>
    /// <param name="node">待原地脱敏的 JSON 节点；为 null 时不作修改。</param>
    /// <returns>当前 JSON 节点或其后代内容发生脱敏修改时返回 true；未修改时返回 false。</returns>
    private static bool RedactNode(JsonNode? node)
    {
        bool changed = false;
        if (node is JsonObject value)
        {
            string? receiptName = value.Select(item => item.Key)
                .FirstOrDefault(name => string.Equals(
                    name, "receipt", StringComparison.OrdinalIgnoreCase));
            string? presentationName = value.Select(item => item.Key)
                .FirstOrDefault(name => string.Equals(
                    name, "presentation", StringComparison.OrdinalIgnoreCase));
            if (receiptName is not null && presentationName is not null)
            {
                value.Remove(presentationName);
                string? resultName = value.Select(item => item.Key)
                    .FirstOrDefault(name => string.Equals(
                        name, "result", StringComparison.OrdinalIgnoreCase));
                if (resultName is not null)
                {
                    value.Remove(resultName);
                }

                value["presentationRedacted"] = true;
                changed = true;
            }

            foreach ((string key, JsonNode? child) in value.ToArray())
            {
                if (child is JsonValue scalar
                    && scalar.TryGetValue(out string? text)
                    && text is not null
                    && text.Contains("presentation", StringComparison.OrdinalIgnoreCase))
                {
                    string projected = ProjectPayload(text);
                    if (!string.Equals(projected, text, StringComparison.Ordinal))
                    {
                        value[key] = projected;
                        changed = true;
                    }
                }
                else
                {
                    changed |= RedactNode(child);
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (JsonNode? child in array)
            {
                changed |= RedactNode(child);
            }
        }

        return changed;
    }
    #endregion

    #endregion
}
