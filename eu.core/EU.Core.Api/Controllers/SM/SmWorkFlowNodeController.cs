/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmWorkFlowNode.cs
*
* 功 能： N / A
* 类 名： SmWorkFlowNode
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2024/11/29 11:20:21  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 工作流节点(Controller)
/// </summary>
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SM)]
public class SmWorkFlowNodeController : BaseController<ISmWorkFlowNodeServices, SmWorkFlowNode, SmWorkFlowNodeDto, InsertSmWorkFlowNodeInput, EditSmWorkFlowNodeInput>
{
    public SmWorkFlowNodeController(ISmWorkFlowNodeServices service) : base(service)
    {
    }
}