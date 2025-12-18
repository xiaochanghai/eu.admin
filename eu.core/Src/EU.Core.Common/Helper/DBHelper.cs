using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq.Expressions;
using System.Text;
using EU.Core.Common.Const;
using EU.Core.Common.DB;
using EU.Core.Common.DB.Dapper;
using EU.Core.Common.Enums;

namespace EU.Core.Common.Helper;

/// <summary>
/// 数据库操作帮助类
/// 提供对数据库的基础操作封装，包括查询、插入、更新、删除等功能
/// </summary>
public class DBHelper
{
    /// <summary>
    /// 获取SQL Dapper实例
    /// 用于执行数据库操作的核心对象
    /// </summary>
    public static ISqlDapper Instance
    {
        get
        {
            return DBServerProvider.SqlDapper;
        }
    }

    /// <summary>
    /// 判断当前数据库类型是否为MySQL
    /// </summary>
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
                sql = di.GetSql();
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


    /// <summary>
    /// 执行SQL查询并返回DataTable
    /// </summary>
    /// <param name="sql">SQL语句</param>
    /// <param name="param">查询参数</param>
    /// <param name="commandType">命令类型</param>
    /// <param name="beginTransaction">是否开启事务</param>
    /// <returns>返回查询结果DataTable</returns>
    public static DataTable GetDataTable(string sql, object param = null, CommandType? commandType = null, bool beginTransaction = false) => Instance.GetDataTable(sql, param, commandType, beginTransaction);
    
    /// <summary>
    /// 异步执行SQL查询并返回DataTable
    /// </summary>
    /// <param name="cmd">SQL命令</param>
    /// <param name="param">查询参数</param>
    /// <param name="transaction">数据库事务</param>
    /// <param name="commandType">命令类型</param>
    /// <param name="commandTimeout">命令超时时间</param>
    /// <returns>返回查询结果DataTable</returns>
    public static async Task<DataTable> GetDataTableAsync(string cmd, object param = null, IDbTransaction transaction = null, CommandType? commandType = null, int? commandTimeout = null) => await Instance.GetDataTableAsync(cmd, param, transaction, commandType, commandTimeout);

    /// <summary>
    /// 查询并返回实体列表
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="cmd">SQL命令</param>
    /// <param name="param">查询参数</param>
    /// <param name="commandType">命令类型</param>
    /// <param name="beginTransaction">是否开启事务</param>
    /// <returns>返回实体列表</returns>
    public static List<T> QueryList<T>(string cmd, object param = null, CommandType? commandType = null, bool beginTransaction = false) where T : class => Instance.QueryList<T>(cmd, param, commandType, beginTransaction);

    /// <summary>
    /// 异步查询并返回实体列表
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="cmd">SQL命令</param>
    /// <param name="param">查询参数</param>
    /// <param name="beginTransaction">数据库事务</param>
    /// <param name="commandType">命令类型</param>
    /// <param name="commandTimeout">命令超时时间</param>
    /// <returns>返回实体列表</returns>
    public static async Task<List<T>> QueryListAsync<T>(string cmd, object param = null, IDbTransaction beginTransaction = null, CommandType? commandType = null, int? commandTimeout = null) where T : class => await Instance.QueryListAsync<T>(cmd, param, commandType, beginTransaction, commandTimeout);

    /// <summary>
    /// 查询并返回第一个实体对象
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="cmd">SQL命令</param>
    /// <param name="param">查询参数</param>
    /// <param name="commandType">命令类型</param>
    /// <param name="beginTransaction">是否开启事务</param>
    /// <returns>返回第一个实体对象</returns>
    public static T QueryFirst<T>(string cmd, object param = null, CommandType? commandType = null, bool beginTransaction = false) where T : class => Instance.QueryFirst<T>(cmd, param, commandType, beginTransaction);
    
    /// <summary>
    /// 异步查询并返回第一个实体对象
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="cmd">SQL命令</param>
    /// <param name="param">查询参数</param>
    /// <param name="commandType">命令类型</param>
    /// <param name="beginTransaction">数据库事务</param>
    /// <param name="commandTimeout">命令超时时间</param>
    /// <returns>返回第一个实体对象</returns>
    public static async Task<T> QueryFirstAsync<T>(string cmd, object param = null, CommandType? commandType = null, IDbTransaction beginTransaction = null, int? commandTimeout = null) where T : class => await Instance.QueryFirstAsync<T>(cmd, param, commandType, beginTransaction, commandTimeout);

