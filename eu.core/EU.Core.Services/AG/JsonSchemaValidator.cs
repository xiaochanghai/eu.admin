#nullable enable

using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EU.Core.Services;

// 文件职责：JsonSchemaValidator 职责实现

/// <summary>
/// JSON Schema 规范化及有效性校验结果。
/// </summary>
/// <param name="IsValid">JSON Schema 是否有效。</param>
/// <param name="CanonicalJson">规范化后的 JSON Schema；校验失败时为空。</param>
/// <param name="Sha256">规范化内容的 SHA-256 摘要；校验失败时为空。</param>
/// <param name="Error">校验失败原因；校验成功时为空。</param>
public sealed record JsonSchemaValidationResult(bool IsValid, string? CanonicalJson, string? Sha256, string? Error)
{
    #region 创建 Schema 校验失败结果（Invalid）
    /// <summary>
    /// 创建 Schema 校验失败结果（Invalid）。
    /// </summary>
    /// <param name="error">需要写入校验结果的失败原因。</param>
    /// <returns>返回 IsValid 为 false、规范化 JSON 和摘要为 null，并包含指定错误说明的结果。</returns>
    public static JsonSchemaValidationResult Invalid(string error) => new(false, null, null, error);
    #endregion
}

/// <summary>
/// JSON 实例针对指定 Schema 的校验结果。
/// </summary>
/// <param name="Succeeded">JSON 实例是否通过校验。</param>
/// <param name="Error">校验失败原因；校验成功时为空。</param>
public sealed record JsonInstanceValidationResult(bool Succeeded, string? Error)
{
    #region 创建 JSON 实例校验成功结果（Success）
    /// <summary>
    /// 创建 JSON 实例校验成功结果（Success）。
    /// </summary>
    /// <returns>返回 Succeeded 为 true、Error 为 null 的结果。</returns>
    public static JsonInstanceValidationResult Success() => new(true, null);
    #endregion

    #region 创建 JSON 实例校验失败结果（Invalid）
    /// <summary>
    /// 创建 JSON 实例校验失败结果（Invalid）。
    /// </summary>
    /// <param name="error">需要写入校验结果的失败原因。</param>
    /// <returns>返回 Succeeded 为 false 且包含指定错误说明的结果。</returns>
    public static JsonInstanceValidationResult Invalid(string error) => new(false, error);
    #endregion
}

/// <summary>
/// 提供 JSON Schema 规范化和实例校验能力。
/// </summary>
public sealed class JsonSchemaValidator
{
    private const int MaximumSchemaCharacters = 65_536;
    private const int MaximumSchemaDepth = 16;
    private const int MaximumSchemaNodes = 512;
    private static readonly HashSet<string> SupportedTypes = new(StringComparer.Ordinal)
    {
        "object", "array", "string", "number", "integer", "boolean", "null"
    };

