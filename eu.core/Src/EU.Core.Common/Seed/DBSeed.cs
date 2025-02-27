using EU.Core.Common.DB;
using EU.Core.Common.Extensions;
using EU.Core.Common.Helper;
using EU.Core.Model.Models;
using EU.Core.Model.Tenants;
using Magicodes.ExporterAndImporter.Excel;
using Newtonsoft.Json;
using SqlSugar;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using EU.Core.Common.Const;
using EU.Core.Model.Models.RootTkey;
using EU.Core.Model;
using Microsoft.Extensions.DependencyInjection;

namespace EU.Core.Common.Seed;

public class DBSeed
{
    private static string SeedDataFolder = "TiobonCore.Data.json/{0}.tsv";


    /// <summary>
    /// 异步添加种子数据
    /// </summary>
    /// <param name="myContext"></param>
    /// <param name="WebRootPath"></param>
    /// <returns></returns>
    public static async Task SeedAsync(MyContext myContext, string WebRootPath)
    {
        try
        {
            if (string.IsNullOrEmpty(WebRootPath))
            {
                throw new Exception("获取wwwroot路径时，异常！");
            }

            SeedDataFolder = Path.Combine(WebRootPath, SeedDataFolder);

            Console.WriteLine("************ EU.Core DataBase Set *****************");
            Console.WriteLine($"Master DB ConId: {myContext.Db.CurrentConnectionConfig.ConfigId}");
            Console.WriteLine($"Master DB Type: {myContext.Db.CurrentConnectionConfig.DbType}");
            Console.WriteLine($"Master DB ConnectString: {myContext.Db.CurrentConnectionConfig.ConnectionString}");
            Console.WriteLine();
            if (BaseDBConfig.MainConfig.SlaveConnectionConfigs.AnyNoException())
            {
                var index = 0;
                BaseDBConfig.MainConfig.SlaveConnectionConfigs.ForEach(m =>
                {
                    index++;
                    Console.WriteLine($"Slave{index} DB HitRate: {m.HitRate}");
                    Console.WriteLine($"Slave{index} DB ConnectString: {m.ConnectionString}");
                    Console.WriteLine($"--------------------------------------");
                });
            }
            else if (BaseDBConfig.ReuseConfigs.AnyNoException())
            {
                var index = 0;
                BaseDBConfig.ReuseConfigs.ForEach(m =>
                {
                    index++;
                    Console.WriteLine($"Reuse{index} DB ID: {m.ConfigId}");
                    Console.WriteLine($"Reuse{index} DB Type: {m.DbType}");
                    Console.WriteLine($"Reuse{index} DB ConnectString: {m.ConnectionString}");
                    Console.WriteLine($"--------------------------------------");
                });
            }

            Console.WriteLine();

            // 创建数据库
            Console.WriteLine($"Create Database(The Db Id:{MyContext.ConnId})...");

            if (MyContext.DbType != SqlSugar.DbType.Oracle && MyContext.DbType != SqlSugar.DbType.Dm)
            {
                myContext.Db.DbMaintenance.CreateDatabase();
                ConsoleHelper.WriteSuccessLine($"Database created successfully!");
            }
            else
            {
                //Oracle 数据库不支持该操作
                ConsoleHelper.WriteSuccessLine($"Oracle 数据库不支持该操作，可手动创建Oracle/Dm数据库!");
            }

            // 创建数据库表，遍历指定命名空间下的class，
            // 注意不要把其他命名空间下的也添加进来。
            Console.WriteLine("Create Tables...");

            var path = AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory;
            var referencedAssemblies = System.IO.Directory.GetFiles(path, "EU.Core.Model.dll")
                .Select(Assembly.LoadFrom).ToArray();
            var modelTypes = referencedAssemblies
                .SelectMany(a => a.DefinedTypes)
                .Select(type => type.AsType())
                .Where(x => x.IsClass && x.Namespace is "EU.Core.Model.Models")
                .Where(s => !s.IsDefined(typeof(MultiTenantAttribute), false))
                .ToList();
            modelTypes.ForEach(t =>
            {
                // 这里只支持添加表，不支持删除
                // 如果想要删除，数据库直接右键删除，或者联系SqlSugar作者；
                if (!myContext.Db.DbMaintenance.IsAnyTable(t.Name))
                {
                    Console.WriteLine(t.Name);
                    myContext.Db.CodeFirst.SplitTables().InitTables(t);
                }
            });
            ConsoleHelper.WriteSuccessLine($"Tables created successfully!");
            Console.WriteLine();

            if (AppSettings.app(new string[] { "AppSettings", "SeedDBDataEnabled" }).ObjToBool())
            {
                JsonSerializerSettings setting = new JsonSerializerSettings();
                JsonConvert.DefaultSettings = new Func<JsonSerializerSettings>(() =>
                {
                    //日期类型默认格式化处理
                    setting.DateFormatHandling = DateFormatHandling.MicrosoftDateFormat;
                    setting.DateFormatString = "yyyy-MM-dd HH:mm:ss";

                    //空值处理
                    setting.NullValueHandling = NullValueHandling.Ignore;

                    //高级用法九中的Bool类型转换 设置
                    //setting.Converters.Add(new BoolConvert("是,否"));

                    return setting;
                });

                Console.WriteLine($"Seeding database data (The Db Id:{MyContext.ConnId})...");

                var importer = new ExcelImporter();

                //种子初始化
                await SeedDataAsync(myContext.Db);

                ConsoleHelper.WriteSuccessLine($"Done seeding database!");
            }

            Console.WriteLine();
        }
        catch (Exception ex)
        {
            throw new Exception(
                $"1、若是Mysql,查看常见问题:https://github.com/anjoy8/EU.Core/issues/148#issue-776281770 \n" +
                $"2、若是Oracle,查看常见问题:https://github.com/anjoy8/EU.Core/issues/148#issuecomment-752340231 \n" +
                "3、其他错误：" + ex.Message);
        }
    }

