/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmWorkFlow.cs
*
* 功 能： N / A
* 类 名： SmWorkFlow
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2024/11/26 19:51:15  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 工作流 (服务)
/// </summary>
public class SmWorkFlowServices : BaseServices<SmWorkFlow, SmWorkFlowDto, InsertSmWorkFlowInput, EditSmWorkFlowInput>, ISmWorkFlowServices
{
    private readonly IBaseRepository<SmWorkFlow> _dal;
    private readonly ISmWorkFlowNodeServices _smWorkFlowNodeServices;
    public SmWorkFlowServices(IBaseRepository<SmWorkFlow> dal, ISmWorkFlowNodeServices smWorkFlowNodeServices)
    {
        this._dal = dal;
        base.BaseDal = dal;
        _smWorkFlowNodeServices = smWorkFlowNodeServices;
    }

    #region 流程节点保存
    /// <summary>
    /// 流程节点保存
    /// </summary>
    /// <returns></returns>
    public async Task<ServiceResult> NodeSave(WorkFlowNode node, Guid id)
    {
        var list = ConvertTreeToList(node);
        await _smWorkFlowNodeServices.Delete(x => x.WorkFlowId == id);
        list.ForEach(x => x.WorkFlowId = id);
        await _smWorkFlowNodeServices.Add(list);
        return Success();
    }

    public static List<InsertSmWorkFlowNodeInput> ConvertTreeToList(WorkFlowNode root)
    {
        List<InsertSmWorkFlowNodeInput> list = new();

        // 使用递归添加节点及其所有子节点到列表中
        AddNodesToList(root, list, null);


        return list;
    }

    private static void AddNodesToList(WorkFlowNode node, List<InsertSmWorkFlowNodeInput> list, string parentId)
    {
        if (node == null)
        {
            return;
        }

        // 添加当前节点
        list.Add(new InsertSmWorkFlowNodeInput()
        {
            NodeId = node.id,
            NodeType = node.nodeType,
            NodeName = node.name,
            ParentNodeId = parentId
        });

        if (node.childNode.IsNotEmptyOrNull())
            AddNodesToList(node.childNode, list, node.id);

        // 对于每个子节点，递归调用此方法
        if (node.conditionNodeList.IsNotEmptyOrNull())
            foreach (var child in node.conditionNodeList)
            {
                AddNodesToList(child, list, node.id);
            }
    }
    #endregion
}