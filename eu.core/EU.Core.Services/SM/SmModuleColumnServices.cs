/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmModuleColumn.cs
*
*功 能： N / A
* 类 名： SmModuleColumn
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/21 0:35:40  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// SmModuleColumn (服务)
/// </summary>
public class SmModuleColumnServices : BaseServices<SmModuleColumn, SmModuleColumnDto, InsertSmModuleColumnInput, EditSmModuleColumnInput>, ISmModuleColumnServices
{
    private readonly IBaseRepository<SmModuleColumn> _dal;
    public SmModuleColumnServices(IBaseRepository<SmModuleColumn> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    #region 新增
    public override async Task<Guid> Add(object entity)
    {
        var model = ConvertToEntity(entity);
        if (string.IsNullOrWhiteSpace(model.FormTitle))
            model.FormTitle = model.Title;
        model.HideInForm = model.HideInForm ?? false;
        entity = JsonHelper.ObjToJson(model);
        var result = await base.Add(entity);

        ModuleSqlColumn.Init();
        return result;
    }
    #endregion

    #region 更新
    public override async Task<bool> Update(Guid Id, object entity)
    {
        var result = await base.Update(Id, entity);

        ModuleSqlColumn.Init();
        return result;
    }
    #endregion
}