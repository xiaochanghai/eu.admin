using EU.Core.Api.Agent.Security;
using EU.Core.IServices;
using EU.Core.IServices.Agents;
using EU.Core.Model;
using EU.Core.Model.Models;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace EU.Core.Api.Agent.Controllers;


/// <summary>
/// 提供 Agent 定义及版本管理的 HTTP 接口。
/// </summary>
/// <param name="modelProfiles">用于查询可公开展示的模型配置的目录。</param>
/// <param name="agentDefinitionServices">用于管理 Agent 定义及版本的服务。</param>
[Route("api/agents")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class AgentsController(IPublicModelProfileCatalog modelProfiles, IAgAgentDefinitionServices agentDefinitionServices) : Base.ControllerBase
{
    #region 查询列表（List）
    /// <summary>
    /// 查询列表（List）
    /// </summary>
    /// <param name="search">用于筛选记录的搜索文本。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 管理摘要数组，失败时包含错误状态和提示。</returns>
    [HttpGet]
    public async Task<ServiceResult<AgentListItem[]>> List([FromQuery] string? search, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        AgentRuntimeStatus? runtimeStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (string.Equals(status, nameof(AgentRuntimeStatus.Enabled), StringComparison.Ordinal))
            {
                runtimeStatus = AgentRuntimeStatus.Enabled;
            }
            else if (string.Equals(status, nameof(AgentRuntimeStatus.Disabled), StringComparison.Ordinal))
            {
                runtimeStatus = AgentRuntimeStatus.Disabled;
            }
            else if (string.Equals(status, nameof(AgentRuntimeStatus.Archived), StringComparison.Ordinal))
            {
                runtimeStatus = AgentRuntimeStatus.Archived;
            }
            else
            {
                throw new Exception("The status filter is invalid.");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        var definitions = await agentDefinitionServices.QueryAgentList(
            search,
            runtimeStatus?.ToString(),
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        AgentListItem[] values = definitions.Select(definition => new AgentListItem(
            definition.ID,
            definition.Code,
            definition.Name,
            definition.Description,
            ParseRuntimeStatus(definition.RuntimeStatus),
            definition.LogicalRevision ?? throw new InvalidDataException(
                $"Agent '{definition.Code}' does not have a LogicalRevision."),
            definition.DraftLabel,
            definition.DraftModelProfileId,
            definition.CurrentPublishedLabel)).ToArray();
        return ServiceResult<AgentListItem[]>.QuerySuccess(values);
    }
    #endregion

    #region 获取（Get）
    /// <summary>
    /// 获取（Get）
    /// </summary>
    /// <param name="id">Agent 定义标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 定义明细，失败时包含错误状态和提示。</returns>
    [HttpGet("{id:guid}")]
    public async Task<ServiceResult<AgAgentDefinitionDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        AgAgentDefinitionDetailDto? value = await agentDefinitionServices.QueryAgent(
            id,
            cancellationToken);
        if (value is null)
        {
            throw new Exception("The Agent was not found.");
        }

        return ServiceResult<AgAgentDefinitionDetailDto>.QuerySuccess(value);
    }
    #endregion

    #region 创建（Create）
    /// <summary>
    /// 创建（Create）
    /// </summary>
    /// <param name="request">创建Agent 定义所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 定义明细，失败时包含错误状态和提示。</returns>
    [HttpPost]
    [ProducesResponseType(
        typeof(ServiceResult<AgAgentDefinitionDetailDto>),
        StatusCodes.Status201Created)]
    public async Task<ServiceResult<AgAgentDefinitionDetailDto>> Create([FromBody] CreateAgentRequest request, CancellationToken cancellationToken)
    {
        var result = await agentDefinitionServices.CreateAsync(new CreateAgentCommand(request.Code, request.Name, request.Description), cancellationToken);
        if (!result.Success)
        {
            return ServiceResult<AgAgentDefinitionDetailDto>.Failure(
                result.Status,
                result.Message);
        }

        AgAgentDefinitionDetailDto value = await agentDefinitionServices.QueryAgent(
            result.Data,
            cancellationToken)
            ?? throw new InvalidDataException("The newly created Agent could not be loaded.");
        Response.Headers.Location = $"/api/agents/{result.Data}";
        Response.StatusCode = StatusCodes.Status201Created;
        return Success(value, "创建成功");
    }
    #endregion

    #region 保存（SaveDraft）
    /// <summary>
    /// 保存（SaveDraft）
    /// </summary>
    /// <param name="id">Agent 定义标识。</param>
    /// <param name="request">保存草稿Agent 定义所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 定义，失败时包含错误状态和提示。</returns>
    [HttpPut("{id:guid}/draft")]
    public async Task<ServiceResult<AgentDefinition>> SaveDraft(Guid id, [FromBody] SaveAgentDraftRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.ModelProfileId) &&
            !await modelProfiles.ExistsAsync(request.ModelProfileId, cancellationToken))
        {
            throw new Exception("The selected model profile is not available.");
        }

        ServiceResult<AgentDefinition> result = await agentDefinitionServices.SaveDraftAsync(
            new SaveAgentDraftCommand(
                id,
                request.ExpectedLogicalRevision,
                request.Instructions,
                request.ModelProfileId,
                request.OutputMode,
                request.OutputJsonSchema,
                request.Name,
                request.Description,
                request.SkillVersionIds,
                request.ToolVersionIds,
                request.KnowledgeBaseIds)
            {
                ChildAgentIds = request.ChildAgentIds,
                OrchestrationIds = request.OrchestrationIds
            },
            cancellationToken);
        return result;
    }
    #endregion

    #region 发布（Publish）
    /// <summary>
    /// 发布（Publish）
    /// </summary>
    /// <param name="id">Agent 定义标识。</param>
    /// <param name="request">发布Agent 定义所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 定义，失败时包含错误状态和提示。</returns>
    [HttpPost("{id:guid}/publish")]
    public async Task<ServiceResult<AgentDefinition>> Publish(Guid id, [FromBody] ExpectedRevisionRequest request, CancellationToken cancellationToken)
    {
        return await agentDefinitionServices.PublishAsync(
            new PublishAgentCommand(id, request.ExpectedLogicalRevision),
            cancellationToken);
    }
    #endregion

    #region 设置（SetStatus）
    /// <summary>
    /// 设置（SetStatus）
    /// </summary>
    /// <param name="id">Agent 定义标识。</param>
    /// <param name="request">变更状态Agent 定义所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 定义，失败时包含错误状态和提示。</returns>
    [HttpPut("{id:guid}/status")]
    public async Task<ServiceResult<AgentDefinition>> SetStatus(Guid id, [FromBody] SetAgentStatusRequest request, CancellationToken cancellationToken)
    {
        return await agentDefinitionServices.SetRuntimeStatusAsync(
            new SetAgentRuntimeStatusCommand(
                id,
                request.ExpectedLogicalRevision,
                request.RuntimeStatus),
            cancellationToken);
    }
    #endregion

    #region 导出（Export）
    /// <summary>
    /// 导出（Export）
    /// </summary>
    /// <param name="id">Agent 定义标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>成功时下载 UTF-8 编码的 agent-package.json；失败时以 HTTP 200 返回原服务错误包装。</returns>
    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id, CancellationToken cancellationToken)
    {
        ServiceResult<string> result = await agentDefinitionServices.ExportAsync(id, cancellationToken);
        return result.Success
            ? File(
                Encoding.UTF8.GetBytes(result.Data),
                "application/json",
                "agent-package.json")
            : new JsonResult(result)
            {
                StatusCode = StatusCodes.Status200OK
            };
    }
    #endregion

    #region 导入（Import）
    /// <summary>
    /// 导入（Import）
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 定义，失败时包含错误状态和提示。</returns>
    [HttpPost("import")]
    [ProducesResponseType(
        typeof(ServiceResult<AgentDefinition>),
        StatusCodes.Status201Created)]
    public async Task<ServiceResult<AgentDefinition>> Import(CancellationToken cancellationToken)
    {
        if (!IsJsonContentType(Request.ContentType))
        {
            throw new Exception("The Agent package must use a JSON content type.");
        }

        using var reader = new StreamReader(
            Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 4096,
            leaveOpen: true);
        string json = await reader.ReadToEndAsync(cancellationToken);
        ServiceResult<AgentDefinition> result =
            await agentDefinitionServices.ImportAsync(json, cancellationToken);
        if (!result.Success)
        {
            return result;
        }

        Response.Headers.Location = $"/api/agents/{result.Data.Id}";
        Response.StatusCode = StatusCodes.Status201Created;
        return Success(result.Data, "导入成功");
    }
    #endregion

    #region 判断 JSON 内容类型（IsJsonContentType）
    /// <summary>
    /// 忽略媒体类型参数及大小写，判断 HTTP 内容类型是否为 JSON（IsJsonContentType）。
    /// </summary>
    /// <param name="contentType">需要检查的 HTTP 内容类型。</param>
    /// <returns>媒体类型为 application/json 或以 +json 结尾时返回 true；空值或其他类型返回 false。</returns>
    private static bool IsJsonContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        string mediaType = contentType.Split(';', 2)[0].Trim();
        return string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase) ||
               mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
    }
    #endregion

    #region 解析（ParseRuntimeStatus）
    /// <summary>
    /// 解析（ParseRuntimeStatus）
    /// </summary>
    /// <param name="value">区分大小写的 Agent 运行状态文本。</param>
    /// <returns>按区分大小写方式解析的 Agent 运行状态；解析失败时抛出 InvalidDataException。</returns>
    private static AgentRuntimeStatus ParseRuntimeStatus(string value) =>
        Enum.TryParse(value, ignoreCase: false, out AgentRuntimeStatus status)
            ? status
            : throw new InvalidDataException($"Unsupported Agent runtime status '{value}'.");
    #endregion
}

