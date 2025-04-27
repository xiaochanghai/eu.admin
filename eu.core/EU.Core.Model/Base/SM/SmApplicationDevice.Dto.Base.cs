/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmApplicationDevice.cs
*
* 功 能： N / A
* 类 名： SmApplicationDevice
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/4/27 16:04:05  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Base;

/// <summary>
/// APP客户端记录 (Dto.Base)
/// </summary>
public class SmApplicationDeviceBase : BasePoco
{

    /// <summary>
    /// 设备ID
    /// </summary>
    [Display(Name = "UUID"), Description("设备ID"), MaxLength(64, ErrorMessage = "设备ID 不能超过 64 个字符")]
    public string UUID { get; set; }

    /// <summary>
    /// 平台
    /// </summary>
    [Display(Name = "Platform"), Description("平台"), MaxLength(32, ErrorMessage = "平台 不能超过 32 个字符")]
    public string Platform { get; set; }

    /// <summary>
    /// 版本
    /// </summary>
    [Display(Name = "Version"), Description("版本"), MaxLength(32, ErrorMessage = "版本 不能超过 32 个字符")]
    public string Version { get; set; }

    /// <summary>
    /// 品牌
    /// </summary>
    [Display(Name = "Brand"), Description("品牌"), MaxLength(32, ErrorMessage = "品牌 不能超过 32 个字符")]
    public string Brand { get; set; }

    /// <summary>
    /// 型号
    /// </summary>
    [Display(Name = "Model"), Description("型号"), MaxLength(32, ErrorMessage = "型号 不能超过 32 个字符")]
    public string Model { get; set; }

    /// <summary>
    /// 应用包名
    /// </summary>
    [Display(Name = "BundleId"), Description("应用包名"), MaxLength(64, ErrorMessage = "应用包名 不能超过 64 个字符")]
    public string BundleId { get; set; }

    /// <summary>
    /// 应用包版本
    /// </summary>
    [Display(Name = "BundleVersion"), Description("应用包版本"), MaxLength(32, ErrorMessage = "应用包版本 不能超过 32 个字符")]
    public string BundleVersion { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Display(Name = "Remark"), Description("备注"), MaxLength(2000, ErrorMessage = "备注 不能超过 2000 个字符")]
    public string Remark { get; set; }
}
