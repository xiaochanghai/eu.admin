#nullable enable

using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EU.Core.Model.ViewModels.Extend;

namespace EU.Core.IServices.Evaluation;

/// <summary>
/// 定义评测套件领域错误码。
/// </summary>
public static class EvaluationSuiteErrorCodes
{
    /// <summary>表示 <c>NotFound</c> 场景的错误码。</summary>
    public const string NotFound = "EVALUATION_SUITE_NOT_FOUND";
    /// <summary>表示 <c>CodeInvalid</c> 场景的错误码。</summary>
    public const string CodeInvalid = "EVALUATION_SUITE_CODE_INVALID";
    /// <summary>表示 <c>CodeConflict</c> 场景的错误码。</summary>
    public const string CodeConflict = "EVALUATION_SUITE_CODE_CONFLICT";
    /// <summary>表示 <c>DefinitionInvalid</c> 场景的错误码。</summary>
    public const string DefinitionInvalid = "EVALUATION_SUITE_DEFINITION_INVALID";
    /// <summary>表示 <c>RowVersionConflict</c> 场景的错误码。</summary>
    public const string RowVersionConflict = "EVALUATION_SUITE_ROW_VERSION_CONFLICT";
    /// <summary>表示 <c>TargetUnavailable</c> 场景的错误码。</summary>
    public const string TargetUnavailable = "EVALUATION_SUITE_TARGET_UNAVAILABLE";
    /// <summary>表示 <c>LifecycleTransitionInvalid</c> 场景的错误码。</summary>
    public const string LifecycleTransitionInvalid = "EVALUATION_SUITE_LIFECYCLE_TRANSITION_INVALID";
}

/// <summary>
/// 评测套件定义的状态。
/// </summary>
public enum EvaluationSuiteStatus
{
    /// <summary>评测套件处于可用状态。</summary>
    Active,
    /// <summary>评测套件已归档。</summary>
    Archived
}

/// <summary>
/// 评测套件中的用例定义。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="Name">显示名称或指标名称。</param>
/// <param name="Input">运行或评测用例的输入内容。</param>
/// <param name="TargetAgentId">被评测的目标 Agent 标识。</param>
/// <param name="TargetAgentVersionId">被评测的目标 Agent 版本标识。</param>
/// <param name="Specification">运行结果评测规则。</param>
public sealed record EvaluationCaseDefinition(
    Guid Id,
    string Name,
    string Input,
    Guid TargetAgentId,
    Guid TargetAgentVersionId,
    RunEvaluationSpecification Specification);

/// <summary>
/// 评测套件草稿。
/// </summary>
/// <param name="Cases">评测用例定义、结果或对比集合。</param>
public sealed record EvaluationSuiteDraft(
    IReadOnlyList<EvaluationCaseDefinition> Cases);

/// <summary>
/// 已发布的评测套件版本。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="Label">版本标签。</param>
/// <param name="ContentSha256">内容的 SHA-256 摘要。</param>
/// <param name="PublishedAtUtc">版本发布的 UTC 时间。</param>
/// <param name="PublishedBy">发布版本的用户标识。</param>
/// <param name="Cases">评测用例定义、结果或对比集合。</param>
public sealed record PublishedEvaluationSuiteVersion(
    Guid Id,
    string Label,
    string ContentSha256,
    DateTimeOffset PublishedAtUtc,
    string PublishedBy,
    IReadOnlyList<EvaluationCaseDefinition> Cases);

/// <summary>
/// 评测套件定义及其版本集合。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="Code">业务唯一编码或检查项编码。</param>
/// <param name="Name">显示名称或指标名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="LogicalRevision">当前逻辑版本。</param>
/// <param name="CreatedAtUtc">记录创建的 UTC 时间。</param>
/// <param name="UpdatedAtUtc">记录最近更新的 UTC 时间。</param>
/// <param name="CreatedBy">创建评测套件的用户标识。</param>
/// <param name="UpdatedBy">最近更新评测套件的用户标识。</param>
/// <param name="Draft">当前评测套件草稿。</param>
/// <param name="PublishedVersions">已发布版本集合。</param>
public sealed record EvaluationSuiteDefinition(
    Guid Id,
    string TenantId,
    string Code,
    string Name,
    string Description,
    long LogicalRevision,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string CreatedBy,
    string UpdatedBy,
    EvaluationSuiteDraft Draft,
    IReadOnlyList<PublishedEvaluationSuiteVersion> PublishedVersions)
{
    /// <summary>
    /// 当前状态。
    /// </summary>
    public EvaluationSuiteStatus Status { get; init; } = EvaluationSuiteStatus.Active;
}

/// <summary>
/// 创建评测套件的命令。
/// </summary>
/// <param name="TenantId">租户标识。</param>
/// <param name="ActorUserId">执行命令的用户标识。</param>
/// <param name="Code">业务唯一编码或检查项编码。</param>
/// <param name="Name">显示名称或指标名称。</param>
/// <param name="Description">说明文本。</param>
public sealed record CreateEvaluationSuiteCommand(
    string TenantId,
    string ActorUserId,
    string Code,
    string Name,
    string Description);