    /// <summary>
    /// 执行SQL查询并返回单个值（第一行第一列）
    /// </summary>
    /// <param name="cmd">SQL命令</param>
    /// <param name="param">查询参数</param>
    /// <param name="commandType">命令类型</param>
    /// <param name="beginTransaction">是否开启事务</param>
    /// <returns>返回单个值</returns>
    public static object ExecuteScalar(string cmd, object param = null, CommandType? commandType = null, bool beginTransaction = false) => Instance.ExecuteScalar(cmd, param, commandType, beginTransaction);

    /// <summary>
    /// 异步执行SQL查询并返回单个值（第一行第一列）
    /// </summary>
    /// <param name="cmd">SQL命令</param>
    /// <param name="param">查询参数</param>
    /// <param name="commandType">命令类型</param>
    /// <param name="beginTransaction">是否开启事务</param>
    /// <returns>返回单个值</returns>
    public static async Task<object> ExecuteScalarAsync(string cmd, object param = null, CommandType? commandType = null, bool beginTransaction = false) => await Instance.ExecuteScalarAsync(cmd, param, commandType, beginTransaction);
    
    /// <summary>
    /// 执行SQL命令（不返回结果集）
    /// </summary>
    /// <param name="cmd">SQL命令</param>
    /// <param name="param">查询参数</param>
    /// <param name="commandType">命令类型</param>
    /// <param name="beginTransaction">是否开启事务</param>
    /// <returns>返回受影响的行数</returns>
    public static int ExecuteNonQuery(string cmd, object param = null, CommandType? commandType = null, bool beginTransaction = false) => Instance.ExecuteNonQuery(cmd, param, commandType, beginTransaction);
    
    /// <summary>
    /// 执行SQL查询并返回两个结果集
    /// </summary>
    /// <typeparam name="T1">第一个结果集的实体类型</typeparam>
    /// <typeparam name="T2">第二个结果集的实体类型</typeparam>
    /// <param name="cmd">SQL命令</param>
    /// <param name="param">查询参数</param>
    /// <param name="commandType">命令类型</param>
    /// <param name="beginTransaction">是否开启事务</param>
    /// <returns>返回两个实体列表的元组</returns>
    public static (List<T1>, List<T2>) QueryMultiple<T1, T2>(string cmd, object param = null, CommandType? commandType = null, bool beginTransaction = false) => Instance.QueryMultiple<T1, T2>(cmd, param, commandType, beginTransaction);

    /// <summary>
    /// 执行SQL查询并返回三个结果集
    /// </summary>
    /// <typeparam name="T1">第一个结果集的实体类型</typeparam>
    /// <typeparam name="T2">第二个结果集的实体类型</typeparam>
    /// <typeparam name="T3">第三个结果集的实体类型</typeparam>
    /// <param name="cmd">SQL命令</param>
    /// <param name="param">查询参数</param>
    /// <param name="commandType">命令类型</param>
    /// <param name="beginTransaction">是否开启事务</param>
    /// <returns>返回三个实体列表的元组</returns>
    public static (List<T1>, List<T2>, List<T3>) QueryMultiple<T1, T2, T3>(string cmd, object param = null, CommandType? commandType = null, bool beginTransaction = false) => Instance.QueryMultiple<T1, T2, T3>(cmd, param, commandType, beginTransaction);

    /// <summary>
    /// 执行DML语句（数据操作语言：INSERT、UPDATE、DELETE）
    /// </summary>
    /// <param name="cmd">SQL命令</param>
    /// <param name="param">查询参数</param>
    /// <param name="commandType">命令类型</param>
    /// <param name="dbTransaction">数据库事务</param>
    /// <returns>返回受影响的行数</returns>
    public static int ExecuteDML(string cmd, object param = null, CommandType? commandType = null, IDbTransaction dbTransaction = null) => Instance.ExecuteDML(cmd, param, commandType, dbTransaction);

