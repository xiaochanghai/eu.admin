#nullable enable

using System.Text.RegularExpressions;
using EU.Core.IServices.Mcp;

namespace EU.Core.IServices.UnifiedEntry;

/// <summary>
/// 描述业务查询工具允许访问的服务、工具和结果限制。
/// </summary>
public sealed partial class BusinessQueryToolPolicy
{
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

    public bool Matches(string serverCode, string toolName, string endpoint) =>
        string.Equals(ServerCode, serverCode, StringComparison.Ordinal)
        && string.Equals(ToolName, toolName, StringComparison.Ordinal)
        && Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri)
        && string.Equals(Origin.Scheme, uri.Scheme, StringComparison.OrdinalIgnoreCase)
        && string.Equals(Origin.Host, uri.Host, StringComparison.OrdinalIgnoreCase)
        && Origin.Port == uri.Port;

    public bool Matches(PublishedMcpToolReference tool) =>
        tool is not null
        && string.Equals(ServerCode, tool.ServerCode, StringComparison.Ordinal)
        && string.Equals(ToolName, tool.ToolName, StringComparison.Ordinal);

    [GeneratedRegex("^[a-z][a-z0-9-]{1,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();

    [GeneratedRegex("^alias:[a-z][a-z0-9.-]{1,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex AliasPattern();

    [GeneratedRegex("^[a-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex HashPattern();
}

/// <summary>
/// 当前执行范围内的业务查询策略访问器。
/// </summary>
/// <param name="Policy">当前业务查询工具策略。</param>
public sealed record BusinessQueryToolPolicyAccessor(
    BusinessQueryToolPolicy? Policy);
