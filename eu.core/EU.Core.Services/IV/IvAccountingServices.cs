/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IvAccounting.cs
*
* 功 能： N / A
* 类 名： IvAccounting
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2024/12/18 12:27:55  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 库存建帐 (服务)
/// </summary>
public class IvAccountingServices : BaseServices<IvAccounting, IvAccountingDto, InsertIvAccountingInput, EditIvAccountingInput>, IIvAccountingServices
{
    private readonly IBaseRepository<IvAccounting> _dal;
    public IvAccountingServices(IBaseRepository<IvAccounting> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }
}