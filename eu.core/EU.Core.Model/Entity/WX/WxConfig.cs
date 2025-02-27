/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* WxConfig.cs
*
* 功 能： N / A
* 类 名： WxConfig
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/2/27 18:31:18  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 微信账号配置 (Model)
/// </summary>
[SugarTable("WxConfig", "微信账号配置"), Entity(TableCnName = "微信账号配置", TableName = "WxConfig")]
public class WxConfig : BasePoco
{

    /// <summary>
    /// 接口类型
    /// </summary>
    [Display(Name = "InterfaceType"), Description("接口类型"), SugarColumn(IsNullable = true, Length = 32)]
    public string InterfaceType { get; set; }

    /// <summary>
    /// 令牌
    /// </summary>
    [Display(Name = "Token"), Description("令牌"), SugarColumn(IsNullable = true, Length = 64)]
    public string Token { get; set; }

    /// <summary>
    /// AppId
    /// </summary>
    [Display(Name = "AppId"), Description("AppId"), SugarColumn(IsNullable = true, Length = 64)]
    public string AppId { get; set; }

    /// <summary>
    /// AppSecret
    /// </summary>
    [Display(Name = "AppSecret"), Description("AppSecret"), SugarColumn(IsNullable = true, Length = 64)]
    public string AppSecret { get; set; }

    /// <summary>
    /// 原始ID
    /// </summary>
    [Display(Name = "OriginId"), Description("原始ID"), SugarColumn(IsNullable = true, Length = 64)]
    public string OriginId { get; set; }

    /// <summary>
    /// 微信ID
    /// </summary>
    [Display(Name = "WeixinId"), Description("微信ID"), SugarColumn(IsNullable = true, Length = 64)]
    public string WeixinId { get; set; }

    /// <summary>
    /// 微信名
    /// </summary>
    [Display(Name = "WeixinName"), Description("微信名"), SugarColumn(IsNullable = true, Length = 64)]
    public string WeixinName { get; set; }

    /// <summary>
    /// EncodingAESKey
    /// </summary>
    [Display(Name = "AESKey"), Description("EncodingAESKey"), SugarColumn(IsNullable = true, Length = 128)]
    public string AESKey { get; set; }

    /// <summary>
    /// 关注提醒内容
    /// </summary>
    [Display(Name = "SubscribeContent"), Description("关注提醒内容"), SugarColumn(IsNullable = true, Length = 128)]
    public string SubscribeContent { get; set; }

    /// <summary>
    /// 自动回复内容
    /// </summary>
    [Display(Name = "AutoReplyContent"), Description("自动回复内容"), SugarColumn(IsNullable = true, Length = 128)]
    public string AutoReplyContent { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}
