/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmUserModuleColumn.cs
*
*功 能： N / A
* 类 名： SmUserModuleColumn
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/7/1 16:42:48  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/
namespace EU.Core.Model.Models;


/// <summary>
/// 用户模块列 (Model)
/// </summary>
[SugarTable("SmUserModuleColumn", "SmUserModuleColumn"), Entity(TableCnName = "用户模块列", TableName = "SmUserModuleColumn")]
public class SmUserModuleColumn : BasePoco
{

    /// <summary>
    /// 模块ID
    /// </summary>
    public Guid? SmModuleId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// 栏位名
    /// </summary>
    [Display(Name = "DataIndex"), Description("栏位名"), MaxLength(32, ErrorMessage = "栏位名 不能超过 32 个字符")]
    public string DataIndex { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int? TaxisNo { get; set; }

    /// <summary>
    /// 是否显示
    /// </summary>
    public bool? IsShow { get; set; }

    /// <summary>
    /// 固定位置
    /// </summary>
    [Display(Name = "Fixed"), Description("固定位置"), MaxLength(32, ErrorMessage = "固定位置 不能超过 32 个字符")]
    public string Fixed { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}
