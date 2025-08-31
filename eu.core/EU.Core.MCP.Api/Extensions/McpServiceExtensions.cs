using Autofac;
using EU.Core.Api.MCP.Interfaces;
using EU.Core.Api.MCP.Services.Implementations; 

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

        services.AddScoped<ISupplierService, SupplierService>();    //注册服务

        return services;
    }
}

public class AutofacMCPModuleRegister : Autofac.Module
{
    //protected override void Load(ContainerBuilder builder)
    //{
    //    builder.RegisterGeneric(typeof(Services.BASE.BaseServices<>)).As(typeof(IBaseServices<>)).InstancePerDependency();     //注册服务

    //}
}