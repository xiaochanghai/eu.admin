using System.Data;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Text;
using EU.Core.Common.Const;
using EU.Core.Common.DB;
using EU.Core.Common.DB.Dapper;
using EU.Core.Common.Enums;

namespace EU.Core.Common.Helper;

/// <summary>
/// DB操作帮助类
/// </summary>
public class DBHelper
{
    public static ISqlDapper Instance
    {
        get
        {
            return DBServerProvider.SqlDapper;
        }
    }


    public static bool MySql
    {
        get
        {
            return DBType.Name == DbCurrentType.MySql.ToString() ? true : false;
        }
    }

    #region 获取SQL插入语句
    /// <summary>
    /// 获取SQL插入语句
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="columnName">列名</param>
    /// <param name="columnValue">列值</param>
    /// <returns>SQL插入语句</returns>
    public StringBuilder GetInsertSql(string tableName, string columnName, string columnValue)
    {
        try
        {
            DbInsert di = null;
            string sql = null;
            var sqls = new StringBuilder();
            var ds = new DbSelect(tableName + " A", "A");
            ds.IsInitDefaultValue = false;
            ds.Where("A." + columnName, "=", columnValue);
            var dt = Instance.GetDataTable(ds.GetSql(), null);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                di = new(tableName, "GetInsertSql");
                di.IsInitDefaultValue = false;
                di.IsInitRowId = false;
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    di.Values(dt.Columns[j].ColumnName, dt.Rows[i][dt.Columns[j].ColumnName].ToString());
                }
                sql = di .GetSql();
                sqls.Append(sql + ";\n");
            }
            return sqls;
        }
        catch (Exception) { throw; }
    }
    /// <summary>
    /// 获取SQL插入语句
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="columnName">列名</param>
    /// <param name="columnValue">列值</param>
    /// <returns>SQL插入语句</returns>
    public StringBuilder GetInsertSql(string tableName, string columnName, Guid columnValue) => GetInsertSql(tableName, columnName, columnValue.ToString());

    #endregion

    #region 数据库名称
    /// <summary>
    /// 数据库名称
    /// </summary>
    public static string DatabaseName
    {
        get
        {
            return Instance.Connection.Database;
        }
    }

    #endregion


    public static DataTable GetDataTable(string sql, object param = null, CommandType? commandType = null, bool beginTransaction = false) => Instance.GetDataTable(sql, param, commandType, beginTransaction);
    public static async Task<DataTable> GetDataTableAsync(string cmd, object param = null, IDbTransaction transaction = null, CommandType? commandType = null, int? commandTimeout = null) => await Instance.GetDataTableAsync(cmd, param, transaction, commandType, commandTimeout);

    public static List<T> QueryList<T>(string cmd, object param = null, CommandType? commandType = null, bool beginTransaction = false) where T : class => Instance.QueryList<T>(cmd, param, commandType, beginTransaction);

    public static async Task<List<T>> QueryListAsync<T>(string cmd, object param = null, IDbTransaction beginTransaction = null, CommandType? commandType = null, int? commandTimeout = null) where T : class => await Instance.QueryListAsync<T>(cmd, param, commandType, beginTransaction, commandTimeout);

    public static T QueryFirst<T>(string cmd, object param = null, CommandType? commandType = null, bool beginTransaction = false) where T : class => Instance.QueryFirst<T>(cmd, param, commandType, beginTransaction);
    public static async Task<T> QueryFirstAsync<T>(string cmd, object param = null, CommandType? commandType = null, IDbTransaction beginTransaction = null, int? commandTimeout = null) where T : class => await Instance.QueryFirstAsync<T>(cmd, param, commandType, beginTransaction, commandTimeout);

    public static object ExecuteScalar(string cmd, object param = null, CommandType? commandType = null, bool beginTransaction = false) => Instance.ExecuteScalar(cmd, param, commandType, beginTransaction);

    public static async Task<object> ExecuteScalarAsync(string cmd, object param = null, CommandType? commandType = null, bool beginTransaction = false) => await Instance.ExecuteScalarAsync(cmd, param, commandType, beginTransaction);
    public static int ExcuteNonQuery(string cmd, object param = null, CommandType? commandType = null, bool beginTransaction = false) => Instance.ExcuteNonQuery(cmd, param, commandType, beginTransaction);
    public static (List<T1>, List<T2>) QueryMultiple<T1, T2>(string cmd, object param = null, CommandType? commandType = null, bool beginTransaction = false) => Instance.QueryMultiple<T1, T2>(cmd, param, commandType, beginTransaction);

    public static (List<T1>, List<T2>, List<T3>) QueryMultiple<T1, T2, T3>(string cmd, object param = null, CommandType? commandType = null, bool beginTransaction = false) => Instance.QueryMultiple<T1, T2, T3>(cmd, param, commandType, beginTransaction);

    public static int ExecuteDML(string cmd, object param = null, CommandType? commandType = null, IDbTransaction dbTransaction = null) => Instance.ExecuteDML(cmd, param, commandType, dbTransaction);

    public static async Task<int> ExecuteDMLAsync(string cmd, object param = null, CommandType? commandType = null, IDbTransaction dbTransaction = null) => await Instance.ExecuteDMLAsync(cmd, param, commandType, dbTransaction);

    public static int Add<T>(T entity, Expression<Func<T, object>> updateFileds = null, bool beginTransaction = false) => Instance.Add(entity, updateFileds, beginTransaction);

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entities"></param>
    /// <param name="updateFileds">指定插入的字段</param>
    /// <param name="beginTransaction">是否开启事务</param>
    /// <returns></returns>
    public static int AddRange<T>(IEnumerable<T> entities, Expression<Func<T, object>> addFileds = null, bool beginTransaction = true) => Instance.AddRange(entities, addFileds, beginTransaction);

    public static int Update<T>(T entity, Expression<Func<T, object>> updateFileds = null, bool beginTransaction = true) => Instance.Update(entity, updateFileds, beginTransaction);

    /// <summary>
    ///(根据主键批量更新实体) sqlserver使用的临时表参数化批量更新，mysql待优化
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entities">实体必须带主键</param>
    /// <param name="updateFileds">批定更新字段</param>
    /// <param name="beginTransaction"></param>
    /// <returns></returns>
    public static int UpdateRange<T>(IEnumerable<T> entities, Expression<Func<T, object>> updateFileds = null, bool beginTransaction = true) => Instance.UpdateRange(entities, updateFileds, beginTransaction);


    public static int BulkInsert(DataTable table, string tableName, SqlBulkCopyOptions? sqlBulkCopyOptions = null, string fileName = null, string tmpPath = null) => Instance.BulkInsert(table, tableName, sqlBulkCopyOptions, fileName, tmpPath);

}