/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* AgAgentVersionBinding.cs
*
* 功 能： N / A
* 类 名： AgAgentVersionBinding
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2026/8/12 14:21:09  SahHsiao   初版
*
* Copyright(c) 2026 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// Agent 版本资源绑定表，统一保存 Skill、MCP 工具、知识库、子 Agent 和编排绑定 (Dto.Base)
/// </summary>
public class AgAgentVersionBindingBase : BasePoco
{

    /// <summary>
    /// 所属 Agent 版本主键，对应 AgAgentVersion.ID
    /// </summary>
    [Display(Name = "VersionId"), Description("所属 Agent 版本主键，对应 AgAgentVersion.ID")]
    public Guid? VersionId { get; set; }

    /// <summary>
    /// 绑定范围：Version 表示版本配置，Snapshot 表示发布快照
    /// </summary>
    [Display(Name = "Scope"), Description("绑定范围：Version 表示版本配置，Snapshot 表示发布快照"), MaxLength(16, ErrorMessage = "绑定范围：Version 表示版本配置，Snapshot 表示发布快照 不能超过 16 个字符")]
    public string Scope { get; set; }

    /// <summary>
    /// 绑定类型：Skill、Tool、KnowledgeBase、ChildAgent 或 Orchestration
    /// </summary>
    [Display(Name = "BindingType"), Description("绑定类型：Skill、Tool、KnowledgeBase、ChildAgent 或 Orchestration"), MaxLength(32, ErrorMessage = "绑定类型：Skill、Tool、KnowledgeBase、ChildAgent 或 Orchestration 不能超过 32 个字符")]
    public string BindingType { get; set; }

    /// <summary>
    /// 同一版本、范围及类型下的排列顺序
    /// </summary>
    [Display(Name = "Ordinal"), Description("同一版本、范围及类型下的排列顺序")]
    public int? Ordinal { get; set; }

    /// <summary>
    /// 被绑定资源的主键
    /// </summary>
    [Display(Name = "ReferenceId"), Description("被绑定资源的主键")]
    public Guid? ReferenceId { get; set; }

    /// <summary>
    /// 发布时固定的资源版本主键，适用于子 Agent 和编排等资源
    /// </summary>
    [Display(Name = "ReferenceVersionId"), Description("发布时固定的资源版本主键，适用于子 Agent 和编排等资源")]
    public Guid? ReferenceVersionId { get; set; }

    /// <summary>
    /// 发布时固定的资源逻辑修订号，主要用于知识库
    /// </summary>
    [Display(Name = "LogicalRevision"), Description("发布时固定的资源逻辑修订号，主要用于知识库")]
    public long? LogicalRevision { get; set; }

    /// <summary>
    /// 发布时冻结的被绑定资源编码
    /// </summary>
    [Display(Name = "ReferenceCode"), Description("发布时冻结的被绑定资源编码"), MaxLength(128, ErrorMessage = "发布时冻结的被绑定资源编码 不能超过 128 个字符")]
    public string ReferenceCode { get; set; }

    /// <summary>
    /// 发布时冻结的被绑定资源名称
    /// </summary>
    [Display(Name = "ReferenceName"), Description("发布时冻结的被绑定资源名称"), MaxLength(256, ErrorMessage = "发布时冻结的被绑定资源名称 不能超过 256 个字符")]
    public string ReferenceName { get; set; }

    /// <summary>
    /// 发布时冻结的被绑定资源说明
    /// </summary>
    [Display(Name = "ReferenceDescription"), Description("发布时冻结的被绑定资源说明"), MaxLength(-1, ErrorMessage = "发布时冻结的被绑定资源说明 不能超过 -1 个字符")]
    public string ReferenceDescription { get; set; }
}
