namespace EU.Core.Agent.Application.Abstractions.Security;

public interface ICallerContext
{
    string UserId { get; }

    string TenantId { get; }

    IReadOnlySet<string> Permissions { get; }

    string CorrelationId { get; }
}
