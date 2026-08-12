/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* AgAgentVersionSnapshot.cs
*
* 功 能： N / A
* 类 名： AgAgentVersionSnapshot
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2026/8/12 14:22:27  SahHsiao   初版
*
* Copyright(c) 2026 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// Agent 发布快照表，冻结发布时的 Agent 运行配置 (Dto.Base)
/// </summary>
public class AgAgentVersionSnapshotBase : BasePoco
{

    /// <summary>
    /// 所属 Agent 版本主键，对应 AgAgentVersion.ID
    /// </summary>
    [Display(Name = "VersionId"), Description("所属 Agent 版本主键，对应 AgAgentVersion.ID")]
    public Guid? VersionId { get; set; }

    /// <summary>
    /// 快照记录的 Agent 版本标识
    /// </summary>
    [Display(Name = "SnapshotVersionId"), Description("快照记录的 Agent 版本标识")]
    public Guid? SnapshotVersionId { get; set; }

    /// <summary>
    /// 发布时冻结的 Agent 编码
    /// </summary>
    [Display(Name = "AgentCode"), Description("发布时冻结的 Agent 编码"), MaxLength(128, ErrorMessage = "发布时冻结的 Agent 编码 不能超过 128 个字符")]
    public string AgentCode { get; set; }

    /// <summary>
    /// 发布时冻结的 Agent 系统指令
    /// </summary>
    [Display(Name = "Instructions"), Description("发布时冻结的 Agent 系统指令"), MaxLength(-1, ErrorMessage = "发布时冻结的 Agent 系统指令 不能超过 -1 个字符")]
    public string Instructions { get; set; }

    /// <summary>
    /// 发布时冻结的模型配置标识
    /// </summary>
    [Display(Name = "ModelProfileId"), Description("发布时冻结的模型配置标识"), MaxLength(256, ErrorMessage = "发布时冻结的模型配置标识 不能超过 256 个字符")]
    public string ModelProfileId { get; set; }

    /// <summary>
    /// 发布时冻结的输出模式
    /// </summary>
    [Display(Name = "OutputMode"), Description("发布时冻结的输出模式"), MaxLength(32, ErrorMessage = "发布时冻结的输出模式 不能超过 32 个字符")]
    public string OutputMode { get; set; }

    /// <summary>
    /// 发布时冻结的结构化输出 JSON Schema
    /// </summary>
    [Display(Name = "OutputJsonSchema"), Description("发布时冻结的结构化输出 JSON Schema"), MaxLength(-1, ErrorMessage = "发布时冻结的结构化输出 JSON Schema 不能超过 -1 个字符")]
    public string OutputJsonSchema { get; set; }

    /// <summary>
    /// 发布时冻结的 Agent 名称
    /// </summary>
    [Display(Name = "AgentName"), Description("发布时冻结的 Agent 名称"), MaxLength(256, ErrorMessage = "发布时冻结的 Agent 名称 不能超过 256 个字符")]
    public string AgentName { get; set; }

    /// <summary>
    /// 发布时冻结的 Agent 说明
    /// </summary>
    [Display(Name = "AgentDescription"), Description("发布时冻结的 Agent 说明"), MaxLength(-1, ErrorMessage = "发布时冻结的 Agent 说明 不能超过 -1 个字符")]
    public string AgentDescription { get; set; }
}
