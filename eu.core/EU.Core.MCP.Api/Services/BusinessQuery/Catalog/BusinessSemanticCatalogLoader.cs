using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Catalog;

public sealed record BusinessCatalogLoadResult(
    BusinessCatalogSnapshot? Snapshot,
    BusinessCatalogValidationError? Error)
{
    public bool Succeeded => Error is null;
}

public sealed class BusinessSemanticCatalogLoader
{
    public const int MaximumCatalogUtf8Bytes = 256 * 1024;
    public const int MaximumJsonDepth = 32;
    public const string CatalogRequired = "BUSINESS_CATALOG_REQUIRED";
    public const string CatalogTooLarge = "BUSINESS_CATALOG_TOO_LARGE";
    public const string CatalogInvalidJson = "BUSINESS_CATALOG_INVALID_JSON";
    public const string CatalogUnknownProperty = "BUSINESS_CATALOG_UNKNOWN_PROPERTY";
    public const string CatalogDuplicateProperty = "BUSINESS_CATALOG_DUPLICATE_PROPERTY";

    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        AllowTrailingCommas = false,
        MaxDepth = MaximumJsonDepth,
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters =
        {
            new JsonStringEnumConverter(
                JsonNamingPolicy.CamelCase,
                allowIntegerValues: false)
        }
    };

    private readonly BusinessSemanticCatalogValidator _validator;

    public BusinessSemanticCatalogLoader(
        BusinessSemanticCatalogValidator? validator = null)
    {
        _validator = validator ?? new BusinessSemanticCatalogValidator();
    }

    public BusinessCatalogLoadResult Load(
        string? json,
        string? expectedSha256 = null)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Failure(CatalogRequired, "A semantic Catalog is required.");
        }

        int byteCount;
        try
        {
            byteCount = StrictUtf8.GetByteCount(json);
        }
        catch (EncoderFallbackException)
        {
            return Failure(
                CatalogInvalidJson,
                "The semantic Catalog is not valid UTF-8 text.");
        }

        if (byteCount > MaximumCatalogUtf8Bytes)
        {
            return Failure(
                CatalogTooLarge,
                "The semantic Catalog exceeds the supported size.");
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(
                json,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = MaximumJsonDepth
                });
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Failure(
                    CatalogInvalidJson,
                    "The semantic Catalog root must be an object.");
            }

            if (HasDuplicateProperties(document.RootElement))
            {
                return Failure(
                    CatalogDuplicateProperty,
                    "The semantic Catalog contains a duplicate JSON property.");
            }

            string canonicalJson = Canonicalize(document.RootElement);
            byte[] hashBytes = SHA256.HashData(StrictUtf8.GetBytes(canonicalJson));
            string sha256 = Convert.ToHexString(hashBytes).ToLowerInvariant();
            if (!MatchesExpectedHash(hashBytes, expectedSha256))
            {
                return Failure(
                    BusinessSemanticCatalogValidator.CatalogHashMismatch,
                    "The semantic Catalog does not match its trusted expected hash.");
            }

            BusinessSemanticCatalog? catalog;
            try
            {
                catalog = JsonSerializer.Deserialize<BusinessSemanticCatalog>(
                    document.RootElement.GetRawText(),
                    SerializerOptions);
            }
            catch (JsonException exception) when (IsUnknownProperty(exception))
            {
                return Failure(
                    CatalogUnknownProperty,
                    "The semantic Catalog contains an unsupported property.");
            }

            BusinessCatalogValidationResult validation = _validator.Validate(
                catalog,
                canonicalJson,
                sha256);
            return validation.Succeeded
                ? new BusinessCatalogLoadResult(validation.Snapshot, null)
                : new BusinessCatalogLoadResult(null, validation.Error);
        }
        catch (JsonException exception) when (IsUnknownProperty(exception))
        {
            return Failure(
                CatalogUnknownProperty,
                "The semantic Catalog contains an unsupported property.");
        }
        catch (JsonException)
        {
            return Failure(
                CatalogInvalidJson,
                "The semantic Catalog is not valid JSON.");
        }
    }

    private static bool MatchesExpectedHash(
        byte[] actual,
        string? expectedSha256)
    {
        if (string.IsNullOrWhiteSpace(expectedSha256))
        {
            return true;
        }

        if (expectedSha256.Length != 64)
        {
            return false;
        }

        try
        {
            byte[] expected = Convert.FromHexString(expectedSha256);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string Canonicalize(JsonElement root)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(
                   stream,
                   new JsonWriterOptions { Indented = false }))
        {
            WriteCanonical(writer, root);
        }

        return StrictUtf8.GetString(stream.ToArray());
    }

    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (JsonProperty property in value
                             .EnumerateObject()
                             .OrderBy(item => item.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(writer, property.Value);
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (JsonElement item in value.EnumerateArray())
                {
                    WriteCanonical(writer, item);
                }

                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(value.GetString());
                break;
            case JsonValueKind.Number when value.TryGetInt64(out long integer):
                writer.WriteNumberValue(integer);
                break;
            case JsonValueKind.Number when value.TryGetDecimal(out decimal number):
                writer.WriteNumberValue(number);
                break;
            case JsonValueKind.Number:
                double floatingPoint = value.GetDouble();
                if (!double.IsFinite(floatingPoint))
                {
                    throw new JsonException("The Catalog contains a non-finite number.");
                }

                writer.WriteNumberValue(floatingPoint);
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
            default:
                throw new JsonException("Unsupported JSON token.");
        }
    }

    private static bool HasDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)
                    || HasDuplicateProperties(property.Value))
                {
                    return true;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in element.EnumerateArray())
            {
                if (HasDuplicateProperties(item))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsUnknownProperty(JsonException exception) =>
        exception.Message.Contains(
            "could not be mapped",
            StringComparison.OrdinalIgnoreCase);

    private static BusinessCatalogLoadResult Failure(
        string code,
        string message) =>
        new(null, new BusinessCatalogValidationError(code, message));
}
