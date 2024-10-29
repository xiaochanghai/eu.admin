using EU.Core.Common.Helper;

namespace EU.Core.Common.Module;

public class GridList
{
    public string SqlSelect;
    public string SqlDefaultCondition;
    public string SqlQueryCondition;
    public int? CurrentPage { get; set; }
    public int? PageSize { get; set; }
    public string SortField { get; set; }
    public string SortDirection { get; set; }
    public string ModuleCode { get; set; }
    public string FullSql { get; set; }
    public string GetQueryString()
    {
        string queryString = string.Empty;

        if (string.IsNullOrEmpty(SortField))
        {
            if (string.IsNullOrEmpty(FullSql))
                queryString = "SELECT * FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY ROW_ID) NUM FROM (SELECT * FROM (" + SqlSelect + " WHERE 1=1 ";
            else
                queryString = "SELECT * FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY ROW_ID) NUM FROM (SELECT * FROM (" + FullSql + " ";
        }
        else
        {
            if (!string.IsNullOrEmpty(SortDirection))
            {
                if (string.IsNullOrEmpty(FullSql))
                    queryString = "SELECT * FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY " + SortField + " " + SortDirection + ") NUM FROM (SELECT * FROM (" + SqlSelect + " WHERE 1=1 ";
                else
                    queryString = "SELECT * FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY " + SortField + " " + SortDirection + ") NUM FROM (SELECT * FROM (" + FullSql + " ";
            }
            else
            {
                if (string.IsNullOrEmpty(FullSql))
                    queryString = "SELECT * FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY " + SortField + " DESC) NUM FROM (SELECT * FROM (" + SqlSelect + " WHERE 1=1 ";
                else
                    queryString = "SELECT * FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY " + SortField + " DESC) NUM FROM (SELECT * FROM (" + FullSql + " ";
            }
        }
        if (!string.IsNullOrEmpty(SqlDefaultCondition))
            queryString += " AND " + SqlDefaultCondition;

        if (!string.IsNullOrEmpty(SqlQueryCondition))
            queryString += " AND " + SqlQueryCondition;

        queryString += ") A ) B ) C";
        if (CurrentPage != null && PageSize != null)
            queryString += " WHERE NUM <= " + CurrentPage * PageSize + " AND NUM >" + (CurrentPage - 1) * PageSize;
        return queryString;
    }

    /// <summary>
    /// 求总记录数查询SQl语句 
    /// </summary>
    /// <param name="sqlDefaultCondition"></param>
    /// <param name="SqlQueryCondition"></param>
    /// <returns></returns>
    public string GetCountString()
    {
        string queryString = SqlSelect + " where 1=1";
        int fromIndex = queryString.ToUpper().LastIndexOf(" FROM ");
        queryString = "SELECT COUNT(1) " + queryString.Substring(fromIndex);

        if (!string.IsNullOrEmpty(SqlDefaultCondition))
            queryString += " and " + SqlDefaultCondition;

        if (!string.IsNullOrEmpty(SqlQueryCondition))
            queryString += " and " + SqlQueryCondition;

        return queryString;
    }

    /// <summary>
    /// 求总记录数
    /// </summary>
    /// <returns></returns>
    public int GetTotalCount(params object[] innerCondition)
    {
        try
        {
            string sql;
            if (string.IsNullOrEmpty(FullSql))
                sql = GetCountString(ModuleCode, SqlSelect, SqlDefaultCondition, SqlQueryCondition);
            else
            {
                sql = "SELECT COUNT(0) FROM ( " + FullSql + " ) A WHERE 1=1";
       
                if (!string.IsNullOrEmpty(SqlQueryCondition))
                    sql += " and " + SqlQueryCondition;

                else if (ModuleInfo.GetIsExecQuery(ModuleCode) == false)
                    sql += " AND 1<>1";

            }
            sql = string.Format(sql, innerCondition);
            int count = Convert.ToInt32(DBHelper.ExecuteScalar(sql));
            return count;
        }
        catch (Exception)
        {
            //Logger.WriteLog("GridList", sql);
            //Logger.WriteLog("GridList", Ex.StackTrace);
            //Logger.WriteLog("GridList", Ex.Message);
            throw;
        }
    }

    public string GetCountString(string moduleCode, string sqlSelect, string sqlDefaultCondition, string SqlQueryCondition)
    {
        string queryString = sqlSelect + " where 1=1";
        string queryString1 = sqlSelect + " where 1=1";
        int fromIndex = queryString.ToUpper().IndexOf("FROM ");
        queryString = "SELECT COUNT(1) " + queryString.Substring(fromIndex);

        if (!string.IsNullOrEmpty(sqlDefaultCondition))
        {
            queryString += " and " + sqlDefaultCondition;
            queryString1 += " and " + sqlDefaultCondition;
        }
        if (!string.IsNullOrEmpty(SqlQueryCondition))
        {
            queryString += " and " + SqlQueryCondition;
            queryString1 += " and " + SqlQueryCondition;
        }
        else if (ModuleInfo.GetIsExecQuery(moduleCode) == false)
        {
            queryString += " AND 1<>1";
            queryString1 += " AND 1<>1";
        }
        return queryString;
    }
    public string GetCountString1(string moduleCode, string sqlSelect, string sqlDefaultCondition, string SqlQueryCondition)
    {
        string queryString = sqlSelect + " ";
        string queryString1 = sqlSelect + " ";
        queryString = "SELECT COUNT(1) FROM (" + queryString + " ";

        if (!string.IsNullOrEmpty(sqlDefaultCondition))
        {
            queryString += " and " + sqlDefaultCondition;
            queryString1 += " and " + sqlDefaultCondition;
        }
        if (!string.IsNullOrEmpty(SqlQueryCondition))
        {
            queryString += " and " + SqlQueryCondition;
            queryString1 += " and " + SqlQueryCondition;
        }
        else if (ModuleInfo.GetIsExecQuery(moduleCode) == false)
        {
            queryString += " AND 1<>1";
            queryString1 += " AND 1<>1";
        }
        queryString += " ) Z";
        queryString1 += " ) Z";
        return queryString;
    }
}
