/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmRoleDataScopeServices.cs
*
* 功 能： 角色数据权限服务
* 类 名： SmRoleDataScopeServices
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/6/23  EU Team   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│ 此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露． │
*│ 版权所有：EU Team                              │
*└──────────────────────────────────┘
*/

using EU.Core.Common.Helper;
using EU.Core.Common.UserManager;
using MathNet.Numerics.Distributions;
using Newtonsoft.Json;

namespace EU.Core.Services;

/// <summary>
/// 角色数据权限服务
/// 提供数据权限的树形结构构建、权限保存、权限查询等功能
/// </summary>
public class SmRoleDataScopeServices : BaseServices<SmRoleDataScope, SmRoleDataScopeDto, InsertSmRoleDataScopeInput, EditSmRoleDataScopeInput>, ISmRoleDataScopeServices
{
    #region 字段

    private readonly IBaseRepository<SmRoleDataScope> _dal;
    private readonly IBaseRepository<SmRoleDataScopeAudit> _auditDal;
    private readonly ISmGroupServices _smGroupServices;
    private readonly ISmCompanyServices _smCompanyServices;

    #endregion

    #region 构造函数

    /// <summary>
    /// 构造函数
    /// </summary>
    public SmRoleDataScopeServices(
        IBaseRepository<SmRoleDataScope> dal,
        IBaseRepository<SmRoleDataScopeAudit> auditDal,
        ISmGroupServices smGroupServices,
        ISmCompanyServices smCompanyServices)
    {
        _dal = dal;
        base.BaseDal = dal;
        _auditDal = auditDal;
        _smGroupServices = smGroupServices;
        _smCompanyServices = smCompanyServices;
    }

    #endregion

    #region 获取所有数据权限树

    /// <summary>
    /// 获取所有数据权限树（集团-公司树形结构）
    /// </summary>
    public async Task<ServiceResult<List<DataScopeTree>>> GetAllDataScopeTree()
    {
        try
        {
            // 1. 查询所有集团
            var groups = await Db.Queryable<SmGroup>()
                .OrderBy(x => x.GroupCode)
                .ToListAsync();

            // 2. 查询所有公司
            var companies = await Db.Queryable<SmCompany>()
                .OrderBy(x => x.CompanyCode)
                .ToListAsync();

            // 3. 构建树形结构
            var tree = new List<DataScopeTree>();
            foreach (var group in groups)
            {
                var groupNode = new DataScopeTree
                {
                    key = group.ID.ToString(),
                    title = $"{group.GroupName}",
                    value = group.ID,
                    isLeaf = false,
                    children = new List<DataScopeTree>()
                };

                // 查询该集团下的所有公司
                var groupCompanies = companies.Where(c => c.GroupId == group.ID).ToList();

                foreach (var company in groupCompanies)
                {
                    groupNode.children.Add(new DataScopeTree
                    {
                        key = company.ID.ToString(),
                        title = company.CompanyName,
                        value = company.ID,
                        isLeaf = true
                    });
                }

                tree.Add(groupNode);
            }

            return Success(tree);
        }
        catch (Exception ex)
        {
            return Failed<List<DataScopeTree>>($"获取数据权限树失败: {ex.Message}");
        }
    }

    #endregion

    #region 更新角色数据权限