    #region 校验并规范化受支持的 JSON Schema（Validate）
    /// <summary>
    /// 校验并规范化受支持的 JSON Schema（Validate）。
    /// </summary>
    /// <param name="schema">待校验的 Schema JSON 文本，根节点须为对象并声明受支持的类型；空白视为无效。</param>
    /// <returns>成功时返回 IsValid 为 true、规范化 JSON 及其小写 SHA-256 摘要；失败时返回错误说明，规范化 JSON 和摘要均为 null。</returns>
    public JsonSchemaValidationResult Validate(string? schema)
    {
        if (string.IsNullOrWhiteSpace(schema))
        {
            return JsonSchemaValidationResult.Invalid("A structured output schema is required.");
        }

        if (schema.Length > MaximumSchemaCharacters)
        {
            return JsonSchemaValidationResult.Invalid("The structured output schema exceeds the supported size.");
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(schema, new JsonDocumentOptions { MaxDepth = MaximumSchemaDepth });
            if (document.RootElement.ValueKind is not JsonValueKind.Object)
            {
                return JsonSchemaValidationResult.Invalid("The schema root must be a JSON object.");
            }

            int nodeCount = 0;
            if (!ValidateSchemaObject(document.RootElement, true, 0, ref nodeCount, out string? error))
            {
                return JsonSchemaValidationResult.Invalid(error!);
            }

            string canonicalJson = Canonicalize(document.RootElement);
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalJson))).ToLowerInvariant();
            return new JsonSchemaValidationResult(true, canonicalJson, hash, null);
        }
        catch (JsonException)
        {
            return JsonSchemaValidationResult.Invalid("The schema is not valid JSON.");
        }
    }
    #endregion

    #region 校验 JSON 实例是否符合受支持的 Schema 规则（ValidateInstance）
    /// <summary>
    /// 校验 JSON 实例是否符合受支持的 Schema 规则（ValidateInstance）。
    /// </summary>
    /// <param name="schema">先进行结构校验和规范化的 Schema JSON。</param>
    /// <param name="instance">待校验的非 null JSON 实例文本，本方法不是完整 JSON Schema 标准实现。</param>
    /// <returns>通过时返回 Succeeded 为 true 且 Error 为 null；Schema 无效、实例 JSON 解析失败或受支持规则不满足时返回失败原因。</returns>
    public JsonInstanceValidationResult ValidateInstance(string schema, string instance)
    {
        JsonSchemaValidationResult schemaResult = Validate(schema);
        if (!schemaResult.IsValid)
        {
            return JsonInstanceValidationResult.Invalid(schemaResult.Error!);
        }

        try
        {
            using JsonDocument schemaDocument = JsonDocument.Parse(schemaResult.CanonicalJson!);
            using JsonDocument instanceDocument = JsonDocument.Parse(
                instance,
                new JsonDocumentOptions { MaxDepth = MaximumSchemaDepth });
            return ValidateInstanceCore(
                schemaDocument.RootElement,
                instanceDocument.RootElement,
                0,
                out string? error)
                ? JsonInstanceValidationResult.Success()
                : JsonInstanceValidationResult.Invalid(error!);
        }
        catch (JsonException)
        {
            return JsonInstanceValidationResult.Invalid(
                "The structured Agent output is not valid JSON.");
        }
    }
    #endregion

    #region 按受支持的 Schema 规则校验 JSON 实例（ValidateInstanceCore）
    /// <summary>
    /// 按受支持的 Schema 规则校验 JSON 实例（ValidateInstanceCore）。
    /// </summary>
    /// <param name="schema">已通过结构检查的 Schema 对象，仅使用本实现支持的规则。</param>
    /// <param name="value">当前待校验的 JSON 实例。</param>
    /// <param name="depth">当前对象属性的递归校验深度。</param>
    /// <param name="error">失败时输出首个校验错误；成功时为 null。</param>
    /// <returns>实例满足本方法的深度、类型、必填属性及已定义对象属性检查时返回 true，否则返回 false；不执行完整 JSON Schema 标准校验。</returns>
    private static bool ValidateInstanceCore(JsonElement schema, JsonElement value, int depth, out string? error)
    {
        if (depth > MaximumSchemaDepth)
        {
            error = "The structured Agent output exceeds the supported depth.";
            return false;
        }

        string? type = schema.TryGetProperty("type", out JsonElement typeElement)
            ? typeElement.GetString()
            : null;
        bool typeMatches = type switch
        {
            null => true,
            "object" => value.ValueKind == JsonValueKind.Object,
            "array" => value.ValueKind == JsonValueKind.Array,
            "string" => value.ValueKind == JsonValueKind.String,
            "number" => value.ValueKind == JsonValueKind.Number,
            "integer" => value.ValueKind == JsonValueKind.Number &&
                         value.TryGetInt64(out _),
            "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            "null" => value.ValueKind == JsonValueKind.Null,
            _ => false
        };
        if (!typeMatches)
        {
            error = $"The structured Agent output does not match schema type '{type}'.";
            return false;
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            if (schema.TryGetProperty("required", out JsonElement required))
            {
                foreach (JsonElement requiredName in required.EnumerateArray())
                {
                    if (!value.TryGetProperty(requiredName.GetString()!, out _))
                    {
                        error = $"The structured Agent output is missing required property '{requiredName.GetString()}'.";
                        return false;
                    }
                }
            }

            if (schema.TryGetProperty("properties", out JsonElement properties))
            {
                foreach (JsonProperty property in properties.EnumerateObject())
                {
                    if (value.TryGetProperty(property.Name, out JsonElement propertyValue) &&
                        !ValidateInstanceCore(
                            property.Value,
                            propertyValue,
                            depth + 1,
                            out error))
                    {
                        return false;
                    }
                }
            }
        }

        error = null;
        return true;
    }
    #endregion

    #region 递归校验受支持的 Schema 对象结构（ValidateSchemaObject）
    /// <summary>
    /// 递归校验受支持的 Schema 对象结构（ValidateSchemaObject）。
    /// </summary>
    /// <param name="schema">待校验的 JSON 对象形式 Schema。</param>
    /// <param name="requireType">是否要求当前 Schema 对象显式声明受支持的 type。</param>
    /// <param name="depth">当前 Schema 对象的递归深度。</param>
    /// <param name="nodeCount">累计检查的 Schema 对象节点数，通过引用更新；失败时不回退。</param>
    /// <param name="error">失败时输出首个结构错误；成功时为 null。</param>
    /// <returns>深度、节点数、类型、属性定义和 required 列表均符合本实现约束时返回 true，否则返回 false；不代表支持所有 JSON Schema 关键字。</returns>
    private static bool ValidateSchemaObject(JsonElement schema, bool requireType, int depth, ref int nodeCount, out string? error)
    {
        if (depth > MaximumSchemaDepth || ++nodeCount > MaximumSchemaNodes)
        {
            error = "The schema exceeds the supported complexity.";
            return false;
        }

        var fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (JsonProperty property in schema.EnumerateObject())
        {
            if (!fields.TryAdd(property.Name, property.Value))
            {
                error = "Schema object properties must be unique.";
                return false;
            }
        }

        bool hasType = fields.TryGetValue("type", out JsonElement typeElement);
        if (requireType && !hasType)
        {
            error = "The schema root requires a supported type.";
            return false;
        }

        if (hasType && (typeElement.ValueKind is not JsonValueKind.String || !SupportedTypes.Contains(typeElement.GetString()!)))
        {
            error = "The schema type is not supported.";
            return false;
        }

        if (fields.TryGetValue("properties", out JsonElement properties))
        {
            if (properties.ValueKind is not JsonValueKind.Object)
            {
                error = "Schema properties must be an object.";
                return false;
            }

            if (hasType && !string.Equals(typeElement.GetString(), "object", StringComparison.Ordinal))
            {
                error = "Only object schemas may define properties.";
                return false;
            }

            var propertyNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonProperty property in properties.EnumerateObject())
            {
                if (!propertyNames.Add(property.Name))
                {
                    error = "Schema properties must have unique names.";
                    return false;
                }

                if (property.Value.ValueKind is not JsonValueKind.Object)
                {
                    error = "Each schema property must be a schema object.";
                    return false;
                }

                if (!ValidateSchemaObject(property.Value, false, depth + 1, ref nodeCount, out error))
                {
                    return false;
                }
            }
        }

        if (fields.TryGetValue("required", out JsonElement required))
        {
            if (required.ValueKind is not JsonValueKind.Array || !fields.TryGetValue("properties", out JsonElement definedProperties))
            {
                error = "Schema required must name properties from a properties object.";
                return false;
            }

            var knownProperties = new HashSet<string>(definedProperties.EnumerateObject().Select(property => property.Name), StringComparer.Ordinal);
            var requiredNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonElement requiredName in required.EnumerateArray())
            {
                if (requiredName.ValueKind is not JsonValueKind.String ||
                    !requiredNames.Add(requiredName.GetString()!) ||
                    !knownProperties.Contains(requiredName.GetString()!))
                {
                    error = "Schema required must contain unique known property names.";
                    return false;
                }
            }
        }

        error = null;
        return true;
    }
    #endregion

    #region 规范化 JSON（Canonicalize）
    /// <summary>
    /// 规范化 JSON（Canonicalize）
    /// </summary>
    /// <param name="element">需要按规范化规则写入的 JSON 元素。</param>
    /// <returns>按规范化写入规则生成的紧凑 JSON 文本。</returns>
    private static string Canonicalize(JsonElement element)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        WriteCanonical(element, writer);
        writer.Flush();
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
    #endregion

    #region 写入（WriteCanonical）
    /// <summary>
    /// 写入（WriteCanonical）
    /// </summary>
    /// <param name="element">需要按规范化规则写入的 JSON 元素。</param>
    /// <param name="writer">用于输出 JSON 内容的写入器。</param>
    private static void WriteCanonical(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (JsonProperty property in element.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(property.Value, writer);
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (JsonElement item in element.EnumerateArray())
                {
                    WriteCanonical(item, writer);
                }

                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }
    #endregion
}
