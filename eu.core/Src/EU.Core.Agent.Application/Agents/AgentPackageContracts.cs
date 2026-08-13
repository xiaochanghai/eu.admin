using System.Text.Json.Serialization;
using EU.Core.Model.ViewModels.Extend;

namespace EU.Core.Agent.Application.Agents;

public interface IModelProfileReferenceCatalog
{
    Task<bool> ExistsAsync(
        string modelProfileId,
        CancellationToken cancellationToken = default);
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

    public Task<bool> ExistsAsync(
        string modelProfileId,
        CancellationToken cancellationToken = default)
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
        return segments.All(IsSafeSegment);
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

public sealed record AgentPackageChildBindingV1(
    string AgentId,
    string AgentVersionId);

public sealed record AgentPackageOrchestrationBindingV1(
    string OrchestrationId,
    string OrchestrationVersionId);

public sealed record AgentPackageDraftV1(
    [property: JsonPropertyOrder(0)] string Instructions,
    [property: JsonPropertyOrder(1)] string ModelProfileId,
    [property: JsonPropertyOrder(2)] string OutputMode,
    [property: JsonPropertyOrder(3)] string? OutputJsonSchema);

public sealed record AgentPackageDeploymentV1(
    [property: JsonPropertyOrder(0)] string Target,
    [property: JsonPropertyOrder(1)] string Host);
