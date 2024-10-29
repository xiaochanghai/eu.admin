/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* WxConfig.cs
*
*功 能： N / A
* 类 名： WxConfig
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V0.01  2024/6/21 0:48:51  SimonHsiao   初版
*
* Copyright(c) 2024 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SimonHsiao                                                  │
*└──────────────────────────────────┘
*/ 
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SqlSugar;

namespace EU.Core.Model.Models
{

    /// <summary>
    /// WxConfig (Model)
    /// </summary>
    [SugarTable("WxConfig", "WxConfig"), Entity(TableCnName = "WxConfig", TableName = "WxConfig")]
    public class WxConfig : BasePoco
    {

        /// <summary>
        /// 接口类型
        /// </summary>
        [Display(Name = "InterfaceType"), Description("接口类型"), MaxLength(32, ErrorMessage = "接口类型 不能超过 32 个字符")]
        public string InterfaceType { get; set; }

        /// <summary>
        /// 令牌
        /// </summary>
        [Display(Name = "Token"), Description("令牌"), MaxLength(64, ErrorMessage = "令牌 不能超过 64 个字符")]
        public string Token { get; set; }

        /// <summary>
        /// AppId
        /// </summary>
        [Display(Name = "AppId"), Description("AppId"), MaxLength(64, ErrorMessage = "AppId 不能超过 64 个字符")]
        public string AppId { get; set; }

        /// <summary>
        /// AppSecret
        /// </summary>
        [Display(Name = "AppSecret"), Description("AppSecret"), MaxLength(64, ErrorMessage = "AppSecret 不能超过 64 个字符")]
        public string AppSecret { get; set; }

        /// <summary>
        /// 原始ID
        /// </summary>
        [Display(Name = "OriginId"), Description("原始ID"), MaxLength(64, ErrorMessage = "原始ID 不能超过 64 个字符")]
        public string OriginId { get; set; }

        /// <summary>
        /// 微信ID
        /// </summary>
        [Display(Name = "WeixinId"), Description("微信ID"), MaxLength(64, ErrorMessage = "微信ID 不能超过 64 个字符")]
        public string WeixinId { get; set; }

        /// <summary>
        /// 微信名
        /// </summary>
        [Display(Name = "WeixinName"), Description("微信名"), MaxLength(64, ErrorMessage = "微信名 不能超过 64 个字符")]
        public string WeixinName { get; set; }

        /// <summary>
        /// EncodingAESKey
        /// </summary>
        [Display(Name = "AESKey"), Description("EncodingAESKey"), MaxLength(128, ErrorMessage = "EncodingAESKey 不能超过 128 个字符")]
        public string AESKey { get; set; }

        /// <summary>
        /// 关注提醒内容
        /// </summary>
        [Display(Name = "SubscribeContent"), Description("关注提醒内容"), MaxLength(128, ErrorMessage = "关注提醒内容 不能超过 128 个字符")]
        public string SubscribeContent { get; set; }

        /// <summary>
        /// 自动回复内容
        /// </summary>
        [Display(Name = "AutoReplyContent"), Description("自动回复内容"), MaxLength(128, ErrorMessage = "自动回复内容 不能超过 128 个字符")]
        public string AutoReplyContent { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
        public string Remark { get; set; }
    }
}
