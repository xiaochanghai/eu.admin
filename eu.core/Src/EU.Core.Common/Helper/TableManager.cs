using System.Data;
using EU.Core.Common.Const;
using EU.Core.Common.DB;
using EU.Core.Common.Enums;

namespace EU.Core.Common.Helper;

public class TableManager
{
    public static void InitTableAndField(string tableCode, Guid? id)
    {
        InitTableAndField(tableCode, "U", id);
        InitTableAndField(tableCode, "V", id);
    }
    #region 初始化公共方法
    private static void InitTableAndField(string tableCode, string typeCode, Guid? id)
    {
        string sql1 = string.Empty;
        try
        {

            #region 变量定义
            int count = 0;
            string sql = string.Empty;

            typeCode = typeCode == "U" ? "TABLE" : "VIEW";

            #endregion
            #region 处理表
            sql = "SELECT COUNT(0) FROM SmTableCatalog A WHERE (A.TableCode='{0}' OR A.TableCode='{1}') AND A.IsDeleted='false' AND A.IsActive='true'";
            sql = string.Format(sql, tableCode, tableCode.ToUpper());
            count = int.Parse(Convert.ToString(DBHelper.ExecuteScalar(sql)));
            sql = @"SELECT f.value TableName
                            FROM sysobjects d
                                 LEFT JOIN sys.extended_properties f
                                    ON d.id = f.major_id AND f.minor_id = 0
                            WHERE d.name = '{0}'";
            sql = string.Format(sql, tableCode);
            string tableName = Convert.ToString(DBHelper.ExecuteScalar(sql, null));
            if (count == 0)
            {
                DbInsert di = new("SmTableCatalog", "InitTableAndField");
                di.Values("TableCode", tableCode);
                di.Values("TableName", tableName);
                di.Values("TypeCode", typeCode);
                DBHelper.ExecuteDML(di.GetSql());
                id = Guid.Parse(di.RowId);
            }
            else
            {
                sql = $"UPDATE A  SET A.TableCode='{tableCode}' ,A.TableName='{tableName}',A.UpdateTime=getdate() FROM SmTableCatalog A WHERE (A.TableCode='{tableCode}' OR A.TableCode='{tableCode.ToUpper()}') AND A.IsDeleted='false' AND A.IsActive='true'";

                DBHelper.ExecuteDML(sql);
            }
            #endregion
            #region 处理新增列
            DataTable dtFieldCatalog = new();
            DataTable dtUserTabColumns = null;
            bool isMySql = DBType.Name == DbCurrentType.MySql.ToString();
            if (isMySql)
            {
                sql = @"SELECT A.COLUMN_NAME,
                               CASE
                                  WHEN A.DATA_TYPE IN ('varchar', 'char', 'text')
                                  THEN
                                     A.character_maximum_length
                                  WHEN A.DATA_TYPE IN ('timestamp')
                                  THEN
                                     A.DATETIME_PRECISION
                                  WHEN A.DATA_TYPE IN ('int', 'decimal')
                                  THEN
                                     A.NUMERIC_PRECISION
                               END
                                  AS DATA_LENGTH,
                               CASE
                                  WHEN data_type IN ('BIT','BOOL','bit','bool')
                                  THEN
                                     'BOOL'
                                  WHEN data_type IN ('tinyint', 'TINYINT')
                                  THEN
                                     'SBYTE'
                                  WHEN data_type IN ('MEDIUMINT','mediumint','int','INT','year','Year')
                                  THEN
                                     'INT'
                                  WHEN data_type IN ('BIGINT', 'bigint')
                                  THEN
                                     'BIGINT'
                                  WHEN data_type IN ('FLOAT','DOUBLE','DECIMAL', 'float','double', 'decimal')
                                  THEN
                                     'DECIMAL'
                                  WHEN data_type IN ('CHAR',
                                                     'VARCHAR',
                                                     'TINY TEXT',
                                                     'TEXT',
                                                     'MEDIUMTEXT',
                                                     'LONGTEXT',
                                                     'TINYBLOB',
                                                     'BLOB',
                                                     'MEDIUMBLOB',
                                                     'LONGBLOB',
                                                     'Time',
                                                     'char',
                                                     'varchar',
                                                     'tiny text',
                                                     'text',
                                                     'mediumtext',
                                                     'longtext',
                                                     'tinyblob',
                                                     'blob',
                                                     'mediumblob',
                                                     'longblob',
                                                     'time')
                                  THEN
                                     'STRING'
                                  WHEN data_type IN ('Date','DateTime','TimeStamp','date','datetime','timestamp')
                                  THEN
                                     'DATETIME'
                                  ELSE
                                     'STRING'
                               END
                                  AS DATA_TYPE
                        FROM information_schema.COLUMNS A
                        WHERE table_name = '{0}' and table_schema = '{1}'";
                sql = string.Format(sql, tableCode, GetMysqlTableSchema());
            }
            else
            {
                sql = @"SELECT A.name AS table_name,
                               B.name AS COLUMN_NAME,
                               D.data_type,
                               C.value AS COLUMN_DESCRIPTION,
                               D.NUMERIC_PRECISION,
                               D.NUMERIC_SCALE,
                               D.CHARACTER_MAXIMUM_LENGTH,
                               F.TYPE DATA_TYPE,
                               F.LENGTH DATA_LENGTH
                        FROM sys.tables A
                             INNER JOIN sys.columns B ON B.object_id = A.object_id
                             LEFT JOIN sys.extended_properties C
                                ON C.major_id = B.object_id AND C.minor_id = B.column_id
                             LEFT JOIN information_schema.columns D
                                ON D.column_name = B.name AND D.TABLE_NAME = '{0}'
                             LEFT JOIN SYSOBJECTS E ON B.[name] = E.[name]
                             LEFT JOIN SYSCOLUMNS F ON E.ID = F.ID
                        WHERE A.name = '{0}'
                        ORDER BY B.column_id ASC";
                sql = string.Format(sql, tableCode);
            }
            dtUserTabColumns = DBHelper.GetDataTable(sql);
            if (dtUserTabColumns != null && dtUserTabColumns.Rows.Count > 0)
            {
                //sql = "DELETE A FROM SmFieldCatalog A WHERE (A.TableCode='{0}' OR A.TableCode='{1}') AND (A.DataType='NUMBER' OR A.DataType='DATE')";
                //sql = string.Format(sql, tableCode, tableCode.ToUpper());
                //DBHelper.ExecuteScalar(sql);
                for (int i = 0; i < dtUserTabColumns.Rows.Count; i++)
                {
                    sql = "SELECT COUNT(0) FROM SmFieldCatalog A WHERE A.TableCode='{0}' AND A.ColumnCode='{1}'";
                    sql = string.Format(sql, tableCode, Convert.ToString(dtUserTabColumns.Rows[i]["COLUMN_NAME"]).ToUpper());
                    count = int.Parse(Convert.ToString(DBHelper.ExecuteScalar(sql)));
                    var description = Convert.ToString(dtUserTabColumns.Rows[i]["COLUMN_DESCRIPTION"]);
                    var columnCode = Convert.ToString(dtUserTabColumns.Rows[i]["COLUMN_NAME"]);
                    if (count == 0)
                    {
                        //DataRow row = dtFieldCatalog.NewRow();
                        //row["ID"] = Utility.GuidId1;
                        //row["TABLE_CODE"] = tableCode;
                        //row["COLUMN_CODE"] = Convert.ToString(dtUserTabColumns.Rows[i]["COLUMN_NAME"]).ToUpper();
                        //row["COLUMN_NAME"] = Convert.ToString(dtUserTabColumns.Rows[i]["COLUMN_NAME"]).ToUpper();

                        DbInsert di = new("SmFieldCatalog", "InitTableAndField");
                        di.Values("TableCatalogId", id);
                        di.Values("TableCode", tableCode);
                        di.Values("ColumnCode", columnCode);
                        di.Values("ColumnName", description);
                        string dataType = Convert.ToString(dtUserTabColumns.Rows[i]["DATA_TYPE"]);
                        if (!isMySql)
                        {

                            switch (dataType)
                            {
                                case "37":
                                    dataType = "GUID";
                                    break;
                                case "111":
                                    dataType = "DATETIME";
                                    break;
                                case "0":
                                    dataType = "DATE";
                                    break;
                                case "106":
                                    dataType = "DECIMAL";
                                    break;
                                case "38":
                                    dataType = "INT";
                                    break;
                                case "39":
                                default:
                                    dataType = "STRING";
                                    break;
                            }
                        }
                        di.Values("DataType", dataType);
                        string len = Convert.ToString(dtUserTabColumns.Rows[i]["DATA_LENGTH"]);
                        int length = 0;
                        if (!string.IsNullOrEmpty(len))
                            length = int.Parse(len);

                        di.Values("DataLength", length);
                        sql1 = di.GetSql();
                        DBHelper.ExecuteDML(sql1);
                        //row["DATA_TYPE"] = dataType;
                        //row["DATA_LENGTH"] = int.Parse(Convert.ToString(dtUserTabColumns.Rows[i]["DATA_LENGTH"]));
                        //dtFieldCatalog.Rows.Add(row);
                        //DBHelper.BulkInsert(dtFieldCatalog, "SmFieldCatalog");
                    }
                    else
                    {
                        sql = $"UPDATE A SET A.TableCode='{tableCode}' ,A.UpdateTime=getdate() ,A.ColumnCode='{columnCode}'," +
                            $"A.ColumnName='{description}' FROM SmFieldCatalog A " +
                            $"WHERE (A.TableCode='{tableCode}' OR A.TableCode='{tableCode.ToUpper()}') AND(A.ColumnCode='{columnCode}' OR A.ColumnCode='{columnCode.ToUpper()}') AND A.IsDeleted='false' AND A.IsActive='true'";
                        DBHelper.ExecuteDML(sql);
                    }
                }
            }
            #endregion

            #region 删除列中不存在的数据
            sql = "SELECT A.* FROM SmFieldCatalog A WHERE A.TableCode='{0}'";
            sql = string.Format(sql, tableCode);
            dtFieldCatalog = DBHelper.GetDataTable(sql);
            for (int i = 0; i < dtFieldCatalog.Rows.Count; i++)
            {
                if (isMySql)
                {
                    sql = @"SELECT COUNT(0) FROM information_schema.COLUMNS WHERE table_name = '{0}'  AND table_schema = '{1}' AND COLUMN_NAME = '{2}'";
                    sql = string.Format(sql, tableCode, GetMysqlTableSchema(), dtFieldCatalog.Rows[i]["COLUMN_CODE"].ToString());
                }
                else
                {
                    sql = "SELECT COUNT(0) FROM SYSOBJECTS A,SYSCOLUMNS B WHERE A.ID=B.ID AND A.NAME='{0}' AND B.NAME='{1}'";
                    sql = string.Format(sql, tableCode, dtFieldCatalog.Rows[i]["ColumnCode"].ToString());
                }
                count = Convert.ToInt32(DBHelper.ExecuteScalar(sql));
                if (count == 0)
                {
                    sql = "DELETE FROM SmFieldCatalog WHERE ID='{0}'";
                    sql = string.Format(sql, dtFieldCatalog.Rows[i]["ID"].ToString());
                    DBHelper.ExecuteScalar(sql);
                }
            }
            #endregion
        }
        catch (Exception)
        {
            throw;
            //Logger.WriteLog("GridList", sql1);
            //Logger.WriteLog("GridList", E.StackTrace);

        }
    }
    #endregion

    /// <summary>
    /// 2020.05.17增加mysql获取表结构时区分当前所在数据库
    /// </summary>
    /// <returns></returns>
    private static string GetMysqlTableSchema()
    {
        try
        {
            string dbName = DBServerProvider.GetConnectionString().Split("Database=")[1].Split(";")[0]?.Trim();
            if (string.IsNullOrEmpty(dbName))
            {
                Console.WriteLine($"获取mysql数据库名失败:值为空!");
            }
            return dbName;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取mysql数据库名异常:{ex.Message}");
            return "";
        }
    }
}