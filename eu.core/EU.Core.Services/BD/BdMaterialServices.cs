/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdMaterial.cs
*
*功 能： N / A
* 类 名： BdMaterial
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/25 17:53:44  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 物料管理 (服务)
/// </summary>
public class BdMaterialServices : BaseServices<BdMaterial, BdMaterialDto, InsertBdMaterialInput, EditBdMaterialInput>, IBdMaterialServices
{
    private readonly IBaseRepository<BdMaterial> _dal;
    public BdMaterialServices(IBaseRepository<BdMaterial> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }
    public override async Task<BdMaterialDto> QueryDto(object objId, bool blnUseCache = false)
    {
        var data = await Db.Ado.SqlQuerySingleAsync<BdMaterialDto>($"SELECT * FROM BdMaterial_V WHERE ID='{objId}'") ;
        return data;
    }

}