﻿/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmImportDataDetail.cs
*
*功 能： N / A
* 类 名： SmImportDataDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/22 9:46:31  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 系统导入数据明细 (服务)
/// </summary>
public class SmImportDataDetailServices : BaseServices<SmImportDataDetail, SmImportDataDetailDto, InsertSmImportDataDetailInput, EditSmImportDataDetailInput>, ISmImportDataDetailServices
{
    private readonly IBaseRepository<SmImportDataDetail> _dal;
    public SmImportDataDetailServices(IBaseRepository<SmImportDataDetail> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }
}