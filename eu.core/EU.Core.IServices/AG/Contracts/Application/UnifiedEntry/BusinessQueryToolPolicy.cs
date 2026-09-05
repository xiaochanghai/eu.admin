#nullable enable

using System.Text.RegularExpressions;
using EU.Core.IServices.Mcp;

namespace EU.Core.IServices.UnifiedEntry;

/// <summary>
/// 描述业务查询工具允许访问的服务、工具和结果限制。
/// </summary>
public sealed partial class BusinessQueryToolPolicy
{
    #region 构造（BusinessQueryToolPolicy）
    /// <summary>
    /// 构造（BusinessQueryToolPolicy）
    /// </summary>
    /// <param name="serverCode">MCP 服务器编码。</param>
    /// <param name="toolName">工具名称。</param>
    /// <param name="origin">业务查询服务的源地址。</param>
    /// <param name="issuer">业务查询上下文令牌的签发方。</param>
    /// <param name="audience">业务查询上下文令牌的接收方。</param>
    /// <param name="signingKeyAlias">上下文令牌签名密钥别名。</param>
    /// <param name="catalogRevision">受控业务查询目录的修订号。</param>
    /// <param name="catalogHash">受控业务查询目录的摘要。</param>
    /// <param name="toolSchemaHash">业务查询工具架构的摘要。</param>
    /// <param name="tokenLifetime">业务查询上下文令牌的有效期。</param>
    /// <param name="allowDevelopmentHttp">是否允许开发环境使用 HTTP。</param>
    public BusinessQueryToolPolicy(
        string serverCode,
        string toolName,
        Uri origin,
        string issuer,
        string audience,
        string signingKeyAlias,
        long catalogRevision,
        string catalogHash,
        string toolSchemaHash,
        TimeSpan tokenLifetime,
        bool allowDevelopmentHttp)
    {
        if (!CodePattern().IsMatch(serverCode ?? string.Empty)
            || !string.Equals(toolName, "query_business_data", StringComparison.Ordinal)
            || origin is null
            || !origin.IsAbsoluteUri
            || origin.UserInfo.Length > 0
            || origin.Query.Length > 0
            || origin.Fragment.Length > 0
            || origin.AbsolutePath != "/"
            || (origin.Scheme != Uri.UriSchemeHttps
                && !(allowDevelopmentHttp && origin.Scheme == Uri.UriSchemeHttp))
            || !CodePattern().IsMatch(issuer ?? string.Empty)
            || !CodePattern().IsMatch(audience ?? string.Empty)
            || !AliasPattern().IsMatch(signingKeyAlias ?? string.Empty)
            || catalogRevision < 1
            || !HashPattern().IsMatch(catalogHash ?? string.Empty)
            || !HashPattern().IsMatch(toolSchemaHash ?? string.Empty)
            || tokenLifetime <= TimeSpan.Zero
            || tokenLifetime > TimeSpan.FromSeconds(60))
        {
            throw new ArgumentException("The Business Query tool policy is invalid.");
        }

        ServerCode = serverCode!;
        ToolName = toolName!;
        Origin = origin!;
        Issuer = issuer!;
        Audience = audience!;
        SigningKeyAlias = signingKeyAlias!;
        CatalogRevision = catalogRevision;
        CatalogHash = catalogHash!;
        ToolSchemaHash = toolSchemaHash!;
        TokenLifetime = tokenLifetime;
        AllowDevelopmentHttp = allowDevelopmentHttp;
    }
    #endregion

    /// <summary>
    /// 获取 MCP 服务器编码。
    /// </summary>
    public string ServerCode { get; }
    /// <summary>
    /// 获取 MCP 工具名称。
    /// </summary>
    public string ToolName { get; }
    /// <summary>
    /// 获取允许的请求来源。
    /// </summary>
    public Uri Origin { get; }
    /// <summary>
    /// 获取上下文令牌签发方。
    /// </summary>
    public string Issuer { get; }
    /// <summary>
    /// 获取上下文令牌接收方。
    /// </summary>
    public string Audience { get; }
    /// <summary>
    /// 获取上下文令牌签名密钥别名。
    /// </summary>
    public string SigningKeyAlias { get; }
    /// <summary>
    /// 获取工具目录修订号。
    /// </summary>
    public long CatalogRevision { get; }
    /// <summary>
    /// 获取工具目录摘要。
    /// </summary>
    public string CatalogHash { get; }
    /// <summary>
    /// 获取工具输入架构摘要。
    /// </summary>
    public string ToolSchemaHash { get; }
    /// <summary>
    /// 获取上下文令牌有效期。
    /// </summary>
    public TimeSpan TokenLifetime { get; }
    /// <summary>
    /// 获取是否允许开发环境使用 HTTP。
    /// </summary>
    public bool AllowDevelopmentHttp { get; }

    #region 匹配业务查询工具及端点来源（Matches）
    /// <summary>
    /// 匹配业务查询工具及端点来源（Matches）。
    /// </summary>
    /// <param name="serverCode">MCP 服务器编码。</param>
    /// <param name="toolName">工具名称。</param>
    /// <param name="endpoint">远程服务端点地址。</param>
    /// <returns>服务器编码、工具名完全匹配，且端点为绝对 URI、协议和主机忽略大小写匹配、端口一致时返回 true，否则返回 false；不比较 URI 路径。</returns>
    public bool Matches(string serverCode, string toolName, string endpoint) =>
        string.Equals(ServerCode, serverCode, StringComparison.Ordinal)
        && string.Equals(ToolName, toolName, StringComparison.Ordinal)
        && Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri)
        && string.Equals(Origin.Scheme, uri.Scheme, StringComparison.OrdinalIgnoreCase)
        && string.Equals(Origin.Host, uri.Host, StringComparison.OrdinalIgnoreCase)
        && Origin.Port == uri.Port;
    #endregion

    #region 匹配已发布业务查询工具标识（Matches）
    /// <summary>
    /// 匹配已发布业务查询工具标识（Matches）。
    /// </summary>
    /// <param name="tool">待匹配的已发布 MCP 工具引用。</param>
    /// <returns>工具非 null 且服务器编码、工具名均区分大小写匹配时返回 true，否则返回 false；此重载不检查端点或版本。</returns>
    public bool Matches(PublishedMcpToolReference tool) =>
        tool is not null
        && string.Equals(ServerCode, tool.ServerCode, StringComparison.Ordinal)
        && string.Equals(ToolName, tool.ToolName, StringComparison.Ordinal);
    #endregion

    #region 处理（CodePattern）
    /// <summary>
    /// 处理（CodePattern）
    /// </summary>
    /// <returns>用于校验业务查询策略编码格式的正则表达式。</returns>
    [GeneratedRegex("^[a-z][a-z0-9-]{1,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();
    #endregion

    #region 处理（AliasPattern）
    /// <summary>
    /// 处理（AliasPattern）
    /// </summary>
    /// <returns>用于校验业务查询凭据别名格式的正则表达式。</returns>
    [GeneratedRegex("^alias:[a-z][a-z0-9.-]{1,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex AliasPattern();
    #endregion

    #region 检查是否存在（HashPattern）
    /// <summary>
    /// 检查是否存在（HashPattern）
    /// </summary>
    /// <returns>用于校验业务查询策略摘要格式的正则表达式。</returns>
    [GeneratedRegex("^[a-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex HashPattern();
    #endregion
}

/// <summary>
/// 当前执行范围内的业务查询策略访问器。
/// </summary>
/// <param name="Policy">当前业务查询工具策略。</param>
public sealed record BusinessQueryToolPolicyAccessor(
    BusinessQueryToolPolicy? Policy);
