using EU.Core.Common.Caches;
using EU.Core.Common.Enums;
using EU.Core.Model.Entity;
using SqlSugar;

namespace EU.Core.Common.Helper;

/// <summary>
/// 值列表帮助类
/// </summary>
public class LovHelper
{
    private static RedisCacheService _redisInstance;
    private static RedisCacheService redis => _redisInstance ??= RedisCacheService.Create(3);
    private static readonly string lovCacheCode = CacheKeys.SmLov.ToString();
    private static readonly string commonListCacheCode = CacheKeys.CommonListSql.ToString();

    #region 获取值列表
    /// <summary>
    /// 获取值列表（使用同步 Redis 操作）
    /// </summary>
    /// <param name="db">数据库实例</param>
    /// <param name="code">值代码</param>
    /// <returns></returns>
    [Obsolete("建议使用 GetLovListAsync 方法以获得更好的异步性能")]
    public static async Task<List<LovInfo>> GetLovList(ISqlSugarClient db, string code)
    {
        var cache = redis.Get<List<LovInfo>>(lovCacheCode, code);
        if (cache == null)
        {
            await Init(db);
            cache = redis.Get<List<LovInfo>>(lovCacheCode, code);
        }
        return cache ?? new List<LovInfo>();
    }

    /// <summary>
    /// 获取值列表（推荐使用，完全异步）
    /// </summary>
    /// <param name="db">数据库实例</param>
    /// <param name="code">值代码</param>
    /// <returns></returns>
    public static async Task<List<LovInfo>> GetLovListAsync(ISqlSugarClient db, string code)
    {
        var cache = await redis.GetAsync<List<LovInfo>>(lovCacheCode, code);
        if (cache == null)
        {
            await Init(db);
            cache = await redis.GetAsync<List<LovInfo>>(lovCacheCode, code);
        }
        return cache ?? new List<LovInfo>();
    }

    /// <summary>
    /// 获取值列表的文本显示
    /// </summary>
    /// <param name="db">数据库实例</param>
    /// <param name="code">值代码</param>
    /// <param name="value">值</param>
    /// <returns>对应的文本，如果未找到则返回原值</returns>
    public static async Task<string> GetLovText(ISqlSugarClient db, string code, string value)
    {
        var list = await GetLovListAsync(db, code);
        return list.FirstOrDefault(x => x.Value == value)?.Text ?? value;
    }

    /// <summary>
    /// 根据代码获取通用列表 SQL
    /// </summary>
    /// <param name="db">数据库实例</param>
    /// <param name="code">通用列表代码</param>
    /// <returns>SQL 语句，如果未找到则返回空字符串</returns>
    public static async Task<string> GetCommonListSql(ISqlSugarClient db, string code)
    {
        var cache = redis.Get<string>(commonListCacheCode, code);
        if (cache == null)
        {
            await InitCommonListSql(db);
            cache = redis.Get<string>(commonListCacheCode, code);
        }
        return cache ?? string.Empty;
    }


    public static async Task<SmCommonListSql> GetCommonListSqlEntity(ISqlSugarClient db, string code)
    {
        var cache = redis.Get<SmCommonListSql>(commonListCacheCode, code + "_Entity");
        if (cache == null)
        {
            await InitCommonListSql(db);
            cache = redis.Get<SmCommonListSql>(commonListCacheCode, code + "_Entity");
        }
        return cache ?? null;
    }

    /// <summary>
    /// 根据 ID 获取通用列表 SQL
    /// </summary>
    /// <param name="db">数据库实例</param>
    /// <param name="commonListSqlId">通用列表 SQL ID</param>
    /// <returns>SQL 语句，如果未找到则返回空字符串</returns>
    public static async Task<string> GetCommonListSql(ISqlSugarClient db, Guid? commonListSqlId)
    {
        var cache = redis.Get<string>(commonListCacheCode, commonListSqlId.ObjToString());
        if (cache == null)
        {
            await InitCommonListSql(db);
            cache = redis.Get<string>(commonListCacheCode, commonListSqlId.ObjToString());
        }
        return cache ?? string.Empty;
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化值列表缓存
    /// </summary>
    /// <param name="db">数据库实例</param>
    public static async Task Init(ISqlSugarClient db)
    {
        redis.Remove(lovCacheCode);

        var lovs = await db.Queryable<SmLov>().ToListAsync();

        var cache = await db.Queryable<LovInfo>().AS("SmLov_V").ToListAsync();
        foreach (var item in lovs)
        {
            var list = cache.Where(x => x.LovCode == item.LovCode).ToList();
            redis.AddObject(lovCacheCode, item.LovCode, list);
        }
    }

    /// <summary>
    /// 初始化通用下拉列表 SQL 缓存
    /// </summary>
    /// <param name="db">数据库实例</param>
    public static async Task InitCommonListSql(ISqlSugarClient db)
    {
        redis.Remove(commonListCacheCode);

        var listSqls = await db.Queryable<SmCommonListSql>().ToListAsync();
        listSqls.ForEach(item =>
        {
            redis.AddObject(commonListCacheCode, item.CommonCode, item.SelectSql);
            redis.AddObject(commonListCacheCode, item.ID.ObjToString(), item.SelectSql);
            redis.AddObject(commonListCacheCode, item.CommonCode + "_Entity", item);
        });
    }

    #endregion
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
    /// 边框变体
    /// </summary>
    public string TagVariant { get; set; }

    /// <summary>
    /// 是否标签显示
    /// </summary>
    public bool? IsTagDisplay { get; set; }
}

