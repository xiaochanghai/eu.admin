using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EU.Core.Agent.Application.Validation;

public sealed record JsonSchemaValidationResult(bool IsValid, string? CanonicalJson, string? Sha256, string? Error)
{
    public static JsonSchemaValidationResult Invalid(string error) => new(false, null, null, error);
}

public sealed record JsonInstanceValidationResult(bool Succeeded, string? Error)
{
    public static JsonInstanceValidationResult Success() => new(true, null);

    public static JsonInstanceValidationResult Invalid(string error) => new(false, error);
}

public sealed class JsonSchemaValidator
{
    private const int MaximumSchemaCharacters = 65_536;
    private const int MaximumSchemaDepth = 16;
    private const int MaximumSchemaNodes = 512;
    private static readonly HashSet<string> SupportedTypes = new(StringComparer.Ordinal)
    {
        "object", "array", "string", "number", "integer", "boolean", "null"
    };

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

    private static bool ValidateInstanceCore(
        JsonElement schema,
        JsonElement value,
        int depth,
        out string? error)
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

    private static string Canonicalize(JsonElement element)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        WriteCanonical(element, writer);
        writer.Flush();
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

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
}
