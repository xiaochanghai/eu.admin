using EU.Core.Common.Caches;
using EU.Core.Common.Enums;
using EU.Core.Common.Helper;
using EU.Core.Model;
using NPOI.Util.ArrayExtensions;

namespace EU.Core.Common.Module;

public class ModuleSql
{
    private string moduleCode;
    private static RedisCacheService Redis = new(2);

    public ModuleSql(string moduleCode)
    {
        this.moduleCode = moduleCode;
    }
    #region 获取模块SQL
    /// <summary>
    /// 获取模块SQL
    /// </summary>
    /// <returns></returns>
    public SmModuleSqlExtend GetModuleSql()
    {
        var module = Redis.Get<SmModuleSqlExtend>(CacheKeys.SmModuleSql.ToString(), moduleCode);
        var cache = new List<SmModuleSqlExtend>();
        if (module == null)
        {
            cache = GetModuleSqlList();
            Redis.Remove(CacheKeys.SmModuleSql.ToString());
            cache.ForEach(item => Redis.AddObject(CacheKeys.SmModuleSql.ToString(), item.ModuleCode, item));
            module = cache.Where(x => x.ModuleCode == moduleCode).FirstOrDefault();
        }
        return module;
    }
    public List<SmModuleSqlExtend> GetModuleSqlList()
    {
        string sql = @"SELECT A.*, B.ModuleCode
                                FROM SmModuleSql A
                                     JOIN SmModules B
                                        ON A.ModuleId = B.ID
                                           AND A.IsDeleted = B.IsDeleted
                                WHERE A.IsDeleted = 'false'";
        return DBHelper.QueryList<SmModuleSqlExtend>(sql);
    }
    #endregion

