using System.Data;
using System.Data.SqlClient;
using EU.Core.Common.Const;
using EU.Core.Common.DB.Dapper;
using EU.Core.Common.Enums;
using EU.Core.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace EU.Core.Common.DB;

public class DBServerProvider
{
    private static Dictionary<string, string> ConnectionPool = new(StringComparer.OrdinalIgnoreCase);

    public static string DefaultConnName = "defalut";
    public static IConfiguration Configuration { get; private set; }

    /// <summary>
    /// 封装要操作的字符
    /// </summary>
    /// <param name="sections">节点配置</param>
    /// <returns></returns>
    public static string app(params string[] sections)
    {
        try
        {

            if (sections.Any())
            {
                return Configuration[string.Join(":", sections)];
            }
        }
        catch (Exception) { }

        return "";
    }
    /// <summary>
    /// 递归获取配置信息数组
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sections"></param>
    /// <returns></returns>
    public static List<T> app<T>(params string[] sections)
    {
        List<T> list = new List<T>();
        // 引用 Microsoft.Extensions.Configuration.Binder 包
        Configuration.Bind(string.Join(":", sections), list);
        return list;
    }
    static DBServerProvider()
    {
        Configuration = new ConfigurationBuilder()
         .Add(new JsonConfigurationSource { Path = "appsettings.json", ReloadOnChange = true })
         .Build();
        MainDb.CurrentDbConnId = app("MainDB");
        List<MutiDBOperate> listdatabase = app<MutiDBOperate>("DBS")
           .Where(i => i.Enabled).ToList();
        var mainConnetctDb = listdatabase.Find(x => x.ConnId == MainDb.CurrentDbConnId);
        SetConnection(DefaultConnName, mainConnetctDb.Connection);
    }
    public static void SetConnection(string key, string val)
    {
        if (ConnectionPool.ContainsKey(key))
        {
            ConnectionPool[key] = val;
            return;
        }
        ConnectionPool.Add(key, val);
    }
    /// <summary>
    /// 设置默认数据库连接
    /// </summary>
    /// <param name="val"></param>
    public static void SetDefaultConnection(string val)
    {
        SetConnection(DefaultConnName, val);
    }

    public static string GetConnectionString(string key)
    {
        key = key ?? DefaultConnName;
        if (ConnectionPool.ContainsKey(key))
        {
            return ConnectionPool[key];
        }
        return key;
    }
    /// <summary>
    /// 获取默认数据库连接
    /// </summary>
    /// <returns></returns>
    public static string GetConnectionString()
    {
        return GetConnectionString(DefaultConnName);
    }
    private static bool _isMysql = DBType.Name == DbCurrentType.MySql.ToString();
    public static IDbConnection GetDbConnection(string connString = null)
    {
        if (_isMysql)
        {
            return new MySql.Data.MySqlClient.MySqlConnection(connString ?? ConnectionPool[DefaultConnName]);
        }
        return new SqlConnection(connString ?? ConnectionPool[DefaultConnName]);
    }
    public static IDbConnection GetMyDbConnection(string connString = null)
    {
        //new MySql.Data.MySqlClient.MySqlConnection(connString);
        string mySql = "Data Source=132.232.2.109;Database=mysql;User ID=xx;Password=xxx;pooling=true;CharSet=utf8;port=3306;sslmode=none";
        // MySqlConnector
        return new MySql.Data.MySqlClient.MySqlConnection(mySql);

    }
    //public static VOLContext DbContext
    //{
    //    get { return GetEFDbContext(); }
    //}
    //public static VOLContext GetEFDbContext()
    //{
    //    return GetEFDbContext(null);
    //}
    //public static VOLContext GetEFDbContext(string dbName)
    //{
    //    VOLContext beefContext = Utilities.HttpContext.Current.RequestServices.GetService(typeof(VOLContext)) as VOLContext;
    //    if (dbName != null)
    //    {
    //        if (!ConnectionPool.ContainsKey(dbName))
    //        {
    //            throw new Exception("数据库连接名称错误");
    //        }
    //        beefContext.Database.GetDbConnection().ConnectionString = ConnectionPool[dbName];
    //    }
    //    return beefContext;
    //}

    //public static void SetDbContextConnection(VOLContext beefContext, string dbName)
    //{
    //    if (!ConnectionPool.ContainsKey(dbName))
    //    {
    //        throw new Exception("数据库连接名称错误");
    //    }
    //    beefContext.Database.GetDbConnection().ConnectionString = ConnectionPool[dbName];
    //}
    /// <summary>
    /// 获取实体的数据库连接
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="defaultDbContext"></param>
    /// <returns></returns>
    //public static void GetDbContextConnection<TEntity>(VOLContext defaultDbContext)
    //{
    //    //string connstr= defaultDbContext.Database.GetDbConnection().ConnectionString;
    //    // if (connstr != ConnectionPool[DefaultConnName])
    //    // {
    //    //     defaultDbContext.Database.GetDbConnection().ConnectionString = ConnectionPool[DefaultConnName];
    //    // };
    //}

    public static ISqlDapper SqlDapper
    {
        get
        {
            return new SqlDapper(DefaultConnName);
        }
    }
    public static ISqlDapper GetSqlDapper(string dbName = null)
    {
        return new SqlDapper(dbName ?? DefaultConnName);
    }
    public static ISqlDapper GetSqlDapper<TEntity>()
    {
        //获取实体真实的数据库连接池对象名，如果不存在则用默认数据连接池名
        string dbName = typeof(TEntity).GetTypeCustomValue<DBConnectionAttribute>(x => x.DBName) ?? DefaultConnName;
        return GetSqlDapper(dbName);
    }
    public class DBConnectionAttribute : Attribute
    {
        public string DBName { get; set; }
    }
}
