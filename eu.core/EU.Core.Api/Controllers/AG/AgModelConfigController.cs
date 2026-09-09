/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* AgModelConfig.cs
*
* 功 能： N / A
* 类 名： AgModelConfig
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2026/9/9 22:53:25  SahHsiao   初版
*
* Copyright(c) 2026 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 平台共享模型配置，API 密钥仅以密文持久化。(Controller)
/// </summary>
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_AG)]
public class AgModelConfigController : BaseController<IAgModelConfigServices, AgModelConfig, AgModelConfigDto, InsertAgModelConfigInput, EditAgModelConfigInput>
{
    public AgModelConfigController(IAgModelConfigServices service) : base(service)
    {
    }
}