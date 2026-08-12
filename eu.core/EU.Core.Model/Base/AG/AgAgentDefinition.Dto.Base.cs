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

namespace EU.Core.Model.Base;

/// <summary>
/// Agent 定义主表，保存 Agent 身份、名称、说明、运行状态和逻辑版本 (Dto.Base)
/// </summary>
public class AgAgentDefinitionBase : BasePoco
{

    /// <summary>
    /// Agent 唯一编码
    /// </summary>
    [Display(Name = "Code"), Description("Agent 唯一编码"), MaxLength(128, ErrorMessage = "Agent 唯一编码 不能超过 128 个字符")]
    public string Code { get; set; }

    /// <summary>
    /// 逻辑修订号，用于乐观并发控制
    /// </summary>
    [Display(Name = "LogicalRevision"), Description("逻辑修订号，用于乐观并发控制")]
    public long? LogicalRevision { get; set; }

    /// <summary>
    /// Agent 显示名称
    /// </summary>
    [Display(Name = "Name"), Description("Agent 显示名称"), MaxLength(256, ErrorMessage = "Agent 显示名称 不能超过 256 个字符")]
    public string Name { get; set; }

    /// <summary>
    /// Agent 功能说明
    /// </summary>
    [Display(Name = "Description"), Description("Agent 功能说明"), MaxLength(-1, ErrorMessage = "Agent 功能说明 不能超过 -1 个字符")]
    public string Description { get; set; }

    /// <summary>
    /// 运行状态：Enabled、Disabled 或 Archived
    /// </summary>
    [Display(Name = "RuntimeStatus"), Description("运行状态：Enabled、Disabled 或 Archived"), MaxLength(32, ErrorMessage = "运行状态：Enabled、Disabled 或 Archived 不能超过 32 个字符")]
    public string RuntimeStatus { get; set; }
}
