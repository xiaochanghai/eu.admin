/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmImpTemplate.cs
*
*功 能： N / A
* 类 名： SmImpTemplate
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/22 9:39:17  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 系统导入模板(Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SM)]
public class SmImpTemplateController : BaseController<ISmImpTemplateServices, SmImpTemplate, SmImpTemplateDto, InsertSmImpTemplateInput, EditSmImpTemplateInput>
{
    public SmImpTemplateController(ISmImpTemplateServices service) : base(service)
    {
    }

    #region 根据模板ID获取明细
    /// <summary>
    /// 根据模板ID获取明细
    /// </summary>
    /// <param name="moduleId">模板ID</param> 
    /// <returns></returns>
    [HttpGet("QueryByModuleId/{moduleId}")]
    public async Task<ServiceResult<SmImpTemplateDto>> QueryByModuleId(Guid moduleId)=> await _service.QueryByModuleId(moduleId);
    #endregion
}