using System.Data;
using System.Text;
using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

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
        DocumentSummaryInformation dsi = PropertySetFactory.CreateDocumentSummaryInformation();
        dsi.Company = "EUCloud";
        //workbook.DocumentSummaryInformation = dsi;
        SummaryInformation si = PropertySetFactory.CreateSummaryInformation();
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
                        sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(0, 0, 0, dtSource.Columns.Count - 1));

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
    public static DataTable ImportExcel(string strFileName, string sheetName = "")
    {
        DataTable dt = new DataTable();
        //HSSFWorkbook hssfworkbook;
        IWorkbook hssfworkbook;
        ISheet sheet;
        using (FileStream file = new FileStream(strFileName, FileMode.Open, FileAccess.Read))
        {
            //hssfworkbook = new HSSFWorkbook(file);
            //hssfworkbook = new XSSFWorkbook(file);
            hssfworkbook = NPOI.SS.UserModel.WorkbookFactory.Create(file);
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
        System.Collections.IEnumerator rows = sheet.GetRowEnumerator();
        IRow headerRow = sheet.GetRow(0);
        int cellCount = headerRow.LastCellNum;
        for (int j = 0; j < cellCount; j++)
        {
            ICell cell = headerRow.GetCell(j);
            dt.Columns.Add(cell.ToString());
        }
        for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++)
        {
            IRow row = sheet.GetRow(i);
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

    /// <summary>
    /// 获取单元格类型
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    private static string GetCellValue(ICell cell)
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
                    return DateTimeHelper.ConvertToSecondString(cell.DateCellValue);
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
        DocumentSummaryInformation dsi = PropertySetFactory.CreateDocumentSummaryInformation();
        dsi.Company = "517best.com";
        workbook.DocumentSummaryInformation = dsi;
        SummaryInformation si = PropertySetFactory.CreateSummaryInformation();
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
                        sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(0, 0, 0, dtSource.Columns.Count - 1));

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
}
