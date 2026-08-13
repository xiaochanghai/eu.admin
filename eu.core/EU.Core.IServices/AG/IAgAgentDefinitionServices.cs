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
using EU.Core.Agent.Application.Agents;
using EU.Core.Model.ViewModels.Extend;

#nullable enable

namespace EU.Core.IServices;

/// <summary>
/// Agent 定义表(自定义服务接口)
/// </summary>	
public interface IAgAgentDefinitionServices : IBaseServices<AgAgentDefinition, AgAgentDefinitionDto, InsertAgAgentDefinitionInput, EditAgAgentDefinitionInput>
{
    /// <summary>
    /// 查询 Agent 管理列表，并批量加载草稿及最新发布版本摘要。
    /// </summary>
    Task<List<AgAgentDefinitionDto>> QueryAgentList(string? search = null, string? runtimeStatus = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询 Agent 明细及其版本、快照和资源绑定。
    /// </summary>
    Task<AgAgentDefinitionDetailDto?> QueryAgent(Guid id, CancellationToken cancellationToken = default);

    Task<AgentOperationResult<AgentDefinition>> CreateAsync(CreateAgentCommand command, CancellationToken cancellationToken = default);

    Task<AgentOperationResult<AgentDefinition>> CreateImportedAsync(ImportAgentCommand command, CancellationToken cancellationToken = default);

    Task<AgentOperationResult<AgentDefinition>> SaveDraftAsync(SaveAgentDraftCommand command, CancellationToken cancellationToken = default);

    Task<AgentOperationResult<AgentDefinition>> SetRuntimeStatusAsync(SetAgentRuntimeStatusCommand command, CancellationToken cancellationToken = default);

    Task<AgentOperationResult<AgentDefinition>> PublishAsync(PublishAgentCommand command, CancellationToken cancellationToken = default);

    Task<AgentOperationResult<string>> ExportAsync(Guid agentId, CancellationToken cancellationToken = default);

    Task<AgentOperationResult<AgentDefinition>> ImportAsync(string json, CancellationToken cancellationToken = default);
}
