using EU.Core.Common.Helper;
using EU.Core.Common.UserManager;
namespace EU.Core.Common;

/// <summary>
/// Sql插入类
/// </summary>
public class DbInsert
{
    #region 变量定义
    private string sql;
    private string createProgram;
    private string rowId;
    /// <summary>
    /// 如果使用线程，此处可以指定插入用户代码，否则CreatedBy为空
    /// </summary>
    public string CreatedBy { get; set; }
    public string RowId
    {
        get { return rowId; }
        set { rowId = value; }
    }
    private bool isInitDefaultValue = true;
    /// <summary>
    /// 是否初始化默认字段，像：ID，CreatedBy，CreatedTime，tag，IsActive等，默认会初始化
    /// </summary>
    public bool IsInitDefaultValue
    {
        get
        {
            return isInitDefaultValue;
        }
        set
        {
            isInitDefaultValue = value;
        }
    }
    private bool isInitRowId = true;
    /// <summary>
    /// 是否初始化ROW_ID字段(默认初始化),只有IsInitDefaultValue为true时才有效!
    /// </summary>
    public bool IsInitRowId
    {
        get
        {
            return isInitRowId;
        }
        set
        {
            isInitRowId = value;
        }
    }
    #endregion

    #region 构造函数
    public DbInsert()
    {
    }

    public DbInsert(string tableName)
    {
        SetTableName(tableName.ToUpper());
    }

    public DbInsert(string tableName, string createProgram)
    {
        SetTableName(tableName.ToUpper());
        this.createProgram = createProgram;
    }

    public void SetTableName(string tableName)
    {
        sql = "INSERT INTO {0} () VALUES ()";
        sql = string.Format(sql, tableName.ToUpper());
    }
    #endregion

    #region Values
    public void Values(string fieldName, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        value = value.Replace("'", "''");
        value = value.Trim();
        string s = "N'{0}'";
        s = string.Format(s, value);

        FormatValue(fieldName.ToUpper(), s);
    }
    public void Values(string fieldName, Guid value)
    {
        string value1 = value.ToString();
        if (string.IsNullOrEmpty(value1)) return;
        value1 = value1.Replace("'", "''");
        value1 = value1.Trim();
        string s = "N'{0}'";
        s = string.Format(s, value1);

        FormatValue(fieldName.ToUpper(), s);
    }

    public void Values(string fieldName, Guid? value)
    {
        string value1 = value.ToString();
        if (string.IsNullOrEmpty(value1)) return;
        value1 = value1.Replace("'", "''");
        value1 = value1.Trim();
        string s = "N'{0}'";
        s = string.Format(s, value1);

        FormatValue(fieldName.ToUpper(), s);
    }

    public void Values(string fieldName, int value)
    {
        FormatValue(fieldName.ToUpper(), Convert.ToString(value));
    }

    public void Values(string fieldName, double value)
    {
        FormatValue(fieldName.ToUpper(), Convert.ToString(value));
    }

    public void Values(string fieldName, decimal value)
    {
        FormatValue(fieldName.ToUpper(), Convert.ToString(value));
    }

    public void Values(string fieldName, decimal? value)
    {
        FormatValue(fieldName.ToUpper(), Convert.ToString(value));
    }

    public void Values(string fieldName, DateTime value)
    {
        if (value == DateTime.MinValue) return;
        string valTemp = string.Empty;
        valTemp = Convert.ToString(value);
        //valTemp = valTemp.Replace("'", "''");
        string s = string.Empty;
        s = "CAST('{0}' AS DATETIME)";
        s = string.Format(s, valTemp);

        FormatValue(fieldName.ToUpper(), s);
    }
    public void Values(string fieldName, DateTime? value)
    {
        if (value == null) return;
        string valTemp = string.Empty;
        valTemp = Convert.ToString(value);
        //valTemp = valTemp.Replace("'", "''");
        string s = string.Empty;
        s = "CAST('{0}' AS DATETIME)";
        s = string.Format(s, valTemp);

        FormatValue(fieldName.ToUpper(), s);
    }
    #endregion

    #region 加密插入
    /// <summary>
    /// 以加密方式插入
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="value"></param>
    public void ValuesAsSecurity(string fieldName, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        value = value.Replace("'", "''");
        string s = "encryptbykey(key_guid('fookey'),N'{0}')";
        s = string.Format(s, value);

        FormatValue(fieldName.ToUpper(), s);
    }
    /// <summary>
    /// 加密保存
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="value"></param>
    public void ValuesAsSecurity(string fieldName, int value)
    {
        ValuesAsSecurity(fieldName.ToUpper(), Convert.ToString(value));
    }
    /// <summary>
    /// 加密保存
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="value"></param>
    public void ValuesAsSecurity(string fieldName, double value)
    {
        ValuesAsSecurity(fieldName.ToUpper(), Convert.ToString(value));
    }
    /// <summary>
    /// 加密保存
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="value"></param>
    public void ValuesAsSecurity(string fieldName, decimal value)
    {
        ValuesAsSecurity(fieldName.ToUpper(), Convert.ToString(value));
    }
    #endregion

    #region Public Method
    public string GetSql()
    {
        if (isInitRowId == true)
        {
            rowId = StringHelper.Id;
            Values("ID", rowId);
        }
        if (isInitDefaultValue == true)
            InitDefaultValues();

        return sql;
    }
    #endregion

    #region Private Method
    private void FormatValue(string fieldName, string value)
    {
        if (value != null)
        {
            value = value.Trim();
        }
        if (DBHelper.MySql)
        {
            fieldName = fieldName.Replace("[", "`");
            fieldName = fieldName.Replace("]", "`");
        }
        sql = sql.Replace("{", "{{");
        sql = sql.Replace("}", "}}");
        int n = sql.IndexOf("() VALUES");

        if (n == -1)
        {
            n = sql.IndexOf(") VALUES");
            sql = sql.Insert(n, ", {0} ");
            n = sql.LastIndexOf(")");
            sql = sql.Insert(n, ", {1}");
        }
        else
        {
            sql = sql.Insert(++n, " {0} ");
            n = sql.LastIndexOf(")");
            sql = sql.Insert(n, " {1}");
        }
        sql = string.Format(sql, fieldName.ToUpper(), value);
    }

    /// <summary>
    /// 初始化Insert语句默认需要插入的值
    /// </summary>
    private void InitDefaultValues()
    {
        //DatabaseType dbType = DBHelper.GetDatabaseType();
        //row_id
        //if (DBHelper.IsOracle())
        //{
        //    rowId = DBHelper.ExecuteScalar("SELECT F_GET_SYSID FROM DUAL") as string;
        //}
        //else
        //{
        //    rowId = DBHelper.ExecuteScalar("SELECT REPLACE(REPLACE(REPLACE(CONVERT(VARCHAR, GETDATE(), 120 ),'-',''),' ',''),':','')") as string;
        //}

        //create_by
        Guid? createBy = UserContext.Current.User_Id;
        if (createBy != Guid.Empty)
            Values("CreatedBy", createBy);

        //create_date 
        Values("CreatedTime", Utility.GetSysDate());
        Values("GroupId", UserContext.Current.GroupId);
        Values("CompanyId", UserContext.Current.CompanyId);

        //create_program
        //Values("CREATED_PROGRAM", createProgram);
        //tag
        Values("Tag", 1);
        //active_flag
        //Values("DELETE_FLAG", "N");
    }
    #endregion
}
