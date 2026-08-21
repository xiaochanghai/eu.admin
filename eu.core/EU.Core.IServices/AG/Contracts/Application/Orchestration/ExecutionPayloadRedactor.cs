#nullable enable

using System.Buffers;
using System.Text;
using System.Text.Json;

namespace EU.Core.IServices.Orchestration;

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

    private static bool IsSensitive(string name)
    {
        string normalized = Normalize(name);
        return SensitiveTerms.Any(term =>
            normalized.Contains(term, StringComparison.Ordinal));
    }

    private static string Normalize(string value) =>
        new(value.Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
}
