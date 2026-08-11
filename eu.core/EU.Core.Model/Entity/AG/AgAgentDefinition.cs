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
    /// Id
    /// </summary>
    [Display(Name = "Id"), Description("Id")]
    public Guid? Id { get; set; }

    /// <summary>
    /// Code
    /// </summary>
    [Display(Name = "Code"), Description("Code"), SugarColumn(IsNullable = true, Length = 128)]

    /// <summary>
    /// LogicalRevision
    /// </summary>
    [Display(Name = "LogicalRevision"), Description("LogicalRevision")]
    public long? LogicalRevision { get; set; }

    /// <summary>
    /// DocumentJson
    /// </summary>
    [Display(Name = "DocumentJson"), Description("DocumentJson"), SugarColumn(IsNullable = true, Length = -1)]
}
