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
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Services;

/// <summary>
/// 角色模块权限 (服务)
/// </summary>
public class SmRoleModuleServices : BaseServices<SmRoleModule, SmRoleModuleDto, InsertSmRoleModuleInput, EditSmRoleModuleInput>, ISmRoleModuleServices
{
    private readonly IBaseRepository<SmRoleModule> _dal;
    private List<InsertSmRoleModuleInput> inserts = new List<InsertSmRoleModuleInput>();


    ISmModulesServices _smModulesServices;
    public SmRoleModuleServices(IBaseRepository<SmRoleModule> dal, ISmModulesServices smModulesServices, DataContext context)
    {
        this._dal = dal;
        base.BaseDal = dal;
        _smModulesServices = smModulesServices;
    }

    #region 批量保存角色数据
    public async Task<ServiceResult> BatchInsertRoleModule(RoleModuleVM roleModuleVm)
    {
        await Db.Ado.BeginTranAsync();
        try
        {
            inserts = new List<InsertSmRoleModuleInput>();

            var moduleList = roleModuleVm.ModuleList.Distinct().ToList();
            var roleId = roleModuleVm.RoleId;
            if (moduleList.Contains("All"))
            {
                var data = await _smModulesServices.Query(x => x.IsDeleted == false);
                inserts = data.Select(x => new InsertSmRoleModuleInput
                {
                    SmModuleId = x.ID,
                    SmRoleId = roleId,
                }).ToList();
            }
            else
            {
                await Db.Ado.ExecuteCommandAsync("DELETE FROM SmRoleModule WHERE SmRoleId='" + roleId + "'");
                inserts = moduleList.Select(x => new InsertSmRoleModuleInput()
                {
                    SmModuleId = Guid.Parse(x),
                    SmRoleId = roleId,
                }).ToList();
                //for (int i = 0; i < moduleList.Count; i++)
                //{
                //    //如果是父目录
                //    //var result = await _smModulesServices.QueryById(Guid.Parse(moduleList[i].ToString()));
                //    var result = ModuleList.Where(x => x.ID == Guid.Parse(moduleList[i].ToString())).SingleOrDefault();

                //    if (result.IsParent == true)
                //    {
                //        await SaveParentModule(roleId, result.ID);
                //    }
                //    else
                //    {
                //        inserts.Add(new InsertSmRoleModuleInput
                //        {
                //            SmModuleId = Guid.Parse(moduleList[i].ToString()),
                //            SmRoleId = roleId,
                //        });
                //    }
                //}
            }
            await Add(inserts);
            await Db.Ado.CommitTranAsync();

        }
        catch (Exception)
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
        return Success("角色模块保存成功！");
    }

    public async Task<ServiceResult> UpdateRoleModule(Guid RoleId, List<string> ModuleList)
    {
        await Db.Ado.BeginTranAsync();
        try
        {
            inserts = new List<InsertSmRoleModuleInput>();
            var roleFunctions = new List<SmRoleFunction>();
            var keyList = ModuleList.Distinct().ToList();
            if (keyList.Contains("All"))
            {
                var data = await _smModulesServices.Query(x => x.IsDeleted == false);
                inserts = data.Select(x => new InsertSmRoleModuleInput
                {
                    SmModuleId = x.ID,
                    SmRoleId = RoleId,
                }).ToList();
            }
            else
            {
                await Delete(x => x.SmRoleId == RoleId);
                await Db.Deleteable<SmRoleFunction>().Where(it => it.SmRoleId == RoleId).ExecuteCommandAsync();

                roleFunctions = keyList.Where(x => x.Contains("functionPrivileges_"))
                    .Select(x => new SmRoleFunction()
                    {
                        SmFunctionId = Guid.Parse(x.Replace("functionPrivileges_", null)),
                        SmRoleId = RoleId,
                        SmModuleId = FunctionPrivilege.Query(Guid.Parse(x.Replace("functionPrivileges_", null)))?.SmModuleId
                    }).ToList();

                keyList.Where(x => x.Contains("CommonOption_")).ToList()
                    .ForEach(x =>
                    {
                        var array = x.Split('_');
                        roleFunctions.Add(new SmRoleFunction()
                        {
                            SmModuleId = Guid.Parse(array[2]),
                            SmRoleId = RoleId,
                            ActionCode = array[1]
                        });
                    });

                var queryRoleFunctions = roleFunctions.Where(x => x.ActionCode == "Query").ToList();
                var modules = ModuleInfo.GetModuleList();

                var moduleIds = queryRoleFunctions.Select(x => x.SmModuleId).ToList();

                queryRoleFunctions.ForEach(x =>
                {
                    LoopToAppendParentModuleId(modules, x.SmModuleId, moduleIds);
                });
                moduleIds = moduleIds.Distinct().ToList();

                inserts = moduleIds
                .Select(x => new InsertSmRoleModuleInput()
                {
                    SmModuleId = x,
                    SmRoleId = RoleId,
                }).ToList();
                await Db.Insertable(roleFunctions).ExecuteCommandAsync();
            }
            await Add(inserts);
            await Db.Ado.CommitTranAsync();
        }
        catch (Exception)
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
        Utility.ReInitCache();

        return Success("角色模块保存成功！");
    }

