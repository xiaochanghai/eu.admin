using System.Text.Json;

namespace EU.Core.Agent.Runtime;

public static class McpToolArgumentNormalizer
{
    private const int MaximumStructuredStringLength = 65_536;

    public static IReadOnlyDictionary<string, object?> Normalize(
        IReadOnlyDictionary<string, object?> arguments,
        JsonElement inputSchema)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (!inputSchema.TryGetProperty("properties", out JsonElement properties)
            || properties.ValueKind != JsonValueKind.Object)
        {
            return arguments;
        }

        Dictionary<string, object?>? normalized = null;
        foreach ((string name, object? value) in arguments)
        {
            if (!TryGetString(value, out string text)
                || text.Length == 0
                || text.Length > MaximumStructuredStringLength
                || !properties.TryGetProperty(name, out JsonElement propertySchema)
                || !TryExpectedKind(propertySchema, out JsonValueKind expectedKind))
            {
                continue;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(text);
                if (document.RootElement.ValueKind != expectedKind)
                {
                    continue;
                }

                normalized ??= new Dictionary<string, object?>(
                    arguments, StringComparer.Ordinal);
                normalized[name] = document.RootElement.Clone();
            }
            catch (JsonException)
            {
                // Preserve invalid model output so normal Schema validation fails closed.
            }
        }

        return normalized ?? arguments;
    }

    private static bool TryGetString(object? value, out string text)
    {
        if (value is string stringValue)
        {
            text = stringValue;
            return true;
        }

        if (value is JsonElement { ValueKind: JsonValueKind.String } element)
        {
            text = element.GetString() ?? string.Empty;
            return true;
        }

        text = string.Empty;
        return false;
    }

    private static bool TryExpectedKind(
        JsonElement schema,
        out JsonValueKind expectedKind)
    {
        expectedKind = JsonValueKind.Undefined;
        var kinds = new HashSet<JsonValueKind>();
        CollectStructuredKinds(schema, kinds);
        if (kinds.Count != 1)
        {
            return false;
        }

        expectedKind = kinds.Single();
        return true;
    }

    private static void CollectStructuredKinds(
        JsonElement schema,
        ISet<JsonValueKind> kinds)
    {
        if (schema.TryGetProperty("oneOf", out JsonElement oneOf)
            && oneOf.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement candidate in oneOf.EnumerateArray())
            {
                CollectStructuredKinds(candidate, kinds);
            }
        }

        if (!schema.TryGetProperty("type", out JsonElement type))
        {
            return;
        }

        IEnumerable<string> values = type.ValueKind switch
        {
            JsonValueKind.String => [type.GetString() ?? string.Empty],
            JsonValueKind.Array => type.EnumerateArray()
                .Where(value => value.ValueKind == JsonValueKind.String)
                .Select(value => value.GetString() ?? string.Empty),
            _ => []
        };
        foreach (string value in values)
        {
            if (string.Equals(value, "object", StringComparison.Ordinal))
            {
                kinds.Add(JsonValueKind.Object);
            }
            else if (string.Equals(value, "array", StringComparison.Ordinal))
            {
                kinds.Add(JsonValueKind.Array);
            }
        }
    }
}
