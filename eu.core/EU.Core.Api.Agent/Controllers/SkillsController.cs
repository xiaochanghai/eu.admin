using EU.Core.IServices.Skills;
using EU.Core.Api.Agent.Errors;
using EU.Core.Api.Agent.Security;
using EU.Core.IServices;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text;

namespace EU.Core.Api.Agent.Controllers;

// 文件职责：SkillsController 接口处理

/// <summary>
/// 提供技能定义及版本管理的 HTTP 接口。
/// </summary>
/// <param name="lifecycle">用于管理技能定义及发布版本的服务。</param>
/// <param name="agents">用于查询 Agent 定义及已发布版本的目录。</param>
[Route("api/skills")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class SkillsController(
    IAgSkillDefinitionServices lifecycle,
    IAgentDefinitionCatalog agents) : Base.ControllerBase
{
    #region 查询列表（List）
    /// <summary>
    /// 查询列表（List）
    /// </summary>
    /// <param name="search">用于筛选记录的搜索文本。</param>
    /// <param name="category">用于筛选技能的分类。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能摘要集合，失败时包含错误状态和提示。</returns>
    [HttpGet]
    public async Task<ActionResult<ServiceResult<IReadOnlyList<SkillListItem>>>> List(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        SkillStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (string.Equals(status, nameof(SkillStatus.Active), StringComparison.Ordinal))
            {
                parsedStatus = SkillStatus.Active;
            }
            else if (string.Equals(status, nameof(SkillStatus.Archived), StringComparison.Ordinal))
            {
                parsedStatus = SkillStatus.Archived;
            }
            else
            {
                return FromError(
                    SkillErrorCodes.LifecycleTransitionInvalid,
                    "Skill status must be Active or Archived.",
                    StatusCodes.Status400BadRequest);
            }
        }

        IReadOnlyList<SkillListItem> values = await lifecycle.ListAsync(
            new SkillQuery(search, category, parsedStatus), cancellationToken);
        return ServiceResult<IReadOnlyList<SkillListItem>>.QuerySuccess(values);
    }
    #endregion

    #region 获取（Get）
    /// <summary>
    /// 获取（Get）
    /// </summary>
    /// <param name="id">技能标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能及版本详情，失败时包含错误状态和提示。</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceResult<SkillDefinitionDetailResponse>>> Get(Guid id, CancellationToken cancellationToken)
    {
        SkillDefinition? skill = await lifecycle.GetAsync(id, cancellationToken);
        if (skill is null)
        {
            return FromError(SkillErrorCodes.NotFound, "The Skill was not found.");
        }

        IReadOnlyList<AgentDefinition> agentDefinitions = await agents.ListDefinitionsAsync(
            new AgentDefinitionQuery(),
            cancellationToken);
        var value = new SkillDefinitionDetailResponse(
            skill.Id,
            skill.Code,
            skill.Name,
            skill.Description,
            skill.Category,
            skill.Status,
            skill.DraftRevision,
            skill.PublishedVersions.Select(version => new SkillPublishedVersionResponse(
                version.Id,
                version.Label,
                version.ManifestSha256,
                version.PublishedAtUtc,
                version.Files,
                agentDefinitions
                    .Where(agent =>
                        agent.Draft.SkillVersionIds.Contains(version.Id) ||
                        agent.PublishedVersions.Any(published =>
                            published.Snapshot?.Skills.Any(binding =>
                                binding.SkillVersionId == version.Id) == true))
                    .Select(agent => new SkillBoundAgentResponse(agent.Id, agent.Code, agent.Name))
                    .DistinctBy(agent => agent.Id)
                    .OrderBy(agent => agent.Code, StringComparer.Ordinal)
                    .ToArray()))
                .ToArray());
        return ServiceResult<SkillDefinitionDetailResponse>.QuerySuccess(value);
    }
    #endregion

    #region 创建（Create）
    /// <summary>
    /// 创建（Create）
    /// </summary>
    /// <param name="request">创建技能所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能定义，失败时包含错误状态和提示。</returns>
    [HttpPost]
    public async Task<ActionResult<ServiceResult<SkillDefinition>>> Create([FromBody] CreateSkillRequest request, CancellationToken cancellationToken)
    {
        ServiceResult<SkillDefinition> result = await lifecycle.CreateAsync(
            new CreateSkillCommand(
                request.Code,
                request.Name,
                request.Description,
                request.Category),
            cancellationToken);
        if (!result.Success)
        {
            return FromServiceError(result);
        }

        Response.Headers.Location = $"/api/skills/{result.Data.Id}";
        return new JsonResult(
            Success(result.Data, "创建成功"))
        {
            StatusCode = StatusCodes.Status201Created
        };
    }
    #endregion

    #region 更新（Update）
    /// <summary>
    /// 更新（Update）
    /// </summary>
    /// <param name="id">技能标识。</param>
    /// <param name="request">更新技能所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能定义，失败时包含错误状态和提示。</returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ServiceResult<SkillDefinition>>> Update(Guid id, [FromBody] UpdateSkillRequest request, CancellationToken cancellationToken)
    {
        ServiceResult<SkillDefinition> result = await lifecycle.UpdateAsync(
            new UpdateSkillCommand(
                id,
                request.ExpectedDraftRevision,
                request.Name,
                request.Description,
                request.Category),
            cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }
    #endregion

    #region 查询列表（ListFiles）
    /// <summary>
    /// 查询列表（ListFiles）
    /// </summary>
    /// <param name="id">技能标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能文件条目集合，失败时包含错误状态和提示。</returns>
    [HttpGet("{id:guid}/files")]
    public async Task<ActionResult<ServiceResult<IReadOnlyList<SkillFileEntry>>>> ListFiles(Guid id, CancellationToken cancellationToken)
    {
        ServiceResult<IReadOnlyList<SkillFileEntry>> result =
            await lifecycle.ListFilesAsync(id, cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }
    #endregion

    #region 读取（ReadFile）
    /// <summary>
    /// 读取（ReadFile）
    /// </summary>
    /// <param name="id">技能标识。</param>
    /// <param name="path">相对于技能草稿目录的文件路径。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>成功时返回 UTF-8 纯文本文件内容，失败时返回技能服务错误的 JSON 响应。</returns>
    [HttpGet("{id:guid}/files/content")]
    public async Task<IActionResult> ReadFile(Guid id, [FromQuery] string? path, CancellationToken cancellationToken)
    {
        ServiceResult<string> result = await lifecycle.ReadFileAsync(
            id,
            path ?? string.Empty,
            cancellationToken);
        return result.Success
            ? Content(result.Data, "text/plain", Encoding.UTF8)
            : FromServiceError(result);
    }
    #endregion

    #region 保存（SaveFile）
    /// <summary>
    /// 保存（SaveFile）
    /// </summary>
    /// <param name="id">技能标识。</param>
    /// <param name="request">保存草稿文件技能所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能定义，失败时包含错误状态和提示。</returns>
    [HttpPut("{id:guid}/files/content")]
    public async Task<ActionResult<ServiceResult<SkillDefinition>>> SaveFile(
        Guid id,
        [FromBody] SaveSkillFileRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<SkillDefinition> result = await lifecycle.SaveFileAsync(
            new SaveSkillFileCommand(
                id,
                request.ExpectedDraftRevision,
                request.Path,
                request.Content),
            cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }
    #endregion

    #region 删除（DeleteFile）
    /// <summary>
    /// 删除（DeleteFile）
    /// </summary>
    /// <param name="id">技能标识。</param>
    /// <param name="request">删除草稿文件技能所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能定义，失败时包含错误状态和提示。</returns>
    [HttpDelete("{id:guid}/files/content")]
    public async Task<ActionResult<ServiceResult<SkillDefinition>>> DeleteFile(
        Guid id,
        [FromBody] DeleteSkillFileRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<SkillDefinition> result = await lifecycle.DeleteFileAsync(
            new DeleteSkillFileCommand(
                id,
                request.ExpectedDraftRevision,
                request.Path),
            cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }
    #endregion

    #region 发布（Publish）
    /// <summary>
    /// 发布（Publish）
    /// </summary>
    /// <param name="id">技能标识。</param>
    /// <param name="request">发布技能所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能定义，失败时包含错误状态和提示。</returns>
    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<ServiceResult<SkillDefinition>>> Publish(
        Guid id,
        [FromBody] PublishSkillRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<SkillDefinition> result = await lifecycle.PublishAsync(
            new PublishSkillCommand(
                id,
                request.ExpectedDraftRevision,
                request.VersionLabel),
            cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }
    #endregion

    #region 设置（SetArchived）
    /// <summary>
    /// 设置（SetArchived）
    /// </summary>
    /// <param name="id">技能标识。</param>
    /// <param name="request">归档或恢复技能所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能定义，失败时包含错误状态和提示。</returns>
    [HttpPut("{id:guid}/archive")]
    public async Task<ActionResult<ServiceResult<SkillDefinition>>> SetArchived(
        Guid id,
        [FromBody] SetSkillArchiveRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<SkillDefinition> result = await lifecycle.SetArchivedAsync(
            new SetSkillArchiveCommand(id, request.ExpectedDraftRevision, request.Archived),
            cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }
    #endregion

    #region 转换（FromServiceError）
    /// <summary>
    /// 转换（FromServiceError）
    /// </summary>
    /// <param name="result">操作结果。</param>
    /// <typeparam name="T">待处理数据的泛型类型。</typeparam>
    /// <returns>将技能服务状态转换为业务错误码后生成的统一失败响应。</returns>
    private JsonResult FromServiceError<T>(ServiceResult<T> result) =>
        FromError(SkillServiceStatusCodes.ToErrorCode(result.Status), result.Message);
    #endregion

    #region 转换（FromError）
    /// <summary>
    /// 转换（FromError）
    /// </summary>
    /// <param name="errorCode">操作失败对应的业务错误码。</param>
    /// <param name="message">消息或提示文本。</param>
    /// <param name="httpStatus">需要写入响应的 HTTP 状态码。</param>
    /// <returns>包含技能错误码和请求跟踪标识的失败响应；HTTP 状态依次取显式参数、错误解析器结果或 500。</returns>
    private JsonResult FromError(string errorCode, string message, int? httpStatus = null)
    {
        AgentApiErrorDescriptor descriptor = AgentApiErrorResolver.Resolve(HttpContext, errorCode);
        return new JsonResult(
            ServiceResult<AgentApiErrorData>.Failure(
                descriptor.Status,
                message,
                new AgentApiErrorData(errorCode, HttpContext.TraceIdentifier)))
        {
            StatusCode = httpStatus ?? descriptor.HttpStatus ?? StatusCodes.Status500InternalServerError
        };
    }
    #endregion
}

/// <summary>
/// 创建技能定义的请求。
/// </summary>
/// <param name="Code">业务唯一编码。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Category">技能分类。</param>
public sealed record CreateSkillRequest(
    string Code,
    string Name,
    string Description,
    string Category);

/// <summary>
/// 更新技能定义的请求。
/// </summary>
/// <param name="ExpectedDraftRevision">用于乐观并发控制的预期草稿版本。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Category">技能分类。</param>
public sealed record UpdateSkillRequest(
    long ExpectedDraftRevision,
    string Name,
    string Description,
    string Category);

/// <summary>
/// 保存技能文件的请求。
/// </summary>
/// <param name="ExpectedDraftRevision">用于乐观并发控制的预期草稿版本。</param>
/// <param name="Path">技能包内的相对文件路径。</param>
/// <param name="Content">文本内容。</param>
public sealed record SaveSkillFileRequest(
    long ExpectedDraftRevision,
    string Path,
    string Content);

/// <summary>
/// 删除技能文件的请求。
/// </summary>
/// <param name="ExpectedDraftRevision">用于乐观并发控制的预期草稿版本。</param>
/// <param name="Path">技能包内的相对文件路径。</param>
public sealed record DeleteSkillFileRequest(
    long ExpectedDraftRevision,
    string Path);

/// <summary>
/// 发布技能版本的请求。
/// </summary>
/// <param name="ExpectedDraftRevision">用于乐观并发控制的预期草稿版本。</param>
/// <param name="VersionLabel">发布版本标签。</param>
public sealed record PublishSkillRequest(
    long ExpectedDraftRevision,
    string VersionLabel);

/// <summary>
/// 设置技能归档状态的请求。
/// </summary>
/// <param name="ExpectedDraftRevision">用于乐观并发控制的预期草稿版本。</param>
/// <param name="Archived">是否设置为归档状态。</param>
public sealed record SetSkillArchiveRequest(
    long ExpectedDraftRevision,
    bool Archived);

/// <summary>
/// 技能定义详情响应。
/// </summary>
/// <param name="Id">对象标识。</param>
/// <param name="Code">业务唯一编码。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Category">技能分类。</param>
/// <param name="Status">当前状态。</param>
/// <param name="DraftRevision">当前草稿版本。</param>
/// <param name="PublishedVersions">已发布版本集合。</param>
public sealed record SkillDefinitionDetailResponse(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string Category,
    SkillStatus Status,
    long DraftRevision,
    IReadOnlyList<SkillPublishedVersionResponse> PublishedVersions);

/// <summary>
/// 已发布技能版本响应。
/// </summary>
/// <param name="Id">对象标识。</param>
/// <param name="Label">版本标签。</param>
/// <param name="ManifestSha256">技能清单的 SHA-256 摘要。</param>
/// <param name="PublishedAtUtc">版本发布的 UTC 时间。</param>
/// <param name="Files">版本包含的文件集合。</param>
/// <param name="BoundAgents">绑定当前技能版本的 Agent 集合。</param>
public sealed record SkillPublishedVersionResponse(
    Guid Id,
    string Label,
    string ManifestSha256,
    DateTimeOffset PublishedAtUtc,
    IReadOnlyList<SkillFileHash> Files,
    IReadOnlyList<SkillBoundAgentResponse> BoundAgents);

/// <summary>
/// 绑定技能的 Agent 响应。
/// </summary>
/// <param name="Id">对象标识。</param>
/// <param name="Code">业务唯一编码。</param>
/// <param name="Name">显示名称。</param>
public sealed record SkillBoundAgentResponse(Guid Id, string Code, string Name);
