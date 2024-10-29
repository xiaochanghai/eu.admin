using EU.Core.Repository.Base;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace EU.Core.Services.Extensions;

public static class AppServiceExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
        var types = Assembly.GetExecutingAssembly().GetTypes();

        var assignedTypes = types
            .Where(m => m.GetBaseClasses().Length > 1)
            .ToList();

        foreach (var assignedType in assignedTypes)
        {
            if (assignedType == typeof(BaseServices<,,,>))
            {
                continue;
            }
            // 添加 XXXService 依赖注入
            services.AddScoped(assignedType);

            // 添加 IXXXService -> XXXService 依赖注入
            var interfaceType = assignedType.GetInterfaces().FirstOrDefault(i => i.Name[1..] == assignedType.Name);
            if (interfaceType != null)
            {
                services.AddScoped(interfaceType, assignedType);
            }
        }

        return services;
    }
}
