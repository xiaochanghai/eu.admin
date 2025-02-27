/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmProvince.cs
*
* 功 能： N / A
* 类 名： SmProvince
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 9:26:14  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 省份 (Model)
/// </summary>
[SugarTable("SmProvince", "省份"), Entity(TableCnName = "省份", TableName = "SmProvince")]
public class SmProvince : BasePoco
{

    /// <summary>
    /// 省份代码
    /// </summary>
    [Display(Name = "ProvinceCode"), Description("省份代码"), MaxLength(32, ErrorMessage = "省份代码 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string ProvinceCode { get; set; }

    /// <summary>
    /// 省份
    /// </summary>
    [Display(Name = "ProvinceNameZh"), Description("省份"), MaxLength(32, ErrorMessage = "省份 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string ProvinceNameZh { get; set; }

    /// <summary>
    /// 省份(英文)
    /// </summary>
    [Display(Name = "ProvinceNameEn"), Description("省份(英文)"), MaxLength(32, ErrorMessage = "省份(英文) 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string ProvinceNameEn { get; set; }

    /// <summary>
    /// 省份编号
    /// </summary>
    [Display(Name = "ProvinceNo"), Description("省份编号"), MaxLength(32, ErrorMessage = "省份编号 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string ProvinceNo { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [Display(Name = "TaxisNo"), Description("排序号"), SugarColumn(IsNullable = true)]
    public int? TaxisNo { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string Remark { get; set; }
}
