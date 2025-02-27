/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmImpTemplateDetail.cs
*
* 功 能： N / A
* 类 名： SmImpTemplateDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 9:26:07  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// 导入模板定义明细 (Dto.Base)
/// </summary>
public class SmImpTemplateDetailBase : BasePoco
{

    /// <summary>
    /// 模板ID
    /// </summary>
    [Display(Name = "ImpTemplateId"), Description("模板ID"), SugarColumn(IsNullable = true)]
    public Guid? ImpTemplateId { get; set; }

    /// <summary>
    /// Execl列号
    /// </summary>
    [Display(Name = "ColumnNo"), Description("Execl列号"), SugarColumn(IsNullable = true)]
    public int? ColumnNo { get; set; }

    /// <summary>
    /// 列名称
    /// </summary>
    [Display(Name = "ColumnCode"), Description("列名称"), MaxLength(32, ErrorMessage = "列名称 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string ColumnCode { get; set; }

    /// <summary>
    /// 是否唯一
    /// </summary>
    [Display(Name = "IsUnique"), Description("是否唯一"), SugarColumn(IsNullable = true)]
    public bool? IsUnique { get; set; }

    /// <summary>
    /// 是否插入
    /// </summary>
    [Display(Name = "IsInsert"), Description("是否插入"), SugarColumn(IsNullable = true)]
    public bool? IsInsert { get; set; }

    /// <summary>
    /// 格式
    /// </summary>
    [Display(Name = "DateFormate"), Description("格式"), MaxLength(32, ErrorMessage = "格式 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string DateFormate { get; set; }

    /// <summary>
    /// 最大长度
    /// </summary>
    [Display(Name = "MaxLength"), Description("最大长度"), SugarColumn(IsNullable = true)]
    public int? MaxLength { get; set; }

    /// <summary>
    /// 允许为空
    /// </summary>
    [Display(Name = "IsAllowNull"), Description("允许为空"), SugarColumn(IsNullable = true)]
    public bool? IsAllowNull { get; set; }

    /// <summary>
    /// 加密
    /// </summary>
    [Display(Name = "IsEncrypt"), Description("加密"), SugarColumn(IsNullable = true)]
    public bool? IsEncrypt { get; set; }

    /// <summary>
    /// 参数代码
    /// </summary>
    [Display(Name = "LovCode"), Description("参数代码"), MaxLength(32, ErrorMessage = "参数代码 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string LovCode { get; set; }

    /// <summary>
    /// 映射表
    /// </summary>
    [Display(Name = "CorresTableCode"), Description("映射表"), MaxLength(32, ErrorMessage = "映射表 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string CorresTableCode { get; set; }

    /// <summary>
    /// 映射字段
    /// </summary>
    [Display(Name = "CorresColumnCode"), Description("映射字段"), MaxLength(32, ErrorMessage = "映射字段 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string CorresColumnCode { get; set; }

    /// <summary>
    /// 转换字段
    /// </summary>
    [Display(Name = "TransColumnCode"), Description("转换字段"), MaxLength(32, ErrorMessage = "转换字段 不能超过 32 个字符"), SugarColumn(IsNullable = true)]
    public string TransColumnCode { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string Remark { get; set; }
}
