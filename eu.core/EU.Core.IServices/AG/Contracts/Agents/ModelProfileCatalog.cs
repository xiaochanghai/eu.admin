#nullable enable

using EU.Core.Model.ViewModels.Extend;

namespace EU.Core.IServices.Agents;

/// <summary>
/// 定义模型配置标识的查询目录。
/// </summary>
public interface IModelProfileReferenceCatalog
{
    #region 检查模型配置引用是否存在。
    /// <summary>检查模型配置引用是否存在。</summary>
    /// <param name="modelProfileId">模型配置标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>异步查询结果：指定模型配置引用存在时返回 true，否则返回 false。</returns>
    Task<bool> ExistsAsync(string modelProfileId, CancellationToken cancellationToken = default);
    #endregion
}

/// <summary>
/// 定义可公开展示的模型配置目录。
/// </summary>
public interface IPublicModelProfileCatalog : IModelProfileReferenceCatalog
{
    /// <summary>获取可公开使用的模型配置标识集合。</summary>
    IReadOnlyList<string> ProfileIds { get; }
}

/// <summary>
/// 基于配置提供可公开展示的模型配置目录。
/// </summary>
public sealed class PublicModelProfileCatalog : IPublicModelProfileCatalog
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

    private readonly HashSet<string> _profileIds;

    #region 构造（PublicModelProfileCatalog）
    /// <summary>
    /// 构造（PublicModelProfileCatalog）
    /// </summary>
    /// <param name="profileIds">允许公开使用的模型配置标识集合。</param>
    public PublicModelProfileCatalog(IEnumerable<string> profileIds)
    {
        ArgumentNullException.ThrowIfNull(profileIds);
        string[] configuredValues = profileIds.ToArray();
        if (!AreValid(configuredValues))
        {
            throw new ArgumentException(
                "The public model profile identifier configuration is invalid.",
                nameof(profileIds));
        }

        string[] values = configuredValues
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        ProfileIds = AgentContractCloner.ReadOnly(values);
        _profileIds = new HashSet<string>(values, StringComparer.Ordinal);
    }
    #endregion

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

    /// <summary>
    /// 获取可公开使用的模型配置标识集合。
    /// </summary>
    public IReadOnlyList<string> ProfileIds { get; }

    #region 查询公开模型配置是否存在（ExistsAsync）
    /// <summary>
    /// 按区分大小写的完整标识查询公开模型配置是否存在（ExistsAsync）。
    /// </summary>
    /// <param name="modelProfileId">模型配置标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>异步查询结果：标识存在于已配置的公开目录中时返回 true，否则返回 false。</returns>
    public Task<bool> ExistsAsync(string modelProfileId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_profileIds.Contains(modelProfileId));
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