    /// <summary>
    /// 种子初始化数据
    /// </summary>
    /// <param name="myContext"></param>
    /// <returns></returns>
    private static async Task SeedDataAsync(ISqlSugarClient db)
    {
        // 获取所有种子配置-初始化数据
        var seedDataTypes = AssemblysExtensions.GetAllAssemblies().SelectMany(s => s.DefinedTypes)
            .Where(u => !u.IsInterface && !u.IsAbstract && u.IsClass)
            .Where(u =>
            {
                var esd = u.GetInterfaces()
                    .FirstOrDefault(i => i.HasImplementedRawGeneric(typeof(IEntitySeedData<>)));
                if (esd is null)
                {
                    return false;
                }

                var eType = esd.GenericTypeArguments[0];
                if (eType.GetCustomAttribute<MultiTenantAttribute>() is null)
                {
                    return true;
                }

                return false;
            });

        if (!seedDataTypes.Any()) return;
        foreach (var seedType in seedDataTypes)
        {
            dynamic instance = Activator.CreateInstance(seedType);
            //初始化数据
            {
                var seedData = instance.InitSeedData();
                if (seedData != null && Enumerable.Any(seedData))
                {
                    var entityType = seedType.GetInterfaces().First().GetGenericArguments().First();
                    var entity = db.EntityMaintenance.GetEntityInfo(entityType);

                    if (!await db.Queryable(entity.DbTableName, "").AnyAsync())
                    {
                        await db.Insertable(Enumerable.ToList(seedData)).ExecuteCommandAsync();
                        Console.WriteLine($"Table:{entity.DbTableName} init success!");
                    }
                }
            }

            //种子数据
            {
                var seedData = instance.SeedData();
                if (seedData != null && Enumerable.Any(seedData))
                {
                    var entityType = seedType.GetInterfaces().First().GetGenericArguments().First();
                    var entity = db.EntityMaintenance.GetEntityInfo(entityType);

                    await db.Storageable(Enumerable.ToList(seedData)).ExecuteCommandAsync();
                    Console.WriteLine($"Table:{entity.DbTableName} seedData success!");
                }
            }

            //自定义处理
            {
                await instance.CustomizeSeedData(db);
            }
        }
    }

