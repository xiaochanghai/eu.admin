using EU.Core.Common.Enums;
using EU.Core.Common.Extensions;
using EU.Core.Model;
using EU.Core.Model.Entity;
using EU.Core.Model.ViewModels.Extend;
using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using SqlSugar;
using System.Collections;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace EU.Core.Common.Helper;

public class NPOIHelper
{
    #region DataTable 导出到 Excel 的 MemoryStream
    /// <summary>
    /// DataTable 导出到 Excel 的 MemoryStream
    /// </summary>
    /// <param name="dtSource">源 DataTable</param>
    /// <param name="strHeaderText">表头文本 空值未不要表头标题</param>
    /// <returns></returns>
    public static MemoryStream ExportExcel(DataTable dtSource, string strHeaderText)
    {
        //HSSFWorkbook workbook = new HSSFWorkbook();
        XSSFWorkbook workbook = new XSSFWorkbook();
        ISheet sheet = workbook.CreateSheet();

        #region 文件属性
        DocumentSummaryInformation dsi = new();
        dsi.Company = "EUCloud";

        //workbook.DocumentSummaryInformation = dsi;
        SummaryInformation si = new();
        si.Author = "EUCloud";
        si.ApplicationName = "EUCloud";
        si.LastAuthor = "EUCloud";
        si.Comments = "";
        si.Title = "";
        si.Subject = "";

        si.CreateDateTime = DateTime.Now;
        //workbook.SummaryInformation = si;
        #endregion

        ICellStyle dateStyle = workbook.CreateCellStyle();
        IDataFormat format = workbook.CreateDataFormat();
        dateStyle.DataFormat = format.GetFormat("yyyy-mm-dd");
        ICellStyle datetimeStyle = workbook.CreateCellStyle();
        datetimeStyle.DataFormat = format.GetFormat("yyyy-mm-dd hh:mm");
        ICellStyle datetimesStyle = workbook.CreateCellStyle();
        datetimesStyle.DataFormat = format.GetFormat("yyyy-mm-dd hh:mm:ss");
        int[] arrColWidth = new int[dtSource.Columns.Count];
        foreach (DataColumn item in dtSource.Columns)
        {
            arrColWidth[item.Ordinal] = Encoding.GetEncoding("utf-8").GetBytes(item.ColumnName.ToString()).Length;
        }
        for (int i = 0; i < dtSource.Rows.Count; i++)
        {
            for (int j = 0; j < dtSource.Columns.Count; j++)
            {
                int intTemp = Encoding.GetEncoding("utf-8").GetBytes(dtSource.Rows[i][j].ToString()).Length;
                if (intTemp > arrColWidth[j])
                {
                    arrColWidth[j] = intTemp;
                }
            }
        }
        int rowIndex = 0;
        int intTop = 0;
        foreach (DataRow row in dtSource.Rows)
        {
            #region 新建表、填充表头、填充列头，样式
            if (rowIndex == 655350 || rowIndex == 0)
            {
                if (rowIndex != 0)
                {
                    sheet = workbook.CreateSheet();
                }
                intTop = 0;
                #region 表头及样式
                {
                    if (strHeaderText.Length > 0)
                    {
                        IRow headerRow = sheet.CreateRow(intTop);
                        intTop += 1;
                        headerRow.HeightInPoints = 25;
                        headerRow.CreateCell(0).SetCellValue(strHeaderText);
                        ICellStyle headStyle = workbook.CreateCellStyle();
                        headStyle.Alignment = HorizontalAlignment.Center;
                        IFont font = workbook.CreateFont();
                        font.FontHeightInPoints = 20;
                        font.IsBold = true;
                        headStyle.SetFont(font);
                        headerRow.GetCell(0).CellStyle = headStyle;
                        sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, dtSource.Columns.Count - 1));

                    }
                }
                #endregion
                #region  列头及样式
                {
                    IRow headerRow = sheet.CreateRow(intTop);
                    intTop += 1;
                    ICellStyle headStyle = workbook.CreateCellStyle();
                    headStyle.Alignment = HorizontalAlignment.Center;
                    headStyle.BorderBottom = BorderStyle.Medium;
                    headStyle.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
                    headStyle.FillPattern = FillPattern.NoFill;
                    IFont font = workbook.CreateFont();
                    font.IsBold = true;
                    headStyle.SetFont(font);
                    foreach (DataColumn column in dtSource.Columns)
                    {
                        headerRow.CreateCell(column.Ordinal).SetCellValue(column.ColumnName);
                        headerRow.GetCell(column.Ordinal).CellStyle = headStyle;
                        //设置列宽
                        //sheet.SetColumnWidth(column.Ordinal, (arrColWidth[column.Ordinal] + 1) * 256);
                        if (arrColWidth[column.Ordinal] > 255)
                        {
                            arrColWidth[column.Ordinal] = 254;
                        }
                        else
                        {
                            sheet.SetColumnWidth(column.Ordinal, (arrColWidth[column.Ordinal] + 1) * 256);
                        }
                    }


                }
                #endregion
                rowIndex = intTop;
            }
            #endregion
            #region 填充内容
            IRow dataRow = sheet.CreateRow(rowIndex);
            foreach (DataColumn column in dtSource.Columns)
            {
                ICell newCell = dataRow.CreateCell(column.Ordinal);
                string drValue = row[column].ToString();
                switch (column.DataType.ToString())
                {
                    case "System.String"://字符串类型
                        newCell.SetCellValue(drValue);
                        break;
                    case "System.DateTime"://日期类型
                        DateTime dateV;
                        if (!string.IsNullOrEmpty(drValue))
                        {
                            DateTime.TryParse(drValue, out dateV);
                            //dateV = DateTimeHelper.ConvertToSecondString(dateV);
                            newCell.SetCellValue(dateV);
                            if (column.Caption == "renderDateTime")
                            {
                                newCell.CellStyle = datetimeStyle;//格式化显示到分钟
                            }
                            else if (column.Caption == "renderDate")
                            {
                                newCell.CellStyle = dateStyle;//格式化显示到天
                            }
                            else
                            {
                                newCell.CellStyle = datetimesStyle;//格式化显示到秒
                            }
                        }
                        break;
                    case "System.Boolean"://布尔型
                        bool boolV = false;
                        bool.TryParse(drValue, out boolV);
                        newCell.SetCellValue(boolV);
                        break;
                    case "System.Int16":
                    case "System.Int32":
                    case "System.Int64":
                    case "System.Byte":
                        int intV = 0;
                        int.TryParse(drValue, out intV);
                        newCell.SetCellValue(intV);
                        break;
                    case "System.Decimal":
                    case "System.Double":
                        double doubV = 0;
                        double.TryParse(drValue, out doubV);
                        newCell.SetCellValue(doubV);
                        break;
                    case "System.DBNull"://空值处理
                        newCell.SetCellValue("");
                        break;
                    default:
                        newCell.SetCellValue("");
                        break;
                }
            }
            #endregion
            rowIndex++;
        }
        using (MemoryStream ms = new MemoryStream())
        {
            workbook.Write(ms);
            ms.Flush();
            //ms.Position = 0;
            return ms;
        }
    }
    #endregion

    #region DaataTable 导出到 Excel 文件
    /// <summary>
    /// DaataTable 导出到 Excel 文件
    /// </summary>
    /// <param name="dtSource">源 DataaTable</param>
    /// <param name="strHeaderText">表头文本</param>
    /// <param name="strFileName">保存位置(文件名及路径)</param>
    public static void ExportExcel(DataTable dtSource, string strHeaderText, string strFileName)
    {
        using (MemoryStream ms = ExportExcel(dtSource, strHeaderText))
        {
            using (FileStream fs = new FileStream(strFileName, FileMode.Create, FileAccess.Write))
            {
                byte[] data = ms.ToArray();
                fs.Write(data, 0, data.Length);
                fs.Flush();
            }
        }
    }
    #endregion

    #region 读取 excel,默认第一行为标头
    /// <summary>
    /// 读取 excel,默认第一行为标头
    /// </summary>
    /// <param name="strFileName">excel 文档路径</param>
    /// <returns></returns>
    public static DataTable ImportExcel(string strFileName, string sheetName = "", int startRow = 0)
    {
        DataTable dt = new DataTable();
        //HSSFWorkbook hssfworkbook;
        IWorkbook hssfworkbook;
        ISheet sheet;
        using (FileStream file = new FileStream(FileHelper.GetPhysicsPath() + strFileName, FileMode.Open, FileAccess.Read))
        {
            //hssfworkbook = new HSSFWorkbook(file);
            //hssfworkbook = new XSSFWorkbook(file);
            hssfworkbook = WorkbookFactory.Create(file);
        }
        if (hssfworkbook == null) throw new Exception("未能加载excel");
        int sheetCount = hssfworkbook.NumberOfSheets;
        if (sheetCount == 0) throw new Exception("未能加载excel");
        if (string.IsNullOrEmpty(sheetName))
        {
            sheet = hssfworkbook.GetSheetAt(0);
        }
        else
        {
            int sheetIndex = hssfworkbook.GetSheetIndex(sheetName);
            if (sheetIndex >= 0)
            {
                sheet = hssfworkbook.GetSheetAt(sheetIndex);
            }
            else
            {
                throw new Exception($"未能找到{sheetName}这个sheet页");
            }
        }
        IEnumerator rows = sheet.GetRowEnumerator();
        IRow headerRow = sheet.GetRow(startRow);
        int cellCount = headerRow.LastCellNum;
        for (int j = 0; j < cellCount; j++)
        {
            ICell cell = headerRow.GetCell(j);
            string column = cell.ObjToString();
            if (column.IsNotEmptyOrNull())
                dt.Columns.Add(cell.ObjToString());
        }
        for (int i = (startRow + 1); i <= sheet.LastRowNum; i++)
        {
            IRow row = sheet.GetRow(i);
            if (row is null) continue;
            if (row.GetCell(row.FirstCellNum) != null && row.GetCell(row.FirstCellNum).ToString().Length > 0)
            //if (row.GetCell(row.FirstCellNum) != null)
            {
                DataRow dataRow = dt.NewRow();
                for (int j = row.FirstCellNum; j < cellCount; j++)
                {
                    if (row.GetCell(j) != null)
                    {
                        DateTime dateV = DateTime.MinValue;
                        try
                        {
                            dataRow[j] = GetCellValue(row.GetCell(j));
                            //if (row.GetCell(j).IsDate())
                            //{
                            //    dateV = row.GetCell(j).DateCellValue;
                            //    dataRow[j] = DateTimeHelper.ConvertToSecondString(dateV);
                            //}
                            //else
                            //{
                            //    dataRow[j] = row.GetCell(j).ToString();
                            //}
                        }
                        catch { }
                        //if (dateV == DateTime.MinValue)
                        //{
                        //    dataRow[j] = row.GetCell(j).ToString();
                        //}
                        //else
                        //{
                        //    dataRow[j] = DateTimeHelper.ConvertToSecondString(dateV);
                        //}

                    }
                }
                dt.Rows.Add(dataRow);
            }
        }
        return dt;

    }
    #endregion

    #region 读取 excel,默认第一行为标头
    /// <summary>
    /// 读取 excel,默认第一行为标头
    /// </summary>
    /// <param name="strFileName">excel 文档路径</param>
    /// <returns></returns>
    public static TemplateInfo GetTemplateInfo(string strFileName)
    {
        TemplateInfo info = new();
        try
        {
            string sheetName = "模板信息";
            IWorkbook hssfworkbook;
            ISheet sheet;
            using (FileStream file = new FileStream(FileHelper.GetPhysicsPath() + strFileName, FileMode.Open, FileAccess.Read))
            {
                hssfworkbook = WorkbookFactory.Create(file);
            }
            if (hssfworkbook == null) throw new Exception("未能加载excel");

            int sheetCount = hssfworkbook.NumberOfSheets;
            if (sheetCount == 0) throw new Exception("未能加载excel");

            if (string.IsNullOrEmpty(sheetName))
                sheet = hssfworkbook.GetSheetAt(0);
            else
            {
                int sheetIndex = hssfworkbook.GetSheetIndex(sheetName);
                if (sheetIndex >= 0)
                    sheet = hssfworkbook.GetSheetAt(sheetIndex);
                else
                    throw new Exception($"未能找到{sheetName}这个sheet页");
            }

            IEnumerator rows = sheet.GetRowEnumerator();
            IRow headerRow = sheet.GetRow(0);
            ICell cell = headerRow.GetCell(0);
            info.TemplateId = cell.ObjToGuid();
        }
        catch (Exception E)
        {
            info.Message = E.Message;
        }
        return info;
    }
    #endregion

    /// <summary>
    /// 获取单元格类型
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    public static string GetCellValue(ICell cell)
    {
        if (cell == null)
            return null;
        switch (cell.CellType)
        {
            case CellType.Blank: //BLANK:  
                return null;
            case CellType.Boolean: //BOOLEAN:  
                return Convert.ToString(cell.BooleanCellValue);
            case CellType.Numeric: //NUMERIC:  
                if (DateUtil.IsCellDateFormatted(cell))
                {
                    return cell.DateCellValue.ConvertToSecondString();
                }
                else
                {
                    return Convert.ToString(cell);
                }
            case CellType.String: //STRING:  
                return cell.StringCellValue;
            case CellType.Error: //ERROR:  
                return Convert.ToString(cell.ErrorCellValue);
            case CellType.Formula: //FORMULA:  
            default:
                return "=" + cell.CellFormula;
        }
    }

    /// <summary>
    /// DataSet 导出到 Excel 的 MemoryStream
    /// </summary>
    /// <param name="dsSource">源 DataSet</param>
    /// <param name="strHeaderText">表头文本 空值未不要表头标题(多个表对应多个表头以英文逗号(,)分开，个数应与表相同)</param>
    /// <returns></returns>
    public static MemoryStream ExportExcel(DataSet dsSource, string strHeaderText)
    {

        HSSFWorkbook workbook = new HSSFWorkbook();

        #region 文件属性
        DocumentSummaryInformation dsi = new();
        dsi.Company = "517best.com";
        workbook.DocumentSummaryInformation = dsi;
        SummaryInformation si = new();
        si.Author = "517best.com";
        si.ApplicationName = "517best.com";
        si.LastAuthor = "517best.com";
        si.Comments = "";
        si.Title = "";
        si.Subject = "";
        si.CreateDateTime = DateTime.Now;
        workbook.SummaryInformation = si;
        #endregion

        #region 注释


        //ICellStyle dateStyle = workbook.CreateCellStyle();
        //IDataFormat format = workbook.CreateDataFormat();
        //dateStyle.DataFormat = format.GetFormat("yyyy-mm-dd");

        //ISheet sheet = workbook.CreateSheet();
        //int[] arrColWidth = new int[dtSource.Columns.Count];
        //foreach (DataColumn item in dtSource.Columns)
        //{
        //    arrColWidth[item.Ordinal] = Encoding.GetEncoding("gb2312").GetBytes(item.ColumnName.ToString()).Length;
        //}
        //for (int i = 0; i < dtSource.Rows.Count; i++)
        //{
        //    for (int j = 0; j < dtSource.Columns.Count; j++)
        //    {
        //        int intTemp = Encoding.GetEncoding("gb2312").GetBytes(dtSource.Rows[i][j].ToString()).Length;
        //        if (intTemp > arrColWidth[j])
        //        {
        //            arrColWidth[j] = intTemp;
        //        }
        //    }
        //}
        //int rowIndex = 0;
        //int intTop = 0;
        //foreach (DataRow row in dtSource.Rows)
        //{
        //    #region 新建表、填充表头、填充列头，样式
        //    if (rowIndex == 65535 || rowIndex == 0)
        //    {
        //        if (rowIndex != 0)
        //        {
        //            sheet = workbook.CreateSheet();
        //        }
        //        intTop = 0;
        //        #region 表头及样式
        //        {
        //            if (strHeaderText.Length > 0)
        //            {
        //                IRow headerRow = sheet.CreateRow(intTop);
        //                intTop += 1;
        //                headerRow.HeightInPoints = 25;
        //                headerRow.CreateCell(0).SetCellValue(strHeaderText);
        //                ICellStyle headStyle = workbook.CreateCellStyle();
        //                headStyle.Alignment = HorizontalAlignment.CENTER;
        //                IFont font = workbook.CreateFont();
        //                font.FontHeightInPoints = 20;
        //                font.Boldweight = 700;
        //                headStyle.SetFont(font);
        //                headerRow.GetCell(0).CellStyle = headStyle;
        //                sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(0, 0, 0, dtSource.Columns.Count - 1));

        //            }
        //        }
        //        #endregion
        //        #region  列头及样式
        //        {
        //            IRow headerRow = sheet.CreateRow(intTop);
        //            intTop += 1;
        //            ICellStyle headStyle = workbook.CreateCellStyle();
        //            headStyle.Alignment = HorizontalAlignment.CENTER;
        //            IFont font = workbook.CreateFont();
        //            font.Boldweight = 700;
        //            headStyle.SetFont(font);
        //            foreach (DataColumn column in dtSource.Columns)
        //            {
        //                headerRow.CreateCell(column.Ordinal).SetCellValue(column.ColumnName);
        //                headerRow.GetCell(column.Ordinal).CellStyle = headStyle;
        //                //设置列宽
        //                sheet.SetColumnWidth(column.Ordinal, (arrColWidth[column.Ordinal] + 1) * 256);
        //            }


        //        }
        //        #endregion
        //        rowIndex = intTop;
        //    }
        //    #endregion
        //    #region 填充内容
        //    IRow dataRow = sheet.CreateRow(rowIndex);
        //    foreach (DataColumn column in dtSource.Columns)
        //    {
        //        ICell newCell = dataRow.CreateCell(column.Ordinal);
        //        string drValue = row[column].ToString();
        //        switch (column.DataType.ToString())
        //        {
        //            case "System.String"://字符串类型
        //                newCell.SetCellValue(drValue);
        //                break;
        //            case "System.DateTime"://日期类型
        //                DateTime dateV;
        //                DateTime.TryParse(drValue, out dateV);
        //                newCell.SetCellValue(dateV);
        //                newCell.CellStyle = dateStyle;//格式化显示
        //                break;
        //            case "System.Boolean"://布尔型
        //                bool boolV = false;
        //                bool.TryParse(drValue, out boolV);
        //                newCell.SetCellValue(boolV);
        //                break;
        //            case "System.Int16":
        //            case "System.Int32":
        //            case "System.Int64":
        //            case "System.Byte":
        //                int intV = 0;
        //                int.TryParse(drValue, out intV);
        //                newCell.SetCellValue(intV);
        //                break;
        //            case "System.Decimal":
        //            case "System.Double":
        //                double doubV = 0;
        //                double.TryParse(drValue, out doubV);
        //                newCell.SetCellValue(doubV);
        //                break;
        //            case "System.DBNull"://空值处理
        //                newCell.SetCellValue("");
        //                break;
        //            default:
        //                newCell.SetCellValue("");
        //                break;
        //        }
        //    }
        //    #endregion
        //    rowIndex++;
        //}
        #endregion

        string[] strNewText = strHeaderText.Split(Convert.ToChar(","));
        if (dsSource.Tables.Count == strNewText.Length)
        {
            for (int i = 0; i < dsSource.Tables.Count; i++)
            {
                ExportFromDSExcel(workbook, dsSource.Tables[i], strNewText[i]);
            }
        }

        using (MemoryStream ms = new MemoryStream())
        {
            workbook.Write(ms);
            ms.Flush();
            ms.Position = 0;
            return ms;
        }
    }
    /// <summary>
    /// DataTable 导出到 Excel 的 MemoryStream
    /// </summary>
    /// <param name="workbook">源 workbook</param>
    /// <param name="dtSource">源 DataTable</param>
    /// <param name="strHeaderText">表头文本 空值未不要表头标题(多个表对应多个表头以英文逗号(,)分开，个数应与表相同)</param>
    /// <returns></returns>
    public static void ExportFromDSExcel(HSSFWorkbook workbook, DataTable dtSource, string strHeaderText)
    {
        ICellStyle dateStyle = workbook.CreateCellStyle();
        IDataFormat format = workbook.CreateDataFormat();
        dateStyle.DataFormat = format.GetFormat("yyyy-MM-dd HH:mm:ss");
        ISheet sheet = workbook.CreateSheet(strHeaderText);

        int[] arrColWidth = new int[dtSource.Columns.Count];
        foreach (DataColumn item in dtSource.Columns)
        {
            arrColWidth[item.Ordinal] = Encoding.GetEncoding("utf-8").GetBytes(item.ColumnName.ToString()).Length;
        }
        for (int i = 0; i < dtSource.Rows.Count; i++)
        {
            for (int j = 0; j < dtSource.Columns.Count; j++)
            {
                int intTemp = Encoding.GetEncoding("utf-8").GetBytes(dtSource.Rows[i][j].ToString()).Length;
                if (intTemp > arrColWidth[j])
                {
                    arrColWidth[j] = intTemp;
                }
            }
        }
        int rowIndex = 0;
        int intTop = 0;
        foreach (DataRow row in dtSource.Rows)
        {
            #region 新建表、填充表头、填充列头，样式
            if (rowIndex == 65535 || rowIndex == 0)
            {
                if (rowIndex != 0)
                {
                    sheet = workbook.CreateSheet();
                }
                intTop = 0;
                #region 表头及样式
                {
                    if (strHeaderText.Length > 0)
                    {
                        IRow headerRow = sheet.CreateRow(intTop);
                        intTop += 1;
                        headerRow.HeightInPoints = 25;
                        headerRow.CreateCell(0).SetCellValue(strHeaderText);
                        ICellStyle headStyle = workbook.CreateCellStyle();
                        headStyle.Alignment = HorizontalAlignment.Center;
                        IFont font = workbook.CreateFont();
                        font.FontHeightInPoints = 20;
                        font.IsBold = true;
                        headStyle.SetFont(font);
                        headerRow.GetCell(0).CellStyle = headStyle;
                        sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, dtSource.Columns.Count - 1));

                    }
                }
                #endregion
                #region  列头及样式
                {
                    IRow headerRow = sheet.CreateRow(intTop);
                    intTop += 1;
                    ICellStyle headStyle = workbook.CreateCellStyle();
                    headStyle.Alignment = HorizontalAlignment.Center;
                    IFont font = workbook.CreateFont();
                    font.IsBold = true;
                    headStyle.SetFont(font);
                    foreach (DataColumn column in dtSource.Columns)
                    {
                        headerRow.CreateCell(column.Ordinal).SetCellValue(column.ColumnName);
                        headerRow.GetCell(column.Ordinal).CellStyle = headStyle;
                        //设置列宽
                        // sheet.SetColumnWidth(column.Ordinal, (arrColWidth[column.Ordinal] + 1) * 256); // 设置设置列宽 太长会报错 修改2014 年9月22日
                        int dd = (arrColWidth[column.Ordinal] + 1) * 256;

                        if (dd > 200 * 256)
                        {
                            dd = 100 * 256;
                        }


                        sheet.SetColumnWidth(column.Ordinal, dd);
                    }


                }
                #endregion
                rowIndex = intTop;
            }
            #endregion
            #region 填充内容
            IRow dataRow = sheet.CreateRow(rowIndex);
            foreach (DataColumn column in dtSource.Columns)
            {
                ICell newCell = dataRow.CreateCell(column.Ordinal);
                string drValue = row[column].ToString();
                switch (column.DataType.ToString())
                {
                    case "System.String"://字符串类型
                        newCell.SetCellValue(drValue);
                        break;
                    case "System.DateTime"://日期类型
                        if (drValue.Length > 0)
                        {
                            DateTime dateV;
                            DateTime.TryParse(drValue, out dateV);
                            newCell.SetCellValue(dateV);
                            newCell.CellStyle = dateStyle;//格式化显示
                        }
                        else { newCell.SetCellValue(drValue); }
                        break;
                    case "System.Boolean"://布尔型
                        bool boolV = false;
                        bool.TryParse(drValue, out boolV);
                        newCell.SetCellValue(boolV);
                        break;
                    case "System.Int16":
                    case "System.Int32":
                    case "System.Int64":
                    case "System.Byte":
                        int intV = 0;
                        int.TryParse(drValue, out intV);
                        newCell.SetCellValue(intV);
                        break;
                    case "System.Decimal":
                    case "System.Double":
                        double doubV = 0;
                        double.TryParse(drValue, out doubV);
                        newCell.SetCellValue(doubV);
                        break;
                    case "System.DBNull"://空值处理
                        newCell.SetCellValue("");
                        break;
                    default:
                        newCell.SetCellValue("");
                        break;
                }
            }
            #endregion
            rowIndex++;
        }
    }


    #region 导出到Excel模板文件
    /// <summary>
    /// 导出到Excel模板文件
    /// </summary>
    /// <param name="dtSource">源 DataaTable</param>
    /// <param name="strHeaderText">表头文本</param>
    /// <param name="strFileName">保存位置(文件名及路径)</param>
    public static async Task ExportExcelTemplate(ISqlSugarClient _Db, List<SmImpTemplateDetail> details, string strHeaderText, string sheetName, string strFileName, string templatefileUrl, int startRow)
    {
        using (MemoryStream ms = await ExportExcelTemplate(_Db, details, strHeaderText, sheetName, templatefileUrl, startRow))
        {
            using (FileStream fs = new FileStream(strFileName, FileMode.Create, FileAccess.Write))
            {
                byte[] data = ms.ToArray();
                fs.Write(data, 0, data.Length);
                fs.Flush();
            }
        }
    }

    #region DataTable 导出到 Excel 的 MemoryStream
    /// <summary>
    /// DataTable 导出到 Excel 的 MemoryStream
    /// </summary>
    /// <param name="dtSource">源 DataTable</param>
    /// <param name="strHeaderText">表头文本 空值未不要表头标题</param>
    /// <returns></returns>
    private async static Task<MemoryStream> ExportExcelTemplate(ISqlSugarClient _Db, List<SmImpTemplateDetail> templateDetails, string strHeaderText, string sheetName, string templatefileUrl, int startRow)
    {
        //var details = templateDetails.Where(x => x.DataType == null).ToList();
        //var masterDetails = templateDetails.Where(x => x.DataType == "Master").ToList();
        var details = templateDetails.ToList();
        XSSFWorkbook workbook;
        ISheet sheet;

        bool isExistFile = false;

        // 检查是否传入了模板文件URL且文件存在
        if (!string.IsNullOrEmpty(templatefileUrl) && File.Exists(FileHelper.GetPhysicsPath() + templatefileUrl))
        {
            // 从现有模板文件读取workbook
            using (FileStream file = new(FileHelper.GetPhysicsPath() + templatefileUrl, FileMode.Open, FileAccess.Read))
            {
                workbook = new XSSFWorkbook(file);
            }

            // 获取或创建指定的sheet
            int sheetIndex = workbook.GetSheetIndex(sheetName);
            if (sheetIndex >= 0)
                sheet = workbook.GetSheetAt(sheetIndex);
            else
                sheet = workbook.CreateSheet(sheetName);

            // 检查并删除已存在的"模板信息"sheet
            int templateInfoSheetIndex = workbook.GetSheetIndex("模板信息");
            if (templateInfoSheetIndex >= 0)
                workbook.RemoveSheetAt(templateInfoSheetIndex);

            isExistFile = true;
        }
        else
        {
            // 创建新的workbook和sheet
            workbook = new XSSFWorkbook();
            sheet = workbook.CreateSheet(sheetName);
        }

        #region 文件属性 
        DocumentSummaryInformation dsi = new();
        dsi.Company = "EUCloud";
        //workbook.DocumentSummaryInformation = dsi;
        SummaryInformation si = new();

        si.Author = "EUCloud";
        si.ApplicationName = "EUCloud";
        si.LastAuthor = "EUCloud";
        si.Comments = "";
        si.Title = "";
        si.Subject = "";
        si.CreateDateTime = DateTime.Now;
        //workbook.SummaryInformation = si;
        #endregion

        ICellStyle dateStyle = workbook.CreateCellStyle();
        IDataFormat format = workbook.CreateDataFormat();
        dateStyle.DataFormat = format.GetFormat("yyyy-mm-dd");
        ICellStyle datetimeStyle = workbook.CreateCellStyle();
        datetimeStyle.DataFormat = format.GetFormat("yyyy-mm-dd hh:mm");
        ICellStyle datetimesStyle = workbook.CreateCellStyle();
        datetimesStyle.DataFormat = format.GetFormat("yyyy-mm-dd hh:mm:ss");
        int[] arrColWidth = new int[details.Count];

        foreach (var (item, index) in details.Select((value, i) => (value, i)))
            arrColWidth[index] = Encoding.GetEncoding("utf-8").GetBytes(item.ColumnCode).Length;

        int rowCount = 20;
        //for (int i = 0; i < 20; i++)
        //    for (int j = 0; j < details.Count; j++)
        //    {
        //        int intTemp = Encoding.GetEncoding("utf-8").GetBytes(dtSource.Rows[i][j].ToString()).Length;
        //        if (intTemp > arrColWidth[j])
        //        {
        //            arrColWidth[j] = intTemp;
        //        }
        //    }
        int rowIndex = 0;
        int intTop = 0;

        foreach (var (item, index) in details.Select((value, i) => (value, i)))
        {
            var dataType = item.DataType;
            var rowNo = (item.RowNo ?? 0) - 1;
            var colNo = (item.ColumnNo ?? 0) - 1;
            if (item.CommonListSqlId.IsNotEmptyOrNull())
            {
                var sql = await LovHelper.GetCommonListSql(_Db, item.CommonListSqlId);
                if (sql.IsNotEmptyOrNull())
                {
                    var data = DBHelper.QueryList<ComboGridData>(sql);

                    // 创建隐藏的 sheet 用于存放下拉选项的数据源
                    ISheet hiddenSheet1 = workbook.CreateSheet(item.ColumnName);

                    for (int m = 0; m < data.Count; m++)
                    {
                        var row1 = hiddenSheet1.CreateRow(m);
                        var cell1 = row1.CreateCell(0);
                        cell1.SetCellValue(data[m].label);

                        workbook.SetSheetHidden(workbook.GetSheetIndex(hiddenSheet1), true);
                    }

                    IDataValidationHelper dvHelper = sheet.GetDataValidationHelper();
                    CellRangeAddressList addressList = dataType == "Master" ? new CellRangeAddressList(rowNo, rowNo, colNo, colNo) : new CellRangeAddressList(startRow, 65535, colNo, colNo); // B2:Bn

                    // 设置数据验证约束（引用隐藏sheet中的范围）
                    IDataValidationConstraint constraint = dvHelper.CreateFormulaListConstraint($"{item.ColumnName}!$A$1:$A${data.Count}");
                    IDataValidation validation = dvHelper.CreateValidation(constraint, addressList);

                    // 提交数据验证规则到sheet
                    sheet.AddValidationData(validation);
                }
            }
            else if (item.LovCode.IsNotEmptyOrNull())
            {
                var enumData = await LovHelper.GetLovList(_Db, item.LovCode);

                var vals = enumData.Select(x => x.Text).ToArray();
                //設置生成下拉框的行和列
                var cellRegions = dataType == "Master" ? new CellRangeAddressList(rowNo, rowNo, colNo, colNo) : new CellRangeAddressList(startRow, 65535, colNo, colNo);
                IDataValidation validation = null;

                if (sheet.GetType().Name.Contains("XSSF")) // .xlsx
                {
                    XSSFDataValidationHelper helper = new XSSFDataValidationHelper((XSSFSheet)sheet);//获得一个数据验证Helper  
                                                                                                     //IDataValidation
                    validation = helper.CreateValidation(
                    helper.CreateExplicitListConstraint(vals), cellRegions);//创建约束
                }
                else // HSSF .xls
                {
                    //設置 下拉框內容
                    DVConstraint constraint = DVConstraint.CreateExplicitListConstraint(vals);
                    validation = new HSSFDataValidation(cellRegions, constraint);

                }

                validation.CreateErrorBox("输入不合法", "请输入或选择下拉列表中的值。");
                validation.ShowPromptBox = true;

                sheet.AddValidationData(validation);
            }
        }

        for (int i = 0; i < rowCount; i++)
        {
            #region 新建表、填充表头、填充列头，样式
            if (rowIndex == 655350 || rowIndex == 0)
            {
                if (rowIndex != 0)
                {
                    sheet = workbook.CreateSheet();
                }
                intTop = 0;

                #region 表头及样式
                if (strHeaderText.Length > 0 && !isExistFile)
                {
                    IRow headerRow = sheet.CreateRow(intTop);
                    intTop += 1;
                    headerRow.HeightInPoints = 25;
                    headerRow.CreateCell(0).SetCellValue(strHeaderText);
                    ICellStyle headStyle = workbook.CreateCellStyle();
                    headStyle.Alignment = HorizontalAlignment.Center;//居中
                    headStyle.VerticalAlignment = VerticalAlignment.Center;//垂直居中
                    headStyle.BorderBottom = BorderStyle.Thin;
                    headStyle.BorderLeft = BorderStyle.None;
                    headStyle.BorderRight = BorderStyle.None;
                    headStyle.BorderTop = BorderStyle.None;

                    IFont font = workbook.CreateFont();
                    font.FontHeightInPoints = (short)14;
                    font.FontName = "宋体";
                    font.IsBold = true;
                    headStyle.SetFont(font);
                    headerRow.GetCell(0).CellStyle = headStyle;
                    sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, details.Count - 1));
                }
                #endregion

                #region  列头及样式
                if (!isExistFile)
                {
                    IRow headerRow = sheet.CreateRow(intTop);
                    intTop += 1;
                    ICellStyle headStyle = workbook.CreateCellStyle();
                    headStyle.Alignment = HorizontalAlignment.Center; //居中
                    headStyle.VerticalAlignment = VerticalAlignment.Center;//垂直居中 
                                                                           //headStyle.WrapText = true;//自动换行
                                                                           // 边框
                    headStyle.BorderBottom = BorderStyle.Thin;
                    headStyle.BorderLeft = BorderStyle.Thin;
                    headStyle.BorderRight = BorderStyle.Thin;
                    headStyle.BorderTop = BorderStyle.Thin;
                    // 字体
                    IFont font = workbook.CreateFont();
                    font.FontHeightInPoints = (short)10;
                    font.IsBold = true;
                    font.FontName = "宋体";
                    headStyle.SetFont(font);

                    foreach (var (item, index) in details.Select((value, i) => (value, i)))
                    {
                        headerRow.CreateCell(index).SetCellValue(item.ColumnName);
                        headerRow.GetCell(index).CellStyle = headStyle;
                        sheet.SetColumnWidth(index, (arrColWidth[index] + 1) * 256);

                        if (item.CommonListSqlId.IsNotEmptyOrNull())
                        {
                            var sql = $"SELECT * FROM SmCommonListSql WHERE IsDeleted='false' AND ID='{item.CommonListSqlId}'";
                            var listSql = DBHelper.QueryFirst<SmCommonListSql>(sql);
                            if (listSql.IsNotEmptyOrNull())
                            {
                                var data = DBHelper.QueryList<ComboGridData>(listSql.SelectSql);

                                // 创建隐藏的 sheet 用于存放下拉选项的数据源
                                ISheet hiddenSheet1 = workbook.CreateSheet(item.ColumnName);

                                for (int m = 0; m < data.Count; m++)
                                {
                                    var row1 = hiddenSheet1.CreateRow(m);
                                    var cell1 = row1.CreateCell(0);
                                    cell1.SetCellValue(data[m].label);

                                    workbook.SetSheetHidden(workbook.GetSheetIndex(hiddenSheet1), true);
                                }

                                IDataValidationHelper dvHelper = sheet.GetDataValidationHelper();
                                CellRangeAddressList addressList = new CellRangeAddressList(intTop, 65535, index, index); // B2:Bn

                                // 设置数据验证约束（引用隐藏sheet中的范围）
                                IDataValidationConstraint constraint = dvHelper.CreateFormulaListConstraint($"{item.ColumnName}!$A$1:$A${data.Count}");
                                IDataValidation validation = dvHelper.CreateValidation(constraint, addressList);

                                // 提交数据验证规则到sheet
                                sheet.AddValidationData(validation);
                            }
                        }
                        else if (item.LovCode.IsNotEmptyOrNull())
                        {
                            var enumData = await LovHelper.GetLovList(_Db, item.LovCode);

                            var vals = enumData.Select(x => x.Text).ToArray();
                            //設置生成下拉框的行和列
                            var cellRegions = new CellRangeAddressList(1, 65535, index, index);
                            IDataValidation validation = null;

                            if (sheet.GetType().Name.Contains("XSSF")) // .xlsx
                            {
                                XSSFDataValidationHelper helper = new XSSFDataValidationHelper((XSSFSheet)sheet);//获得一个数据验证Helper  
                                                                                                                 //IDataValidation
                                validation = helper.CreateValidation(
                                helper.CreateExplicitListConstraint(vals), cellRegions);//创建约束
                            }
                            else // HSSF .xls
                            {
                                //設置 下拉框內容
                                DVConstraint constraint = DVConstraint.CreateExplicitListConstraint(vals);
                                validation = new HSSFDataValidation(cellRegions, constraint);

                            }

                            validation.CreateErrorBox("输入不合法", "请输入或选择下拉列表中的值。");
                            validation.ShowPromptBox = true;

                            sheet.AddValidationData(validation);
                        }
                    }
                }
                else
                {

                }
                #endregion

                rowIndex = intTop;
            }
            #endregion

            rowIndex++;
        }
        #region 设置模板信息
        var hiddenSheet = workbook.CreateSheet("模板信息");
        var row = hiddenSheet.CreateRow(0);
        var cell = row.CreateCell(0);
        cell.SetCellValue(details.FirstOrDefault().ImpTemplateId.ObjToString());

        row = hiddenSheet.CreateRow(1);
        cell = row.CreateCell(0);
        cell.SetCellValue(DateTimeHelper.ConvertToSecondString(Utility.GetSysDate()));
        workbook.SetSheetHidden(workbook.GetSheetIndex(hiddenSheet), true);
        #endregion

        using (MemoryStream ms = new MemoryStream())
        {
            workbook.Write(ms);
            ms.Flush();
            //ms.Position = 0;
            return ms;
        }
    }
    #endregion

    #endregion
}
