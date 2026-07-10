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
*│ 此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露． │
*│ 版权所有：SahHsiao                              │
*└──────────────────────────────────┘
*/
using EU.Core.Model.Entity;

namespace EU.Core.IServices;

/// <summary>
/// 工作流(自定义服务接口)
/// </summary>
public interface ISmWorkFlowServices : IBaseServices<SmWorkFlow, SmWorkFlowDto, InsertSmWorkFlowInput, EditSmWorkFlowInput>
{
    /// <summary>
    /// 保存工作流节点（发布时使用）
    /// </summary>
    /// <param name="node">节点树</param>
    /// <param name="id">工作流ID</param>
    Task<ServiceResult> NodeSave(WorkFlowNode node, Guid id);

    /// <summary>
    /// 根据工作流ID获取流程节点树
    /// </summary>
    /// <param name="id">工作流ID</param>
    Task<ServiceResult<WorkFlowNode>> QueryNode(Guid id);

    /// <summary>
    /// 根据模块ID获取工作流（不存在则自动初始化一条）
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    Task<ServiceResult<SmWorkFlow>> GetByModuleId(Guid moduleId);

    /// <summary>
    /// 根据模块ID获取流程节点树（优先草稿 → 已发布 → 节点表重建）
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    Task<ServiceResult<WorkFlowNode>> QueryNodeByModuleId(Guid moduleId);

}
