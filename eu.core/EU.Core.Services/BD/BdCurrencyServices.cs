/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdCurrency.cs
*
* 功 能： N / A
* 类 名： BdCurrency
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/2/28 10:54:30  SahHsiao   初版
*
* Copyright(c) 2025 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 币别 (服务)
/// </summary>
public class BdCurrencyServices : BaseServices<BdCurrency, BdCurrencyDto, InsertBdCurrencyInput, EditBdCurrencyInput>, IBdCurrencyServices
{
    public BdCurrencyServices(IBaseRepository<BdCurrency> dal)
    {
        BaseDal = dal;
    }
}