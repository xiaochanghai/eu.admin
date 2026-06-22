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
*│　版权所有：SahHsiao                                │
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
    [HttpPost("Publish/{id}")]
    public Task<ServiceResult> NodeSave([FromBody] WorkFlowNode node, Guid id) => _service.NodeSave(node.childNode, id);
    #endregion

    #region 获取流程节点
    /// <summary>
    /// 获取流程节点
    /// </summary>
    /// <param name="id">流程ID</param>
    /// <returns></returns>
    [HttpGet("QueryNode/{id}")]
    public Task<ServiceResult<WorkFlowNode>> QueryNode(Guid id) => _service.QueryNode(id);
    #endregion

    #region 按模块获取工作流
    /// <summary>
    /// 按模块ID获取工作流（不存在则自动初始化）
    /// 用于流程设计器页面加载时，根据当前模块找到对应的 SmWorkFlow 记录
    /// </summary>
    /// <param name="moduleId">模块ID（SmModules.ID）</param>
    /// <returns>工作流实体（含 FlowJson / DraftJson）</returns>
    [HttpGet("ForModule/{moduleId}")]
    public Task<ServiceResult<SmWorkFlow>> ForModule(Guid moduleId) => _service.GetByModuleId(moduleId);
    #endregion

    #region 按模块获取流程节点
    /// <summary>
    /// 按模块ID获取流程节点树
    /// 加载优先级：草稿 JSON → 已发布 JSON → 从节点表重建
    /// </summary>
    /// <param name="moduleId">模块ID（SmModules.ID）</param>
    /// <returns>流程节点树</returns>
    [HttpGet("QueryNodeByModule/{moduleId}")]
    public Task<ServiceResult<WorkFlowNode>> QueryNodeByModule(Guid moduleId) => _service.QueryNodeByModuleId(moduleId);
    #endregion

    #region 保存草稿
    /// <summary>
    /// 保存未发布的草稿 JSON（设计器实时自动保存，发布后清空）
    /// </summary>
    /// <param name="moduleId">模块ID（SmModules.ID）</param>
    /// <param name="draftJson">草稿节点树 JSON</param>
    /// <returns></returns>
    [HttpPut("SaveDraft/{moduleId}")]
    public Task<ServiceResult> SaveDraft(Guid moduleId, [FromBody] string draftJson) => _service.SaveDraft(moduleId, draftJson);
    #endregion
}