/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PdOrderProcess.cs
*
*功 能： N / A
* 类 名： PdOrderProcess
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 14:39:53  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 生产工单-工艺路线
/// </summary>
[GlobalActionFilter, ApiExplorerSettings(GroupName = Grouping.GroupName_PD)]
public class PdOrderProcessController : BaseController1<PdOrderProcess>
{
    /// <summary>
    /// 生产工单-工艺路线
    /// </summary>
    /// <param name="_context"></param>
    /// <param name="BaseCrud"></param>
    public PdOrderProcessController(DataContext _context, IBaseCRUDVM<PdOrderProcess> BaseCrud) : base(_context, BaseCrud)
    {
    }
}