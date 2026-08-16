using EU.Core.Agent.Application.Abstractions.Auditing;
using EU.Core.Api.Agent.Security;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public sealed class AgAgentOperationAuditMiddleware_Should
{
    [Fact]
    public void Resolve_audit_repository_per_request()
    {
        Type middlewareType = typeof(AgentOperationAuditMiddleware);

        Assert.DoesNotContain(
            middlewareType.GetConstructors().SelectMany(constructor =>
                constructor.GetParameters()),
            parameter => parameter.ParameterType ==
                typeof(IAgentOperationAuditRepository));

        System.Reflection.MethodInfo invokeMethod = Assert.Single(
            middlewareType.GetMethods(),
            method => method.Name == nameof(AgentOperationAuditMiddleware.InvokeAsync));
        Type[] parameterTypes = invokeMethod
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        Assert.Equal(
            [typeof(HttpContext), typeof(IAgentOperationAuditRepository)],
            parameterTypes);
    }
}
