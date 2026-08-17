using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Http.Features;

namespace EU.Core.Api.Agent.Errors;

/// <summary>
/// 在 HTTP 边界解析 Agent 错误映射，并记录遗漏的固定错误码。
/// </summary>
public static class AgentApiErrorResolver
{
    private const string LoggerCategory = "EU.Core.Api.Agent.Errors.AgentApiErrorCatalog";

    public static AgentApiErrorDescriptor Resolve(HttpContext context, string errorCode)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!string.IsNullOrWhiteSpace(errorCode)
            && AgentApiErrorCatalog.All.TryGetValue(errorCode, out AgentApiErrorDescriptor? descriptor))
        {
            return descriptor;
        }

        context.Features
            .Get<IServiceProvidersFeature>()?
            .RequestServices
            .GetService<ILoggerFactory>()?
            .CreateLogger(LoggerCategory)
            .LogWarning(
                "Agent API ErrorCode '{ErrorCode}' is not registered; using the safe fallback mapping.",
                errorCode);
        return AgentApiErrorCatalog.Resolve(errorCode);
    }
}
