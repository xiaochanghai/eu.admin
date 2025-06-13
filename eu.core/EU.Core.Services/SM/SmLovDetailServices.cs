/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmLovDetail.cs
*
*功 能： N / A
* 类 名： SmLovDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/26 14:20:45  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// SmLovDetail (服务)
/// </summary>
public class SmLovDetailServices : BaseServices<SmLovDetail, SmLovDetailDto, InsertSmLovDetailInput, EditSmLovDetailInput>, ISmLovDetailServices
{
    private readonly IBaseRepository<SmLovDetail> _dal;
    public SmLovDetailServices(IBaseRepository<SmLovDetail> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }
}