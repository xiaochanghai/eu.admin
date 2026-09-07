using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;
using SqlSugar;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Security;

/// <summary>将经过签名验证的调用身份映射为项目模块和公司范围；不信任模型传入的授权信息。</summary>
public class BusinessProjectCallerResolver(IBusinessProjectAccessReader reader)
{
    public virtual async Task<BusinessCallerContext?> ResolveAsync(BusinessQueryExecutionContext context, BusinessCatalogEntitySnapshot entity,
        string tenantId, string dataSourceCode, ISqlSugarClient database, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(context.TenantId, tenantId, StringComparison.Ordinal))
            return null;

        if (string.IsNullOrEmpty(entity.ProjectModuleCode))
            return new BusinessCallerContext(context.UserId, context.TenantId, context.Permissions, [dataSourceCode],
                BusinessQueryTrustedScopes.FromTenant(entity.DefaultScopeField, context.TenantId));

        if (!context.Permissions.Contains("business.project.query", StringComparer.Ordinal)
            || !Guid.TryParse(context.UserId, out Guid userId) || userId == Guid.Empty)
            return null;

        // TEMP-PROJECT-AUTH: 用户明确要求所有环境暂停模块/公司权限。恢复时取消下方整段注释，
        // 并移除临时返回及 BusinessQueryPolicy.TryBuildScope 中同标记的项目范围旁路。
        /*
        BusinessProjectAccess access = await reader.ReadAsync(database, userId, entity.ProjectModuleCode, entity.PhysicalTable, cancellationToken);
        if (!access.ModuleAllowed || access.CompanyIds.Count is 0 or > 100 || access.CompanyIds.Any(id => id == Guid.Empty))
            return null;

        return new BusinessCallerContext(context.UserId, context.TenantId, context.Permissions, [dataSourceCode],
            new Dictionary<string, IReadOnlyList<string>>
            {
                [entity.DefaultScopeField] = access.CompanyIds.Distinct().Order().Select(id => id.ToString("D")).ToArray()
            });
        */
        _ = reader; // 保留依赖注入和原权限读取器，恢复权限时无需重新注册。
        return await Task.FromResult(new BusinessCallerContext(context.UserId, context.TenantId, context.Permissions,
            [dataSourceCode], new Dictionary<string, IReadOnlyList<string>>()));
    }
}
