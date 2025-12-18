/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmRoleModule.cs
*
*功 能： N / A
* 类 名： SmRoleModule
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/23 22:11:39  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Services;

/// <summary>
/// 角色模块权限服务
/// 提供角色与模块之间的权限关系管理功能
/// </summary>
public class SmRoleModuleServices : BaseServices<SmRoleModule, SmRoleModuleDto, InsertSmRoleModuleInput, EditSmRoleModuleInput>, ISmRoleModuleServices
{
    #region 常量定义

    /// <summary>
    /// 全选标识
    /// </summary>
    private const string ALL_SELECTION = "All";

    /// <summary>
    /// 功能权限前缀
    /// </summary>
    private const string FUNCTION_PRIVILEGES_PREFIX = "functionPrivileges_";

    /// <summary>
    /// 通用选项前缀
    /// </summary>
    private const string COMMON_OPTION_PREFIX = "CommonOption_";

    /// <summary>
    /// 分隔符
    /// </summary>
    private const char SEPARATOR = '_';

    #endregion

    #region 字段

    private readonly IBaseRepository<SmRoleModule> _dal;
    private readonly ISmModulesServices _smModulesServices;
    private List<InsertSmRoleModuleInput> _inserts = new List<InsertSmRoleModuleInput>();

    #endregion

    #region 构造函数

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dal">角色模块数据访问层</param>
    /// <param name="smModulesServices">模块服务</param>
    /// <param name="context">数据上下文</param>
    public SmRoleModuleServices(IBaseRepository<SmRoleModule> dal, ISmModulesServices smModulesServices)
    {
        this._dal = dal;
        base.BaseDal = dal;
        this._smModulesServices = smModulesServices;
    }

    #endregion

    #region 批量保存角色数据

    /// <summary>
    /// 批量保存角色模块关联关系
    /// </summary>
    /// <param name="roleModuleVm">角色模块视图模型，包含角色ID和模块ID列表</param>
    /// <returns>保存结果</returns>
    /// <remarks>
    /// 如果模块列表包含"All"，则关联所有未删除的模块；
    /// 否则先删除该角色的现有模块关联，再添加新的关联关系
    /// </remarks>
    public async Task<ServiceResult> BatchInsertRoleModule(RoleModuleVM roleModuleVm)
    {
        await Db.Ado.BeginTranAsync();
        try
        {
            _inserts = new List<InsertSmRoleModuleInput>();

            // 获取去重后的模块列表
            var moduleList = roleModuleVm.ModuleList.Distinct().ToList();
            var roleId = roleModuleVm.RoleId;

            // 判断是否选择全部模块
            if (moduleList.Contains(ALL_SELECTION))
            {
                // 获取所有未删除的模块
                var data = await _smModulesServices.Query(x => x.IsDeleted == false);
                _inserts = data.Select(x => new InsertSmRoleModuleInput
                {
                    SmModuleId = x.ID,
                    SmRoleId = roleId,
                }).ToList();
            }
            else
            {
                // 先删除该角色的现有模块关联
                await Db.Deleteable<SmRoleModule>().Where(it => it.SmRoleId == roleId).ExecuteCommandAsync();

                // 创建新的模块关联列表
                _inserts = moduleList.Select(x => new InsertSmRoleModuleInput()
                {
                    SmModuleId = Guid.Parse(x),
                    SmRoleId = roleId,
                }).ToList();
            }

            // 批量添加角色模块关联
            await Add(_inserts);
            await Db.Ado.CommitTranAsync();
        }
        catch (Exception)
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }

