/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdMaterialType.cs
*
*功 能： N / A
* 类 名： BdMaterialType
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/23 20:13:33  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 物料类型 (服务)
/// </summary>
public class BdMaterialTypeServices : BaseServices<BdMaterialType, BdMaterialTypeDto, InsertBdMaterialTypeInput, EditBdMaterialTypeInput>, IBdMaterialTypeServices
{
    private readonly IBaseRepository<BdMaterialType> _dal;
    public BdMaterialTypeServices(IBaseRepository<BdMaterialType> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    #region 获取物料类型树结构数据
    [NonAction]
    public void LoopToAppendChildren(List<BdMaterialType> list, MaterialTypeTree moduleTree)
    {
        var subItems = new List<MaterialTypeTree>();
        if (moduleTree.key == "All")
        {
            subItems = list.Where(x => x.ParentTypeId == null)
                .OrderBy(x => x.TaxisNo)
                .Select(y => new MaterialTypeTree
                {
                    title = y.MaterialTypeNames,
                    value = y.ID.ToString(),
                    key = y.ID.ToString()
                }).ToList();
        }
        else
        {
            subItems = list.Where(x => x.ParentTypeId == Guid.Parse(moduleTree.key))
                .Select(y => new MaterialTypeTree
                {
                    title = y.MaterialTypeNames,
                    value = y.ID.ToString(),
                    key = y.ID.ToString(),
                }).ToList();
        }
        moduleTree.children = [.. subItems];
        foreach (var subItem in subItems)
        {
            LoopToAppendChildren(list, subItem);
        }
    }

    /// <summary>
    /// 获取物料类型树结构数据
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ServiceResult<MaterialTypeTree>> GetAllMaterialType()
    {
        var moduleTree = new MaterialTypeTree()
        {
            key = "All",
            value = "All",
            title = "物料类型",
            selectable = false
        };

        var list = await base.Query("", "TaxisNo ASC");
        LoopToAppendChildren(list, moduleTree);

        return Success(moduleTree);
    }
    public async Task<ServiceResult<MaterialTypeTree>> QueryClass(Guid classId)
    {
        var type = await base.QuerySingle(x => x.ID == classId);
        if (type != null)
        {
            var moduleTree = new MaterialTypeTree()
            {
                key = type.ID.ToString(),
                value = type.ID.ToString(),
                title = type.MaterialTypeNames,
                selectable = false
            };

            var list = await base.Query("", "TaxisNo ASC");
            LoopToAppendChildren(list, moduleTree);

            return Success(moduleTree);
        }
        else
        {
            var moduleTree = new MaterialTypeTree()
            {
                selectable = false
            };
            return Success(moduleTree);
        }

    }
    #endregion

}