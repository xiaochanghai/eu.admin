using EU.Core.Common.Helper;
using EU.Core.Common.UserManager;

namespace EU.Core.Common;

public class DbUpdate
{
    #region 变量定义
    public string Database { get; set; }
    private string sql;
    private string whereCondition;
    private string sqlTag;
    private decimal tag;
    public decimal Tag
    {
        get { return tag; }
        set { tag = value; }
    }
    private bool isInitDefaultValue = true;
    /// <summary>
    /// 是否初始化默认字段，像：UpdateBy，UpdateTime，tag，ModificationNum等，默认会初始化
    /// </summary>
    public bool IsInitDefaultValue
    {
        get
        {
            return isInitDefaultValue;
        }
        set { isInitDefaultValue = value; }
    }

    private string updateProgram = string.Empty;
    public string UpdateProgram
    {
        get { return updateProgram; }
        set { updateProgram = value; }
    }
    #endregion

    #region 构造函数
    public DbUpdate(string tableName)
    {
        sql = "UPDATE {0} SET WHERE {1}";
        sql = string.Format(sql, tableName.ToUpper(), "1=1");
        sqlTag = "SELECT TAG FROM {0} WHERE {1}";
        sqlTag = string.Format(sqlTag, tableName.ToUpper(), "1=1");
    }
    public DbUpdate(string tableName, string condition)
    {
        sql = "UPDATE {0} SET WHERE {1}";
        sql = string.Format(sql, tableName.ToUpper(), condition);
        sqlTag = "SELECT TAG FROM {0} WHERE {1}";
        sqlTag = string.Format(sqlTag, tableName.ToUpper(), condition);
    }
    public DbUpdate(string tableName, string fieldName, string fieldValue)
    {
        sql = "UPDATE {0} SET WHERE {1} = N'{2}'";
        sql = string.Format(sql, tableName.ToUpper(), fieldName.ToUpper(), fieldValue);
        sqlTag = "SELECT TAG FROM {0} WHERE {1} = N'{2}'";
        sqlTag = string.Format(sqlTag, tableName.ToUpper(), fieldName.ToUpper(), fieldValue);
    }
    public DbUpdate(string tableName, string fieldName, string fieldValue, string updateProgram)
    {
        sql = "UPDATE {0} SET WHERE {1} = N'{2}'";
        sql = string.Format(sql, tableName.ToUpper(), fieldName.ToUpper(), fieldValue);
        sqlTag = "SELECT TAG FROM {0} WHERE {1} = N'{2}'";
        sqlTag = string.Format(sqlTag, tableName.ToUpper(), fieldName.ToUpper(), fieldValue);
        this.updateProgram = updateProgram;
    }
    #endregion

    #region Set
    public void Set(string fieldName, int value)
    {
        inset(fieldName.ToUpper(), Convert.ToString(value));
    }
    public void Set(string fieldName, decimal? value)
    {
        inset(fieldName.ToUpper(), Convert.ToString(value));
    }
    public void Set(string fieldName, int? value)
    {
        inset(fieldName.ToUpper(), Convert.ToString(value));
    }

    public void Set(string fieldName, decimal value)
    {
        inset(fieldName.ToUpper(), Convert.ToString(value));
    }