        return Success("角色模块保存成功！");
    }

    /// <summary>
    /// 更新角色模块及功能权限
    /// </summary>
    /// <param name="RoleId">角色ID</param>
    /// <param name="ModuleList">模块及权限键列表</param>
    /// <returns>更新结果</returns>
    /// <remarks>
    /// 支持三种类型的权限键：
    /// 1. "All" - 分配所有模块
    /// 2. "functionPrivileges_{FunctionId}" - 功能权限
    /// 3. "CommonOption_{ActionCode}_{ModuleId}" - 通用操作权限
    /// 同时会自动添加所有父级模块以确保菜单层级完整性
    /// </remarks>
    public async Task<ServiceResult> UpdateRoleModule(Guid RoleId, List<string> ModuleList)
    {
        await Db.Ado.BeginTranAsync();
        try
        {
            _inserts = new List<InsertSmRoleModuleInput>();
            var roleFunctions = new List<SmRoleFunction>();
            var keyList = ModuleList.Distinct().ToList();

            // 判断是否选择全部模块
            if (keyList.Contains(ALL_SELECTION))
            {
                // 获取所有未删除的模块
                var data = await _smModulesServices.Query(x => x.IsDeleted == false);
                _inserts = data.Select(x => new InsertSmRoleModuleInput
                {
                    SmModuleId = x.ID,
                    SmRoleId = RoleId,
                }).ToList();
            }
            else
            {
                // 删除角色的现有模块和功能权限
                await Delete(x => x.SmRoleId == RoleId);
                await Db.Deleteable<SmRoleFunction>().Where(it => it.SmRoleId == RoleId).ExecuteCommandAsync();

                // 处理功能权限（格式：functionPrivileges_{FunctionId}）
                roleFunctions = keyList.Where(x => x.Contains(FUNCTION_PRIVILEGES_PREFIX))
                    .Select(x => new SmRoleFunction()
                    {
                        SmFunctionId = Guid.Parse(x.Replace(FUNCTION_PRIVILEGES_PREFIX, null)),
                        SmRoleId = RoleId,
                        SmModuleId = FunctionPrivilege.Query(Guid.Parse(x.Replace(FUNCTION_PRIVILEGES_PREFIX, null)))?.SmModuleId
                    }).ToList();

                // 处理通用操作权限（格式：CommonOption_{ActionCode}_{ModuleId}）
                keyList.Where(x => x.Contains(COMMON_OPTION_PREFIX)).ToList()
                    .ForEach(x =>
                    {
                        var array = x.Split(SEPARATOR);
                        roleFunctions.Add(new SmRoleFunction()
                        {
                            SmModuleId = Guid.Parse(array[2]),
                            SmRoleId = RoleId,
                            ActionCode = array[1]
                        });
                    });

                // 获取所有具有查询权限的功能
                var queryRoleFunctions = roleFunctions.Where(x => x.ActionCode == "Query").ToList();
                var modules = ModuleInfo.GetModuleList();

                // 收集模块ID列表
                var moduleIds = queryRoleFunctions.Select(x => x.SmModuleId).ToList();

                // 递归添加所有父级模块ID，确保菜单层级完整
                queryRoleFunctions.ForEach(x =>
                {
                    LoopToAppendParentModuleId(modules, x.SmModuleId, moduleIds);
                });

                // 去重模块ID
                moduleIds = moduleIds.Distinct().ToList();

                // 创建模块关联数据
                _inserts = moduleIds
                    .Select(x => new InsertSmRoleModuleInput()
                    {
                        SmModuleId = x,
                        SmRoleId = RoleId,
                    }).ToList();

                // 保存角色功能权限
                await Db.Insertable(roleFunctions).ExecuteCommandAsync();
            }

            // 保存角色模块关联
            await Add(_inserts);
            await Db.Ado.CommitTranAsync();
        }
        catch (Exception)
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }

        // 重新初始化缓存
        Utility.ReInitCache();

        return Success("角色模块保存成功！");
    }

    /// <summary>
    /// 递归添加父级模块ID
    /// </summary>
    /// <param name="modules">所有模块列表</param>
    /// <param name="moduleId">当前模块ID</param>
    /// <param name="moduleIds">模块ID集合（输出参数）</param>
    /// <remarks>
    /// 递归查找并添加当前模块的所有父级模块ID，确保菜单树的完整性
    /// 只有当模块存在父级且不是详情页面时，才会继续递归
    /// </remarks>
    public void LoopToAppendParentModuleId(List<SmModules> modules, Guid? moduleId, List<Guid?> moduleIds)
    {
        var module = modules.FirstOrDefault(x => x.ID == moduleId);
        if (module != null)
        {
            // 如果存在父级模块且不是详情页面，递归添加父级模块
            if (module.ParentId != null && module.IsDetail != true)
            {
                moduleIds.Add(module.ParentId);
                LoopToAppendParentModuleId(modules, module.ParentId, moduleIds);
            }
        }
    }

    #endregion

    #region 获取角色模块数据

    /// <summary>
    /// 获取指定角色的模块和权限列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>权限键列表</returns>
    /// <remarks>
    /// 返回的权限键包括三种格式：
    /// 1. "CommonOption_{ActionCode}_{ModuleId}" - 通用操作权限
    /// 2. "functionPrivileges_{FunctionId}" - 功能权限
    /// 3. "{ModuleId}" - 模块ID
    /// </remarks>
    public async Task<ServiceResult<List<string>>> GetRoleModule(Guid roleId)
    {
        // 查询角色的功能权限
        var roleFunctions = await Db.Queryable<SmRoleFunction>()
            .LeftJoin<SmModules>((a, b) => a.SmModuleId == b.ID && b.IsDeleted == false)
            .Where((a, b) => a.SmRoleId == roleId)
            .Select((a, b) => new { a.SmModuleId, a.ActionCode, a.SmFunctionId })
            .ToListAsync();

        // 构建通用操作权限键列表（格式：CommonOption_{ActionCode}_{ModuleId}）
        var ids = roleFunctions
            .Where(x => x.SmModuleId != null && x.ActionCode != null)
            .Select(x => COMMON_OPTION_PREFIX + x.ActionCode + SEPARATOR + x.SmModuleId)
            .ToList();

        // 添加功能权限键（格式：functionPrivileges_{FunctionId}）
        ids.AddRange(roleFunctions
            .Where(x => x.SmFunctionId != null)
            .Select(x => FUNCTION_PRIVILEGES_PREFIX + x.SmFunctionId)
            .Distinct());

        // 添加角色关联的模块ID
        var ids1 = await Db.Queryable<SmRoleModule>()
            .Where(x => x.SmRoleId == roleId)
            .Select(x => x.SmModuleId)
            .Distinct()
            .ToListAsync();
        ids.AddRange(ids1.Select(x => x.ObjToString()));

        return Success(ids, ResponseText.QUERY_SUCCESS);
    }

    #endregion

    #region 获取模块数据

    /// <summary>
    /// 获取所有模块的树形结构
    /// </summary>
    /// <returns>模块树形结构</returns>
    /// <remarks>
    /// 返回包含所有模块及其权限的树形结构，用于角色权限分配界面
    /// 叶子节点包含该模块的所有可用操作权限和功能权限
    /// </remarks>
    public async Task<ServiceResult<ModuleTree>> GetAllModuleList()
    {
        // 初始化根节点
        var moduleTree = new ModuleTree
        {
            key = ALL_SELECTION,
            title = "请选择角色模块"
        };

        // 获取所有未删除的模块并排序
        var smModules = ModuleInfo.GetModuleList()
            .OrderBy(x => x.TaxisNo)
            .ThenBy(x => x.ModuleName)
            .Where(x => x.IsDeleted == false)
            .ToList();

        // 获取所有功能权限
        var functionPrivileges = await Db.Queryable<SmFunctionPrivilege>()
            .Where(x => x.IsDeleted == false)
            .Select(z => new SmFunctionPrivilege
            {
                ID = z.ID,
                SmModuleId = z.SmModuleId,
                FunctionCode = z.FunctionCode,
                FunctionName = z.FunctionName
            })
            .ToListAsync();

        // 递归构建树形结构
        LoopToAppendChildren(smModules, moduleTree, functionPrivileges);

        return Success(moduleTree, ResponseText.QUERY_SUCCESS);
    }
    /// <summary>
    /// 递归构建模块树的子节点
    /// </summary>
    /// <param name="smModules">所有模块列表</param>
    /// <param name="moduleTree">当前模块树节点</param>
    /// <param name="functionPrivileges">功能权限列表</param>
    /// <remarks>
    /// 递归构建模块树结构，对于叶子节点会添加操作权限和功能权限作为子节点
    /// 操作权限包括：查询、新建、修改、查看明细、删除、批量删除、审核、撤销、导出Excel、导入Excel
    /// 根据模块配置的显示标识动态控制显示哪些操作权限
    /// </remarks>
    public void LoopToAppendChildren(List<SmModules> smModules, ModuleTree moduleTree, List<SmFunctionPrivilege> functionPrivileges)
    {
        var subItems = new List<ModuleTree>();

        // 根据当前节点类型获取子模块
        if (moduleTree.key == ALL_SELECTION)
        {
            // 获取顶级父模块（没有父级的模块）
            subItems = smModules
                .Where(x => x.IsParent == true && string.IsNullOrEmpty(x.ParentId.ToString()))
                .Select(y => new ModuleTree
                {
                    title = y.ModuleName,
                    key = y.ID.ToString(),
                    isLeaf = y.IsParent != null ? !y.IsParent : true
                }).ToList();
        }
        else
        {
            // 获取当前模块的子模块
            subItems = smModules
                .Where(x => x.ParentId == Guid.Parse(moduleTree.key))
                .Select(y => new ModuleTree
                {
                    // 如果是详情页且有所属模块，显示为 "所属模块/模块名"
                    title = y.IsDetail == true && y.BelongModuleId != null
                        ? ModuleInfo.GetModuleNameById(y.BelongModuleId) + "/" + y.ModuleName
                        : y.ModuleName,
                    key = y.ID.ToString(),
                    isLeaf = y.IsParent != null ? !y.IsParent : true
                }).ToList();
        }

        // 设置子节点
        moduleTree.children = [.. subItems];

        // 递归处理每个子节点
        foreach (var subItem in subItems)
        {
            LoopToAppendChildren(smModules, subItem, functionPrivileges);
        }

        // 如果是叶子节点，添加操作权限和功能权限
        if (moduleTree.isLeaf == true)
        {
            var module = smModules.Where(x => x.ID == Guid.Parse(moduleTree.key)).First();

            // 定义所有可用的操作权限
            var allActions = new Dictionary<string, string>
            {
                { "Query", "查询" },
                { "Add", "新建" },
                { "Update", "修改" },
                { "View", "查看明细" },
                { "Delete", "删除" },
                { "BatchDelete", "批量删除" },
                { "Audit", "审核" },
                { "Revocation", "撤销" },
                { "ExportExcel", "导出Excel" },
                { "ImportExcel", "导入Excel" },
            };

            // 定义过滤条件（根据模块配置控制显示）
            var filters = new List<Func<string, bool>>
            {
                key => module.IsShowAdd != true && key == "Add",
                key => module.IsShowUpdate != true && key == "Update",
                key => module.IsShowDelete != true && key == "Delete",
                key => module.IsShowBatchDelete != true && key == "BatchDelete",
                key => module.IsShowView != true && key == "View",
                key => module.IsShowAudit != true && (key == "Audit" || key == "Revocation"),
                key => module.IsExportExcel != true && key == "ExportExcel",
                key => module.IsImportExcel != true && key == "ImportExcel",
            };

            // 过滤出该模块可用的操作权限
            var availableActions = allActions
                .Where(kvp => !filters.Any(f => f(kvp.Key)))
                .ToList();

            // 构建操作权限树节点
            var actionNodes = availableActions.Select(x =>
                new ModuleTree
                {
                    key = COMMON_OPTION_PREFIX + x.Key + SEPARATOR + moduleTree.key,
                    title = x.Value,
                    isLeaf = true
                }).ToList();

            moduleTree.children = actionNodes;

            // 添加功能权限节点
            var moduleFunctionPrivileges = functionPrivileges
                .Where(o => o.SmModuleId != null && o.SmModuleId.ToString() == moduleTree.key)
                .Select(y => new ModuleTree
                {
                    title = y.FunctionName,
                    key = FUNCTION_PRIVILEGES_PREFIX + y.ID.ToString(),
                    isLeaf = true
                }).ToList();

            if (moduleFunctionPrivileges.Any())
            {
                if (moduleTree.children is null)
                {
                    moduleTree.children = moduleFunctionPrivileges;
                }
                else
                {
                    moduleTree.children.AddRange(moduleFunctionPrivileges);
                }
            }

            // 如果有子节点，设置为非叶子节点
            moduleTree.isLeaf = false;
        }
    }
    #endregion
}