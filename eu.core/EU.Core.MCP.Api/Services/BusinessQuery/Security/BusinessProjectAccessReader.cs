using SqlSugar;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Security;

/// <summary>复用 EU 用户、角色、模块与公司授权表；仅执行参数化只读查询，不回填 Redis。</summary>
public sealed class BusinessProjectAccessReader : IBusinessProjectAccessReader
{
    public async Task<BusinessProjectAccess> ReadAsync(ISqlSugarClient database, Guid userId, string moduleCode, string physicalTable, CancellationToken cancellationToken)
    {
        var denied = new BusinessProjectAccess(false, []);
        if (!await database.Queryable<SmUsers>().Where(user => user.ID == userId && !user.IsDeleted && user.IsActive == true)
                .AnyAsync(cancellationToken))
            return denied;

        var modules = await database.Queryable<SmModules>()
            .Where(module => module.ModuleCode == moduleCode && !module.IsDeleted && module.IsActive == true && module.IsRoleDataScope == true)
            .Select(module => module.ID).Take(2).ToListAsync(cancellationToken);
        if (modules.Count != 1)
            return denied;

        Guid moduleId = modules[0];
        var tables = await database.Queryable<SmModuleSql>()
            .Where(sql => sql.ModuleId == moduleId && !sql.IsDeleted && sql.IsActive == true)
            .Select(sql => sql.PrimaryTableName).Take(2).ToListAsync(cancellationToken);
        if (tables.Count != 1 || !string.Equals(tables[0], physicalTable, StringComparison.OrdinalIgnoreCase))
            return denied;

        var roles = await database.Queryable<SmUserRole, SmRoles>((link, role) => new JoinQueryInfos(JoinType.Inner, link.SmRoleId == role.ID))
            .Where((link, role) => link.SmUserId == userId && !link.IsDeleted && link.IsActive == true && !role.IsDeleted && role.IsActive == true)
            .Select((link, role) => role.ID).Distinct().Take(1001).ToListAsync(cancellationToken);
        if (roles.Count is 0 or > 1000)
            return denied;

        bool allowed = await database.Queryable<SmRoleModule>()
            .Where(link => roles.Contains(link.SmRoleId!.Value) && link.SmModuleId == moduleId && !link.IsDeleted && link.IsActive == true)
            .AnyAsync(cancellationToken);
        if (!allowed)
            return denied;

        var companies = await database.Queryable<SmRoleDataScope>()
            .Where(scope => roles.Contains(scope.SmRoleId) && scope.CompanyId != null && !scope.IsDeleted && scope.IsActive == true)
            .Select(scope => scope.CompanyId!.Value).Distinct().Take(101).ToListAsync(cancellationToken);
        return new BusinessProjectAccess(true, companies);
    }
}
