using System.Collections.ObjectModel;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Security;

public sealed class BusinessCallerContext
{
    public BusinessCallerContext(
        string userId,
        string tenantId,
        IEnumerable<string> permissions,
        IEnumerable<string> allowedDataSourceCodes,
        IReadOnlyDictionary<string, IReadOnlyList<string>> dataScopes)
    {
        UserId = userId ?? string.Empty;
        TenantId = tenantId ?? string.Empty;
        Permissions = new ReadOnlySet<string>(new HashSet<string>(
            permissions ?? [],
            StringComparer.Ordinal));
        AllowedDataSourceCodes = new ReadOnlySet<string>(new HashSet<string>(
            allowedDataSourceCodes ?? [],
            StringComparer.Ordinal));
        DataScopes = new ReadOnlyDictionary<string, IReadOnlyList<string>>(
            (dataScopes ?? new Dictionary<string, IReadOnlyList<string>>())
                .ToDictionary(
                    item => item.Key,
                    item => (IReadOnlyList<string>)new ReadOnlyCollection<string>(
                        (item.Value ?? []).ToArray()),
                    StringComparer.Ordinal));
    }

    public string UserId { get; }

    public string TenantId { get; }

    public IReadOnlySet<string> Permissions { get; }

    public IReadOnlySet<string> AllowedDataSourceCodes { get; }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> DataScopes { get; }
}
