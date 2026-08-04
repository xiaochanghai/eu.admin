using EU.Core.Common.DB;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EU.Core.DataAccess;

/// <summary>
/// 直接创建 Context
/// </summary>
public static class ContextFactory
{
    /// <summary>
    /// 创建DbContext
    /// </summary>
    /// <returns></returns>
    public static DataContext CreateContext()
    {
        var builder = new DbContextOptionsBuilder<DataContext>();

        var mainConnetctDb = BaseDBConfig.MutiConnectionString.allDbs.Find(x => x.ConnId == MainDb.CurrentDbConnId);
        if (mainConnetctDb == null)
            throw new InvalidOperationException($"未找到主数据库配置：{MainDb.CurrentDbConnId}");

        switch (mainConnetctDb.DbType)
        {
            case DataBaseType.SqlServer:
                builder.UseSqlServer(mainConnetctDb.Connection, o => o.UseCompatibilityLevel(120));
                break;
            case DataBaseType.MySql:
                builder.UseMySQL(mainConnetctDb.Connection);
                break;
            default:
                throw new NotSupportedException($"CreateContext 暂不支持数据库类型：{mainConnetctDb.DbType}");
        }

        return new DataContext(builder.Options);
    }

    public static void AddDataContextSetup(this IServiceCollection services)
    {
        var mainConnetctDb = BaseDBConfig.MutiConnectionString.allDbs.Find(x => x.ConnId == MainDb.CurrentDbConnId);
        services.AddDbContext<DataContext>(options =>
        {
            options.UseLazyLoadingProxies().UseSqlServer(mainConnetctDb.Connection, o => o.UseCompatibilityLevel(120));
        });
    }

    public static void UseDataContext(this WebApplication app)
    {
        var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        try
        {
            var dbContext = scope.ServiceProvider.GetService<DataContext>();

            dbContext.Database.EnsureCreated();
            dbContext.Database.Migrate();
        }
        catch (Exception)
        {
            throw;
        }
    }
}
