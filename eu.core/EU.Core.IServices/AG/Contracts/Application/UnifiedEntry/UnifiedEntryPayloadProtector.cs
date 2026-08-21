#nullable enable

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EU.Core.IServices.Orchestration;

namespace EU.Core.IServices.UnifiedEntry;

public sealed record ProtectedUnifiedPayload(
    string Content,
    string OriginalSha256,
    int OriginalUtf8Bytes,
    int PersistedUtf8Bytes);

public static class UnifiedEntryPayloadProtector
{
    public const int InternalPayloadLimitUtf8Bytes = 32_768;
    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);
    private static readonly Regex CredentialPattern = new(
        """
        (?ix)
        (?:\b[a-z][a-z0-9+.-]*://[^\s/:@]+:[^\s/@]+@)
        |
        (?:\b(?:bearer|basic)\s+[a-z0-9+/_.=-]+)
        |
        (?:\b(?:password|pwd|api[\s_-]?key|token|secret|connection[\s_-]?string)\s*[:=]\s*[^\s;,]+)
        """,
        RegexOptions.CultureInvariant);

    public static ProtectedUnifiedPayload ProtectInternal(string? value) =>
        Protect(
            value,
            InternalPayloadLimitUtf8Bytes,
            InternalPayloadLimitUtf8Bytes);

    public static ProtectedUnifiedPayload Protect(
        string? value,
        int originalLimitUtf8Bytes,
        int persistedLimitUtf8Bytes)
    {
        ValidateLimit(originalLimitUtf8Bytes);
        ValidateLimit(persistedLimitUtf8Bytes);

        string original = value ?? string.Empty;
        int originalByteCount;
        try
        {
            originalByteCount = StrictUtf8.GetByteCount(original);
        }
        catch (EncoderFallbackException)
        {
            throw new UnifiedEntryException(
                UnifiedEntryErrorCodes.PayloadInvalidEncoding,
                "The unified entry payload is not valid UTF-8 text.");
        }

        if (originalByteCount > originalLimitUtf8Bytes)
        {
            throw LimitExceeded();
        }

        byte[] originalBytes = StrictUtf8.GetBytes(original);
        string protectedContent = ProtectContent(original);
        int persistedBytes;
        try
        {
            persistedBytes = StrictUtf8.GetByteCount(protectedContent);
        }
        catch (EncoderFallbackException)
        {
            throw new UnifiedEntryException(
                UnifiedEntryErrorCodes.PayloadInvalidEncoding,
                "The protected unified entry payload is not valid UTF-8 text.");
        }
        if (persistedBytes > persistedLimitUtf8Bytes)
        {
            throw LimitExceeded();
        }

        return new ProtectedUnifiedPayload(
            protectedContent,
            Convert.ToHexString(SHA256.HashData(originalBytes)).ToLowerInvariant(),
            originalByteCount,
            persistedBytes);
    }

    private static string ProtectContent(string value)
    {
        string trimmed = value.TrimStart();
        bool looksJson = LooksLikeJson(trimmed);
        if (looksJson)
        {
            try
            {
                using JsonDocument _ = JsonDocument.Parse(value);
            }
            catch (JsonException)
            {
                bool malformedObject = trimmed.StartsWith('{');
                bool containsCredential = CredentialPattern.IsMatch(value);
                bool containsSensitiveJsonName = string.Equals(
                    ExecutionPayloadRedactor.RedactJson(value),
                    "[REDACTED_INVALID_JSON]",
                    StringComparison.Ordinal);
                if (malformedObject
                    || containsCredential
                    || containsSensitiveJsonName)
                {
                    return "[REDACTED_INVALID_JSON]";
                }
            }
        }

        if (!looksJson && CredentialPattern.IsMatch(value))
        {
            return "[REDACTED]";
        }

        return ExecutionPayloadRedactor.RedactJson(value);
    }

    private static bool LooksLikeJson(string value)
    {
        if (value.StartsWith('{'))
        {
            return true;
        }

        if (!value.StartsWith('['))
        {
            return false;
        }

        int index = 1;
        while (index < value.Length && char.IsWhiteSpace(value[index]))
        {
            index++;
        }

        if (index == value.Length)
        {
            return true;
        }

        return value[index] is '{' or '[' or '"' or ']' or '-'
            or '0' or '1' or '2' or '3' or '4'
            or '5' or '6' or '7' or '8' or '9'
            or 't' or 'f' or 'n';
    }

    private static void ValidateLimit(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Payload limits cannot be negative.");
        }
    }

    private static UnifiedEntryException LimitExceeded() =>
        new(
            UnifiedEntryErrorCodes.PayloadLimitExceeded,
            "The unified entry payload exceeds its configured UTF-8 byte limit.");
}
