/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* AgModelConfig.cs
*
* 功 能： N / A
* 类 名： AgModelConfig
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2026/9/9 23:28:44  SahHsiao   初版
*
* Copyright(c) 2026 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model.Entity;

/// <summary>
/// 平台共享模型配置，API 密钥仅以密文持久化。 (Model)
/// </summary>
[SugarTable("AgModelConfig", "平台共享模型配置，API 密钥仅以密文持久化。"), Entity(TableCnName = "平台共享模型配置，API 密钥仅以密文持久化。", TableName = "AgModelConfig")]
public class AgModelConfig : BasePoco
{

    /// <summary>
    /// 不可变的公开模型配置标识，区分大小写且唯一，供 Agent 引用。
    /// </summary>
    [Display(Name = "ProfileCode"), Description("不可变的公开模型配置标识，区分大小写且唯一，供 Agent 引用。"), SugarColumn(IsNullable = true, Length = 200)]
    public string ProfileCode { get; set; }

    /// <summary>
    /// 模型配置在管理页面显示的名称。
    /// </summary>
    [Display(Name = "DisplayName"), Description("模型配置在管理页面显示的名称。"), SugarColumn(IsNullable = true, Length = 200)]
    public string DisplayName { get; set; }

    /// <summary>
    /// 模型协议提供商，目前仅支持 OpenAICompatible。
    /// </summary>
    [Display(Name = "Provider"), Description("模型协议提供商，目前仅支持 OpenAICompatible。"), SugarColumn(IsNullable = true, Length = 32)]
    public string Provider { get; set; }

    /// <summary>
    /// 经部署白名单校验的 HTTPS 模型服务基础地址。
    /// </summary>
    [Display(Name = "Endpoint"), Description("经部署白名单校验的 HTTPS 模型服务基础地址。"), SugarColumn(IsNullable = true, Length = 512)]
    public string Endpoint { get; set; }

    /// <summary>
    /// 供应商实际模型名称，可以不同于公开模型配置标识。
    /// </summary>
    [Display(Name = "ModelName"), Description("供应商实际模型名称，可以不同于公开模型配置标识。"), SugarColumn(IsNullable = true, Length = 200)]
    public string ModelName { get; set; }

    /// <summary>
    /// 绑定当前记录 ID 的 AES-GCM API 密钥密文，禁止明文存储、接口输出或日志记录。
    /// </summary>
    [Display(Name = "ApiKeyCiphertext"), Description("绑定当前记录 ID 的 AES-GCM API 密钥密文，禁止明文存储、接口输出或日志记录。"), SugarColumn(IsNullable = true, Length = 5600)]
    [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
    public string ApiKeyCiphertext { get; set; }

    /// <summary>
    /// 凭据修订号，更新 API 密钥时递增。
    /// </summary>
    [Display(Name = "CredentialRevision"), Description("凭据修订号，更新 API 密钥时递增。")]
    public long? CredentialRevision { get; set; }

    /// <summary>
    /// 配置乐观并发修订号，用于防止并发更新覆盖。
    /// </summary>
    [Display(Name = "LogicalRevision"), Description("配置乐观并发修订号，用于防止并发更新覆盖。")]
    public long? LogicalRevision { get; set; }

    /// <summary>
    /// 模型调用启用开关；1 允许新运行使用该配置，0 禁用。
    /// </summary>
    [Display(Name = "Enabled"), Description("模型调用启用开关；1 允许新运行使用该配置，0 禁用。")]
    public bool? Enabled { get; set; }

    /// <summary>
    /// 模型请求超时秒数，允许范围 5 至 600。
    /// </summary>
    [Display(Name = "TimeoutSeconds"), Description("模型请求超时秒数，允许范围 5 至 600。")]
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Qwen 思考开关；1 开启，0 关闭，NULL 使用供应商默认。
    /// </summary>
    [Display(Name = "EnableThinking"), Description("Qwen 思考开关；1 开启，0 关闭，NULL 使用供应商默认。"), SugarColumn(IsNullable = true)]
    public bool? EnableThinking { get; set; }
}