/// <summary>
/// 创建 Agent 定义的请求。
/// </summary>
/// <param name="Code">业务唯一编码。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
public sealed record CreateAgentRequest(string Code, string Name, string Description);

/// <summary>
/// 保存 Agent 草稿的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Instructions">Agent 的系统指令。</param>
/// <param name="ModelProfileId">模型配置标识。</param>
/// <param name="OutputMode">输出内容模式。</param>
/// <param name="OutputJsonSchema">约束结构化输出的 JSON Schema。</param>
/// <param name="SkillVersionIds">绑定的技能版本标识集合。</param>
/// <param name="ToolVersionIds">绑定的工具版本标识集合。</param>
/// <param name="KnowledgeBaseIds">绑定的知识库标识集合。</param>
public sealed record SaveAgentDraftRequest(long ExpectedLogicalRevision, string Name, string Description, string Instructions, string ModelProfileId,
    AgentOutputMode OutputMode, string? OutputJsonSchema, IReadOnlyList<Guid>? SkillVersionIds, IReadOnlyList<Guid>? ToolVersionIds, IReadOnlyList<Guid>? KnowledgeBaseIds)
{
    /// <summary>
    /// 绑定的子 Agent 标识集合。
    /// </summary>
    public IReadOnlyList<Guid>? ChildAgentIds { get; init; }
    /// <summary>
    /// 绑定的编排标识集合。
    /// </summary>
    public IReadOnlyList<Guid>? OrchestrationIds { get; init; }
}

/// <summary>
/// 携带预期逻辑版本的并发控制请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
public sealed record ExpectedRevisionRequest(long ExpectedLogicalRevision);

/// <summary>
/// 设置 Agent 运行状态的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="RuntimeStatus">Agent 的目标运行状态。</param>
public sealed record SetAgentStatusRequest(long ExpectedLogicalRevision, AgentRuntimeStatus RuntimeStatus);
