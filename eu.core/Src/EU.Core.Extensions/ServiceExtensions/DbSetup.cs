using EU.Core.Common.Seed;
using EU.Core.Extensions.Middlewares;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

    public static void DapperSqlMapper()
    {
        Dapper.SqlMapper.AddTypeHandler(new GuidTypeHandler());
        Dapper.SqlMapper.AddTypeHandler(new NullableGuidTypeHandler());
    }
}
