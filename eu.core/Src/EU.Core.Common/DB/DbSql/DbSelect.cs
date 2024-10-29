namespace EU.Core.Common;

public class DbSelect
{
    #region 属性
    private string tableNames;
    private string primaryTableAlias;
    private string companyId;
    private string selectItems;
    private string whereCondition;
    private string orderBy;
    private string groupBy;
    private bool isInitDefaultValue = true;
    /// <summary>
    /// 是否初始化默认查询条件，如CompanyId等
    /// </summary>
    public bool IsInitDefaultValue
    {
        get
        {
            return isInitDefaultValue;
        }
        set { isInitDefaultValue = value; }
    }
    #endregion

    #region 构造函数
    public DbSelect(string tableNames, string primaryTableAlias, string companyId)
    {
        this.tableNames = tableNames;
        this.primaryTableAlias = primaryTableAlias;
        this.companyId = companyId;
    }

    public DbSelect(string tableNames, string primaryTableAlias)
    {
        this.tableNames = tableNames;
        this.primaryTableAlias = primaryTableAlias;
        this.companyId = null;
    }
    #endregion

    #region Select
    /// <summary>
    /// 解密查询
    /// </summary>
    /// <param name="selectItem"></param>
    public void SelectSecurity(string selectItem)
    {
        string aliasName = selectItem.Substring(selectItem.IndexOf(".") + 1);
        this.selectItems += "cast(decryptbykey(" + selectItem + ") AS NVARCHAR(256))" + " AS " + aliasName + ",";
    }
    /// <summary>
    /// 解密查询
    /// </summary>
    /// <param name="selectItem"></param>
    /// <param name="aliasName"></param>
    public void SelectSecurity(string selectItem, string aliasName)
    {
        this.selectItems += "cast(decryptbykey(" + selectItem + ") AS NVARCHAR(256))" + " AS " + aliasName + ",";
    }
    /// <summary>
    /// 对加密字段求和
    /// </summary>
    /// <param name="selectItem"></param>
    public void SelectSecuritySum(string selectItem)
    {
        string aliasName = selectItem.Substring(selectItem.IndexOf(".") + 1);
        this.selectItems += "isnull(sum(cast (cast(decryptbykey(" + selectItem + ") AS NVARCHAR(256)) as decimal(20,6))),0)" + " AS " + aliasName + ",";
    }
    /// <summary>
    /// 对加密字段求和
    /// </summary>
    /// <param name="selectItem"></param>
    /// <param name="aliasName"></param>
    public void SelectSecuritySum(string selectItem, string aliasName)
    {
        this.selectItems += "isnull(sum(cast (cast(decryptbykey(" + selectItem + ") AS NVARCHAR(256)) as decimal(20,6))),0)" + " AS " + aliasName + ",";
    }

