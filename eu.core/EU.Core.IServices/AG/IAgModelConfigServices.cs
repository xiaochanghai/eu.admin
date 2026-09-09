/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* AgModelConfig.cs
*
* 功 能： N / A
* 类 名： AgModelConfig
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2026/9/9 23:28:44  SahHsiao   初版
*
* Copyright(c) 2026 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.IServices;

/// <summary>
/// 平台共享模型配置，API 密钥仅以密文持久化。(自定义服务接口)
/// </summary>	
public interface IAgModelConfigServices : IBaseServices<AgModelConfig, AgModelConfigDto, InsertAgModelConfigInput, EditAgModelConfigInput>
{
}