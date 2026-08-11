using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using EU.Core.Agent.Application.Agents;

namespace EU.Core.Agent.Application.Skills;

public sealed partial class SkillLifecycleService(
    ISkillRepository repository,
    ISkillFileStore fileStore,
    IAgentRepository? agents = null)
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> Locks = new();

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,62}$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();

    [GeneratedRegex("^(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)$", RegexOptions.CultureInvariant)]
    private static partial Regex VersionPattern();

    public async Task<SkillOperationResult<SkillDefinition>> CreateAsync(
        CreateSkillCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        string code = command.Code?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!CodePattern().IsMatch(code) ||
            !string.Equals(code, command.Code?.Trim(), StringComparison.Ordinal))
        {
            return Failure(SkillErrorCodes.CodeInvalid, "Skill code must be lowercase kebab-case.");
        }

        var definition = new SkillDefinition(
            Guid.NewGuid(),
            code,
            command.Name?.Trim() ?? string.Empty,
            command.Description ?? string.Empty,
            command.Category?.Trim() ?? string.Empty,
            0,
            SkillContractCloner.ReadOnly(Array.Empty<SkillVersion>()));
        if (!await repository.TryCreateAsync(definition, cancellationToken))
        {
            return Failure(SkillErrorCodes.CodeConflict, "A Skill already uses this code.");
        }

        await fileStore.EnsureDraftAsync(
            code,
            definition.Name,
            definition.Description,
            cancellationToken);
        return SkillOperationResult<SkillDefinition>.Success(definition);
    }

    public Task<SkillDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(id, cancellationToken);

    public async Task<IReadOnlyList<SkillListItem>> ListAsync(
        SkillQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SkillDefinition> definitions = await repository.ListAsync(query, cancellationToken);
        return SkillContractCloner.ReadOnly(definitions.Select(definition =>
        {
            SkillVersion? current = definition.PublishedVersions.LastOrDefault();
            return new SkillListItem(
                definition.Id,
                definition.Code,
                definition.Name,
                definition.Description,
                definition.Category,
                definition.DraftRevision,
                current?.Label,
                current?.ManifestSha256)
            {
                Status = definition.Status
            };
        }));
    }

    public async Task<SkillOperationResult<SkillDefinition>> UpdateAsync(
        UpdateSkillCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return await WithLockAsync(command.SkillId, async () =>
        {
            SkillDefinition? existing = await repository.GetByIdAsync(command.SkillId, cancellationToken);
            if (existing is null)
            {
                return Failure(SkillErrorCodes.NotFound, "The Skill was not found.");
            }

            if (existing.DraftRevision != command.ExpectedDraftRevision)
            {
                return Failure(SkillErrorCodes.RevisionConflict, "The Skill Draft changed before this operation completed.");
            }

            if (existing.Status is SkillStatus.Archived)
            {
                return Failure(
                    SkillErrorCodes.LifecycleTransitionInvalid,
                    "An archived Skill must be restored before it can be edited.");
            }

            SkillDefinition updated = existing with
            {
                Name = command.Name?.Trim() ?? string.Empty,
                Description = command.Description ?? string.Empty,
                Category = command.Category?.Trim() ?? string.Empty,
                DraftRevision = existing.DraftRevision + 1
            };
            return await repository.TryReplaceAsync(updated, existing.DraftRevision, cancellationToken)
                ? SkillOperationResult<SkillDefinition>.Success(updated)
                : Failure(SkillErrorCodes.RevisionConflict, "The Skill Draft changed before this operation completed.");
        }, cancellationToken);
    }

    public async Task<SkillOperationResult<SkillDefinition>> SaveFileAsync(
        SaveSkillFileCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return await WithLockAsync(command.SkillId, async () =>
        {
            SkillDefinition? existing = await repository.GetByIdAsync(command.SkillId, cancellationToken);
            if (existing is null)
            {
                return Failure(SkillErrorCodes.NotFound, "The Skill was not found.");
            }

            if (existing.DraftRevision != command.ExpectedDraftRevision)
            {
                return Failure(SkillErrorCodes.RevisionConflict, "The Skill Draft changed before this operation completed.");
            }

            if (existing.Status is SkillStatus.Archived)
            {
                return Failure(
                    SkillErrorCodes.LifecycleTransitionInvalid,
                    "An archived Skill must be restored before its Draft files can be edited.");
            }

            try
            {
                await fileStore.WriteDraftTextAsync(
                    existing.Code,
                    command.RelativePath,
                    command.Content,
                    cancellationToken);
            }
            catch (SkillFileStoreException exception)
            {
                return Failure(exception.Code, exception.Message);
            }

            SkillDefinition updated = existing with { DraftRevision = existing.DraftRevision + 1 };
            return await repository.TryReplaceAsync(updated, existing.DraftRevision, cancellationToken)
                ? SkillOperationResult<SkillDefinition>.Success(updated)
                : Failure(SkillErrorCodes.RevisionConflict, "The Skill Draft changed before this operation completed.");
        }, cancellationToken);
    }

    public async Task<SkillOperationResult<SkillDefinition>> PublishAsync(
        PublishSkillCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        string versionLabel = command.VersionLabel ?? string.Empty;
        if (!VersionPattern().IsMatch(versionLabel))
        {
            return Failure(SkillErrorCodes.VersionInvalid, "Skill version must be strict SemVer major.minor.patch.");
        }

        return await WithLockAsync(command.SkillId, async () =>
        {
            SkillDefinition? existing = await repository.GetByIdAsync(command.SkillId, cancellationToken);
            if (existing is null)
            {
                return Failure(SkillErrorCodes.NotFound, "The Skill was not found.");
            }

            if (existing.DraftRevision != command.ExpectedDraftRevision)
            {
                return Failure(SkillErrorCodes.RevisionConflict, "The Skill Draft changed before this operation completed.");
            }

            if (existing.Status is SkillStatus.Archived)
            {
                return Failure(
                    SkillErrorCodes.LifecycleTransitionInvalid,
                    "An archived Skill must be restored before it can be published.");
            }

            if (existing.PublishedVersions.Any(version =>
                string.Equals(version.Label, command.VersionLabel, StringComparison.Ordinal)))
            {
                return Failure(SkillErrorCodes.VersionConflict, "The Skill version already exists.");
            }

            SkillPublishArtifact artifact;
            try
            {
                artifact = await fileStore.PublishAsync(existing.Code, versionLabel, cancellationToken);
            }
            catch (SkillFileStoreException exception)
            {
                return Failure(exception.Code, exception.Message);
            }

            var version = new SkillVersion(
                Guid.NewGuid(),
                artifact.VersionLabel,
                artifact.ManifestSha256,
                DateTimeOffset.UtcNow,
                artifact.Files);
            SkillDefinition updated = existing with
            {
                DraftRevision = existing.DraftRevision + 1,
                PublishedVersions = SkillContractCloner.ReadOnly(existing.PublishedVersions.Append(version))
            };
            if (await repository.TryReplaceAsync(updated, existing.DraftRevision, cancellationToken))
            {
                return SkillOperationResult<SkillDefinition>.Success(updated);
            }

            await fileStore.DeletePublishedAsync(existing.Code, versionLabel, cancellationToken);
            return Failure(SkillErrorCodes.RevisionConflict, "The Skill Draft changed before publish completed.");
        }, cancellationToken);
    }

    public async Task<SkillOperationResult<IReadOnlyList<SkillFileEntry>>> ListFilesAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        SkillDefinition? definition = await repository.GetByIdAsync(id, cancellationToken);
        if (definition is null)
        {
            return SkillOperationResult<IReadOnlyList<SkillFileEntry>>.Failure(
                SkillErrorCodes.NotFound,
                "The Skill was not found.");
        }

        try
        {
            return SkillOperationResult<IReadOnlyList<SkillFileEntry>>.Success(
                await fileStore.ListDraftAsync(definition.Code, cancellationToken));
        }
        catch (SkillFileStoreException exception)
        {
            return SkillOperationResult<IReadOnlyList<SkillFileEntry>>.Failure(exception.Code, exception.Message);
        }
    }

    public async Task<SkillOperationResult<SkillDefinition>> SetArchivedAsync(
        SetSkillArchiveCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return await WithLockAsync(command.SkillId, async () =>
        {
            SkillDefinition? existing = await repository.GetByIdAsync(command.SkillId, cancellationToken);
            if (existing is null)
            {
                return Failure(SkillErrorCodes.NotFound, "The Skill was not found.");
            }

            if (existing.DraftRevision != command.ExpectedDraftRevision)
            {
                return Failure(SkillErrorCodes.RevisionConflict, "The Skill Draft changed before this operation completed.");
            }

            SkillStatus target = command.Archived ? SkillStatus.Archived : SkillStatus.Active;
            if (existing.Status == target)
            {
                return Failure(
                    SkillErrorCodes.LifecycleTransitionInvalid,
                    command.Archived ? "The Skill is already archived." : "Only an archived Skill can be restored.");
            }

            if (command.Archived && agents is not null)
            {
                var versionIds = existing.PublishedVersions.Select(value => value.Id).ToHashSet();
                IReadOnlyList<AgentDefinition> enabledAgents = await agents.ListAsync(
                    new AgentDefinitionQuery(RuntimeStatus: AgentRuntimeStatus.Enabled),
                    cancellationToken);
                string[] blockers = enabledAgents
                    .Where(value => value.PublishedVersions.LastOrDefault()?.Snapshot?.Skills
                        .Any(binding => versionIds.Contains(binding.SkillVersionId)) == true)
                    .Select(value => value.Code)
                    .Take(8)
                    .ToArray();
                if (blockers.Length > 0)
                {
                    return Failure(
                        SkillErrorCodes.ArchiveBlocked,
                        $"The Skill is still referenced by Agent(s): {string.Join(", ", blockers)}.");
                }
            }

            SkillDefinition updated = existing with
            {
                Status = target,
                DraftRevision = existing.DraftRevision + 1
            };
            return await repository.TryReplaceAsync(updated, existing.DraftRevision, cancellationToken)
                ? SkillOperationResult<SkillDefinition>.Success(updated)
                : Failure(SkillErrorCodes.RevisionConflict, "The Skill Draft changed before this operation completed.");
        }, cancellationToken);
    }

    public async Task<SkillOperationResult<string>> ReadFileAsync(
        Guid id,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        SkillDefinition? definition = await repository.GetByIdAsync(id, cancellationToken);
        if (definition is null)
        {
            return SkillOperationResult<string>.Failure(SkillErrorCodes.NotFound, "The Skill was not found.");
        }

        try
        {
            return SkillOperationResult<string>.Success(
                await fileStore.ReadDraftTextAsync(definition.Code, relativePath, cancellationToken));
        }
        catch (SkillFileStoreException exception)
        {
            return SkillOperationResult<string>.Failure(exception.Code, exception.Message);
        }
    }

    private static SkillOperationResult<SkillDefinition> Failure(string code, string message) =>
        SkillOperationResult<SkillDefinition>.Failure(code, message);

    private static async Task<T> WithLockAsync<T>(
        Guid id,
        Func<Task<T>> action,
        CancellationToken cancellationToken)
    {
        SemaphoreSlim gate = Locks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await action();
        }
        finally
        {
            gate.Release();
        }
    }
}
