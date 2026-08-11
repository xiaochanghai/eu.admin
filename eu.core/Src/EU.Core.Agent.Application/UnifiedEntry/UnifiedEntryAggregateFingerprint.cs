using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EU.Core.Agent.Application.UnifiedEntry;

public static class UnifiedEntryAggregateFingerprint
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string ComputeSha256(UnifiedEntryAggregate value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var document = new FingerprintDocument(
            value.Conversation,
            value.Messages,
            value.Details,
            value.Events);
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(
            document,
            SerializerOptions);
        return Convert.ToHexString(SHA256.HashData(json)).ToLowerInvariant();
    }

    private sealed record FingerprintDocument(
        ConversationRecord Conversation,
        IReadOnlyList<ConversationMessageRecord> Messages,
        UnifiedRunDetails Details,
        IReadOnlyList<UnifiedRunEventRecord> Events);
}
