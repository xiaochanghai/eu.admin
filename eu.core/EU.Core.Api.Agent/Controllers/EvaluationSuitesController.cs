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

#region 文件职责：EvaluationSuitesController 接口处理

/// <summary>
/// 提供评测套件生命周期管理的 HTTP 接口。
/// </summary>
[Route("api/evaluation-suites")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class EvaluationSuitesController(
    IAgEvaluationSuiteServices _service,
    ICallerContext caller) : Base.ControllerBase
{
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

    [HttpGet("{id:guid}")]
    public async Task<ServiceResult<EvaluationSuiteDefinition>> Get(Guid id, CancellationToken cancellationToken)
    {
        var value = await _service.GetAsync(
            id, caller.TenantId, cancellationToken);
        return value is null
            ? FromError<EvaluationSuiteDefinition>(EvaluationSuiteErrorCodes.NotFound, "The evaluation suite was not found.")
            : Success(value);
    }

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

    private ServiceResult<T> FromServiceError<T>(ServiceResult<T> result)
    {
        var descriptor = AgentApiErrorResolver.Resolve(
            HttpContext,
            EvaluationSuiteServiceStatusCodes.ToErrorCode(result.Status));
        Response.StatusCode = descriptor.HttpStatus ?? StatusCodes.Status500InternalServerError;
        return result;
    }

    private ServiceResult<T> FromError<T>(string errorCode, string message)
    {
        var descriptor = AgentApiErrorResolver.Resolve(HttpContext, errorCode);
        Response.StatusCode = descriptor.HttpStatus ?? StatusCodes.Status500InternalServerError;
        return ServiceResult<T>.Failure(descriptor.Status, message);
    }
}

/// <summary>
/// 创建评测套件的请求。
/// </summary>
/// <param name="Code">业务唯一编码。</param>
/// <param name="Name">显示名称。</param>
/// <param name="Description">说明文本。</param>
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

#endregion
