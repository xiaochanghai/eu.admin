#nullable enable

using System.Buffers;
using System.Text;
using System.Text.Json;

namespace EU.Core.IServices.Orchestration;

/// <summary>
/// 对编排执行载荷执行截断和脱敏处理。
/// </summary>
public static class ExecutionPayloadRedactor
{
    private static readonly string[] SensitiveTerms =
    [
        "authorization",
        "apikey",
        "password",
        "secret",
        "token",
        "credential",
        "connectionstring"
    ];

    #region 处理（RedactJson）
    /// <summary>
    /// 处理（RedactJson）
    /// </summary>
    /// <param name="value">需要按敏感字段规则脱敏的 JSON 或普通文本。</param>
    /// <returns>敏感字段脱敏后的 JSON；非 JSON 文本包含敏感词时返回脱敏占位文本，否则原样保留。</returns>
    public static string RedactJson(string? value)
    {
        string source = value ?? string.Empty;
        if (source.Length == 0)
        {
            return source;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(source);
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                Write(document.RootElement, writer, sensitive: false);
            }

            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }
        catch (JsonException)
        {
            string normalized = Normalize(source);
            return SensitiveTerms.Any(term => normalized.Contains(term, StringComparison.Ordinal))
                ? "[REDACTED_INVALID_JSON]"
                : source;
        }
    }
    #endregion

    #region 写入（Write）
    /// <summary>
    /// 写入（Write）
    /// </summary>
    /// <param name="element">当前递归处理、需要脱敏敏感属性的 JSON 元素。</param>
    /// <param name="writer">用于输出 JSON 内容的写入器。</param>
    /// <param name="sensitive">是否将当前内容视为敏感数据并进行脱敏。</param>
    private static void Write(JsonElement element, Utf8JsonWriter writer, bool sensitive)
    {
        if (sensitive)
        {
            writer.WriteStringValue("[REDACTED]");
            return;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    Write(property.Value, writer, IsSensitive(property.Name));
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (JsonElement item in element.EnumerateArray())
                {
                    Write(item, writer, sensitive: false);
                }
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }
    #endregion

    #region 识别执行载荷的敏感字段名（IsSensitive）
    /// <summary>
    /// 识别执行载荷的敏感字段名（IsSensitive）。
    /// </summary>
    /// <param name="name">待检查的载荷字段名称，本方法不检查字段值。</param>
    /// <returns>字段名移除非字母数字字符并转为小写后，包含任一配置的敏感词时返回 true，否则返回 false。</returns>
    private static bool IsSensitive(string name)
    {
        string normalized = Normalize(name);
        return SensitiveTerms.Any(term =>
            normalized.Contains(term, StringComparison.Ordinal));
    }
    #endregion

    #region 处理（Normalize）
    /// <summary>
    /// 处理（Normalize）
    /// </summary>
    /// <param name="value">需要仅保留字母数字并转为小写的文本。</param>
    /// <returns>仅保留字母和数字并转换为小写的文本，用于敏感词匹配。</returns>
    private static string Normalize(string value) =>
        new(value.Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    #endregion
}
