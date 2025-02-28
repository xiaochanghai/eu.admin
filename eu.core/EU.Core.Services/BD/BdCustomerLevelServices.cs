/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdCustomerLevel.cs
*
* 功 能： N / A
* 类 名： BdCustomerLevel
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/2/28 10:57:51  SahHsiao   初版
*
* Copyright(c) 2025 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 客户等级 (服务)
/// </summary>
public class BdCustomerLevelServices : BaseServices<BdCustomerLevel, BdCustomerLevelDto, InsertBdCustomerLevelInput, EditBdCustomerLevelInput>, IBdCustomerLevelServices
{
    public BdCustomerLevelServices(IBaseRepository<BdCustomerLevel> dal)
    {
        BaseDal = dal;
    }
}