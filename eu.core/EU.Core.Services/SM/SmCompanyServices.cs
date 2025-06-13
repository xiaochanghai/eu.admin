/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmCompany.cs
*
*功 能： N / A
* 类 名： SmCompany
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/24 16:25:45  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 组织 (服务)
/// </summary>
public class SmCompanyServices : BaseServices<SmCompany, SmCompanyDto, InsertSmCompanyInput, EditSmCompanyInput>, ISmCompanyServices
{
    private readonly IBaseRepository<SmCompany> _dal;
    public SmCompanyServices(IBaseRepository<SmCompany> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }
}