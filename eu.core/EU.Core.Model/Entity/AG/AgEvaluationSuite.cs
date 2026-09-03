/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* AgEvaluationSuite.cs
*
* 功 能： N / A
* 类 名： AgEvaluationSuite
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2026/9/3 9:11:41  SahHsiao   初版
*
* Copyright(c) 2026 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 评测套件定义主表 (Model)
/// </summary>
[SugarTable("AgEvaluationSuite", "评测套件定义主表"), Entity(TableCnName = "评测套件定义主表", TableName = "AgEvaluationSuite")]
public class AgEvaluationSuite : BasePoco
{

    /// <summary>
    /// 租户标识
    /// </summary>
    [Display(Name = "TenantId"), Description("租户标识"), SugarColumn(IsNullable = true, Length = 128)]
    public string TenantId { get; set; }

    /// <summary>
    /// 租户内唯一编码
    /// </summary>
    [Display(Name = "Code"), Description("租户内唯一编码"), SugarColumn(IsNullable = true, Length = 128)]
    public string Code { get; set; }

    /// <summary>
    /// 逻辑修订号
    /// </summary>
    [Display(Name = "LogicalRevision"), Description("逻辑修订号")]
    public long? LogicalRevision { get; set; }

    /// <summary>
    /// 套件显示名称
    /// </summary>
    [Display(Name = "Name"), Description("套件显示名称"), SugarColumn(IsNullable = true, Length = 256)]
    public string Name { get; set; }

    /// <summary>
    /// 套件说明
    /// </summary>
    [Display(Name = "Description"), Description("套件说明"), SugarColumn(IsNullable = true, Length = -1)]
    public string Description { get; set; }

    /// <summary>
    /// 生命周期状态：Active 或 Archived
    /// </summary>
    [Display(Name = "Status"), Description("生命周期状态：Active 或 Archived"), SugarColumn(IsNullable = true, Length = 32)]
    public string Status { get; set; }

    /// <summary>
    /// 业务创建 UTC 时间
    /// </summary>
    [Display(Name = "CreatedAtUtc"), Description("业务创建 UTC 时间"), SugarColumn(IsNullable = true)]
    public DateTime? CreatedAtUtc { get; set; }

    /// <summary>
    /// 业务更新 UTC 时间
    /// </summary>
    [Display(Name = "UpdatedAtUtc"), Description("业务更新 UTC 时间"), SugarColumn(IsNullable = true)]
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>
    /// 创建用户标识
    /// </summary>
    [Display(Name = "CreatedByUserId"), Description("创建用户标识"), SugarColumn(IsNullable = true, Length = 256)]
    public string CreatedByUserId { get; set; }

    /// <summary>
    /// 更新用户标识
    /// </summary>
    [Display(Name = "UpdatedByUserId"), Description("更新用户标识"), SugarColumn(IsNullable = true, Length = 256)]
    public string UpdatedByUserId { get; set; }
}
