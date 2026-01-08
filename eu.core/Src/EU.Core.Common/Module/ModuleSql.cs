using Dm.util;
using EU.Core.Common.Caches;
using EU.Core.Common.Enums;
using EU.Core.Common.Helper;
using EU.Core.Model;
using EU.Core.Model.Entity;
using SqlSugar;

namespace EU.Core.Common.Module;

public class ModuleSql
{
    private ISqlSugarClient Db;
    private readonly string moduleCode;
    private static RedisCacheService _redisInstance;
    private static RedisCacheService Redis => _redisInstance ??= RedisCacheService.Create(2);

    private readonly string key = CacheKeys.SmModuleSql.ToString();

    public ModuleSql(string moduleCode, ISqlSugarClient _Db)
    {
        this.moduleCode = moduleCode;
        Db = _Db;
    }

    #region 获取模块SQL
    /// <summary>
    /// 获取模块SQL
    /// </summary>
    public SmModuleSqlExtend GetModuleSql()
    {
        var module = Redis.Get<SmModuleSqlExtend>(key, moduleCode);
        if (module == null)
        {
            var cache = GetModuleSqlList();
            Redis.Remove(key);
            cache.ForEach(item => Redis.AddObject(key, item.ModuleCode, item));
            module = cache.FirstOrDefault(x => x.ModuleCode == moduleCode);
        }
        return module;
    }

    public List<SmModuleSqlExtend> GetModuleSqlList()
    {
        var cache = Db.Queryable<SmModuleSql, SmModules>((a, b) =>
            new object[]
            {
                JoinType.Inner, a.ModuleId == b.ID
            })
             .Select((a, b) => new { a, b.ModuleCode })
             .Select<SmModuleSqlExtend>()
             .ToList();
        return cache;
    }
    #endregion

    #region 获取TableName
    public string GetTableName()
    {
        return GetModuleSql()?.TableNames ?? string.Empty;
    }
    #endregion

    #region 全部表别名
    public string GetTableAliasName()
    {
        return GetModuleSql()?.TableAliasNames ?? string.Empty;
    }
    #endregion

    #region 获取Sql
    public string GetSqlSelectBrwAndTable()
    {
        var result = $"{GetSqlSelectBrw()} FROM {GetTableNamesAndTableAliasNames()}";

        if (result.IsNotEmptyOrNull() && Db != null)
            if (Db.Ado.Context.CurrentConnectionConfig.DbType == DbType.MySql)
                result = result.replace("[Text]", "`Text`").replace("[Value]", "`Value`");
        return result;
    }
    #endregion

    #region 获取Select语句
    public string GetModuleSqlSelect()
    {
        var result = GetModuleSql()?.SqlSelect ?? string.Empty;

        if (string.IsNullOrEmpty(result))
            result = GetModuleSqlSelect();
        return result;
    }
    #endregion

    #region 获取首页Select语句
    public string GetSqlSelectBrw()
    {
        var moduleSql = GetModuleSql();
        var result = moduleSql?.SqlSelectBrw ?? string.Empty;

        if (string.IsNullOrEmpty(result))
            result = GetModuleSqlSelect();

        return result;
    }
    #endregion

    #region GetTableNamesAndTableAliasNames
    public string GetTableNamesAndTableAliasNames()
    {
        var tableNames = SplitCsv(GetTableName());
        var tableAliasNames = SplitCsv(GetTableAliasName());

        if (tableNames == null || tableAliasNames == null || tableNames.Length == 0 || tableNames.Length != tableAliasNames.Length)
            return string.Empty;

        var mainTables = tableNames.Zip(tableAliasNames, (name, alias) => $"{name} {alias}");
        var result = string.Join(",", mainTables);

        var moduleJoinAll = GetModuleJoinAll();
        if (!string.IsNullOrWhiteSpace(moduleJoinAll))
            result += " " + moduleJoinAll;

        return result;
    }
    #endregion

    public string GetFullSql()
    {
        return GetModuleSql()?.FullSql ?? string.Empty;
    }

