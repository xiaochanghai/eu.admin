using EU.Core.Common.Caches;
using EU.Core.Common.Enums;
using EU.Core.Common.Extensions;
using EU.Core.Common.Https;
using EU.Core.Common.Module;
using EU.Core.Model;
using EU.Core.Model.ViewModels;
using EU.Core.Module;
using SqlSugar;
using System.Data;
using System.Text;
using UAParser;

namespace EU.Core.Common.Helper;

/// <summary>
/// 通用工具帮助类
/// 提供系统常用的工具方法，包括ID生成、序列号生成、数据格式化、缓存管理、日志记录等功能
/// </summary>
public static class Utility
{

    #region DataTable转Tree
    /// <summary>
    /// 将DataTable格式化为树形结构的DataTable
    /// 根据模块配置对数据进行格式化处理，包括日期格式化、数字格式化、布尔值转换等
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="userId">用户ID</param>
    /// <param name="dt">原始DataTable数据</param>
    /// <returns>返回格式化后的DataTable</returns>
    public static DataTable FormatDataTableForTree(string moduleCode, string userId, DataTable dt)
    {
        ModuleSqlColumn moduleColumnInfo = new(moduleCode);
        var dvModuleColumns = moduleColumnInfo.GetModuleSqlColumn();

        if (!dvModuleColumns.Where(x => x.DataIndex == "ID").Any())
            dvModuleColumns.Add(new SmModuleColumnExtend() { DataIndex = "ID" });

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

                if (!dtTree.Columns.Contains(columnName))
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
                    else value = string.Empty;

                    if (dvModuleColumns[j].IsBool != null && dvModuleColumns[j].IsBool.Value)
                        IsBool = dvModuleColumns[j].IsBool.Value;
                    else
                        IsBool = false;
                    if ((valueType == "date" || valueType == "dateTime" || valueType == "time") && !string.IsNullOrEmpty(dateFormat))
                    {
                        switch (dateFormat)
                        {
                            case "Y/m":
                                value = value.ConvertToYearMonthString();
                                break;
                            case "Y-m":
                                value = value.ConvertToYearMonthString1();
                                break;
                            case "Y/m/d":
                                value = value.ConvertToDayString();
                                break;
                            case "Y/m/d H":
                                value = value.ConvertToHourString();
                                break;
                            case "Y/m/d H:i":
                                value = value.ConvertToMiniuteString();
                                break;
                            case "Y/m/d H:i:s":
                                value = value.ConvertToSecondString();
                                break;
                            case "H:i":
                                value = value.ConvertToOnlyHourMinuteString();
                                break;
                            default:
                                break;
                        }
                    }
                    else if (valueType == "digit" && !string.IsNullOrEmpty(dateFormat))
                    {
                        if (string.IsNullOrEmpty(dateFormat) || dateFormat == "-1")
                            value = TrimDecimalString(value, -1);
                        else
                            value = TrimDecimalString(value, Convert.ToInt32(dateFormat));
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
                                value = TrimDecimalString(value, -1);
                            else
                                value = TrimDecimalString(value, Convert.ToInt32(dateFormat));
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
    /// 获取系统当前日期时间
    /// </summary>
    /// <returns>返回当前系统DateTime</returns>
    public static DateTime GetSysDate() => DateTime.Now;
    #endregion

    #region 求系统唯一字符串
    /// <summary>
    /// 生成系统唯一字符串ID（基于时间戳和GUID）
    /// 格式：yyMMddHHmmss + 长整型数字，常用于ROW_ID值
    /// </summary>
    /// <returns>返回唯一字符串ID</returns>
    public static string GetSysID()
    {
        var sid = string.Empty;

        byte[] buffer = Guid.NewGuid().ToByteArray();
        sid = DateTime.Now.ToString("yyMMddHHmmss") + BitConverter.ToInt64(buffer, 0).ToString();
        return sid;
    }
    #endregion

    #region 雪花ID
    /// <summary>
    /// 生成雪花算法ID（分布式唯一ID）
    /// </summary>
    /// <returns>返回64位长整型唯一ID</returns>
    public static long SnowID() => SnowFlakeSingle.Instance.NextId();
    #endregion

    #region 求GUID
    /// <summary>
    /// 生成一个GUID字符串
    /// </summary>
    /// <param name="format">GUID格式（N=32位无分隔符, D=36位带连字符, B=38位带花括号等），默认为N</param>
    /// <returns>返回格式化的GUID字符串</returns>
    public static string GetGUID(string format = "N")
    {
        return Guid.NewGuid().ToString(format);
    }

    /// <summary>  
    /// 根据GUID生成19位的唯一长整型数字序列  
    /// </summary>  
    /// <returns>返回19位长整型唯一ID</returns>  
    public static long GetGuidToLongID()
    {
        var buffer = Guid.NewGuid().ToByteArray();
        return BitConverter.ToInt64(buffer, 0);
    }

    /// <summary>  
    /// 生成唯一长整型数字序列（使用雪花算法）
    /// </summary>  
    /// <returns>返回长整型唯一ID</returns>  
    public static long GetLongID()
    {
        return SnowFlakeSingle.Instance.NextId();
    }

    /// <summary>
    /// 获取GUID字符串（带连字符格式）
    /// </summary>
    public static string GuidId1
    {
        get
        {
            var id = Guid.NewGuid();
            return id.ToString();
        }
    }

    /// <summary>
    /// 获取新的GUID对象
    /// </summary>
    public static Guid GuidId
    {
        get
        {
            return Guid.NewGuid();
        }
    }
    #endregion

    #region 获得当前公司ID
    /// <summary>
    /// 获取当前公司ID（字符串格式）
    /// </summary>
    /// <returns>返回公司ID字符串</returns>
    public static string GetCompanyId() => GetCompanyGuidId().ToString();

    /// <summary>
    /// 获取当前公司ID（GUID格式）
    /// </summary>
    /// <returns>返回公司GUID</returns>
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
    /// 获取当前登录用户ID（字符串格式）
    /// </summary>
    /// <returns>返回用户ID字符串，未登录返回null</returns>
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
    /// 获取当前登录用户ID（GUID格式）
    /// </summary>
    /// <returns>返回用户GUID，未登录返回null</returns>
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
    /// 获取当前集团ID（字符串格式）
    /// </summary>
    /// <returns>返回集团ID字符串</returns>
    public static string GetGroupId() => GetGroupGuidId().ToString();

    /// <summary>
    /// 获取当前集团ID（GUID格式）
    /// </summary>
    /// <returns>返回集团GUID</returns>
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
    /// 清空Redis缓存中的所有数据
    /// </summary>
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
    /// 清空所有Redis缓存数据库，并重新加载模块、SQL、权限等配置信息
    /// </summary>
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
    /// 自动生成连续的序列号（使用此函数时，一定要把存放此Sequence的列设为Unique）
    /// 根据编号规则自动生成带前缀和日期的连续序列号
    /// </summary>
    /// <param name="sequenceCode">编号规则代码</param>
    /// <param name="trans">是否使用事务，默认false</param>
    /// <returns>返回新生成的序列号</returns>
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
    /// 自动生成连续的序列号（使用此函数时，一定要把存放此Sequence的列设为Unique）
    /// 根据编号规则和自定义前缀生成连续序列号，支持日期格式
    /// </summary>
    /// <param name="sequenceCode">编号规则代码</param>
    /// <param name="prefix">自定义前缀（会附加在规则前缀之前）</param>
    /// <param name="trans">是否使用事务，默认false</param>
    /// <returns>返回新生成的序列号</returns>
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

    /// <summary>
    /// 自动生成连续的序列号（根据指定表和列）
    /// </summary>
    /// <param name="tableCode">表名</param>
    /// <param name="columnCode">列名</param>
    /// <param name="prefix">前缀</param>
    /// <param name="length">序列号总长度</param>
    /// <param name="trans">是否使用事务，默认false</param>
    /// <returns>返回新生成的序列号</returns>
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

    /// <summary>
    /// 自动生成连续的整型序列号
    /// </summary>
    /// <param name="tableCode">表名</param>
    /// <param name="columnCode">列名</param>
    /// <param name="fieldName">过滤字段名</param>
    /// <param name="fieldValue">过滤字段值</param>
    /// <param name="trans">是否使用事务，默认false</param>
    /// <returns>返回新生成的整型序列号</returns>
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
    /// 异步自动生成连续的序列号（使用此函数时，一定要把存放此Sequence的列设为Unique）
    /// </summary>
    /// <param name="Db">SqlSugar数据库客户端</param>
    /// <param name="sequenceCode">编号规则代码</param>
    /// <returns>返回新生成的序列号</returns>
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
    /// 异步自动生成连续的序列号（使用此函数时，一定要把存放此Sequence的列设为Unique）
    /// 根据编号规则和自定义前缀生成连续序列号，支持日期格式
    /// </summary>
    /// <param name="Db">SqlSugar数据库客户端</param>
    /// <param name="sequenceCode">编号规则代码</param>
    /// <param name="prefix">自定义前缀（会附加在规则前缀之前）</param>
    /// <returns>返回新生成的序列号</returns>
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
    /// 记录用户对模块数据的增删改查操作
    /// </summary>
    /// <param name="userId">操作用户ID</param>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="tableCode">表名</param>
    /// <param name="tableRowId">记录ID</param>
    /// <param name="operateType">操作类型（Add/Edit/Delete/View等）</param>
    /// <param name="programName">程序名称</param>
    /// <param name="remark">备注信息</param>
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
            DBHelper.ExecuteNonQuery(di.GetSql());
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region 记录登录日志
    /// <summary>
    /// 记录用户登录日志
    /// 记录用户登录信息，包括IP地址、归属地、操作系统、客户端类型等
    /// </summary>
    /// <param name="userId">登录用户ID</param>
    /// <param name="loginClass">登录类型（Web/Mobile/Desktop等）</param>
    /// <param name="remark">备注信息</param>
    /// <param name="companyId">公司ID，默认为null时使用当前公司ID</param>
    public static async void RecordEntryLog(Guid userId, string loginClass, string remark = null, string companyId = null)
    {
        try
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
            if (ipAddress.IsNotEmptyOrNull() && ipAddress != "127.0.0.1")
            {
                var request = new RequestUtility();
                var result = await request.Get<IPLocation>("https://ip9.com.cn/get?ip=" + ipAddress);

                if (result.Success)
                {
                    di.Values("IpAddressName1", result.Data.data.country + result.Data.data.prov + result.Data.data.city);
                }
            }


            di.Values("LoginUserId", userId.ToString());
            di.Values("IpAddress", ipAddress);
            di.Values("IpAddressName1", countryName);
            di.Values("IpAddressName2", cityName);
            di.Values("LoginDate", GetSysDate());
            di.Values("LoginClass", loginClass);
            di.Values("OSName", os);
            di.Values("ClientType", clientType);
            di.Values("Remark", remark);
            DBHelper.ExecuteDML(di.GetSql());


            var du = new DbUpdate("SmUsers", "ID", userId);
            du.Set("LastLoginTime", GetSysDate());
            DBHelper.ExecuteDML(du.GetSql());
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion  

    #region 去除后面多余的零
    /// <summary>
    /// 去除小数点后面多余的零（可空类型）
    /// 例如：1.2000 → 1.2, 1.0 → 1
    /// </summary>
    /// <param name="dValue">decimal可空值</param>
    /// <returns>返回去除尾部零后的字符串，空值返回null</returns>
    public static string RemoveZero(this decimal? dValue)
    {
        if (dValue.IsNullOrEmpty())
            return null;
        return RemoveZero(dValue.Value);
    }
    ///// <summary>
    ///// 去除后面多余的零
    ///// </summary>
    ///// <param name="dValue"></param>
    ///// <returns></returns>
    //public static string RemoveZero(decimal? dValue)
    //{
    //    if (dValue.IsNullOrEmpty())
    //        return null;
    //    return RemoveZero(dValue.Value);
    //}

    /// <summary>
    /// 去除小数点后面多余的零
    /// 例如：1.2000 → 1.2, 1.0 → 1
    /// </summary>
    /// <param name="dValue">decimal值</param>
    /// <returns>返回去除尾部零后的字符串</returns>
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
    #endregion

    #region 格式化数字字符
    /// <summary>
    /// 格式化数字字符串，去除尾部多余的零
    /// 例如：传入"1.24500"，返回"1.245"
    /// </summary>
    /// <param name="value">数字字符串</param>
    /// <returns>返回格式化后的数字字符串</returns>
    public static string TrimDecimalString(string value)
    {
        try
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(value))
            {
                Decimal tmp = Decimal.Parse(value);
                result = string.Format("{0:#0.##########}", tmp);
            }
            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }
    /// <summary>
    /// 格式化数字对象，去除尾部多余的零
    /// 例如：传入1.24500，返回"1.245"
    /// </summary>
    /// <param name="value">数字对象</param>
    /// <returns>返回格式化后的数字字符串</returns>
    public static string TrimDecimalString(object value)
    {
        try
        {
            string result = string.Empty;
            if (value != null)
            {
                Decimal tmp = Decimal.Parse(value.ToString());
                result = string.Format("{0:#0.##########}", tmp);
            }
            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// 格式化数字字符串，并保留指定的小数位数
    /// </summary>
    /// <param name="value">需要处理的数字字符串</param>
    /// <param name="reservedDigit">保留小数点后位数，-1表示只去除尾部的零</param>
    /// <returns>返回格式化后的数字字符串</returns>
    public static string TrimDecimalString(string value, int reservedDigit)
    {
        try
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(value))
            {
                Decimal tmp = Decimal.Parse(value);
                if (reservedDigit == -1)
                    result = string.Format("{0:#0.##########}", tmp);
                else
                {
                    result = String.Format("{0:N" + reservedDigit.ToString() + "}", tmp);
                    result = result.Replace(",", "");
                }
            }
            return result;
        }
        catch (Exception) { throw; }
    }

    /// <summary>
    /// 格式化数字对象，并保留指定的小数位数
    /// </summary>
    /// <param name="value">需要处理的数字对象</param>
    /// <param name="reservedDigit">保留小数点后位数，-1表示只去除尾部的零</param>
    /// <returns>返回格式化后的数字字符串</returns>
    public static string TrimDecimalString(object value, int reservedDigit)
    {
        try
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(Convert.ToString(value)))
            {
                Decimal tmp = Decimal.Parse(Convert.ToString(value));
                if (reservedDigit == -1)
                    result = string.Format("{0:#0.##########}", tmp);
                else
                {
                    result = String.Format("{0:N" + reservedDigit.ToString() + "}", tmp);
                    result = result.Replace(",", "");
                }
            }
            return result;
        }
        catch (Exception) { throw; }
    }

