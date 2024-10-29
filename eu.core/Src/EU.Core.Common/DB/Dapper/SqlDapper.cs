using Dapper;
using EU.Core.Common.Const;
using EU.Core.Common.Enums;
using EU.Core.Common.Extensions;
using EU.Core.Common.Helper;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Text;

namespace EU.Core.Common.DB.Dapper;

public class SqlDapper : ISqlDapper
{
    private string _connectionString;
    private IDbConnection _connection { get; set; }
    public IDbConnection Connection
    {
        get
        {
            if (_connection == null || _connection.State == ConnectionState.Closed)
                _connection = DBServerProvider.GetDbConnection(_connectionString);
            return _connection;
        }
    }
    public SqlDapper()
    {
        _connectionString = DBServerProvider.GetConnectionString();
    }
    /// <summary>
    ///      string mySql = "Data Source=132.232.2.109;Database=mysql;User 
    ///      ID=root;Password=mysql;pooling=true;CharSet=utf8;port=3306;sslmode=none";
    ///  this.conn = new MySql.Data.MySqlClient.MySqlConnection(mySql);
    /// </summary>
    /// <param name="connKeyName"></param>
    public SqlDapper(string connKeyName)
    {
        _connectionString = DBServerProvider.GetConnectionString(connKeyName);
    }

    /// <summary>
    /// var p = new object();
    //        p.Add("@a", 11);
    //p.Add("@b", dbType: DbType.Int32, direction: ParameterDirection.Output);
    //p.Add("@c", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
    //        /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="cmd"></param>
    /// <param name="param"></param>
    /// <param name="commandType"></param>
    /// <returns></returns>
    public List<T> QueryList<T>(string cmd, object param, CommandType? commandType = null, bool beginTransaction = false) where T : class
    {
        return Execute((conn, dbTransaction) =>
        {
            return conn.Query<T>(cmd, param, dbTransaction, commandType: commandType ?? CommandType.Text).ToList();
        }, beginTransaction);
    }

