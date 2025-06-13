﻿/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdMaterialIVChange.cs
*
*功 能： N / A
* 类 名： BdMaterialIVChange
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 11:49:15  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Services;

/// <summary>
/// BdMaterialIVChange (服务)
/// </summary>
public class BdMaterialIVChangeServices : BaseServices<BdMaterialIVChange, BdMaterialIVChangeDto, InsertBdMaterialIVChangeInput, EditBdMaterialIVChangeInput>, IBdMaterialIVChangeServices
{
    private readonly IBaseRepository<BdMaterialIVChange> _dal;
    public BdMaterialIVChangeServices(IBaseRepository<BdMaterialIVChange> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }
}