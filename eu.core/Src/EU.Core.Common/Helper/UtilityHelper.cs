using System.Data;
using EU.Core.Common.Caches;
using EU.Core.Common.Enums;
using EU.Core.Common.Extensions;
using EU.Core.Common.Module;
using EU.Core.Model;
using EU.Core.Model.Models;
using EU.Core.Module;
using Newtonsoft.Json;
using SqlSugar;
using UAParser;

namespace EU.Core.Common.Helper;

/// <summary>
/// 方法类
/// </summary>
public partial class Utility
{

    #region DataTable转Tree
    /// <summary>
    /// DataTable转Tree
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="userId">用户ID</param>
    /// <param name="dt">DataTable</param>
    /// <returns></returns>
    public static DataTable FormatDataTableForTree(string moduleCode, string userId, DataTable dt)
    {
        ModuleSqlColumn moduleColumnInfo = new(moduleCode);
        var dvModuleColumns = moduleColumnInfo.GetModuleSqlColumn();

        var List = dvModuleColumns.Where(x => x.DataIndex == "ID").ToList();

        if (List.Count == 0)
        {
            var SmModuleColumn = new SmModuleColumnExtend();
            SmModuleColumn.DataIndex = "ID";
            dvModuleColumns.Add(SmModuleColumn);
        }

        string columnName = string.Empty;
        string valueType = string.Empty;
        string dateFormat = string.Empty;
        string value = string.Empty;
        bool IsBool = false;

        var dtTree = new DataTable();
        DataRow drTree = null;

        if (dvModuleColumns != null && dvModuleColumns.Count > 0)
            for (int i = 0; i < dvModuleColumns.Count; i++)
            {
                columnName = dvModuleColumns[i].DataIndex;
                dtTree.Columns.Add(columnName, typeof(string));
            }

        for (int i = 0; i < dt.Rows.Count; i++)
        {
            drTree = dtTree.NewRow();
            if (dvModuleColumns != null && dvModuleColumns.Count > 0)
            {
                for (int j = 0; j < dvModuleColumns.Count; j++)
                {
                    columnName = dvModuleColumns[j].DataIndex;
                    valueType = dvModuleColumns[j].ValueType;
                    dateFormat = dvModuleColumns[j].DataFormate;
                    if (dt.Columns.Contains(columnName))
                        value = dt.Rows[i][columnName].ToString();

                    if (dvModuleColumns[j].IsBool != null && dvModuleColumns[j].IsBool.Value)
                        IsBool = dvModuleColumns[j].IsBool.Value;
                    else
                        IsBool = false;
                    if ((valueType == "date" || valueType == "dateTime" || valueType == "time") && !string.IsNullOrEmpty(dateFormat))
                    {
                        switch (dateFormat)
                        {
                            case "Y/m":
                                value = DateTimeHelper.ConvertToYearMonthString(value);
                                break;
                            case "Y-m":
                                value = DateTimeHelper.ConvertToYearMonthString1(value);
                                break;
                            case "Y/m/d":
                                value = DateTimeHelper.ConvertToDayString(value);
                                break;
                            case "Y/m/d H":
                                value = DateTimeHelper.ConvertToHourString(value);
                                break;
                            case "Y/m/d H:i":
                                value = DateTimeHelper.ConvertToMiniuteString(value);
                                break;
                            case "Y/m/d H:i:s":
                                value = DateTimeHelper.ConvertToSecondString(value);
                                break;
                            case "H:i":
                                value = DateTimeHelper.ConvertToOnlyHourMinuteString(value);
                                break;
                            default:
                                break;
                        }
                    }
                    else if (valueType == "digit" && !string.IsNullOrEmpty(dateFormat))
                    {
                        if (string.IsNullOrEmpty(dateFormat) || dateFormat == "-1")
                        {
                            value = StringHelper.TrimDecimalString(value, -1);
                        }
                        else
                        {
                            value = StringHelper.TrimDecimalString(value, Convert.ToInt32(dateFormat));
                        }
                    }
                    if (IsBool)
                    {
                        if (value == "True")
                            drTree[columnName] = "true";
                        else
                            drTree[columnName] = "false";
                    }
                    else
                        drTree[columnName] = value;
                }
            }
            dtTree.Rows.Add(drTree);
        }

        #region 处理合计
        var module = ModuleInfo.GetModuleInfo(moduleCode);
        if (module != null && module.IsSum != null && module.IsSum.Value)
        {
            var sumColumns = dvModuleColumns.Where(o => o.IsSum == true && (o.ValueType == "digit" || o.ValueType == "money")).ToList();
            if (!sumColumns.IsNullOrEmpty() && sumColumns.Any())
            {
                drTree = dtTree.NewRow();
                drTree["ID"] = "SumRowID";

                for (int j = 0; j < sumColumns.Count; j++)
                {
                    decimal sum = 0;
                    columnName = sumColumns[j].DataIndex;
                    valueType = sumColumns[j].ValueType;
                    dateFormat = sumColumns[j].DataFormate;
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        value = dt.Rows[i][columnName].ToString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            if (string.IsNullOrEmpty(dateFormat) || dateFormat == "-1")
                                value = StringHelper.TrimDecimalString(value, -1);
                            else
                                value = StringHelper.TrimDecimalString(value, Convert.ToInt32(dateFormat));
                            sum += Convert.ToDecimal(value);
                        }
                    }
                    drTree[columnName] = sum;
                }
                dtTree.Rows.Add(drTree);
            }
        }
        #endregion

        return dtTree;
    }
    #endregion

    #region 求系统当前日期
    /// <summary>
    /// 求系统当前日期（数据库所在服务器的日期）。
    /// </summary>
    /// <returns></returns>
    public static DateTime GetSysDate()
    {
        return DateTime.Now;
    }
    #endregion

    #region 求系统唯一字符串
    /// <summary>
    /// 求系统唯一字符串，常用于ROW_ID值。
    /// </summary>
    /// <returns>字符串</returns>
    public static string GetSysID()
    {
        string sid = string.Empty;

        byte[] buffer = Guid.NewGuid().ToByteArray();
        sid = DateTime.Now.ToString("yyMMddHHmmss") + BitConverter.ToInt64(buffer, 0).ToString();
        return sid;
    }
    #endregion

    #region 获得当前公司ID
    /// <summary>
    /// 获得当前公司ID
    /// </summary>
    /// <returns></returns>
    public static string GetCompanyId()
    {
        try
        {
            return GetCompanyGuidId().ToString();
        }
        catch (Exception)
        {
            throw;
        }
    }
    /// <summary>
    /// 获得当前公司ID
    /// </summary>
    /// <returns></returns>
    public static Guid GetCompanyGuidId()
    {
        try
        {
            return Guid.Parse("e26f359a-4983-42d8-8769-19ddec5b7d23");
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region 获得当前用户ID
    /// <summary>
    /// 获得当前用户ID
    /// </summary>
    /// <returns></returns>
    public static string GetUserIdString()
    {
        try
        {
            var userId = GetUserId();
            if (userId is null)
                return null;
            return userId.ToString();
        }
        catch (Exception)
        {
            throw;
        }
    }
    /// <summary>
    /// 获得当前用户ID
    /// </summary>
    /// <returns></returns>
    public static Guid? GetUserId()
    {
        try
        {
            return App.User?.ID;
        }
        catch (Exception)
        {
            return null;
        }
    }
    #endregion

    #region 获得当前集团ID
    /// <summary>
    /// 获得当前集团ID
    /// </summary>
    /// <returns></returns>
    public static string GetGroupId()
    {
        try
        {
            return GetGroupGuidId().ToString();
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// 获得当前集团ID
    /// </summary>
    /// <returns></returns>
    public static Guid GetGroupGuidId()
    {
        try
        {
            return Guid.Parse("e26f359a-4983-42d8-8769-19ddec5b7d23");
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region 清空Redis缓存
    /// <summary>
    /// 清空Redis缓存
    /// </summary>
    /// <param name="moduleCode"></param>
    public static void ClearCache()
    {
        try
        {
            RedisCacheService di = new();
            di.Clear();
        }
        catch (Exception) { throw; }
    }
    #endregion

    #region 重新初始化缓存
    /// <summary>
    /// 重新初始化缓存
    /// </summary>
    /// <param name="moduleCode"></param>
    public static void ReInitCache()
    {
        try
        {
            new RedisCacheService(1).Clear();
            new RedisCacheService(2).Clear();
            new RedisCacheService(3).Clear();
            new RedisCacheService(4).Clear();

            #region 初始化缓存
            ModuleInfo.Init();
            ModuleSql.Init();
            ModuleSqlColumn.Init();
            LovHelper.Init();
            LovHelper.InitCommonListSql();
            ConfigCache.Init();
            FunctionPrivilege.Init();
            #endregion
        }
        catch (Exception) { throw; }
    }
    #endregion

    #region 检查表中是否已经存在相同代码的数据
    /// <summary>
    /// 检查表中是否已经存在相同代码的数据
    /// </summary>
    /// <param name="companyId">公司ID</param>
    /// <param name="tableName">表名</param>
    /// <param name="fieldName">字段名</param>
    /// <param name="fieldValue">字段值</param>
    /// <param name="modifyType">ModifyType.Add,ModifyType.Edit</param>
    /// <param name="rowid">ModifyType.Edit时修改记录的ROW_ID值</param>
    /// <param name="promptName">判断栏位的提示名称</param>
    public static void CheckCodeExist(string companyId, string tableName, string fieldName, string fieldValue, ModifyType modifyType, string rowid, string promptName)
    {
        try
        {
            CheckCodeExist(companyId, tableName, fieldName, fieldValue, modifyType, rowid, promptName, null);
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// 检查表中是否已经存在相同代码的数据
    /// </summary>
    /// <param name="companyId">公司ID</param>
    /// <param name="tableName">表名</param>
    /// <param name="fieldName">字段名</param>
    /// <param name="fieldValue">字段值</param>
    /// <param name="whereCondition">条件</param>
    /// <param name="modifyType">ModifyType.Add,ModifyType.Edit</param>
    /// <param name="rowid">ModifyType.Edit时修改记录的ROW_ID值</param>
    /// <param name="promptName">判断栏位的提示名称</param>
    /// <param name="whereCondition">Where后的条件，如：IS_ALCON='Y'</param>
    public static bool CheckCodeExist(string companyId, string tableName, string fieldName, string fieldValue, ModifyType modifyType, string rowid, string promptName, string whereCondition)
    {
        try
        {
            bool result = false;
            if (modifyType == ModifyType.Add)
            {
                string sql = string.Empty;
                if (string.IsNullOrEmpty(companyId))
                    sql = "SELECT COUNT(*) FROM " + tableName + " WHERE " + fieldName + "='" + fieldValue + "' AND IsDeleted='false' ";
                else
                    sql = "SELECT COUNT(*) FROM " + tableName + " WHERE " + fieldName + "='" + fieldValue + "' AND CompanyId='" + companyId + "' AND IsDeleted='false' ";
                if (!string.IsNullOrEmpty(whereCondition))
                    sql += " AND " + whereCondition;

                int count = Convert.ToInt32(DBHelper.ExecuteScalar(sql));
                if (count > 0)
                {
                    result = true;
                    throw new Exception(string.Format("{0}【{1}】已经存在！", promptName, fieldValue));
                }
                else
                    result = false;
            }
            else if (modifyType == ModifyType.Edit)
            {
                string sql = string.Empty;
                if (string.IsNullOrEmpty(companyId))
                {
                    sql = "SELECT COUNT(*) FROM " + tableName + " WHERE " + fieldName + "='" + fieldValue + "' AND IsDeleted='false' AND ID!='" + rowid + "'";
                }
                else
                {
                    sql = "SELECT COUNT(*) FROM " + tableName + " WHERE " + fieldName + "='" + fieldValue + "' AND CompanyId='" + companyId + "' AND IsDeleted='false' AND ID!='" + rowid + "'";
                }
                if (!string.IsNullOrEmpty(whereCondition))
                {
                    sql += " AND " + whereCondition;
                }
                int count = Convert.ToInt32(DBHelper.ExecuteScalar(sql));
                if (count > 0)
                {
                    result = true;
                    throw new Exception(string.Format("{0}【{1}】已经存在！", promptName, fieldValue));
                }
                else
                    result = false;

            }
            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region 自动产生序列号(不一定是连续的，但永远不会重复)
    /// <summary>
    /// 自动产生序列号(不一定是连续的，但永远不会重复)
    /// </summary>
    /// <param name="sequenceCode">规则代码</param>
    /// <param name="trans"></param>
    /// <returns></returns>
    //public static string GenerateSequence(string sequenceCode, bool trans = false)
    //{
    //    try
    //    {
    //        #region 变量定义
    //        string result = string.Empty;
    //        string prefix = string.Empty;
    //        int length = 0;
    //        int numberLength = 0;
    //        int prefixLength = 0;
    //        #endregion

    //        DbSelect dsSequenceSetup = new DbSelect("SmAutoCode A", "A");
    //        dsSequenceSetup.IsInitDefaultValue = false;
    //        dsSequenceSetup.Select("A.Prefix,A.NumberLength,A.TableName,A.ColumnName");
    //        dsSequenceSetup.Where("A.NumberCode", "=", sequenceCode);
    //        DataTable dtSequenceSetup = DBHelper.Instance.GetDataTable(dsSequenceSetup.GetSql());
    //        if (dtSequenceSetup.Rows.Count > 0)
    //        {
    //            prefix = dtSequenceSetup.Rows[0]["PREFIX"].ToString();
    //            if (!string.IsNullOrEmpty(prefix))
    //            {
    //                prefixLength = prefix.Length;
    //            }
    //            length = Convert.ToInt32(dtSequenceSetup.Rows[0]["NumberLength"]);
    //            string tableCode = dtSequenceSetup.Rows[0]["TableName"].ToString();
    //            string columnCode = dtSequenceSetup.Rows[0]["ColumnName"].ToString();
    //            numberLength = length - prefix.Length;

    //            var param = new DynamicParameters();
    //            param.Add("@tableCode", tableCode);
    //            param.Add("@columnCode", columnCode);
    //            param.Add("@value", 32);
    //            //var param = new
    //            //{
    //            //    tableCode = tableCode,
    //            //    columnCode = columnCode
    //            //};
    //            string maxSequence1 = (string)DBHelper.Instance.ExecuteScalar("p_get_seq", param, CommandType.StoredProcedure, trans);
    //            string maxSequence = param.Get<string>("@value");
    //            //StoredProcedure store = DBHelper.GetStoredProcedure("p_get_seq");
    //            //store.AddInParameter("tableCode", DbType.String, tableCode);
    //            //store.AddInParameter("columnCode", DbType.String, columnCode);
    //            //store.AddOutParameter("value", DbType.String, 32);
    //            //DBHelper.ExecuteStoredProcedure(store, trans);
    //            //string maxSequence = (string)store.GetParameterValue("@value");

    //            //if (string.IsNullOrEmpty(maxSequence))
    //            //{
    //            //    result = prefix + Convert.ToString(1).PadLeft(numberLength, '0');
    //            //}
    //            //else
    //            //{
    //            //    result = prefix + maxSequence.PadLeft(numberLength, '0');
    //            //}
    //        }
    //        return result;
    //    }
    //    catch (Exception E)
    //    {
    //        throw E;
    //    }
    //}

    /// <summary>
    /// 自动产生连续的序列号（使用此函数时，一定要把存放此Sequence的列设为Unique）
    /// </summary>
    /// <param name="sequenceCode">规则代码</param>
    /// <returns>新的序列号</returns>
    public static string GenerateContinuousSequence(string sequenceCode, bool trans = false)
    {
        try
        {
            return GenerateContinuousSequence(sequenceCode, "", trans);
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// 自动产生连续的序列号（使用此函数时，一定要把存放此Sequence的列设为Unique）
    /// </summary>
    /// <param name="sequenceCode">规则代码</param>
    /// <param name="prefix">代码前缀</param>
    /// <returns>新的序列号</returns>
    public static string GenerateContinuousSequence(string sequenceCode, string prefix, bool trans = false)
    {
        try
        {
            #region 变量定义
            string result = string.Empty;
            string prefixTemp = string.Empty;
            int length = 0;     //设定字符串长度
            int tempLength = 0; //设定字符串临时长度
                                //int numberLength = 0;
                                //int prefixLength = 0;
            string tableCode = string.Empty;
            string columnCode = string.Empty;
            string dataFormatType = string.Empty;
            string dateString = string.Empty;
            int sequence;
            #endregion

            DbSelect dsSequenceSetup = new("SmAutoCode A", "A");
            dsSequenceSetup.IsInitDefaultValue = false;
            dsSequenceSetup.Select("A.Prefix,A.NumberLength,A.TableName,A.ColumnName,A.DateFormatType");
            dsSequenceSetup.Where("A.NumberCode", "=", sequenceCode);
            var dtSequenceSetup = DBHelper.GetDataTable(dsSequenceSetup.GetSql());
            if (dtSequenceSetup.Rows.Count > 0)
            {
                //设定字符串长度
                length = Convert.ToInt32(dtSequenceSetup.Rows[0]["NumberLength"]);

                #region 字符前添加固定字符
                prefixTemp = dtSequenceSetup.Rows[0]["Prefix"].ToString();
                if (!string.IsNullOrEmpty(prefix))
                {
                    prefixTemp = prefix + prefixTemp;
                    length = length + prefix.Length;
                }
                tempLength = length;
                if (!string.IsNullOrEmpty(prefixTemp))
                {
                    tempLength = tempLength - prefixTemp.Length;
                    result = prefixTemp;
                }
                #endregion

                #region 增长日期格式
                dataFormatType = dtSequenceSetup.Rows[0]["DateFormatType"].ToString();
                if (!string.IsNullOrEmpty(dataFormatType))
                {
                    if (dataFormatType == "YYYYMMDDHHMM")
                        dateString = DateTime.Now.ToString("yyyyMMddhhmm");
                    else if (dataFormatType == "YYYYMMDDHH")
                        dateString = DateTime.Now.ToString("yyyyMMddhh");
                    else if (dataFormatType == "YYYYMMDD")
                        dateString = DateTime.Now.ToString("yyyyMMdd");
                    else if (dataFormatType == "YYYYMM")
                        dateString = DateTime.Now.ToString("yyyyMM");
                    else if (dataFormatType == "YYYY")
                        dateString = DateTime.Now.ToString("yyyy");
                }
                result += dateString;
                tempLength = tempLength - dateString.Length;
                #endregion

                tableCode = dtSequenceSetup.Rows[0]["TableName"].ToString();
                columnCode = dtSequenceSetup.Rows[0]["ColumnName"].ToString();
                #region 查询
                DbSelect dbSelect = new(tableCode + " A", "A", null);
                dbSelect.IsInitDefaultValue = false;
                //if (string.IsNullOrEmpty(dateString))
                //{
                //    dbSelect.Select("MAX(A." + columnCode + ")");
                //    //dbSelect.Select("MAX(CONVERT(DECIMAL,SUBSTRING(A.ISSUE_NO," + (prefix.Length + dateString.Length + 1).ToString() + "," + tempLength.ToString() + ")))");
                //}
                //else
                //{
                if (!string.IsNullOrEmpty(prefixTemp) || !string.IsNullOrEmpty(dateString))
                    dbSelect.Select("MAX(SUBSTRING(A." + columnCode + "," + (prefixTemp.Length + dateString.Length + 1).ToString() + "," + tempLength.ToString() + "))");
                else
                    dbSelect.Select("MAX(A." + columnCode + ")");
                //}
                //dbSelect.Select("MAX(CONVERT(DECIMAL,SUBSTRING(A.ISSUE_NO," + (prefix.Length + dateString.Length + 1).ToString() + "," + tempLength.ToString() + ")))");
                if (!string.IsNullOrEmpty(prefixTemp) || !string.IsNullOrEmpty(dateString))
                    dbSelect.Where("SUBSTRING(A." + columnCode + ",1," + (prefixTemp.Length + dateString.Length).ToString() + ")", " = ", prefixTemp + dateString);
                dbSelect.Where("LEN(A." + columnCode + ")", "=", length);
                string maxSequence = Convert.ToString(DBHelper.ExecuteScalar(dbSelect.GetSql(), null, null, trans));
                #endregion
                //tempLength = tempLength - dateString.Length;
                if (string.IsNullOrEmpty(maxSequence))
                    result = prefixTemp + dateString + Convert.ToString(1).PadLeft(tempLength, '0');
                else
                {
                    if (!string.IsNullOrEmpty(prefixTemp) || !string.IsNullOrEmpty(dateString))
                    {
                        if (int.TryParse(maxSequence, out sequence))
                        {
                            sequence += 1;
                            if (sequence.ToString().Length > tempLength)
                                throw new Exception("自动生成字串长度已经超过设定长度!");
                        }
                        else
                            throw new Exception("表中的数据无法进行自动编号,请联系软件开发商!");
                        result = prefixTemp + dateString + sequence.ToString().PadLeft(tempLength, '0');
                    }
                    else
                    {
                        if (int.TryParse(maxSequence, out sequence))
                        {
                            sequence += 1;
                            if (sequence.ToString().Length > length)
                                throw new Exception("自动生成字串长度已经超过设定长度!");
                        }
                        else
                            throw new Exception("表中的数据无法进行自动编号,请联系软件开发商!");
                        result = sequence.ToString().PadLeft(length, '0');
                    }
                }
            }
            else
                throw new Exception("自动编号代码：" + sequenceCode + "没有设置！");
            return result;
        }
        catch (Exception) { throw; }
    }

    public static string GenerateContinuousSequence(string tableCode, string columnCode, string prefix, int length, bool trans = false)
    {
        try
        {
            #region 变量定义
            string result = string.Empty;
            int tempLength = 0; //设定字符串临时长度
            int sequence;
            #endregion
            tempLength = length - prefix.Length;
            DbSelect dbSelect = new(tableCode + " A", "A", null);
            dbSelect.IsInitDefaultValue = false;
            if (!string.IsNullOrEmpty(prefix))
                dbSelect.Select("MAX(SUBSTRING(A." + columnCode + "," + (prefix.Length + 1).ToString() + "," + tempLength.ToString() + "))");
            else
                dbSelect.Select("MAX(A." + columnCode + ")");
            if (!string.IsNullOrEmpty(prefix))
                dbSelect.Where("SUBSTRING(A." + columnCode + ",1," + (prefix.Length).ToString() + ")", " = ", prefix);
            dbSelect.Where("LEN(A." + columnCode + ")", "=", length);
            string maxSequence = Convert.ToString(DBHelper.ExecuteScalar(dbSelect.GetSql(), trans));
            if (string.IsNullOrEmpty(maxSequence))
                result = prefix + Convert.ToString(1).PadLeft(tempLength, '0');
            else
            {
                if (!string.IsNullOrEmpty(prefix))
                {
                    if (int.TryParse(maxSequence, out sequence))
                    {
                        sequence += 1;
                        if (sequence.ToString().Length > tempLength)
                            throw new Exception("自动生成字串长度已经超过设定长度!");
                    }
                    else
                        throw new Exception("表中的数据无法进行自动编号,请联系软件开发商!");
                    result = prefix + sequence.ToString().PadLeft(tempLength, '0');
                }
                else
                {
                    if (int.TryParse(maxSequence, out sequence))
                    {
                        sequence += 1;
                        if (sequence.ToString().Length > length)
                            throw new Exception("自动生成字串长度已经超过设定长度!");
                    }
                    else
                        throw new Exception("表中的数据无法进行自动编号,请联系软件开发商!");
                    result = sequence.ToString().PadLeft(length, '0');
                }
            }
            return result;
        }
        catch (Exception) { throw; }
    }

    public static int GenerateContinuousSequence(string tableCode, string columnCode, string fieldName = null, string fieldValue = null, bool trans = false)
    {
        try
        {
            #region 变量定义
            int sequence = 0;
            #endregion
            DbSelect dbSelect = new(tableCode + " A", "A", null);
            dbSelect.IsInitDefaultValue = false;
            dbSelect.Select("MAX(A." + columnCode + ")");
            if (!string.IsNullOrEmpty(fieldName) && !string.IsNullOrEmpty(fieldValue))
                dbSelect.Where(fieldName, " = ", fieldValue);
            dbSelect.Where("IsDeleted", " = ", false);

            string maxSequence = Convert.ToString(DBHelper.ExecuteScalar(dbSelect.GetSql(), trans));
            if (string.IsNullOrEmpty(maxSequence))
                maxSequence = "0";
            if (int.TryParse(maxSequence, out sequence))
                sequence += 1;
            else
                throw new Exception("表中的数据无法进行自动编号,请联系软件开发商!");
            return sequence;
        }
        catch (Exception) { throw; }
    }

    /// <summary>
    /// 自动产生连续的序列号（使用此函数时，一定要把存放此Sequence的列设为Unique）
    /// </summary>
    /// <param name="sequenceCode">规则代码</param>
    /// <returns>新的序列号</returns>
    public static async Task<string> GenerateContinuousSequence(ISqlSugarClient Db, string sequenceCode)
    {
        try
        {
            return await GenerateContinuousSequence(Db, sequenceCode, "");
        }
        catch (Exception)
        {
            throw;
        }
    }


    /// <summary>
    /// 自动产生连续的序列号（使用此函数时，一定要把存放此Sequence的列设为Unique）
    /// </summary>
    /// <param name="sequenceCode">规则代码</param>
    /// <param name="prefix">代码前缀</param>
    /// <returns>新的序列号</returns>
    public static async Task<string> GenerateContinuousSequence(ISqlSugarClient Db, string sequenceCode, string prefix)
    {
        try
        {
            #region 变量定义
            string result = string.Empty;
            string prefixTemp = string.Empty;
            int length = 0;     //设定字符串长度
            int tempLength = 0; //设定字符串临时长度
                                //int numberLength = 0;
                                //int prefixLength = 0;
            string tableCode = string.Empty;
            string columnCode = string.Empty;
            string dataFormatType = string.Empty;
            string dateString = string.Empty;
            int sequence;
            #endregion

            DbSelect dsSequenceSetup = new("SmAutoCode A", "A");
            dsSequenceSetup.IsInitDefaultValue = false;
            dsSequenceSetup.Select("A.Prefix,A.NumberLength,A.TableName,A.ColumnName,A.DateFormatType");
            dsSequenceSetup.Where("A.NumberCode", "=", sequenceCode);
            var dtSequenceSetup = DBHelper.GetDataTable(dsSequenceSetup.GetSql());
            if (dtSequenceSetup.Rows.Count > 0)
            {
                //设定字符串长度
                length = Convert.ToInt32(dtSequenceSetup.Rows[0]["NumberLength"]);

                #region 字符前添加固定字符
                prefixTemp = dtSequenceSetup.Rows[0]["Prefix"].ToString();
                if (!string.IsNullOrEmpty(prefix))
                {
                    prefixTemp = prefix + prefixTemp;
                    length = length + prefix.Length;
                }
                tempLength = length;
                if (!string.IsNullOrEmpty(prefixTemp))
                {
                    tempLength = tempLength - prefixTemp.Length;
                    result = prefixTemp;
                }
                #endregion

                #region 增长日期格式
                dataFormatType = dtSequenceSetup.Rows[0]["DateFormatType"].ToString();
                if (!string.IsNullOrEmpty(dataFormatType))
                {
                    if (dataFormatType == "YYYYMMDDHHMM")
                        dateString = DateTime.Now.ToString("yyyyMMddhhmm");
                    else if (dataFormatType == "YYYYMMDDHH")
                        dateString = DateTime.Now.ToString("yyyyMMddhh");
                    else if (dataFormatType == "YYYYMMDD")
                        dateString = DateTime.Now.ToString("yyyyMMdd");
                    else if (dataFormatType == "YYYYMM")
                        dateString = DateTime.Now.ToString("yyyyMM");
                    else if (dataFormatType == "YYYY")
                        dateString = DateTime.Now.ToString("yyyy");
                }
                result += dateString;
                tempLength = tempLength - dateString.Length;
                #endregion

                tableCode = dtSequenceSetup.Rows[0]["TableName"].ToString();
                columnCode = dtSequenceSetup.Rows[0]["ColumnName"].ToString();
                #region 查询
                DbSelect dbSelect = new(tableCode + " A", "A", null);
                dbSelect.IsInitDefaultValue = false;
                //if (string.IsNullOrEmpty(dateString))
                //{
                //    dbSelect.Select("MAX(A." + columnCode + ")");
                //    //dbSelect.Select("MAX(CONVERT(DECIMAL,SUBSTRING(A.ISSUE_NO," + (prefix.Length + dateString.Length + 1).ToString() + "," + tempLength.ToString() + ")))");
                //}
                //else
                //{
                if (!string.IsNullOrEmpty(prefixTemp) || !string.IsNullOrEmpty(dateString))
                    dbSelect.Select("MAX(SUBSTRING(A." + columnCode + "," + (prefixTemp.Length + dateString.Length + 1).ToString() + "," + tempLength.ToString() + "))");
                else
                    dbSelect.Select("MAX(A." + columnCode + ")");
                //}
                //dbSelect.Select("MAX(CONVERT(DECIMAL,SUBSTRING(A.ISSUE_NO," + (prefix.Length + dateString.Length + 1).ToString() + "," + tempLength.ToString() + ")))");
                if (!string.IsNullOrEmpty(prefixTemp) || !string.IsNullOrEmpty(dateString))
                    dbSelect.Where("SUBSTRING(A." + columnCode + ",1," + (prefixTemp.Length + dateString.Length).ToString() + ")", " = ", prefixTemp + dateString);
                dbSelect.Where("LEN(A." + columnCode + ")", "=", length);

                string maxSequence = await Db.Ado.GetStringAsync(dbSelect.GetSql());

                //string maxSequence = Convert.ToString(DBHelper.ExecuteScalar(dbSelect.GetSql(), null, null));
                #endregion
                //tempLength = tempLength - dateString.Length;
                if (string.IsNullOrEmpty(maxSequence))
                    result = prefixTemp + dateString + Convert.ToString(1).PadLeft(tempLength, '0');
                else
                {
                    if (!string.IsNullOrEmpty(prefixTemp) || !string.IsNullOrEmpty(dateString))
                    {
                        if (int.TryParse(maxSequence, out sequence))
                        {
                            sequence += 1;
                            if (sequence.ToString().Length > tempLength)
                                throw new Exception("自动生成字串长度已经超过设定长度!");
                        }
                        else
                            throw new Exception("表中的数据无法进行自动编号,请联系软件开发商!");
                        result = prefixTemp + dateString + sequence.ToString().PadLeft(tempLength, '0');
                    }
                    else
                    {
                        if (int.TryParse(maxSequence, out sequence))
                        {
                            sequence += 1;
                            if (sequence.ToString().Length > length)
                                throw new Exception("自动生成字串长度已经超过设定长度!");
                        }
                        else
                            throw new Exception("表中的数据无法进行自动编号,请联系软件开发商!");
                        result = sequence.ToString().PadLeft(length, '0');
                    }
                }
            }
            else
                throw new Exception("自动编号代码：" + sequenceCode + "没有设置！");
            return result;
        }
        catch (Exception) { throw; }
    }
    #endregion

    #region 记录模块操作日志
    /// <summary>
    /// 记录模块操作日志
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="operateType">操作类型</param>
    public static void RecordOperateLog(string userId, string moduleCode, string tableCode, string tableRowId, OperateType operateType, string programName = null, string remark = null)
    {
        try
        {
            DbInsert di = new("SmOperateLog", "RecordOperateLog");
            di.Values("UserId", userId);
            //di.Values("OperateUser", UserContext.Current.UserName);
            di.Values("OperateProgram", programName);
            di.Values("ModuleCode", moduleCode);
            di.Values("TableCode", tableCode);
            di.Values("TableRowId", tableRowId);
            di.Values("OperateDate", DateTime.Now);
            di.Values("Action", operateType.ToString());
            di.Values("Remark", remark);
            DBHelper.ExcuteNonQuery(di.GetSql());
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region 记录登录日志
    /// <summary>
    /// 记录登录日志
    /// </summary>
    /// <param name="userId">用户id</param>
    /// <param name="loginClass">登录类型</param>
    /// <param name="remark">备注</param>
    /// <param name="companyId">公司id</param>
    public static void RecordEntryLog(Guid userId, string loginClass, string remark = null, string companyId = null)
    {
        try
        {

            Task.Factory.StartNew(() =>
            {

                string ipAddress = string.Empty;
                string countryName = string.Empty;
                string cityName = string.Empty;
                string clientType = string.Empty;
                string os = string.Empty;
                if (string.IsNullOrEmpty(companyId))
                    companyId = GetCompanyId();

                ipAddress = HttpContextExtension.GetUserIp(HttpUseContext.Current);

                #region 求IP地址归属地
                // 定义解析结果信息对象
                ClientInfo clientInfo = null;

                // 尝试从头部里面获取User-Agent字符串
                if (HttpUseContext.Current != null)
                    if (HttpUseContext.Current.Request.Headers.TryGetValue("User-Agent", out var requestUserAgent) && !string.IsNullOrEmpty(requestUserAgent))
                    {
                        // 获取UaParser实例
                        var uaParser = Parser.GetDefault();

                        // 解析User-Agent字符串
                        clientInfo = uaParser.Parse(requestUserAgent);
                    }

                if (clientInfo != null)
                {
                    os = clientInfo.OS.Family + clientInfo.OS.Major;
                    clientType = clientInfo.UA.Family + clientInfo.UA.Major;
                    //if (clientType == "Web")
                    //{

                    //}

                }
                #endregion

                if (ipAddress.IsNotEmptyOrNull())
                    ipAddress = ipAddress.Replace("::ffff:", null);

                DbInsert di = new("SmEntryLog");
                di.Values("LoginUserId", userId.ToString());
                di.Values("IpAddress", ipAddress);
                di.Values("IpAddressName1", countryName);
                di.Values("IpAddressName2", cityName);
                di.Values("LoginDate", GetSysDate());
                di.Values("LoginClass", loginClass);
                di.Values("OSName", os);
                di.Values("ClientType", clientType);
                di.Values("Remark", remark);
                DBHelper.ExcuteNonQuery(di.GetSql());

            });

            //DbUpdate du = new DbUpdate("SM_USER", "ROW_ID", userId);
            //du.IsInitDefaultValue = false;
            //du.SetCompute("LOGIN_TIMES_STAT", "LOGIN_TIMES_STAT+1");
            //DBHelper.Instance.ExcuteNonQuery(du.GetSql());
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region 获取分页数据起始
    /// <summary>
    /// 获取分页数据起始
    /// </summary>
    /// <param name="paramData"></param>
    /// <param name="current">当前页码</param>
    /// <param name="pageSize">一个多个数据</param>
    public static void GetPageData(string paramData, out int current, out int pageSize)
    {
        var searchParam = JsonConvert.DeserializeObject<Dictionary<string, object>>(paramData);
        current = 1;
        pageSize = 10000;
        foreach (var item in searchParam)
        {
            if (item.Key == "current")
            {
                current = int.Parse(item.Value.ToString());
                continue;
            }

            if (item.Key == "pageSize")
            {
                pageSize = int.Parse(item.Value.ToString());
                continue;
            }
        }
    }
    #endregion

    #region 计算分页起始索引
    /// <summary>
    /// 计算分页起始索引
    /// </summary>
    /// <param name="paramData"></param>
    /// <param name="current">计算分页起始索引</param>
    /// <param name="pageSize">计算分页结束索引</param>
    public static void GetPageIndex(string paramData, out int startIndex, out int endIndex)
    {
        var searchParam = JsonConvert.DeserializeObject<Dictionary<string, object>>(paramData);
        int current = 1;
        int pageSize = 10000;
        foreach (var item in searchParam)
        {
            if (item.Key == "current")
            {
                current = int.Parse(item.Value.ToString());
                continue;
            }

            if (item.Key == "pageSize")
            {
                pageSize = int.Parse(item.Value.ToString());
                continue;
            }
        }

        int _pageSize = pageSize;
        //计算分页起始索引
        startIndex = current > 1 ? (current - 1) * _pageSize : 0;

        //计算分页结束索引
        endIndex = current * _pageSize;
    }
    #endregion

    /// <summary>
    /// 是否为空
    /// </summary>
    /// <param name="key">key</param>
    /// <returns></returns>
    public static void IsNullOrEmpty(string key, string message)
    {
        if (string.IsNullOrEmpty(key))
            throw new Exception(message);
    }
    public static void IsNullOrEmpty(Guid key, string message)
    {
        if (string.IsNullOrEmpty(key.ToString()))
            throw new Exception(message);
    }

    /// <summary>
    /// 去除后面多余的零
    /// </summary>
    /// <param name="dValue"></param>
    /// <returns></returns>
    public static string RemoveZero(decimal? dValue)
    {
        if (dValue.IsNullOrEmpty())
            return null;
        return RemoveZero(dValue.Value);
    }
    /// <summary>
    /// 去除后面多余的零
    /// </summary>
    /// <param name="dValue"></param>
    /// <returns></returns>
    public static string RemoveZero(decimal dValue)
    {
        string sResult = dValue.ToString();
        if (sResult.IndexOf(".") < 0)
            return sResult;
        int iIndex = sResult.Length - 1;
        for (int i = sResult.Length - 1; i >= 0; i--)
        {
            if (sResult.Substring(i, 1) != "0")
            {
                iIndex = i;
                break;
            }
        }
        sResult = sResult.Substring(0, iIndex + 1);
        if (sResult.EndsWith("."))
            sResult = sResult.Substring(0, sResult.Length - 1);
        return sResult;
    }

}
