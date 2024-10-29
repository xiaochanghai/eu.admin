using EU.Core.Common;
using EU.Core.Common.DB;
using EU.Core.Common.Seed;
using EU.Core.Extensions;
using EU.Core.Model;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace JianLian.HDIS.CodeGenerator;
class Program
{
    public static MutiDBOperate GetMainConnectionDb()
    {
        var mainConnetctDb = BaseDBConfig.MutiConnectionString.allDbs.Find(x => x.ConnId == MainDb.CurrentDbConnId);
        if (BaseDBConfig.MutiConnectionString.allDbs.Count > 0)
        {
            if (mainConnetctDb == null)
            {
                mainConnetctDb = BaseDBConfig.MutiConnectionString.allDbs[0];
            }
        }
        else
        {
            throw new Exception("请确保appsettigns.json中配置连接字符串,并设置Enabled为true;");
        }

        return mainConnetctDb;
    }
    static void Main(string[] args)
    {

        var basePath = AppContext.BaseDirectory;
        IServiceCollection services = new ServiceCollection();
        //services.AddAutoMapperSetup();

        services.AddSingleton(new AppSettings(basePath));
        services.AddScoped<DBSeed>();
        services.AddScoped<MyContext>();
        services.AddSqlsugarSetup();

        //services.AddScoped<SqlSugar.ISqlSugarClient>(o =>
        //{
        //    return new SqlSugar.SqlSugarScope(new SqlSugar.ConnectionConfig()
        //    {
        //        ConnectionString = GetMainConnectionDb().Connection,    //必填, 数据库连接字符串
        //        DbType = (SqlSugar.DbType)GetMainConnectionDb().DbType, //必填, 数据库类型
        //        IsAutoCloseConnection = true,                           //默认false, 时候知道关闭数据库连接, 设置为true无需使用using或者Close操作
        //    });
        //});
        var sp = services.BuildServiceProvider();
        Console.WriteLine("请输入表名!");
        string tableName = Console.ReadLine() ?? "SmApiLog";
        var _sqlSugarClient = sp.GetService<ISqlSugarClient>() as SqlSugarScope;

        string? ConnID = null;

        ConnID = ConnID == null ? MainDb.CurrentDbConnId.ToLower() : ConnID;
        _sqlSugarClient?.ChangeDatabase(ConnID.ToLower());

        var tableName1 = tableName.Split(',');

        var isMuti = BaseDBConfig.IsMulti;
        var data = new ServiceResult<string>() { Success = true, Message = "" };
        if (1 == 1)
        {
            for (int i = 0; i < tableName1.Length; i++)
            {
                string[] tableNames = new string[1];
                tableNames[0] = tableName1[i];
                data.Data += $"Controller层生成：{FrameSeed.CreateControllers(_sqlSugarClient, ConnID, isMuti, tableNames)} || ";
                data.Data += $"库{ConnID}-Model层生成：{FrameSeed.CreateModels(_sqlSugarClient, ConnID, isMuti, tableNames)} || ";
                //data.response += $"库{ConnID}-IRepositorys层生成：{FrameSeed.CreateIRepositorys(_sqlSugarClient, ConnID, isMuti, tableNames)} || ";
                data.Data += $"库{ConnID}-IServices层生成：{FrameSeed.CreateIServices(_sqlSugarClient, ConnID, isMuti, tableNames)} || ";
                //data.response += $"库{ConnID}-Repository层生成：{FrameSeed.CreateRepository(_sqlSugarClient, ConnID, isMuti, tableNames)} || ";
                data.Data += $"库{ConnID}-Services层生成：{FrameSeed.CreateServices(_sqlSugarClient, ConnID, isMuti, tableNames)} || ";
            }
            // 切回主库
            _sqlSugarClient?.ChangeDatabase(MainDb.CurrentDbConnId.ToLower());
        }
        //else
        //{
        //    data.Success = false;
        //    data.Message = "当前不处于开发模式，代码生成不可用！";
        //}

        // ALL Completed
        Console.WriteLine("ALL Completed!");
        Console.ReadKey();
        Thread.Sleep(Timeout.Infinite);

    }

    public static List<string> ToUnderscoreCase(string str)
    {
        var str1 = string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        return str1.Split('_').ToList();
    }
}