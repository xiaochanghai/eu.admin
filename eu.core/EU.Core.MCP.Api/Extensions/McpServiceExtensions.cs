using ClaudeMCP.API.Interfaces;
using ClaudeMCP.API.Services.Implementations;
using ClaudeMCP.API.Services.Tool;

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

        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<ISupplierToolService, SupplierToolService>();

        return services;
    }
}