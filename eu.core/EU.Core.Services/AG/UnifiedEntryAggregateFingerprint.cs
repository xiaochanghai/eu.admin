#nullable enable

using EU.Core.IServices.UnifiedEntry;

using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EU.Core.Services;

#region 文件职责：UnifiedEntryAggregateFingerprint 职责实现

/// <summary>
/// 计算统一入口聚合状态的稳定指纹。
/// </summary>
public static class UnifiedEntryAggregateFingerprint
{
    #region 聚合指纹

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

    #endregion
}

#endregion
