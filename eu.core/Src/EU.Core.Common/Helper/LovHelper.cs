using Dapper;
using EU.Core.Common.Caches;
using EU.Core.Common.Enums;
using EU.Core.Model.Entity;
using SqlSugar;
using System.Threading.Tasks;

namespace EU.Core.Common.Helper;

/// <summary>
/// 参数帮助类
/// </summary>
public class LovHelper
{
    public static RedisCacheService redis = new(3);
    public static string lovCacheCode = CacheKeys.SmLov.ToString();
    public static string commonListCacheCode = CacheKeys.CommonListSql.ToString();

    #region 获取值列表
    /// <summary>
    /// 获取值列表
    /// </summary>
    /// <param name="moduleCode">值代码</param>
    /// <returns></returns>
    public static async Task<List<LovInfo>> GetLovList(ISqlSugarClient _Db, string code)
    {
        var cache = redis.Get<List<LovInfo>>(lovCacheCode, code);
        if (cache == null)
        {
            await Init(_Db);
            cache = redis.Get<List<LovInfo>>(lovCacheCode, code);
        }
        return cache ?? new List<LovInfo>();
    }

    /// <summary>
    /// 获取值列表
    /// </summary>
    /// <param name="moduleCode">值代码</param>
    /// <returns></returns>
    public static async Task<List<LovInfo>> GetLovListAsync(ISqlSugarClient _Db, string code)
    {
        var cache = await redis.GetAsync<List<LovInfo>>(lovCacheCode, code);
        if (cache == null)
        {
            await Init(_Db);
            cache = await redis.GetAsync<List<LovInfo>>(lovCacheCode, code);
        }
        return cache ?? new List<LovInfo>();
    }

    /// <summary>
    /// 获取值列表
    /// </summary>
    /// <param name="moduleCode">值代码</param>
    /// <returns></returns>
    public static async Task<string> GetLovText(ISqlSugarClient _Db, string code, string value)
    {
        var list = await GetLovListAsync(_Db, code);
        if (!list.Any())
            return value;
        return list.Where(x => x.Value == value).Select(x => x.Text).First() ?? value;
    }

    public static async Task<string> GetCommonListSql(ISqlSugarClient _Db, string code)
    {
        var cache = redis.Get<string>(commonListCacheCode, code);
        if (cache == null)
        {
            await InitCommonListSql(_Db);
            cache = redis.Get<string>(commonListCacheCode, code);
        }
        return cache ?? null;
    }

    public static async Task<string> GetCommonListSql(ISqlSugarClient _Db, Guid? commonListSqlId)
    {
        var cache = redis.Get<string>(commonListCacheCode, commonListSqlId.ObjToString());
        if (cache == null)
        {
            await InitCommonListSql(_Db);
            cache = redis.Get<string>(commonListCacheCode, commonListSqlId.ObjToString());
        }
        return cache ?? "";
    }

    #endregion

    /// <summary>
    /// 初始化系统参数
    /// </summary>
    public static async Task Init(ISqlSugarClient _Db)
    {
        redis.Remove(lovCacheCode);

        var lovs = await _Db.Queryable<SmLov>().ToListAsync();

        var cache = await _Db.Queryable<LovInfo>().AS("SmLov_V").ToListAsync();
        foreach (var item in lovs)
        {
            var list = cache.Where(x => x.LovCode == item.LovCode).ToList();
            redis.AddObject(lovCacheCode, item.LovCode, list);
        }
    }
    /// <summary>
    /// 初始化通用下拉
    /// </summary>
    public static async Task InitCommonListSql(ISqlSugarClient _Db)
    {
        redis.Remove(commonListCacheCode);

        var listSqls = await _Db.Queryable<SmCommonListSql>().ToListAsync();
        listSqls.ForEach(item =>
        {
            redis.AddObject(commonListCacheCode, item.CommonCode, item.SelectSql);
            redis.AddObject(commonListCacheCode, item.ID.ObjToString(), item.SelectSql);
        });
    }
}

/// <summary>
/// 字典
/// </summary>
public class LovInfo
{
    /// <summary>
    /// 值
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// 参数
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// 字典代码
    /// </summary>
    public string LovCode { get; set; }

    /// <summary>
    /// 标签颜色
    /// </summary>
    public string TagColor { get; set; }

    /// <summary>
    /// 标签图标
    /// </summary>
    public string TagIcon { get; set; }

    /// <summary>
    /// 边框显示
    /// </summary>
    public bool? TagBordered { get; set; }

    /// <summary>
    /// 是否标签显示
    /// </summary>
    public bool? IsTagDisplay { get; set; }
}

