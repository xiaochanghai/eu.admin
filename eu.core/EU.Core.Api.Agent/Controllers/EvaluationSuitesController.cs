using EU.Core.Api.Agent.Errors;
using EU.Core.Api.Agent.Security;
using EU.Core.IServices;
using EU.Core.IServices.Abstractions.Security;
using EU.Core.IServices.Evaluation;
using EU.Core.IServices.UnifiedEntry;
using EU.Core.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace EU.Core.Api.Agent.Controllers;

// 文件职责：EvaluationSuitesController 接口处理

/// <summary>
/// 提供评测套件生命周期管理的 HTTP 接口。
/// </summary>
/// <param name="_service">用于管理评测套件及用例的服务。</param>
/// <param name="caller">提供当前调用方身份、租户及权限的上下文。</param>
[Route("api/evaluation-suites")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class EvaluationSuitesController(
    IAgEvaluationSuiteServices _service,
    ICallerContext caller) : Base.ControllerBase
{
    #region 查询列表（List）
    /// <summary>
    /// 查询列表（List）
    /// </summary>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含评测套件定义集合，失败时包含错误状态和提示。</returns>
    [HttpGet]
    public async Task<ServiceResult<IReadOnlyList<EvaluationSuiteDefinition>>> List([FromQuery] string? status, CancellationToken cancellationToken)
    {
        EvaluationSuiteStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (string.Equals(status, nameof(EvaluationSuiteStatus.Active), StringComparison.Ordinal))
                parsedStatus = EvaluationSuiteStatus.Active;
            else if (string.Equals(status, nameof(EvaluationSuiteStatus.Archived), StringComparison.Ordinal))
                parsedStatus = EvaluationSuiteStatus.Archived;
            else
                return FromError<IReadOnlyList<EvaluationSuiteDefinition>>(EvaluationSuiteErrorCodes.LifecycleTransitionInvalid,
                    "Evaluation suite status must be Active or Archived.");
        }

        return Success(
            await _service.ListAsync(caller.TenantId, parsedStatus, cancellationToken));
    }
    #endregion

    #region 获取（Get）
    /// <summary>
    /// 获取（Get）
    /// </summary>
    /// <param name="id">评测套件标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含评测套件定义，失败时包含错误状态和提示。</returns>
    [HttpGet("{id:guid}")]
    public async Task<ServiceResult<EvaluationSuiteDefinition>> Get(Guid id, CancellationToken cancellationToken)
    {
        var value = await _service.GetAsync(
            id, caller.TenantId, cancellationToken);
        return value is null
            ? FromError<EvaluationSuiteDefinition>(EvaluationSuiteErrorCodes.NotFound, "The evaluation suite was not found.")
            : Success(value);
    }
    #endregion

    #region 创建（Create）
    /// <summary>
    /// 创建（Create）
    /// </summary>
    /// <param name="request">创建评测套件所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含评测套件定义，失败时包含错误状态和提示。</returns>
    [HttpPost]
    public async Task<ServiceResult<EvaluationSuiteDefinition>> Create([FromBody] CreateEvaluationSuiteRequest request, CancellationToken cancellationToken)
    {
        if (request.AdditionalProperties is { Count: > 0 })
            return ServiceResult<EvaluationSuiteDefinition>.OprateFailed("The evaluation suite definition is invalid.");
        //return FromError<EvaluationSuiteDefinition>(EvaluationSuiteErrorCodes.DefinitionInvalid, "The evaluation suite definition is invalid.");

        var result =
            await _service.CreateAsync(
                new CreateEvaluationSuiteCommand(
                    caller.TenantId,
                    caller.UserId,
                    request.Code,
                    request.Name,
                    request.Description),
                cancellationToken);
        return result;
    }
    #endregion

    #region 保存（SaveDraft）
    /// <summary>
    /// 保存（SaveDraft）
    /// </summary>
    /// <param name="id">评测套件标识。</param>
    /// <param name="request">保存草稿评测套件所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含评测套件定义，失败时包含错误状态和提示。</returns>
    [HttpPut("{id:guid}/draft")]
    public async Task<ServiceResult<EvaluationSuiteDefinition>> SaveDraft(
        Guid id,
        [FromBody] SaveEvaluationSuiteDraftRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryMapCases(request, out IReadOnlyList<EvaluationCaseDefinition> cases))
        {
            return FromError<EvaluationSuiteDefinition>(EvaluationSuiteErrorCodes.DefinitionInvalid, "The evaluation suite definition is invalid.");
        }

        var result =
            await _service.SaveDraftAsync(
                new SaveEvaluationSuiteDraftCommand(
                    id,
                    caller.TenantId,
                    caller.UserId,
                    request.ExpectedLogicalRevision,
                    request.Name,
                    request.Description,
                    cases),
                cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }
    #endregion

    #region 发布（Publish）
    /// <summary>
    /// 发布（Publish）
    /// </summary>
    /// <param name="id">评测套件标识。</param>
    /// <param name="request">发布评测套件所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含评测套件定义，失败时包含错误状态和提示。</returns>
    [HttpPost("{id:guid}/publish")]
    public async Task<ServiceResult<EvaluationSuiteDefinition>> Publish(
        Guid id,
        [FromBody] PublishEvaluationSuiteRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AdditionalProperties is { Count: > 0 })
        {
            return FromError<EvaluationSuiteDefinition>(EvaluationSuiteErrorCodes.DefinitionInvalid, "The evaluation suite definition is invalid.");
        }

        var result =
            await _service.PublishAsync(
                new PublishEvaluationSuiteCommand(
                    id,
                    caller.TenantId,
                    caller.UserId,
                    request.ExpectedLogicalRevision),
                cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }
    #endregion

    #region 设置（SetArchived）
    /// <summary>
    /// 设置（SetArchived）
    /// </summary>
    /// <param name="id">评测套件标识。</param>
    /// <param name="request">归档或恢复评测套件所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含评测套件定义，失败时包含错误状态和提示。</returns>
    [HttpPut("{id:guid}/archive")]
    public async Task<ServiceResult<EvaluationSuiteDefinition>> SetArchived(
        Guid id,
        [FromBody] SetEvaluationSuiteArchiveRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AdditionalProperties is { Count: > 0 })
            return FromError<EvaluationSuiteDefinition>(EvaluationSuiteErrorCodes.DefinitionInvalid, "The evaluation suite definition is invalid.");

        var result =
            await _service.SetArchivedAsync(
                new SetEvaluationSuiteArchiveCommand(
                    id,
                    caller.TenantId,
                    caller.UserId,
                    request.ExpectedLogicalRevision,
                    request.Archived),
                cancellationToken);
        return result.Success ? result : FromServiceError(result);
    }
    #endregion

    #region 校验并转换评测用例（TryMapCases）
    /// <summary>
    /// 校验草稿请求中的扩展属性、评测规范和期望状态，并转换评测用例（TryMapCases）。
    /// </summary>
    /// <param name="request">包含待保存评测用例的套件草稿请求。</param>
    /// <param name="cases">校验成功时输出转换后的评测用例集合；失败时输出空集合。</param>
    /// <returns>请求及所有用例通过本方法校验时返回 true，否则返回 false。</returns>
    private static bool TryMapCases(SaveEvaluationSuiteDraftRequest request, out IReadOnlyList<EvaluationCaseDefinition> cases)
    {
        cases = [];
        if (request.AdditionalProperties is { Count: > 0 } || request.Cases is null)
        {
            return false;
        }

        var mapped = new List<EvaluationCaseDefinition>(request.Cases.Count);
        foreach (EvaluationCaseApiRequest value in request.Cases)
        {
            if (value.AdditionalProperties is { Count: > 0 }
                || value.Specification is null
                || value.Specification.AdditionalProperties is { Count: > 0 }
                || !TryStatus(value.Specification.ExpectedStatus, out UnifiedRunStatus? status))
            {
                return false;
            }

            mapped.Add(new EvaluationCaseDefinition(
                value.Id,
                value.Name,
                value.Input,
                value.TargetAgentId,
                value.TargetAgentVersionId,
                new RunEvaluationSpecification(
                    status,
                    value.Specification.OutputContains ?? [],
                    value.Specification.OutputExcludes ?? [],
                    value.Specification.RequiredEventKinds ?? [],
                    value.Specification.MaximumToolCalls,
                    value.Specification.MaximumDurationMilliseconds)));
        }

        cases = mapped;
        return true;
    }
    #endregion

    #region 解析可选期望运行状态（TryStatus）
    /// <summary>
    /// 忽略大小写解析已定义的运行状态，空文本表示不限定状态（TryStatus）。
    /// </summary>
    /// <param name="value">可选的期望运行状态文本。</param>
    /// <param name="status">成功解析的状态；文本为空或解析失败时为 null。</param>
    /// <returns>文本为空或可解析为已定义状态时返回 true；非法状态返回 false。</returns>
    private static bool TryStatus(string? value, out UnifiedRunStatus? status)
    {
        status = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!Enum.TryParse(value, true, out UnifiedRunStatus parsed)
            || !Enum.IsDefined(parsed))
        {
            return false;
        }

        status = parsed;
        return true;
    }
    #endregion

    #region 转换（FromServiceError）
    /// <summary>
    /// 转换（FromServiceError）
    /// </summary>
    /// <param name="result">操作结果。</param>
    /// <typeparam name="T">待处理数据的泛型类型。</typeparam>
    /// <returns>包含对应业务错误状态和提示信息的失败服务结果。</returns>
    private ServiceResult<T> FromServiceError<T>(ServiceResult<T> result)
    {
        var descriptor = AgentApiErrorResolver.Resolve(
            HttpContext,
            EvaluationSuiteServiceStatusCodes.ToErrorCode(result.Status));
        Response.StatusCode = descriptor.HttpStatus ?? StatusCodes.Status500InternalServerError;
        return result;
    }
    #endregion

    #region 转换（FromError）
    /// <summary>
    /// 转换（FromError）
    /// </summary>
    /// <param name="errorCode">操作失败对应的业务错误码。</param>
    /// <param name="message">消息或提示文本。</param>
    /// <typeparam name="T">待处理数据的泛型类型。</typeparam>
    /// <returns>包含对应业务错误状态和提示信息的失败服务结果。</returns>
    private ServiceResult<T> FromError<T>(string errorCode, string message)
    {
        var descriptor = AgentApiErrorResolver.Resolve(HttpContext, errorCode);
        Response.StatusCode = descriptor.HttpStatus ?? StatusCodes.Status500InternalServerError;
        return ServiceResult<T>.Failure(descriptor.Status, message);
    }
    #endregion
}

