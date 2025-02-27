/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmUserModuleColumn.cs
*
* 功 能： N / A
* 类 名： SmUserModuleColumn
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 9:26:23  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 用户模块列 (Dto.Base)
/// </summary>
public class SmUserModuleColumnBase : BasePoco
{

    /// <summary>
    /// 模块ID
    /// </summary>
    [Display(Name = "SmModuleId"), Description("模块ID"), SugarColumn(IsNullable = true)]
    public Guid? SmModuleId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [Display(Name = "UserId"), Description("用户ID"), SugarColumn(IsNullable = true)]
    public Guid? UserId { get; set; }

    /// <summary>
    /// 栏位名
    /// </summary>
    [Display(Name = "DataIndex"), Description("栏位名"), MaxLength(32, ErrorMessage = "栏位名 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string DataIndex { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [Display(Name = "TaxisNo"), Description("排序号"), SugarColumn(IsNullable = true)]
    public int? TaxisNo { get; set; }

    /// <summary>
    /// 是否显示
    /// </summary>
    [Display(Name = "IsShow"), Description("是否显示"), SugarColumn(IsNullable = true)]
    public bool? IsShow { get; set; }

    /// <summary>
    /// 固定位置
    /// </summary>
    [Display(Name = "Fixed"), Description("固定位置"), MaxLength(32, ErrorMessage = "固定位置 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string Fixed { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string Remark { get; set; }
}
