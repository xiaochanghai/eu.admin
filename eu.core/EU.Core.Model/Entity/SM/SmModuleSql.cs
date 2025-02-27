/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmModuleSql.cs
*
* 功 能： N / A
* 类 名： SmModuleSql
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:31:01  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 系统模块SQL (Model)
/// </summary>
[SugarTable("SmModuleSql", "系统模块SQL"), Entity(TableCnName = "系统模块SQL", TableName = "SmModuleSql")]
public class SmModuleSql : BasePoco
{

    /// <summary>
    /// 模块ID
    /// </summary>
    [Display(Name = "ModuleId"), Description("模块ID"), SugarColumn(IsNullable = true)]
    public Guid? ModuleId { get; set; }

    /// <summary>
    /// 主表名
    /// </summary>
    [Display(Name = "PrimaryTableName"), Description("主表名"), SugarColumn(IsNullable = true, Length = 64)]
    public string PrimaryTableName { get; set; }

    /// <summary>
    /// 全部表名
    /// </summary>
    [Display(Name = "TableNames"), Description("全部表名"), SugarColumn(IsNullable = true, Length = 128)]
    public string TableNames { get; set; }

    /// <summary>
    /// 全部表别名
    /// </summary>
    [Display(Name = "TableAliasNames"), Description("全部表别名"), SugarColumn(IsNullable = true, Length = 64)]
    public string TableAliasNames { get; set; }

    /// <summary>
    /// 主键
    /// </summary>
    [Display(Name = "PrimaryKey"), Description("主键"), SugarColumn(IsNullable = true, Length = 64)]
    public string PrimaryKey { get; set; }

    /// <summary>
    /// Select语句
    /// </summary>
    [Display(Name = "SqlSelect"), Description("Select语句"), SugarColumn(IsNullable = true, Length = 4000)]
    public string SqlSelect { get; set; }

    /// <summary>
    /// 首页Select语句
    /// </summary>
    [Display(Name = "SqlSelectBrw"), Description("首页Select语句"), SugarColumn(IsNullable = true, Length = 4000)]
    public string SqlSelectBrw { get; set; }

    /// <summary>
    /// 关联类型
    /// </summary>
    [Display(Name = "JoinType"), Description("关联类型"), SugarColumn(IsNullable = true, Length = 512)]
    public string JoinType { get; set; }

    /// <summary>
    /// 关联表
    /// </summary>
    [Display(Name = "SqlJoinTable"), Description("关联表"), SugarColumn(IsNullable = true, Length = 256)]
    public string SqlJoinTable { get; set; }

    /// <summary>
    /// 关联表别名
    /// </summary>
    [Display(Name = "SqlJoinTableAlias"), Description("关联表别名"), SugarColumn(IsNullable = true, Length = 64)]
    public string SqlJoinTableAlias { get; set; }

    /// <summary>
    /// 关联条件
    /// </summary>
    [Display(Name = "SqlJoinCondition"), Description("关联条件"), SugarColumn(IsNullable = true, Length = 4000)]
    public string SqlJoinCondition { get; set; }

    /// <summary>
    /// 默认条件
    /// </summary>
    [Display(Name = "SqlDefaultCondition"), Description("默认条件"), SugarColumn(IsNullable = true, Length = 4000)]
    public string SqlDefaultCondition { get; set; }

    /// <summary>
    /// 回收站条件
    /// </summary>
    [Display(Name = "SqlRecycleCondition"), Description("回收站条件"), SugarColumn(IsNullable = true, Length = 4000)]
    public string SqlRecycleCondition { get; set; }

    /// <summary>
    /// 初始查询条件
    /// </summary>
    [Display(Name = "SqlQueryCondition"), Description("初始查询条件"), SugarColumn(IsNullable = true, Length = 4000)]
    public string SqlQueryCondition { get; set; }

    /// <summary>
    /// 主表默认排序列名
    /// </summary>
    [Display(Name = "DefaultSortField"), Description("主表默认排序列名"), SugarColumn(IsNullable = true, Length = 64)]
    public string DefaultSortField { get; set; }

    /// <summary>
    /// 主表默认排序方向
    /// </summary>
    [Display(Name = "DefaultSortDirection"), Description("主表默认排序方向"), SugarColumn(IsNullable = true, Length = 64)]
    public string DefaultSortDirection { get; set; }

    /// <summary>
    /// GROUP_BY
    /// </summary>
    [Display(Name = "GroupBy"), Description("GROUP_BY"), SugarColumn(IsNullable = true, Length = 64)]
    public string GroupBy { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [Display(Name = "Description"), Description("描述"), SugarColumn(IsNullable = true, Length = 512)]
    public string Description { get; set; }

    /// <summary>
    /// 完整SQL
    /// </summary>
    [Display(Name = "FullSql"), Description("完整SQL"), SugarColumn(IsNullable = true, Length = 2147483647)]
    public string FullSql { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }

    /// <summary>
    /// ID1
    /// </summary>
    [Display(Name = "ID1"), Description("ID1"), SugarColumn(IsNullable = true)]
    public Guid? ID1 { get; set; }
}
