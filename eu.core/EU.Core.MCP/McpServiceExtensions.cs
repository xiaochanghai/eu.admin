using EU.Core.MCP.Interfaces;
using EU.Core.MCP.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EU.Core.MCP;

/// <summary>
/// MCP 服务扩展
/// </summary>
public static class McpServiceExtensions
{
    /// <summary>
    /// 添加 MCP 服务
    /// </summary>
    public static IServiceCollection AddMcpServices(this IServiceCollection services)
    {
        services.AddScoped<IMcpService, McpService>();
        services.AddScoped<IMcpService, FastMcpService>();
        services.AddScoped<IMcpStreamService, McpStreamService>();

        return services;
    }
}
