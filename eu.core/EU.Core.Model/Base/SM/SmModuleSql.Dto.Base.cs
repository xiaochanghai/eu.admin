/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmModuleSql.cs
*
* 功 能： N / A
* 类 名： SmModuleSql
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 9:26:13  SimonHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Models;

/// <summary>
/// 系统模块SQL (Dto.Base)
/// </summary>
public class SmModuleSqlBase : BasePoco
{

    /// <summary>
    /// 模块ID
    /// </summary>
    [Display(Name = "ModuleId"), Description("模块ID"), SugarColumn(IsNullable = true)]
    public Guid? ModuleId { get; set; }

    /// <summary>
    /// 主表名
    /// </summary>
    [Display(Name = "PrimaryTableName"), Description("主表名"), MaxLength(64, ErrorMessage = "主表名 不能超过 64 个字符"), SugarColumn(IsNullable = true)]
    public string PrimaryTableName { get; set; }

    /// <summary>
    /// 全部表名
    /// </summary>
    [Display(Name = "TableNames"), Description("全部表名"), MaxLength(128, ErrorMessage = "全部表名 不能超过 128 个字符"), SugarColumn(IsNullable = true)]
    public string TableNames { get; set; }

    /// <summary>
    /// 全部表别名
    /// </summary>
    [Display(Name = "TableAliasNames"), Description("全部表别名"), MaxLength(64, ErrorMessage = "全部表别名 不能超过 64 个字符"), SugarColumn(IsNullable = true)]
    public string TableAliasNames { get; set; }

    /// <summary>
    /// 主键
    /// </summary>
    [Display(Name = "PrimaryKey"), Description("主键"), MaxLength(64, ErrorMessage = "主键 不能超过 64 个字符"), SugarColumn(IsNullable = true)]
    public string PrimaryKey { get; set; }

    /// <summary>
    /// Select语句
    /// </summary>
    [Display(Name = "SqlSelect"), Description("Select语句"), MaxLength(4000, ErrorMessage = "Select语句 不能超过 4000 个字符"), SugarColumn(IsNullable = true)]
    public string SqlSelect { get; set; }

    /// <summary>
    /// 首页Select语句
    /// </summary>
    [Display(Name = "SqlSelectBrw"), Description("首页Select语句"), MaxLength(4000, ErrorMessage = "首页Select语句 不能超过 4000 个字符"), SugarColumn(IsNullable = true)]
    public string SqlSelectBrw { get; set; }

    /// <summary>
    /// 关联类型
    /// </summary>
    [Display(Name = "JoinType"), Description("关联类型"), MaxLength(512, ErrorMessage = "关联类型 不能超过 512 个字符"), SugarColumn(IsNullable = true)]
    public string JoinType { get; set; }

    /// <summary>
    /// 关联表
    /// </summary>
    [Display(Name = "SqlJoinTable"), Description("关联表"), MaxLength(256, ErrorMessage = "关联表 不能超过 256 个字符"), SugarColumn(IsNullable = true)]
    public string SqlJoinTable { get; set; }

    /// <summary>
    /// 关联表别名
    /// </summary>
    [Display(Name = "SqlJoinTableAlias"), Description("关联表别名"), MaxLength(64, ErrorMessage = "关联表别名 不能超过 64 个字符"), SugarColumn(IsNullable = true)]
    public string SqlJoinTableAlias { get; set; }

    /// <summary>
    /// 关联条件
    /// </summary>
    [Display(Name = "SqlJoinCondition"), Description("关联条件"), MaxLength(4000, ErrorMessage = "关联条件 不能超过 4000 个字符"), SugarColumn(IsNullable = true)]
    public string SqlJoinCondition { get; set; }

    /// <summary>
    /// 默认条件
    /// </summary>
    [Display(Name = "SqlDefaultCondition"), Description("默认条件"), MaxLength(4000, ErrorMessage = "默认条件 不能超过 4000 个字符"), SugarColumn(IsNullable = true)]
    public string SqlDefaultCondition { get; set; }

    /// <summary>
    /// 回收站条件
    /// </summary>
    [Display(Name = "SqlRecycleCondition"), Description("回收站条件"), MaxLength(4000, ErrorMessage = "回收站条件 不能超过 4000 个字符"), SugarColumn(IsNullable = true)]
    public string SqlRecycleCondition { get; set; }

    /// <summary>
    /// 初始查询条件
    /// </summary>
    [Display(Name = "SqlQueryCondition"), Description("初始查询条件"), MaxLength(4000, ErrorMessage = "初始查询条件 不能超过 4000 个字符"), SugarColumn(IsNullable = true)]
    public string SqlQueryCondition { get; set; }

    /// <summary>
    /// 主表默认排序列名
    /// </summary>
    [Display(Name = "DefaultSortField"), Description("主表默认排序列名"), MaxLength(64, ErrorMessage = "主表默认排序列名 不能超过 64 个字符"), SugarColumn(IsNullable = true)]
    public string DefaultSortField { get; set; }

    /// <summary>
    /// 主表默认排序方向
    /// </summary>
    [Display(Name = "DefaultSortDirection"), Description("主表默认排序方向"), MaxLength(64, ErrorMessage = "主表默认排序方向 不能超过 64 个字符"), SugarColumn(IsNullable = true)]
    public string DefaultSortDirection { get; set; }

    /// <summary>
    /// GROUP_BY
    /// </summary>
    [Display(Name = "GroupBy"), Description("GROUP_BY"), MaxLength(64, ErrorMessage = "GROUP_BY 不能超过 64 个字符"), SugarColumn(IsNullable = true)]
    public string GroupBy { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [Display(Name = "Description"), Description("描述"), MaxLength(512, ErrorMessage = "描述 不能超过 512 个字符"), SugarColumn(IsNullable = true)]
    public string Description { get; set; }

    /// <summary>
    /// 完整SQL
    /// </summary>
    [Display(Name = "FullSql"), Description("完整SQL"), MaxLength(2147483647, ErrorMessage = "完整SQL 不能超过 2147483647 个字符"), SugarColumn(IsNullable = true)]
    public string FullSql { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符"), SugarColumn(IsNullable = true)]
    public string Remark { get; set; }

    /// <summary>
    /// ID1
    /// </summary>
    [Display(Name = "ID1"), Description("ID1"), SugarColumn(IsNullable = true)]
    public Guid? ID1 { get; set; }
}
