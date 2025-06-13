/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdMaterialInventory.cs
*
* 功 能： N / A
* 类 名： BdMaterialInventory
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/1/23 10:39:19  SimonHsiao   初版
*
* Copyright(c) 2025 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 物料库存 (服务)
/// </summary>
public class BdMaterialInventoryServices : BaseServices<BdMaterialInventory, BdMaterialInventoryDto, InsertBdMaterialInventoryInput, EditBdMaterialInventoryInput>, IBdMaterialInventoryServices
{
    private readonly IBaseRepository<BdMaterialInventory> _dal;
    public BdMaterialInventoryServices(IBaseRepository<BdMaterialInventory> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }
}