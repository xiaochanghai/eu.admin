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

        return identifier.Split('/').All(IsSafeSegment);
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
