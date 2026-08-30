using System.Globalization;
using EU.Core.Common.HttpContextUser;
using EU.Core.IServices.Abstractions.Security;

namespace EU.Core.Api.Agent.Security;

public sealed class HttpCallerContext : ICallerContext
{
    public HttpCallerContext(
        IHttpContextAccessor accessor,
        IUser user)
    {
        HttpContext context = accessor.HttpContext ?? throw InvalidContext();
        if (context.User.Identity?.IsAuthenticated != true)
        {
            throw InvalidContext();
        }

        UserId = user.ID?.ToString("D") ?? throw InvalidContext();
        TenantId = user.TenantId.ToString(CultureInfo.InvariantCulture);
        Permissions = new HashSet<string>(StringComparer.Ordinal);
        CorrelationId = string.IsNullOrWhiteSpace(context.TraceIdentifier)
            ? throw InvalidContext()
            : context.TraceIdentifier;
    }

    public string UserId { get; }

    public string TenantId { get; }

    public IReadOnlySet<string> Permissions { get; }

    public string CorrelationId { get; }

    private static InvalidOperationException InvalidContext() =>
        new("A complete trusted caller context is required.");
}
