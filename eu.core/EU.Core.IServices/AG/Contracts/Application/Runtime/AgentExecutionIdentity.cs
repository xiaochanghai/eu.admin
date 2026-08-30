#nullable enable

using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace EU.Core.IServices.Runtime;

public sealed partial class AgentExecutionIdentity
{
    public AgentExecutionIdentity(
        string userId,
        string tenantId,
        IEnumerable<string> permissions,
        string correlationId)
    {
        UserId = Required(userId, nameof(userId));
        TenantId = Required(tenantId, nameof(tenantId));
        CorrelationId = Required(correlationId, nameof(correlationId));
        string[] frozenPermissions = permissions?
            .Select(value => value?.Trim() ?? string.Empty)
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray() ?? throw new ArgumentNullException(nameof(permissions));
        if (frozenPermissions.Length > 128
            || frozenPermissions.Any(value => !PermissionPattern().IsMatch(value)))
        {
            throw new ArgumentException("Execution permissions are invalid.", nameof(permissions));
        }

        Permissions = new ReadOnlyCollection<string>(frozenPermissions);
    }

    public string UserId { get; }

    public string TenantId { get; }

    public IReadOnlyList<string> Permissions { get; }

    public string CorrelationId { get; }

    private static string Required(string? value, string parameterName)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is 0 or > 256 || normalized.Any(char.IsControl))
        {
            throw new ArgumentException("Execution identity value is invalid.", parameterName);
        }

        return normalized;
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9._:-]{0,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex PermissionPattern();
}
