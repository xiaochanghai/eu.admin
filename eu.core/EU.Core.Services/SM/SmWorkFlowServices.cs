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
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

using EU.Core.Model;
using EU.Core.Model.Entity;
using MongoDB.Bson;

namespace EU.Core.Services;

/// <summary>
/// 工作流 (服务)
/// </summary>
public class SmWorkFlowServices : BaseServices<SmWorkFlow, SmWorkFlowDto, InsertSmWorkFlowInput, EditSmWorkFlowInput>, ISmWorkFlowServices
{
    private const string StartNodeId = "start";
    private const string StartNodeType = "start";

    private readonly IBaseRepository<SmWorkFlow> _dal;
    private readonly ISmWorkFlowNodeServices _smWorkFlowNodeServices;
    private readonly ISmWorkFlowNodeAuditServices _smWorkFlowNodeAuditServices;

    /// <summary>
    /// 构造函数,初始化工作流服务
    /// </summary>
    /// <param name="dal">工作流数据访问层</param>
    /// <param name="smWorkFlowNodeServices">工作流节点服务</param>
    /// <param name="smWorkFlowNodeAuditServices">工作流节点审核人员服务</param>
    public SmWorkFlowServices(IBaseRepository<SmWorkFlow> dal, ISmWorkFlowNodeServices smWorkFlowNodeServices, ISmWorkFlowNodeAuditServices smWorkFlowNodeAuditServices)
    {
        this._dal = dal;
        base.BaseDal = dal;
        _smWorkFlowNodeServices = smWorkFlowNodeServices;
        _smWorkFlowNodeAuditServices = smWorkFlowNodeAuditServices;
    }

