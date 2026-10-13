#nullable enable

namespace EU.Core.IServices.Agents;

/// <summary>
/// 提供模型配置标识的安全格式校验。
/// </summary>
public static class PublicModelProfileCatalog
{
    private static readonly string[] SensitiveTerms =
    [
        "apikey",
        "authorization",
        "connection",
        "credential",
        "password",
        "secret",
        "token"
    ];

    #region 校验公开模型配置标识集合（AreValid）
    /// <summary>
    /// 检查集合中每个模型配置标识是否符合公开标识的格式及敏感内容限制（AreValid）。
    /// </summary>
    /// <param name="profileIds">允许公开使用的模型配置标识集合。</param>
    /// <returns>集合非 null 且所有标识均合法时返回 true，空集合也返回 true；否则返回 false。</returns>
    public static bool AreValid(IEnumerable<string>? profileIds)
    {
        if (profileIds is null)
        {
            return false;
        }

        foreach (string? value in profileIds)
        {
            if (!IsSafePublicIdentifier(value))
            {
                return false;
            }
        }

        return true;
    }
    #endregion

    #region 校验单个公开模型标识（IsSafePublicIdentifier）
    /// <summary>
    /// 校验模型标识的长度、路径片段以及敏感前缀和关键词（IsSafePublicIdentifier）。
    /// </summary>
    /// <param name="value">待校验的模型标识，校验时忽略首尾空白。</param>
    /// <returns>标识通过全部公开格式检查时返回 true；为空或不符合限制时返回 false。</returns>
    private static bool IsSafePublicIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string identifier = value.Trim();
        if (identifier.Length is > 200 ||
            identifier.StartsWith("alias:", StringComparison.OrdinalIgnoreCase) ||
            identifier.StartsWith("sk-", StringComparison.OrdinalIgnoreCase) ||
            identifier.StartsWith("bearer", StringComparison.OrdinalIgnoreCase) ||
            identifier.StartsWith("eyJ", StringComparison.OrdinalIgnoreCase) ||
            identifier.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        string normalized = new(identifier
            .Where(char.IsAsciiLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
        if (SensitiveTerms.Any(term => normalized.Contains(term, StringComparison.Ordinal)))
        {
            return false;
        }

        return identifier.Split('/').All(IsSafeSegment);
    }
    #endregion

    #region 校验模型标识片段（IsSafeSegment）
    /// <summary>
    /// 校验斜杠分隔的单个模型标识片段（IsSafeSegment）。
    /// </summary>
    /// <param name="segment">需要校验的模型配置标识片段。</param>
    /// <returns>长度为 1 至 128、首尾为 ASCII 字母或数字，且仅含 ASCII 字母、数字、点、下划线或连字符时返回 true，否则返回 false。</returns>
    private static bool IsSafeSegment(string segment)
    {
        if (segment.Length is 0 or > 128 ||
            !char.IsAsciiLetterOrDigit(segment[0]) ||
            !char.IsAsciiLetterOrDigit(segment[^1]))
        {
            return false;
        }

        return segment.All(character =>
            char.IsAsciiLetterOrDigit(character) ||
            character is '.' or '_' or '-');
    }
    #endregion
}