    /// <summary>
    /// 迁移日志数据库
    /// </summary>
    /// <returns></returns>
    public static void MigrationLogs(MyContext myContext)
    {
        // 创建数据库表，遍历指定命名空间下的class，
        // 注意不要把其他命名空间下的也添加进来。
        Console.WriteLine("Create Log Tables...");
        if (!myContext.Db.IsAnyConnection(SqlSugarConst.LogConfigId.ToLower()))
        {
            throw new ApplicationException("未配置日志数据库，请在appsettings.json中DBS节点中配置");
        }

        var logDb = myContext.Db.GetConnection(SqlSugarConst.LogConfigId.ToLower());
        Console.WriteLine($"Create log Database(The Db Id:{SqlSugarConst.LogConfigId.ToLower()})...");
        logDb.DbMaintenance.CreateDatabase();
        ConsoleHelper.WriteSuccessLine($"Log Database created successfully!");
        var path = AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory;
        var referencedAssemblies = System.IO.Directory.GetFiles(path, "EU.Core.Model.dll")
            .Select(Assembly.LoadFrom).ToArray();
        var modelTypes = referencedAssemblies
            .SelectMany(a => a.DefinedTypes)
            .Select(type => type.AsType())
            .Where(x => x.IsClass && x.Namespace != null && x.Namespace.StartsWith("EU.Core.Model.Logs"))
            .ToList();
        Stopwatch sw = Stopwatch.StartNew();

        var tables = logDb.DbMaintenance.GetTableInfoList();

        modelTypes.ForEach(t =>
        {
            // 这里只支持添加修改表，不支持删除
            // 如果想要删除，数据库直接右键删除，或者联系SqlSugar作者；
            if (!tables.Any(s => s.Name.Contains(t.Name)))
            {
                Console.WriteLine(t.Name);
                if (t.GetCustomAttribute<SplitTableAttribute>() != null)
                {
                    logDb.CodeFirst.SplitTables().InitTables(t);
                }
                else
                {
                    logDb.CodeFirst.InitTables(t);
                }
            }
        });

        sw.Stop();

        $"Log Tables created successfully! {sw.ElapsedMilliseconds}ms".WriteSuccessLine();
        Console.WriteLine();
    }


    /// <summary>
    /// 初始化 多租户
    /// </summary>
    /// <param name="myContext"></param>
    /// <returns></returns>
    public static async Task TenantSeedAsync(MyContext myContext)
    {
        var tenants = await myContext.Db.Queryable<SysTenant>().Where(s => s.TenantType == TenantTypeEnum.Db)
            .ToListAsync();
        if (tenants.Any())
        {
            Console.WriteLine($@"Init Multi Tenant Db");
            foreach (var tenant in tenants)
            {
                Console.WriteLine($@"Init Multi Tenant Db : {tenant.ConfigId}/{tenant.Name}");
                await InitTenantSeedAsync(myContext.Db.AsTenant(), tenant.GetConnectionConfig());
            }
        }

        tenants = await myContext.Db.Queryable<SysTenant>().Where(s => s.TenantType == TenantTypeEnum.Tables)
            .ToListAsync();
        if (tenants.Any())
        {
            await InitTenantSeedAsync(myContext, tenants);
        }
    }

    #region 多租户 多表 初始化

    private static async Task InitTenantSeedAsync(MyContext myContext, List<SysTenant> tenants)
    {
        ConsoleHelper.WriteInfoLine($"Init Multi Tenant Tables : {myContext.Db.CurrentConnectionConfig.ConfigId}");

        // 获取所有实体表-初始化租户业务表
        var entityTypes = TenantUtil.GetTenantEntityTypes(TenantTypeEnum.Tables);
        if (!entityTypes.Any()) return;

        foreach (var sysTenant in tenants)
        {
            foreach (var entityType in entityTypes)
            {
                myContext.Db.CodeFirst
                    .As(entityType, entityType.GetTenantTableName(myContext.Db, sysTenant))
                    .InitTables(entityType);

                Console.WriteLine($@"Init Tables:{entityType.GetTenantTableName(myContext.Db, sysTenant)}");
            }

            myContext.Db.SetTenantTable(sysTenant.ID.ToString());
            //多租户初始化种子数据
            await TenantSeedDataAsync(myContext.Db, TenantTypeEnum.Tables);
        }

        ConsoleHelper.WriteSuccessLine(
            $"Init Multi Tenant Tables : {myContext.Db.CurrentConnectionConfig.ConfigId} created successfully!");
    }

    #endregion

    #region 多租户 多库 初始化

