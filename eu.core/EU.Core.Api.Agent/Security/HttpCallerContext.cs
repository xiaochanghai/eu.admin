using System.Globalization;
using EU.Core.Common.HttpContextUser;
using EU.Core.IServices.Abstractions.Security;

namespace EU.Core.Api.Agent.Security;

public sealed class HttpCallerContext : ICallerContext
{
    private readonly IUser _user;

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
        _user = user;
        // 仅允许发起项目查询；具体模块/公司授权由 MCP 在业务库中重新核实。
        Permissions = new HashSet<string>(StringComparer.Ordinal) { "business.project.query" };
        CorrelationId = string.IsNullOrWhiteSpace(context.TraceIdentifier)
            ? throw InvalidContext()
            : context.TraceIdentifier;
    }

    public string UserId { get; }

    public string TenantId { get; }

    /// <summary>复用现有 IUser 提供的集团归属。</summary>
    public Guid? GroupId => _user.GroupId;

    /// <summary>复用现有 IUser 提供的公司归属，不接受客户端所有者参数。</summary>
    public Guid? CompanyId => _user.CompanyId;

    public IReadOnlySet<string> Permissions { get; }

    public string CorrelationId { get; }

    private static InvalidOperationException InvalidContext() =>
        new("A complete trusted caller context is required.");
}
