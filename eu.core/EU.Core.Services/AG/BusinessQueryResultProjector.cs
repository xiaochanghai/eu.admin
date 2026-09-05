#nullable enable

using EU.Core.IServices.UnifiedEntry;

using System.Text.Json;
using System.Text.Json.Nodes;

namespace EU.Core.Services;

#region 文件职责：BusinessQueryResultProjector 职责实现

/// <summary>
/// 将业务查询结果投影为统一入口可持久化的结构。
/// </summary>
public static class BusinessQueryResultProjector
{
    #region 结果投影与脱敏

    public static IReadOnlyList<ConversationMessageRecord> ProjectMessages(IReadOnlyList<ConversationMessageRecord> values, bool includePresentation) =>
        includePresentation
            ? UnifiedEntryContractCloner.ReadOnly(values.Select(value => value with { }))
            : UnifiedEntryContractCloner.ReadOnly(values.Select(value =>
                value.Kind == ConversationMessageKind.BusinessQueryResult
                    ? BusinessQueryResultRedaction.Redact(value)
                    : value with { }));

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

    public static IReadOnlyList<UnifiedRunEventRecord> ProjectEvents(IReadOnlyList<UnifiedRunEventRecord> values, bool includePresentation) =>
        includePresentation
            ? UnifiedEntryContractCloner.ReadOnly(values.Select(value => value with { }))
            : UnifiedEntryContractCloner.ReadOnly(values.Select(value => value with
            {
                PayloadJson = ProjectPayload(value.PayloadJson)
            }));

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
}

#endregion
