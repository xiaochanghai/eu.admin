/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* DataScopeTree.cs
*
* 功 能： 数据权限树形结构视图模型
* 类 名： DataScopeTree
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/6/23  EU Team   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│ 此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露． │
*│ 版权所有：EU Team                              │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model;

/// <summary>
/// 数据权限树节点（参考 ModuleTree）
/// 用于前端树形控件展示集团-公司层级结构
/// </summary>
public class DataScopeTree
{
    /// <summary>
    /// 节点标题（显示名称）
    /// </summary>
    public string title { get; set; }

    /// <summary>
    /// 节点键值
    /// 集团节点为集团 GUID，公司节点为公司 GUID
    /// </summary>
    public string key { get; set; }

    /// <summary>
    /// 节点值（实际 ID）
    /// </summary>
    public Guid value { get; set; }

    /// <summary>
    /// 是否叶子节点
    /// </summary>
    public bool? isLeaf { get; set; }

    /// <summary>
    /// 子节点列表
    /// </summary>
    public List<DataScopeTree> children { get; set; }
}
