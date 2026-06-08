using EU.Core.Common.Caches;
using EU.Core.Common.Enums;
using EU.Core.Model.Entity;
using SqlSugar;

namespace EU.Core.Common.Helper;

/// <summary>
/// 值列表帮助类
/// </summary>
public class LanguageHelper
{
    private static RedisCacheService _redisInstance;
    private static RedisCacheService redis => _redisInstance ??= RedisCacheService.Create(3);
    private static readonly string languageConfigCode = CacheKeys.SmLanguageConfig.ToString();

    #region 获取值列表

    /// <summary>
    /// 获取值列表（推荐使用，完全异步）
    /// </summary>
    /// <param name="db">数据库实例</param>
    /// <param name="code">值代码</param>
    /// <returns></returns>
    public static async Task<List<LanguageConfigInfo>> GetListAsync(ISqlSugarClient db, string refType, Guid? refId)
    {
        var code = refId.ObjToString() + "_" + refType;
        var cache = await redis.GetAsync<List<LanguageConfigInfo>>(languageConfigCode, code);
        return cache ?? new List<LanguageConfigInfo>();
    }

    /// <summary>
    /// 获取值列表的文本显示
    /// </summary>
    /// <param name="db">数据库实例</param>
    /// <param name="code">值代码</param>
    /// <param name="value">值</param>
    /// <returns>对应的文本，如果未找到则返回原值</returns>
    public static async Task<LanguageConfigInfo> Get(ISqlSugarClient db, string refType, Guid? refId, string refField)
    {
        var list = await GetListAsync(db, refType, refId);
        return list.FirstOrDefault(x => x.RefField == refField) ?? null;
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化值列表缓存
    /// </summary>
    /// <param name="db">数据库实例</param>
    public static async Task Init(ISqlSugarClient db)
    {
        redis.Remove(languageConfigCode);

        var allData = await db.Queryable<SmLanguageConfig>().ToListAsync();

        // 第三步：在内存中映射（如果数据量不大）
        var result = allData.Select(stat => new
        {
            RefId = stat.RefId.ObjToString(),
            stat.RefType,
            Items = allData
            .Where(d => d.RefId == stat.RefId && d.RefType == stat.RefType)
            .Select(x => new LanguageConfigInfo
            {
                RefField = x.RefField,
                Value_ZH = x.Value_ZH,
                Value_EN = x.Value_EN
            }).ToList()
        }).ToList();

        foreach (var item in result)
        {
            redis.AddObject(languageConfigCode, item.RefId + "_" + item.RefType, item.Items);
        }
    }


    #endregion
}

/// <summary>
/// 字典
/// </summary>
public class LanguageConfigInfo
{
    /// <summary>
    /// 值
    /// </summary>
    public string RefField { get; set; }

    /// <summary>
    /// 参数
    /// </summary>
    public string Value_ZH { get; set; }

    /// <summary>
    /// 字典代码
    /// </summary>
    public string Value_EN { get; set; }
}
