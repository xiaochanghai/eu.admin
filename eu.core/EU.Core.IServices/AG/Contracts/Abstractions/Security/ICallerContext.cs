#nullable enable

namespace EU.Core.IServices.Abstractions.Security;

public interface ICallerContext
{
    string UserId { get; }

    string TenantId { get; }

    IReadOnlySet<string> Permissions { get; }

    string CorrelationId { get; }
}
