using EU.Core.Common.Enums;
using EU.Core.Common.Extensions;
using EU.Core.Model.Entity;
using NPOI.SS.UserModel;
using SqlSugar;
using System.Data;
using System.Text;
using static EU.Core.Model.Consts;

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
    public static async Task ImportData(ISqlSugarClient Db, SmImpTemplate smImpTemplate, Guid importDataId, string filePath, string fileName, DataTable dt)
    {
        try
        {
            SmImportData importData = new()
            {
                ID = importDataId,
                ImportFileName = filePath + fileName,
            };
            await Db.Insertable(importData).ExecuteCommandAsync();

            var importDataDetails = new List<SmImportDataDetail>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                SmImportDataDetail detail = new()
                {
                    ImportDataId = importDataId,
                    SheetName = smImpTemplate.SheetName,
                    LineNo = i + 2,
                    DataType = ImportDataType.Detail.ObjToString()
                };
                for (int j = 0; j < dt.Columns.Count; j++)
                    detail.SetPropertyValue("Col" + (j + 1), dt.Rows[i][j].ObjToString());
                importDataDetails.Add(detail);
            }
            await Db.Insertable(importDataDetails).ExecuteCommandAsync();

            //#region 记录模块操作日志
            //try
            //{
            //    DBHelper.RecordOperateLog(userId, smImpTemplate.ModuleCode, "SmImportDataDetail", "", OperateType.Import);
            //}
            //catch { }
            //#endregion

            int count = await Check(Db, importDataId, Utility.GetUserIdString(), Utility.GetGroupId(), Utility.GetCompanyId(), smImpTemplate.TemplateCode);
            if (count > 0)
                throw new Exception("导入文件中存在错误！");

            //TransferData(importDataId, smImpTemplate.TemplateCode, userId, false);
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// 导入Excel数据（优化版本）
    /// </summary>
    /// <param name="Db">数据库客户端</param>
    /// <param name="importDataId">导入数据ID</param>
    /// <param name="template">导入模板</param>
    /// <param name="filePath">文件路径</param>
    public static async Task ImportData(ISqlSugarClient Db, Guid importDataId, SmImpTemplate template, string filePath)
    {
        // 参数验证
        ArgumentNullException.ThrowIfNull(template);
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        var fullPath = FileHelper.GetPhysicsPath() + filePath;
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"文件不存在: {fullPath}");

        // 预先获取模板详情，避免在事务中执行查询
        var templateDetails = await Db.Queryable<SmImpTemplateDetail>()
            .Where(x => x.ImpTemplateId == template.ID)
            .OrderBy(x => x.ColumnNo)
            .ToListAsync();

        if (!templateDetails.Any())
            throw new InvalidOperationException("导入模板没有配置字段详情");

        // 使用事务确保数据一致性
        await Db.Ado.BeginTranAsync();
        try
        {
            // 创建导入数据记录
            var importData = new SmImportData
            {
                ID = importDataId,
                ImportFileName = filePath,
                CreatedTime = DateTime.Now
            };
            await Db.Insertable(importData).ExecuteCommandAsync();

            // 处理Excel文件（在事务外处理文件读取以减少事务时间）
            var importDataDetails = ProcessExcelFile(fullPath, template, templateDetails, importDataId);

            // 批量插入数据 - 使用更大的批次大小提高性能
            if (importDataDetails.Count > 0)
            {
                const int batchSize = 2000; // 增加批次大小
                var tasks = new List<Task>();

                for (int i = 0; i < importDataDetails.Count; i += batchSize)
                {
                    var batch = importDataDetails.Skip(i).Take(batchSize).ToList();
                    // 使用 ExecuteCommandAsync 而不是并行任务，避免数据库连接问题
                    await Db.Insertable(batch).ExecuteCommandAsync();
                }
            }

            await Db.Ado.CommitTranAsync();
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    /// <summary>
    /// 处理Excel文件并提取数据
    /// </summary>
    private static List<SmImportDataDetail> ProcessExcelFile(
        string fullPath,
        SmImpTemplate template,
        List<SmImpTemplateDetail> templateDetails,
        Guid importDataId)
    {
        var importDataDetails = new List<SmImportDataDetail>();
        var sheetName = template.SheetName;
        var startRow = template.StartRow ?? 0;

        using var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        using var workbook = WorkbookFactory.Create(fileStream);

        if (workbook?.NumberOfSheets == 0)
            throw new InvalidOperationException("Excel文件无效或没有工作表");

        var sheet = GetWorksheet(workbook, sheetName);

        #region 处理主数据
        var masterDetails = templateDetails
           .Where(x => x.DataType == ImportDataType.Master.ObjToString())
           .ToList();

        foreach (var templateDetail in masterDetails)
        {
            try
            {
                var rowIndex = (templateDetail.RowNo ?? 1) - 1;
                var columnIndex = (templateDetail.ColumnNo ?? 1) - 1;

                var row = sheet.GetRow(rowIndex);
                if (row == null) continue;

                var cell = row.GetCell(columnIndex);
                var value = NPOIHelper.GetCellValue(cell);

                if (string.IsNullOrEmpty(value)) continue;

                var detail = new SmImportDataDetail
                {
                    ImportDataId = importDataId,
                    SheetName = sheetName,
                    LineNo = templateDetail.RowNo,
                    DataType = ImportDataType.Master.ObjToString()
                };

                detail.SetPropertyValue($"Col{templateDetail.ColumnNo}", value);
                importDataDetails.Add(detail);
            }
            catch (Exception ex)
            {
                // 记录错误但继续处理
                Console.WriteLine($"处理主数据时出错 (行:{templateDetail.RowNo}, 列:{templateDetail.ColumnNo}): {ex.Message}");
            }
        }
        #endregion

        #region 处理详细数据
        var detailTemplates = templateDetails
            .Where(x => x.DataType != ImportDataType.Master.ObjToString())
            .ToList();

        if (detailTemplates.Any())
            for (int rowIndex = startRow + 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null) continue;

                // 检查第一个单元格是否有数据，如果没有则跳过整行
                var firstCell = row.GetCell(row.FirstCellNum);
                if (firstCell == null || string.IsNullOrEmpty(firstCell.ToString().Trim()))
                    continue;

                var detail = CreateDetailRecord(row, detailTemplates, importDataId, sheetName, rowIndex);
                if (detail != null)
                    importDataDetails.Add(detail);
            }
        #endregion

        return importDataDetails;
    }

    /// <summary>
    /// 获取工作表
    /// </summary>
    private static ISheet GetWorksheet(IWorkbook workbook, string sheetName)
    {
        if (string.IsNullOrEmpty(sheetName))
            return workbook.GetSheetAt(0);

        var sheetIndex = workbook.GetSheetIndex(sheetName);
        if (sheetIndex >= 0)
            return workbook.GetSheetAt(sheetIndex);

        throw new InvalidOperationException($"未找到工作表: {sheetName}");
    }

    /// <summary>
    /// 创建详细数据记录
    /// </summary>
    private static SmImportDataDetail CreateDetailRecord(
        IRow row,
        List<SmImpTemplateDetail> detailTemplates,
        Guid importDataId,
        string sheetName,
        int rowIndex)
    {
        var detail = new SmImportDataDetail
        {
            ImportDataId = importDataId,
            SheetName = sheetName,
            LineNo = rowIndex + 2, // Excel行号从1开始，加上标题行
            DataType = ImportDataType.Detail.ObjToString()
        };

        bool hasValidData = false;

        foreach (var template in detailTemplates)
        {
            try
            {
                var columnIndex = (template.ColumnNo ?? 1) - 1;
                var cell = row.GetCell(columnIndex);
                var value = NPOIHelper.GetCellValue(cell);

                if (!string.IsNullOrEmpty(value))
                {
                    hasValidData = true;
                }

                detail.SetPropertyValue($"Col{template.ColumnNo}", value);
            }
            catch (Exception ex)
            {
                // 记录单元格处理错误但继续处理其他单元格
                Console.WriteLine($"处理单元格数据时出错 (行:{rowIndex + 1}, 列:{template.ColumnNo}): {ex.Message}");
            }
        }

        return hasValidData ? detail : null;
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
    public static async Task<int> Check(ISqlSugarClient Db, Guid importDataId, string userId, string saveGroupId, string companyId, string importTemplateCode)
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
            string importDataId1 = importDataId.ObjToString();
            string moduleId = string.Empty;
            string UserCode = userId;
            string GroupId = saveGroupId;
            string CompanyId = companyId;
            #endregion

            #region 删除原有验证错误和更新错误标志
            // 使用事务避免 MultipleActiveResultSets 问题
            await Db.Ado.BeginTranAsync();
            try
            {
                await Db.Deleteable<SmImportError>().Where(it => it.ImportDataId == importDataId).ExecuteCommandAsync();
                await Db.Deleteable<SmImportDataErrorCol>().Where(it => it.ImportDataId == importDataId).ExecuteCommandAsync();
                await Db.Updateable<SmImportDataDetail>()
                    .SetColumns(it => new SmImportDataDetail() { IsError = null }, true)
                    .Where(it => it.ImportDataId == importDataId)
                    .ExecuteCommandAsync();
                await Db.Ado.CommitTranAsync();
            }
            catch
            {
                await Db.Ado.RollbackTranAsync();
                throw;
            }
            #endregion

            var impTemplate = await Db.Queryable<SmImpTemplate>().Where(x => x.TemplateCode == importTemplateCode).FirstAsync();

            if (impTemplate == null)
                throw new Exception("Excel导入模板ID【" + importTemplateCode + "】不存在！");

            string label = impTemplate.Label;
            sheetName = impTemplate.SheetName;

            #region 求导入模板对应表名
            tableCode = impTemplate.TableCode;
            #endregion

            #region 求导入模板子表并进行验证
            var dtImpTemplateDetail = await Db.Queryable<SmImpTemplateDetail>().Where(it => it.ImpTemplateId == impTemplate.ID && it.DataType == ImportDataType.Detail.ObjToString()).ToListAsync();

            for (int i = 0; i < dtImpTemplateDetail.Count; i++)
            {
                var detail = dtImpTemplateDetail[i];
                string columnCode = detail.ColumnCode;
                int columnIndex = detail.ColumnNo ?? 0;

                #region 验证数据类型
                await CheckFieldType(Db, importDataId, columnIndex, tableCode, columnCode, sheetName);
                #endregion

                #region 日期格式
                string dateFormat = detail.DateFormate;
                if (dateFormat.IsNotEmptyOrNull())
                    await CheckFieldFormat(Db, importDataId, columnIndex, dateFormat, sheetName);
                #endregion

                #region 最大长度
                int? maxLength = detail.MaxLength;
                if (maxLength.IsNotEmptyOrNull())
                    await CheckFieldLength(Db, importDataId, columnIndex, maxLength ?? 10000, sheetName);
                #endregion

                #region 允许为空
                if (detail.IsAllowNull == false)
                    await CheckFieldIsNull(Db, importDataId, columnIndex, sheetName, userId);
                #endregion

                #region 唯一性检查 
                if (detail.IsUnique == true)
                    await CheckFieldUnique(Db, importDataId, columnIndex, tableCode, columnCode, companyId, sheetName, userId);
                #endregion

                #region 参数值
                string lovCode = detail.LovCode;
                string corresTableCode = detail.CorresTableCode;
                string corresColumnCode = detail.CorresColumnCode;
                Guid? commonListSqlId = detail.CommonListSqlId;
                if (!string.IsNullOrEmpty(lovCode))
                    await CheckLovCode(Db, importDataId, columnIndex, lovCode, corresTableCode, corresColumnCode, companyId, sheetName);
                #endregion

                #region 映射表和映射字段
                else if (!string.IsNullOrEmpty(corresTableCode) && !string.IsNullOrEmpty(corresColumnCode))
                    await CheckCorresTable(Db, importDataId, columnIndex, corresTableCode, corresColumnCode, companyId, sheetName, userId, commonListSqlId);
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

            return await Db.Queryable<SmImportError>().Where(it => it.ImportDataId == importDataId).CountAsync();
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
    public static async Task CheckFieldType(ISqlSugarClient Db, Guid importDataId, int columnIndex, string tableCode, string fieldName, string sheetName)
    {
        try
        {
            string fieldType = string.Empty;

            var field = await Db.Queryable<SmFieldCatalog>()
                .Where(x => x.TableCode == tableCode && x.ColumnCode == fieldName)
                .FirstAsync();

            if (field.IsNotEmptyOrNull())
            {
                fieldType = field.DataType;

                if (fieldType == "STRING")
                {
                    return;
                }
                else
                {
                    string columnName = $"Col{columnIndex}";
                    var dtImpDataDetail = await Db.Queryable<SmImportDataDetail>()
                        .OrderBy(x => x.LineNo)
                        .Where(x => x.ImportDataId == importDataId && x.SheetName == sheetName)
                        .ToListAsync();
                    //DbSelect dsImpDataDetail = new("SmImportDataDetail A", "A", null);
                    //dsImpDataDetail.Select("A." + columnName);
                    //dsImpDataDetail.Select("A.[LineNo]");
                    //dsImpDataDetail.Where("A.ImportDataId", "=", importDataId);
                    //dsImpDataDetail.Where("A.SheetName", "=", sheetName);
                    //dsImpDataDetail.OrderBy("A.[LineNo]", "ASC");
                    //var dtImpDataDetail = DBHelper.GetDataTable(dsImpDataDetail.GetSql(), null);
                    //bool result = true;
                    if (fieldType == "DATE")
                    {
                        for (int i = 0; i < dtImpDataDetail.Count; i++)
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
                        for (int i = 0; i < dtImpDataDetail.Count; i++)
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
    public static async Task CheckFieldFormat(ISqlSugarClient Db, Guid importDataId, int columnIndex, string fieldFormat, string sheetName)
    {
        try
        {
            string columnName = $"Col{columnIndex}";

            var selector = new List<SelectModel>() {
                new SelectModel(){  FieldName = columnName},
                new SelectModel(){ FieldName = "LineNo"} };

            var dtImpDataDetail = await Db.Queryable<SmImportDataDetail>().Select(selector)
                     .Where(x => x.ImportDataId == importDataId && x.SheetName == sheetName)
                     .ToListAsync();

            for (int i = 0; i < dtImpDataDetail.Count; i++)
            {
                var detail = dtImpDataDetail[i];
                var isDateFormat = detail.GetPropertyValue(columnName).IsDateTimeFormat(fieldFormat);
                //if (!isDateFormat)
                //{
                //    UpdateImportDataErrorFlag(importDataId1, Convert.ToInt32(dt.Rows[i]["LineNo"]), columnIndex, Resource.GetOnlyMessage("CC-00299"), sheetName, "E", null);
                //    //Excel中【{0}】行【{1}】列的字符【{2}】的格式跟设定字符【{3}】的格式不符合！
                //InsertErrorMsg("CheckFieldFormat", Resource.GetOnlyMessage("CC-00247", dt.Rows[i]["LineNo"].ToString(), Utility.GetExcelColumnName(columnIndex), dt.Rows[i][columnName].ToString(), fieldFormat), "E", sheetName, "CheckFieldFormat");
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
    public static async Task CheckFieldLength(ISqlSugarClient Db, Guid importDataId, int columnIndex, int fieldLength, string sheetName)
    {
        try
        {
            string columnName = $"Col{columnIndex}";

            var selector = new List<SelectModel>() {
                new SelectModel(){  FieldName = columnName},
                new SelectModel(){ FieldName = "LineNo"} };

            var dtImpDataDetail = await Db.Queryable<SmImportDataDetail>().Select(selector)
                     .Where(x => x.ImportDataId == importDataId && x.SheetName == sheetName)
                     .ToListAsync();

            for (int i = 0; i < dtImpDataDetail.Count; i++)
            {
                var detail = dtImpDataDetail[i];
                var value = detail.GetPropertyValue(columnName);
                if (value.IsNotEmptyOrNull() && value.Length > fieldLength)
                {

                }
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
    public static async Task CheckFieldIsNull(ISqlSugarClient Db, Guid importDataId, int columnIndex, string sheetName, string userId)
    {
        try
        {
            string columnName = $"Col{columnIndex}";
            var selector = new List<SelectModel>() {
                new SelectModel(){  FieldName = columnName},
                new SelectModel(){ FieldName = "LineNo"} };

            var details = await Db.Queryable<SmImportDataDetail>().Select(selector)
                     .Where(x => x.ImportDataId == importDataId && x.SheetName == sheetName)
                     .ToListAsync();

            var nullDetails = new List<SmImportDataDetail>();
            details.ForEach(x =>
            {
                if (x.GetPropertyValue(columnName).IsNullOrEmpty())
                    nullDetails.Add(x);
            });

            var smImportDataDetailList = new List<SmImportDataDetail>();
            var importErrors = new List<SmImportError>();
            var importDataErrorCols = new List<SmImportDataErrorCol>();

            for (int i = 0; i < nullDetails.Count; i++)
            {
                SmImportDataDetail smImportDataDetail = new();
                smImportDataDetail.ID = nullDetails[i].ID;
                smImportDataDetail.IsError = true;
                smImportDataDetailList.Add(smImportDataDetail);

                var importDataErrorCol = new SmImportDataErrorCol();

                importDataErrorCol.ImportDataId = importDataId;
                importDataErrorCol.SheetName = sheetName;
                importDataErrorCol.LineNo = nullDetails[i].LineNo;
                importDataErrorCol.ColumnNo = columnIndex;
                importDataErrorCol.ErrorType = "E";
                importDataErrorCol.ErrorMessage = "";// Resource.GetOnlyMessage("CC-00301");
                importDataErrorCols.Add(importDataErrorCol);
                //UpdateImportDataErrorFlag(importDataId1, Convert.ToInt32(dt.Rows[i]["LineNo"]), columnIndex, Resource.GetOnlyMessage("CC-00301"), sheetName, "E", null);

                var importDataError = new SmImportError();
                importDataError.ImportDataId = importDataId;
                importDataError.SheetName = sheetName;
                importDataError.ErrorCode = "CheckFieldIsNull";
                string errorMsg = "Excel中【{0}】行【{1}】列的数据不允许为空！";
                errorMsg = string.Format(errorMsg, nullDetails[i].LineNo, GetExcelColumnName(columnIndex));
                importDataError.ErrorName = errorMsg;
                importDataError.ErrorType = "E";
                importDataError.ModuleCode = "";
                importErrors.Add(importDataError);
            }
            // 使用事务避免 MultipleActiveResultSets 问题
            if (smImportDataDetailList.Any() || importErrors.Any() || importDataErrorCols.Any())
            {
                await Db.Ado.BeginTranAsync();
                try
                {
                    if (smImportDataDetailList.Any())
                        await Db.Updateable(smImportDataDetailList)
                           .UpdateColumns(it => new { it.IsError }, true)
                           .ExecuteCommandAsync();
                    if (importErrors.Any())
                        await Db.Insertable(importErrors).ExecuteCommandAsync();
                    if (importDataErrorCols.Any())
                        await Db.Insertable(importDataErrorCols).ExecuteCommandAsync();
                    await Db.Ado.CommitTranAsync();
                }
                catch
                {
                    await Db.Ado.RollbackTranAsync();
                    throw;
                }
            }

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
    public static async Task CheckFieldUnique(ISqlSugarClient Db, Guid importDataId, int columnIndex, string tableCode, string columnCode, string companyId, string sheetName, string userId)
    {
        try
        {
            var importErrors = new List<SmImportError>();

            #region 验证当前导入的临时表中的数据是否重复

            string columnName = "Col" + columnIndex.ObjToString();
            var selector = new List<SelectModel>()
            {
                new SelectModel(){  FieldName = columnName,AsName="Col1"}
            };

            var detailGroup = await Db.Queryable<SmImportDataDetail>()
                .Select(selector)
                .GroupBy(x => x.Col1)
                .Where(x => x.ImportDataId == importDataId && x.SheetName == sheetName)
                .Select(it => new
                {
                    it.Col1,
                    Count = SqlFunc.AggregateCount(it.Col1),
                })
                .ToListAsync();
            var duplicateList = detailGroup.Where(x => x.Count > 1).ToList();

            for (int i = 0; i < duplicateList.Count; i++)
            {
                var lineNos = await Db.Queryable<SmImportDataDetail>()
                 .Where(x => x.ImportDataId == importDataId && x.SheetName == sheetName)
                 .Where($"{columnName}=@ColA", new { ColA = duplicateList[i].Col1 })
                 .Select(x => x.LineNo)
                 .ToListAsync();
                var itemNos = string.Join(",", lineNos.Select(x => x));

                var importError = new SmImportError();
                importError.ImportDataId = importDataId;
                importError.SheetName = sheetName;
                importError.ErrorCode = "CheckLovCode";
                string errorMsg = "Excel中【{0}】行【{1}】列的数据【{2}】重复！";
                errorMsg = string.Format(errorMsg, itemNos, GetExcelColumnName(columnIndex), duplicateList[i].Col1);
                importError.ErrorName = errorMsg;
                importError.ErrorType = "E";
                importErrors.Add(importError);
            }
            #endregion

            #region 验证临时表和正式表中的数据是否重复
            string sql = @" SELECT DISTINCT A.{0} AS {0}
                           FROM SmImportDataDetail A
                          WHERE A.ImportDataId = '{1}' AND A.SheetName='{4}'
                            AND EXISTS (SELECT 1 FROM {2} B
                                         WHERE B.{3} = A.{0}
                                           AND B.IsDeleted='false')";
            sql = string.Format(sql, columnName, importDataId, tableCode, columnCode, sheetName);
            var dt = await Db.Ado.GetDataTableAsync(sql);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var lineNos = await Db.Queryable<SmImportDataDetail>()
                 .Where(x => x.ImportDataId == importDataId && x.SheetName == sheetName)
                 .Where($"{columnName}=@ColA", new { ColA = dt.Rows[i][columnName].ToString() })
                 .Select(x => x.LineNo)
                 .ToListAsync();
                var itemNos = string.Join(",", lineNos.Select(x => x));

                var importError = new SmImportError();
                importError.ImportDataId = importDataId;
                importError.SheetName = sheetName;
                importError.ErrorCode = "CheckLovCode";
                string errorMsg = "Excel中【{0}】行【{1}】列的数据【{2}】和系统中已经存在的数据重复！";
                errorMsg = string.Format(errorMsg, itemNos, GetExcelColumnName(columnIndex), dt.Rows[i][0].ToString());
                importError.ErrorName = errorMsg;
                importError.ErrorType = "E";
                importErrors.Add(importError);
                //Excel中【{0}】行【{1}】列的数据【{2}】和已经存在的数据重复！
                //InsertErrorMsg("CheckFieldUnique", Resource.GetOnlyMessage("CC-00263", itemNos, Utility.GetExcelColumnName(columnIndex), dt.Rows[i][columnName].ToString()), "E", sheetName, "CheckFieldUnique");
            }
            if (importErrors.Any())
                await Db.Insertable(importErrors).ExecuteCommandAsync();
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
    //public static void CheckLovCode1(string importDataId, int columnIndex, string lovCode, string sheetName)
    //{
    //    try
    //    {
    //        string columnName = "COL" + columnIndex.ToString();
    //        DbSelect dsImpTempDetail = null;
    //        DataTable dtImpTempDetail = null;
    //        string sql = @" SELECT A.{0} AS LovValue
    //                              FROM SmImportDataDetail A
    //                             WHERE A.ImportDataId = '{1}' AND A.SheetName='{3}'
    //                               AND NOT EXISTS (SELECT 1 FROM SmLov_V B
    //                                            WHERE B.VALUE = A.{0}
    //                                              AND B.LovCode='{2}')";
    //        sql = string.Format(sql, columnName, importDataId, lovCode, sheetName);
    //        var dt = DBHelper.GetDataTable(sql, null);
    //        for (int i = 0; i < dt.Rows.Count; i++)
    //        {
    //            dsImpTempDetail = new("SmImportDataDetail A", "A", null);
    //            dsImpTempDetail.Select("A.[LineNo]");
    //            dsImpTempDetail.Where("A.ImportDataId", "=", importDataId);
    //            dsImpTempDetail.Where("A.SheetName", "=", sheetName);
    //            dsImpTempDetail.Where("A." + columnName, "=", dt.Rows[i]["LovValue"].ToString());
    //            dtImpTempDetail = DBHelper.GetDataTable(dsImpTempDetail.GetSql(), null);


    //            string itemNos = string.Empty;
    //            for (int j = 0; j < dtImpTempDetail.Rows.Count; j++)
    //            {
    //                //UpdateImportDataErrorFlag(importDataId, Convert.ToInt32(dtImpTempDetail.Rows[j]["LineNo"]), columnIndex, Resource.GetOnlyMessage("CC-00304", lovCode), sheetName, "E", null);
    //                //itemNos += dtImpTempDetail.Rows[j]["LineNo"].ToString() + ",";
    //            }
    //            if (!string.IsNullOrEmpty(itemNos))
    //            {
    //                itemNos = itemNos.Substring(0, itemNos.Length - 1);
    //            }
    //            //Excel中【{0}】行【{1}】列的参数值【{2}】在参数【{3}】中不存在！
    //            //InsertErrorMsg("CheckLovCode", Resource.GetOnlyMessage("CC-00217", itemNos, Utility.GetExcelColumnName(columnIndex), dt.Rows[i]["LovValue"].ToString(), lovCode), "E", "SM_LOV_MNG", sheetName, "CheckLovCode");
    //        }
    //    }
    //    catch (Exception) { throw; }
    //}

    /// <summary>
    /// 验证列中的值是否在LOV中存在
    /// </summary>
    /// <param name="columnIndex"></param>
    /// <param name="lovCode"></param>
    /// <param name="corresTableCode"></param>
    /// <param name="corresColumnCode"></param>
    public static async Task CheckLovCode(ISqlSugarClient Db, Guid importDataId, int columnIndex, string lovCode, string corresTableCode, string corresColumnCode, string companyId, string sheetName)
    {
        try
        {
            string columnName = $"Col{columnIndex}";
            string sql = @" SELECT DISTINCT A.{0} AS CORRES_VALUE
                                  FROM SmImportDataDetail A
                                 WHERE A.ImportDataId = '{1}' AND A.SheetName='{5}'
                                   AND (A.{0} IS NOT NULL AND A.{0}!='')
                                   AND NOT EXISTS (SELECT 1 FROM {2} B
                                                WHERE B.{3} = A.{0} AND B.LovCode='{4}')";
            sql = string.Format(sql, columnName, importDataId, corresTableCode, corresColumnCode, lovCode, sheetName);
            var dt = await Db.Ado.GetDataTableAsync(sql);

            string lovName = await Db.Queryable<SmLov>().Where(x => x.LovCode == lovCode).Select(x => x.LovName).FirstAsync();

            if (lovName.IsNullOrEmpty())
                lovName = lovCode;

            List<SmImportDataDetail> smImportDataDetailList = new();
            List<SmImportDataErrorCol> smImportDataErrorCols = new();
            List<SmImportError> smImportErrors = new();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var impTempDetails = await Db.Queryable<SmImportDataDetail>()
                    .Where(x => x.ImportDataId == importDataId && x.SheetName == sheetName)
                    .Where($"{columnName}=@ColA", new { ColA = dt.Rows[i]["CORRES_VALUE"].ToString() })
                    .Select(x => new { x.ID, x.LineNo })
                    .ToListAsync();

                for (int j = 0; j < impTempDetails.Count; j++)
                {
                    SmImportDataDetail smImportDataDetail = new();
                    smImportDataDetail.ID = impTempDetails[j].ID;
                    smImportDataDetail.IsError = true;
                    smImportDataDetailList.Add(smImportDataDetail);

                    SmImportDataErrorCol importDataErrorCol = new();
                    importDataErrorCol.ImportDataId = importDataId;
                    importDataErrorCol.SheetName = sheetName;
                    importDataErrorCol.LineNo = impTempDetails[j].LineNo;
                    importDataErrorCol.ColumnNo = columnIndex;
                    importDataErrorCol.ErrorType = "E";
                    importDataErrorCol.ErrorMessage = "";// Resource.GetOnlyMessage("CC-00304", lovName);
                    smImportDataErrorCols.Add(importDataErrorCol);
                    //UpdateImportDataErrorFlag(importDataId1, Convert.ToInt32(dtImpTempDetail.Rows[j]["LineNo"]), columnIndex, Resource.GetOnlyMessage("CC-00304", lovCode),sheetName,"E", null);
                }

                var itemNos = string.Join(",", impTempDetails.Select(x => x.LineNo));

                var importError = new SmImportError();
                importError.ImportDataId = importDataId;
                importError.SheetName = sheetName;
                importError.ErrorCode = "CheckLovCode";

                string errorMsg = "Excel中第【{0}】行【{1}】列的值【{2}】在参数设置中不存在！";
                errorMsg = string.Format(errorMsg, itemNos, GetExcelColumnName(columnIndex), dt.Rows[i]["CORRES_VALUE"].ToString());
                importError.ErrorName = errorMsg;// Resource.GetOnlyMessage("CC - 00217", itemNos, Utility.GetExcelColumnName(columnIndex), dt.Rows[i]["CORRES_VALUE"].ToString(), lovName);
                importError.ErrorType = "E";
                importError.ModuleCode = "";
                smImportErrors.Add(importError);

                //Excel中【{0}】行【{1}】列的参数值【{2}】在参数【{3}】中不存在！
                //InsertErrorMsg("CheckLovCode", Resource.GetOnlyMessage("CC-00217", itemNos, Utility.GetExcelColumnName(columnIndex), dt.Rows[i]["CORRES_VALUE"].ToString(), lovCode), "E", sheetName,"CheckLovCode");
            }

            // 使用事务避免 MultipleActiveResultSets 问题
            if (smImportDataDetailList.Any() || smImportDataErrorCols.Any() || smImportErrors.Any())
            {
                await Db.Ado.BeginTranAsync();
                try
                {
                    if (smImportDataDetailList.Any())
                        await Db.Updateable(smImportDataDetailList)
                            .UpdateColumns(it => new { it.IsError }, true)
                            .ExecuteCommandAsync();

                    if (smImportDataErrorCols.Any())
                        await Db.Insertable(smImportDataErrorCols).ExecuteCommandAsync();

                    if (smImportErrors.Any())
                        await Db.Insertable(smImportErrors).ExecuteCommandAsync();

                    await Db.Ado.CommitTranAsync();
                }
                catch
                {
                    await Db.Ado.RollbackTranAsync();
                    throw;
                }
            }
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
    public static async Task CheckCorresTable(ISqlSugarClient Db, Guid importDataId, int columnIndex, string corresTableCode, string corresColumnCode, string companyId, string sheetName, string userId, Guid? commonListSqlId)
    {
        try
        {
            string columnName = $"Col{columnIndex}";
            string sql = string.Empty;
            string corresTableCodeTemp = corresTableCode;
            string corresColumnCodeTemp = corresColumnCode;

            if (commonListSqlId.IsNotEmptyOrNull())
            {
                sql = LovHelper.GetCommonListSql(commonListSqlId);
                sql = @$"SELECT DISTINCT A.{columnName} AS CORRES_VALUE
                            FROM SmImportDataDetail A
                            WHERE     A.ImportDataId = '{importDataId}'
                                  AND A.SheetName = '{sheetName}'
                                  AND (A.{columnName} IS NOT NULL AND A.{columnName} ! = '')
                                  AND NOT EXISTS
                                         (SELECT 1
                                          FROM ({sql})
                                               B
                                          WHERE B.label = A.{columnName})";
            }
            else
            {
                if (corresTableCode == "HR_COMPANY")
                    sql = @" SELECT DISTINCT A.{0} AS CORRES_VALUE
                                  FROM SmImportDataDetail A
                                 WHERE A.ImportDataId = '{1}' AND A.SheetName='{4}'
                                   AND (A.{0} IS NOT NULL AND A.{0}!='')
                                   AND NOT EXISTS (SELECT 1 FROM {2} B
                                                WHERE B.{3} = A.{0} AND B.IsDeleted='false')";
                else
                    sql = @" SELECT DISTINCT A.{0} AS CORRES_VALUE
                                  FROM SmImportDataDetail A
                                 WHERE A.ImportDataId = '{1}' AND A.SheetName='{4}'
                                   AND (A.{0} IS NOT NULL AND A.{0}!='')
                                   AND NOT EXISTS (SELECT 1 FROM {2} B
                                                WHERE B.{3} = A.{0} AND B.IsDeleted='false')";
                sql = string.Format(sql, columnName, importDataId, corresTableCode, corresColumnCode, sheetName);
            }


            var dt = await Db.Ado.GetDataTableAsync(sql);

            var table = await Db.Queryable<SmTableCatalog>().Where(x => x.TableCode == corresTableCode).FirstAsync();

            var field = await Db.Queryable<SmFieldCatalog>().Where(x => x.TableCode == corresTableCode && x.ColumnCode == corresColumnCode).FirstAsync();

            corresTableCodeTemp = table.TableName;
            corresColumnCodeTemp = field.ColumnName;

            string errorMsg = "在【{0}】中的【{1}】不存在！";
            errorMsg = string.Format(errorMsg, corresTableCodeTemp, corresColumnCodeTemp);
            string errorMsg1 = string.Empty;

            List<SmImportDataDetail> smImportDataDetailList = new();
            List<SmImportDataErrorCol> smImportDataErrorCols = new();
            List<SmImportError> smImportErrors = new();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var impTempDetails = await Db.Queryable<SmImportDataDetail>()
                    .Where(x => x.ImportDataId == importDataId && x.SheetName == sheetName)
                    .Where($"{columnName}=@ColA", new { ColA = dt.Rows[i]["CORRES_VALUE"].ToString() })
                    .Select(x => new { x.ID, x.LineNo })
                    .ToListAsync();

                for (int j = 0; j < impTempDetails.Count; j++)
                {
                    var detail = impTempDetails[j];
                    SmImportDataDetail smImportDataDetail = new();
                    smImportDataDetail.ID = detail.ID;
                    smImportDataDetail.IsError = true;
                    smImportDataDetailList.Add(smImportDataDetail);

                    var importDataErrorCol = new SmImportDataErrorCol();
                    importDataErrorCol.ImportDataId = importDataId;
                    importDataErrorCol.SheetName = sheetName;
                    importDataErrorCol.LineNo = detail.LineNo;
                    importDataErrorCol.ColumnNo = columnIndex;
                    importDataErrorCol.ErrorType = "E";
                    importDataErrorCol.ErrorMessage = errorMsg;
                    smImportDataErrorCols.Add(importDataErrorCol);
                    //UpdateImportDataErrorFlag(importDataId1, Convert.ToInt32(dtImpTempDetail.Rows[j]["LineNo"]), columnIndex, Resource.GetOnlyMessage("CC-00305", corresTableCodeTemp, corresColumnCodeTemp), sheetName, "E", null);
                }
                var itemNos = string.Join(",", impTempDetails.Select(x => x.LineNo));

                #region Excel中【{0}】行【{1}】列的值【{2}】在表【{3}】的列【{4}】中不存在！
                errorMsg1 = "Excel中第【{0}】行【{1}】列的值【{2}】在【{3}】对应的【{4}】中不存在！";
                SmImportError importError = new();
                importError.ImportDataId = importDataId;
                importError.SheetName = sheetName;
                importError.ErrorCode = "CheckCorresTable";
                errorMsg1 = string.Format(errorMsg1, itemNos, GetExcelColumnName(columnIndex), dt.Rows[i]["CORRES_VALUE"].ToString(), corresTableCodeTemp, corresColumnCodeTemp);
                importError.ErrorName = errorMsg1;
                importError.ErrorType = "E";
                importError.ModuleCode = "";
                smImportErrors.Add(importError);
                #endregion
            }
            // 使用事务避免 MultipleActiveResultSets 问题
            if (smImportDataDetailList.Any() || smImportDataErrorCols.Any() || smImportErrors.Any())
            {
                await Db.Ado.BeginTranAsync();
                try
                {
                    if (smImportDataDetailList.Any())
                        await Db.Updateable(smImportDataDetailList)
                               .UpdateColumns(it => new { it.IsError }, true)
                               .ExecuteCommandAsync();
                    if (smImportDataErrorCols.Any())
                        await Db.Insertable(smImportDataErrorCols).ExecuteCommandAsync();
                    if (smImportErrors.Any())
                        await Db.Insertable(smImportErrors).ExecuteCommandAsync();
                    await Db.Ado.CommitTranAsync();
                }
                catch
                {
                    await Db.Ado.RollbackTranAsync();
                    throw;
                }
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
    /// <summary>
    /// 获取Excel列名（优化版本）
    /// 使用缓存和更高效的算法
    /// </summary>
    /// <param name="index">列索引（从1开始）</param>
    /// <returns>Excel列名（如A, B, ..., Z, AA, AB等）</returns>
    public static string GetExcelColumnName(int index)
    {
        if (index <= 0) throw new ArgumentOutOfRangeException(nameof(index), "列索引必须大于0");

        // 使用缓存提高性能
        if (_columnNameCache.TryGetValue(index, out string? cachedName))
            return cachedName;

        var result = new StringBuilder();
        int tempIndex = index;

        while (tempIndex > 0)
        {
            tempIndex--; // 转换为0基索引
            result.Insert(0, (char)('A' + tempIndex % 26));
            tempIndex /= 26;
        }

        var columnName = result.ToString();

        // 缓存结果（限制缓存大小避免内存泄漏）
        if (_columnNameCache.Count < 1000)
            _columnNameCache[index] = columnName;

        return columnName;
    }

    // 静态缓存字典
    private static readonly Dictionary<int, string> _columnNameCache = new();
    #endregion

    #region 直接转换进入正式表
    /// <summary>
    /// 直接转换进入正式表
    /// </summary>
    /// <param name="importDataId">导入数据ID</param>
    /// <param name="importTemplateCode">模板代码</param>
    /// <param name="userId">用户ID</param>
    /// <param name="isImportLineNo">是否导入序号</param>
    public static async Task TransferData(ISqlSugarClient Db, Guid importDataId, string importTemplateCode, string userId, bool isImportLineNo)
    {
        try
        {
            #region 获取导入模板信息
            var impTemplate = await Db.Queryable<SmImpTemplate>().Where(x => x.TemplateCode == importTemplateCode).FirstAsync();
            if (impTemplate == null)
                throw new Exception($"Excel导入模板代码【{importTemplateCode}】不存在！");
            #endregion

            #region 变量定义
            string tableCode = impTemplate.TableCode;
            string masterTableCode = impTemplate.MasterTableCode;
            string sql = string.Empty;
            string sheetName = impTemplate.SheetName;
            #endregion


            #region 获取模板详细配置
            var impTemplateDetails = await Db.Queryable<SmImpTemplateDetail>()
                .OrderBy(x => x.ColumnNo)
                .Where(x => x.ImpTemplateId == impTemplate.ID).ToListAsync();
            var masterDetails = impTemplateDetails
                .Where(x => x.DataType == ImportDataType.Master.ObjToString())
                .ToList();
            var templateDetails = impTemplateDetails
                .Where(x => x.DataType != ImportDataType.Master.ObjToString()).ToList();
            #endregion

            #region 复制数据到临时表

            // 清理旧的临时数据
            await Db.Deleteable<SmImportDataDetailTemp>().Where(x => x.ImportDataId == importDataId && x.SheetName == sheetName).ExecuteCommandAsync();

            // 复制有效数据到临时表

            // 1. 查询源表数据（映射到实体）
            var sourceData = await Db.Queryable<SmImportDataDetail>()
                .Where(x => x.ImportDataId == importDataId && x.SheetName == sheetName && (x.IsError == false || x.IsError == null))
                .ToListAsync();

            // 2. 批量插入到目标表（目标表对应相同实体或不同实体）
            await Db.Insertable(sourceData)
                .AS("SmImportDataDetailTemp") // 指定目标表名
                .ExecuteCommandAsync();
            #endregion

            #region 更新另一个表 
            for (int j = 0; j < impTemplateDetails.Count; j++)
            {
                var fieldName = impTemplateDetails[j].ColumnCode;
                var isUnique = impTemplateDetails[j].IsUnique;
                var lovCode = impTemplateDetails[j].LovCode;
                var corresTableCode = impTemplateDetails[j].CorresTableCode;
                var corresColumnCode = impTemplateDetails[j].CorresColumnCode;
                var transColumnCode = impTemplateDetails[j].TransColumnCode;
                var isEncrypt = impTemplateDetails[j].IsEncrypt;
                var commonListSqlId = impTemplateDetails[j].CommonListSqlId;

                if (!string.IsNullOrEmpty(fieldName))
                {
                    var columnIndex = impTemplateDetails[j].ColumnNo;
                    var columnName = "COL" + columnIndex.ToString();

                    #region 处理映射表的情况
                    if (!string.IsNullOrEmpty(lovCode))
                    {
                        sql = @"UPDATE A SET A.{0}=B.{1}
                                      FROM SmImportDataDetailTemp A,{2} B
                                     WHERE B.{3}=A.{0} AND B.LovCode='{4}' AND A.ImportDataId='{5}' AND A.SheetName='{6}' AND B.IsDeleted='false'";
                        sql = string.Format(sql, columnName, transColumnCode, "SmLov_V", "", lovCode, importDataId, sheetName);
                        sql = @$"UPDATE A
                                    SET A.{columnName} = B.[Value]
                                    FROM SmImportDataDetailTemp A, SmLov_V B
                                    WHERE     B.[Text] = A.{columnName}
                                          AND B.LovCode = '{lovCode}'
                                          AND A.ImportDataId = '{importDataId}'
                                          AND A.SheetName = '{sheetName}' ";
                        await Db.Ado.ExecuteCommandAsync(sql);
                    }
                    else if (commonListSqlId.IsNotEmptyOrNull())
                    {
                        string commonListSql = LovHelper.GetCommonListSql(commonListSqlId);

                        sql = @"UPDATE A SET A.{0}=B.{1}
                                          FROM SmImportDataDetailTemp A,({2}) B
                                         WHERE B.{3}=A.{0} AND A.ImportDataId='{4}' AND A.SheetName='{5}'";
                        sql = string.Format(sql, columnName, "value", commonListSql, "label", importDataId, sheetName);
                        await Db.Ado.ExecuteCommandAsync(sql);
                    }
                    else if (!string.IsNullOrEmpty(corresTableCode) && !string.IsNullOrEmpty(corresColumnCode) && !string.IsNullOrEmpty(transColumnCode))
                    {
                        sql = @"UPDATE A SET A.{0}=B.{1}
                                          FROM SmImportDataDetailTemp A,{2} B
                                         WHERE B.{3}=A.{0} AND A.ImportDataId='{4}' AND A.SheetName='{5}' AND B.IsDeleted='false'";
                        sql = string.Format(sql, columnName, transColumnCode, corresTableCode, corresColumnCode, importDataId, sheetName);
                        await Db.Ado.ExecuteCommandAsync(sql);
                    }
                    #endregion
                }
            }
            #endregion

            #region 处理主表数据
            var dt = new Dictionary<string, object>
            {
                { "ImportDataId", importDataId },
                { "IsDeleted", true },
                { "UpdateTime", DateTime.Now },
                { "UpdateBy", Utility.GetUserId() }
            };
            if (masterTableCode.IsNotEmptyOrNull())
            {
                await Db.Updateable(dt).AS(masterTableCode)
                    .WhereColumns("ImportDataId")
                    .ExecuteCommandAsync();

                var selector1 = masterDetails.Select(x => new SelectModel()
                {
                    FieldName = $"Col{x.ColumnNo}"
                }).ToList();
                selector1.Add(new SelectModel() { FieldName = "LineNo" });

                var masterDict = new Dictionary<string, object>
                {
                    { "ID", Utility.GuidId },
                    { "CreatedBy", Utility.GetUserId() },
                    { "CreatedTime", DateTimeHelper.GetSysDateTimeString() },
                    { "TAG", "0" },
                    { "ImportDataId", importDataId },
                    { "GroupId", Utility.GetGroupId() },
                    { "AuditStatus", "Add" },
                    { "CompanyId", Utility.GetCompanyId() }
                };

                var dtImpDataMaster = await Db.Queryable<SmImportDataDetailTemp>()
                    .Select(selector1)
                    .Where(x => x.ImportDataId == importDataId && x.SheetName == sheetName && x.DataType == ImportDataType.Master.ObjToString())
                    .ToListAsync();
                masterDetails.ForEach(x =>
                {
                    dtImpDataMaster.Where(dataDetail => dataDetail.LineNo == x.RowNo).ToList()
                    .ForEach(dataDetail =>
                    {
                        var value = dataDetail.GetPropertyValue($"Col{x.ColumnNo}");
                        if (x.IsInsert == true && value.IsNotEmptyOrNull())
                            masterDict.Add(x.ColumnCode, value);
                    });
                });

                await Db.Insertable(masterDict).AS(masterTableCode).ExecuteCommandAsync();
            }
            #endregion

            #region 处理明细数据
            await Db.Updateable(dt).AS(tableCode)
                .WhereColumns("ImportDataId")
                .ExecuteCommandAsync();

            var selector = templateDetails.Select(x => new SelectModel()
            {
                FieldName = $"Col{x.ColumnNo}"
            }).ToList();

            var dicts = new List<Dictionary<string, object>>();

            var dtImpDataDetail = await Db.Queryable<SmImportDataDetailTemp>()
                .Select(selector)
                .Where(x => x.ImportDataId == importDataId && x.SheetName == sheetName && x.DataType == ImportDataType.Detail.ObjToString())
                .ToListAsync();
            dtImpDataDetail.ForEach(dataDetail =>
            {
                var dc = new Dictionary<string, object>
                {
                    { "ID", Utility.GuidId },
                    { "CreatedBy", Utility.GetUserId() },
                    { "CreatedTime", DateTimeHelper.GetSysDateTimeString() },
                    { "TAG", "0" },
                    { "ImportDataId", importDataId },
                    { "GroupId", Utility.GetGroupId() },
                    { "CompanyId", Utility.GetCompanyId() }
                };

                templateDetails.ForEach(x =>
                {
                    if (x.IsInsert == true)
                        dc.Add(x.ColumnCode, dataDetail.GetPropertyValue($"Col{x.ColumnNo}"));

                });
                dicts.Add(dc);
            });
            await Db.Insertable(dicts).AS(tableCode).ExecuteCommandAsync();
            #endregion

            #region 清理临时数据
            await Db.Deleteable<SmImportDataDetailTemp>().Where(x => x.ImportDataId == importDataId && x.SheetName == sheetName).ExecuteCommandAsync();
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
    public static async Task<List<SmImportError>> GetImportErrorList(ISqlSugarClient Db, Guid importDataId)
    {
        return await Db.Queryable<SmImportError>().Where(x => x.ImportDataId == importDataId).ToListAsync();
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
    public static async Task<DataTable> GetImportDataDetailList(ISqlSugarClient Db, Guid importDataId, Guid importTemplateId)
    {
        try
        {
            var impTemplateDetails = await Db.Queryable<SmImpTemplateDetail>()
                .OrderBy(x => x.ColumnNo)
                .Where(x => x.ImpTemplateId == importTemplateId && (x.DataType == ImportDataType.Detail.ObjToString() || x.DataType == null))
                .ToListAsync();

            string tempColumnCode = string.Empty;
            DbSelect ds = new("SmImportDataDetail A", "A", null);
            ds.Select("TOP 10 A.[LineNo]", "行号");
            if (impTemplateDetails.Count > 0)
            {
                for (int i = 1; i < impTemplateDetails.Count + 1; i++)
                {
                    if (impTemplateDetails[i - 1].ColumnName.IsNotEmptyOrNull())
                        ds.Select("A.Col" + impTemplateDetails[i - 1].ColumnNo, impTemplateDetails[i - 1].ColumnName);
                    else
                    {
                        tempColumnCode = await GetImportTemplateDetailColumnCode(Db, importTemplateId, impTemplateDetails[i - 1].ColumnNo);
                        if (tempColumnCode.IsNotEmptyOrNull())
                            ds.Select("A.Col" + impTemplateDetails[i - 1].ColumnNo, tempColumnCode);
                        else
                            ds.Select("A.Col" + impTemplateDetails[i - 1].ColumnNo);
                    }
                }
            }
            ds.Where("A.ImportDataId", "=", importDataId);
            ds.Where("A.DataType", "!=", "Master");
            string sql = ds.GetSql();
            //List<SmImportDataDetail> list = DBHelper.QueryList<SmImportDataDetail>(sql);
            return await Db.Ado.GetDataTableAsync(ds.GetSql());
        }
        catch (Exception)
        {
            throw;
        }
    }


    /// <summary>
    /// 获取导入明细
    /// </summary>
    /// <param name="importDataId">导入数据ID</param>
    /// <param name="importTemplateId">导入模板ID</param>
    /// <returns></returns>
    public static async Task<DataTable> GetImportDataMasterList(ISqlSugarClient Db, Guid importDataId, Guid importTemplateId)
    {
        try
        {
            DataTable dt = new DataTable();
            // 2. 添加列（指定列名和数据类型）
            dt.Columns.Add("key", typeof(int));
            dt.Columns.Add("Code", typeof(string));
            dt.Columns.Add("label", typeof(string));
            dt.Columns.Add("children", typeof(string)); // 或 object，如果你要存多种类型
            var impTemplateDetails = await Db.Queryable<SmImpTemplateDetail>()
                .OrderBy(x => x.SerialNumber)
                .Where(x => x.ImpTemplateId == importTemplateId && x.DataType == ImportDataType.Master.ObjToString())
                .ToListAsync();

            var importDataDetails = await Db.Queryable<SmImportDataDetail>()
                .Where(x =>
                x.ImportDataId == importDataId &&
                x.DataType == ImportDataType.Master.ObjToString())
                .ToListAsync();
            for (int i = 0; i < impTemplateDetails.Count; i++)
            {
                string value = string.Empty;
                for (int j = 0; j < importDataDetails.Count; j++)
                {
                    if (impTemplateDetails[i].RowNo == importDataDetails[j].LineNo)
                    {
                        var value1 = importDataDetails[j].GetPropertyValue($"Col{impTemplateDetails[i].ColumnNo}");
                        if (value1.IsNotEmptyOrNull())
                            value = value1.Trim();
                    }
                }
                dt.Rows.Add(i, impTemplateDetails[i].ColumnCode, impTemplateDetails[i].ColumnName, value);

            }
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
    public static async Task<string> GetImportTemplateDetailColumnCode(ISqlSugarClient Db, Guid importTemplateId, int? columnNo)
    {
        return await Db.Queryable<SmImpTemplateDetail>().Where(x => x.ImpTemplateId == importTemplateId && x.ColumnNo == columnNo).Select(x => x.ColumnCode).FirstAsync();
    }
    #endregion

    #region 数据转换后执行
    /// <summary>
    /// 数据转换后执行
    /// </summary>
    /// <param name="templateCode">模板代码</param>
    /// <param name="importDataId">导入数据ID</param>
    /// <param name="masterId">masterId</param>
    public static async Task AfterImport(ISqlSugarClient Db, string templateCode, Guid importDataId, string masterId)
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
                        await Db.Ado.ExecuteCommandAsync(sql);

                        break;
                    }
                #endregion

                #region 销售订单导入
                case "IMPORT_SALE_ORDER_MNG":
                    {
                        var OrderNo = Utility.GenerateContinuousSequence("SdOrderNo");
                        var SalesOrderStatus = DIC_SALES_ORDER_STATUS.WaitShip;

                        var dt = new Dictionary<string, object>
                        {
                            { "ImportDataId", importDataId },
                            { "OrderNo", OrderNo },
                            { "SalesOrderStatus", SalesOrderStatus },
                            { "UpdateTime", DateTime.Now },
                            { "UpdateBy", Utility.GetUserId() }
                        };
                        await Db.Updateable<SdOrder>()
                             .SetColumns(it => it.OrderNo == OrderNo)
                             .SetColumns(it => it.SalesOrderStatus == SalesOrderStatus)
                             .Where(it => it.ImportDataId == importDataId)
                             .ExecuteCommandAsync();

                        var order = await Db.Queryable<SdOrder>().Where(x => x.ImportDataId == importDataId).FirstAsync();

                        var details = await Db.Queryable<SdOrderDetail>().Where(x => x.ImportDataId == importDataId).ToArrayAsync();
                        if (details.Any())
                        {
                            for (int i = 0; i < details.Length; i++)
                            {
                                (decimal? NoTaxAmount, decimal? TaxAmount, decimal? TaxIncludedAmount) = IVChangeHelper.UpdataTaxAmount(order.TaxType, order.TaxRate, details[i].Price, details[i].QTY);
                                details[i].NoTaxAmount = NoTaxAmount;
                                details[i].TaxAmount = TaxAmount;
                                details[i].TaxIncludedAmount = TaxIncludedAmount;
                                details[i].OrderId = order.ID;
                            }
                            await Db.Updateable(details)
                                .UpdateColumns(it => new
                                {
                                    it.NoTaxAmount,
                                    it.TaxAmount,
                                    it.TaxIncludedAmount,
                                    it.OrderId
                                })
                                .ExecuteCommandAsync();

                            await IVChangeHelper.UpdataOrderDetailSerialNumber(Db, "SdOrderDetail", order.ID);

                        }
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