    /// <summary>
    /// 异步执行DML语句（数据操作语言：INSERT、UPDATE、DELETE）
    /// </summary>
    /// <param name="cmd">SQL命令</param>
    /// <param name="param">查询参数</param>
    /// <param name="commandType">命令类型</param>
    /// <param name="dbTransaction">数据库事务</param>
    /// <returns>返回受影响的行数</returns>
    public static async Task<int> ExecuteDMLAsync(string cmd, object param = null, CommandType? commandType = null, IDbTransaction dbTransaction = null) => await Instance.ExecuteDMLAsync(cmd, param, commandType, dbTransaction);

    /// <summary>
    /// 添加单个实体到数据库
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entity">要添加的实体</param>
    /// <param name="updateFileds">指定要插入的字段</param>
    /// <param name="beginTransaction">是否开启事务</param>
    /// <returns>返回受影响的行数</returns>
    public static int Add<T>(T entity, Expression<Func<T, object>> updateFileds = null, bool beginTransaction = false) => Instance.Add(entity, updateFileds, beginTransaction);

    /// <summary>
    /// 批量添加实体到数据库
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entities">要添加的实体集合</param>
    /// <param name="addFileds">指定要插入的字段</param>
    /// <param name="beginTransaction">是否开启事务，默认开启</param>
    /// <returns>返回受影响的行数</returns>
    public static int AddRange<T>(IEnumerable<T> entities, Expression<Func<T, object>> addFileds = null, bool beginTransaction = true) => Instance.AddRange(entities, addFileds, beginTransaction);

    /// <summary>
    /// 更新单个实体到数据库
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entity">要更新的实体</param>
    /// <param name="updateFileds">指定要更新的字段</param>
    /// <param name="beginTransaction">是否开启事务，默认开启</param>
    /// <returns>返回受影响的行数</returns>
    public static int Update<T>(T entity, Expression<Func<T, object>> updateFileds = null, bool beginTransaction = true) => Instance.Update(entity, updateFileds, beginTransaction);

    /// <summary>
    /// 根据主键批量更新实体
    /// SQL Server使用临时表参数化批量更新，MySQL待优化
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entities">要更新的实体集合（实体必须带主键）</param>
    /// <param name="updateFileds">指定要更新的字段</param>
    /// <param name="beginTransaction">是否开启事务，默认开启</param>
    /// <returns>返回受影响的行数</returns>
    public static int UpdateRange<T>(IEnumerable<T> entities, Expression<Func<T, object>> updateFileds = null, bool beginTransaction = true) => Instance.UpdateRange(entities, updateFileds, beginTransaction);

    /// <summary>
    /// 批量插入数据（使用SqlBulkCopy）
    /// </summary>
    /// <param name="table">要插入的数据表</param>
    /// <param name="tableName">目标表名</param>
    /// <param name="sqlBulkCopyOptions">批量复制选项</param>
    /// <param name="fileName">文件名（用于MySQL导入）</param>
    /// <param name="tmpPath">临时文件路径（用于MySQL导入）</param>
    /// <returns>返回受影响的行数</returns>
    public static int BulkInsert(DataTable table, string tableName, SqlBulkCopyOptions? sqlBulkCopyOptions = null, string fileName = null, string tmpPath = null) => Instance.BulkInsert(table, tableName, sqlBulkCopyOptions, fileName, tmpPath);
    
    /// <summary>
    /// 异步批量插入数据（使用SqlBulkCopy）
    /// </summary>
    /// <param name="table">要插入的数据表</param>
    /// <param name="tableName">目标表名</param>
    /// <param name="sqlBulkCopyOptions">批量复制选项</param>
    /// <param name="fileName">文件名（用于MySQL导入）</param>
    /// <param name="tmpPath">临时文件路径（用于MySQL导入）</param>
    /// <returns>返回受影响的行数</returns>
    public static async Task<int> BulkInsertAsync(DataTable table, string tableName, SqlBulkCopyOptions? sqlBulkCopyOptions = null, string fileName = null, string tmpPath = null) => await Instance.BulkInsertAsync(table, tableName, sqlBulkCopyOptions, fileName, tmpPath);

}