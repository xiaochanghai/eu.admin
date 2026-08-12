/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* AgAgentDefinition.cs
*
* 功 能： N / A
* 类 名： AgAgentDefinition
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2026/8/12 14:13:35  SahHsiao   初版
*
* Copyright(c) 2026 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// Agent 定义主表，保存 Agent 身份、名称、说明、运行状态和逻辑版本 (Model)
/// </summary>
[SugarTable("AgAgentDefinition", "Agent 定义主表，保存 Agent 身份、名称、说明、运行状态和逻辑版本"), Entity(TableCnName = "Agent 定义主表，保存 Agent 身份、名称、说明、运行状态和逻辑版本", TableName = "AgAgentDefinition")]
public class AgAgentDefinition : BasePoco
{

    /// <summary>
    /// Agent 唯一编码
    /// </summary>
    [Display(Name = "Code"), Description("Agent 唯一编码"), SugarColumn(IsNullable = true, Length = 128)]
    public string Code { get; set; }

    /// <summary>
    /// 逻辑修订号，用于乐观并发控制
    /// </summary>
    [Display(Name = "LogicalRevision"), Description("逻辑修订号，用于乐观并发控制")]
    public long? LogicalRevision { get; set; }

    /// <summary>
    /// Agent 显示名称
    /// </summary>
    [Display(Name = "Name"), Description("Agent 显示名称"), SugarColumn(IsNullable = true, Length = 256)]
    public string Name { get; set; }

    /// <summary>
    /// Agent 功能说明
    /// </summary>
    [Display(Name = "Description"), Description("Agent 功能说明"), SugarColumn(IsNullable = true, Length = -1)]
    public string Description { get; set; }

    /// <summary>
    /// 运行状态：Enabled、Disabled 或 Archived
    /// </summary>
    [Display(Name = "RuntimeStatus"), Description("运行状态：Enabled、Disabled 或 Archived"), SugarColumn(IsNullable = true, Length = 32)]
    public string RuntimeStatus { get; set; }
}
