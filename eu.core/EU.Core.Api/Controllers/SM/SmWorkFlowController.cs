/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmWorkFlow.cs
*
* 功 能： N / A
* 类 名： SmWorkFlow
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2024/11/26 19:51:14  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 工作流(Controller)
/// </summary>
//[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SM)]
public class SmWorkFlowController : BaseController<ISmWorkFlowServices, SmWorkFlow, SmWorkFlowDto, InsertSmWorkFlowInput, EditSmWorkFlowInput>
{
    public SmWorkFlowController(ISmWorkFlowServices service) : base(service)
    {
    }

    #region 流程节点保存
    /// <summary>
    /// 流程节点保存
    /// </summary>
    /// <param name="node">节点数据</param>
    /// <param name="id">流程ID</param>
    /// <returns></returns>
    [HttpPost("{id}")]
    public Task<ServiceResult> NodeSave([FromBody] WorkFlowNode node, Guid id) => _service.NodeSave(node, id);
    #endregion
}