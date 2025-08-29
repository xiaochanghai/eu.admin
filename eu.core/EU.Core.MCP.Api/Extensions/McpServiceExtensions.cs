using ClaudeMCP.API.Interfaces;
using ClaudeMCP.API.Services.Implementations;

namespace EU.Core.MCP.Api.Extensions;

public static class McpServiceExtensions
{
    /// <summary>
    /// 添加 MCP 服务
    /// </summary>
    public static IServiceCollection AddMcpServices(this IServiceCollection services)
    {

        services.AddScoped<IMcpService, McpService>();
        services.AddScoped<IToolService, TestToolService>();

        return services;
    }
}