    /// <summary>
    /// 初始化多库
    /// </summary>
    /// <param name="itenant"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static async Task InitTenantSeedAsync(ITenant itenant, ConnectionConfig config)
    {
        itenant.RemoveConnection(config.ConfigId);
        itenant.AddConnection(config);

        var db = itenant.GetConnectionScope(config.ConfigId);

        db.DbMaintenance.CreateDatabase();
        ConsoleHelper.WriteSuccessLine($"Init Multi Tenant Db : {config.ConfigId} Database created successfully!");

        Console.WriteLine($@"Init Multi Tenant Db : {config.ConfigId}  Create Tables");

        // 获取所有实体表-初始化租户业务表
        var entityTypes = TenantUtil.GetTenantEntityTypes(TenantTypeEnum.Db);
        if (!entityTypes.Any()) return;
        foreach (var entityType in entityTypes)
        {
            var splitTable = entityType.GetCustomAttribute<SplitTableAttribute>();
            if (splitTable == null)
                db.CodeFirst.InitTables(entityType);
            else
                db.CodeFirst.SplitTables().InitTables(entityType);

            Console.WriteLine(entityType.Name);
        }

        //多租户初始化种子数据
        await TenantSeedDataAsync(db, TenantTypeEnum.Db);
    }

    #endregion

    #region 多租户 种子数据 初始化

    private static async Task TenantSeedDataAsync(ISqlSugarClient db, TenantTypeEnum tenantType)
    {
        // 获取所有种子配置-初始化数据
        var seedDataTypes = AssemblysExtensions.GetAllAssemblies().SelectMany(s => s.DefinedTypes)
            .Where(u => !u.IsInterface && !u.IsAbstract && u.IsClass)
            .Where(u =>
            {
                var esd = u.GetInterfaces()
                    .FirstOrDefault(i => i.HasImplementedRawGeneric(typeof(IEntitySeedData<>)));
                if (esd is null)
                {
                    return false;
                }

                var eType = esd.GenericTypeArguments[0];
                return eType.IsTenantEntity(tenantType);
            });
        if (!seedDataTypes.Any()) return;
        foreach (var seedType in seedDataTypes)
        {
            dynamic instance = Activator.CreateInstance(seedType);
            //初始化数据
            {
                var seedData = instance.InitSeedData();
                if (seedData != null && Enumerable.Any(seedData))
                {
                    var entityType = seedType.GetInterfaces().First().GetGenericArguments().First();
                    var entity = db.EntityMaintenance.GetEntityInfo(entityType);

                    if (!await db.Queryable(entity.DbTableName, "").AnyAsync())
                    {
                        await db.Insertable(Enumerable.ToList(seedData)).ExecuteCommandAsync();
                        Console.WriteLine($"Table:{entity.DbTableName} init success!");
                    }
                }
            }

            //种子数据
            {
                var seedData = instance.SeedData();
                if (seedData != null && Enumerable.Any(seedData))
                {
                    var entityType = seedType.GetInterfaces().First().GetGenericArguments().First();
                    var entity = db.EntityMaintenance.GetEntityInfo(entityType);

                    await db.Storageable(Enumerable.ToList(seedData)).ExecuteCommandAsync();
                    Console.WriteLine($"Table:{entity.DbTableName} seedData success!");
                }
            }

            //自定义处理
            {
                await instance.CustomizeSeedData(db);
            }
        }
    }

    #endregion



    #region 数据表结构迁移到Mysql
    /// <summary>
    /// 数据表结构迁移到Mysql
    /// </summary>
    /// <param name="myContext"></param>
    /// <exception cref="ApplicationException"></exception>
    public static void MigrationLogs1(MyContext myContext)
    {
        // 创建数据库表，遍历指定命名空间下的class，
        // 注意不要把其他命名空间下的也添加进来。
        Console.WriteLine("Create Log Tables...");

        var configID = "WMEU_MYSQL".ToLower();
        if (!myContext.Db.IsAnyConnection(configID))
        {
            throw new ApplicationException("未配置日志数据库，请在appsettings.json中DBS节点中配置");
        }

        var logDb = myContext.Db.GetConnection(configID);
        Console.WriteLine($"Create log Database(The Db Id:{configID})...");
        logDb.DbMaintenance.CreateDatabase();
        ConsoleHelper.WriteSuccessLine($"Log Database created successfully!");
        var path = AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory;
        var referencedAssemblies = System.IO.Directory.GetFiles(path, "EU.Core.Model.dll")
            .Select(Assembly.LoadFrom).ToArray();
        var modelTypes = referencedAssemblies
            .SelectMany(a => a.DefinedTypes)
            .Select(type => type.AsType())
            .Where(x => x.IsClass && x.Namespace != null &&
            x.Namespace.StartsWith("EU.Core.Model.Entity")
            //x.Name == "SmImportDataDetail"
            //!x.Name.EndsWith("BaseEntity") &&
            //!x.Name.EndsWith("BusinessTable") &&
            //!x.Name.EndsWith("MultiBusinessTable") &&
            //!x.Name.EndsWith("SubLibraryBusinessTable") &&
            //!x.Name.EndsWith("SysTenant") &&
            //!x.Name.EndsWith("TasksQz") &&
            //!x.Name.EndsWith("SplitDemo") &&
            //!x.Name.EndsWith("SplitDemo") &&
            //!x.Name.EndsWith("Input") &&
            //!x.Name.EndsWith("Dto")
            )
            .ToList();
        Stopwatch sw = Stopwatch.StartNew();

        var tables = logDb.DbMaintenance.GetTableInfoList();

        modelTypes.ForEach(t =>
        {
            // 这里只支持添加修改表，不支持删除
            // 如果想要删除，数据库直接右键删除，或者联系SqlSugar作者；
            if (!tables.Any(s => s.Name.Contains(t.Name)))
            {
                try
                {

                    Console.WriteLine(t.Name);
                    if (t.GetCustomAttribute<SplitTableAttribute>() != null)
                    {
                        logDb.CodeFirst.SplitTables().InitTables(t);
                    }
                    else
                    {
                        logDb.CodeFirst.InitTables(t);
                    }
                }
                catch (Exception E)
                {

                }
            }
        });

        sw.Stop();

        $"Log Tables created successfully! {sw.ElapsedMilliseconds}ms".WriteSuccessLine();
        Console.WriteLine();
    }
    #endregion