    #region 流程节点保存
    /// <summary>
    /// 流程节点保存,将工作流节点树结构保存到数据库
    /// </summary>
    /// <param name="node">工作流节点树根节点</param>
    /// <param name="id">工作流ID</param>
    /// <returns>操作结果</returns>
    public async Task<ServiceResult> NodeSave(WorkFlowNode node, Guid id)
    {
        if (node == null)
            return Failed("工作流节点不能为空");

        try
        {
            var (nodes, audits) = ConvertTreeToList(node);

            if (!nodes.Any())
                return Failed("工作流节点数据无效");

            // 使用事务确保数据一致性
            await Db.Ado.BeginTranAsync();
            try
            {
                // 删除旧数据
                await _smWorkFlowNodeServices.Delete(x => x.WorkFlowId == id);
                await _smWorkFlowNodeAuditServices.Delete(x => x.WorkFlowId == id);

                // 设置工作流ID并插入新数据
                nodes.ForEach(x => x.WorkFlowId = id);
                audits.ForEach(x => x.WorkFlowId = id);

                if (nodes.Any())
                    await Db.Insertable(nodes).ExecuteCommandAsync();

                if (audits.Any())
                    await _smWorkFlowNodeAuditServices.Add(audits);

                await Db.Ado.CommitTranAsync();
                return Success();
            }
            catch
            {
                await Db.Ado.RollbackTranAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            return Failed($"保存工作流节点失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 将工作流节点树转换为扁平化列表
    /// </summary>
    /// <param name="root">工作流节点树的根节点</param>
    /// <returns>包含节点列表和审核人员列表的元组</returns>
    public static (List<SmWorkFlowNode> nodes, List<InsertSmWorkFlowNodeAuditInput> audits) ConvertTreeToList(WorkFlowNode root)
    {
        List<SmWorkFlowNode> list = new();
        List<InsertSmWorkFlowNodeAuditInput> auditList = new();

        // 使用递归添加节点及其所有子节点到列表中
        AddNodesToList(root, list, null, auditList);

        return (list, auditList);
    }

    /// <summary>
    /// 递归将节点树转换为列表(深度优先遍历)
    /// </summary>
    /// <param name="node">当前处理的节点</param>
    /// <param name="list">节点列表(输出)</param>
    /// <param name="parentId">父节点ID</param>
    /// <param name="auditList">审核人员列表(输出)</param>
    private static void AddNodesToList(WorkFlowNode node, List<SmWorkFlowNode> list, Guid? parentId, List<InsertSmWorkFlowNodeAuditInput> auditList)
    {
        if (node == null)
            return;

        var nodeId = node.id.ObjToGuid();
        if (!nodeId.HasValue)
            return;

        // 添加当前节点
        list.Add(new SmWorkFlowNode
        {
            NodeId = nodeId,
            ID = nodeId.Value,
            NodeType = node.nodeType,
            NodeName = node.name,
            ParentNodeId = parentId
        });

        // 添加审核人员列表
        if (node.approverSettings?.auditList != null && node.approverSettings.auditList.Any())
        {
            foreach (var audit in node.approverSettings.auditList)
            {
                if (audit?.objectId != null)
                {
                    auditList.Add(new InsertSmWorkFlowNodeAuditInput
                    {
                        NodeId = nodeId,
                        ObjectId = audit.objectId
                    });
                }
            }
        }

        // 递归处理子节点
        if (node.childNode != null)
        {
            AddNodesToList(node.childNode, list, nodeId, auditList);
        }

        // 递归处理条件节点列表
        if (node.conditionNodeList != null && node.conditionNodeList.Any())
        {
            foreach (var child in node.conditionNodeList)
            {
                AddNodesToList(child, list, nodeId, auditList);
            }
        }
    }
    #endregion

    #region 获取流程节点
    /// <summary>
    /// 获取流程节点
    /// </summary>
    /// <param name="id">工作流ID</param>
    /// <returns>流程节点树</returns>
    public async Task<ServiceResult<WorkFlowNode>> QueryNode(Guid id)
    {
        try
        {
            // 并行查询节点和审核人员数据以提高性能
            var nodes = await _smWorkFlowNodeServices.Query(x => x.WorkFlowId == id);
            var audits = await _smWorkFlowNodeAuditServices.Query(x => x.WorkFlowId == id);

            if (!nodes.Any())
                return Success(new WorkFlowNode { id = StartNodeId, nodeType = StartNodeType });

            // 获取所有审核人员ID并查询用户信息
            var auditIds = audits
                .Where(x => x.ObjectId.HasValue)
                .Select(x => x.ObjectId.Value)
                .Distinct()
                .ToList();

            var users = auditIds.Any()
                ? await Db.Queryable<SmUsers>()
                    .In(x => x.ID, auditIds)
                    .ToListAsync()
                : new List<SmUsers>();

            // 构建用户字典以提高查询性能
            var userDict = users.ToDictionary(x => x.ID, x => x);

            // 构建起始节点
            var rootNode = new WorkFlowNode
            {
                id = StartNodeId,
                nodeType = StartNodeType,
            };

            // 递归构建节点树
            BuildNodeTree(nodes, audits, rootNode, userDict);

            return Success(rootNode);
        }
        catch (Exception ex)
        {
            return Failed<WorkFlowNode>($"查询流程节点失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 递归构建节点树(将扁平化的节点列表转换为树形结构)
    /// </summary>
    /// <param name="nodes">所有节点的扁平化列表</param>
    /// <param name="audits">所有审核人员的扁平化列表</param>
    /// <param name="parentNode">当前父节点</param>
    /// <param name="userDict">用户字典,用于快速查找用户信息</param>
    private static void BuildNodeTree(List<SmWorkFlowNode> nodes, List<SmWorkFlowNodeAudit> audits, WorkFlowNode parentNode, Dictionary<Guid, SmUsers> userDict)
    {
        if (parentNode == null || nodes == null || !nodes.Any())
            return;

        var parentId = parentNode.id == StartNodeId ? (Guid?)null : Guid.Parse(parentNode.id);

        // 查找所有子节点（包括普通子节点和条件节点）
        var childNodes = nodes.Where(x => x.ParentNodeId == parentId).ToList();

        if (!childNodes.Any())
            return;

        // 如果只有一个子节点,设置为 childNode
        if (childNodes.Count == 1)
        {
            var workFlowNode = CreateWorkFlowNode(childNodes[0], audits, userDict);
            parentNode.childNode = workFlowNode;

            // 递归处理子节点
            BuildNodeTree(nodes, audits, workFlowNode, userDict);
        }
        else
        {
            // 多个子节点,构建为条件节点列表
            parentNode.conditionNodeList = new List<WorkFlowNode>();

            foreach (var childNode in childNodes)
            {
                var workFlowNode = CreateWorkFlowNode(childNode, audits, userDict);
                parentNode.conditionNodeList.Add(workFlowNode);

                // 递归处理每个条件节点的子节点
                BuildNodeTree(nodes, audits, workFlowNode, userDict);
            }
        }
    }

    /// <summary>
    /// 创建工作流节点对象,并填充审核人员信息
    /// </summary>
    /// <param name="node">数据库中的节点实体</param>
    /// <param name="audits">该节点的审核人员列表</param>
    /// <param name="userDict">用户字典,用于快速查找用户信息</param>
    /// <returns>包含完整信息的工作流节点对象</returns>
    private static WorkFlowNode CreateWorkFlowNode(SmWorkFlowNode node, List<SmWorkFlowNodeAudit> audits, Dictionary<Guid, SmUsers> userDict)
    {
        if (node == null)
            return null;

        var auditList = audits
            .Where(y => y.NodeId == node.ID && y.ObjectId.HasValue)
            .Select(y =>
            {
                var userName = userDict.TryGetValue(y.ObjectId.Value, out var user) ? user.UserName : null;
                return new AuditList
                {
                    userType = node.NodeType,
                    label = userName,
                    objectId = y.ObjectId.Value
                };
            })
            .ToList();

        return new WorkFlowNode
        {
            id = node.ID.ToString(),
            nodeType = node.NodeType,
            name = node.NodeName,
            approverSettings = auditList.Any() ? new ApproverSettings
            {
                auditList = auditList
            } : null
        };
    }
    #endregion
}