using EU.Core.Model.Models.RootTkey.Interface;

namespace EU.Core.Model.Models.RootTkey;

[SugarIndex("index_{table}_Enabled", nameof(IsActive), OrderByType.Asc)]
[SugarIndex("index_{table}_IsDeleted", nameof(IsDeleted), OrderByType.Asc)]
public class BaseEntity : RootEntityTkey<Guid>, IDeleteFilter
{
    #region 数据状态管理

    /// <summary>
    /// 中立字段，某些表可使用某些表不使用   <br/>
    /// 逻辑上的删除，非物理删除  <br/>
    /// 例如：单据删除并非直接删除
    /// </summary>
    [Display(Name = "删除标记"), SugarColumn(IsNullable = true)]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 导入模板ID
    /// </summary>
    [Display(Name = "导入模板ID"), SugarColumn(IsNullable = true)]
    public Guid? ImportDataId { get; set; }

    /// <summary>
    /// 修改次数
    /// </summary>
    [Display(Name = "修改次数"), SugarColumn(IsNullable = true)]
    public int? ModificationNum { get; set; }

    /// <summary>
    /// 修改标志
    /// </summary>
    [Display(Name = "修改标志"), SugarColumn(IsNullable = true)]
    public int? Tag { get; set; }

    /// <summary>
    /// 集团ID
    /// </summary>
    [Display(Name = "集团ID"), SugarColumn(IsOnlyIgnoreUpdate = true, IsNullable = true)]
    public Guid? GroupId { get; set; }

    /// <summary>
    /// 公司ID
    /// </summary>
    [Display(Name = "公司ID"), SugarColumn(IsOnlyIgnoreUpdate = true, IsNullable = true)]
    public Guid? CompanyId { get; set; }

    /// <summary>
    /// 审核状态
    /// </summary>
    [Display(Name = "审核状态"), Column(TypeName = "varchar(32)"), SugarColumn(IsNullable = true)]
    public string AuditStatus { get; set; } = "Add";

    /// <summary>
    /// 当前流程节点
    /// </summary>
    [Display(Name = "当前流程节点"), Column(TypeName = "nvarchar(50)"), SugarColumn(IsNullable = true)]
    public string CurrentNode { get; set; }

    /// <summary>
    /// 有效标志'true':有效,'false':未生效
    /// </summary>
    [Display(Name = "'true':有效,'false':未生效"), SugarColumn(IsNullable = true)]
    public bool? IsActive { get; set; } = true;

    #endregion

    #region 创建
    /// <summary>
    /// 创建人
    /// </summary>
    [Display(Name = "创建人"), SugarColumn(IsNullable = true, IsOnlyIgnoreUpdate = true)]
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [Display(Name = "创建时间")]
    [Column(TypeName = "datetime"), SugarColumn(IsNullable = true, IsOnlyIgnoreUpdate = true, InsertServerTime = true)]
    public DateTime? CreatedTime { get; set; }
    #endregion

    #region 修改
    /// <summary>
    /// 最后修改人
    /// </summary>
    [Display(Name = "最后修改人"), SugarColumn(IsNullable = true, IsOnlyIgnoreInsert = true)]
    public Guid? UpdateBy { get; set; }

    /// <summary>
    /// 最后修改时间
    /// </summary>
    [Display(Name = "最后修改时间"), SugarColumn(IsNullable = true, IsOnlyIgnoreInsert = true, UpdateServerTime = true), Column(TypeName = "datetime")]
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// 模块代码
    /// </summary>
    [Display(Name = "模块代码"), SugarColumn(IsIgnore = true), NotMapped]
    public virtual string ModuleCode { get; set; }

    #endregion
}