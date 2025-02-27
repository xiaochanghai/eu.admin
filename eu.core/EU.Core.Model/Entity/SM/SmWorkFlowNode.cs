/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmWorkFlowNode.cs
*
* 功 能： N / A
* 类 名： SmWorkFlowNode
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:31:17  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 工作流节点 (Model)
/// </summary>
[SugarTable("SmWorkFlowNode", "工作流节点"), Entity(TableCnName = "工作流节点", TableName = "SmWorkFlowNode")]
public class SmWorkFlowNode : BasePoco
{

    /// <summary>
    /// 工作流ID
    /// </summary>
    [Display(Name = "WorkFlowId"), Description("工作流ID"), SugarColumn(IsNullable = true)]
    public Guid? WorkFlowId { get; set; }

    /// <summary>
    /// 上级节点ID
    /// </summary>
    [Display(Name = "ParentNodeId"), Description("上级节点ID"), SugarColumn(IsNullable = true, Length = 64)]
    public string ParentNodeId { get; set; }

    /// <summary>
    /// 节点ID
    /// </summary>
    [Display(Name = "NodeId"), Description("节点ID"), SugarColumn(IsNullable = true, Length = 64)]
    public string NodeId { get; set; }

    /// <summary>
    /// 节点类型
    /// </summary>
    [Display(Name = "NodeType"), Description("节点类型"), SugarColumn(IsNullable = true, Length = 32)]
    public string NodeType { get; set; }

    /// <summary>
    /// 节点名
    /// </summary>
    [Display(Name = "NodeName"), Description("节点名"), SugarColumn(IsNullable = true, Length = 32)]
    public string NodeName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark1"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string ExtRemark1 { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark2"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string ExtRemark2 { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark3"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string ExtRemark3 { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark4"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string ExtRemark4 { get; set; }
}
