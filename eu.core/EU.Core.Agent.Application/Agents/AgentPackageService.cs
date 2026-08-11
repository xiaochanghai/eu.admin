using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EU.Core.Agent.Application.Validation;
using EU.Core.Agent.Application.Skills;
using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Application.Knowledge;
using EU.Core.Agent.Application.Orchestration;

namespace EU.Core.Agent.Application.Agents;

public interface IModelProfileReferenceCatalog
{
    Task<bool> ExistsAsync(string modelProfileId, CancellationToken cancellationToken = default);
}

public interface IPublicModelProfileCatalog : IModelProfileReferenceCatalog
{
    IReadOnlyList<string> ProfileIds { get; }
}

public sealed class PublicModelProfileCatalog : IPublicModelProfileCatalog
{
    private static readonly string[] SensitiveTerms =
    [
        "apikey",
        "authorization",
        "connection",
        "credential",
        "password",
        "secret",
        "token"
    ];

    private readonly HashSet<string> _profileIds;

    public PublicModelProfileCatalog(IEnumerable<string> profileIds)
    {
        ArgumentNullException.ThrowIfNull(profileIds);
        string[] configuredValues = profileIds.ToArray();
        if (!AreValid(configuredValues))
        {
            throw new ArgumentException(
                "The public model profile identifier configuration is invalid.",
                nameof(profileIds));
        }

        string[] values = configuredValues
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        ProfileIds = AgentContractCloner.ReadOnly(values);
        _profileIds = new HashSet<string>(values, StringComparer.Ordinal);
    }

    public static bool AreValid(IEnumerable<string>? profileIds)
    {
        if (profileIds is null)
        {
            return false;
        }

        foreach (string? value in profileIds)
        {
            if (!IsSafePublicIdentifier(value))
            {
                return false;
            }
        }

        return true;
    }

    public IReadOnlyList<string> ProfileIds { get; }

