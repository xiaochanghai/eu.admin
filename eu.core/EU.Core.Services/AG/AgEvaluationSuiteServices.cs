using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EU.Core.IServices.Agents;
using EU.Core.IServices.Evaluation;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.IServices.UnifiedEntry;

#nullable enable

namespace EU.Core.Services;

/// <summary>
/// 评测套件、版本、用例和规则的规范化持久化服务。
/// </summary>
public sealed partial class AgEvaluationSuiteServices :
    BaseServices<AgEvaluationSuite>,
    IAgEvaluationSuiteServices
{
    private const string OutputContainsRule = "OutputContains";
    private const string OutputExcludesRule = "OutputExcludes";
    private const string RequiredEventKindRule = "RequiredEventKind";
    private readonly IEvaluationTargetCatalog? targets;
    private readonly TimeProvider timeProvider;

    public AgEvaluationSuiteServices(
        IBaseRepository<AgEvaluationSuite> dal,
        IEvaluationTargetCatalog? targets = null,
        TimeProvider? timeProvider = null)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
        this.targets = targets;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<EvaluationSuiteDefinition?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            AgEvaluationSuite? suite = await Db.Queryable<AgEvaluationSuite>()
                .Where(value =>
                    value.ID == id &&
                    value.TenantId == tenantId &&
                    !value.IsDeleted)
                .FirstAsync();
            EvaluationSuiteDefinition? result = suite is null
                ? null
                : await LoadSuiteAsync(suite, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await Db.Ado.CommitTranAsync();
            return result;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    private async Task<IReadOnlyList<EvaluationSuiteDefinition>> ListPersistedAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            List<AgEvaluationSuite> suites = await Db.Queryable<AgEvaluationSuite>()
                .Where(value => value.TenantId == tenantId && !value.IsDeleted)
                .OrderBy(value => value.Code)
                .OrderBy(value => value.ID)
                .ToListAsync();
            IReadOnlyList<EvaluationSuiteDefinition> result = await LoadSuitesAsync(
                suites,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await Db.Ado.CommitTranAsync();
            return result;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<bool> TryCreateAsync(
        EvaluationSuiteDefinition value,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            bool exists = await Db.Queryable<AgEvaluationSuite>()
                .Where(candidate =>
                    !candidate.IsDeleted &&
                    (candidate.ID == value.Id ||
                     (candidate.TenantId == value.TenantId && candidate.Code == value.Code)))
                .AnyAsync();
            if (exists)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            await Db.Insertable(MapSuiteEntity(value)).ExecuteCommandAsync();
            Guid draftVersionId = Guid.NewGuid();
            await Db.Insertable(MapDraftVersionEntity(value.Id, draftVersionId))
                .ExecuteCommandAsync();
            await InsertCasesAsync(
                value.Id,
                draftVersionId,
                value.Draft.Cases,
                cancellationToken);
            for (int index = 0; index < value.PublishedVersions.Count; index++)
            {
                await InsertPublishedVersionAsync(
                    value.Id,
                    value.PublishedVersions[index],
                    index + 1,
                    cancellationToken);
            }

            await Db.Ado.CommitTranAsync();
            return true;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<bool> TryReplaceAsync(
        EvaluationSuiteDefinition value,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (expectedLogicalRevision == long.MaxValue ||
            value.LogicalRevision != expectedLogicalRevision + 1)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            List<AgEvaluationSuiteVersion> existingVersions =
                await Db.Queryable<AgEvaluationSuiteVersion>()
                    .Where(candidate => candidate.SuiteId == value.Id && !candidate.IsDeleted)
                    .OrderBy(candidate => candidate.Ordinal)
                    .OrderBy(candidate => candidate.ID)
                    .ToListAsync();
            AgEvaluationSuiteVersion? draft = existingVersions.SingleOrDefault(
                candidate => candidate.IsDraft == true);
            HashSet<Guid> existingPublishedIds = existingVersions
                .Where(candidate => candidate.IsDraft == false)
                .Select(candidate => candidate.ID)
                .ToHashSet();
            HashSet<Guid> requestedPublishedIds = value.PublishedVersions
                .Select(version => version.Id)
                .ToHashSet();
            if (draft is null ||
                !existingPublishedIds.IsSubsetOf(requestedPublishedIds) ||
                value.PublishedVersions.Count != requestedPublishedIds.Count)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            AgEvaluationSuite entity = MapSuiteEntity(value);
            int updated = await Db.Updateable(entity)
                .UpdateColumns(candidate => new
                {
                    candidate.Name,
                    candidate.Description,
                    candidate.Status,
                    candidate.LogicalRevision,
                    candidate.UpdatedAtUtc,
                    candidate.UpdatedByUserId
                })
                .Where(candidate =>
                    candidate.ID == value.Id &&
                    candidate.TenantId == value.TenantId &&
                    candidate.Code == value.Code &&
                    candidate.LogicalRevision == expectedLogicalRevision &&
                    !candidate.IsDeleted)
                .ExecuteCommandAsync();
            if (updated != 1)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            await DeleteVersionCasesAsync(draft.ID);
            await InsertCasesAsync(
                value.Id,
                draft.ID,
                value.Draft.Cases,
                cancellationToken);

            for (int index = 0; index < value.PublishedVersions.Count; index++)
            {
                PublishedEvaluationSuiteVersion version = value.PublishedVersions[index];
                if (!existingPublishedIds.Contains(version.Id))
                {
                    await InsertPublishedVersionAsync(
                        value.Id,
                        version,
                        index + 1,
                        cancellationToken);
                }
            }

            await Db.Ado.CommitTranAsync();
            return true;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    private async Task<EvaluationSuiteDefinition> LoadSuiteAsync(
        AgEvaluationSuite suite,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<EvaluationSuiteDefinition> values = await LoadSuitesAsync(
            [suite],
            cancellationToken);
        return values[0];
    }

    private async Task<IReadOnlyList<EvaluationSuiteDefinition>> LoadSuitesAsync(
        IReadOnlyList<AgEvaluationSuite> suites,
        CancellationToken cancellationToken)
    {
        if (suites.Count == 0)
        {
            return [];
        }

        Guid[] suiteIds = suites.Select(value => value.ID).ToArray();
        List<AgEvaluationSuiteVersion> versions = await Db.Queryable<AgEvaluationSuiteVersion>()
            .Where(value =>
                value.SuiteId.HasValue &&
                suiteIds.Contains(value.SuiteId.Value) &&
                !value.IsDeleted)
            .OrderBy(value => value.SuiteId)
            .OrderBy(value => value.Ordinal)
            .OrderBy(value => value.ID)
            .ToListAsync();
        Guid[] versionIds = versions.Select(value => value.ID).ToArray();
        List<AgEvaluationCase> cases = versionIds.Length == 0
            ? []
            : await Db.Queryable<AgEvaluationCase>()
                .Where(value =>
                    value.VersionId.HasValue &&
                    versionIds.Contains(value.VersionId.Value) &&
                    !value.IsDeleted)
                .OrderBy(value => value.VersionId)
                .OrderBy(value => value.Ordinal)
                .OrderBy(value => value.ID)
                .ToListAsync();
        Guid[] caseRowIds = cases.Select(value => value.ID).ToArray();
        List<AgEvaluationCaseRule> rules = caseRowIds.Length == 0
            ? []
            : await Db.Queryable<AgEvaluationCaseRule>()
                .Where(value =>
                    value.EvaluationCaseId.HasValue &&
                    caseRowIds.Contains(value.EvaluationCaseId.Value) &&
                    !value.IsDeleted)
                .OrderBy(value => value.EvaluationCaseId)
                .OrderBy(value => value.RuleType)
                .OrderBy(value => value.Ordinal)
                .OrderBy(value => value.ID)
                .ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyDictionary<Guid, AgEvaluationCaseRule[]> rulesByCase = rules
            .GroupBy(value => Required(value.EvaluationCaseId, "Rule.EvaluationCaseId"))
            .ToDictionary(group => group.Key, group => group.ToArray());
        IReadOnlyDictionary<Guid, AgEvaluationCase[]> casesByVersion = cases
            .GroupBy(value => Required(value.VersionId, "Case.VersionId"))
            .ToDictionary(group => group.Key, group => group.ToArray());
        IReadOnlyDictionary<Guid, AgEvaluationSuiteVersion[]> versionsBySuite = versions
            .GroupBy(value => Required(value.SuiteId, "Version.SuiteId"))
            .ToDictionary(group => group.Key, group => group.ToArray());

        return EvaluationSuiteContractCloner.ReadOnly(suites.Select(suite => MapSuite(
            suite,
            versionsBySuite.GetValueOrDefault(suite.ID) ?? [],
            casesByVersion,
            rulesByCase)));
    }

    private static EvaluationSuiteDefinition MapSuite(
        AgEvaluationSuite suite,
        IReadOnlyList<AgEvaluationSuiteVersion> versions,
        IReadOnlyDictionary<Guid, AgEvaluationCase[]> casesByVersion,
        IReadOnlyDictionary<Guid, AgEvaluationCaseRule[]> rulesByCase)
    {
        AgEvaluationSuiteVersion draft = versions.SingleOrDefault(value => value.IsDraft == true)
            ?? throw new InvalidDataException(
                $"Evaluation suite '{suite.Code}' does not have exactly one draft version.");
        IReadOnlyList<EvaluationCaseDefinition> MapCases(Guid versionId) =>
            Array.AsReadOnly((casesByVersion.GetValueOrDefault(versionId) ?? [])
                .OrderBy(value => Required(value.Ordinal, "Case.Ordinal"))
                .ThenBy(value => value.ID)
                .Select(value => MapCase(value, rulesByCase.GetValueOrDefault(value.ID) ?? []))
                .ToArray());

        return new EvaluationSuiteDefinition(
            suite.ID,
            Required(suite.TenantId, "TenantId"),
            Required(suite.Code, "Code"),
            Required(suite.Name, "Name"),
            Required(suite.Description, "Description"),
            Required(suite.LogicalRevision, "LogicalRevision"),
            ToOffset(Required(suite.CreatedAtUtc, "CreatedAtUtc")),
            ToOffset(Required(suite.UpdatedAtUtc, "UpdatedAtUtc")),
            Required(suite.CreatedByUserId, "CreatedByUserId"),
            Required(suite.UpdatedByUserId, "UpdatedByUserId"),
            new EvaluationSuiteDraft(MapCases(draft.ID)),
            Array.AsReadOnly(versions
                .Where(value => value.IsDraft == false)
                .OrderBy(value => Required(value.Ordinal, "Version.Ordinal"))
                .ThenBy(value => value.ID)
                .Select(value => new PublishedEvaluationSuiteVersion(
                    value.ID,
                    Required(value.Label, "Version.Label"),
                    Required(value.ContentSha256, "Version.ContentSha256"),
                    ToOffset(Required(value.PublishedAtUtc, "Version.PublishedAtUtc")),
                    Required(value.PublishedByUserId, "Version.PublishedByUserId"),
                    MapCases(value.ID)))
                .ToArray()))
        {
            Status = ParseSuiteStatus(suite.Status)
        };
    }

    private static EvaluationCaseDefinition MapCase(
        AgEvaluationCase value,
        IReadOnlyList<AgEvaluationCaseRule> rules)
    {
        string[] RuleValues(string type) => rules
            .Where(rule => string.Equals(rule.RuleType, type, StringComparison.Ordinal))
            .OrderBy(rule => Required(rule.Ordinal, "Rule.Ordinal"))
            .ThenBy(rule => rule.ID)
            .Select(rule => Required(rule.Value, "Rule.Value"))
            .ToArray();
        return new EvaluationCaseDefinition(
            Required(value.CaseId, "Case.CaseId"),
            Required(value.Name, "Case.Name"),
            Required(value.Input, "Case.Input"),
            Required(value.TargetAgentId, "Case.TargetAgentId"),
            Required(value.TargetAgentVersionId, "Case.TargetAgentVersionId"),
            new RunEvaluationSpecification(
                ParseExpectedStatus(value.ExpectedStatus),
                RuleValues(OutputContainsRule),
                RuleValues(OutputExcludesRule),
                RuleValues(RequiredEventKindRule),
                value.MaximumToolCalls,
                value.MaximumDurationMilliseconds));
    }

    private async Task InsertPublishedVersionAsync(
        Guid suiteId,
        PublishedEvaluationSuiteVersion version,
        int ordinal,
        CancellationToken cancellationToken)
    {
        await Db.Insertable(new AgEvaluationSuiteVersion
        {
            ID = version.Id,
            SuiteId = suiteId,
            Ordinal = ordinal,
            Label = version.Label,
            IsDraft = false,
            ContentSha256 = version.ContentSha256,
            PublishedAtUtc = version.PublishedAtUtc.UtcDateTime,
            PublishedByUserId = version.PublishedBy,
            IsDeleted = false,
            IsActive = true
        }).ExecuteCommandAsync();
        await InsertCasesAsync(suiteId, version.Id, version.Cases, cancellationToken);
    }

    private async Task InsertCasesAsync(
        Guid suiteId,
        Guid versionId,
        IReadOnlyList<EvaluationCaseDefinition> cases,
        CancellationToken cancellationToken)
    {
        for (int ordinal = 0; ordinal < cases.Count; ordinal++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EvaluationCaseDefinition value = cases[ordinal];
            Guid rowId = Guid.NewGuid();
            await Db.Insertable(new AgEvaluationCase
            {
                ID = rowId,
                SuiteId = suiteId,
                VersionId = versionId,
                Ordinal = ordinal,
                CaseId = value.Id,
                Name = value.Name,
                Input = value.Input,
                TargetAgentId = value.TargetAgentId,
                TargetAgentVersionId = value.TargetAgentVersionId,
                ExpectedStatus = value.Specification.ExpectedStatus?.ToString(),
                MaximumToolCalls = value.Specification.MaximumToolCalls,
                MaximumDurationMilliseconds = value.Specification.MaximumDurationMilliseconds,
                IsDeleted = false,
                IsActive = true
            }).ExecuteCommandAsync();
            await InsertRulesAsync(
                suiteId,
                versionId,
                rowId,
                OutputContainsRule,
                value.Specification.OutputContains);
            await InsertRulesAsync(
                suiteId,
                versionId,
                rowId,
                OutputExcludesRule,
                value.Specification.OutputExcludes);
            await InsertRulesAsync(
                suiteId,
                versionId,
                rowId,
                RequiredEventKindRule,
                value.Specification.RequiredEventKinds);
        }
    }

    private async Task InsertRulesAsync(
        Guid suiteId,
        Guid versionId,
        Guid caseRowId,
        string ruleType,
        IReadOnlyList<string> values)
    {
        if (values.Count == 0)
        {
            return;
        }

        List<AgEvaluationCaseRule> rows = values.Select((value, ordinal) =>
            new AgEvaluationCaseRule
            {
                ID = Guid.NewGuid(),
                SuiteId = suiteId,
                VersionId = versionId,
                EvaluationCaseId = caseRowId,
                RuleType = ruleType,
                Ordinal = ordinal,
                Value = value,
                IsDeleted = false,
                IsActive = true
            }).ToList();
        await Db.Insertable(rows).ExecuteCommandAsync();
    }

    private async Task DeleteVersionCasesAsync(Guid versionId)
    {
        Guid[] caseIds = await Db.Queryable<AgEvaluationCase>()
            .Where(value => value.VersionId == versionId)
            .Select(value => value.ID)
            .ToArrayAsync();
        if (caseIds.Length > 0)
        {
            await Db.Deleteable<AgEvaluationCaseRule>()
                .Where(value =>
                    value.EvaluationCaseId.HasValue &&
                    caseIds.Contains(value.EvaluationCaseId.Value))
                .ExecuteCommandAsync();
        }
        await Db.Deleteable<AgEvaluationCase>()
            .Where(value => value.VersionId == versionId)
            .ExecuteCommandAsync();
    }

    private static AgEvaluationSuite MapSuiteEntity(EvaluationSuiteDefinition value) =>
        new()
        {
            ID = value.Id,
            TenantId = value.TenantId,
            Code = value.Code,
            Name = value.Name,
            Description = value.Description,
            Status = value.Status.ToString(),
            LogicalRevision = value.LogicalRevision,
            CreatedAtUtc = value.CreatedAtUtc.UtcDateTime,
            UpdatedAtUtc = value.UpdatedAtUtc.UtcDateTime,
            CreatedByUserId = value.CreatedBy,
            UpdatedByUserId = value.UpdatedBy,
            IsDeleted = false,
            IsActive = true
        };

    private static AgEvaluationSuiteVersion MapDraftVersionEntity(
        Guid suiteId,
        Guid draftVersionId) =>
        new()
        {
            ID = draftVersionId,
            SuiteId = suiteId,
            Ordinal = 0,
            Label = "draft",
            IsDraft = true,
            ContentSha256 = string.Empty,
            PublishedAtUtc = null,
            PublishedByUserId = string.Empty,
            IsDeleted = false,
            IsActive = true
        };

    private static EvaluationSuiteStatus ParseSuiteStatus(string? value) =>
        Enum.TryParse(value, ignoreCase: false, out EvaluationSuiteStatus result) &&
        Enum.IsDefined(result)
            ? result
            : throw new InvalidDataException(
                $"Evaluation suite Status contains unsupported value '{value}'.");

    private static UnifiedRunStatus? ParseExpectedStatus(string? value) =>
        string.IsNullOrEmpty(value)
            ? null
            : Enum.TryParse(value, ignoreCase: false, out UnifiedRunStatus result) &&
              Enum.IsDefined(result)
                ? result
                : throw new InvalidDataException(
                    $"Evaluation case ExpectedStatus contains unsupported value '{value}'.");

    private static DateTimeOffset ToOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static T Required<T>(T? value, string field) where T : struct =>
        value ?? throw new InvalidDataException($"Evaluation suite field '{field}' is missing.");

    private static string Required(string? value, string field) =>
        value ?? throw new InvalidDataException($"Evaluation suite field '{field}' is missing.");

    private static readonly JsonSerializerOptions HashJsonOptions = new(JsonSerializerDefaults.Web);

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

        DateTimeOffset now = timeProvider.GetUtcNow().ToUniversalTime();
        var value = new EvaluationSuiteDefinition(
            Guid.NewGuid(), command.TenantId, code, command.Name.Trim(),
            command.Description?.Trim() ?? string.Empty, 0, now, now,
            command.ActorUserId, command.ActorUserId, new EvaluationSuiteDraft([]), []);
        return await TryCreateAsync(value, cancellationToken)
            ? Success(value)
            : Failure(EvaluationSuiteErrorCodes.CodeConflict, "An evaluation suite already uses this code.");
    }

    public async Task<IReadOnlyList<EvaluationSuiteDefinition>> ListAsync(
        string tenantId,
        EvaluationSuiteStatus? status = null,
        CancellationToken cancellationToken = default) =>
        EvaluationSuiteContractCloner.ReadOnly((await ListPersistedAsync(tenantId, cancellationToken))
            .Where(value => status.HasValue
                ? value.Status == status.Value
                : value.Status is not EvaluationSuiteStatus.Archived));

    public async Task<ServiceResult<EvaluationSuiteDefinition>> SaveDraftAsync(
        SaveEvaluationSuiteDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        EvaluationSuiteDefinition? existing = await GetAsync(command.Id, command.TenantId, cancellationToken);
        if (existing is null) return Failure(EvaluationSuiteErrorCodes.NotFound, "The evaluation suite was not found.");
        if (existing.LogicalRevision != command.ExpectedLogicalRevision) return Conflict();
        if (existing.Status is EvaluationSuiteStatus.Archived)
        {
            return Failure(EvaluationSuiteErrorCodes.LifecycleTransitionInvalid,
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
            UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime(),
            UpdatedBy = command.ActorUserId,
            Draft = new EvaluationSuiteDraft(CloneCases(command.Cases))
        };
        return await TryReplaceAsync(updated, command.ExpectedLogicalRevision, cancellationToken)
            ? Success(updated) : Conflict();
    }

    public async Task<ServiceResult<EvaluationSuiteDefinition>> PublishAsync(
        PublishEvaluationSuiteCommand command,
        CancellationToken cancellationToken = default)
    {
        EvaluationSuiteDefinition? existing = await GetAsync(command.Id, command.TenantId, cancellationToken);
        if (existing is null) return Failure(EvaluationSuiteErrorCodes.NotFound, "The evaluation suite was not found.");
        if (existing.LogicalRevision != command.ExpectedLogicalRevision) return Conflict();
        if (existing.Status is EvaluationSuiteStatus.Archived)
        {
            return Failure(EvaluationSuiteErrorCodes.LifecycleTransitionInvalid,
                "An archived evaluation suite must be restored before it can be published.");
        }
        if (!ValidIdentity(command.TenantId, command.ActorUserId)
            || !TryValidateCases(existing.Draft.Cases, allowEmpty: false))
        {
            return Invalid("A valid non-empty Draft is required before publication.");
        }

        foreach ((Guid agentId, Guid versionId) in existing.Draft.Cases
            .Select(value => (value.TargetAgentId, value.TargetAgentVersionId)).Distinct())
        {
            if (targets is null || !await targets.IsPublishedAsync(agentId, versionId, cancellationToken))
            {
                return Failure(EvaluationSuiteErrorCodes.TargetUnavailable,
                    "Every evaluation target must reference an existing published Agent version.");
            }
        }

        DateTimeOffset now = timeProvider.GetUtcNow().ToUniversalTime();
        IReadOnlyList<EvaluationCaseDefinition> cases = CloneCases(existing.Draft.Cases);
        var version = new PublishedEvaluationSuiteVersion(Guid.NewGuid(),
            $"{existing.PublishedVersions.Count + 1}.0.0", ComputeContentHash(cases),
            now, command.ActorUserId, cases);
        EvaluationSuiteDefinition updated = existing with
        {
            LogicalRevision = existing.LogicalRevision + 1,
            UpdatedAtUtc = now,
            UpdatedBy = command.ActorUserId,
            PublishedVersions = new ReadOnlyCollection<PublishedEvaluationSuiteVersion>(
                existing.PublishedVersions.Append(version).ToArray())
        };
        return await TryReplaceAsync(updated, command.ExpectedLogicalRevision, cancellationToken)
            ? Success(updated) : Conflict();
    }

    public async Task<ServiceResult<EvaluationSuiteDefinition>> SetArchivedAsync(
        SetEvaluationSuiteArchiveCommand command,
        CancellationToken cancellationToken = default)
    {
        EvaluationSuiteDefinition? existing = await GetAsync(command.Id, command.TenantId, cancellationToken);
        if (existing is null) return Failure(EvaluationSuiteErrorCodes.NotFound, "The evaluation suite was not found.");
        if (existing.LogicalRevision != command.ExpectedLogicalRevision) return Conflict();
        if (!ValidIdentity(command.TenantId, command.ActorUserId)) return Invalid("Trusted tenant and actor are required.");

        EvaluationSuiteStatus target = command.Archived ? EvaluationSuiteStatus.Archived : EvaluationSuiteStatus.Active;
        if (existing.Status == target)
        {
            return Failure(EvaluationSuiteErrorCodes.LifecycleTransitionInvalid,
                command.Archived ? "The evaluation suite is already archived."
                    : "Only an archived evaluation suite can be restored.");
        }

        EvaluationSuiteDefinition updated = existing with
        {
            Status = target,
            LogicalRevision = existing.LogicalRevision + 1,
            UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime(),
            UpdatedBy = command.ActorUserId
        };
        return await TryReplaceAsync(updated, command.ExpectedLogicalRevision, cancellationToken)
            ? Success(updated) : Conflict();
    }

    private static bool TryValidateCases(IReadOnlyList<EvaluationCaseDefinition>? cases, bool allowEmpty)
    {
        if (cases is null || cases.Count > 100 || (!allowEmpty && cases.Count == 0)
            || cases.Select(value => value.Id).Distinct().Count() != cases.Count) return false;
        foreach (EvaluationCaseDefinition value in cases)
        {
            if (value.Id == Guid.Empty || value.TargetAgentId == Guid.Empty
                || value.TargetAgentVersionId == Guid.Empty || string.IsNullOrWhiteSpace(value.Name)
                || value.Name.Trim().Length > 120 || string.IsNullOrWhiteSpace(value.Input)
                || Encoding.UTF8.GetByteCount(value.Input) > 32_768 || value.Specification is null) return false;
            try { RunEvaluationSpecificationValidator.Validate(value.Specification); }
            catch (RunEvaluationException) { return false; }
        }
        return true;
    }

    private static IReadOnlyList<EvaluationCaseDefinition> CloneCases(IEnumerable<EvaluationCaseDefinition> cases) =>
        new ReadOnlyCollection<EvaluationCaseDefinition>(cases.Select(value => value with
        {
            Name = value.Name.Trim(), Input = value.Input.Trim(),
            Specification = value.Specification with
            {
                OutputContains = value.Specification.OutputContains.ToArray(),
                OutputExcludes = value.Specification.OutputExcludes.ToArray(),
                RequiredEventKinds = value.Specification.RequiredEventKinds.ToArray()
            }
        }).ToArray());

    private static string ComputeContentHash(IReadOnlyList<EvaluationCaseDefinition> cases) =>
        Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(cases, HashJsonOptions)));

    private static bool ValidIdentity(string tenantId, string actorUserId) =>
        !string.IsNullOrWhiteSpace(tenantId) && tenantId.Length <= 128
        && !string.IsNullOrWhiteSpace(actorUserId) && actorUserId.Length <= 256;

    private static bool ValidMetadata(string? name, string? description) =>
        !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= 120
        && (description?.Trim().Length ?? 0) <= 1000;

    private static ServiceResult<EvaluationSuiteDefinition> Invalid(string message) =>
        Failure(EvaluationSuiteErrorCodes.DefinitionInvalid, message);

    private static ServiceResult<EvaluationSuiteDefinition> Conflict() =>
        Failure(EvaluationSuiteErrorCodes.RowVersionConflict, "The evaluation suite changed; reload and retry.");

    private static ServiceResult<EvaluationSuiteDefinition> Failure(string code, string message) =>
        ServiceResult<EvaluationSuiteDefinition>.Failure(
            EvaluationSuiteServiceStatusCodes.FromErrorCode(code), message);
}

/// <summary>
/// 基于已发布 Agent 定义实现的评测目标目录。
/// </summary>
public sealed class PublishedAgentEvaluationTargetCatalog(IAgentDefinitionCatalog agents)
    : IEvaluationTargetCatalog
{
    /// <summary>
    /// 仅当目标 Agent 存在、目标版本已发布且存在可执行快照时返回 <see langword="true"/>。
    /// </summary>
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
