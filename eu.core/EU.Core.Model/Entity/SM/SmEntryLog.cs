/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmEntryLog.cs
*
* 功 能： N / A
* 类 名： SmEntryLog
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/12/19 17:02:17  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// SmEntryLog (Model)
/// </summary>
[SugarTable("SmEntryLog", "SmEntryLog"), Entity(TableCnName = "SmEntryLog", TableName = "SmEntryLog")]
public class SmEntryLog : BasePoco
{

    /// <summary>
    /// 操作人ID
    /// </summary>
    [Display(Name = "LoginUserId"), Description("操作人ID"), SugarColumn(IsNullable = true)]
    public Guid? LoginUserId { get; set; }

    /// <summary>
    /// 操作人
    /// </summary>
    [Display(Name = "IpAddress"), Description("操作人"), SugarColumn(IsNullable = true, Length = 128)]
    public string IpAddress { get; set; }

    /// <summary>
    /// 操作程序
    /// </summary>
    [Display(Name = "IpAddressName1"), Description("操作程序"), SugarColumn(IsNullable = true, Length = 128)]
    public string IpAddressName1 { get; set; }

    /// <summary>
    /// 模块代码
    /// </summary>
    [Display(Name = "IpAddressName2"), Description("模块代码"), SugarColumn(IsNullable = true, Length = 128)]
    public string IpAddressName2 { get; set; }

    /// <summary>
    /// 表名
    /// </summary>
    [Display(Name = "LoginDate"), Description("表名"), SugarColumn(IsNullable = true)]
    public DateTime? LoginDate { get; set; }

    /// <summary>
    /// 数据ID
    /// </summary>
    [Display(Name = "LoginClass"), Description("数据ID"), SugarColumn(IsNullable = true, Length = 64)]
    public string LoginClass { get; set; }

    /// <summary>
    /// 操作时间
    /// </summary>
    [Display(Name = "OSName"), Description("操作时间"), SugarColumn(IsNullable = true, Length = 64)]
    public string OSName { get; set; }

    /// <summary>
    /// 操作类型
    /// </summary>
    [Display(Name = "ClientType"), Description("操作类型"), SugarColumn(IsNullable = true, Length = 64)]
    public string ClientType { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark1"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string ExtRemark1 { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark2"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string ExtRemark2 { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark3"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string ExtRemark3 { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "ExtRemark4"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string ExtRemark4 { get; set; }
}
