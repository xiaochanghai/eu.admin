﻿/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmApplicationRecord.cs
*
* 功 能： N / A
* 类 名： SmApplicationRecord
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/4/29 22:59:37  SahHsiao   初版
*
* Copyright(c) 2025 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// APP客户端记录 (服务)
/// </summary>
public class SmApplicationRecordServices : BaseServices<SmApplicationRecord, SmApplicationRecordDto, InsertSmApplicationRecordInput, EditSmApplicationRecordInput>, ISmApplicationRecordServices
{
    public SmApplicationRecordServices(IBaseRepository<SmApplicationRecord> dal)
    {
        BaseDal = dal;
    }
}