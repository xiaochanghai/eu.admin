/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* AgAgentDefinition.cs
*
* 功 能： N / A
* 类 名： AgAgentDefinition
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2026/8/12 1:00:19  SahHsiao   初版
*
* Copyright(c) 2026 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
///  Agent 定义表 (Model)
/// </summary>
[SugarTable("AgAgentDefinition", " Agent 定义表"), Entity(TableCnName = " Agent 定义表", TableName = "AgAgentDefinition")]
public class AgAgentDefinition : BasePoco
{
    /// <summary>
    /// Agent code
    /// </summary>
    [Display(Name = "Code"), Description("Agent code"), SugarColumn(Length = 128)]
    public string Code { get; set; }

    /// <summary>
    /// Agent name
    /// </summary>
    [Display(Name = "Name"), Description("Agent name"), SugarColumn(Length = 256)]
    public string Name { get; set; }

    /// <summary>
    /// Agent description
    /// </summary>
    [Display(Name = "Description"), Description("Agent description"), SugarColumn(ColumnDataType = "nvarchar(max)")]
    public string Description { get; set; }

    /// <summary>
    /// Enabled, Disabled, or Archived
    /// </summary>
    [Display(Name = "RuntimeStatus"), Description("Runtime status"), SugarColumn(Length = 32)]
    public string RuntimeStatus { get; set; }

    /// <summary>
    /// Optimistic concurrency revision
    /// </summary>
    [Display(Name = "LogicalRevision"), Description("Logical revision")]
    public long LogicalRevision { get; set; }
}
