/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmRoleDataScopeAudit.cs
*
* 功 能： 角色数据权限审计日志
* 类 名： SmRoleDataScopeAudit
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/6/23  EU Team   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│ 此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露． │
*│ 版权所有：EU Team                              │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 角色数据权限审计日志 (Model)
/// 记录数据权限的变更历史
/// </summary>
[SugarTable("SmRoleDataScopeAudit", "角色数据权限审计日志"), Entity(TableCnName = "角色数据权限审计日志", TableName = "SmRoleDataScopeAudit")]
public class SmRoleDataScopeAudit : BasePoco
{
    /// <summary>
    /// 角色ID
    /// </summary>
    [Display(Name = "SmRoleId"), Description("角色ID"), SugarColumn(IsNullable = false)]
    public Guid SmRoleId { get; set; }

    /// <summary>
    /// 操作类型：Add / Update / Delete
    /// </summary>
    [Display(Name = "Action"), Description("操作类型"), SugarColumn(IsNullable = false, Length = 32)]
    public string Action { get; set; }

    /// <summary>
    /// 旧值（JSON格式）
    /// </summary>
    [Display(Name = "OldValue"), Description("旧值"), SugarColumn(IsNullable = true, ColumnDataType = "text")]
    public string OldValue { get; set; }

    /// <summary>
    /// 新值（JSON格式）
    /// </summary>
    [Display(Name = "NewValue"), Description("新值"), SugarColumn(IsNullable = true, ColumnDataType = "text")]
    public string NewValue { get; set; }

    /// <summary>
    /// 操作人ID
    /// </summary>
    [Display(Name = "OperatedBy"), Description("操作人ID"), SugarColumn(IsNullable = true)]
    public Guid? OperatedBy { get; set; }

    /// <summary>
    /// 操作时间
    /// </summary>
    [Display(Name = "OperatedTime"), Description("操作时间"), SugarColumn(IsNullable = false)]
    public DateTime OperatedTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    [Display(Name = "IpAddress"), Description("IP地址"), SugarColumn(IsNullable = true, Length = 50)]
    public string IpAddress { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    [Display(Name = "UserAgent"), Description("用户代理"), SugarColumn(IsNullable = true, Length = 500)]
    public string UserAgent { get; set; }

    /// <summary>
    /// 操作是否成功
    /// </summary>
    [Display(Name = "IsSuccess"), Description("操作是否成功"), SugarColumn(IsNullable = false)]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    [Display(Name = "ErrorMessage"), Description("错误消息"), SugarColumn(IsNullable = true, ColumnDataType = "text")]
    public string ErrorMessage { get; set; }

    /// <summary>
    /// 变更原因
    /// </summary>
    [Display(Name = "Reason"), Description("变更原因"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Reason { get; set; }
}
