using System.Data;
using EU.Core.Model.Models;

namespace EU.Core.Common.Helper;

public class ImportHelper
{
    #region 导入数据
    /// <summary>
    /// 导入数据
    /// </summary>
    /// <param name="smImpTemplate">导入模板</param>
    /// <param name="importDataId">导入数据ID</param>
    /// <param name="filePath">文件路径</param>
    /// <param name="fileName">文件名</param>
    /// <param name="dt">数据data</param>
    /// <param name="userId">用户ID</param>
    public static void ImportData(SmImpTemplate smImpTemplate, string importDataId, string filePath, string fileName, DataTable dt, string userId = null)
    {
        try
        {
            DbInsert di = new("SmImportData");
            di.IsInitDefaultValue = false;
            di.IsInitRowId = false;
            di.Values("ID", importDataId);
            di.Values("CreatedBy", userId);
            di.Values("CreatedTime", Utility.GetSysDate());
            di.Values("GroupId", Utility.GetGroupId());
            di.Values("CompanyId", Utility.GetCompanyId());
            di.Values("ImportFileName", filePath + fileName);
            DBHelper.ExecuteDML(di.GetSql());

            string sql = "SELECT * FROM SmImportDataDetail WHERE 1!=1";
            var dt1 = DBHelper.GetDataTable(sql);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var dr = dt1.NewRow();
                dr["ID"] = Utility.GuidId;
                dr["CreatedBy"] = userId;
                dr["CreatedTime"] = Utility.GetSysDate();
                dr["ImportDataId"] = importDataId;
                di.Values("GroupId", Utility.GetGroupId());
                di.Values("CompanyId", Utility.GetCompanyId());
                dr["SheetName"] = smImpTemplate.SheetName;
                dr["LineNo"] = i + 2;
                for (int j = 0; j < dt.Columns.Count; j++)
                    dr["Col" + (j + 1)] = Convert.ToString(dt.Rows[i][j]);
                dt1.Rows.Add(dr);
            }
            DBHelper.BulkInsert(dt1, "SmImportDataDetail");

            //#region 记录模块操作日志
            //try
            //{
            //    DBHelper.RecordOperateLog(userId, smImpTemplate.ModuleCode, "SmImportDataDetail", "", OperateType.Import);
            //}
            //catch { }
            //#endregion

            int count = Check(importDataId, userId, Utility.GetGroupId(), Utility.GetCompanyId(), smImpTemplate.TemplateCode);
            if (count > 0)
                throw new Exception("导入文件中存在错误！");

            //TransferData(importDataId, smImpTemplate.TemplateCode, userId, false);
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region 验证规则
    //public static int Check(string importDataId, string importTemplateCode, string userId, string groupId, string companyId)
    //{
    //    try
    //    {
    //        return Check(importDataId, userId, "jt20080101", "20080101", importTemplateCode);
    //    }
    //    catch (Exception Ex)
    //    {
    //        throw Ex;
    //    }
    //}
    /// <summary>
    /// 验证规则
    /// </summary>
    /// <param name="importDataId">导入数据主表ID</param>
    public static int Check(string importDataId, string userId, string saveGroupId, string companyId, string importTemplateCode)
    {
        try
        {
            #region 变量定义
            //string importTemplateId = string.Empty;
            string sheetName = string.Empty;
            string ruleCode = string.Empty;
            string alterType = string.Empty;
            string ruleValue = string.Empty;
            string tableCode = string.Empty;
            string sql = string.Empty;
            string importDataId1 = importDataId;
            string moduleId = string.Empty;
            string UserCode = userId;
            string GroupId = saveGroupId;
            string CompanyId = companyId;
            #endregion

            #region 删除原有验证错误
            sql = "DELETE A FROM SmImportError A WHERE A.ImportDataId='{0}';" +
                  "DELETE A FROM SmImportDataErrorCol A WHERE A.ImportDataId='{0}'";
            sql = string.Format(sql, importDataId);
            DBHelper.ExecuteDML(sql);
            #endregion

            #region 更新错误标志
            sql = "UPDATE SmImportDataDetail SET IsError=NULL WHERE ImportDataId='" + importDataId + "'";
            DBHelper.ExecuteDML(sql);
            #endregion

            sql = "SELECT * FROM SmImpTemplate WHERE TemplateCode='{0}'";
            sql = string.Format(sql, importTemplateCode);
            var impTemplate = DBHelper.QueryFirst<SmImpTemplate>(sql);
            if (impTemplate == null)
                throw new Exception("Excel导入模板ID【" + importTemplateCode + "】不存在！");

            string label = impTemplate.Label;
            sheetName = impTemplate.SheetName;

            #region 求导入模板对应表名
            tableCode = impTemplate.TableCode;
            #endregion

            #region 求导入模板子表并进行验证
            string columnCode = string.Empty;
            string dateFormat = string.Empty;
            int maxLength = -1;
            string isAllowNull = string.Empty;
            string isUnique = string.Empty;
            int columnIndex = -1;
            DbSelect dsImpTempDetail = new("SmImpTemplateDetail A", "A", null);
            dsImpTempDetail.Select("A.*");
            dsImpTempDetail.Where("A.ImpTemplateId", "=", impTemplate.ID);
            var dtImpTemplateDetail = DBHelper.GetDataTable(dsImpTempDetail.GetSql());
            for (int i = 0; i < dtImpTemplateDetail.Rows.Count; i++)
            {
                columnCode = dtImpTemplateDetail.Rows[i]["ColumnCode"].ToString();
                columnIndex = Convert.ToInt32(dtImpTemplateDetail.Rows[i]["ColumnNo"]);

                #region 验证数据类型
                CheckFieldType(importDataId, columnIndex, tableCode, columnCode, sheetName);
                #endregion

                #region 日期格式
                dateFormat = dtImpTemplateDetail.Rows[i]["DateFormate"].ToString();
                if (!string.IsNullOrEmpty(dateFormat))
                {
                    CheckFieldFormat(importDataId, columnIndex, dateFormat, sheetName);
                }
                #endregion

                #region 最大长度
                if (!string.IsNullOrEmpty(dtImpTemplateDetail.Rows[i]["MaxLength"].ToString()))
                {
                    maxLength = Convert.ToInt32(dtImpTemplateDetail.Rows[i]["MaxLength"]);
                    CheckFieldLength(importDataId, columnIndex, maxLength, sheetName);
                }
                #endregion

                #region 允许为空
                isAllowNull = dtImpTemplateDetail.Rows[i]["IsAllowNull"].ToString();
                if (!string.IsNullOrEmpty(isAllowNull) && isAllowNull == "N")
                {
                    CheckFieldIsNull(importDataId, columnIndex, sheetName, userId);
                }
                #endregion

                #region 唯一性检查
                isUnique = dtImpTemplateDetail.Rows[i]["IsUnique"].ToString();
                if (!string.IsNullOrEmpty(isUnique) && isUnique == "Y")
                {
                    CheckFieldUnique(importDataId, columnIndex, tableCode, columnCode, companyId, sheetName, userId);
                }
                #endregion

                #region 参数值
                string lovCode = string.Empty;
                string corresTableCode = string.Empty;
                string corresColumnCode = string.Empty;
                lovCode = dtImpTemplateDetail.Rows[i]["LovCode"].ToString();
                corresTableCode = dtImpTemplateDetail.Rows[i]["CorresTableCode"].ToString();
                corresColumnCode = dtImpTemplateDetail.Rows[i]["CorresColumnCode"].ToString();
                if (!string.IsNullOrEmpty(lovCode))
                {
                    CheckLovCode(importDataId, columnIndex, lovCode, corresTableCode, corresColumnCode, companyId, sheetName);
                }
                #endregion

                #region 映射表和映射字段
                else if (!string.IsNullOrEmpty(corresTableCode) && !string.IsNullOrEmpty(corresColumnCode))
                {
                    CheckCorresTable(importDataId, columnIndex, corresTableCode, corresColumnCode, companyId, sheetName, userId);
                }
                #endregion
            }
            #endregion

            #region 求导入模板验证规则
            //DbSelect dsImpTempRule = new DbSelect("SM_IMP_TEMPLATE_RULE A", "A", null);
            //dsImpTempRule.Select("A.*");
            //dsImpTempRule.Where("A.IMP_TEMPLATE_ID", "=", impTemplate.ID);
            //DataTable dtImpTemplateRule = DBHelper.GetDataTable(dsImpTempRule.GetSql(), null);
            //if (dtImpTemplateRule.Rows.Count > 0)
            //{
            //    for (int i = 0; i < dtImpTemplateRule.Rows.Count; i++)
            //    {
            //        ruleCode = Convert.ToString(dtImpTemplateRule.Rows[i]["RULE_CODE"]);
            //        alterType = Convert.ToString(dtImpTemplateRule.Rows[i]["ALERT_TYPE"]);
            //        if (string.IsNullOrEmpty(alterType))
            //        {
            //            alterType = "E";
            //        }
            //        ruleValue = Convert.ToString(dtImpTemplateRule.Rows[i]["RULE_VALUE"]);
            //        moduleId = Convert.ToString(dtImpTemplateRule.Rows[i]["MODULE_ID"]);
            //    }
            //}
            #endregion

            sql = $"SELECT COUNT(0) FROM SmImportError WHERE ImportDataId='{importDataId}'";
            int count = Convert.ToInt32(DBHelper.ExecuteScalar(sql));
            return count;
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region 验证列类型
    /// <summary>
    /// 验证列类型
    /// </summary>
    /// <param name="columnIndex"></param>
    /// <param name="fieldName"></param>
    public static void CheckFieldType(string importDataId, int columnIndex, string tableCode, string fieldName, string sheetName)
    {
        try
        {
            string fieldType = string.Empty;
            DbSelect ds = new("SmFieldCatalog A", "A", null);
            ds.Select("A.ColumnCode,A.DataType");
            ds.Where("A.TableCode", "=", tableCode);
            ds.Where("A.ColumnCode", "=", fieldName);
            var dt = DBHelper.GetDataTable(ds.GetSql());
            if (dt.Rows.Count > 0)
            {
                fieldType = dt.Rows[0]["DataType"].ToString();
                if (fieldType == "STRING")
                {
                    return;
                }
                else
                {
                    string columnName = "Col" + columnIndex.ToString();
                    DbSelect dsImpDataDetail = new("SmImportDataDetail A", "A", null);
                    dsImpDataDetail.Select("A." + columnName);
                    dsImpDataDetail.Select("A.[LineNo]");
                    dsImpDataDetail.Where("A.ImportDataId", "=", importDataId);
                    dsImpDataDetail.Where("A.SheetName", "=", sheetName);
                    dsImpDataDetail.OrderBy("A.[LineNo]", "ASC");
                    var dtImpDataDetail = DBHelper.GetDataTable(dsImpDataDetail.GetSql(), null);
                    //bool result = true;
                    if (fieldType == "DATE")
                    {
                        for (int i = 0; i < dtImpDataDetail.Rows.Count; i++)
                        {
                            //result = Utility.IsDateTime(dtImpDataDetail.Rows[i][columnName].ToString());
                            //if (result == false)
                            //{
                            //    UpdateImportDataErrorFlag(importDataId1, i + 2, columnIndex, Resource.GetOnlyMessage("CC-00297"), sheetName, "E", null);
                            //    //Excel中【{0}】行【{1}】列的数据类型不正确，正确类型应为日期类型！
                            //    InsertErrorMsg("CheckFieldType", Resource.GetOnlyMessage("CC-00245", (i + 2).ToString(), Utility.GetExcelColumnName(columnIndex)), "E", sheetName, "CheckFieldType");
                            //}
                        }
                    }
                    else if (fieldType == "NUMBER")
                    {
                        for (int i = 0; i < dtImpDataDetail.Rows.Count; i++)
                        {
                            //result = Utility.IsNumber(dtImpDataDetail.Rows[i][columnName].ToString());
                            //if (result == false)
                            //{
                            //    UpdateImportDataErrorFlag(importDataId1, i + 2, columnIndex, Resource.GetOnlyMessage("CC-00298"), sheetName, "E", null);
                            //    //Excel中【{0}】行【{1}】列的数据类型不正确，正确类型应为数字类型！
                            //    InsertErrorMsg("CheckFieldType", Resource.GetOnlyMessage("CC-00246", (i + 2).ToString(), Utility.GetExcelColumnName(columnIndex)), "E", sheetName, "CheckFieldType");
                            //}
                        }
                    }
                }
            }
        }
        catch (Exception) { throw; }
    }
    #endregion

    #region 验证列格式
    /// <summary>
    /// 验证列格式
    /// </summary>
    /// <param name="columnIndex"></param>
    /// <param name="fieldFormat"></param>
    public static void CheckFieldFormat(string importDataId, int columnIndex, string fieldFormat, string sheetName)
    {
        try
        {
            //bool isDateFormat;
            string columnName = "COL" + columnIndex.ToString();
            string sql = @"SELECT {0},[LineNo]
                                 FROM SmImportDataDetail A
                                WHERE ImportDataId='{1}' AND SheetName='{2}'
                                  AND {0} IS NOT NULL";
            sql = string.Format(sql, columnName, importDataId, sheetName);
            var dt = DBHelper.GetDataTable(sql);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                //isDateFormat = Utility.IsDateTimeFormat(dt.Rows[0][columnName].ToString(), fieldFormat);
                //if (!isDateFormat)
                //{
                //    UpdateImportDataErrorFlag(importDataId1, Convert.ToInt32(dt.Rows[i]["LineNo"]), columnIndex, Resource.GetOnlyMessage("CC-00299"), sheetName, "E", null);
                //    //Excel中【{0}】行【{1}】列的字符【{2}】的格式跟设定字符【{3}】的格式不符合！
                //    InsertErrorMsg("CheckFieldFormat", Resource.GetOnlyMessage("CC-00247", dt.Rows[i]["LineNo"].ToString(), Utility.GetExcelColumnName(columnIndex), dt.Rows[i][columnName].ToString(), fieldFormat), "E", sheetName, "CheckFieldFormat");
                //}
            }
            //fieldFormat.
        }
        catch (Exception) { throw; }
    }
    #endregion

    #region 验证列长度
    /// <summary>
    /// 验证列长度
    /// </summary>
    /// <param name="columnIndex"></param>
    /// <param name="fieldLength"></param>
    public static void CheckFieldLength(string importDataId, int columnIndex, int fieldLength, string sheetName)
    {
        try
        {
            string columnName = "Col" + columnIndex.ToString();
            string sql = @"SELECT LEN({0}) AS FIELD_LEN,[LineNo] 
                                 FROM SmImportDataDetail A
                                WHERE LEN({0}) > {1} AND ImportDataId='{2}' AND SheetName='{3}'";
            sql = string.Format(sql, columnName, fieldLength.ToString(), importDataId, sheetName);
            var dt = DBHelper.GetDataTable(sql);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                //UpdateImportDataErrorFlag(importDataId1, Convert.ToInt32(dt.Rows[i]["LineNo"]), columnIndex, Resource.GetOnlyMessage("CC-00300", fieldLength.ToString()), sheetName, "E", null);
                ////Excel中【{0}】行【{1}】列的长度超过最大长度【{2}】！
                //InsertErrorMsg("CheckFieldLength", Resource.GetOnlyMessage("CC-00248", dt.Rows[i]["LineNo"].ToString(), Utility.GetExcelColumnName(columnIndex), fieldLength.ToString()), "E", sheetName, "CheckFieldLength");
            }
        }
        catch (Exception) { throw; }
    }
    #endregion

    #region 验证列是否为空
    /// <summary>
    /// 验证列是否为空
    /// </summary>
    /// <param name="columnIndex"></param>
    public static void CheckFieldIsNull(string importDataId, int columnIndex, string sheetName, string userId)
    {
        try
        {
            string columnName = "Col" + columnIndex.ToString();
            string sql = @"SELECT A.ID,A.[LineNo] 
                                 FROM SmImportDataDetail A
                                WHERE ({0} IS NULL OR {0}='') AND ImportDataId='{1}' AND SheetName='{2}'";
            sql = string.Format(sql, columnName, importDataId, sheetName);
            var dt = DBHelper.GetDataTable(sql);

            #region 准备表结构
            DbSelect dsImportDataDetail = new("SmImportDataDetail A", "A");
            dsImportDataDetail.Select("A.*");
            dsImportDataDetail.Where("1 = 2");
            DataTable dtImportDataDetail = DBHelper.GetDataTable(dsImportDataDetail.GetSql(), null);
            //DataRow drImportDataDetail = null;

            DbSelect dsImportDataErrorCol = new("SmImportDataErrorCol A", "A");
            dsImportDataErrorCol.Select("A.*");
            dsImportDataErrorCol.Where("1 = 2");
            var dtImportDataErrorCol = DBHelper.GetDataTable(dsImportDataErrorCol.GetSql(), null);
            DataRow drImportDataErrorCol = null;

            DbSelect dsImportError = new("SmImportError A", "A");
            dsImportError.Select("A.*");
            dsImportError.Where("1 = 2");
            var dtImportError = DBHelper.GetDataTable(dsImportError.GetSql(), null);
            DataRow drImportError = null;
            #endregion

            var smImportDataDetailList = new List<SmImportDataDetail>();


            for (int i = 0; i < dt.Rows.Count; i++)
            {
                //drImportDataDetail = dtImportDataDetail.NewRow();
                //drImportDataDetail["ID"] = dt.Rows[i]["ID"].ToString();
                //drImportDataDetail["IsError"] = true;
                //dtImportDataDetail.Rows.Add(drImportDataDetail);
                //drImportDataDetail.AcceptChanges();
                //drImportDataDetail.SetModified();

                SmImportDataDetail smImportDataDetail = new();
                smImportDataDetail.ID = Guid.Parse(dt.Rows[i]["ID"].ToString());
                smImportDataDetail.IsError = true;
                smImportDataDetailList.Add(smImportDataDetail);

                drImportDataErrorCol = dtImportDataErrorCol.NewRow();
                drImportDataErrorCol["ID"] = Utility.GuidId1;
                drImportDataErrorCol["CreatedBy"] = userId;
                //drImportDataErrorCol["CREATED_PROGRAM"] = "CommonImport";
                drImportDataErrorCol["CreatedTime"] = Utility.GetSysDate();
                //drImportDataErrorCol["CompanyId"] = Utility.GetCompanyId();
                drImportDataErrorCol["ImportDataId"] = importDataId;
                drImportDataErrorCol["SheetName"] = sheetName;
                drImportDataErrorCol["LineNo"] = Convert.ToInt32(dt.Rows[i]["LineNo"]);
                drImportDataErrorCol["ColumnNo"] = columnIndex;
                drImportDataErrorCol["ErrorType"] = "E";
                drImportDataErrorCol["ErrorMessage"] = "";// Resource.GetOnlyMessage("CC-00301");
                dtImportDataErrorCol.Rows.Add(drImportDataErrorCol);
                //UpdateImportDataErrorFlag(importDataId1, Convert.ToInt32(dt.Rows[i]["LineNo"]), columnIndex, Resource.GetOnlyMessage("CC-00301"), sheetName, "E", null);

                drImportError = dtImportError.NewRow();
                drImportError["ID"] = Utility.GuidId1;
                drImportError["CreatedBy"] = userId;
                //drImportError["CREATED_PROGRAM"] = "CommonImport";
                drImportError["CreatedTime"] = Utility.GetSysDate();
                //drImportError["CompanyId"] = Utility.GetCompanyId();
                drImportError["ImportDataId"] = importDataId;
                drImportError["SheetName"] = sheetName;
                drImportError["ErrorCode"] = "CheckFieldIsNull";
                string errorMsg = "Excel中【{0}】行【{1}】列的数据不允许为空！";
                errorMsg = string.Format(errorMsg, dt.Rows[i]["LineNo"].ToString(), GetExcelColumnName(columnIndex));
                drImportError["ErrorName"] = errorMsg;
                drImportError["ErrorType"] = "E";
                drImportError["ModuleCode"] = "";
                dtImportError.Rows.Add(drImportError);
            }
            //DBHelper.BulkInsert(dtImportDataDetail, "SmImportDataDetail");
            DBHelper.UpdateRange(smImportDataDetailList, x => new { x.IsError });
            DBHelper.BulkInsert(dtImportDataErrorCol, "SmImportDataErrorCol");
            DBHelper.BulkInsert(dtImportError, "SmImportError");

        }
        catch (Exception) { throw; }
    }
    #endregion

    #region 验证列是否唯一
    /// <summary>
    /// 验证列中的字段是否唯一
    /// </summary>
    /// <param name="columnIndex">列索引</param>
    /// <param name="tableCode">表名</param>
    /// <param name="columnCode">列代码</param>
    public static void CheckFieldUnique(string importDataId, int columnIndex, string tableCode, string columnCode, string companyId, string sheetName, string userId)
    {
        try
        {
            DbSelect dsImportError = new("SmImportError A", "A");
            dsImportError.Select("A.*");
            dsImportError.Where("1 = 2");
            var dtImportError = DBHelper.GetDataTable(dsImportError.GetSql(), null);
            DataRow drImportError = null;

            #region 验证当前导入的临时表中的数据是否重复
            string columnName = "Col" + columnIndex.ToString();
            DbSelect dsImpTempDetail = null;
            DataTable dtImpTempDetail = null;
            string sql = @" SELECT DISTINCT {0} AS {0} FROM SmImportDataDetail A
                                 WHERE A.ImportDataId = '{1}' AND A.SheetName='{2}'
                                   AND A.ID > (SELECT MIN(B.ID)
                                                     FROM SmImportDataDetail B
                                                    WHERE B.ImportDataId = A.ImportDataId AND A.SheetName=B.SheetName
                                                      AND B.{0} = A.{0})";
            sql = string.Format(sql, columnName, importDataId, sheetName);
            var dt = DBHelper.GetDataTable(sql, null);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dsImpTempDetail = new("SmImportDataDetail A", "A", null);
                dsImpTempDetail.IsInitDefaultValue = false;
                dsImpTempDetail.Select("A.[LineNo]");
                dsImpTempDetail.Where("A.ImportDataId", "=", importDataId);
                dsImpTempDetail.Where("A.SheetName", "=", sheetName);
                dsImpTempDetail.Where("A." + columnName, "=", dt.Rows[i][columnName].ToString());
                dtImpTempDetail = DBHelper.GetDataTable(dsImpTempDetail.GetSql(), null);
                string itemNos = string.Empty;
                for (int j = 0; j < dtImpTempDetail.Rows.Count; j++)
                {
                    //UpdateImportDataErrorFlag(importDataId, Convert.ToInt32(dtImpTempDetail.Rows[j]["LineNo"]), columnIndex, Resource.GetOnlyMessage("CC-00302"), sheetName, "E", null);
                    itemNos += dtImpTempDetail.Rows[j]["LineNo"].ToString() + ",";
                }
                if (!string.IsNullOrEmpty(itemNos))
                {
                    itemNos = itemNos.Substring(0, itemNos.Length - 1);
                }
                drImportError = dtImportError.NewRow();
                drImportError["ID"] = Utility.GuidId1;
                drImportError["CreatedBy"] = userId;
                //drImportError["CREATED_PROGRAM"] = "CommonImport";
                drImportError["CreatedTime"] = Utility.GetSysDate();
                //drImportError["CompanyId"] = Utility.GetCompanyId();
                drImportError["ImportDataId"] = importDataId;
                drImportError["SheetName"] = sheetName;
                drImportError["ErrorCode"] = "CheckLovCode";
                string errorMsg = "Excel中【{0}】行【{1}】列的数据【{2}】重复！";
                errorMsg = string.Format(errorMsg, itemNos, GetExcelColumnName(columnIndex), dt.Rows[i][0].ToString());
                drImportError["ErrorName"] = errorMsg;
                drImportError["ErrorType"] = "E";
                dtImportError.Rows.Add(drImportError);
            }
            #endregion

            #region 验证临时表和正式表中的数据是否重复
            sql = @" SELECT DISTINCT A.{0} AS {0}
                           FROM SmImportDataDetail A
                          WHERE A.ImportDataId = '{1}' AND A.SheetName='{4}'
                            AND EXISTS (SELECT 1 FROM {2} B
                                         WHERE B.{3} = A.{0}
                                           AND B.IsDeleted='false')";
            sql = string.Format(sql, columnName, importDataId, tableCode, columnCode, sheetName);
            dt = DBHelper.GetDataTable(sql, null);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dsImpTempDetail = new("SmImportDataDetail A", "A", null);
                dsImpTempDetail.IsInitDefaultValue = false;
                dsImpTempDetail.Select("A.[LineNo]");
                dsImpTempDetail.Where("A.ImportDataId", "=", importDataId);
                dsImpTempDetail.Where("A.SheetName", "=", sheetName);
                dsImpTempDetail.Where("A." + columnName, "=", dt.Rows[i][columnName].ToString());
                dtImpTempDetail = DBHelper.GetDataTable(dsImpTempDetail.GetSql(), null);
                string itemNos = string.Empty;
                for (int j = 0; j < dtImpTempDetail.Rows.Count; j++)
                {
                    //UpdateImportDataErrorFlag(importDataId, Convert.ToInt32(dtImpTempDetail.Rows[j]["LineNo"]), columnIndex, Resource.GetOnlyMessage("CC-00303"), sheetName, "E", null);
                    itemNos += dtImpTempDetail.Rows[j]["LineNo"].ToString() + ",";
                }
                if (!string.IsNullOrEmpty(itemNos))
                {
                    itemNos = itemNos.Substring(0, itemNos.Length - 1);
                }
                drImportError = dtImportError.NewRow();
                drImportError["ID"] = Utility.GuidId1;
                drImportError["CreatedBy"] = userId;
                //drImportError["CREATED_PROGRAM"] = "CommonImport";
                drImportError["CreatedTime"] = Utility.GetSysDate();
                //drImportError["CompanyId"] = Utility.GetCompanyId();
                drImportError["ImportDataId"] = importDataId;
                drImportError["SheetName"] = sheetName;
                drImportError["ErrorCode"] = "CheckLovCode";
                string errorMsg = "Excel中【{0}】行【{1}】列的数据【{2}】和系统中已经存在的数据重复！";
                errorMsg = string.Format(errorMsg, itemNos, GetExcelColumnName(columnIndex), dt.Rows[i][0].ToString());
                drImportError["ErrorName"] = errorMsg;
                drImportError["ErrorType"] = "E";
                dtImportError.Rows.Add(drImportError);
                //Excel中【{0}】行【{1}】列的数据【{2}】和已经存在的数据重复！
                //InsertErrorMsg("CheckFieldUnique", Resource.GetOnlyMessage("CC-00263", itemNos, Utility.GetExcelColumnName(columnIndex), dt.Rows[i][columnName].ToString()), "E", sheetName, "CheckFieldUnique");
            }
            DBHelper.BulkInsert(dtImportError, "SmImportError");
            #endregion
        }
        catch (Exception) { throw; }
    }
    #endregion

    #region 验证列中的值是否在LOV中存在
    /// <summary>
    /// 验证列长度
    /// </summary>
    /// <param name="columnIndex"></param>
    /// <param name="fieldLength"></param>
    public static void CheckLovCode1(string importDataId, int columnIndex, string lovCode, string sheetName)
    {
        try
        {
            string columnName = "COL" + columnIndex.ToString();
            DbSelect dsImpTempDetail = null;
            DataTable dtImpTempDetail = null;
            string sql = @" SELECT A.{0} AS LovValue
                                  FROM SmImportDataDetail A
                                 WHERE A.ImportDataId = '{1}' AND A.SheetName='{3}'
                                   AND NOT EXISTS (SELECT 1 FROM SmLov_V B
                                                WHERE B.VALUE = A.{0}
                                                  AND B.LovCode='{2}')";
            sql = string.Format(sql, columnName, importDataId, lovCode, sheetName);
            var dt = DBHelper.GetDataTable(sql, null);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dsImpTempDetail = new("SmImportDataDetail A", "A", null);
                dsImpTempDetail.Select("A.[LineNo]");
                dsImpTempDetail.Where("A.ImportDataId", "=", importDataId);
                dsImpTempDetail.Where("A.SheetName", "=", sheetName);
                dsImpTempDetail.Where("A." + columnName, "=", dt.Rows[i]["LovValue"].ToString());
                dtImpTempDetail = DBHelper.GetDataTable(dsImpTempDetail.GetSql(), null);
                string itemNos = string.Empty;
                for (int j = 0; j < dtImpTempDetail.Rows.Count; j++)
                {
                    //UpdateImportDataErrorFlag(importDataId, Convert.ToInt32(dtImpTempDetail.Rows[j]["LineNo"]), columnIndex, Resource.GetOnlyMessage("CC-00304", lovCode), sheetName, "E", null);
                    //itemNos += dtImpTempDetail.Rows[j]["LineNo"].ToString() + ",";
                }
                if (!string.IsNullOrEmpty(itemNos))
                {
                    itemNos = itemNos.Substring(0, itemNos.Length - 1);
                }
                //Excel中【{0}】行【{1}】列的参数值【{2}】在参数【{3}】中不存在！
                //InsertErrorMsg("CheckLovCode", Resource.GetOnlyMessage("CC-00217", itemNos, Utility.GetExcelColumnName(columnIndex), dt.Rows[i]["LovValue"].ToString(), lovCode), "E", "SM_LOV_MNG", sheetName, "CheckLovCode");
            }
        }
        catch (Exception) { throw; }
    }

    /// <summary>
    /// 验证列中的值是否在LOV中存在
    /// </summary>
    /// <param name="columnIndex"></param>
    /// <param name="lovCode"></param>
    /// <param name="corresTableCode"></param>
    /// <param name="corresColumnCode"></param>
    public static void CheckLovCode(string importDataId, int columnIndex, string lovCode, string corresTableCode, string corresColumnCode, string companyId, string sheetName)
    {
        try
        {
            string columnName = "COL" + columnIndex.ToString();
            DbSelect dsImpTempDetail = null;
            DataTable dtImpTempDetail = null;
            string sql = @" SELECT DISTINCT A.{0} AS CORRES_VALUE
                                  FROM SmImportDataDetail A
                                 WHERE A.ImportDataId = '{1}' AND A.SheetName='{5}'
                                   AND (A.{0} IS NOT NULL AND A.{0}!='')
                                   AND NOT EXISTS (SELECT 1 FROM {2} B
                                                WHERE B.{3} = A.{0} AND B.LovCode='{4}')";
            sql = string.Format(sql, columnName, importDataId, corresTableCode, corresColumnCode, lovCode, sheetName);
            var dt = DBHelper.GetDataTable(sql, null);
            sql = "SELECT TOP 1 LovName FROM SmLov WHERE LovCode='{0}'";
            sql = string.Format(sql, lovCode);
            string lovName = Convert.ToString(DBHelper.ExecuteScalar(sql, null));
            if (string.IsNullOrEmpty(lovName))
            {
                lovName = lovCode;
            }

            #region 准备表结构
            DbSelect dsImportDataDetail = new("SmImportDataDetail A", "A");
            dsImportDataDetail.Select("A.*");
            dsImportDataDetail.Where("1 = 2");
            DataTable dtImportDataDetail = DBHelper.GetDataTable(dsImportDataDetail.GetSql(), null);
            //DataRow drImportDataDetail = null;

            DbSelect dsImportDataErrorCol = new("SmImportDataErrorCol A", "A");
            dsImportDataErrorCol.Select("A.*");
            dsImportDataErrorCol.Where("1 = 2");
            DataTable dtImportDataErrorCol = DBHelper.GetDataTable(dsImportDataErrorCol.GetSql(), null);
            DataRow drImportDataErrorCol = null;

            DbSelect dsImportError = new("SmImportError A", "A");
            dsImportError.Select("A.*");
            dsImportError.Where("1 = 2");
            var dtImportError = DBHelper.GetDataTable(dsImportError.GetSql(), null);
            DataRow drImportError = null;
            #endregion

            var smImportDataDetailList = new List<SmImportDataDetail>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dsImpTempDetail = new DbSelect("SmImportDataDetail A", "A", null);
                dsImpTempDetail.IsInitDefaultValue = false;
                dsImpTempDetail.Select("A.ID,A.[LineNo]");
                dsImpTempDetail.Where("A.ImportDataId", "=", importDataId);
                dsImpTempDetail.Where("A.SheetName", "=", sheetName);
                dsImpTempDetail.Where("A." + columnName, "=", dt.Rows[i]["CORRES_VALUE"].ToString());
                dtImpTempDetail = DBHelper.GetDataTable(dsImpTempDetail.GetSql(), null);
                string itemNos = string.Empty;
                for (int j = 0; j < dtImpTempDetail.Rows.Count; j++)
                {
                    //drImportDataDetail = dtImportDataDetail.NewRow();
                    //drImportDataDetail["ID"] = dtImpTempDetail.Rows[j]["ID"].ToString();
                    //drImportDataDetail["IsError"] = true;
                    //dtImportDataDetail.Rows.Add(drImportDataDetail);
                    //drImportDataDetail.AcceptChanges();
                    //drImportDataDetail.SetModified();
                    SmImportDataDetail smImportDataDetail = new();
                    smImportDataDetail.ID = Guid.Parse(dtImpTempDetail.Rows[j]["ID"].ToString());
                    smImportDataDetail.IsError = true;
                    smImportDataDetailList.Add(smImportDataDetail);

                    drImportDataErrorCol = dtImportDataErrorCol.NewRow();
                    drImportDataErrorCol["ID"] = Utility.GuidId1;
                    //drImportDataErrorCol["CreatedBy"] = UserCode;
                    //drImportDataErrorCol["CREATED_PROGRAM"] = "CommonImport";
                    drImportDataErrorCol["CreatedTime"] = Utility.GetSysDate();
                    //drImportDataErrorCol["CompanyId"] = Utility.GetCompanyId();
                    drImportDataErrorCol["ImportDataId"] = importDataId;
                    drImportDataErrorCol["SheetName"] = sheetName;
                    drImportDataErrorCol["LineNo"] = Convert.ToInt32(dtImpTempDetail.Rows[j]["LineNo"]);
                    drImportDataErrorCol["ColumnNo"] = columnIndex;
                    drImportDataErrorCol["ErrorType"] = "E";
                    drImportDataErrorCol["ErrorMessage"] = "";// Resource.GetOnlyMessage("CC-00304", lovName);
                    dtImportDataErrorCol.Rows.Add(drImportDataErrorCol);
                    //UpdateImportDataErrorFlag(importDataId1, Convert.ToInt32(dtImpTempDetail.Rows[j]["LineNo"]), columnIndex, Resource.GetOnlyMessage("CC-00304", lovCode),sheetName,"E", null);
                    itemNos += dtImpTempDetail.Rows[j]["LineNo"].ToString() + ",";
                }
                if (!string.IsNullOrEmpty(itemNos))
                {
                    itemNos = itemNos.Substring(0, itemNos.Length - 1);
                }
                drImportError = dtImportError.NewRow();
                drImportError["ID"] = Utility.GuidId1;
                //drImportError["CreatedBy"] = UserCode;
                //drImportError["CREATED_PROGRAM"] = "CommonImport";
                drImportError["CreatedTime"] = Utility.GetSysDate();
                drImportError["CompanyId"] = Utility.GetCompanyId();
                drImportError["ImportDataId"] = importDataId;
                drImportError["SheetName"] = sheetName;
                drImportError["ErrorCode"] = "CheckLovCode";
                string errorMsg = "Excel中第【{0}】行【{1}】列的值【{2}】在参数设置中不存在！";
                errorMsg = string.Format(errorMsg, itemNos, GetExcelColumnName(columnIndex), dt.Rows[i]["CORRES_VALUE"].ToString());
                drImportError["ErrorName"] = errorMsg;// Resource.GetOnlyMessage("CC-00217", itemNos, Utility.GetExcelColumnName(columnIndex), dt.Rows[i]["CORRES_VALUE"].ToString(), lovName);
                drImportError["ErrorType"] = "E";
                drImportError["ModuleCode"] = "";
                dtImportError.Rows.Add(drImportError);
                //Excel中【{0}】行【{1}】列的参数值【{2}】在参数【{3}】中不存在！
                //InsertErrorMsg("CheckLovCode", Resource.GetOnlyMessage("CC-00217", itemNos, Utility.GetExcelColumnName(columnIndex), dt.Rows[i]["CORRES_VALUE"].ToString(), lovCode), "E", sheetName,"CheckLovCode");
            }
            //BatchUpdateImportDataDetail(dtImportDataDetail, null);
            //BatchInsertDataErrorCol(dtImportDataErrorCol, null);
            //BatchInsertError(dtImportError, null);
            //DBHelper.BulkInsert(dtImportDataDetail, "SmImportDataDetail");
            DBHelper.UpdateRange(smImportDataDetailList, x => new { x.IsError });
            DBHelper.BulkInsert(dtImportDataErrorCol, "SmImportDataErrorCol");
            DBHelper.BulkInsert(dtImportError, "SmImportError");
        }
        catch (Exception) { throw; }
    }
    #endregion

    #region 验证列中映射表和字段
    /// <summary>
    /// 验证列中映射表和字段
    /// </summary>
    /// <param name="columnIndex"></param>
    /// <param name="fieldLength"></param>
    public static void CheckCorresTable(string importDataId, int columnIndex, string corresTableCode, string corresColumnCode, string companyId, string sheetName, string userId)
    {
        try
        {
            string columnName = "COL" + columnIndex.ToString();
            DataTable dtImpTempDetail = null;
            string sql = string.Empty;
            string corresTableCodeTemp = corresTableCode;
            string corresColumnCodeTemp = corresColumnCode;
            if (corresTableCode == "HR_COMPANY")
            {
                sql = @" SELECT DISTINCT A.{0} AS CORRES_VALUE
                                  FROM SmImportDataDetail A
                                 WHERE A.ImportDataId = '{1}' AND A.SheetName='{4}'
                                   AND (A.{0} IS NOT NULL AND A.{0}!='')
                                   AND NOT EXISTS (SELECT 1 FROM {2} B
                                                WHERE B.{3} = A.{0} AND B.IsDeleted='false')";
            }
            else
            {
                sql = @" SELECT DISTINCT A.{0} AS CORRES_VALUE
                                  FROM SmImportDataDetail A
                                 WHERE A.ImportDataId = '{1}' AND A.SheetName='{4}'
                                   AND (A.{0} IS NOT NULL AND A.{0}!='')
                                   AND NOT EXISTS (SELECT 1 FROM {2} B
                                                WHERE B.{3} = A.{0} AND B.IsDeleted='false')";
            }
            sql = string.Format(sql, columnName, importDataId, corresTableCode, corresColumnCode, sheetName);
            var dt = DBHelper.GetDataTable(sql, null);

            sql = "SELECT * FROM SmTableCatalog WHERE TableCode='{0}'";
            sql = string.Format(sql, corresTableCode);
            var table = DBHelper.QueryFirst<SmTableCatalog>(sql, null);
            sql = "SELECT * FROM SmFieldCatalog WHERE TableCode='{0}' AND ColumnCode='{1}'";
            sql = string.Format(sql, corresTableCode, corresColumnCode);
            var field = DBHelper.QueryFirst<SmFieldCatalog>(sql, null);
            corresTableCodeTemp = table.TableName;
            corresColumnCodeTemp = field.ColumnName;

            #region 准备表结构
            DbSelect dsImportDataDetail = new("SmImportDataDetail A", "A");
            dsImportDataDetail.Select("A.*");
            //dsImportDataDetail.Select("A.ID,a.IsError");
            dsImportDataDetail.Where("1 = 2");
            var dtImportDataDetail = DBHelper.GetDataTable(dsImportDataDetail.GetSql(), null);
            //DataRow drImportDataDetail = null;

            DbSelect dsImportDataErrorCol = new("SmImportDataErrorCol A", "A");
            dsImportDataErrorCol.Select("A.*");
            //dsImportDataErrorCol.Select("A.ID,A.CreatedBy,A.CREATED_PROGRAM,A.CreatedTime,A.CompanyId,A.ImportDataId,A.SheetName,A.LineNo,A.ColumnNo,A.ErrorType,A.ErrorMessage");
            dsImportDataErrorCol.Where("1 = 2");
            var dtImportDataErrorCol = DBHelper.GetDataTable(dsImportDataErrorCol.GetSql(), null);
            //DataRow drImportDataErrorCol = null;

            DbSelect dsImportError = new("SmImportError A", "A");
            dsImportError.Select("A.*");
            //dsImportError.Select("A.ID,A.CreatedBy,A.CREATED_PROGRAM,A.CreatedTime,A.CompanyId,A.ImportDataId,A.SheetName,A.ErrorCode,A.ErrorName,A.ErrorType,A.ModuleCode");
            dsImportError.Where("1 = 2");
            var dtImportError = DBHelper.GetDataTable(dsImportError.GetSql(), null);
            //DataRow drImportError = null;
            #endregion
            string errorMsg = "在【{0}】中的【{1}】不存在！";
            errorMsg = string.Format(errorMsg, corresTableCodeTemp, corresColumnCodeTemp);
            string errorMsg1 = string.Empty;

            var smImportDataDetailList = new List<SmImportDataDetail>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                sql = "SELECT A.ID,A.[LineNo] FROM SmImportDataDetail A WHERE A.ImportDataId = '{0}' AND A.SheetName = N'{1}' AND A." + columnName + "=N'{2}'";
                sql = string.Format(sql, importDataId, sheetName, dt.Rows[i]["CORRES_VALUE"].ToString());
                dtImpTempDetail = DBHelper.GetDataTable(sql, null);
                string itemNos = string.Empty;
                for (int j = 0; j < dtImpTempDetail.Rows.Count; j++)
                {
                    //DataRow drImportDataDetail = dtImportDataDetail.NewRow();
                    //drImportDataDetail["ID"] = dtImpTempDetail.Rows[j]["ID"].ToString();
                    //drImportDataDetail["IsError"] = true;
                    //dtImportDataDetail.Rows.Add(drImportDataDetail);
                    //drImportDataDetail.AcceptChanges();
                    //drImportDataDetail.SetModified();
                    SmImportDataDetail smImportDataDetail = new();
                    smImportDataDetail.ID = Guid.Parse(dtImpTempDetail.Rows[j]["ID"].ToString());
                    smImportDataDetail.IsError = true;
                    smImportDataDetailList.Add(smImportDataDetail);

                    var drImportDataErrorCol = dtImportDataErrorCol.NewRow();
                    drImportDataErrorCol["ID"] = Utility.GuidId1;
                    drImportDataErrorCol["CreatedBy"] = userId;
                    //drImportDataErrorCol["CREATED_PROGRAM"] = "CommonImport";
                    drImportDataErrorCol["CreatedTime"] = Utility.GetSysDate();
                    //drImportDataErrorCol["CompanyId"] = Utility.GetCompanyId();
                    drImportDataErrorCol["ImportDataId"] = importDataId;
                    drImportDataErrorCol["SheetName"] = sheetName;
                    drImportDataErrorCol["LineNo"] = Convert.ToInt32(dtImpTempDetail.Rows[j]["LineNo"]);
                    drImportDataErrorCol["ColumnNo"] = columnIndex;
                    drImportDataErrorCol["ErrorType"] = "E";
                    drImportDataErrorCol["ErrorMessage"] = errorMsg;
                    dtImportDataErrorCol.Rows.Add(drImportDataErrorCol);
                    //UpdateImportDataErrorFlag(importDataId1, Convert.ToInt32(dtImpTempDetail.Rows[j]["LineNo"]), columnIndex, Resource.GetOnlyMessage("CC-00305", corresTableCodeTemp, corresColumnCodeTemp), sheetName, "E", null);
                    itemNos += dtImpTempDetail.Rows[j]["LineNo"].ToString() + ",";
                }
                if (!string.IsNullOrEmpty(itemNos))
                {
                    itemNos = itemNos.Substring(0, itemNos.Length - 1);
                }

                #region Excel中【{0}】行【{1}】列的值【{2}】在表【{3}】的列【{4}】中不存在！
                errorMsg1 = "Excel中第【{0}】行【{1}】列的值【{2}】在【{3}】对应的【{4}】中不存在！";
                var drImportError = dtImportError.NewRow();
                drImportError["ID"] = Utility.GuidId1;
                drImportError["CreatedBy"] = userId;
                //drImportError["CREATED_PROGRAM"] = "CommonImport";
                drImportError["CreatedTime"] = Utility.GetSysDate();
                //drImportError["CompanyId"] = Utility.GetCompanyId();
                drImportError["ImportDataId"] = importDataId;
                drImportError["SheetName"] = sheetName;
                drImportError["ErrorCode"] = "CheckCorresTable";
                errorMsg1 = string.Format(errorMsg1, itemNos, GetExcelColumnName(columnIndex), dt.Rows[i]["CORRES_VALUE"].ToString(), corresTableCodeTemp, corresColumnCodeTemp);
                drImportError["ErrorName"] = errorMsg1;
                drImportError["ErrorType"] = "E";
                drImportError["ModuleCode"] = "";
                dtImportError.Rows.Add(drImportError);
                #endregion
            }
            //DBHelper.ExecuteBatchUpdate("SmImportDataDetail", dtImportDataDetail, "ID", null, null, null);
            //DBHelper.ExecuteBatchInsert("SmImportDataErrorCol", dtImportDataErrorCol, null);
            //DBHelper.ExecuteBatchInsert("SmImportError", dtImportError, null);
            //DBHelper.BulkInsert(dtImportDataDetail, "SmImportDataDetail");
            if (smImportDataDetailList.Count > 0)
            {
                DBHelper.UpdateRange(smImportDataDetailList, x => new { x.IsError });
                DBHelper.BulkInsert(dtImportDataErrorCol, "SmImportDataErrorCol");
                DBHelper.BulkInsert(dtImportError, "SmImportError");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region 取得EXCEL表中的字段名称(值1时返回A)
    /// <summary>
    /// 取得EXCEL表中的字段名称(值1时返回A)
    /// </summary>
    /// <param name="index">索引从1开始</param>
    /// <returns></returns>
    public static string GetExcelColumnName(int index)
    {
        try
        {
            string[] StringArray = new string[] {"A","B","C","D","E","F","G","H","I","J","K","L",
                                                        "M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"};
            string ReturnValue = "";
            int iQuotient = 0;
            int iRemainder = 0;
            iQuotient = index / 26;
            iRemainder = index % 26;
            if (index <= 26)
            {
                ReturnValue = StringArray[index - 1];
            }
            else
            {
                if (iQuotient > 0 && iRemainder == 0)
                {
                    ReturnValue = StringArray[iQuotient - 2] + StringArray[StringArray.Length - 1];
                }
                else if (iQuotient > 0 && iRemainder > 0)
                {
                    ReturnValue = StringArray[iQuotient - 1] + StringArray[iRemainder - 1];
                }
            }
            return ReturnValue;
        }
        catch (Exception E)
        {
            throw new Exception(E.Message);
        }
    }
    #endregion

    #region 直接转换进入正式表
    /// <summary>
    /// 直接转换进入正式表
    /// </summary>
    /// <param name="importDataId">导入数据ID</param>
    /// <param name="importTemplateCode">模板代码</param>
    /// <param name="userId">用户ID</param>
    /// <param name="isImportLineNo">是否导入序号</param>
    public static void TransferData(string importDataId, string importTemplateCode, string userId, bool isImportLineNo)
    {
        try
        {
            #region 变量定义
            string sql = $"SELECT * FROM SmImpTemplate WHERE TemplateCode='{importTemplateCode}'";
            var impTemplate = DBHelper.QueryFirst<SmImpTemplate>(sql);
            if (impTemplate == null)
                throw new Exception("Excel导入模板代码【" + importTemplateCode + "】不存在！");

            string fieldName = string.Empty;
            string isUnique = string.Empty;
            string columnName = string.Empty;
            int columnIndex = -1;
            string tableCode = impTemplate.TableCode;
            string lovCode = string.Empty;
            string corresTableCode = string.Empty;
            string corresColumnCode = string.Empty;
            string transColumnCode = string.Empty;
            string corresSql = string.Empty;
            string isInsert = string.Empty;
            string isEncrypt = "False";
            string sheetName = impTemplate.SheetName;
            #endregion

            #region 求导入模板子表
            DbSelect dsImpTempDetail = new("SmImpTemplateDetail A", "A", null);
            dsImpTempDetail.Select("A.*");
            dsImpTempDetail.Where("A.ImpTemplateId", "=", impTemplate.ID);
            dsImpTempDetail.OrderBy("A.ColumnNo", "ASC");
            var dtImpTempDetail = DBHelper.GetDataTable(dsImpTempDetail.GetSql());
            #endregion

            #region 把数据插入到另一个表
            sql = "DELETE FROM SmImportDataDetailTemp WHERE ImportDataId='{0}' AND SheetName='{1}'";
            sql = string.Format(sql, importDataId, sheetName);
            DBHelper.ExecuteDML(sql);
            sql = @"INSERT INTO SmImportDataDetailTemp
                        SELECT *
                          FROM SmImportDataDetail
                         WHERE ImportDataId = '{0}' AND SheetName='{1}' AND (IsError!='true' OR IsError IS NULL OR IsError='')";
            sql = string.Format(sql, importDataId, sheetName);
            DBHelper.ExecuteDML(sql);
            #endregion

            #region 更新另一个表
            string insertColumn = "ID,CreatedBy,CreatedTime,TAG,ImportDataId,GroupId,CompanyId,IsActive,IsDeleted";
            string selectColumn = "NEWID()" + ",'" + userId + "','" + Utility.GetSysDate().ToString() + "',1,'" + importDataId + "','" + Utility.GetGroupId() + "','" + Utility.GetCompanyId() + "','true','false'";
            if (isImportLineNo == true)
            {
                insertColumn += ",[LineNo],SheetName";
                selectColumn += ",[LineNo],SheetName";
            }
            for (int j = 0; j < dtImpTempDetail.Rows.Count; j++)
            {
                fieldName = dtImpTempDetail.Rows[j]["ColumnCode"].ToString();
                isUnique = dtImpTempDetail.Rows[j]["IsUnique"].ToString();
                lovCode = dtImpTempDetail.Rows[j]["LovCode"].ToString();
                corresTableCode = dtImpTempDetail.Rows[j]["CorresTableCode"].ToString();
                corresColumnCode = dtImpTempDetail.Rows[j]["CorresColumnCode"].ToString();
                transColumnCode = dtImpTempDetail.Rows[j]["TransColumnCode"].ToString();
                isEncrypt = dtImpTempDetail.Rows[j]["IsEncrypt"].ToString();
                isInsert = dtImpTempDetail.Rows[j]["IsInsert"].ToString();
                if (!string.IsNullOrEmpty(fieldName))
                {
                    #region 处理映射表的情况
                    if (!string.IsNullOrEmpty(lovCode) && !string.IsNullOrEmpty(corresTableCode) && !string.IsNullOrEmpty(corresColumnCode) && !string.IsNullOrEmpty(transColumnCode))
                    {
                        columnIndex = Convert.ToInt32(dtImpTempDetail.Rows[j]["ColumnNo"]);
                        columnName = "COL" + columnIndex.ToString();
                        sql = @"UPDATE A SET A.{0}=B.{1}
                                      FROM SmImportDataDetailTemp A,{2} B
                                     WHERE B.{3}=A.{0} AND B.LovCode='{4}' AND A.ImportDataId='{5}' AND A.SheetName='{6}' AND B.IsDeleted='false'";
                        sql = string.Format(sql, columnName, transColumnCode, corresTableCode, corresColumnCode, lovCode, importDataId, sheetName);
                        DBHelper.ExcuteNonQuery(sql);
                    }
                    else if (!string.IsNullOrEmpty(corresTableCode) && !string.IsNullOrEmpty(corresColumnCode) && !string.IsNullOrEmpty(transColumnCode))
                    {
                        columnIndex = Convert.ToInt32(dtImpTempDetail.Rows[j]["ColumnNo"]);
                        columnName = "COL" + columnIndex.ToString();
                        sql = @"UPDATE A SET A.{0}=B.{1}
                                          FROM SmImportDataDetailTemp A,{2} B
                                         WHERE B.{3}=A.{0} AND A.ImportDataId='{4}' AND A.SheetName='{5}' AND B.IsDeleted='false'";
                        sql = string.Format(sql, columnName, transColumnCode, corresTableCode, corresColumnCode, importDataId, sheetName);
                        DBHelper.ExcuteNonQuery(sql);
                    }
                    #endregion
                    else
                    {
                        columnIndex = Convert.ToInt32(dtImpTempDetail.Rows[j]["ColumnNo"]);
                        columnName = "COL" + columnIndex.ToString();
                    }
                    if (isInsert == "False")
                    {
                    }
                    else
                    {
                        insertColumn += "," + fieldName;
                        if (isEncrypt == "True")
                        {
                            selectColumn += ",encryptbykey(key_guid('fookey')," + columnName + ")";
                        }
                        else
                        {
                            selectColumn += "," + columnName;
                        }
                    }
                }
            }

            sql = "INSERT INTO {0}({1}) SELECT {2} FROM SmImportDataDetailTemp WHERE ImportDataId='{3}' AND SheetName='{4}'";
            sql = string.Format(sql, tableCode, insertColumn, selectColumn, importDataId, sheetName);
            DBHelper.ExecuteDML(sql);

            sql = "DELETE FROM SmImportDataDetailTemp WHERE ImportDataId='{0}' AND SheetName='{1}'";
            sql = string.Format(sql, importDataId, sheetName);
            DBHelper.ExecuteDML(sql);
            #endregion
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region 插入错误
    private static void InsertErrorMsg(string importDataId, string errorCode, string errorName, string errorType, string moduleCode, string sheetName, string programName)
    {
        try
        {
            //DbInsert diError = new DbInsert("SmImportError", programName);
            //diError.Values("ImportDataId", importDataId);
            //diError.Values("SheetName", sheetName);
            //diError.Values("ErrorCode", errorCode);
            //diError.Values("ErrorName", errorName);
            //diError.Values("ErrorType", errorType);
            //diError.Values("ModuleCode", moduleCode);
            //DBHelper.ExecuteDML(diError.GetSql());
        }
        catch (Exception) { throw; }
    }
    /// <summary>
    /// 插入错误
    /// </summary>
    /// <param name="errorCode">错误代码</param>
    /// <param name="errorName">错误描述</param>
    /// <param name="errorType">E:错误,W:警告</param>
    /// <param name="programName">程序名称</param>
    private static void InsertErrorMsg(string errorCode, string errorName, string errorType, string sheetName, string programName)
    {
        try
        {
            //InsertErrorMsg(errorCode, errorName, errorType, null, sheetName, programName);
        }
        catch (Exception) { throw; }
    }
    #endregion

    #region 获取导入错误
    /// <summary>
    /// 获取导入错误
    /// </summary>
    /// <param name="importDataId">导入数据ID</param>
    /// <returns></returns>
    public static List<SmImportError> GetImportErrorList(string importDataId)
    {
        try
        {
            string sql = "SELECT * FROM SmImportError WHERE ImportDataId='{0}'";
            sql = string.Format(sql, importDataId);
            var list = DBHelper.QueryList<SmImportError>(sql);
            return list;
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region 获取导入明细

    //public static List<SmImportDataDetail> GetImportDataDetailList(string importDataId)
    //{
    //    try
    //    {
    //        string sql = "SELECT * FROM SmImportDataDetail WHERE ImportDataId='{0}' AND IsDeleted='false' ORDER BY [LineNo] ASC";
    //        sql = string.Format(sql, importDataId);
    //        List<SmImportDataDetail> list = DBHelper.QueryList<SmImportDataDetail>(sql);
    //        return list;
    //    }
    //    catch (Exception)
    //    {
    //        throw;
    //    }
    //}
    /// <summary>
    /// 获取导入明细
    /// </summary>
    /// <param name="importDataId">导入数据ID</param>
    /// <param name="importTemplateId">导入模板ID</param>
    /// <returns></returns>
    public static DataTable GetImportDataDetailList(string importDataId, Guid importTemplateId)
    {
        try
        {
            DataTable dt = null;
            DbSelect dsImpTemplateDetail = new("SmImpTemplateDetail A", "A", null);
            dsImpTemplateDetail.Select("A.*");
            dsImpTemplateDetail.Where("A.ImpTemplateId", "=", importTemplateId);
            dsImpTemplateDetail.OrderBy("A.ColumnNo", "ASC");
            DataTable dtImpTemplateDetail = DBHelper.GetDataTable(dsImpTemplateDetail.GetSql());

            string tempColumnCode = string.Empty;
            DbSelect ds = new("SmImportDataDetail A", "A", null);
            ds.Select("TOP 10 A.[LineNo]", "行号");
            if (dtImpTemplateDetail.Rows.Count > 0)
            {
                for (int i = 1; i < dtImpTemplateDetail.Rows.Count + 1; i++)
                {
                    if (!string.IsNullOrEmpty(dtImpTemplateDetail.Rows[i - 1]["Remark"].ToString()))
                    {
                        ds.Select("A.Col" + dtImpTemplateDetail.Rows[i - 1]["ColumnNo"].ToString(), dtImpTemplateDetail.Rows[i - 1]["Remark"].ToString());
                    }
                    else
                    {
                        tempColumnCode = GetImportTemplateDetailColumnCode(importTemplateId, dtImpTemplateDetail.Rows[i - 1]["ColumnNo"].ToString());
                        if (!string.IsNullOrEmpty(tempColumnCode))
                        {
                            ds.Select("A.Col" + dtImpTemplateDetail.Rows[i - 1]["ColumnNo"].ToString(), tempColumnCode);
                        }
                        else
                        {
                            ds.Select("A.Col" + dtImpTemplateDetail.Rows[i - 1]["ColumnNo"].ToString());
                        }
                    }
                }
            }
            ds.Where("A.ImportDataId", "=", importDataId);
            string sql = ds.GetSql();
            //List<SmImportDataDetail> list = DBHelper.QueryList<SmImportDataDetail>(sql);
            dt = DBHelper.GetDataTable(ds.GetSql());
            return dt;
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region 求导入模板从表的字段名
    /// <summary>
    /// 求导入模板从表的字段名
    /// </summary>
    /// <param name="importTemplateId"></param>
    /// <param name="columnNo"></param>
    /// <returns></returns>
    public static string GetImportTemplateDetailColumnCode(Guid importTemplateId, string columnNo)
    {
        try
        {
            string result = string.Empty;

            string sql = "SELECT ColumnCode FROM SmImpTemplateDetail WHERE ImpTemplateId='{0}' AND ColumnNo='{1}'";
            sql = string.Format(sql, importTemplateId, columnNo);
            result = Convert.ToString(DBHelper.ExecuteScalar(sql));
            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region 数据转换后执行
    /// <summary>
    /// 数据转换后执行
    /// </summary>
    /// <param name="templateCode">模板代码</param>
    /// <param name="importDataId">导入数据ID</param>
    /// <param name="masterId">masterId</param>
    public static void AfterImport(string templateCode, string importDataId, string masterId)
    {
        try
        {
            switch (templateCode)
            {
                #region 库存初始化
                case "IV_INIT":
                    {
                        string sql = string.Empty;
                        sql = @"UPDATE A
                                    SET A.OrderId = '{1}'
                                    FROM IvInitDetail A
                                    WHERE     A.OrderId IS NULL
                                          AND A.ImportDataId = '{0}';";

                        sql = sql + @"UPDATE A
                                    SET A.SerialNumber = C.NUM
                                    FROM IvInitDetail A
                                         JOIN
                                         (SELECT *, ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) NUM
                                          FROM (SELECT A.*
                                                FROM IvInitDetail A
                                                WHERE     A.IsDeleted = 'false'
                                                      AND A.OrderId = '{1}'
                                                      AND A.IsActive = 'true') B) C
                                            ON A.ID = C.ID";
                        sql = string.Format(sql, importDataId, masterId);
                        DBHelper.ExecuteDML(sql);

                        break;
                    }
                #endregion

                #region 默认
                default:
                    {
                        // CommonImport.TransferData(importDataId, importTemplateId, UserCode, SaveGroupId, CompanyId, null);
                        break;
                    }
                    #endregion
            }
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion
}
