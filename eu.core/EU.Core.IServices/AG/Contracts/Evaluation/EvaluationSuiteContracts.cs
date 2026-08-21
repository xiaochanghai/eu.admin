#nullable enable

using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EU.Core.IServices.Agents;
using EU.Core.Model.ViewModels.Extend;

namespace EU.Core.IServices.Evaluation;

public static class EvaluationSuiteErrorCodes
{
    public const string NotFound = "EVALUATION_SUITE_NOT_FOUND";
    public const string CodeInvalid = "EVALUATION_SUITE_CODE_INVALID";
    public const string CodeConflict = "EVALUATION_SUITE_CODE_CONFLICT";
    public const string DefinitionInvalid = "EVALUATION_SUITE_DEFINITION_INVALID";
    public const string RowVersionConflict = "EVALUATION_SUITE_ROW_VERSION_CONFLICT";
    public const string TargetUnavailable = "EVALUATION_SUITE_TARGET_UNAVAILABLE";
    public const string LifecycleTransitionInvalid = "EVALUATION_SUITE_LIFECYCLE_TRANSITION_INVALID";
}

public enum EvaluationSuiteStatus
{
    Active,
    Archived
}

public sealed record EvaluationCaseDefinition(
    Guid Id,
    string Name,
    string Input,
    Guid TargetAgentId,
    Guid TargetAgentVersionId,
    RunEvaluationSpecification Specification);

public sealed record EvaluationSuiteDraft(
    IReadOnlyList<EvaluationCaseDefinition> Cases);

public sealed record PublishedEvaluationSuiteVersion(
    Guid Id,
    string Label,
    string ContentSha256,
    DateTimeOffset PublishedAtUtc,
    string PublishedBy,
    IReadOnlyList<EvaluationCaseDefinition> Cases);

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
    public EvaluationSuiteStatus Status { get; init; } = EvaluationSuiteStatus.Active;
}

public sealed record CreateEvaluationSuiteCommand(
    string TenantId,
    string ActorUserId,
    string Code,
    string Name,
    string Description);

public sealed record SaveEvaluationSuiteDraftCommand(
    Guid Id,
    string TenantId,
    string ActorUserId,
    long ExpectedLogicalRevision,
    string Name,
    string Description,
    IReadOnlyList<EvaluationCaseDefinition> Cases);

public sealed record PublishEvaluationSuiteCommand(
    Guid Id,
    string TenantId,
    string ActorUserId,
    long ExpectedLogicalRevision);

public sealed record SetEvaluationSuiteArchiveCommand(
    Guid Id,
    string TenantId,
    string ActorUserId,
    long ExpectedLogicalRevision,
    bool Archived);

public static class EvaluationSuiteServiceStatusCodes
{
    public const int NotFound = 670001;
    public const int CodeInvalid = 670002;
    public const int CodeConflict = 670003;
    public const int DefinitionInvalid = 670004;
    public const int RowVersionConflict = 670005;
    public const int TargetUnavailable = 670006;
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

public interface IEvaluationSuiteRepository
{
    Task<EvaluationSuiteDefinition?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvaluationSuiteDefinition>> ListAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<bool> TryCreateAsync(
        EvaluationSuiteDefinition value,
        CancellationToken cancellationToken = default);

    Task<bool> TryReplaceAsync(
        EvaluationSuiteDefinition value,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default);
}

public interface IEvaluationTargetCatalog
{
    Task<bool> IsPublishedAsync(
        Guid agentId,
        Guid agentVersionId,
        CancellationToken cancellationToken = default);
}

public sealed class PublishedAgentEvaluationTargetCatalog(IAgentDefinitionCatalog agents)
    : IEvaluationTargetCatalog
{
    public async Task<bool> IsPublishedAsync(
        Guid agentId,
        Guid agentVersionId,
        CancellationToken cancellationToken = default)
    {
        AgentDefinition? agent = await agents.GetDefinitionAsync(agentId, cancellationToken);
        return agent?.PublishedVersions.Any(version =>
            version.Id == agentVersionId && version.Snapshot is not null) == true;
    }
}

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