    /// <summary>
    /// 更新角色数据权限（直接接收公司ID列表）
    /// </summary>
    public async Task<ServiceResult> UpdateDataScope(Guid roleId, List<Guid> scopeKeys)
    {
        try
        {
            // 0. 输入验证
            if (scopeKeys == null)
            {
                return Failed("请求参数不能为空");
            }

            // 验证 GUID 格式
            var companyIds = scopeKeys.Distinct().ToList();

            // 1. 并发控制检查
            if (!await Db.Queryable<SmRoles>().Where(x => x.ID == roleId).AnyAsync())
                return Failed("角色不存在");

            // 2. 获取旧值（用于审计日志）
            var oldValue = await GetRoleDataScope(roleId);

            // 3. 开启事务
            await Db.Ado.BeginTranAsync();

            try
            {
                // 4. 删除旧权限
                await Db.Deleteable<SmRoleDataScope>()
                    .Where(x => x.SmRoleId == roleId)
                    .ExecuteCommandAsync();

                // 5. 插入新权限
                if (companyIds.Any())
                {
                    var newScopes = companyIds.Select(companyId => new SmRoleDataScope
                    {
                        SmRoleId = roleId,
                        CompanyId = companyId
                    }).ToList();
                    await Db.Insertable(newScopes).ExecuteCommandAsync();
                }

                // 6. 提交事务
                await Db.Ado.CommitTranAsync();

                // 7. 记录审计日志
                await AddAuditLog(new SmRoleDataScopeAudit
                {
                    SmRoleId = roleId,
                    Action = "Update",
                    OldValue = JsonConvert.SerializeObject(oldValue.Data),
                    NewValue = JsonConvert.SerializeObject(companyIds),
                    OperatedBy = UserContext.Current.User_Id,
                    OperatedTime = DateTime.Now,
                    IpAddress = HttpContextExtension.GetUserIp(HttpUseContext.Current),
                    UserAgent = HttpUseContext.Current?.Request?.Headers["User-Agent"].ToString(),
                    IsSuccess = true,
                    Reason = "更新数据权限"
                });

                // 8. 清除受影响用户的缓存
                await ClearAffectedUsersCache(roleId);

                return Success();
            }
            catch (Exception)
            {
                await Db.Ado.RollbackTranAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            return Failed($"更新失败: {ex.Message}");
        }
    }
    #endregion

    #region 获取角色数据权限

    /// <summary>
    /// 获取角色的数据权限（直接返回公司ID列表）
    /// </summary>
    public async Task<ServiceResult<List<Guid>>> GetRoleDataScope(Guid roleId)
    {
        try
        {
            // 直接查询并返回公司ID
            var companyIds = await Db.Queryable<SmRoleDataScope>()
                .Where(x => x.SmRoleId == roleId && x.CompanyId != null)
                .Select(x => x.CompanyId.Value)
                .ToListAsync();

            return Success(companyIds);
        }
        catch (Exception ex)
        {
            return Failed<List<Guid>>($"获取角色数据权限失败: {ex.Message}");
        }
    }

    #endregion

    #region 获取用户数据权限

    /// <summary>
    /// 获取用户的实际数据范围
    /// </summary>
    public async Task<ServiceResult<UserDataScopeModel>> GetUserDataScope(Guid userId)
    {
        try
        {
            var model = new UserDataScopeModel();

            // 1. 获取用户的所有角色
            var userRoles = await Db.Queryable<SmUserRole>()
                .Where(x => x.SmUserId == userId)
                .Select(x => x.SmRoleId)
                .ToListAsync();

            // 场景1：用户没有角色 → 返回空列表（拒绝访问）
            if (!userRoles.Any())
            {
                return Success(model);
            }

            // 2. 获取这些角色的数据权限（公司 ID 列表）
            var companyIds = await Db.Queryable<SmRoleDataScope>()
                .Where(x => userRoles.Contains(x.SmRoleId))
                .Select(x => x.CompanyId.Value)
                .Distinct()
                .ToListAsync();

            // 场景2：有角色但无数据权限 → 返回空列表（拒绝访问）
            // 场景3：有数据权限 → 返回公司ID列表
            model.CompanyIds = companyIds;

            return Success(model);
        }
        catch (Exception ex)
        {
            return Failed<UserDataScopeModel>($"获取用户数据权限失败: {ex.Message}");
        }
    }

    #endregion

    #region 审计日志

    /// <summary>
    /// 添加审计日志
    /// </summary>
    private async Task AddAuditLog(SmRoleDataScopeAudit audit)
    {
        try
        {
            await Db.Insertable(audit).ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            // 审计日志记录失败不影响主业务流程
            Console.WriteLine($"审计日志记录失败: {ex.Message}");
        }
    }

    #endregion

    #region 缓存清除

    /// <summary>
    /// 清除受影响用户的缓存
    /// </summary>
    private async Task ClearAffectedUsersCache(Guid roleId)
    {
        try
        {
            var userIds = await Db.Queryable<SmUserRole>()
                .Where(x => x.SmUserId != null && x.SmRoleId == roleId)
                .Select(x => x.SmUserId.Value)
                .ToListAsync();

            foreach (var userId in userIds)
            {
                DataScopeHelper.ClearCache(userId);
            }
        }
        catch (Exception ex)
        {
            // 缓存清除失败不影响主业务流程
            Console.WriteLine($"缓存清除失败: {ex.Message}");
        }
    }

    #endregion
}