    public string GetModuleJoinAll()
    {
        var joinType = GetModuleJoinType();
        var joinTable = GetModuleJoinTable();
        var joinTableAlias = GetModuleJoinTableAlias();
        var joinCondition = GetModuleJoinCondition();

        if (joinType == null || joinTable == null || joinTableAlias == null || joinCondition == null)
            return string.Empty;

        bool lengthMismatch = joinType.Length != joinTable.Length || joinType.Length != joinTableAlias.Length || joinType.Length != joinCondition.Length;
        if (joinType.Length == 0 || lengthMismatch)
            return string.Empty;

        var joins = Enumerable.Range(0, joinType.Length)
                              .Select(i => $"{joinType[i]} {joinTable[i]} {joinTableAlias[i]} ON {joinCondition[i]}");

        var result = string.Join(" ", joins);
        return result.Trim() == "ON" ? string.Empty : result;
    }

    public string[]? GetModuleJoinType()
    {
        return SplitCsv(GetModuleSql()?.JoinType);
    }

    public string[]? GetModuleJoinTable()
    {
        return SplitCsv(GetModuleSql()?.SqlJoinTable);
    }

    public string[]? GetModuleJoinTableAlias()
    {
        return SplitCsv(GetModuleSql()?.SqlJoinTableAlias);
    }

    public string[]? GetModuleJoinCondition()
    {
        var joinType = GetModuleSql()?.SqlJoinCondition;

        if (string.IsNullOrWhiteSpace(joinType))
            return null;

        var result = joinType.Split(',', StringSplitOptions.RemoveEmptyEntries)
                             .Select(v => v.Replace("，", ",").Trim())
                             .Where(v => !string.IsNullOrEmpty(v))
                             .ToArray();

        return result.Length == 0 ? null : result;
    }

    #region 获取Sql
    public string GetSqlSelectAndTable()
    {
        return $"{GetModuleSqlSelect()} FROM {GetTableNamesAndTableAliasNames()}";
    }
    #endregion

    #region 获取SqlDefaultCondition
    public string GetSqlDefaultCondition()
    {
        string sqlDefaultCondition = GetModuleSql()?.SqlDefaultCondition ?? string.Empty;
        if (sqlDefaultCondition.IsNotEmptyOrNull() && Db != null)
            if (Db.Ado.Context.CurrentConnectionConfig.DbType == DbType.MySql)
                sqlDefaultCondition = sqlDefaultCondition.replace("'true'", "'1'").replace("'false'", "'0'");
        return sqlDefaultCondition;
    }
    #endregion

    #region 获取主表默认排序列名
    public string GetDefaultSortField()
    {
        return GetModuleSql()?.DefaultSortField ?? string.Empty;
    }
    #endregion

    #region 获取主表默认排序方向
    public string GetDefaultSortDirection()
    {
        return GetModuleSql()?.DefaultSortDirection ?? string.Empty;
    }
    #endregion

    public string GetSqlQueryCondition()
    {
        return GetModuleSql()?.SqlQueryCondition ?? string.Empty;
    }

    public static string GetCountString(string moduleCode, string sqlSelect, string sqlDefaultCondition, string SqlQueryCondition)
    {
        string queryString = sqlSelect + " WHERE 1=1";
        int fromIndex = queryString.ToUpper().IndexOf("FROM ");
        queryString = "SELECT COUNT(1) " + queryString.Substring(fromIndex);

        if (!string.IsNullOrEmpty(sqlDefaultCondition))
            queryString += " AND " + sqlDefaultCondition;

        if (!string.IsNullOrEmpty(SqlQueryCondition))
            queryString += " AND " + SqlQueryCondition;
        else if (ModuleInfo.GetIsExecQuery(moduleCode) == false)
            queryString += " AND 1<>1";

        return queryString;
    }

    public static string GetCountString1(string moduleCode, string sqlSelect, string sqlDefaultCondition, string SqlQueryCondition)
    {
        string queryString = "SELECT COUNT(1) FROM (" + sqlSelect + " ";

        if (!string.IsNullOrEmpty(sqlDefaultCondition))
            queryString += " AND " + sqlDefaultCondition;

        if (!string.IsNullOrEmpty(SqlQueryCondition))
            queryString += " AND " + SqlQueryCondition;
        else if (ModuleInfo.GetIsExecQuery(moduleCode) == false)
            queryString += " AND 1<>1";

        queryString += " ) Z";
        return queryString;
    }