    public Task<bool> ExistsAsync(string modelProfileId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_profileIds.Contains(modelProfileId));
    }

    private static bool IsSafePublicIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string identifier = value.Trim();
        if (identifier.Length is > 200 ||
            identifier.StartsWith("alias:", StringComparison.OrdinalIgnoreCase) ||
            identifier.StartsWith("sk-", StringComparison.OrdinalIgnoreCase) ||
            identifier.StartsWith("bearer", StringComparison.OrdinalIgnoreCase) ||
            identifier.StartsWith("eyJ", StringComparison.OrdinalIgnoreCase) ||
            identifier.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        string normalized = new(identifier
            .Where(char.IsAsciiLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
        if (SensitiveTerms.Any(term => normalized.Contains(term, StringComparison.Ordinal)))
        {
            return false;
        }

        string[] segments = identifier.Split('/');
        if (segments.Any(segment => !IsSafeSegment(segment)))
        {
            return false;
        }

        return true;
    }

    private static bool IsSafeSegment(string segment)
    {
        if (segment.Length is 0 or > 128 ||
            !char.IsAsciiLetterOrDigit(segment[0]) ||
            !char.IsAsciiLetterOrDigit(segment[^1]))
        {
            return false;
        }

        return segment.All(character =>
            char.IsAsciiLetterOrDigit(character) ||
            character is '.' or '_' or '-');
    }
}

public sealed record AgentPackageV1(
    [property: JsonPropertyOrder(0)] string Format,
    [property: JsonPropertyOrder(1)] string Version,
    [property: JsonPropertyOrder(2)] AgentPackageAgentV1 Agent);

public sealed record AgentPackageAgentV1(
    [property: JsonPropertyOrder(0)] string Code,
    [property: JsonPropertyOrder(1)] string Name,
    [property: JsonPropertyOrder(2)] string Description,
    [property: JsonPropertyOrder(3)] string RuntimeStatus,
    [property: JsonPropertyOrder(4)] AgentPackageDraftV1 Draft,
    [property: JsonPropertyOrder(5)] AgentPackageDeploymentV1 Deployment,
    [property: JsonPropertyOrder(6)] IReadOnlyList<string> Skills,
    [property: JsonPropertyOrder(7)] IReadOnlyList<string> Tools,
    [property: JsonPropertyOrder(8)]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<string>? KnowledgeBases = null)
{
    [JsonPropertyOrder(9)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<AgentPackageChildBindingV1>? ChildAgents { get; init; }

    [JsonPropertyOrder(10)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<AgentPackageOrchestrationBindingV1>? Orchestrations { get; init; }
}

public sealed record AgentPackageChildBindingV1(string AgentId, string AgentVersionId);

public sealed record AgentPackageOrchestrationBindingV1(string OrchestrationId, string OrchestrationVersionId);

public sealed record AgentPackageDraftV1(
    [property: JsonPropertyOrder(0)] string Instructions,
    [property: JsonPropertyOrder(1)] string ModelProfileId,
    [property: JsonPropertyOrder(2)] string OutputMode,
    [property: JsonPropertyOrder(3)] string? OutputJsonSchema);

public sealed record AgentPackageDeploymentV1(
    [property: JsonPropertyOrder(0)] string Target,
    [property: JsonPropertyOrder(1)] string Host);

public sealed class AgentPackageService
{
    public const string FormatIdentifier = "eu.core.agent-package";
    public const string CurrentVersion = "1.0.0";

    private const int MaximumPackageUtf8Bytes = 131_072;
    private const int MaximumPackageDepth = 24;
    private const int MaximumPackageNodes = 2_048;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = false
    };

    private static readonly HashSet<string> ForbiddenPropertyNames = new(StringComparer.Ordinal)
    {
        "apikey",
        "connectionstring",
        "credential",
        "credentialalias",
        "endpoint",
        "password",
        "secret",
        "token",
        "accesstoken"
    };

    private readonly IAgentRepository _repository;
    private readonly AgentLifecycleService _lifecycle;
    private readonly IModelProfileReferenceCatalog _modelProfiles;
    private readonly JsonSchemaValidator _schemaValidator;
    private readonly IPublishedSkillVersionCatalog? _skillVersions;
    private readonly IPublishedMcpToolCatalog? _toolVersions;
    private readonly IPublishedKnowledgeCatalog? _knowledgeBases;
    private readonly IPublishedOrchestrationCatalog? _orchestrationCatalog;

    public AgentPackageService(
        IAgentRepository repository,
        AgentLifecycleService lifecycle,
        IModelProfileReferenceCatalog modelProfiles,
        JsonSchemaValidator? schemaValidator = null,
        IPublishedSkillVersionCatalog? skillVersions = null,
        IPublishedMcpToolCatalog? toolVersions = null,
        IPublishedKnowledgeCatalog? knowledgeBases = null,
        IPublishedOrchestrationCatalog? orchestrationCatalog = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        _modelProfiles = modelProfiles ?? throw new ArgumentNullException(nameof(modelProfiles));
        _schemaValidator = schemaValidator ?? new JsonSchemaValidator();
        _skillVersions = skillVersions;
        _toolVersions = toolVersions;
        _knowledgeBases = knowledgeBases;
        _orchestrationCatalog = orchestrationCatalog;
    }

    public async Task<AgentOperationResult<string>> ExportAsync(
        Guid agentId,
        CancellationToken cancellationToken = default)
    {
        AgentDefinition? definition = await _repository.GetByIdAsync(agentId, cancellationToken);
        if (definition is null)
        {
            return AgentOperationResult<string>.Failure(
                AgentErrorCodes.NotFound,
                "The Agent was not found.");
        }

        AgentError? bindingError = await ValidateDraftChildReferencesAsync(
            definition.Draft.ChildAgentIds, definition.Draft.ChildAgentPins, cancellationToken);
        if (bindingError is not null)
        {
            return new AgentOperationResult<string>(null, bindingError);
        }
        bindingError = await ValidateDraftOrchestrationReferencesAsync(
            definition.Draft.OrchestrationIds, definition.Draft.OrchestrationPins, cancellationToken);
        if (bindingError is not null)
        {
            return new AgentOperationResult<string>(null, bindingError);
        }

        var package = new AgentPackageV1(
            FormatIdentifier,
            CurrentVersion,
            new AgentPackageAgentV1(
                definition.Code,
                definition.Name,
                definition.Description,
                definition.RuntimeStatus.ToString(),
                new AgentPackageDraftV1(
                    definition.Draft.Instructions,
                    definition.Draft.ModelProfileId,
                    definition.Draft.OutputMode.ToString(),
                    definition.Draft.OutputJsonSchema),
                new AgentPackageDeploymentV1(
                    AgentDefinition.ServerDeploymentTarget,
                    AgentDefinition.ApiHost),
                AgentContractCloner.ReadOnly(definition.Draft.SkillVersionIds.Select(
                    id => id.ToString("D"))),
                AgentContractCloner.ReadOnly(definition.Draft.ToolVersionIds.Select(
                    id => id.ToString("D"))),
                definition.Draft.KnowledgeBaseIds.Count == 0
                    ? null
                    : AgentContractCloner.ReadOnly(definition.Draft.KnowledgeBaseIds.Select(
                        id => id.ToString("D"))))
            {
                ChildAgents = await ExportChildBindingsAsync(definition.Draft.ChildAgentIds, definition.Draft.ChildAgentPins, cancellationToken),
                Orchestrations = await ExportOrchestrationBindingsAsync(definition.Draft.OrchestrationIds, definition.Draft.OrchestrationPins, cancellationToken)
            });

        string json = JsonSerializer.Serialize(package, SerializerOptions);
        if (!TryReadPackage(json, out AgentPackageV1? verifiedPackage, out AgentError? safetyError))
        {
            return new AgentOperationResult<string>(null, safetyError);
        }

        if (!TryValidate(verifiedPackage!, out _, out _, out AgentError? contractError))
        {
            return new AgentOperationResult<string>(null, contractError);
        }

        AgentError? referenceError = await ValidateModelReferenceAsync(
            verifiedPackage!.Agent.Draft.ModelProfileId,
            cancellationToken);
        if (referenceError is not null)
        {
            return new AgentOperationResult<string>(null, referenceError);
        }

        referenceError = await ValidateToolReferencesAsync(
            verifiedPackage.Agent.Tools,
            cancellationToken);
        if (referenceError is not null)
        {
            return new AgentOperationResult<string>(null, referenceError);
        }

        referenceError = await ValidateSkillReferencesAsync(
            verifiedPackage.Agent.Skills,
            cancellationToken);
        if (referenceError is not null)
        {
            return new AgentOperationResult<string>(null, referenceError);
        }

        referenceError = await ValidateKnowledgeReferencesAsync(
            verifiedPackage.Agent.KnowledgeBases ?? Array.Empty<string>(),
            cancellationToken);
        if (referenceError is not null)
        {
            return new AgentOperationResult<string>(null, referenceError);
        }

        referenceError = await ValidateChildBindingReferencesAsync(
            verifiedPackage.Agent.ChildAgents ?? Array.Empty<AgentPackageChildBindingV1>(), cancellationToken);
        if (referenceError is not null) return new AgentOperationResult<string>(null, referenceError);
        referenceError = await ValidateOrchestrationBindingReferencesAsync(
            verifiedPackage.Agent.Orchestrations ?? Array.Empty<AgentPackageOrchestrationBindingV1>(), cancellationToken);
        if (referenceError is not null) return new AgentOperationResult<string>(null, referenceError);

        return AgentOperationResult<string>.Success(json);
    }

    public async Task<AgentOperationResult<AgentDefinition>> ImportAsync(
        string json,
        CancellationToken cancellationToken = default)
    {
        if (!TryReadPackage(json, out AgentPackageV1? package, out AgentError? error))
        {
            return new AgentOperationResult<AgentDefinition>(null, error);
        }

        if (!TryValidate(package!, out AgentRuntimeStatus runtimeStatus, out AgentOutputMode outputMode, out error))
        {
            return new AgentOperationResult<AgentDefinition>(null, error);
        }

        string modelProfileId = package!.Agent.Draft.ModelProfileId;
        AgentError? referenceError = await ValidateModelReferenceAsync(modelProfileId, cancellationToken);
        if (referenceError is not null)
        {
            return new AgentOperationResult<AgentDefinition>(null, referenceError);
        }

        referenceError = await ValidateToolReferencesAsync(
            package.Agent.Tools,
            cancellationToken);
        if (referenceError is not null)
        {
            return new AgentOperationResult<AgentDefinition>(null, referenceError);
        }

        referenceError = await ValidateSkillReferencesAsync(
            package.Agent.Skills,
            cancellationToken);
        if (referenceError is not null)
        {
            return new AgentOperationResult<AgentDefinition>(null, referenceError);
        }

        referenceError = await ValidateKnowledgeReferencesAsync(
            package.Agent.KnowledgeBases ?? Array.Empty<string>(),
            cancellationToken);
        if (referenceError is not null)
        {
            return new AgentOperationResult<AgentDefinition>(null, referenceError);
        }

        referenceError = await ValidateChildBindingReferencesAsync(
            package.Agent.ChildAgents ?? Array.Empty<AgentPackageChildBindingV1>(), cancellationToken);
        if (referenceError is not null) return new AgentOperationResult<AgentDefinition>(null, referenceError);
        referenceError = await ValidateOrchestrationBindingReferencesAsync(
            package.Agent.Orchestrations ?? Array.Empty<AgentPackageOrchestrationBindingV1>(), cancellationToken);
        if (referenceError is not null) return new AgentOperationResult<AgentDefinition>(null, referenceError);

        Guid[] skillVersionIds = package.Agent.Skills
            .Select(Guid.Parse)
            .ToArray();
        Guid[] toolVersionIds = package.Agent.Tools
            .Select(Guid.Parse)
            .ToArray();
        Guid[] knowledgeBaseIds = (package.Agent.KnowledgeBases ?? Array.Empty<string>())
            .Select(Guid.Parse)
            .ToArray();

        AgentOperationResult<AgentDefinition> result = await _lifecycle.CreateImportedAsync(
            new ImportAgentCommand(
                package.Agent.Code,
                package.Agent.Name,
                package.Agent.Description,
                runtimeStatus,
                package.Agent.Draft.Instructions,
                modelProfileId,
                outputMode,
                package.Agent.Draft.OutputJsonSchema,
                AgentContractCloner.ReadOnly(skillVersionIds),
                AgentContractCloner.ReadOnly(toolVersionIds),
                AgentContractCloner.ReadOnly(knowledgeBaseIds))
            {
                ChildAgentIds = AgentContractCloner.ReadOnly((package.Agent.ChildAgents ?? []).Select(value => Guid.Parse(value.AgentId))),
                OrchestrationIds = AgentContractCloner.ReadOnly((package.Agent.Orchestrations ?? []).Select(value => Guid.Parse(value.OrchestrationId))),
                ChildAgentPins = AgentContractCloner.ReadOnly((package.Agent.ChildAgents ?? []).Select(value =>
                    new AgentChildBindingSnapshot(Guid.Parse(value.AgentId), Guid.Parse(value.AgentVersionId)))),
                OrchestrationPins = AgentContractCloner.ReadOnly((package.Agent.Orchestrations ?? []).Select(value =>
                    new AgentOrchestrationBindingSnapshot(Guid.Parse(value.OrchestrationId), Guid.Parse(value.OrchestrationVersionId))))
            },
            cancellationToken);

        if (result.Error?.Code is AgentErrorCodes.CodeInvalid or AgentErrorCodes.RuntimeStatusInvalid)
        {
            return AgentOperationResult<AgentDefinition>.Failure(
                AgentErrorCodes.PackageInvalid,
                result.Error.Message);
        }

        return result;
    }

    private async Task<AgentError?> ValidateModelReferenceAsync(
        string modelProfileId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(modelProfileId) &&
            !await _modelProfiles.ExistsAsync(modelProfileId, cancellationToken))
        {
            return new AgentError(
                AgentErrorCodes.ReferenceMissing,
                "The package references a model profile that is not available.");
        }

        return null;
    }

    private async Task<AgentError?> ValidateToolReferencesAsync(
        IReadOnlyList<string> references,
        CancellationToken cancellationToken)
    {
        foreach (string reference in references)
        {
            if (!Guid.TryParseExact(reference, "D", out Guid versionId) ||
                _toolVersions is null ||
                !await _toolVersions.ExistsAsync(versionId, cancellationToken))
            {
                return new AgentError(
                    AgentErrorCodes.ReferenceMissing,
                    "The package references an MCP tool version that is not available.");
            }
        }

        return null;
    }

    private async Task<AgentError?> ValidateSkillReferencesAsync(
        IReadOnlyList<string> references,
        CancellationToken cancellationToken)
    {
        foreach (string reference in references)
        {
            if (!Guid.TryParseExact(reference, "D", out Guid versionId) ||
                _skillVersions is null ||
                !await _skillVersions.ExistsAsync(versionId, cancellationToken))
            {
                return new AgentError(
                    AgentErrorCodes.ReferenceMissing,
                    "The package references a Skill version that is not published.");
            }
        }

        return null;
    }

    private async Task<AgentError?> ValidateKnowledgeReferencesAsync(
        IReadOnlyList<string> references,
        CancellationToken cancellationToken)
    {
        IReadOnlySet<Guid> available = _knowledgeBases is null
            ? new HashSet<Guid>()
            : (await _knowledgeBases.ListAsync(cancellationToken))
                .Select(value => value.KnowledgeBaseId).ToHashSet();
        foreach (string reference in references)
        {
            if (!Guid.TryParseExact(reference, "D", out Guid id) || !available.Contains(id))
            {
                return new AgentError(
                    AgentErrorCodes.ReferenceMissing,
                    "The package references a knowledge base that is not enabled and indexed.");
            }
        }
        return null;
    }

    private async Task<AgentError?> ValidateDraftChildReferencesAsync(
        IReadOnlyList<Guid> ids,
        IReadOnlyList<AgentChildBindingSnapshot> pins,
        CancellationToken cancellationToken)
    {
        if (pins.Count > 0 && pins.Select(value => value.AgentId).Distinct().Count() != pins.Count)
            return new AgentError(AgentErrorCodes.ReferenceMissing, "The package child Agent pins contain duplicate identities.");
        IReadOnlyDictionary<Guid, AgentChildBindingSnapshot>? byId = pins.Count == 0 ? null :
            pins.ToDictionary(value => value.AgentId);
        if (byId is not null && (byId.Count != ids.Count || byId.Keys.Except(ids).Any()))
            return new AgentError(AgentErrorCodes.ReferenceMissing, "The package child Agent pins do not match its identities.");
        foreach (Guid id in ids)
        {
            AgentDefinition? agent = await _repository.GetByIdAsync(id, cancellationToken);
            Guid versionId = byId?.TryGetValue(id, out AgentChildBindingSnapshot? pin) is true
                ? pin.AgentVersionId : agent?.PublishedVersions[^1].Id ?? Guid.Empty;
            if (agent is null || agent.RuntimeStatus is not AgentRuntimeStatus.Enabled ||
                !agent.PublishedVersions.Any(value => value.Id == versionId))
                return new AgentError(AgentErrorCodes.ReferenceMissing, "The package references an enabled published child Agent that is not available.");
        }
        return null;
    }

    private async Task<AgentError?> ValidateDraftOrchestrationReferencesAsync(
        IReadOnlyList<Guid> ids,
        IReadOnlyList<AgentOrchestrationBindingSnapshot> pins,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PublishedOrchestrationReference> values = _orchestrationCatalog is null ? [] :
            await _orchestrationCatalog.ListPublishedAsync(cancellationToken);
        if (pins.Count > 0 && pins.Select(value => value.OrchestrationId).Distinct().Count() != pins.Count)
            return new AgentError(AgentErrorCodes.ReferenceMissing, "The package orchestration pins contain duplicate identities.");
        IReadOnlyDictionary<Guid, AgentOrchestrationBindingSnapshot>? byId = pins.Count == 0 ? null :
            pins.ToDictionary(value => value.OrchestrationId);
        if (byId is not null && (byId.Count != ids.Count || byId.Keys.Except(ids).Any()))
            return new AgentError(AgentErrorCodes.ReferenceMissing, "The package orchestration pins do not match its identities.");
        return ids.Any(id =>
            (byId?.TryGetValue(id, out AgentOrchestrationBindingSnapshot? pin) is true
                ? values.SingleOrDefault(value => value.OrchestrationId == id && value.OrchestrationVersionId == pin.OrchestrationVersionId)
                : values.LastOrDefault(value => value.OrchestrationId == id)) is not { Enabled: true })
            ? new AgentError(AgentErrorCodes.ReferenceMissing, "The package references an enabled published orchestration that is not available.")
            : null;
    }

    private async Task<IReadOnlyList<AgentPackageChildBindingV1>?> ExportChildBindingsAsync(
        IReadOnlyList<Guid> ids,
        IReadOnlyList<AgentChildBindingSnapshot> pins,
        CancellationToken cancellationToken) =>
        pins.Count > 0 ? AgentContractCloner.ReadOnly(pins.Select(value =>
            new AgentPackageChildBindingV1(value.AgentId.ToString("D"), value.AgentVersionId.ToString("D")))) :
        ids.Count == 0 ? null : AgentContractCloner.ReadOnly(await Task.WhenAll(ids.Select(async id =>
        {
            AgentDefinition agent = (await _repository.GetByIdAsync(id, cancellationToken))!;
            return new AgentPackageChildBindingV1(id.ToString("D"), agent.PublishedVersions[^1].Id.ToString("D"));
        })));

    private async Task<IReadOnlyList<AgentPackageOrchestrationBindingV1>?> ExportOrchestrationBindingsAsync(
        IReadOnlyList<Guid> ids,
        IReadOnlyList<AgentOrchestrationBindingSnapshot> pins,
        CancellationToken cancellationToken)
    {
        if (pins.Count > 0) return AgentContractCloner.ReadOnly(pins.Select(value =>
            new AgentPackageOrchestrationBindingV1(value.OrchestrationId.ToString("D"), value.OrchestrationVersionId.ToString("D"))));
        if (ids.Count == 0) return null;
        IReadOnlyDictionary<Guid, PublishedOrchestrationReference> values =
            (await _orchestrationCatalog!.ListPublishedAsync(cancellationToken))
            .GroupBy(value => value.OrchestrationId)
            .ToDictionary(group => group.Key, group => group.Last());
        return AgentContractCloner.ReadOnly(ids.Select(id => new AgentPackageOrchestrationBindingV1(
            id.ToString("D"), values[id].OrchestrationVersionId.ToString("D"))));
    }

    private async Task<AgentError?> ValidateChildBindingReferencesAsync(
        IReadOnlyList<AgentPackageChildBindingV1> references, CancellationToken cancellationToken)
    {
        foreach (AgentPackageChildBindingV1 reference in references)
        {
            if (!Guid.TryParseExact(reference.AgentId, "D", out Guid id) ||
                !Guid.TryParseExact(reference.AgentVersionId, "D", out Guid versionId))
                return new AgentError(AgentErrorCodes.ReferenceMissing, "The package references an invalid child Agent version.");
            AgentDefinition? agent = await _repository.GetByIdAsync(id, cancellationToken);
            if (agent is null || agent.RuntimeStatus is not AgentRuntimeStatus.Enabled ||
                !agent.PublishedVersions.Any(value => value.Id == versionId))
                return new AgentError(AgentErrorCodes.ReferenceMissing, "The package references a child Agent version that is not available.");
        }
        return null;
    }

    private async Task<AgentError?> ValidateOrchestrationBindingReferencesAsync(
        IReadOnlyList<AgentPackageOrchestrationBindingV1> references, CancellationToken cancellationToken)
    {
        IReadOnlyList<PublishedOrchestrationReference> values = _orchestrationCatalog is null
            ? []
            : await _orchestrationCatalog.ListPublishedAsync(cancellationToken);
        foreach (AgentPackageOrchestrationBindingV1 reference in references)
        {
            if (!Guid.TryParseExact(reference.OrchestrationId, "D", out Guid id) ||
                !Guid.TryParseExact(reference.OrchestrationVersionId, "D", out Guid versionId) ||
                !values.Any(value => value.OrchestrationId == id &&
                                     value.OrchestrationVersionId == versionId && value.Enabled))
                return new AgentError(AgentErrorCodes.ReferenceMissing, "The package references an orchestration version that is not available.");
        }
        return null;
    }

    private static bool TryReadPackage(
        string? json,
        out AgentPackageV1? package,
        out AgentError? error)
    {
        package = null;
        error = null;
        if (string.IsNullOrWhiteSpace(json) || Encoding.UTF8.GetByteCount(json) > MaximumPackageUtf8Bytes)
        {
            error = PackageInvalid("The Agent package is empty or exceeds the supported size.");
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(
                json,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = MaximumPackageDepth
                });
            if (document.RootElement.ValueKind is not JsonValueKind.Object)
            {
                error = PackageInvalid("The Agent package root must be a JSON object.");
                return false;
            }

            int nodeCount = 0;
            if (!ValidateJsonSafety(document.RootElement, ref nodeCount, out string? safetyError))
            {
                error = PackageInvalid(safetyError!);
                return false;
            }

            package = JsonSerializer.Deserialize<AgentPackageV1>(json, SerializerOptions);
            if (package is null)
            {
                error = PackageInvalid("The Agent package is missing.");
                return false;
            }

            return true;
        }
        catch (JsonException)
        {
            error = PackageInvalid("The Agent package is not valid supported JSON.");
            return false;
        }
        catch (NotSupportedException)
        {
            error = PackageInvalid("The Agent package contains unsupported values.");
            return false;
        }
    }

    private bool TryValidate(
        AgentPackageV1 package,
        out AgentRuntimeStatus runtimeStatus,
        out AgentOutputMode outputMode,
        out AgentError? error)
    {
        runtimeStatus = default;
        outputMode = default;
        error = null;

        if (!string.Equals(package.Format, FormatIdentifier, StringComparison.Ordinal))
        {
            error = PackageInvalid("The Agent package format identifier is not supported.");
            return false;
        }

        if (!TryParseSemanticVersion(package.Version, out int major))
        {
            error = PackageInvalid("The Agent package version is not a semantic version.");
            return false;
        }

        if (major != 1)
        {
            error = new AgentError(
                AgentErrorCodes.PackageVersionUnsupported,
                "The Agent package major version is not supported.");
            return false;
        }

        AgentPackageAgentV1? agent = package.Agent;
        if (agent is null ||
            agent.Draft is null ||
            agent.Deployment is null ||
            agent.Skills is null ||
            agent.Tools is null ||
            agent.Code is null ||
            agent.Name is null ||
            agent.Description is null ||
            agent.RuntimeStatus is null ||
            agent.Draft.Instructions is null ||
            agent.Draft.ModelProfileId is null ||
            agent.Draft.OutputMode is null)
        {
            error = PackageInvalid("The Agent package is missing required fields.");
            return false;
        }

        IReadOnlyList<string> knowledgeBases = agent.KnowledgeBases ?? Array.Empty<string>();
        if (knowledgeBases.Count > 32 ||
            knowledgeBases.Distinct(StringComparer.Ordinal).Count() != knowledgeBases.Count ||
            knowledgeBases.Any(reference => !Guid.TryParseExact(reference, "D", out _)))
        {
            error = PackageInvalid("Knowledge references must be unique enabled knowledge base IDs.");
            return false;
        }

        IReadOnlyList<AgentPackageChildBindingV1> childAgents = agent.ChildAgents ?? [];
        if (childAgents.Any(value => value is null ||
                                     !Guid.TryParseExact(value.AgentId, "D", out _) ||
                                     !Guid.TryParseExact(value.AgentVersionId, "D", out _)) ||
            childAgents.Select(value => Guid.Parse(value.AgentId)).Distinct().Count() != childAgents.Count)
        {
            error = PackageInvalid("Child Agent references must contain unique Agent and published version IDs.");
            return false;
        }

        IReadOnlyList<AgentPackageOrchestrationBindingV1> orchestrations = agent.Orchestrations ?? [];
        if (orchestrations.Any(value => value is null ||
                                        !Guid.TryParseExact(value.OrchestrationId, "D", out _) ||
                                        !Guid.TryParseExact(value.OrchestrationVersionId, "D", out _)) ||
            orchestrations.Select(value => Guid.Parse(value.OrchestrationId)).Distinct().Count() != orchestrations.Count)
        {
            error = PackageInvalid("Orchestration references must contain unique orchestration and published version IDs.");
            return false;
        }

        if (!IsNormalizedCode(agent.Code))
        {
            error = PackageInvalid("Imported Agent code must be lowercase kebab-case.");
            return false;
        }

        if (string.Equals(agent.RuntimeStatus, nameof(AgentRuntimeStatus.Enabled), StringComparison.Ordinal))
        {
            runtimeStatus = AgentRuntimeStatus.Enabled;
        }
        else if (string.Equals(agent.RuntimeStatus, nameof(AgentRuntimeStatus.Disabled), StringComparison.Ordinal))
        {
            runtimeStatus = AgentRuntimeStatus.Disabled;
        }
        else if (string.Equals(agent.RuntimeStatus, nameof(AgentRuntimeStatus.Archived), StringComparison.Ordinal))
        {
            runtimeStatus = AgentRuntimeStatus.Archived;
        }
        else
        {
            error = PackageInvalid("Runtime status must be Enabled, Disabled, or Archived.");
            return false;
        }

        if (string.Equals(agent.Draft.OutputMode, nameof(AgentOutputMode.Text), StringComparison.Ordinal))
        {
            outputMode = AgentOutputMode.Text;
        }
        else if (string.Equals(agent.Draft.OutputMode, nameof(AgentOutputMode.Structured), StringComparison.Ordinal))
        {
            outputMode = AgentOutputMode.Structured;
        }
        else
        {
            error = PackageInvalid("Output mode must be Text or Structured.");
            return false;
        }

        if (!string.Equals(agent.Deployment.Target, AgentDefinition.ServerDeploymentTarget, StringComparison.Ordinal) ||
            !string.Equals(agent.Deployment.Host, AgentDefinition.ApiHost, StringComparison.Ordinal))
        {
            error = PackageInvalid("Deployment must target Server on EU.Core.Agent.Api.");
            return false;
        }

        if (agent.Tools.Count > 128 ||
            agent.Tools.Distinct(StringComparer.Ordinal).Count() != agent.Tools.Count ||
            agent.Tools.Any(reference => !Guid.TryParseExact(reference, "D", out _)))
        {
            error = PackageInvalid("Tool references must be unique available MCP tool version IDs.");
            return false;
        }

        if (agent.Skills.Count > 64 ||
            agent.Skills.Distinct(StringComparer.Ordinal).Count() != agent.Skills.Count ||
            agent.Skills.Any(reference => !Guid.TryParseExact(reference, "D", out _)))
        {
            error = PackageInvalid("Skill references must be unique published Skill version IDs.");
            return false;
        }

        if (outputMode is AgentOutputMode.Text)
        {
            if (agent.Draft.OutputJsonSchema is not null)
            {
                error = PackageInvalid("Text output cannot carry a JSON schema.");
                return false;
            }
        }
        else
        {
            JsonSchemaValidationResult schema = _schemaValidator.Validate(agent.Draft.OutputJsonSchema);
            if (!schema.IsValid)
            {
                error = new AgentError(AgentErrorCodes.OutputSchemaInvalid, schema.Error!);
                return false;
            }
        }

        return true;
    }

    private static bool ValidateJsonSafety(JsonElement element, ref int nodeCount, out string? error)
    {
        if (++nodeCount > MaximumPackageNodes)
        {
            error = "The Agent package exceeds the supported complexity.";
            return false;
        }

        if (element.ValueKind is JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    error = "The Agent package cannot contain duplicate property names.";
                    return false;
                }

                string normalizedName = new(property.Name.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
                if (ForbiddenPropertyNames.Contains(normalizedName))
                {
                    error = "The Agent package cannot contain credential, endpoint, or connection properties.";
                    return false;
                }

                if (!ValidateJsonSafety(property.Value, ref nodeCount, out error))
                {
                    return false;
                }
            }
        }
        else if (element.ValueKind is JsonValueKind.Array)
        {
            foreach (JsonElement item in element.EnumerateArray())
            {
                if (!ValidateJsonSafety(item, ref nodeCount, out error))
                {
                    return false;
                }
            }
        }
        else if (element.ValueKind is JsonValueKind.String)
        {
            string value = element.GetString() ?? string.Empty;
            if (LooksLikeAbsolutePath(value) || LooksLikeSecretReference(value))
            {
                error = "The Agent package cannot contain secret-shaped references or absolute paths.";
                return false;
            }
        }

        error = null;
        return true;
    }

    private static bool IsNormalizedCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value[0] == '-' || value[^1] == '-')
        {
            return false;
        }

        bool previousHyphen = false;
        foreach (char character in value)
        {
            if (character is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                previousHyphen = false;
            }
            else if (character == '-' && !previousHyphen)
            {
                previousHyphen = true;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryParseSemanticVersion(string? value, out int major)
    {
        major = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string[] parts = value.Split('.');
        return parts.Length == 3 &&
               TryParseCanonicalNumericIdentifier(parts[0], out major) &&
               TryParseCanonicalNumericIdentifier(parts[1], out _) &&
               TryParseCanonicalNumericIdentifier(parts[2], out _);
    }

    private static bool TryParseCanonicalNumericIdentifier(string value, out int number)
    {
        number = 0;
        return value.Length > 0 &&
               (value.Length == 1 || value[0] != '0') &&
               value.All(char.IsAsciiDigit) &&
               int.TryParse(value, out number);
    }

    private static bool LooksLikeAbsolutePath(string value)
    {
        if (value.StartsWith("/", StringComparison.Ordinal) ||
            value.Contains(" /", StringComparison.Ordinal) ||
            value.Contains("\\\\", StringComparison.Ordinal) ||
            value.Contains("file:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        for (int index = 0; index + 2 < value.Length; index++)
        {
            if (char.IsAsciiLetter(value[index]) &&
                value[index + 1] == ':' &&
                (value[index + 2] == '\\' || value[index + 2] == '/'))
            {
                return true;
            }
        }

        return false;
    }

    private static bool LooksLikeSecretReference(string value) =>
        value.Contains("alias:", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("sk-", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("password=", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("api key=", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("apikey=", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("connection string=", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("data source=", StringComparison.OrdinalIgnoreCase);

    private static AgentError PackageInvalid(string message) =>
        new(AgentErrorCodes.PackageInvalid, message);
}
