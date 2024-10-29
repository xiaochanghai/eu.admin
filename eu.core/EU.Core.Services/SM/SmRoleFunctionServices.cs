/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmRoleFunction.cs
*
*功 能： N / A
* 类 名： SmRoleFunction
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/21 1:05:42  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// SmRoleFunction (服务)
/// </summary>
public class SmRoleFunctionServices : BaseServices<SmRoleFunction, SmRoleFunctionDto, InsertSmRoleFunctionInput, EditSmRoleFunctionInput>, ISmRoleFunctionServices
{
    private readonly IBaseRepository<SmRoleFunction> _dal;
    public SmRoleFunctionServices(IBaseRepository<SmRoleFunction> dal, DataContext context)
    {
        this._dal = dal;
        base.BaseDal = dal;
        base._context = context;
    }

    public async Task<ServiceResult<dynamic>> GetModuleFunction(Guid roleId, Guid moduleId)
    {
        dynamic data = new ExpandoObject();
        var dict = new Dictionary<string, string>
        {
            { "Add", "新建" },
            { "Update", "修改" },
            { "View", "查看" },
            { "Delete", "删除" },
            { "BatchDelete", "批量删除" },
            { "ExportExcel", "导出Excel" },
            { "ImportExcel", "导入Excel" },
        };
        var functionData = dict.ToList().Select(x => new functionData { value = x.Key, label = x.Value }).ToList();

        var noAction = await _context.SmRoleFunction.AsNoTracking().Where(x => x.IsDeleted == false && x.SmRoleId == roleId && x.SmModuleId == moduleId)
            .Select(y => y.ActionCode)
            .ToListAsync();

        data.checkValue = noAction;
        data.functionData = functionData;
        return ServiceResult<dynamic>.OprateSuccess(data, ResponseText.QUERY_SUCCESS);

    }

    public async Task<ServiceResult> SaveModuleFunction(RoleFuncVM roleFuncVm)
    {
        var adds = new List<InsertSmRoleFunctionInput>();
        var noAction = await _context.SmRoleFunction.AsNoTracking()
            .Where(x => x.IsDeleted == false && x.SmRoleId == roleFuncVm.RoleId && x.SmModuleId == roleFuncVm.SmModuleId)
            .Select(x => new { x.ID, x.ActionCode })
            .ToListAsync();

        roleFuncVm.RoleFuncData.ForEach(x =>
        {
            if (!noAction.Any(o => o.ActionCode == x))
                adds.Add(new InsertSmRoleFunctionInput()
                {
                    SmRoleId = roleFuncVm.RoleId,
                    SmModuleId = roleFuncVm.SmModuleId,
                    ActionCode = x
                });
        });
        var ids = noAction.Where(x => !roleFuncVm.RoleFuncData.Contains(x.ActionCode)).Select(x => x.ID).ToList();
        if (ids.Any())
            await base.Delete(x => ids.Contains(x.ID));
        if (adds.Any())
            await base.Add(adds);
        new RedisCacheService(1).Clear();
        return ServiceResult.OprateSuccess(ResponseText.UPDATE_SUCCESS);

    }

    public async Task<ServiceResult<List<Guid?>>> GetRoleFuncPriv(Guid RoleId)
    {
        var ids = await _context.SmRoleFunction
                .Where(x => x.IsDeleted == false && x.SmRoleId == RoleId && x.SmFunctionId != null)
                .Select(y => y.SmFunctionId).ToListAsync();
        return ServiceResult<List<Guid?>>.OprateSuccess(ids, ResponseText.QUERY_SUCCESS, ids.Count);
    }

    public async Task<ServiceResult<DataTree>> GetAllFuncPriv()
    {
        var roleTree = new DataTree();
        roleTree.key = "All";
        roleTree.title = "请选择功能定义";
        //roleTree.children = await _context.SmFunctionPrivilege
        //    .OrderBy(x => x.SmModule.ModuleName)
        //    .ThenBy(x => x.FunctionName)
        //    .Where(x => x.IsDeleted == false)
        //    .Select(y => new DataTree
        //    {
        //        title = y.SmModule.ModuleName + "/" + y.FunctionName,
        //        key = y.ID.ToString().ToLower(),
        //        isLeaf = true
        //    }).ToListAsync();

        roleTree.children = await _context.SmFunctionPrivilege.Where(x => x.IsDeleted == false)
           .Join(_context.SmModules, x => x.SmModuleId, y => y.ID, (x, y) => new { x, y })
           .Where(z => z.y.IsDeleted == false && z.y.IsActive == true && z.y.IsParent == false)
           .Select(z => new DataTree
           {
               title = z.y.ModuleName + "/" + z.x.FunctionName,
               key = z.x.ID.ToString().ToLower(),
               isLeaf = true
           }).ToListAsync();

        return ServiceResult<DataTree>.OprateSuccess(roleTree, ResponseText.QUERY_SUCCESS);
    }

    public async Task<ServiceResult> SaveRoleFuncPriv(RoleFuncPric roleFuncPric)
    {
        var functionList = roleFuncPric.FunctionList;
        var roleId = roleFuncPric.RoleId;

        if (functionList.Contains("All"))
            functionList.Remove("All");

        //删除不包含新数据的数据
        var deleteFuncPrivs = await _context.SmRoleFunction
            .Where(x => x.IsDeleted == false && x.SmFunctionId != null && x.SmRoleId == roleId && !functionList.Contains(x.SmFunctionId.ToString())).ToListAsync();
        for (int i = 0; i < deleteFuncPrivs.Count; i++)
        {
            deleteFuncPrivs[i].IsDeleted = true;
            _context.Update(deleteFuncPrivs[i]);
        }

        //查询相同的数据
        var sameFuncPriv = await _context.SmRoleFunction
            .Where(x => x.IsDeleted == false && x.SmFunctionId != null && x.SmRoleId == roleId && functionList.Contains(x.SmFunctionId.ToString())).ToListAsync();
        for (int i = 0; i < sameFuncPriv.Count; i++)
        {
            functionList.Remove(sameFuncPriv[i].SmFunctionId.ToString());
        }

        //添加剩下的数据
        var functions = new List<SmRoleFunction>();
        for (int i = 0; i < functionList.Count; i++)
        {
            var smRoleFunction = new SmRoleFunction();
            smRoleFunction.SmRoleId = roleId;
            smRoleFunction.SmFunctionId = Guid.Parse(functionList[i]);
            functions.Add(smRoleFunction);
        }

        await _context.AddRangeAsync(functions);
        await _context.SaveChangesAsync();
        return ServiceResult.OprateSuccess("功能定义保存成功！");

    }
}