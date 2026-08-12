/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* AgAgentVersion.cs
*
* 功 能： N / A
* 类 名： AgAgentVersion
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2026/8/12 14:16:33  SahHsiao   初版
*
* Copyright(c) 2026 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// Agent 版本表，保存当前草稿和历次已发布版本配置 (Dto.Base)
/// </summary>
public class AgAgentVersionBase : BasePoco
{

    /// <summary>
    /// 所属 Agent 主键，对应 AgAgentDefinition.ID
    /// </summary>
    [Display(Name = "AgentId"), Description("所属 Agent 主键，对应 AgAgentDefinition.ID")]
    public Guid? AgentId { get; set; }

    /// <summary>
    /// 版本排列顺序；草稿固定为 0，发布版本按顺序保存
    /// </summary>
    [Display(Name = "Ordinal"), Description("版本排列顺序；草稿固定为 0，发布版本按顺序保存")]
    public int? Ordinal { get; set; }

    /// <summary>
    /// 版本标签，例如 1.0.0
    /// </summary>
    [Display(Name = "Label"), Description("版本标签，例如 1.0.0"), MaxLength(128, ErrorMessage = "版本标签，例如 1.0.0 不能超过 128 个字符")]
    public string Label { get; set; }

    /// <summary>
    /// 是否为草稿版本；每个 Agent 只能有一个草稿
    /// </summary>
    [Display(Name = "IsDraft"), Description("是否为草稿版本；每个 Agent 只能有一个草稿")]
    public bool? IsDraft { get; set; }

    /// <summary>
    /// Agent 系统指令
    /// </summary>
    [Display(Name = "Instructions"), Description("Agent 系统指令"), MaxLength(-1, ErrorMessage = "Agent 系统指令 不能超过 -1 个字符")]
    public string Instructions { get; set; }

    /// <summary>
    /// 模型配置标识
    /// </summary>
    [Display(Name = "ModelProfileId"), Description("模型配置标识"), MaxLength(256, ErrorMessage = "模型配置标识 不能超过 256 个字符")]
    public string ModelProfileId { get; set; }

    /// <summary>
    /// 输出模式：Text 或 Structured
    /// </summary>
    [Display(Name = "OutputMode"), Description("输出模式：Text 或 Structured"), MaxLength(32, ErrorMessage = "输出模式：Text 或 Structured 不能超过 32 个字符")]
    public string OutputMode { get; set; }

    /// <summary>
    /// 结构化输出使用的 JSON Schema
    /// </summary>
    [Display(Name = "OutputJsonSchema"), Description("结构化输出使用的 JSON Schema"), MaxLength(-1, ErrorMessage = "结构化输出使用的 JSON Schema 不能超过 -1 个字符")]
    public string OutputJsonSchema { get; set; }

    /// <summary>
    /// 输出 JSON Schema 的 SHA-256 摘要
    /// </summary>
    [Display(Name = "OutputSchemaSha256"), Description("输出 JSON Schema 的 SHA-256 摘要"), MaxLength(64, ErrorMessage = "输出 JSON Schema 的 SHA-256 摘要 不能超过 64 个字符")]
    public string OutputSchemaSha256 { get; set; }
}
