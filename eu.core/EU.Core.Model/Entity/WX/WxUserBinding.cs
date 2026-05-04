namespace EU.Core.Model.Entity;

/// <summary>
/// 微信用户绑定 (Model)
/// </summary>
[SugarTable("WxUserBinding", "微信用户绑定"), Entity(TableCnName = "微信用户绑定", TableName = "WxUserBinding")]
[SugarIndex("index_WxUserBinding_Channel_WeixinId_OpenId", nameof(Channel), OrderByType.Asc, nameof(WeixinId), OrderByType.Asc, nameof(OpenId), OrderByType.Asc, true)]
[SugarIndex("index_WxUserBinding_Channel_WeixinId_UnionId", nameof(Channel), OrderByType.Asc, nameof(WeixinId), OrderByType.Asc, nameof(UnionId), OrderByType.Asc, false)]
[SugarIndex("index_WxUserBinding_UserId", nameof(UserId), OrderByType.Asc)]
public class WxUserBinding : BasePoco
{
    /// <summary>
    /// 渠道类型 mp/wxopen/work
    /// </summary>
    [Display(Name = "Channel"), Description("渠道类型"), SugarColumn(IsNullable = true, Length = 32)]
    public string Channel { get; set; }

    /// <summary>
    /// 微信账号标识
    /// </summary>
    [Display(Name = "WeixinId"), Description("微信账号标识"), SugarColumn(IsNullable = true, Length = 64)]
    public string WeixinId { get; set; }

    /// <summary>
    /// AppId
    /// </summary>
    [Display(Name = "AppId"), Description("AppId"), SugarColumn(IsNullable = true, Length = 64)]
    public string AppId { get; set; }

    /// <summary>
    /// OpenId
    /// </summary>
    [Display(Name = "OpenId"), Description("OpenId"), SugarColumn(IsNullable = true, Length = 128)]
    public string OpenId { get; set; }

    /// <summary>
    /// UnionId
    /// </summary>
    [Display(Name = "UnionId"), Description("UnionId"), SugarColumn(IsNullable = true, Length = 128)]
    public string UnionId { get; set; }

    /// <summary>
    /// 企业微信用户ID
    /// </summary>
    [Display(Name = "WorkUserId"), Description("企业微信用户ID"), SugarColumn(IsNullable = true, Length = 128)]
    public string WorkUserId { get; set; }

    /// <summary>
    /// 系统用户ID
    /// </summary>
    [Display(Name = "UserId"), Description("系统用户ID"), SugarColumn(IsNullable = true)]
    public Guid? UserId { get; set; }

    /// <summary>
    /// 绑定时间
    /// </summary>
    [Display(Name = "BindTime"), Description("绑定时间"), SugarColumn(IsNullable = true)]
    public DateTime? BindTime { get; set; }

    /// <summary>
    /// 最近登录时间
    /// </summary>
    [Display(Name = "LastLoginTime"), Description("最近登录时间"), SugarColumn(IsNullable = true)]
    public DateTime? LastLoginTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}