    public void LoopToAppendParentModuleId(List<SmModules> modules, Guid? moduleId, List<Guid?> moduleIds)
    {
        var module = modules.FirstOrDefault(x => x.ID == moduleId);
        if (module.ParentId != null && module.IsDetail != true)
        {
            moduleIds.Add(module.ParentId);
            LoopToAppendParentModuleId(modules, module.ParentId, moduleIds);
        }
    }
    #endregion

    #region 获取角色模块数据
    public async Task<ServiceResult<List<string>>> GetRoleModule(Guid roleId)
    {
        string sql = @$"SELECT A.*
                        FROM SmRoleFunction A
                             JOIN SmModules B ON A.SmModuleId = B.ID AND B.IsDeleted = 'false'
                        WHERE     A.SmRoleId = '{roleId}'
                              AND A.IsDeleted = 'false'";
        var roleFunctions = await Db.Ado.SqlQueryAsync<SmRoleFunction>(sql);

        var ids = roleFunctions.Where(x => x.SmModuleId != null && x.ActionCode != null).Select(x => "CommonOption_" + x.ActionCode + "_" + x.SmModuleId).ToList();

        ids.AddRange(roleFunctions.Where(x => x.SmFunctionId != null).Select(x => "functionPrivileges_" + x.SmFunctionId).Distinct());
        var ids1 = await Db.Queryable<SmRoleModule>().Where(x => x.SmRoleId == roleId).Select(x => x.SmModuleId).Distinct().ToListAsync();
        ids.AddRange(ids1.Select(x => x.ObjToString()));
        return ServiceResult<List<string>>.OprateSuccess(ids, ResponseText.QUERY_SUCCESS);
    }
    #endregion

    #region 获取模块数据
    public async Task<ServiceResult<ModuleTree>> GetAllModuleList()
    {
        var moduleTree = new ModuleTree();
        moduleTree.key = "All";
        moduleTree.title = "请选择角色模块";

        var smModules = ModuleInfo.GetModuleList().OrderBy(x => x.TaxisNo)
            .ThenBy(x => x.ModuleName)
            .Where(x => x.IsDeleted == false)
            .ToList();

        var functionPrivileges = await Db.Queryable<SmFunctionPrivilege>().Where(x => x.IsDeleted == false)
            .Select(z => new SmFunctionPrivilege
            {
                ID = z.ID,
                SmModuleId = z.SmModuleId,
                FunctionCode = z.FunctionCode,
                FunctionName = z.FunctionName
            }).ToListAsync();

        LoopToAppendChildren(smModules, moduleTree, functionPrivileges);
        //moduleTree.children = moduleTree.children.Where(x => x.title == "基础档案").ToList();
        return Success(moduleTree, ResponseText.QUERY_SUCCESS);
    }
    public void LoopToAppendChildren(List<SmModules> smModules, ModuleTree moduleTree, List<SmFunctionPrivilege> functionPrivileges)
    {
        var subItems = new List<ModuleTree>();
        if (moduleTree.key == "All")
        {
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
            subItems = smModules
                .Where(x => x.ParentId == Guid.Parse(moduleTree.key))
                .Select(y => new ModuleTree
                {
                    title = y.IsDetail == true && y.BelongModuleId != null ? ModuleInfo.GetModuleNameById(y.BelongModuleId) + "/" + y.ModuleName : y.ModuleName,
                    key = y.ID.ToString(),
                    isLeaf = y.IsParent != null ? !y.IsParent : true
                }).ToList();
        }
        moduleTree.children = [.. subItems];
        foreach (var subItem in subItems)
        {

            LoopToAppendChildren(smModules, subItem, functionPrivileges);
        }
        if (moduleTree.isLeaf == true)
        {
            var module = smModules.Where(x => x.ID == Guid.Parse(moduleTree.key)).First();
            // 定义所有操作
            Dictionary<string, string> dict = new Dictionary<string, string>
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

            // 最终结果
            var actions = dict
                .Where(kvp => !filters.Any(f => f(kvp.Key)))
                .ToList();

            var functionData = actions.Select(x =>
                new ModuleTree
                {
                    key = "CommonOption_" + x.Key + "_" + moduleTree.key,
                    title = x.Value,
                    isLeaf = true
                }).ToList();

            moduleTree.children = functionData;
            if (functionPrivileges.Any(o => o.SmModuleId != null && o.SmModuleId.ToString() == moduleTree.key))
            {
                if (moduleTree.children is null)
                    moduleTree.children = functionPrivileges.Where(o => o.SmModuleId != null && o.SmModuleId.ToString() == moduleTree.key)
                    .Select(y => new ModuleTree
                    {
                        title = y.FunctionName,
                        key = "functionPrivileges_" + y.ID.ToString(),
                        isLeaf = true
                    }).ToList();
                else moduleTree.children.AddRange(functionPrivileges.Where(o => o.SmModuleId != null && o.SmModuleId.ToString() == moduleTree.key)
                    .Select(y => new ModuleTree
                    {
                        title = y.FunctionName,
                        key = "functionPrivileges_" + y.ID.ToString(),
                        isLeaf = true
                    }).ToList());
            }

            moduleTree.isLeaf = false;
        }
    }
    #endregion
}