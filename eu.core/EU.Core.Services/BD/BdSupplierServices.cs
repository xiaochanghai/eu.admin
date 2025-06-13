/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdSupplier.cs
*
*功 能： N / A
* 类 名： BdSupplier
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/25 19:20:50  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 供应商 (服务)
/// </summary>
public class BdSupplierServices : BaseServices<BdSupplier, BdSupplierDto, InsertBdSupplierInput, EditBdSupplierInput>, IBdSupplierServices
{
    private readonly IBaseRepository<BdSupplier> _dal;
    public BdSupplierServices(IBaseRepository<BdSupplier> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }
}