/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* AgAgentVersionSnapshot.cs
*
* 功 能： N / A
* 类 名： AgAgentVersionSnapshot
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2026/8/12 14:11:00  SahHsiao   初版
*
* Copyright(c) 2026 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// Agent 发布快照表，冻结发布时的 Agent 运行配置 (服务)
/// </summary>
public class AgAgentVersionSnapshotServices : BaseServices<AgAgentVersionSnapshot, AgAgentVersionSnapshotDto, InsertAgAgentVersionSnapshotInput, EditAgAgentVersionSnapshotInput>, IAgAgentVersionSnapshotServices
{
    #region 构造（AgAgentVersionSnapshotServices）
    /// <summary>
    /// 构造（AgAgentVersionSnapshotServices）
    /// </summary>
    /// <param name="dal">当前服务使用的数据访问仓储。</param>
    public AgAgentVersionSnapshotServices(IBaseRepository<AgAgentVersionSnapshot> dal)
    {
        BaseDal = dal;
    }
    #endregion
}