    #region 获取TableName
    /// <summary>
    /// 获取TableName
    /// </summary>
    /// <returns></returns>
    public string GetTableName()
    {
        try
        {
            string result = string.Empty;
            var ModuleSql = GetModuleSql();
            if (ModuleSql != null)
                result = ModuleSql.TableNames;
            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region 全部表别名
    /// <summary>
    /// 全部表别名
    /// </summary>
    /// <returns></returns>
    public string GetTableAliasName()
    {
        try
        {
            string result = string.Empty;
            var ModuleSql = GetModuleSql();
            if (ModuleSql != null)
                result = ModuleSql.TableAliasNames;
            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region 获取Sql
    /// <summary>
    /// 获取Sql
    /// </summary>
    /// <returns></returns>
    public string GetSqlSelectBrwAndTable()
    {
        try
        {
            return GetSqlSelectBrw() + " FROM " + GetTableNamesAndTableAliasNames();
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region 获取Select语句
    /// <summary>
    /// 获取Select语句
    /// </summary>
    /// <returns></returns>
    public string GetModuleSqlSelect()
    {
        try
        {
            string result = string.Empty;
            var ModuleSql = GetModuleSql();
            if (ModuleSql != null)
                result = ModuleSql.SqlSelect;

            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region 获取首页Select语句
    /// <summary>
    /// 获取首页Select语句
    /// </summary>
    /// <returns></returns>
    public string GetSqlSelectBrw()
    {
        try
        {
            string result = string.Empty;
            var ModuleSql = GetModuleSql();
            if (ModuleSql != null)
                result = ModuleSql.SqlSelectBrw;

            if (string.IsNullOrEmpty(result))
                result = GetModuleSqlSelect();

            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region GetTableNamesAndTableAliasNames
    /// <summary>
    /// GetTableNamesAndTableAliasNames
    /// </summary>
    /// <returns></returns>
    public string GetTableNamesAndTableAliasNames()
    {
        try
        {
            string result = string.Empty;
            string tableNames = GetTableName();
            string tableAliasNames = GetTableAliasName();
            char[] separator = [','];
            string[] tableName = tableNames.Split(separator);
            string[] tableAliasName = tableAliasNames.Split(separator);
            for (int i = 0; i < tableName.Length; i++)
            {
                result += tableName[i] + " " + tableAliasName[i] + ",";
            }
            result = result.Substring(0, result.Length - 1);

            result += " " + GetModuleJoinAll();

            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    public string GetFullSql()
    {
        string result = string.Empty;
        var ModuleSql = GetModuleSql();
        if (ModuleSql != null)
            result = ModuleSql.FullSql;
        return result;
    }

    public string GetModuleJoinAll()
    {
        try
        {
            string result = string.Empty;
            result = null;//db.HashGet(redisKey, "ModuleJoinAll");
            if (string.IsNullOrEmpty(result))
            {
                string[] joinType = GetModuleJoinType();
                string[] joinTable = GetModuleJoinTable();
                string[] joinTableAlias = GetModuleJoinTableAlias();
                string[] joinCondition = GetModuleJoinCondition();
                if (joinType != null)
                {
                    for (int i = 0; i < joinType.Length; i++)
                    {
                        result += joinType[i] + " " + joinTable[i] + " " + joinTableAlias[i] + " ON " + joinCondition[i] + " ";
                    }
                    //result = GetModuleJoinType() + " " + GetModuleJoinTable() + " " + GetModuleJoinTableAlias() + " ON " + GetModuleJoinCondition();

                    if (result.Trim() == "ON")
                        result = string.Empty;
                }
            }
            return result;
        }
        catch (Exception) { throw; }
    }
    public string[] GetModuleJoinType()
    {
        try
        {
            string[] result = null;
            string joinType = string.Empty;

            var ModuleSql = GetModuleSql();
            if (ModuleSql != null)
                joinType = ModuleSql.JoinType;

            if (!string.IsNullOrEmpty(joinType))
            {
                char[] separator = [','];
                result = joinType.Split(separator);
            }
            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public string[] GetModuleJoinTable()
    {
        try
        {
            string[] result = null;
            string joinType = string.Empty;

            var ModuleSql = GetModuleSql();
            if (ModuleSql != null)
                joinType = ModuleSql.SqlJoinTable;

            if (!string.IsNullOrEmpty(joinType))
            {
                char[] separator = [','];
                result = joinType.Split(separator);
            }

            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }
    public string[] GetModuleJoinTableAlias()
    {
        try
        {
            string[] result = null;
            string joinType = string.Empty;

            var ModuleSql = GetModuleSql();
            if (ModuleSql != null)
                joinType = ModuleSql.SqlJoinTableAlias;

            if (!string.IsNullOrEmpty(joinType))
            {
                char[] separator = [','];
                result = joinType.Split(separator);
            }

            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }
    public string[] GetModuleJoinCondition()
    {
        try
        {
            string[] result = null;
            var joinType = string.Empty;

            var ModuleSql = GetModuleSql();
            if (ModuleSql != null)
                joinType = ModuleSql.SqlJoinCondition;

            if (!string.IsNullOrEmpty(joinType))
            {
                result = joinType.Split([',']);
                for (int i = 0; i < result.Length; i++)
                    result[i] = result[i].Replace("，", ",");
            }

            return result;
        }
        catch (Exception) { throw; }
    }

    #region 获取Sql
    /// <summary>
    /// 获取Sql
    /// </summary>
    /// <returns></returns>
    public string GetSqlSelectAndTable()
    {
        try
        {
            return GetModuleSqlSelect() + " FROM " + GetTableNamesAndTableAliasNames();
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region 获取SqlDefaultCondition
    /// <summary>
    /// 获取SqlDefaultCondition
    /// </summary>
    /// <returns></returns>
    public string GetSqlDefaultCondition()
    {
        try
        {
            string result = string.Empty;
            var ModuleSql = GetModuleSql();
            if (ModuleSql != null)
                result = ModuleSql.SqlDefaultCondition;
            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region 获取主表默认排序列名
    /// <summary>
    /// 获取主表默认排序列名
    /// </summary>
    /// <returns></returns>
    public string GetDefaultSortField()
    {
        try
        {
            string result = string.Empty;
            var ModuleSql = GetModuleSql();
            if (ModuleSql != null)
                result = ModuleSql.DefaultSortField;
            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region 获取主表默认排序方向
    /// <summary>
    /// 获取主表默认排序方向
    /// </summary>
    /// <returns></returns>
    public string GetDefaultSortDirection()
    {
        try
        {
            string result = string.Empty;
            var ModuleSql = GetModuleSql();
            if (ModuleSql != null)
                result = ModuleSql.DefaultSortDirection;
            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    public string GetSqlQueryCondition()
    {
        string result = string.Empty;
        var ModuleSql = GetModuleSql();
        if (ModuleSql != null)
            result = ModuleSql.SqlQueryCondition;
        return result;
    }

    public static string GetCountString(string moduleCode, string sqlSelect, string sqlDefaultCondition, string SqlQueryCondition)
    {
        string queryString = sqlSelect + " WHERE 1=1";
        string queryString1 = sqlSelect + " WHERE 1=1";
        int fromIndex = queryString.ToUpper().IndexOf("FROM ");
        queryString = "SELECT COUNT(1) " + queryString.Substring(fromIndex);

        if (!string.IsNullOrEmpty(sqlDefaultCondition))
        {
            queryString += " AND " + sqlDefaultCondition;
            queryString1 += " AND " + sqlDefaultCondition;
        }
        if (!string.IsNullOrEmpty(SqlQueryCondition))
        {
            queryString += " AND " + SqlQueryCondition;
            queryString1 += " AND " + SqlQueryCondition;
        }
        else if (ModuleInfo.GetIsExecQuery(moduleCode) == false)
        {
            queryString += " AND 1<>1";
            queryString1 += " AND 1<>1";
        }
        return queryString;
    }
    public static string GetCountString1(string moduleCode, string sqlSelect, string sqlDefaultCondition, string SqlQueryCondition)
    {
        string queryString = sqlSelect + " ";
        string queryString1 = sqlSelect + " ";
        queryString = "SELECT COUNT(1) FROM (" + queryString + " ";

        if (!string.IsNullOrEmpty(sqlDefaultCondition))
        {
            queryString += " AND " + sqlDefaultCondition;
            queryString1 += " AND " + sqlDefaultCondition;
        }
        if (!string.IsNullOrEmpty(SqlQueryCondition))
        {
            queryString += " AND " + SqlQueryCondition;
            queryString1 += " AND " + SqlQueryCondition;
        }
        else if (ModuleInfo.GetIsExecQuery(moduleCode) == false)
        {
            queryString += " AND 1<>1";
            queryString1 += " AND 1<>1";
        }
        queryString += " ) Z";
        queryString1 += " ) Z";
        //HttpContext.Current.Session[HttpContext.Current.Session.SessionID + moduleCode + "QuerySql"] = queryString1;
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
    /// <returns></returns>
    public string GetCurrentSql(string moduleCode, int pageIndex, int inPageSize, string sort, string order, string defaultCondition, string queryCondition, out int totalCount, out int pageSize, string database = "", params object[] innerCondition)
    {
        string sortField = string.Empty;
        if (!string.IsNullOrEmpty(sort))
        {
            if (!string.IsNullOrEmpty(order))
                sortField = sort + " " + order;
            else
                sortField = sort;
        }
        int _pageSize = inPageSize;
        //计算分页起始索引
        int startIndex = pageIndex > 1 ? (pageIndex - 1) * _pageSize : 0;

        //计算分页结束索引
        int endIndex = pageIndex * _pageSize;

        string TableName = GetTableName();

        string TableAliasName = GetTableAliasName();
        string SqlSelectBrwAndTable = GetSqlSelectBrwAndTable();
        //SqlSelectBrwAndTable = StringHelper.FormatSqlVariable(SqlSelectBrwAndTable);
        string SqlSelectAndTable = GetSqlSelectAndTable();
        //SqlSelectAndTable = StringHelper.FormatSqlVariable(SqlSelectAndTable);

        SqlSelectBrwAndTable = string.Format(SqlSelectBrwAndTable, TableName);
        SqlSelectAndTable = string.Format(SqlSelectAndTable, TableName);

        #region 处理FULL_SQL
        string fullSql = GetFullSql();
        if (!string.IsNullOrEmpty(fullSql))
            SqlSelectBrwAndTable = fullSql;
        #endregion

        string SqlDefaultCondition = GetSqlDefaultCondition();
        //处理系统模块配置中没有配置默认条件时报错的问题
        if (string.IsNullOrEmpty(SqlDefaultCondition))
            SqlDefaultCondition = "1=1";

        //SqlDefaultCondition = StringHelper.FormatSqlVariable(SqlDefaultCondition);
        if (!string.IsNullOrEmpty(defaultCondition))
        {
            if (string.IsNullOrEmpty(SqlDefaultCondition))
                SqlDefaultCondition = defaultCondition;
            else
            {
                if (defaultCondition.Trim().ToUpper().StartsWith("AND") == true)
                    SqlDefaultCondition += defaultCondition;
                else
                    SqlDefaultCondition += " AND " + defaultCondition;
            }
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
        if (string.IsNullOrEmpty(SqlQueryCondition))
        {
            //SqlQueryCondition = HttpContext.Current.Session.GetString(HttpContext.Current.Session.Id + moduleCode + "queryCondition");
        }
        else
        {
            //HttpContext.Current.Session.SetString(HttpContext.Current.Session.Id + moduleCode + "queryCondition", SqlDefaultCondition);
        }
        if (!string.IsNullOrEmpty(SqlQueryCondition))
        {
            SqlQueryCondition = SqlQueryCondition.Trim();
            if (SqlQueryCondition == "1=1")
                SqlQueryCondition = "";
        }
        if (!string.IsNullOrEmpty(SqlQueryCondition))
        {
            //queryString += " AND " + SqlQueryCondition;
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
        //HttpContext.Current.Session[HttpContext.Current.Session.SessionID + moduleCode + "CurrentPageQuerySql"] = queryString;
        int index = queryString.LastIndexOf("WHERE ROWNUM");
        //HttpContext.Current.Session[HttpContext.Current.Session.SessionID + moduleCode + "PagingSql"] = queryString.Substring(0, index);
        //DataTable dt = null;
        //if (database == "first")
        //{
        //    dt = DBHelper.Instance.GetDataTable(queryString, null);
        //}
        //else
        //{
        //    dt = DBHelper.Instance.GetDataTable(queryString, null);
        //}
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
    public static void Init()
    {
        new ModuleSql("").GetModuleSql();
    }

}
