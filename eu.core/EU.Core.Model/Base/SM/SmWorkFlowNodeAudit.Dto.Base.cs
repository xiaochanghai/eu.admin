/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmWorkFlowNodeAudit.cs
*
* 功 能： N / A
* 类 名： SmWorkFlowNodeAudit
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/11/15 21:58:09  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 工作流节点审核人员 (Dto.Base)
/// </summary>
public class SmWorkFlowNodeAuditBase : BasePoco
{

    /// <summary>
    /// 工作流ID
    /// </summary>
    [Display(Name = "WorkFlowId"), Description("工作流ID")]
    public Guid? WorkFlowId { get; set; }

    /// <summary>
    /// 节点ID
    /// </summary>
    [Display(Name = "NodeId"), Description("节点ID")]
    public Guid? NodeId { get; set; }

    /// <summary>
    /// 审核对象ID
    /// </summary>
    [Display(Name = "ObjectId"), Description("审核对象ID")]
    public Guid? ObjectId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark1"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string ExtRemark1 { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark2"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string ExtRemark2 { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark3"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string ExtRemark3 { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark4"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string ExtRemark4 { get; set; }
}