    public void Select(string selectItems)
    {
        this.selectItems += selectItems + ",";
    }
    public void Select(string selectItem, string aliasName)
    {
        this.selectItems += selectItem + " AS " + aliasName + ",";
    }
    public void Select(string selectItem, string aliasName, DateFormat dateFormat)
    {
        string tempValue = string.Empty;
        switch (dateFormat)
        {
            case DateFormat.Month:
                {
                    tempValue = "DBO.TO_CHAR(" + selectItem + ",'YYYY/MM')" + " AS " + aliasName + ",";
                    break;
                }
            case DateFormat.Day:
                {
                    tempValue = "DBO.TO_CHAR(" + selectItem + ",'YYYY/MM/DD')" + " AS " + aliasName + ",";
                    break;
                }
            case DateFormat.Hour:
                {
                    tempValue = "DBO.TO_CHAR(" + selectItem + ",'YYYY/MM/DD HH24')" + " AS " + aliasName + ",";
                    break;
                }
            case DateFormat.Minute:
                {
                    tempValue = "DBO.TO_CHAR(" + selectItem + ",'YYYY/MM/DD HH24:MI')" + " AS " + aliasName + ",";
                    break;
                }
            case DateFormat.Second:
                {
                    tempValue = "DBO.TO_CHAR(" + selectItem + ",'YYYY/MM/DD HH24:MI:SS')" + " AS " + aliasName + ",";
                    break;
                }
        }
        this.selectItems += tempValue;
    }
    private string GetInternalSelect()
    {
        string result;
        if (string.IsNullOrEmpty(this.selectItems))
        {
            result = "*";
        }
        else
        {
            result = this.selectItems.Substring(0, this.selectItems.Length - 1);
        }
        return result;
    }
    /// <summary>
    /// 返回SQL的SELECT部分，如：SELECT *
    /// </summary>
    /// <returns></returns>
    public string GetSelect()
    {
        string result;
        if (string.IsNullOrEmpty(this.selectItems))
        {
            result = "*";
        }
        else
        {
            result = this.selectItems.Substring(0, this.selectItems.Length - 1);
        }
        return "SELECT " + result;
    }
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
        //if (IsOracle)
        //{
        //    whereCondition = string.Format(whereCondition, "TO_DATE('{0}','YYYY/MM/DD HH24:MI:SS')");
        //}
        //else
        //{
        //    //whereCondition = string.Format(whereCondition, "DBO.TO_DATE('{0}','YYYY/MM/DD HH24:MI:SS')");
        //    whereCondition = string.Format(whereCondition, "'{0}'");
        //}
        whereCondition = string.Format(whereCondition, "'{0}'");
        whereCondition = string.Format(whereCondition, tempFieldValue);
        this.whereCondition += whereCondition + " AND ";
    }
    public void Where(string whereCondition, DateTime fieldValue1, DateTime fieldValue2)
    {
        string tempFieldValue1 = Convert.ToString(fieldValue1);
        tempFieldValue1 = tempFieldValue1.Replace("'", "''");
        string tempFieldValue2 = Convert.ToString(fieldValue2);
        tempFieldValue2 = tempFieldValue2.Replace("'", "''");
        //if (IsOracle)
        //{
        //    whereCondition = string.Format(whereCondition, "TO_DATE('{0}','YYYY/MM/DD HH24:MI:SS')", "TO_DATE('{1}','YYYY/MM/DD HH24:MI:SS')");
        //}
        //else
        //{
        //    //whereCondition = string.Format(whereCondition, "DBO.TO_DATE('{0}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{1}','YYYY/MM/DD HH24:MI:SS')");
        //    whereCondition = string.Format(whereCondition, "'{0}'", "'{1}'");
        //}
        whereCondition = string.Format(whereCondition, "'{0}'", "'{1}'");
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
        //if (IsOracle)
        //{
        //    whereCondition = string.Format(whereCondition, "TO_DATE('{0}','YYYY/MM/DD HH24:MI:SS')", "TO_DATE('{1}','YYYY/MM/DD HH24:MI:SS')", "TO_DATE('{2}','YYYY/MM/DD HH24:MI:SS')");
        //}
        //else
        //{
        //    //whereCondition = string.Format(whereCondition, "DBO.TO_DATE('{0}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{1}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{2}','YYYY/MM/DD HH24:MI:SS')");
        //    whereCondition = string.Format(whereCondition, "'{0}'", "'{1}'", "'{2}'");
        //}
        whereCondition = string.Format(whereCondition, "'{0}'", "'{1}'", "'{2}'");
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
        //if (IsOracle)
        //{
        //    whereCondition = string.Format(whereCondition, "TO_DATE('{0}','YYYY/MM/DD HH24:MI:SS')", "TO_DATE('{1}','YYYY/MM/DD HH24:MI:SS')", "TO_DATE('{2}','YYYY/MM/DD HH24:MI:SS')", "TO_DATE('{3}','YYYY/MM/DD HH24:MI:SS')");
        //}
        //else
        //{
        //    //whereCondition = string.Format(whereCondition, "DBO.TO_DATE('{0}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{1}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{2}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{3}','YYYY/MM/DD HH24:MI:SS')");
        //    whereCondition = string.Format(whereCondition, "'{0}'", "'{1}'", "'{2}'", "'{3}'");
        //}
        whereCondition = string.Format(whereCondition, "'{0}'", "'{1}'", "'{2}'", "'{3}'");
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
        //if (IsOracle)
        //{
        //    whereCondition = string.Format(whereCondition, "TO_DATE('{0}','YYYY/MM/DD HH24:MI:SS')", "TO_DATE('{1}','YYYY/MM/DD HH24:MI:SS')", "TO_DATE('{2}','YYYY/MM/DD HH24:MI:SS')", "TO_DATE('{3}','YYYY/MM/DD HH24:MI:SS')", "TO_DATE('{4}','YYYY/MM/DD HH24:MI:SS')");
        //}
        //else
        //{
        //    //whereCondition = string.Format(whereCondition, "DBO.TO_DATE('{0}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{1}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{2}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{3}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{4}','YYYY/MM/DD HH24:MI:SS')");
        //    whereCondition = string.Format(whereCondition, "'{0}'", "'{1}'", "'{2}'", "'{3}'", "'{4}'");
        //}
        whereCondition = string.Format(whereCondition, "'{0}'", "'{1}'", "'{2}'", "'{3}'", "'{4}'");

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
        //if (IsOracle)
        //{
        //    whereCondition = string.Format(whereCondition, "TO_DATE('{0}','YYYY/MM/DD HH24:MI:SS')", "TO_DATE('{1}','YYYY/MM/DD HH24:MI:SS')", "TO_DATE('{2}','YYYY/MM/DD HH24:MI:SS')", "TO_DATE('{3}','YYYY/MM/DD HH24:MI:SS')", "TO_DATE('{4}','YYYY/MM/DD HH24:MI:SS')", "TO_DATE('{5}','YYYY/MM/DD HH24:MI:SS')");
        //}
        //else
        //{
        //    //whereCondition = string.Format(whereCondition, "DBO.TO_DATE('{0}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{1}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{2}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{3}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{4}','YYYY/MM/DD HH24:MI:SS')", "DBO.TO_DATE('{4}','YYYY/MM/DD HH24:MI:SS')");
        //    whereCondition = string.Format(whereCondition, "'{0}'", "'{1}'", "'{2}'", "'{3}'", "'{4}'", "'{5}'");
        //}
        whereCondition = string.Format(whereCondition, "'{0}'", "'{1}'", "'{2}'", "'{3}'", "'{4}'", "'{5}'");
        whereCondition = string.Format(whereCondition, tempFieldValue1, tempFieldValue2, tempFieldValue3, tempFieldValue4, tempFieldValue5, tempFieldValue6);
        this.whereCondition += whereCondition + " AND ";
    }
    public void Where(string fieldName, string condition, string fieldValue)
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
    public void Where(string fieldName, string condition, int fieldValue)
    {
        this.whereCondition += fieldName.ToUpper() + condition + fieldValue + " AND ";
    }
    public void Where(string fieldName, string condition, bool fieldValue)
    {
        this.whereCondition += fieldName.ToUpper() + condition + "'" + fieldValue + "'" + " AND ";
    }
    public void Where(string fieldName, string condition, DateTime fieldValue)
    {

        string tempFieldValue = Convert.ToString(fieldValue);
        tempFieldValue = tempFieldValue.Replace("'", "''");
        string tempValue = string.Empty;
        //if (IsOracle)
        //{
        //    tempValue = fieldName.ToUpper() + condition + "TO_DATE('{0}','YYYY/MM/DD HH24:MI:SS')" + " AND ";
        //}
        //else
        //{
        //    //tempValue = fieldName.ToUpper() + condition + "DBO.TO_DATE('{0}','YYYY/MM/DD HH24:MI:SS')" + " AND ";
        //    tempValue = fieldName.ToUpper() + condition + "'{0}'" + " AND ";
        //}
        tempValue = fieldName.ToUpper() + condition + "'{0}'" + " AND ";
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
    private string GetInternalWhere()
    {
        string result;
        #region 不初始化默认值
        if (IsInitDefaultValue == false)
        {
            if (string.IsNullOrEmpty(this.whereCondition))
            {
                if (string.IsNullOrEmpty(companyId))
                {
                    result = "1=1";
                }
                else
                {
                    result = primaryTableAlias + ".CompanyId='" + companyId + "'";
                }
            }
            else
            {
                if (string.IsNullOrEmpty(companyId))
                {
                    result = this.whereCondition.Substring(0, this.whereCondition.Length - 5);
                }
                else
                {
                    result = this.whereCondition + primaryTableAlias + ".CompanyId='" + companyId + "'";
                }
            }
        }
        #endregion
        #region 初始化默认值
        else
        {
            if (string.IsNullOrEmpty(this.whereCondition))
            {
                if (string.IsNullOrEmpty(companyId))
                {
                    result = "IsActive='true' AND IsDeleted='false'";
                }
                else
                {
                    result = primaryTableAlias + ".CompanyId='" + companyId + "' AND " + primaryTableAlias + ".IsActive='true' AND " + primaryTableAlias + ".IsDeleted='false'";
                }
            }
            else
            {
                if (string.IsNullOrEmpty(companyId))
                {
                    result = this.whereCondition + primaryTableAlias + ".IsActive='true' AND " + primaryTableAlias + ".IsDeleted='false'";
                }
                else
                {
                    result = this.whereCondition + primaryTableAlias + ".CompanyId='" + companyId + "' AND " + primaryTableAlias + ".IsActive='true' AND " + primaryTableAlias + ".IsDeleted='false'";
                }
            }
        }
        #endregion
        return result;
    }
    /// <summary>
    /// 返回SQL的WHERE部分，如：IsActive='true'
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
    #endregion

    #region 排序
    public void OrderBy(string fieldName, string direction)
    {

        string tempOrderBy = fieldName + " " + direction + ",";
        this.orderBy += tempOrderBy;
    }
    private string GetOrderBy()
    {
        string result = string.Empty;
        if (!string.IsNullOrEmpty(this.orderBy))
        {
            return " ORDER BY " + this.orderBy.Substring(0, this.orderBy.Length - 1);
        }
        else
        {
            return result;
        }
    }
    #endregion

    #region 分组
    public void GroupBy(string fieldName)
    {
        this.groupBy += fieldName + ",";
    }
    private string GetGroupBy()
    {
        string result = string.Empty;
        if (!string.IsNullOrEmpty(this.groupBy))
        {
            return " GROUP BY " + this.groupBy.Substring(0, this.groupBy.Length - 1);
        }
        else
        {
            return result;
        }
    }
    #endregion

    #region 返回SQL语句
    /// <summary>
    /// 返回SQL语句
    /// </summary>
    /// <returns></returns>
    public string GetSql()
    {
        string result = string.Empty;
        result = "SELECT " + GetInternalSelect() + " FROM " + tableNames + " WHERE " + GetInternalWhere() + GetGroupBy() + GetOrderBy();
        return result;
    }
    #endregion
}

public enum DateFormat
{
    Year,
    Quarter,
    /// <summary>
    /// YYYY/MM
    /// </summary>
    Month,
    Week,
    /// <summary>
    /// YYYY/MM/DD
    /// </summary>
    Day,
    /// <summary>
    /// YYYY/MM/DD HH24
    /// </summary>
    Hour,
    /// <summary>
    /// YYYY/MM/DD HH24:MI
    /// </summary>
    Minute,
    /// <summary>
    /// YYYY/MM/DD HH24:MI:SS
    /// </summary>
    Second
}
