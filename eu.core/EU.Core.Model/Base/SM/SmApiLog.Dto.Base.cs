/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmApiLog.cs
*
* 功 能： N / A
* 类 名： SmApiLog
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/12/19 19:38:00  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// SmApiLog (Dto.Base)
/// </summary>
public class SmApiLogBase : BasePoco
{

    /// <summary>
    /// 用户ID
    /// </summary>
    [Display(Name = "UserId"), Description("用户ID")]
    public Guid? UserId { get; set; }

    /// <summary>
    /// IP
    /// </summary>
    [Display(Name = "IP"), Description("IP"), MaxLength(32, ErrorMessage = "IP 不能超过 32 个字符")]
    public string IP { get; set; }

    /// <summary>
    /// 请求地址
    /// </summary>
    [Display(Name = "Path"), Description("请求地址"), MaxLength(200, ErrorMessage = "请求地址 不能超过 200 个字符")]
    public string Path { get; set; }

    /// <summary>
    /// 请求方式
    /// </summary>
    [Display(Name = "Method"), Description("请求方式"), MaxLength(32, ErrorMessage = "请求方式 不能超过 32 个字符")]
    public string Method { get; set; }

    /// <summary>
    /// 来源
    /// </summary>
    [Display(Name = "Source"), Description("来源"), MaxLength(32, ErrorMessage = "来源 不能超过 32 个字符")]
    public string Source { get; set; }

    /// <summary>
    /// 请求内容
    /// </summary>
    [Display(Name = "RequestData"), Description("请求内容"), MaxLength(-1, ErrorMessage = "请求内容 不能超过 -1 个字符")]
    public string RequestData { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    [Display(Name = "BeginTime"), Description("开始时间")]
    public DateTime? BeginTime { get; set; }

    /// <summary>
    /// 操作时长
    /// </summary>
    [Display(Name = "OPTime"), Description("操作时长")]
    public int? OPTime { get; set; }

    /// <summary>
    /// 代理
    /// </summary>
    [Display(Name = "Agent"), Description("代理"), MaxLength(2000, ErrorMessage = "代理 不能超过 2000 个字符")]
    public string Agent { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}
