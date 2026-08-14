/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* AgAgentDefinition.cs
*
* 功 能： N / A
* 类 名： AgAgentDefinition
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2026/8/12 0:58:24  SahHsiao   初版
*
* Copyright(c) 2026 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/ 

namespace EU.Core.Model.Models;

#nullable enable

using EU.Core.Model.ViewModels.Extend;

/// <summary>
/// Agent 定义表(Dto.View)
/// </summary>
public class AgAgentDefinitionDto : AgAgentDefinition
{
    /// <summary>
    /// 当前草稿版本标签。
    /// </summary>
    public string DraftLabel { get; set; } = string.Empty;

    /// <summary>
    /// 当前草稿使用的模型配置标识。
    /// </summary>
    public string DraftModelProfileId { get; set; } = string.Empty;

    /// <summary>
    /// 最新发布版本标签；尚未发布时为空。
    /// </summary>
    public string? CurrentPublishedLabel { get; set; }
}

/// <summary>
/// Agent 明细聚合数据。
/// </summary>
public class AgAgentDefinitionDetailDto
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public AgentRuntimeStatus RuntimeStatus { get; set; }

    public long LogicalRevision { get; set; }

    public AgAgentVersionDetailDto Draft { get; set; } = null!;

    public List<AgAgentVersionDetailDto> PublishedVersions { get; set; } = [];

    public string DeploymentTarget => AgentDefinition.ServerDeploymentTarget;

    public string Host => AgentDefinition.ApiHost;
}

/// <summary>
/// Agent 版本明细聚合数据。
/// </summary>
public class AgAgentVersionDetailDto
{
    public Guid Id { get; set; }

    public string Label { get; set; } = string.Empty;

    public bool IsDraft { get; set; }

    public string Instructions { get; set; } = string.Empty;

    public string ModelProfileId { get; set; } = string.Empty;

    public AgentOutputMode OutputMode { get; set; }

    public string? OutputJsonSchema { get; set; }

    public string? OutputSchemaSha256 { get; set; }

    public AgAgentVersionSnapshotDetailDto? Snapshot { get; set; }

    public List<Guid> SkillVersionIds { get; set; } = [];

    public List<Guid> ToolVersionIds { get; set; } = [];

    public List<Guid> KnowledgeBaseIds { get; set; } = [];

    public List<Guid> ChildAgentIds { get; set; } = [];

    public List<Guid> OrchestrationIds { get; set; } = [];

    public List<AgentChildBindingSnapshot> ChildAgentPins { get; set; } = [];

    public List<AgentOrchestrationBindingSnapshot> OrchestrationPins { get; set; } = [];
}

/// <summary>
/// Agent 发布版本快照数据。
/// </summary>
public class AgAgentVersionSnapshotDetailDto
{
    public Guid VersionId { get; set; }

    public string AgentCode { get; set; } = string.Empty;

    public string Instructions { get; set; } = string.Empty;

    public string ModelProfileId { get; set; } = string.Empty;

    public AgentOutputMode OutputMode { get; set; }

    public string? OutputJsonSchema { get; set; }

    public List<AgentSkillBindingSnapshot> Skills { get; set; } = [];

    public List<AgentToolBindingSnapshot> Tools { get; set; } = [];

    public string? AgentName { get; set; }

    public string? AgentDescription { get; set; }

    public List<AgentKnowledgeBindingSnapshot> KnowledgeBases { get; set; } = [];

    public List<AgentChildBindingSnapshot> ChildAgents { get; set; } = [];

    public List<AgentOrchestrationBindingSnapshot> Orchestrations { get; set; } = [];
}
