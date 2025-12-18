/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* EmRepairOrderLog.cs
*
* 功 能： N / A
* 类 名： EmRepairOrderLog
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/11/23 19:30:18  SahHsiao   初版
*
* Copyright(c) 2025 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 报修工单日志 (服务)
/// </summary>
public class EmRepairOrderLogServices : BaseServices<EmRepairOrderLog, EmRepairOrderLogDto, InsertEmRepairOrderLogInput, EditEmRepairOrderLogInput>, IEmRepairOrderLogServices
{
    public EmRepairOrderLogServices(IBaseRepository<EmRepairOrderLog> dal)
    {
        BaseDal = dal;
    }
}