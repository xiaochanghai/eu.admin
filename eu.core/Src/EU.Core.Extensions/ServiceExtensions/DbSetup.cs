using EU.Core.Common.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace EU.Core.Extensions;

/// <summary>
/// Db 启动服务
/// </summary>
public static class DbSetup
{
    public static void AddDbSetup(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        services.AddScoped<DBSeed>();
        services.AddScoped<MyContext>();
    }
}
