using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EU.Core.IServices.Agents;
using EU.Core.IServices.Evaluation;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.Model;

#nullable enable

namespace EU.Core.Services;

public sealed class EvaluationSuiteLifecycleService(
    IEvaluationSuiteRepository repository,
    IEvaluationTargetCatalog targets,
    TimeProvider? timeProvider = null) : BaseServices
{
    private static readonly JsonSerializerOptions HashJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<ServiceResult<EvaluationSuiteDefinition>> CreateAsync(
        CreateEvaluationSuiteCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!ValidIdentity(command.TenantId, command.ActorUserId))
        {
            return Invalid("Trusted tenant and actor are required.");
        }

        string code = (command.Code ?? string.Empty).Trim().ToLowerInvariant();
        if (!Regex.IsMatch(code, "^[a-z0-9]+(?:-[a-z0-9]+)*$") || code.Length > 80)
        {
            return Failure(EvaluationSuiteErrorCodes.CodeInvalid, "Code must be lowercase kebab-case.");
        }

        if (!ValidMetadata(command.Name, command.Description))
        {
            return Invalid("Suite name or description exceeds its limit.");
        }

        DateTimeOffset now = _timeProvider.GetUtcNow().ToUniversalTime();
        var value = new EvaluationSuiteDefinition(
            Guid.NewGuid(),
            command.TenantId,
            code,
            command.Name.Trim(),
            command.Description?.Trim() ?? string.Empty,
            0,
            now,
            now,
            command.ActorUserId,
            command.ActorUserId,
            new EvaluationSuiteDraft([]),
            []);
        return await repository.TryCreateAsync(value, cancellationToken)
            ? Success(value)
            : Failure(EvaluationSuiteErrorCodes.CodeConflict, "An evaluation suite already uses this code.");
    }

    public Task<EvaluationSuiteDefinition?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default) =>
        repository.GetAsync(id, tenantId, cancellationToken);

    public async Task<IReadOnlyList<EvaluationSuiteDefinition>> ListAsync(
        string tenantId,
        EvaluationSuiteStatus? status = null,
        CancellationToken cancellationToken = default) =>
        EvaluationSuiteContractCloner.ReadOnly((await repository.ListAsync(tenantId, cancellationToken))
            .Where(value => status.HasValue
                ? value.Status == status.Value
                : value.Status is not EvaluationSuiteStatus.Archived));

    public async Task<ServiceResult<EvaluationSuiteDefinition>> SaveDraftAsync(
        SaveEvaluationSuiteDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        EvaluationSuiteDefinition? existing = await repository.GetAsync(
            command.Id, command.TenantId, cancellationToken);
        if (existing is null)
        {
            return Failure(EvaluationSuiteErrorCodes.NotFound, "The evaluation suite was not found.");
        }

        if (existing.LogicalRevision != command.ExpectedLogicalRevision)
        {
            return Conflict();
        }

        if (existing.Status is EvaluationSuiteStatus.Archived)
        {
            return Failure(
                EvaluationSuiteErrorCodes.LifecycleTransitionInvalid,
                "An archived evaluation suite must be restored before its Draft can be edited.");
        }

        if (!ValidIdentity(command.TenantId, command.ActorUserId)
            || !ValidMetadata(command.Name, command.Description)
            || !TryValidateCases(command.Cases, allowEmpty: true))
        {
            return Invalid("The evaluation suite Draft is invalid.");
        }

        EvaluationSuiteDefinition updated = existing with
        {
            Name = command.Name.Trim(),
            Description = command.Description?.Trim() ?? string.Empty,
            LogicalRevision = existing.LogicalRevision + 1,
            UpdatedAtUtc = _timeProvider.GetUtcNow().ToUniversalTime(),
            UpdatedBy = command.ActorUserId,
            Draft = new EvaluationSuiteDraft(CloneCases(command.Cases))
        };
        return await repository.TryReplaceAsync(
            updated, command.ExpectedLogicalRevision, cancellationToken)
            ? Success(updated)
            : Conflict();
    }

    public async Task<ServiceResult<EvaluationSuiteDefinition>> PublishAsync(
        PublishEvaluationSuiteCommand command,
        CancellationToken cancellationToken = default)
    {
        EvaluationSuiteDefinition? existing = await repository.GetAsync(
            command.Id, command.TenantId, cancellationToken);
        if (existing is null)
        {
            return Failure(EvaluationSuiteErrorCodes.NotFound, "The evaluation suite was not found.");
        }

        if (existing.LogicalRevision != command.ExpectedLogicalRevision)
        {
            return Conflict();
        }

        if (existing.Status is EvaluationSuiteStatus.Archived)
        {
            return Failure(
                EvaluationSuiteErrorCodes.LifecycleTransitionInvalid,
                "An archived evaluation suite must be restored before it can be published.");
        }

        if (!ValidIdentity(command.TenantId, command.ActorUserId)
            || !TryValidateCases(existing.Draft.Cases, allowEmpty: false))
        {
            return Invalid("A valid non-empty Draft is required before publication.");
        }

        foreach ((Guid agentId, Guid versionId) in existing.Draft.Cases
            .Select(value => (value.TargetAgentId, value.TargetAgentVersionId))
            .Distinct())
        {
            if (!await targets.IsPublishedAsync(agentId, versionId, cancellationToken))
            {
                return Failure(
                    EvaluationSuiteErrorCodes.TargetUnavailable,
                    "Every evaluation target must reference an existing published Agent version.");
            }
        }

        DateTimeOffset now = _timeProvider.GetUtcNow().ToUniversalTime();
        IReadOnlyList<EvaluationCaseDefinition> cases = CloneCases(existing.Draft.Cases);
        var version = new PublishedEvaluationSuiteVersion(
            Guid.NewGuid(),
            $"{existing.PublishedVersions.Count + 1}.0.0",
            ComputeContentHash(cases),
            now,
            command.ActorUserId,
            cases);
        EvaluationSuiteDefinition updated = existing with
        {
            LogicalRevision = existing.LogicalRevision + 1,
            UpdatedAtUtc = now,
            UpdatedBy = command.ActorUserId,
            PublishedVersions = new ReadOnlyCollection<PublishedEvaluationSuiteVersion>(
                existing.PublishedVersions.Append(version).ToArray())
        };
        return await repository.TryReplaceAsync(
            updated, command.ExpectedLogicalRevision, cancellationToken)
            ? Success(updated)
            : Conflict();
    }

    public async Task<ServiceResult<EvaluationSuiteDefinition>> SetArchivedAsync(
        SetEvaluationSuiteArchiveCommand command,
        CancellationToken cancellationToken = default)
    {
        EvaluationSuiteDefinition? existing = await repository.GetAsync(
            command.Id, command.TenantId, cancellationToken);
        if (existing is null)
        {
            return Failure(EvaluationSuiteErrorCodes.NotFound, "The evaluation suite was not found.");
        }

        if (existing.LogicalRevision != command.ExpectedLogicalRevision)
        {
            return Conflict();
        }

        if (!ValidIdentity(command.TenantId, command.ActorUserId))
        {
            return Invalid("Trusted tenant and actor are required.");
        }

        EvaluationSuiteStatus target = command.Archived
            ? EvaluationSuiteStatus.Archived
            : EvaluationSuiteStatus.Active;
        if (existing.Status == target)
        {
            return Failure(
                EvaluationSuiteErrorCodes.LifecycleTransitionInvalid,
                command.Archived
                    ? "The evaluation suite is already archived."
                    : "Only an archived evaluation suite can be restored.");
        }

        EvaluationSuiteDefinition updated = existing with
        {
            Status = target,
            LogicalRevision = existing.LogicalRevision + 1,
            UpdatedAtUtc = _timeProvider.GetUtcNow().ToUniversalTime(),
            UpdatedBy = command.ActorUserId
        };
        return await repository.TryReplaceAsync(
            updated, command.ExpectedLogicalRevision, cancellationToken)
            ? Success(updated)
            : Conflict();
    }

    private static bool TryValidateCases(
        IReadOnlyList<EvaluationCaseDefinition>? cases,
        bool allowEmpty)
    {
        if (cases is null || cases.Count > 100 || (!allowEmpty && cases.Count == 0)
            || cases.Select(value => value.Id).Distinct().Count() != cases.Count)
        {
            return false;
        }

        foreach (EvaluationCaseDefinition value in cases)
        {
            if (value.Id == Guid.Empty
                || value.TargetAgentId == Guid.Empty
                || value.TargetAgentVersionId == Guid.Empty
                || string.IsNullOrWhiteSpace(value.Name)
                || value.Name.Trim().Length > 120
                || string.IsNullOrWhiteSpace(value.Input)
                || Encoding.UTF8.GetByteCount(value.Input) > 32_768
                || value.Specification is null)
            {
                return false;
            }

            try
            {
                RunEvaluationSpecificationValidator.Validate(value.Specification);
            }
            catch (RunEvaluationException)
            {
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<EvaluationCaseDefinition> CloneCases(
        IEnumerable<EvaluationCaseDefinition> cases) =>
        new ReadOnlyCollection<EvaluationCaseDefinition>(cases.Select(value =>
            value with
            {
                Name = value.Name.Trim(),
                Input = value.Input.Trim(),
                Specification = value.Specification with
                {
                    OutputContains = value.Specification.OutputContains.ToArray(),
                    OutputExcludes = value.Specification.OutputExcludes.ToArray(),
                    RequiredEventKinds = value.Specification.RequiredEventKinds.ToArray()
                }
            }).ToArray());

    private static string ComputeContentHash(IReadOnlyList<EvaluationCaseDefinition> cases)
    {
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(cases, HashJsonOptions);
        return Convert.ToHexStringLower(SHA256.HashData(json));
    }

    private static bool ValidIdentity(string tenantId, string actorUserId) =>
        !string.IsNullOrWhiteSpace(tenantId)
        && tenantId.Length <= 128
        && !string.IsNullOrWhiteSpace(actorUserId)
        && actorUserId.Length <= 256;

    private static bool ValidMetadata(string? name, string? description) =>
        !string.IsNullOrWhiteSpace(name)
        && name.Trim().Length <= 120
        && (description?.Trim().Length ?? 0) <= 1000;

    private static ServiceResult<EvaluationSuiteDefinition> Invalid(
        string message) =>
        Failure(EvaluationSuiteErrorCodes.DefinitionInvalid, message);

    private static ServiceResult<EvaluationSuiteDefinition> Conflict() =>
        Failure(EvaluationSuiteErrorCodes.RowVersionConflict, "The evaluation suite changed; reload and retry.");

    private static ServiceResult<EvaluationSuiteDefinition> Failure(
        string code,
        string message) =>
        ServiceResult<EvaluationSuiteDefinition>.Failure(
            EvaluationSuiteServiceStatusCodes.FromErrorCode(code),
            message);
}