    public void Set(string fieldName, string value)
    {
        try
        {
            //if (value != null)
            if (value != null)
            {
                value = value.Trim();
            }
            if (!string.IsNullOrEmpty(value))
            {
                string s = "N'{0}'";
                value = value.Replace("'", "''");
                s = string.Format(s, value);
                inset(fieldName.ToUpper(), s);
            }
            else
            {
                inset(fieldName.ToUpper(), null);
            }
        }
        catch (Exception) { throw; }
    }
    public void Set(string fieldName, Guid? value)
    {
        try
        {
            string value1 = value.ToString();
            //if (value != null)
            if (value1 != null)
            {
                value1 = value1.Trim();
            }
            if (!string.IsNullOrEmpty(value1))
            {
                string s = "N'{0}'";
                value1 = value1.Replace("'", "''");
                s = string.Format(s, value1);
                inset(fieldName.ToUpper(), s);
            }
            else
            {
                inset(fieldName.ToUpper(), null);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public void Set(string fieldName, DateTime value)
    {
        if (value == DateTime.MinValue) return;
        string valTemp = string.Empty;
        valTemp = Convert.ToString(value);
        valTemp = valTemp.Replace("'", "''");
        string s = string.Empty;
        s = "CAST('{0}' AS DATETIME)";
        s = string.Format(s, valTemp);

        inset(fieldName.ToUpper(), s);
    }

    public void Set(string fieldName, DateTime? value)
    {
        if (value == null) return;
        string valTemp = string.Empty;
        valTemp = Convert.ToString(value);
        valTemp = valTemp.Replace("'", "''");
        string s = string.Empty;
        s = "CAST('{0}' AS DATETIME)";
        s = string.Format(s, valTemp);

        inset(fieldName.ToUpper(), s);
    }
    /// <summary>
    /// 设置计算类型的更新，如SetCompute("TAG","TAG+1")
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="val"></param>
    public void SetCompute(string fieldName, string value)
    {
        try
        {
            if (value != null)
            {
                value = value.Trim();
            }
            inset(fieldName.ToUpper(), value);
        }
        catch (Exception)
        {
            throw;
        }
    }

    #region 加密更新
    /// <summary>
    /// 加密保存
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="value"></param>
    public void SetAsSecurity(string fieldName, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            Set(fieldName, value);
        }
        else
        {
            string temp = "encryptbykey(key_guid('fookey'),N'" + value + "')";
            inset(fieldName.ToUpper(), temp);
        }
    }
    /// <summary>
    /// 加密保存
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="value"></param>
    public void SetAsSecurity(string fieldName, int value)
    {
        SetAsSecurity(fieldName, Convert.ToString(value));
    }
    /// <summary>
    /// 加密保存
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="value"></param>
    public void SetAsSecurity(string fieldName, decimal value)
    {
        SetAsSecurity(fieldName, Convert.ToString(value));
    }
    #endregion


    #endregion

    #region Where
    public void Where(string whereCondition)
    {
        this.whereCondition += whereCondition + " AND ";
    }
    public void Where(string whereCondition, params string[] args)
    {
        this.whereCondition += string.Format(whereCondition, args) + " AND ";
    }
    public void Where(string whereCondition, DateTime fieldValue)
    {
        string tempFieldValue = Convert.ToString(fieldValue);
        tempFieldValue = tempFieldValue.Replace("'", "''");
        whereCondition = string.Format(whereCondition, "DBO.TO_DATE('{0}','YYYY/MM/DD HH24:MI:SS')");
        whereCondition = string.Format(whereCondition, tempFieldValue);
        this.whereCondition += whereCondition + " AND ";
    }
    public void Where(string whereCondition, DateTime fieldValue1, DateTime fieldValue2)
    {
        string tempFieldValue1 = Convert.ToString(fieldValue1);
        tempFieldValue1 = tempFieldValue1.Replace("'", "''");
        string tempFieldValue2 = Convert.ToString(fieldValue2);
        tempFieldValue2 = tempFieldValue2.Replace("'", "''");
        whereCondition = string.Format(whereCondition, "DBO.TO_DATE('{0}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{1}','YYYY/MM/DD HH24:MI:SS')");
        whereCondition = string.Format(whereCondition, tempFieldValue1, tempFieldValue2);
        this.whereCondition += whereCondition + " AND ";
    }
    public void Where(string whereCondition, DateTime fieldValue1, DateTime fieldValue2, DateTime fieldValue3)
    {
        string tempFieldValue1 = Convert.ToString(fieldValue1);
        tempFieldValue1 = tempFieldValue1.Replace("'", "''");
        string tempFieldValue2 = Convert.ToString(fieldValue2);
        tempFieldValue2 = tempFieldValue2.Replace("'", "''");
        string tempFieldValue3 = Convert.ToString(fieldValue3);
        tempFieldValue3 = tempFieldValue3.Replace("'", "''");
        whereCondition = string.Format(whereCondition, "DBO.TO_DATE('{0}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{1}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{2}','YYYY/MM/DD HH24:MI:SS')");
        whereCondition = string.Format(whereCondition, tempFieldValue1, tempFieldValue2, tempFieldValue3);
        this.whereCondition += whereCondition + " AND ";
    }
    public void Where(string whereCondition, DateTime fieldValue1, DateTime fieldValue2, DateTime fieldValue3, DateTime fieldValue4)
    {
        string tempFieldValue1 = Convert.ToString(fieldValue1);
        tempFieldValue1 = tempFieldValue1.Replace("'", "''");
        string tempFieldValue2 = Convert.ToString(fieldValue2);
        tempFieldValue2 = tempFieldValue2.Replace("'", "''");
        string tempFieldValue3 = Convert.ToString(fieldValue3);
        tempFieldValue3 = tempFieldValue3.Replace("'", "''");
        string tempFieldValue4 = Convert.ToString(fieldValue4);
        tempFieldValue4 = tempFieldValue4.Replace("'", "''");
        whereCondition = string.Format(whereCondition, "DBO.TO_DATE('{0}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{1}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{2}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{3}','YYYY/MM/DD HH24:MI:SS')");
        whereCondition = string.Format(whereCondition, tempFieldValue1, tempFieldValue2, tempFieldValue3, tempFieldValue4);
        this.whereCondition += whereCondition + " AND ";
    }
    public void Where(string whereCondition, DateTime fieldValue1, DateTime fieldValue2, DateTime fieldValue3, DateTime fieldValue4, DateTime fieldValue5)
    {
        string tempFieldValue1 = Convert.ToString(fieldValue1);
        tempFieldValue1 = tempFieldValue1.Replace("'", "''");
        string tempFieldValue2 = Convert.ToString(fieldValue2);
        tempFieldValue2 = tempFieldValue2.Replace("'", "''");
        string tempFieldValue3 = Convert.ToString(fieldValue3);
        tempFieldValue3 = tempFieldValue3.Replace("'", "''");
        string tempFieldValue4 = Convert.ToString(fieldValue4);
        tempFieldValue4 = tempFieldValue4.Replace("'", "''");
        string tempFieldValue5 = Convert.ToString(fieldValue5);
        tempFieldValue5 = tempFieldValue5.Replace("'", "''");
        whereCondition = string.Format(whereCondition, "DBO.TO_DATE('{0}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{1}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{2}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{3}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{4}','YYYY/MM/DD HH24:MI:SS')");
        whereCondition = string.Format(whereCondition, tempFieldValue1, tempFieldValue2, tempFieldValue3, tempFieldValue4, tempFieldValue5);
        this.whereCondition += whereCondition + " AND ";
    }
    public void Where(string whereCondition, DateTime fieldValue1, DateTime fieldValue2, DateTime fieldValue3, DateTime fieldValue4, DateTime fieldValue5, DateTime fieldValue6)
    {
        string tempFieldValue1 = Convert.ToString(fieldValue1);
        tempFieldValue1 = tempFieldValue1.Replace("'", "''");
        string tempFieldValue2 = Convert.ToString(fieldValue2);
        tempFieldValue2 = tempFieldValue2.Replace("'", "''");
        string tempFieldValue3 = Convert.ToString(fieldValue3);
        tempFieldValue3 = tempFieldValue3.Replace("'", "''");
        string tempFieldValue4 = Convert.ToString(fieldValue4);
        tempFieldValue4 = tempFieldValue4.Replace("'", "''");
        string tempFieldValue5 = Convert.ToString(fieldValue5);
        tempFieldValue5 = tempFieldValue5.Replace("'", "''");
        string tempFieldValue6 = Convert.ToString(fieldValue6);
        tempFieldValue6 = tempFieldValue6.Replace("'", "''");
        whereCondition = string.Format(whereCondition, "DBO.TO_DATE('{0}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{1}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{2}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{3}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{4}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{4}','YYYY/MM/DD HH24:MI:SS')");
        whereCondition = string.Format(whereCondition, tempFieldValue1, tempFieldValue2, tempFieldValue3, tempFieldValue4, tempFieldValue5, tempFieldValue6);
        this.whereCondition += whereCondition + " AND ";
    }
    public void Where(string fieldName, string condition, string fieldValue)
    {
        this.whereCondition += fieldName.ToUpper() + condition + "'" + fieldValue + "'" + " AND ";
    }
    public void Where(string fieldName, string condition, Guid? fieldValue)
    {
        this.whereCondition += fieldName.ToUpper() + condition + "'" + fieldValue + "'" + " AND ";
    }
    public void Where(string fieldName, string condition, Guid fieldValue)
    {
        this.whereCondition += fieldName.ToUpper() + condition + "'" + fieldValue + "'" + " AND ";
    }
    public void Where(string fieldName, string condition, decimal fieldValue)
    {
        this.whereCondition += fieldName.ToUpper() + condition + fieldValue + " AND ";
    }
    public void Where(string fieldName, string condition, DateTime fieldValue)
    {

        string tempFieldValue = Convert.ToString(fieldValue);
        tempFieldValue = tempFieldValue.Replace("'", "''");
        string tempValue = string.Empty;
        tempValue = fieldName.ToUpper() + condition + "DBO.TO_DATE('{0}','YYYY/MM/DD HH24:MI:SS')" + " AND ";
        tempValue = string.Format(tempValue, tempFieldValue);
        this.whereCondition += tempValue;
    }
    /// <summary>
    /// 对字段解密后进行比较
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="condition"></param>
    /// <param name="fieldValue"></param>
    public void WhereSecurity(string fieldName, string condition, string fieldValue)
    {
        this.whereCondition += "cast(decryptbykey(" + fieldName.ToUpper() + ") AS NVARCHAR(256))" + condition + "'" + fieldValue + "'" + " AND ";
    }
    /// <summary>
    /// 对字段解密后进行比较
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="condition"></param>
    /// <param name="fieldValue"></param>
    public void WhereSecurity(string fieldName, string condition, decimal fieldValue)
    {
        this.whereCondition += "cast(decryptbykey(" + fieldName.ToUpper() + ") AS NVARCHAR(256))" + condition + fieldValue + " AND ";
    }
    /// <summary>
    /// 对字段解密后进行比较
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="condition"></param>
    /// <param name="fieldValue"></param>
    public void WhereSecurity(string fieldName, string condition, int fieldValue)
    {
        this.whereCondition += "cast(decryptbykey(" + fieldName.ToUpper() + ") AS NVARCHAR(256))" + condition + fieldValue + " AND ";
    }
    #endregion

    #region 公有函数
    /// <summary>
    /// 返回SQL语句的WHERE部分，如：DELETE_FLAG='N'
    /// </summary>
    /// <returns></returns>
    public string GetWhere()
    {
        string result;
        if (string.IsNullOrEmpty(this.whereCondition))
        {
            result = "1=1";
        }
        else
        {
            result = this.whereCondition.Substring(0, this.whereCondition.Length - 5);
        }
        return result;
    }

    /// <summary>
    /// 获取完整SQL语句
    /// </summary>
    /// <returns></returns>
    public string GetSql()
    {
        if (isInitDefaultValue == true)
        {
            InitDefaultValues();
        }
        return sql + " AND " + GetWhere();
    }
    #endregion

    #region Private Method
    private void inset(string fieldName, string value)
    {
        sql = sql.Replace("{", "{{");
        sql = sql.Replace("}", "}}");
        int n = sql.IndexOf("SET WHERE");
        if (value == null)
        {
            if (n == -1)
            {
                n = sql.LastIndexOf(" WHERE");
                sql = sql.Insert(n, ", {0} = NULL ");
            }
            else
            {
                n = sql.LastIndexOf(" WHERE");
                sql = sql.Insert(n, " {0} = NULL ");
            }
            sql = string.Format(sql, fieldName.ToUpper());
        }
        else
        {
            if (n == -1)
            {
                n = sql.IndexOf(" WHERE");
                sql = sql.Insert(n, ", {0} = {1} ");
            }
            else // first time
            {
                n = sql.IndexOf(" WHERE");
                sql = sql.Insert(n, " {0} = {1} ");
            }
            sql = string.Format(sql, fieldName.ToUpper(), value);
        }
    }
    /// <summary>
    /// 初始化Update语句默认需要更新的值
    /// </summary>
    private void InitDefaultValues()
    {
        string tempSql = sql.ToUpper();
        //try
        //{
        //    if (UserContext.Current != null)
        //    {
        //        lastUpdBy = UserContext.Current.User_Id;
        //    }
        //}
        //catch { } 

        var lastUpdBy = UserContext.Current.User_Id;
        if (lastUpdBy != null)
            Set("UpdateBy", lastUpdBy);

        if (tempSql.IndexOf("[UpdateTime]") == -1)
        {
            Set("UpdateTime", Utility.GetSysDate());
        }
    }


    #endregion
}
