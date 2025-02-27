/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmWorkFlow.cs
*
* 功 能： N / A
* 类 名： SmWorkFlow
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 9:26:26  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 工作流 (Dto.Base)
/// </summary>
public class SmWorkFlowBase : BasePoco
{

    /// <summary>
    /// 模块ID
    /// </summary>
    [Display(Name = "SmModuleId"), Description("模块ID"), SugarColumn(IsNullable = true)]
    public Guid? SmModuleId { get; set; }

    /// <summary>
    /// 流程代码
    /// </summary>
    [Display(Name = "FlowCode"), Description("流程代码"), MaxLength(10, ErrorMessage = "流程代码 不能超过 10 个字符"), SugarColumn(IsNullable = true)]
    public string FlowCode { get; set; }

    /// <summary>
    /// 流程名
    /// </summary>
    [Display(Name = "FlowName"), Description("流程名"), MaxLength(10, ErrorMessage = "流程名 不能超过 10 个字符"), SugarColumn(IsNullable = true)]
    public string FlowName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string Remark { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark1"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string ExtRemark1 { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark2"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string ExtRemark2 { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark3"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string ExtRemark3 { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark4"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string ExtRemark4 { get; set; }
}
