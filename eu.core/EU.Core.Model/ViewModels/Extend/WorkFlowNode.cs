/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* WorkFlowNode.cs
*
* 功 能： N / A
* 类 名： WorkFlowNode
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
using Newtonsoft.Json;

namespace EU.Core.Model;

#region 自定义功能

/// <summary>
/// 工作流节点视图模型
/// 使用 JsonExtensionData 保留前端扩展字段，确保草稿/发布 JSON 不丢失数据
/// </summary>
public class WorkFlowNode
{
    public string id { get; set; }

    public string nodeType { get; set; }

    public string name { get; set; }

    public WorkFlowNode childNode { get; set; }

    public ApproverSettings approverSettings { get; set; }

    public List<WorkFlowNode> conditionNodeList { get; set; }

    /// <summary>
    /// 扩展数据：存储前端发送的、本类未定义的字段（如 desc, config, transfer 等）
    /// 序列化时会原样写回 JSON，保证草稿和已发布流程数据完整不丢失
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object> ExtensionData { get; set; } = new();
}

/// <summary>
/// 审批人设置
/// 使用 JsonExtensionData 保留前端扩展字段
/// </summary>
public class ApproverSettings
{
    public List<AuditList> auditList { get; set; }

    /// <summary>
    /// 扩展数据：保留前端审批人设置中的其他字段（如 joinType, emptyPass, handleType 等）
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object> ExtensionData { get; set; } = new();
}

public class AuditList
{
    public string userType { get; set; }
    public Guid objectId { get; set; }
    public string label { get; set; }
}
#endregion