    /// <summary>
    /// 格式化数字对象为decimal，并保留指定的小数位数
    /// </summary>
    /// <param name="value">需要处理的数字对象</param>
    /// <param name="reservedDigit">保留小数点后位数，-1表示只去除尾部的零</param>
    /// <returns>返回格式化后的decimal值</returns>
    public static decimal TrimDecimal(object value, int reservedDigit)
    {
        try
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(Convert.ToString(value)))
            {
                Decimal tmp = Decimal.Parse(Convert.ToString(value));
                if (reservedDigit == -1)
                    result = string.Format("{0:#0.##########}", tmp);
                else
                {
                    result = String.Format("{0:N" + reservedDigit.ToString() + "}", tmp);
                    result = result.Replace(",", "");
                }
            }
            return Convert.ToDecimal(result);
        }
        catch (Exception) { throw; }
    }
    #endregion

    #region 根据分隔符返回前n条数据
    /// <summary>
    /// 根据分隔符切分字符串，并返回前n条数据
    /// </summary>
    /// <param name="content">原始数据内容</param>
    /// <param name="separator">分隔符</param>
    /// <param name="top">要返回的前n条数据，0表示返回全部</param>
    /// <param name="isDesc">是否倒序，默认false为正序</param>
    /// <returns>返回字符串列表</returns>
    public static List<string> GetTopDataBySeparator(string content, string separator, int top, bool isDesc = false)
    {
        if (string.IsNullOrEmpty(content))
            return new List<string>() { };

        if (string.IsNullOrEmpty(separator))
            throw new ArgumentException("message", nameof(separator));

        var dataArray = content.Split(separator).Where(d => !string.IsNullOrEmpty(d)).ToArray();
        if (isDesc)
            Array.Reverse(dataArray);

        if (top > 0)
            dataArray = dataArray.Take(top).ToArray();

        return dataArray.ToList();
    }
    #endregion

    #region 根据字段拼接get参数
    /// <summary>
    /// 根据字典拼接GET请求参数字符串
    /// 例如：{name: "张三", age: 18} → "name=张三&age=18"
    /// </summary>
    /// <param name="dic">参数字典</param>
    /// <returns>返回URL参数字符串</returns>
    public static string GetPars(Dictionary<string, object> dic)
    {

        StringBuilder sb = new();
        string urlPars = null;
        bool isEnter = false;
        foreach (var item in dic)
        {
            sb.Append($"{(isEnter ? "&" : "")}{item.Key}={item.Value}");
            isEnter = true;
        }
        urlPars = sb.ToString();
        return urlPars;
    }
    #endregion

    #region 根据字段拼接get参数
    /// <summary>
    /// 根据字典拼接GET请求参数字符串（字符串类型值）
    /// 例如：{name: "张三", city: "北京"} → "name=张三&city=北京"
    /// </summary>
    /// <param name="dic">参数字典（字符串类型）</param>
    /// <returns>返回URL参数字符串</returns>
    public static string GetPars(Dictionary<string, string> dic)
    {

        StringBuilder sb = new();
        string urlPars = null;
        bool isEnter = false;
        foreach (var item in dic)
        {
            sb.Append($"{(isEnter ? "&" : "")}{item.Key}={item.Value}");
            isEnter = true;
        }
        urlPars = sb.ToString();
        return urlPars;
    }
    #endregion

    #region 获取字符串最后X行
    /// <summary>
    /// 获取字符串的最后X行内容
    /// 按行分割字符串，返回最后指定行数的内容
    /// </summary>
    /// <param name="resourceStr">原始字符串</param>
    /// <param name="length">要获取的行数</param>
    /// <returns>返回拼接后的字符串</returns>
    public static string GetCusLine(string resourceStr, int length)
    {
        string[] arrStr = resourceStr.Split("\r\n");
        return string.Join("", (from q in arrStr select q).Skip(arrStr.Length - length + 1).Take(length).ToArray());
    }
    #endregion
}
