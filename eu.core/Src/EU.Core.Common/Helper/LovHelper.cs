using EU.Core.Common.Caches;
using EU.Core.Common.Enums;
using EU.Core.Model.Models;

namespace EU.Core.Common.Helper;

/// <summary>
/// 参数帮助类
/// </summary>
public class LovHelper
{
    public static RedisCacheService redis = new(3);

    #region 获取值列表
    /// <summary>
    /// 获取值列表
    /// </summary>
    /// <param name="moduleCode">值代码</param>
    /// <returns></returns>
    public static List<LovInfo> GetLovList(string code)
    {
        var cache = redis.Get<List<LovInfo>>(CacheKeys.SmLov.ToString(), code);
        if (cache == null)
        {
            Init();
            cache = redis.Get<List<LovInfo>>(CacheKeys.SmLov.ToString(), code);
        }
        return cache ?? new List<LovInfo>();
    }
    public static async Task<List<LovInfo>> GetLovListAsync(string code)
    {
        var cache = await redis.GetAsync<List<LovInfo>>(CacheKeys.SmLov.ToString(), code);
        if (cache == null)
        {
            await InitAsync();
            cache = await redis.GetAsync<List<LovInfo>>(CacheKeys.SmLov.ToString(), code);
        }
        return cache ?? new List<LovInfo>();
    }

    public static string GetCommonListSql(string code)
    {
        var cache = redis.Get<string>(CacheKeys.CommonListSql.ToString(), code);
        if (cache == null)
        {
            InitCommonListSql();
            cache = redis.Get<string>(CacheKeys.CommonListSql.ToString(), code);
        }
        return cache ?? null;
    }

    #endregion

    /// <summary>
    /// 初始化系统参数
    /// </summary>
    public static void Init()
    {
        redis.Remove(CacheKeys.SmLov.ToString());

        string sql = "SELECT LovCode FROM SmLov WHERE IsDeleted='false'";
        var lov = DBHelper.QueryList<SmLov>(sql);
        var cache = new List<LovInfo>();
        sql = "SELECT [Value], [Text], LovCode FROM SmLovV ORDER BY TaxisNo ASC";
        cache = DBHelper.QueryList<LovInfo>(sql);

        foreach (var item in lov)
        {
            var list = cache.Where(x => x.LovCode == item.LovCode).ToList();
            redis.AddObject(CacheKeys.SmLov.ToString(), item.LovCode, list);
        }
    }
    public static void InitCommonListSql()
    {
        redis.Remove(CacheKeys.CommonListSql.ToString());

        var sql = "SELECT * FROM SmCommonListSql WHERE IsDeleted='false'";
        var listSqls = DBHelper.QueryList<SmCommonListSql>(sql);
        listSqls.ForEach(item => redis.AddObject(CacheKeys.CommonListSql.ToString(), item.CommonCode, item.SelectSql));
    }

    public static async Task InitAsync()
    {

        redis.Remove(CacheKeys.SmLov.ToString());

        string sql = "SELECT LovCode FROM SmLov WHERE IsDeleted='false'";
        var lov = await DBHelper.QueryListAsync<SmLov>(sql);
        var cache = new List<LovInfo>();
        sql = "SELECT [Value], [Text], LovCode FROM SmLovV ORDER BY TaxisNo ASC";
        cache = await DBHelper.QueryListAsync<LovInfo>(sql);

        foreach (var item in lov)
        {
            var list = cache.Where(x => x.LovCode == item.LovCode).ToList();
            await redis.AddObjectAsync(CacheKeys.SmLov.ToString(), item.LovCode, list);
        }
    }
}

public class LovInfo
{
    public string Value { get; set; }

    public string Text { get; set; }
    public string LovCode { get; set; }

}

