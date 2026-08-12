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

/// <summary>
/// Agent 定义表(Dto.View)
/// </summary>
public class AgAgentDefinitionDto : AgAgentDefinition
{
    /// <summary>
    /// 当前草稿版本标签。
    /// </summary>
    public string DraftLabel { get; set; }

    /// <summary>
    /// 当前草稿使用的模型配置标识。
    /// </summary>
    public string DraftModelProfileId { get; set; }

    /// <summary>
    /// 最新发布版本标签；尚未发布时为空。
    /// </summary>
    public string CurrentPublishedLabel { get; set; }
}

/// <summary>
/// Agent 明细聚合数据。
/// </summary>
public class AgAgentDefinitionDetailDto
{
    public AgAgentDefinition Definition { get; set; }

    public List<AgAgentVersionDetailDto> Versions { get; set; } = [];
}

/// <summary>
/// Agent 版本明细聚合数据。
/// </summary>
public class AgAgentVersionDetailDto
{
    public AgAgentVersion Version { get; set; }

    public AgAgentVersionSnapshot Snapshot { get; set; }

    public List<AgAgentVersionBinding> Bindings { get; set; } = [];
}
