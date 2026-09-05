/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* AgAgentVersionBinding.cs
*
* 功 能： N / A
* 类 名： AgAgentVersionBinding
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2026/8/12 14:11:01  SahHsiao   初版
*
* Copyright(c) 2026 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// Agent 版本资源绑定表，统一保存 Skill、MCP 工具、知识库、子 Agent 和编排绑定 (服务)
/// </summary>
public class AgAgentVersionBindingServices : BaseServices<AgAgentVersionBinding, AgAgentVersionBindingDto, InsertAgAgentVersionBindingInput, EditAgAgentVersionBindingInput>, IAgAgentVersionBindingServices
{
    #region 构造（AgAgentVersionBindingServices）
    /// <summary>
    /// 构造（AgAgentVersionBindingServices）
    /// </summary>
    /// <param name="dal">当前服务使用的数据访问仓储。</param>
    public AgAgentVersionBindingServices(IBaseRepository<AgAgentVersionBinding> dal)
    {
        BaseDal = dal;
    }
    #endregion
}