    public async Task<List<T>> QueryListAsync<T>(string cmd, object param = null, CommandType? commandType = null, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
    {
        //return await (await ExecuteAsync(async (conn, dbTransaction) =>
        //{
        //    var data = await conn.QueryAsync<T>(cmd, param, dbTransaction, commandType: commandType ?? CommandType.Text);

        //    return data.ToList();
        //}, beginTransaction)).ConfigureAwait(false);

        //return (await Connection.QueryAsync<T>(cmd, param, null, commandType: commandType ?? CommandType.Text)).ToList();

        return (await Connection.QueryAsync<T>(cmd, param, transaction, commandTimeout, commandType)).ToList();
    }


    public T QueryFirst<T>(string cmd, object param = null, CommandType? commandType = null, bool beginTransaction = false) where T : class
    {
        var list = QueryList<T>(cmd, param, commandType: commandType ?? CommandType.Text, beginTransaction: beginTransaction).ToList();
        return list.Count == 0 ? null : list[0];
    }
    public async Task<T> QueryFirstAsync<T>(string cmd, object param = null, CommandType? commandType = null, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
    {
        var data = await QueryListAsync<T>(cmd, param, commandType, transaction, commandTimeout);
        var list = data.ToList();
        return !list.Any() ? null : list[0];
    }

    public object ExecuteScalar(string cmd, object param, CommandType? commandType = null, bool beginTransaction = false)
    {
        try
        {
            return Execute<object>((conn, dbTransaction) =>
            {
                return conn.ExecuteScalar(cmd, param, dbTransaction, commandType: commandType ?? CommandType.Text);
            }, beginTransaction);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<object> ExecuteScalarAsync(string cmd, object param, CommandType? commandType = null, bool beginTransaction = false)
    {
        try
        {
            return await Execute(async (conn, dbTransaction) =>
            {
                return await conn.ExecuteScalarAsync(cmd, param, dbTransaction, commandType: commandType ?? CommandType.Text);
            }, beginTransaction);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public int ExcuteNonQuery(string cmd, object param, CommandType? commandType = null, bool beginTransaction = false)
    {
        return Execute<int>((conn, dbTransaction) =>
        {
            return conn.Execute(cmd, param, dbTransaction, commandType: commandType ?? CommandType.Text);
        }, beginTransaction);
    }
    public IDataReader ExecuteReader(string cmd, object param, CommandType? commandType = null, bool beginTransaction = false)
    {
        return Execute((conn, dbTransaction) =>
        {
            return conn.ExecuteReader(cmd, param, dbTransaction, commandType: commandType ?? CommandType.Text);
        }, beginTransaction, false);
    }
    public SqlMapper.GridReader QueryMultiple(string cmd, object param, CommandType? commandType = null, bool beginTransaction = false)
    {
        return Execute((conn, dbTransaction) =>
        {
            return conn.QueryMultiple(cmd, param, dbTransaction, commandType: commandType ?? CommandType.Text);
        }, beginTransaction, false);
    }

    /// <summary>
    /// 获取output值 param.Get<int>("@b");
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <param name="cmd"></param>
    /// <param name="param"></param>
    /// <param name="commandType"></param>
    /// <param name="dbTransaction"></param>
    /// <returns></returns>
    public (List<T1>, List<T2>) QueryMultiple<T1, T2>(string cmd, object param, CommandType? commandType = null, bool beginTransaction = false)
    {
        using (SqlMapper.GridReader reader = QueryMultiple(cmd, param, commandType, beginTransaction))
        {
            return (reader.Read<T1>().ToList(), reader.Read<T2>().ToList());
        }
    }

    public (List<T1>, List<T2>, List<T3>) QueryMultiple<T1, T2, T3>(string cmd, object param, CommandType? commandType = null, bool beginTransaction = false)
    {
        using (SqlMapper.GridReader reader = QueryMultiple(cmd, param, commandType, beginTransaction))
        {
            return (reader.Read<T1>().ToList(), reader.Read<T2>().ToList(), reader.Read<T3>().ToList());
        }
    }


    public T Execute<T>(Func<IDbConnection, IDbTransaction, T> func, bool beginTransaction = false, bool disposeConn = true)
    {
        IDbTransaction dbTransaction = null;

        if (beginTransaction)
        {
            Connection.Open();
            dbTransaction = Connection.BeginTransaction();
        }
        try
        {
            T reslutT = func(Connection, dbTransaction);
            dbTransaction?.Commit();
            return reslutT;
        }
        catch (Exception)
        {
            dbTransaction?.Rollback();
            Connection.Dispose();
            throw;
        }
        finally
        {
            if (disposeConn)
            {
                Connection.Dispose();
            }
        }
    }


    public int ExecuteDML(string cmd, object param, CommandType? commandType = null, IDbTransaction dbTransaction = null)
    {
        return ExecuteDML<int>((conn, dbTransaction) =>
        {
            return conn.Execute(cmd, param, dbTransaction, commandType: commandType ?? CommandType.Text);
        }, dbTransaction, false);
    }

    public async Task<int> ExecuteDMLAsync(string cmd, object param, CommandType? commandType = null, IDbTransaction dbTransaction = null)
    {
        return await ExecuteDML(async (conn, dbTransaction) =>
        {
            return await conn.ExecuteAsync(cmd, param, dbTransaction, commandType: commandType ?? CommandType.Text);
        }, dbTransaction, false);
    }

    public IDataReader ExecuteDMLReader(string cmd, object param, CommandType? commandType = null, IDbTransaction dbTransaction = null)
    {
        return ExecuteDML((conn, dbTransaction) =>
        {
            return conn.ExecuteReader(cmd, param, dbTransaction, commandType: commandType ?? CommandType.Text);
        }, dbTransaction, false);
    }

    /// <summary>
    /// 执行数据库
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="func"></param>
    /// <param name="dbTransaction"></param>
    /// <param name="disposeConn"></param>
    /// <returns></returns>
    public T ExecuteDML<T>(Func<IDbConnection, IDbTransaction, T> func, IDbTransaction dbTransaction = null, bool disposeConn = false)
    {
        if (dbTransaction == null)
        {
            try
            {
                T reslutT = func(Connection, dbTransaction);
                dbTransaction?.Commit();
                return reslutT;
            }
            catch (Exception)
            {
                dbTransaction?.Rollback();
                Connection.Dispose();
                throw;
            }
            finally
            {
                if (disposeConn)
                    Connection.Dispose();
            }
        }
        else
        {
            try
            {
                if (_connection == null)
                    _connection = dbTransaction.Connection;
                T reslutT = func(Connection, dbTransaction);
                return reslutT;
            }
            catch (Exception)
            {
                Connection.Dispose();
                throw;
            }
            finally
            {
                if (disposeConn)
                    Connection.Dispose();
            }
        }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity"></param>
    /// <param name="updateFileds">指定插入的字段</param>
    /// <param name="beginTransaction">是否开启事务</param>
    /// <returns></returns>
    public int Add<T>(T entity, Expression<Func<T, object>> updateFileds = null, bool beginTransaction = false)
    {
        return AddRange([entity], updateFileds, beginTransaction);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entities"></param>
    /// <param name="updateFileds">指定插入的字段</param>
    /// <param name="beginTransaction">是否开启事务</param>
    /// <returns></returns>
    public int AddRange<T>(IEnumerable<T> entities, Expression<Func<T, object>> addFileds = null, bool beginTransaction = true)
    {
        Type entityType = typeof(T);
        var key = entityType.GetKeyProperty();
        if (key == null)
            throw new Exception("实体必须包括主键才能批量更新");
        string[] columns;

        //指定插入的字段
        if (addFileds != null)
            columns = addFileds.GetExpressionToArray();

        else
        {
            var properties = entityType.GetGenericProperties();
            if (key.PropertyType != typeof(Guid))
                properties = properties.Where(x => x.Name != key.Name).ToArray();
            columns = properties.Select(x => x.Name).ToArray();
        }
        string sql = null;
        bool mysql = DBType.Name == DbCurrentType.MySql.ToString();
        if (mysql)
        {
            //mysql批量写入待优化
            sql = $"insert into {entityType.GetEntityTableName()}({string.Join(",", columns)})" +
             $"values(@{string.Join(",@", columns)});";
        }
        else
        {
            //sqlserver通过临时表批量写入
            sql = $"insert into {entityType.GetEntityTableName()}({string.Join(",", columns)})" +
             $"select *  from  {EntityToSqlTempName.TempInsert};";
            sql = entities.GetEntitySql(entityType == typeof(Guid), sql, null, addFileds, null);
        }
        return Execute<int>((conn, dbTransaction) =>
        {
            return conn.Execute(sql, mysql ? entities.ToList() : null);
        }, beginTransaction);
    }


    /// <summary>
    /// sqlserver使用的临时表参数化批量更新，mysql批量更新待发开
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity">实体必须带主键</param>
    /// <param name="updateFileds">指定更新的字段x=new {x.a,x.b}</param>
    /// <param name="beginTransaction">是否开启事务</param>
    /// <returns></returns>
    public int Update<T>(T entity, Expression<Func<T, object>> updateFileds = null, bool beginTransaction = true)
    {
        return UpdateRange([entity], updateFileds, beginTransaction);
    }

    /// <summary>
    ///(根据主键批量更新实体) sqlserver使用的临时表参数化批量更新，mysql待优化
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entities">实体必须带主键</param>
    /// <param name="updateFileds">批定更新字段</param>
    /// <param name="beginTransaction"></param>
    /// <returns></returns>
    public int UpdateRange<T>(IEnumerable<T> entities, Expression<Func<T, object>> updateFileds = null, bool beginTransaction = true)
    {
        Type entityType = typeof(T);
        var key = entityType.GetKeyProperty();
        if (key == null)
            throw new Exception("实体必须包括主键才能批量更新");

        var properties = entityType.GetGenericProperties()
        .Where(x => x.Name != key.Name);
        if (updateFileds != null)
            properties = properties.Where(x => updateFileds.GetExpressionToArray().Contains(x.Name));

        if (DBType.Name == DbCurrentType.MySql.ToString())
        {
            var paramsList = new List<string>();
            foreach (var item in properties)
            {
                paramsList.Add(item.Name + "=@" + item.Name);
            }
            string sqltext = $@"UPDATE {entityType.GetEntityTableName()} SET {string.Join(",", paramsList)} WHERE {entityType.GetKeyName()} = @{entityType.GetKeyName()} ;";

            return ExcuteNonQuery(sqltext, entities, CommandType.Text, true);
            // throw new Exception("mysql批量更新未实现");
        }
        string fileds = string.Join(",", properties.Select(x => $" a.{x.Name}=b.{x.Name}").ToArray());
        string sql = $"update  a  set {fileds} from  {entityType.GetEntityTableName()} as a inner join {EntityToSqlTempName.TempInsert.ToString()} as b on a.{key.Name}=b.{key.Name}";
        sql = entities.ToList().GetEntitySql(true, sql, null, updateFileds, null);
        return ExcuteNonQuery(sql, null, CommandType.Text, true);
    }

    public int DelWithKey<T>(bool beginTransaction = false, params object[] keys)
    {
        Type entityType = typeof(T);
        var keyProperty = entityType.GetKeyProperty();
        if (keyProperty == null || keys == null || keys.Length == 0) return 0;

        IEnumerable<(bool, string, object)> validation = keyProperty.ValidationValueForDbType(keys);
        if (validation.Any(x => !x.Item1))
        {
            throw new Exception($"主键类型【{validation.Where(x => !x.Item1).Select(s => s.Item3).FirstOrDefault()}】不正确");
        }
        string tKey = entityType.GetKeyProperty().Name;
        FieldType fieldType = entityType.GetFieldType();
        string joinKeys = (fieldType == FieldType.Int || fieldType == FieldType.BigInt)
             ? string.Join(",", keys)
             : $"'{string.Join("','", keys)}'";

        string sql = $"DELETE FROM {entityType.GetEntityTableName()} where {tKey} in ({joinKeys});";
        return (int)ExecuteScalar(sql, null);
    }
    /// <summary>
    /// 使用key批量删除
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="keys"></param>
    /// <returns></returns>
    public int DelWithKey<T>(params object[] keys)
    {
        return DelWithKey<T>(false, keys);
    }

    #region 批量插入
    /// <summary>
    /// 通过Bulk批量插入
    /// </summary>
    /// <param name="table"></param>
    /// <param name="tableName"></param>
    /// <param name="sqlBulkCopyOptions"></param>
    /// <param name="dbKeyName"></param>
    /// <returns></returns>
    private int MSSqlBulkInsert(DataTable table, string tableName, SqlBulkCopyOptions sqlBulkCopyOptions = SqlBulkCopyOptions.UseInternalTransaction, string dbKeyName = null)
    {
        if (!string.IsNullOrEmpty(dbKeyName))
            Connection.ConnectionString = DBServerProvider.GetConnectionString(dbKeyName);

        using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(Connection.ConnectionString, sqlBulkCopyOptions))
        {
            sqlBulkCopy.DestinationTableName = tableName;
            sqlBulkCopy.BatchSize = table.Rows.Count;
            for (int i = 0; i < table.Columns.Count; i++)
            {
                sqlBulkCopy.ColumnMappings.Add(table.Columns[i].ColumnName, table.Columns[i].ColumnName);
            }
            sqlBulkCopy.WriteToServer(table);
            return table.Rows.Count;
        }
    }
    public int BulkInsert<T>(List<T> entities, string tableName = null, Expression<Func<T, object>> columns = null, SqlBulkCopyOptions? sqlBulkCopyOptions = null)
    {
        DataTable table = entities.ToDataTable(columns, false);
        return BulkInsert(table, tableName ?? typeof(T).GetEntityTableName(), sqlBulkCopyOptions);
    }
    public int BulkInsert(DataTable table, string tableName, SqlBulkCopyOptions? sqlBulkCopyOptions = null, string fileName = null, string tmpPath = null)
    {
        if (!string.IsNullOrEmpty(tmpPath))
            tmpPath = tmpPath.ReplacePath();

        if (Connection.GetType().Name == "MySqlConnection")
            return MySqlBulkInsert(table, tableName, fileName, tmpPath);
        return MSSqlBulkInsert(table, tableName, sqlBulkCopyOptions ?? SqlBulkCopyOptions.KeepIdentity);
    }
    #endregion

    #region 大批量数据插入,返回成功插入行数
    /// <summary>
    ///大批量数据插入,返回成功插入行数
    /// </summary>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="table">数据表</param>
    /// <returns>返回成功插入行数</returns>
    private int MySqlBulkInsert(DataTable table, string tableName, string fileName = null, string tmpPath = null)
    {
        if (table.Rows.Count == 0)
            return 0;
        tmpPath = tmpPath ?? FileHelper.GetCurrentDownLoadPath();
        fileName = fileName ?? $"{DateTime.Now.ToString("yyyyMMddHHmmss")}.csv";
        int insertCount = 0;
        string csv = DataTableToCsv(table);
        FileHelper.WriteFile(tmpPath, fileName, csv);
        string path = tmpPath + fileName;
        try
        {
            if (Connection.State == ConnectionState.Closed)
                Connection.Open();
            using (IDbTransaction tran = Connection.BeginTransaction())
            {
                var bulk = new MySqlBulkLoader(Connection as MySqlConnection)
                {
                    FieldTerminator = ",",
                    FieldQuotationCharacter = '"',
                    EscapeCharacter = '"',
                    LineTerminator = "\r\n",
                    FileName = path.ReplacePath(),
                    NumberOfLinesToSkip = 0,
                    TableName = tableName,
                };
                bulk.Columns.AddRange(table.Columns.Cast<DataColumn>().Select(colum => colum.ColumnName).ToList());
                insertCount = bulk.Load();
                tran.Commit();
            }
        }
        catch (Exception) { throw; }
        finally
        {
            Connection?.Dispose();
            Connection?.Close();
        }
        return insertCount;
        //   File.Delete(path);
    }
    #endregion

    #region 将DataTable转换为标准的CSV
    /// <summary>
    ///将DataTable转换为标准的CSV
    /// </summary>
    /// <param name="table">数据表</param>
    /// <returns>返回标准的CSV</returns>
    private string DataTableToCsv(DataTable table)
    {
        //以半角逗号（即,）作分隔符，列为空也要表达其存在。
        //列内容如存在半角逗号（即,）则用半角引号（即""）将该字段值包含起来。
        //列内容如存在半角引号（即"）则应替换成半角双引号（""）转义，并用半角引号（即""）将该字段值包含起来。
        var sb = new StringBuilder();
        DataColumn colum;
        Type typeString = typeof(string);
        Type typeDate = typeof(DateTime);

        foreach (DataRow row in table.Rows)
        {
            for (int i = 0; i < table.Columns.Count; i++)
            {
                colum = table.Columns[i];
                if (i != 0) sb.Append(",");
                if (colum.DataType == typeString && row[colum].ToString().Contains(","))
                {
                    sb.Append("\"" + row[colum].ToString().Replace("\"", "\"\"") + "\"");
                }
                else if (colum.DataType == typeDate)
                {
                    //centos系统里把datatable里的日期转换成了10/18/18 3:26:15 PM格式
                    bool b = DateTime.TryParse(row[colum].ToString(), out DateTime dt);
                    sb.Append(b ? dt.ToString("yyyy-MM-dd HH:mm:ss") : "");
                }
                else sb.Append(row[colum].ToString());
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
    #endregion

    #region 返回DataTable
    /// <summary>
    /// 返回DataTable
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="param"></param>
    /// <param name="commandType"></param>
    /// <param name="beginTransaction"></param>
    /// <returns></returns>
    public DataTable GetDataTable(string cmd, object param, CommandType? commandType = null, bool beginTransaction = false)
    {
        var table = new DataTable("MyTable");
        var reader = ExecuteReader(cmd, param, commandType, beginTransaction);
        table.Load(reader);
        return table;
    }

    public DataTable GetTransDataTable(string cmd, object param, CommandType? commandType = null, IDbTransaction dbTransaction = null)
    {
        var table = new DataTable("MyTable");
        var reader = ExecuteDMLReader(cmd, param, commandType, dbTransaction);
        table.Load(reader);
        return table;
    }

    public async Task<DataTable> GetDataTableAsync(string cmd, object param = null, IDbTransaction transaction = null, CommandType? commandType = null, int? commandTimeout = null)
    {
        var table = new DataTable("MyTable");
        var reader = await Connection.ExecuteReaderAsync(cmd, param, transaction, commandTimeout, commandType: commandType ?? CommandType.Text); ;
        table.Load(reader);
        return table;
    }
    #endregion

    #region Transaction
    public IDbTransaction GetNewTransaction()
    {
        Connection.Open();
        return Connection.BeginTransaction();
    }

    public void CommitTransaction(IDbTransaction dbTransaction)
    {
        dbTransaction?.Commit();
    }

    public void RollbackTransaction(IDbTransaction dbTransaction)
    {
        dbTransaction?.Rollback();
    }
    #endregion Transaction

    #region 查询按实体返回
    public List<T> QueryTransList<T>(string cmd, object param, CommandType? commandType = null, IDbTransaction dbTransaction = null) where T : class
    {
        return ExecuteDML((conn, dbTransaction) =>
        {
            return conn.Query<T>(cmd, param, dbTransaction, commandType: commandType ?? CommandType.Text).ToList();
        }, dbTransaction, false);
    }
    public T QueryTransFirst<T>(string cmd, object param, CommandType? commandType = null, IDbTransaction dbTransaction = null) where T : class
    {
        var list = QueryTransList<T>(cmd, param, commandType: commandType ?? CommandType.Text, dbTransaction: dbTransaction).ToList();
        return list.Count == 0 ? null : list[0];
    }
    #endregion
}