    #region 获取当前查询SQL
    /// <summary>
    /// 获取当前查询SQL
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="pageIndex">起始位置</param>
    /// <param name="inPageSize">每页数量</param>
    /// <param name="sort">排序字段</param>
    /// <param name="order">正序 or 倒序</param>
    /// <param name="defaultCondition">默认条件</param>
    /// <param name="queryCondition">查询条件</param>
    /// <param name="totalCount">总数据条数</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="database">库</param>
    /// <param name="innerCondition">内条件</param>
    public string GetCurrentSql(string moduleCode, int pageIndex, int inPageSize, string sort, string order, string defaultCondition, string queryCondition, out int totalCount, out int pageSize, string database = "", params object[] innerCondition)
    {
        string sortField = string.Empty;
        if (!string.IsNullOrEmpty(sort))
            sortField = string.IsNullOrEmpty(order) ? sort : $"{sort} {order}";

        int _pageSize = inPageSize;
        int startIndex = pageIndex > 1 ? (pageIndex - 1) * _pageSize : 0;
        int endIndex = pageIndex * _pageSize;

        string TableName = GetTableName();
        string TableAliasName = GetTableAliasName();
        string SqlSelectBrwAndTable = GetSqlSelectBrwAndTable();
        string SqlSelectAndTable = GetSqlSelectAndTable();

        SqlSelectBrwAndTable = string.Format(SqlSelectBrwAndTable, TableName);
        SqlSelectAndTable = string.Format(SqlSelectAndTable, TableName);

        #region 处理FULL_SQL
        string fullSql = GetFullSql();
        if (!string.IsNullOrEmpty(fullSql))
            SqlSelectBrwAndTable = fullSql;
        #endregion

        string SqlDefaultCondition = GetSqlDefaultCondition();
        if (string.IsNullOrEmpty(SqlDefaultCondition))
            SqlDefaultCondition = "1=1";

        if (!string.IsNullOrEmpty(defaultCondition))
        {
            if (string.IsNullOrEmpty(SqlDefaultCondition))
                SqlDefaultCondition = defaultCondition;
            else
                SqlDefaultCondition += defaultCondition.Trim().ToUpper().StartsWith("AND") ? defaultCondition : " AND " + defaultCondition;
        }

        string DefaultSortField = GetDefaultSortField();
        string DefaultSortDirection = GetDefaultSortDirection();

        #region  初始查询条件
        string sqlQueryCondition = GetSqlQueryCondition();
        if (string.IsNullOrEmpty(queryCondition))
        {
            if (!string.IsNullOrEmpty(sqlQueryCondition))
                queryCondition = sqlQueryCondition;
        }
        else
            sqlQueryCondition = string.Empty;
        #endregion

        if (string.IsNullOrEmpty(DefaultSortDirection))
            DefaultSortDirection = "ASC";

        string sql = string.Empty;
        string queryString = string.Empty;
        if (string.IsNullOrEmpty(sortField))
        {
            if (string.IsNullOrEmpty(DefaultSortField))
            {
                if (string.IsNullOrEmpty(fullSql))
                {
                    if (DBHelper.MySql)
                        queryString = "SELECT * FROM (SELECT *,(@row_number := @row_number + 1) ROWNUM FROM (SELECT * FROM (" + SqlSelectBrwAndTable + " WHERE 1=1 [SqlDefaultCondition] [SqlQueryCondition]";
                    else
                        queryString = "SELECT * FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY ROW_ID) ROWNUM FROM (SELECT * FROM (" + SqlSelectBrwAndTable + " WHERE 1=1 ";
                }
                else
                {
                    if (DBHelper.MySql)
                        queryString = "SELECT * FROM (SELECT *,(@row_number := @row_number + 1) ROWNUM FROM (SELECT * FROM (" + SqlSelectBrwAndTable + " ";
                    else
                        queryString = "SELECT * FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY ROW_ID) ROWNUM FROM (SELECT * FROM (" + SqlSelectBrwAndTable + " ";
                }
            }
            else
            {
                if (string.IsNullOrEmpty(fullSql))
                {
                    if (DBHelper.MySql)
                        queryString = "SELECT * FROM (SELECT *,(@row_number := @row_number + 1) ROWNUM FROM (SELECT * FROM (" + SqlSelectBrwAndTable + " WHERE 1=1 [SqlDefaultCondition] [SqlQueryCondition] ORDER BY {2} {3}";
                    else
                        queryString = "SELECT * FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY {2} {3}) ROWNUM FROM (SELECT * FROM (" + SqlSelectBrwAndTable + " WHERE 1=1 ";
                }
                else
                {
                    if (DBHelper.MySql)
                        queryString = "SELECT * FROM (SELECT *, (@row_number := @row_number + 1) ROWNUM FROM (SELECT * FROM (" + SqlSelectBrwAndTable + " ORDER BY {2} {3}";
                    else
                        queryString = "SELECT * FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY {2} {3}) ROWNUM FROM (SELECT * FROM (" + SqlSelectBrwAndTable + " ";
                }
            }
        }
        else
        {
            if (string.IsNullOrEmpty(fullSql))
            {
                if (DBHelper.MySql)
                    queryString = "SELECT * FROM (SELECT *, (@row_number := @row_number + 1) ROWNUM FROM (SELECT * FROM (" + SqlSelectBrwAndTable + " WHERE 1=1 [SqlDefaultCondition] [SqlQueryCondition] ORDER BY {2} ";
                else
                    queryString = "SELECT * FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY {2}) ROWNUM FROM (SELECT * FROM (" + SqlSelectBrwAndTable + " WHERE 1=1 ";
            }
            else
            {
                if (DBHelper.MySql)
                    queryString = "SELECT * FROM (SELECT *,(@row_number := @row_number + 1) ROWNUM FROM (SELECT * FROM (" + SqlSelectBrwAndTable + " ORDER BY {2} ";
                else
                    queryString = "SELECT * FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY {2}) ROWNUM FROM (SELECT * FROM (" + SqlSelectBrwAndTable + " ";
            }
        }
        if (!string.IsNullOrEmpty(SqlDefaultCondition))
        {
            if (DBHelper.MySql)
                queryString = queryString.Replace("[SqlDefaultCondition]", " AND " + SqlDefaultCondition);
            else
                queryString += " AND " + SqlDefaultCondition;
        }
        string SqlQueryCondition = queryCondition;
        if (!string.IsNullOrEmpty(SqlQueryCondition))
        {
            SqlQueryCondition = SqlQueryCondition.Trim();
            if (SqlQueryCondition == "1=1")
                SqlQueryCondition = "";
        }
        if (!string.IsNullOrEmpty(SqlQueryCondition))
        {
            if (DBHelper.MySql)
                queryString = queryString.Replace("[SqlQueryCondition]", " AND " + SqlQueryCondition);
            else
                queryString += " AND " + SqlQueryCondition;
        }
        else if (ModuleInfo.GetIsExecQuery(moduleCode) == false)
        {
            if (DBHelper.MySql)
                queryString = queryString.Replace("[SqlQueryCondition]", " AND 1<>1");
            else
                queryString += " AND 1<>1";
        }
        else
        {
            if (DBHelper.MySql)
                queryString = queryString.Replace("[SqlQueryCondition]", "");
        }
        if (DBHelper.MySql)
            queryString += ") A ) B,(SELECT @row_number:= 0) AS t) C";
        else
            queryString += ") A ) B ) C";

        queryString += " WHERE ROWNUM <= {0} AND ROWNUM > {1}";
        if (string.IsNullOrEmpty(sortField))
        {
            if (string.IsNullOrEmpty(DefaultSortField))
                queryString = string.Format(queryString, endIndex.ToString(), startIndex.ToString());
            else
                queryString = string.Format(queryString, endIndex.ToString(), startIndex.ToString(), DefaultSortField, DefaultSortDirection);
        }
        else
            queryString = string.Format(queryString, endIndex.ToString(), startIndex.ToString(), sortField);

        queryString = string.Format(queryString, innerCondition);

        string countString = string.Empty;
        if (string.IsNullOrEmpty(fullSql))
            countString = GetCountString(moduleCode, SqlSelectAndTable, SqlDefaultCondition, SqlQueryCondition);
        else
            countString = GetCountString1(moduleCode, SqlSelectBrwAndTable, SqlDefaultCondition, SqlQueryCondition);

        countString = string.Format(countString, innerCondition);
        countString = string.Format(countString, innerCondition);
        if (database == "first")
            totalCount = Convert.ToInt32(DBHelper.ExecuteScalar(countString));
        else
            totalCount = Convert.ToInt32(DBHelper.ExecuteScalar(countString));

        pageSize = _pageSize;
        return queryString;
    }

    #endregion

    /// <summary>
    /// 初始化
    /// </summary>
    public static void Init(ISqlSugarClient _Db)
    {
        new ModuleSql("", _Db).GetModuleSql();
    }

    private static string[]? SplitCsv(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => v.Trim())
                    .ToArray();
    }
}