    #region 数据表结构迁移到Mysql
    /// <summary>
    /// 数据表结构迁移到Mysql
    /// </summary>
    /// <param name="myContext"></param>
    /// <exception cref="ApplicationException"></exception>
    public static void GenerateAllEntity(MyContext myContext)
    {
        var path = AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory;
        var referencedAssemblies = System.IO.Directory.GetFiles(path, "EU.Core.Model.dll")
            .Select(Assembly.LoadFrom).ToArray();
        var modelTypes = referencedAssemblies
            .SelectMany(a => a.DefinedTypes)
            .Select(type => type.AsType())
            .Where(x => x.IsClass && x.Namespace != null &&
            x.Namespace.StartsWith("EU.Core.Model.Entity")  
             //x.Name == "IvAccounting"
            //!x.Name.EndsWith("BaseEntity") &&
            //!x.Name.EndsWith("BusinessTable") &&
            //!x.Name.EndsWith("MultiBusinessTable") &&
            //!x.Name.EndsWith("SubLibraryBusinessTable") &&
            //!x.Name.EndsWith("SysTenant") &&
            //!x.Name.EndsWith("TasksQz") &&
            //!x.Name.EndsWith("SplitDemo") &&
            //!x.Name.EndsWith("SplitDemo") &&
            //!x.Name.EndsWith("Input") &&
            //!x.Name.EndsWith("Dto")
            )
            .ToList();
        Stopwatch sw = Stopwatch.StartNew();

        string? ConnID = null;

        ConnID = ConnID == null ? MainDb.CurrentDbConnId.ToLower() : ConnID;
        //myContext.Db?.ChangeDatabase(ConnID.ToLower());
         
        var isMuti = false;
        var data = new ServiceResult<string>() { Success = true, Message = "" };
        modelTypes.ForEach(t =>
        {
            string[] tableNames = [t.Name];
            data.Data += $"Controller层生成：{FrameSeed.CreateControllers(myContext.Db, ConnID, isMuti, tableNames)} || ";
            data.Data += $"库{ConnID}-Model层生成：{FrameSeed.CreateModels(myContext.Db, ConnID, isMuti, tableNames)} || ";
            //data.response += $"库{ConnID}-IRepositorys层生成：{FrameSeed.CreateIRepositorys(_sqlSugarClient, ConnID, isMuti, tableNames)} || ";
            data.Data += $"库{ConnID}-IServices层生成：{FrameSeed.CreateIServices(myContext.Db, ConnID, isMuti, tableNames)} || ";
            //data.response += $"库{ConnID}-Repository层生成：{FrameSeed.CreateRepository(_sqlSugarClient, ConnID, isMuti, tableNames)} || ";
            data.Data += $"库{ConnID}-Services层生成：{FrameSeed.CreateServices(myContext.Db, ConnID, isMuti, tableNames)} || ";
        });

        sw.Stop();

        $"Log Tables created successfully! {sw.ElapsedMilliseconds}ms".WriteSuccessLine();
        Console.WriteLine();
    }
    #endregion
}