namespace EU.Core.Api.MCP.Services.BusinessQuery.Security;

/// <summary>只从部署租户生成租户范围；公司等业务范围必须由独立的可信权限解析器提供。</summary>
public static class BusinessQueryTrustedScopes
{
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> FromTenant(string scopeField, string tenantId)
    {
        if (string.IsNullOrEmpty(scopeField)
            || !scopeField.EndsWith(".tenantId", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(tenantId))
            return new Dictionary<string, IReadOnlyList<string>>();

        return new Dictionary<string, IReadOnlyList<string>> { [scopeField] = [tenantId] };
    }
}
