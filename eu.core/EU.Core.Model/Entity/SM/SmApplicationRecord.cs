/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmApplicationRecord.cs
*
* 功 能： N / A
* 类 名： SmApplicationRecord
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/4/29 23:05:57  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// SmApplicationRecord (Model)
/// </summary>
[SugarTable("SmApplicationRecord", "SmApplicationRecord"), Entity(TableCnName = "SmApplicationRecord", TableName = "SmApplicationRecord")]
public class SmApplicationRecord : BasePoco
{

    /// <summary>
    /// 设备ID
    /// </summary>
    [Display(Name = "UUID"), Description("设备ID"), SugarColumn(IsNullable = true, Length = 64)]
    public string UUID { get; set; }

    /// <summary>
    /// 登录时间
    /// </summary>
    [Display(Name = "LaunchTime"), Description("登录时间"), SugarColumn(IsNullable = true)]
    public DateTime? LaunchTime { get; set; }

    /// <summary>
    /// IP
    /// </summary>
    [Display(Name = "IP"), Description("IP"), SugarColumn(IsNullable = true, Length = 64)]
    public string IP { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}
