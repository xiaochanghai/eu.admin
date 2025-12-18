/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdSupplierLevel.cs
*
* 功 能： N / A
* 类 名： BdSupplierLevel
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/12/18 22:25:06  SahHsiao   初版
*
* Copyright(c) 2025 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 供应商等级 (服务)
/// </summary>
public class BdSupplierLevelServices : BaseServices<BdSupplierLevel, BdSupplierLevelDto, InsertBdSupplierLevelInput, EditBdSupplierLevelInput>, IBdSupplierLevelServices
{
    public BdSupplierLevelServices(IBaseRepository<BdSupplierLevel> dal)
    {
        BaseDal = dal;
    }
}