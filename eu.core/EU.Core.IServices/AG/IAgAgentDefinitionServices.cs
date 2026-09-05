/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* AgAgentDefinition.cs
*
* 功 能： N / A
* 类 名： AgAgentDefinition
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2026/8/12 0:58:24  SahHsiao   初版
*
* Copyright(c) 2026 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
using EU.Core.IServices.Agents;
using EU.Core.Model.ViewModels.Extend;

#nullable enable

namespace EU.Core.IServices;

/// <summary>
/// Agent 定义表(自定义服务接口)
/// </summary>	
public interface IAgAgentDefinitionServices : IBaseServices<AgAgentDefinition, AgAgentDefinitionDto, InsertAgAgentDefinitionInput, EditAgAgentDefinitionInput>
{
    #region 查询 Agent 管理列表，并批量加载草稿及最新发布版本摘要。
    /// <summary>
    /// 查询 Agent 管理列表，并批量加载草稿及最新发布版本摘要。
    /// </summary>
    /// <param name="search">用于筛选记录的搜索文本。</param>
    /// <param name="runtimeStatus">Agent 运行时启用状态。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>按编码及标识排序的 Agent 列表摘要，包含草稿和最新发布版本标签；跳过没有唯一草稿的定义。</returns>
    Task<List<AgAgentDefinitionDto>> QueryAgentList(string? search = null, string? runtimeStatus = null, CancellationToken cancellationToken = default);
    #endregion

    #region 查询 Agent 明细及其版本、快照和资源绑定。
    /// <summary>
    /// 查询 Agent 明细及其版本、快照和资源绑定。
    /// </summary>
    /// <param name="id">Agent 定义标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>包含草稿、发布版本、快照及绑定的 Agent 明细；定义不存在或没有版本时为 null。</returns>
    Task<AgAgentDefinitionDetailDto?> QueryAgent(Guid id, CancellationToken cancellationToken = default);
    #endregion

    #region 创建Agent 定义。
    /// <summary>创建Agent 定义。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含新建 Agent 标识，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<Guid>> CreateAsync(CreateAgentCommand command, CancellationToken cancellationToken = default);
    #endregion
    #region 根据导入包创建 Agent 定义及版本。
    /// <summary>根据导入包创建 Agent 定义及版本。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<AgentDefinition>> CreateImportedAsync(ImportAgentCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region 保存Agent 定义草稿。
    /// <summary>保存Agent 定义草稿。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<AgentDefinition>> SaveDraftAsync(SaveAgentDraftCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region 设置 Agent 的运行状态。
    /// <summary>设置 Agent 的运行状态。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<AgentDefinition>> SetRuntimeStatusAsync(SetAgentRuntimeStatusCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region 发布Agent 定义。
    /// <summary>发布Agent 定义。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<AgentDefinition>> PublishAsync(PublishAgentCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region 导出 Agent 定义及其版本包。
    /// <summary>导出 Agent 定义及其版本包。</summary>
    /// <param name="agentId">Agent 定义标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 包 JSON 文本，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<string>> ExportAsync(Guid agentId, CancellationToken cancellationToken = default);
    #endregion

    #region 导入 Agent 定义及其版本包。
    /// <summary>导入 Agent 定义及其版本包。</summary>
    /// <param name="json">待反序列化及校验的 Agent 包 JSON 文本。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<AgentDefinition>> ImportAsync(string json, CancellationToken cancellationToken = default);
    #endregion
}

/// <summary>
/// 提供 Agent 定义及已发布版本的运行时查询能力。
/// </summary>
public interface IAgentDefinitionCatalog
{
    #region 获取指定 Agent 的完整定义。
    /// <summary>获取指定 Agent 的完整定义。</summary>
    /// <param name="id">Agent 定义标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>包含版本及快照的 Agent 定义；定义不存在或没有版本时为 null。</returns>
    Task<AgentDefinition?> GetDefinitionAsync(Guid id, CancellationToken cancellationToken = default);
    #endregion

    #region 查询可用 Agent 定义列表。
    /// <summary>查询可用 Agent 定义列表。</summary>
    /// <param name="query">查询筛选条件。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>按编码及标识排序、包含草稿及发布快照的完整 Agent 定义集合；跳过没有唯一草稿的定义。</returns>
    Task<IReadOnlyList<AgentDefinition>> ListDefinitionsAsync(AgentDefinitionQuery query, CancellationToken cancellationToken = default);
    #endregion
}
