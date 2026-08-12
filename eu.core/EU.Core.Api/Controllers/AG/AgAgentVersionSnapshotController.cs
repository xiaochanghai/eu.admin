/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* AgAgentVersionSnapshot.cs
*
* 功 能： N / A
* 类 名： AgAgentVersionSnapshot
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2026/8/12 14:10:59  SahHsiao   初版
*
* Copyright(c) 2026 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// Agent 发布快照表，冻结发布时的 Agent 运行配置(Controller)
/// </summary>
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_AG)]
public class AgAgentVersionSnapshotController : BaseController<IAgAgentVersionSnapshotServices, AgAgentVersionSnapshot, AgAgentVersionSnapshotDto, InsertAgAgentVersionSnapshotInput, EditAgAgentVersionSnapshotInput>
{
    public AgAgentVersionSnapshotController(IAgAgentVersionSnapshotServices service) : base(service)
    {
    }
}