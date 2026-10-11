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
    /// <param name="issuer">业务查询上下文令牌的签发方。</param>
    /// <param name="audience">业务查询上下文令牌的接收方。</param>
    /// <param name="signingKeyAlias">上下文令牌签名密钥别名。</param>
    /// <param name="catalogRevision">受控业务查询目录的修订号。</param>
    /// <param name="catalogHash">受控业务查询目录的摘要。</param>
    /// <param name="toolSchemaHash">业务查询工具架构的摘要。</param>
    /// <param name="tokenLifetime">业务查询上下文令牌的有效期。</param>
    public BusinessQueryToolPolicy(
        string serverCode,
        string toolName,
        string issuer,
        string audience,
        string signingKeyAlias,
        long catalogRevision,
        string catalogHash,
        string toolSchemaHash,
        TimeSpan tokenLifetime)
    {
        if (!CodePattern().IsMatch(serverCode ?? string.Empty)
            || !string.Equals(toolName, "query_business_data", StringComparison.Ordinal)
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
        Issuer = issuer!;
        Audience = audience!;
        SigningKeyAlias = signingKeyAlias!;
        CatalogRevision = catalogRevision;
        CatalogHash = catalogHash!;
        ToolSchemaHash = toolSchemaHash!;
        TokenLifetime = tokenLifetime;
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
    #region 匹配业务查询工具及端点来源（Matches）
    /// <summary>
    /// 匹配业务查询工具及端点来源（Matches）。
    /// </summary>
    /// <param name="serverCode">MCP 服务器编码。</param>
    /// <param name="toolName">工具名称。</param>
    /// <returns>服务器编码和工具名均完全匹配时返回 true，否则返回 false。</returns>
    public bool Matches(string serverCode, string toolName) =>
        string.Equals(ServerCode, serverCode, StringComparison.Ordinal)
        && string.Equals(ToolName, toolName, StringComparison.Ordinal);
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