/// <summary>
/// 创建评测套件的请求。
/// </summary>
/// <param name="Code">业务唯一编码。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
public sealed record CreateEvaluationSuiteRequest(
    string Code,
    string Name,
    string Description)
{
    [JsonExtensionData]
    /// <summary>
    /// 未识别的附加字段，用于严格输入校验。
    /// </summary>
    public Dictionary<string, object?>? AdditionalProperties { get; init; }
}

/// <summary>
/// 保存评测套件草稿的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Cases">评测用例集合。</param>
public sealed record SaveEvaluationSuiteDraftRequest(
    long ExpectedLogicalRevision,
    string Name,
    string Description,
    IReadOnlyList<EvaluationCaseApiRequest>? Cases)
{
    [JsonExtensionData]
    /// <summary>
    /// 未识别的附加字段，用于严格输入校验。
    /// </summary>
    public Dictionary<string, object?>? AdditionalProperties { get; init; }
}

/// <summary>
/// 评测用例的接口输入。
/// </summary>
/// <param name="Id">对象标识。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Input">运行或评测使用的输入内容。</param>
/// <param name="TargetAgentId">被评测的目标 Agent 标识。</param>
/// <param name="TargetAgentVersionId">被评测的目标 Agent 版本标识。</param>
/// <param name="Specification">评测规则；为空时使用默认规则。</param>
public sealed record EvaluationCaseApiRequest(
    Guid Id,
    string Name,
    string Input,
    Guid TargetAgentId,
    Guid TargetAgentVersionId,
    EvaluateRunRequest? Specification)
{
    [JsonExtensionData]
    /// <summary>
    /// 未识别的附加字段，用于严格输入校验。
    /// </summary>
    public Dictionary<string, object?>? AdditionalProperties { get; init; }
}

/// <summary>
/// 发布评测套件的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
public sealed record PublishEvaluationSuiteRequest(long ExpectedLogicalRevision)
{
    [JsonExtensionData]
    /// <summary>
    /// 未识别的附加字段，用于严格输入校验。
    /// </summary>
    public Dictionary<string, object?>? AdditionalProperties { get; init; }
}

/// <summary>
/// 设置评测套件归档状态的请求。
/// </summary>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Archived">是否设置为归档状态。</param>
public sealed record SetEvaluationSuiteArchiveRequest(
    long ExpectedLogicalRevision,
    bool Archived)
{
    [JsonExtensionData]
    /// <summary>
    /// 未识别的附加字段，用于严格输入校验。
    /// </summary>
    public Dictionary<string, object?>? AdditionalProperties { get; init; }
}