/// <summary>
/// 保存评测套件草稿的命令。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="ActorUserId">执行命令的用户标识。</param>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Name">显示名称或指标名称。</param>
/// <param name="Description">说明文本。</param>
/// <param name="Cases">评测用例定义、结果或对比集合。</param>
public sealed record SaveEvaluationSuiteDraftCommand(
    Guid Id,
    string TenantId,
    string ActorUserId,
    long ExpectedLogicalRevision,
    string Name,
    string Description,
    IReadOnlyList<EvaluationCaseDefinition> Cases);

/// <summary>
/// 发布评测套件版本的命令。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="ActorUserId">执行命令的用户标识。</param>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
public sealed record PublishEvaluationSuiteCommand(
    Guid Id,
    string TenantId,
    string ActorUserId,
    long ExpectedLogicalRevision);

/// <summary>
/// 设置评测套件归档状态的命令。
/// </summary>
/// <param name="Id">对象或记录标识。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="ActorUserId">执行命令的用户标识。</param>
/// <param name="ExpectedLogicalRevision">用于乐观并发控制的预期逻辑版本。</param>
/// <param name="Archived">是否设置为归档状态。</param>
public sealed record SetEvaluationSuiteArchiveCommand(
    Guid Id,
    string TenantId,
    string ActorUserId,
    long ExpectedLogicalRevision,
    bool Archived);

/// <summary>
/// 将评测套件错误映射为服务状态码。
/// </summary>
public static class EvaluationSuiteServiceStatusCodes
{
    /// <summary>表示 <c>NotFound</c> 场景映射的服务状态码。</summary>
    public const int NotFound = 670001;
    /// <summary>表示 <c>CodeInvalid</c> 场景映射的服务状态码。</summary>
    public const int CodeInvalid = 670002;
    /// <summary>表示 <c>CodeConflict</c> 场景映射的服务状态码。</summary>
    public const int CodeConflict = 670003;
    /// <summary>表示 <c>DefinitionInvalid</c> 场景映射的服务状态码。</summary>
    public const int DefinitionInvalid = 670004;
    /// <summary>表示 <c>RowVersionConflict</c> 场景映射的服务状态码。</summary>
    public const int RowVersionConflict = 670005;
    /// <summary>表示 <c>TargetUnavailable</c> 场景映射的服务状态码。</summary>
    public const int TargetUnavailable = 670006;
    /// <summary>表示 <c>LifecycleTransitionInvalid</c> 场景映射的服务状态码。</summary>
    public const int LifecycleTransitionInvalid = 670007;

    public static int FromErrorCode(string code) => code switch
    {
        EvaluationSuiteErrorCodes.NotFound => NotFound,
        EvaluationSuiteErrorCodes.CodeInvalid => CodeInvalid,
        EvaluationSuiteErrorCodes.CodeConflict => CodeConflict,
        EvaluationSuiteErrorCodes.DefinitionInvalid => DefinitionInvalid,
        EvaluationSuiteErrorCodes.RowVersionConflict => RowVersionConflict,
        EvaluationSuiteErrorCodes.TargetUnavailable => TargetUnavailable,
        EvaluationSuiteErrorCodes.LifecycleTransitionInvalid => LifecycleTransitionInvalid,
        _ => 500
    };

    public static string ToErrorCode(int status) => status switch
    {
        NotFound => EvaluationSuiteErrorCodes.NotFound,
        CodeInvalid => EvaluationSuiteErrorCodes.CodeInvalid,
        CodeConflict => EvaluationSuiteErrorCodes.CodeConflict,
        DefinitionInvalid => EvaluationSuiteErrorCodes.DefinitionInvalid,
        RowVersionConflict => EvaluationSuiteErrorCodes.RowVersionConflict,
        TargetUnavailable => EvaluationSuiteErrorCodes.TargetUnavailable,
        LifecycleTransitionInvalid => EvaluationSuiteErrorCodes.LifecycleTransitionInvalid,
        _ => "INTERNAL_ERROR"
    };
}

/// <summary>
/// 提供评测套件契约对象的防御性复制。
/// </summary>
public static class EvaluationSuiteContractCloner
{
    public static EvaluationSuiteDefinition Clone(EvaluationSuiteDefinition value) =>
        value with
        {
            Draft = new EvaluationSuiteDraft(CloneCases(value.Draft.Cases)),
            PublishedVersions = new ReadOnlyCollection<PublishedEvaluationSuiteVersion>(
                value.PublishedVersions.Select(version => version with
                {
                    Cases = CloneCases(version.Cases)
                }).ToArray())
        };

    public static IReadOnlyList<EvaluationSuiteDefinition> ReadOnly(
        IEnumerable<EvaluationSuiteDefinition> values) =>
        new ReadOnlyCollection<EvaluationSuiteDefinition>(
            values.Select(Clone).ToArray());

    private static IReadOnlyList<EvaluationCaseDefinition> CloneCases(
        IEnumerable<EvaluationCaseDefinition> cases) =>
        new ReadOnlyCollection<EvaluationCaseDefinition>(cases.Select(value =>
            value with
            {
                Specification = value.Specification with
                {
                    OutputContains = value.Specification.OutputContains.ToArray(),
                    OutputExcludes = value.Specification.OutputExcludes.ToArray(),
                    RequiredEventKinds = value.Specification.RequiredEventKinds.ToArray()
                }
            }).ToArray());
}
