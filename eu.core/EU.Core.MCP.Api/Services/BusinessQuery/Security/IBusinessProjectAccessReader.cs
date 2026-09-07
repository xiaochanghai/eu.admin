using SqlSugar;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Security;

public sealed record BusinessProjectAccess(bool ModuleAllowed, IReadOnlyList<Guid> CompanyIds);

/// <summary>从业务查询使用的同一数据库读取项目权限，不读取密码或缓存会话。</summary>
public interface IBusinessProjectAccessReader
{
    Task<BusinessProjectAccess> ReadAsync(ISqlSugarClient database, Guid userId, string moduleCode, string physicalTable, CancellationToken cancellationToken);
}
