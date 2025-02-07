/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmImpTemplateDetail.cs
*
*功 能： N / A
* 类 名： SmImpTemplateDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/24 22:43:02  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Services;

public partial class CommonServices : BaseServices<SmModules, SmModulesDto, InsertSmModulesInput, EditSmModulesInput>, ICommonServices
{
    private readonly IBaseRepository<SmModules> _dal;
    public CommonServices(IBaseRepository<SmModules> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    #region 自定义列模块数据返回
    /// <summary>
    /// 自定义列模块数据返回
    /// </summary>
    /// <param name="paramData"></param>
    /// <param name="moduleCode"></param>
    /// <param name="sorter"></param>
    /// <param name="filter"></param>
    /// <param name="parentColumn"></param>
    /// <param name="parentId"></param>
    /// <returns></returns>
    public async Task<GridListReturn> GetGridList(string paramData, string moduleCode, string sorter = "{}", string parentColumn = null, string parentId = null)
    {
        int current = 1;
        int pageSize = 10;
        int total = 0;

        var searchParam = ConvertToDic(paramData);
        //var filterParam = ConvertToDic(filter);
        var sorterParam = JsonHelper.JsonToObj<Dictionary<string, string>>(sorter);

        string queryCodition = "1=1";
        string keyWord = string.Empty;

        #region 处理查询条件
        var moduleColumnInfo = new ModuleSqlColumn(moduleCode);
        var moduleColumns = moduleColumnInfo.GetModuleSqlColumn();

        foreach (var item in searchParam)
        {
            if (item.Key == "current")
            {
                current = int.Parse(item.Value.ToString());
                continue;
            }
            else if (item.Key == "pageSize")
            {
                pageSize = int.Parse(item.Value.ToString());
                continue;
            }
            else if (item.Key == "_timestamp")
            {
                continue;
            }
            else if (item.Key == "keyWord")
            {
                keyWord = item.Value.ToString();
                continue;
            }
            else if (!string.IsNullOrEmpty(item.Value.ToString()))
            {
                if (moduleColumns.Any())
                {
                    var column = moduleColumns.Where(a => a.DataIndex == item.Key).FirstOrDefault();
                    if (column != null)
                        queryCodition += " AND " + column.TableAlias + "." + item.Key + " like '%" + item.Value.ToString() + "%'";
                }
                else
                    queryCodition += " AND A." + item.Key + " like '%" + item.Value.ToString() + "%'";
            }
            //if (string.IsNullOrEmpty(item.Value.ToString()))
            //    queryCodition += " AND A." + item.Key + " =''";
            //else
            //    queryCodition += " AND A." + item.Key + " like '%" + item.Value.ToString() + "%'";
        }
        if (!string.IsNullOrEmpty(parentId) && !string.IsNullOrEmpty(parentColumn))
            queryCodition += " AND A." + parentColumn + " = '" + parentId + "'";
        #endregion

        //#region 处理过滤条件
        //foreach (var item in filterParam)
        //{

        //    if (!string.IsNullOrEmpty(item.Value.ToString()))
        //    {
        //        if (JsonHelper.IsJson(item.Value))
        //        {
        //            var ids = JsonHelper.JsonToObj<List<Guid>>(item.Value.ToString());
        //            if (ids.Any())
        //                queryCodition += $" AND A.{item.Key} IN ({string.Join(",", ids.Select(id => "'" + id + "'"))})";
        //        }
        //        else
        //            queryCodition += " AND A." + item.Key + " = '" + item.Value.ToString() + "'";

        //    }
        //}
        //#endregion

        #region 处理关键字搜索
        string keyWordCondition = string.Empty;
        if (!string.IsNullOrEmpty(keyWord) && moduleColumns.Any())
            moduleColumns.ForEach(item =>
            {
                if (item.ValueType == null && item.HideInSearch == false)
                {
                    string TableAlias = item.TableAlias;
                    string dataIndex = item.DataIndex;
                    if (string.IsNullOrEmpty(keyWordCondition))
                        keyWordCondition = TableAlias + "." + dataIndex + " LIKE '%" + keyWord + "%'";
                    else
                        keyWordCondition += " OR " + TableAlias + "." + dataIndex + " LIKE '%" + keyWord + "%'";
                }
            });
        #endregion

        string userId = string.Empty;
        var moduleSql = new ModuleSql(moduleCode);
        var grid = new GridList();
        string tableName = moduleSql.GetTableName();
        string fullSql = moduleSql.GetFullSql();
        string SqlSelectBrwAndTable = moduleSql.GetSqlSelectBrwAndTable();
        string SqlSelectAndTable = moduleSql.GetSqlSelectAndTable();
        if (!string.IsNullOrEmpty(tableName))
        {
            SqlSelectBrwAndTable = string.Format(SqlSelectBrwAndTable, tableName);
            SqlSelectAndTable = string.Format(SqlSelectAndTable, tableName);
        }
        string SqlDefaultCondition = moduleSql.GetSqlDefaultCondition();

        #region 处理关键字搜索
        if (!string.IsNullOrEmpty(keyWordCondition))
            SqlDefaultCondition += " AND (" + keyWordCondition + ")";
        #endregion

        //SqlDefaultCondition = SqlDefaultCondition.Replace("[UserId]", userId);
        string DefaultSortField = moduleSql.GetDefaultSortField();
        string DefaultSortDirection = moduleSql.GetDefaultSortDirection();
        if (string.IsNullOrEmpty(DefaultSortDirection))
        {
            DefaultSortDirection = "ASC";
        }
        grid.FullSql = fullSql;
        grid.SqlSelect = SqlSelectBrwAndTable;
        grid.SqlDefaultCondition = SqlDefaultCondition;
        grid.SqlQueryCondition = queryCodition;
        grid.SortField = DefaultSortField;
        grid.SortDirection = DefaultSortDirection;

        #region 处理排序
        if (sorterParam.Count > 0)
            foreach (var item in sorterParam)
            {
                grid.SortField = item.Key;
                if (item.Value == "ascend")
                    grid.SortDirection = "ASC";
                else if (item.Value == "descend")
                    grid.SortDirection = "DESC";
            }
        #endregion

        grid.PageSize = pageSize;
        grid.CurrentPage = current;
        grid.ModuleCode = moduleCode;
        total = grid.GetTotalCount();
        string sql = grid.GetQueryString();
        var dtTemp = await Db.Ado.GetDataTableAsync(sql);
        var dt = Utility.FormatDataTableForTree(moduleCode, userId, dtTemp);
        return new GridListReturn(pageSize, current, total, dt, ResponseText.QUERY_SUCCESS);
    }

    /// <summary>
    /// 自定义列模块数据返回
    /// </summary>
    /// <param name="filter">filter</param>
    /// <param name="moduleCode">模块代码</param>
    /// <returns></returns>
    public async Task<GridListReturn> QueryByFilter(QueryFilter filter, string moduleCode)
    {
        int total = 0;

        string queryCodition = "1=1";
        string keyWord = string.Empty;

        #region 处理查询条件
        var moduleColumnInfo = new ModuleSqlColumn(moduleCode);
        var moduleColumns = moduleColumnInfo.GetModuleSqlColumn();

        foreach (var item in filter.@params)
        {
            if (item.Key == "keyWord")
            {
                keyWord = item.Value.ObjToString();
                continue;
            }
            else if (!string.IsNullOrEmpty(item.Value.ObjToString()))
            {
                if (moduleColumns.Any())
                {
                    var column = moduleColumns.Where(a => a.DataIndex == item.Key).FirstOrDefault();
                    if (column != null)
                        queryCodition += " AND " + column.TableAlias + "." + item.Key + " like '%" + item.Value.ObjToString() + "%'";
                }
                else
                    queryCodition += " AND A." + item.Key + " like '%" + item.Value.ObjToString() + "%'";
            }
            //if (string.IsNullOrEmpty(item.Value.ToString()))
            //    queryCodition += " AND A." + item.Key + " =''";
            //else
            //    queryCodition += " AND A." + item.Key + " like '%" + item.Value.ToString() + "%'";
        }
        //if (!string.IsNullOrEmpty(parentId) && !string.IsNullOrEmpty(parentColumn))
        //    queryCodition += " AND A." + parentColumn + " = '" + parentId + "'";
        if (filter.Conditions.IsNotEmptyOrNull())
            queryCodition += " AND " + filter.Conditions;

        #endregion

        //#region 处理过滤条件
        //foreach (var item in filterParam)
        //{

        //    if (!string.IsNullOrEmpty(item.Value.ToString()))
        //    {
        //        if (JsonHelper.IsJson(item.Value))
        //        {
        //            var ids = JsonHelper.JsonToObj<List<Guid>>(item.Value.ToString());
        //            if (ids.Any())
        //                queryCodition += $" AND A.{item.Key} IN ({string.Join(",", ids.Select(id => "'" + id + "'"))})";
        //        }
        //        else
        //            queryCodition += " AND A." + item.Key + " = '" + item.Value.ToString() + "'";

        //    }
        //}
        //#endregion

        #region 处理关键字搜索
        var keyWordCondition = string.Empty;
        if (keyWord.IsNotEmptyOrNull() && moduleColumns.Any())
            moduleColumns.ForEach(item =>
            {
                if (item.ValueType == null && item.HideInSearch == false)
                {
                    var TableAlias = item.TableAlias;
                    var dataIndex = item.DataIndex;
                    if (string.IsNullOrEmpty(keyWordCondition))
                        keyWordCondition = TableAlias + "." + dataIndex + " LIKE '%" + keyWord + "%'";
                    else
                        keyWordCondition += " OR " + TableAlias + "." + dataIndex + " LIKE '%" + keyWord + "%'";
                }
            });
        #endregion

        var userId = string.Empty;
        ModuleSql moduleSql = new(moduleCode);
        GridList grid = new();
        var tableName = moduleSql.GetTableName();
        var fullSql = moduleSql.GetFullSql();
        var sqlSelectBrwAndTable = moduleSql.GetSqlSelectBrwAndTable();
        var sqlSelectAndTable = moduleSql.GetSqlSelectAndTable();
        if (tableName.IsNotEmptyOrNull())
        {
            sqlSelectBrwAndTable = string.Format(sqlSelectBrwAndTable, tableName);
            sqlSelectAndTable = string.Format(sqlSelectAndTable, tableName);
        }
        var sqlDefaultCondition = moduleSql.GetSqlDefaultCondition();

        #region 处理关键字搜索
        if (keyWordCondition.IsNotEmptyOrNull())
            sqlDefaultCondition += " AND (" + keyWordCondition + ")";
        #endregion

        //sqlDefaultCondition = SqlDefaultCondition.Replace("[UserId]", userId);
        var defaultSortField = moduleSql.GetDefaultSortField();
        var defaultSortDirection = moduleSql.GetDefaultSortDirection();
        if (defaultSortDirection.IsNotEmptyOrNull())
            defaultSortDirection = "ASC";

        grid.FullSql = fullSql;
        grid.SqlSelect = sqlSelectBrwAndTable;
        grid.SqlDefaultCondition = sqlDefaultCondition;
        grid.SqlQueryCondition = queryCodition;
        grid.SortField = defaultSortField;
        grid.SortDirection = defaultSortDirection;

        #region 处理排序
        if (filter.sorter != null && filter.sorter.Count > 0)
            foreach (var item in filter.sorter)
            {
                grid.SortField = item.Key;
                grid.SortDirection = item.Value == "ascend" ? "ASC" : "DESC";
                //grid.SortDirection = item.Value switch
                //{
                //    "ascend" => "ASC",
                //    "descend" => "DESC",
                //    _ => "ASC"
                //};
            }
        #endregion

        grid.PageSize = filter.PageSize;
        grid.CurrentPage = filter.PageIndex;
        grid.ModuleCode = moduleCode;
        total = grid.GetTotalCount();
        string sql = grid.GetQueryString();
        var dtTemp = await Db.Ado.GetDataTableAsync(sql);
        var dt = Utility.FormatDataTableForTree(moduleCode, userId, dtTemp);
        return new GridListReturn(filter.PageSize, filter.PageIndex, total, dt, ResponseText.QUERY_SUCCESS);
    }

    #endregion

    #region Excel导出
    /// <summary>
    /// Excel导出
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="paramData">参数</param>
    /// <param name="sorter">排序</param>
    /// <param name="exportExcelColumns">导出栏位</param>
    /// <returns></returns>
    public async Task<ServiceResult<string>> ExportExcelAsync(QueryFilter filter, string moduleCode)
    {
        int current = 1;
        string queryCondition = "1=1 ";
        string defaultCondition = string.Empty;
        int pageCount = 999999999;// options.Rows;
        int totalCount;
        int outPageSize;
        string fileId = Utility.GuidId1;



        string keyWord = string.Empty;

        #region 处理查询条件
        var moduleColumnInfo = new ModuleSqlColumn(moduleCode);
        var moduleColumns = moduleColumnInfo.GetModuleSqlColumn();

        foreach (var item in filter.@params)
        {
            if (item.Key == "keyWord")
            {
                keyWord = item.Value.ObjToString();
                continue;
            }
            else if (!string.IsNullOrEmpty(item.Value.ObjToString()))
            {
                if (moduleColumns.Any())
                {
                    var column = moduleColumns.Where(a => a.DataIndex == item.Key).FirstOrDefault();
                    if (column != null)
                        queryCondition += " AND " + column.TableAlias + "." + item.Key + " like '%" + item.Value.ObjToString() + "%'";
                }
                else
                    queryCondition += " AND A." + item.Key + " like '%" + item.Value.ObjToString() + "%'";
            }
            //if (string.IsNullOrEmpty(item.Value.ToString()))
            //    queryCodition += " AND A." + item.Key + " =''";
            //else
            //    queryCodition += " AND A." + item.Key + " like '%" + item.Value.ToString() + "%'";
        }
        //if (!string.IsNullOrEmpty(parentId) && !string.IsNullOrEmpty(parentColumn))
        //    queryCodition += " AND A." + parentColumn + " = '" + parentId + "'";
        if (filter.Conditions.IsNotEmptyOrNull())
            queryCondition += " AND " + filter.Conditions;

        #endregion

        //var searchParam = ConvertToDic(paramData);
        //var sorterParam = JsonHelper.JsonToObj<Dictionary<string, string>>(sorter);

        #region 处理模块信息
        SmModules module = ModuleInfo.GetModuleInfo(moduleCode);

        ModuleSql moduleSql = new ModuleSql(moduleCode);
        ModuleSqlColumn moduleSqlColumn = new ModuleSqlColumn(moduleCode);
        #endregion

        #region 处理默认条件
        //if (!string.IsNullOrEmpty(options.RowId))
        //{
        //    defaultCondition = " AND " + moduleSql.GetTableAliasName() + ".ROW_ID='" + options.RowId + "'";
        //}
        //if (!string.IsNullOrEmpty(options.MasterId))
        //{
        //    defaultCondition = " AND " + moduleSql.GetTableAliasName() + "." + moduleSql.GetPrimaryKey() + "='" + options.MasterId + "'";
        //}
        #endregion

        #region 处理查询条件
        //foreach (var item in searchParam)
        //{
        //    if (item.Key == "current")
        //        continue;

        //    if (item.Key == "pageSize")
        //        continue;

        //    if (item.Key == "_timestamp")
        //        continue;
        //    if (!string.IsNullOrEmpty(item.Value.ToString()))
        //        queryCondition += " AND A." + item.Key + " like '%" + item.Value.ToString() + "%'";

        //}
        #endregion

        #region 处理排序
        string DefaultSortField = moduleSql.GetDefaultSortField();
        string DefaultSortDirection = moduleSql.GetDefaultSortDirection();
        //if (sorterParam.Count > 0)
        //    foreach (var item in sorterParam)
        //    {
        //        DefaultSortField = item.Key;
        //        if (item.Value == "ascend")
        //            DefaultSortDirection = "ASC";
        //        else if (item.Value == "descend")
        //            DefaultSortDirection = "DESC";

        //    }
        #endregion

        string sql = moduleSql.GetCurrentSql(moduleCode, current, pageCount, DefaultSortField, DefaultSortDirection, defaultCondition, queryCondition, out totalCount, out outPageSize);

        string moduleColumns1 = moduleSqlColumn.GetExportExcelColumns();
        //if (!string.IsNullOrEmpty(exportExcelColumns))
        //    moduleColumns = exportExcelColumns;

        string excelSql = "SELECT " + moduleColumns1 + " FROM (" + sql + ") A";
        string tableName = module.ModuleName;
        string fileName = $"{tableName}_{Utility.GetSysDate().ToSecondString1()}.xlsx";
        string folder = Utility.GetSysDate().ToString("yyyyMMdd");
        string filePath = $"/Download/ExcelExport/{folder}/";
        string savePath = "wwwroot" + filePath;

        FileHelper.CreateDirectory(savePath);

        var dt = DBHelper.GetDataTable(excelSql);
        foreach (DataColumn column in dt.Columns)
            column.Caption = moduleSqlColumn.GetExportExcelColumnRenderer(column.ColumnName);
        NPOIHelper.ExportExcel(dt, tableName, savePath + fileName);

        #region 导入文件数据 
        var di = new DbInsert("FileAttachment");
        di.IsInitRowId = false;
        di.Values("ID", fileId);
        di.Values("OriginalFileName", fileName);
        di.Values("FileName", fileName);
        di.Values("FileExt", "xlsx");
        di.Values("Path", filePath);
        di.Values("ImageType", "ExcelExport");
        await Db.Ado.ExecuteCommandAsync(di.GetSql());
        #endregion

        #region 记录模块操作日志
        try
        {
            //DBHelper.RecordOperateLog(User.Identity.Name, moduleCode, moduleSql.GetTableName(), "", OperateType.Excel, savePath + fileName);
        }
        catch
        {

        }
        #endregion

        return Success(fileId, "导出成功！");
    }

    #endregion

    #region Excel导入
    /// <summary>
    /// Excel导入
    /// </summary>
    /// <param name="import"></param>
    /// <param name="moduleCode">模块代码</param>
    /// <returns></returns>
    public async Task<ServiceResult<ImportExcelResult>> ImportExcelAsync(ImportExcelForm import, string moduleCode)
    {
        string message = string.Empty;
        string importDataId = Utility.GuidId1;

        var result = new ImportExcelResult();

        try
        {
            result.ImportDataId = importDataId;
            var module = ModuleInfo.GetModuleInfo(moduleCode);
            if (module == null)
                return ServiceResult<ImportExcelResult>.OprateFailed($"模块代码【{moduleCode}】不存在！");

            var ext = string.Empty;
            if (import.file.FileName.IsNotEmptyOrNull())
            {
                var dotPos = import.file.FileName.LastIndexOf('.');
                ext = import.file.FileName.Substring(dotPos + 1);
            }

            string filePath = $"/ImportExcel/{DateTime.Now.ToString("yyyyMMdd")}/{Utility.GetLongID()}";

            FileHelper.CreateRootDirectory(filePath);

            var filepath = Path.Combine(filePath, import.fileName);
            using (var stream = File.Create(FileHelper.GetPhysicsPath() + filepath))
            {
                await import.file.CopyToAsync(stream);
            }
            var fileAttachment = new FileAttachment();
            fileAttachment.OriginalFileName = import.file.FileName;
            fileAttachment.FileName = import.file.FileName;
            fileAttachment.FileExt = ext;
            fileAttachment.Length = import.file.Length;
            fileAttachment.Path = filePath;
            fileAttachment.ImageType = "ImportExcel";
            //url = fileName + "." + ext;
            await Db.Insertable(fileAttachment).ExecuteCommandAsync();

            var impTemplate = await Db.Queryable<SmImpTemplate>().Where(x => x.ModuleId == module.ID).FirstAsync();
            if (impTemplate == null)
                return ServiceResult<ImportExcelResult>.OprateFailed($"请配置模块【{module.ModuleName}】的导入模板，详情请联系客服！");
            string sheetName = impTemplate.SheetName;

            var dt = NPOIHelper.ImportExcel(filepath, sheetName);
            if (dt.Rows.Count > 0)
                ImportHelper.ImportData(impTemplate, importDataId, filePath, import.fileName, dt, UserId1);

            var Importobj = new List<string>();
            var ImportNameobj = new List<string>
            {
                "行号"
            };
            var dtImportData = ImportHelper.GetImportDataDetailList(importDataId, impTemplate.ID);

            for (int i = 0; i < dtImportData.Columns.Count; i++)
                Importobj.Add(dtImportData.Columns[i].ColumnName);

            for (int i = 0; i < dt.Columns.Count; i++)
                ImportNameobj.Add(dt.Columns[i].ColumnName);

            result.ImportColumns = Importobj;
            result.ImportColumnNames = ImportNameobj;
            result.ImportList = dtImportData;
        }
        catch (Exception E)
        {
            message = E.Message;
            result.ErrorList = ImportHelper.GetImportErrorList(importDataId);
        }
        return Success(result, message);
    }

    #endregion

    #region Excel导入数据转换
    public async Task<ServiceResult> TransferExcelData(TransferExcelRequest request, string moduleCode)
    {
        string importDataId = request.ImportDataId;
        string importTemplateCode = request.ImportTemplateCode;
        string type = request.Type;
        string masterId = request.MasterId;

        await ImportHelper.TransferData(importDataId, importTemplateCode, UserId1, false);
        await ImportHelper.AfterImport(importTemplateCode, importDataId, masterId);

        return Success("导入成功！");
    }

    #endregion

    #region 清空缓存
    /// <summary>
    /// 清空缓存
    /// </summary>
    /// <returns></returns>
    public ServiceResult ClearCache()
    {
        Utility.ReInitCache();
        return Success(ResponseText.EXECUTE_SUCCESS);
    }
    #endregion

    #region 获取通用下拉数据
    public async Task<ServiceResult<List<ComboGridData>>> ComboGridData(string parentColumn, string parentId, int? current, int? pageSize, string code, string[] items, string key)
    {
        var data = new List<ComboGridData>();
        var sql = LovHelper.GetCommonListSql(code);
        if (!string.IsNullOrWhiteSpace(sql))
        {
            if (!string.IsNullOrWhiteSpace(parentColumn) && !string.IsNullOrWhiteSpace(parentId))
                sql += $" AND {parentColumn} = '{parentId}'";
            sql = @$"SELECT * FROM ({sql}) A";

            if (!string.IsNullOrWhiteSpace(key))
                sql += $" WHERE  label LIKE '%{key}%'";

            data = await Db.Ado.SqlQueryAsync<ComboGridData>(sql);
        }
        return ServiceResult<List<ComboGridData>>.OprateSuccess(data, ResponseText.QUERY_SUCCESS, data.Count);
    }
    public async Task<ServiceResult<List<ComboGridData>>> GetComboGridData(ComboGridDataBody body)
    {
        var data = new List<ComboGridData>();
        var sql = LovHelper.GetCommonListSql(body.code);
        if (!string.IsNullOrWhiteSpace(sql))
        {
            if (!string.IsNullOrWhiteSpace(body.parentColumn) && !string.IsNullOrWhiteSpace(body.parentId))
                sql += $" AND {body.parentColumn} = '{body.parentId}'";
            sql = @$"SELECT * FROM ({sql}) A";

            if (!string.IsNullOrWhiteSpace(body.key))
                sql += $" WHERE  label LIKE '%{body.key}%'";

            data = await Db.Ado.SqlQueryAsync<ComboGridData>(sql);
        }
        return ServiceResult<List<ComboGridData>>.OprateSuccess(data, ResponseText.QUERY_SUCCESS, data.Count);
    }
    #endregion

    #region 增删查改

    /// <summary>
    /// 查询数据
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="id">主键ID</param>
    /// <returns></returns>
    public async Task<ServiceResult<object>> Query(string moduleCode, Guid id)
    {
        #region 判断模块是否存在
        var module = ModuleInfo.GetModuleInfo(moduleCode);
        if (module.IsNull())
            return ServiceResult<object>.OprateFailed(ResponseText.INVALID_MODULE_CODE);
        #endregion

        var moduleSql = new ModuleSql(moduleCode);
        string tableName = moduleSql.GetTableName();
        var isDeleted = false;

        var data = await Db.Queryable<dynamic>().AS(tableName).Where("ID=@id AND IsDeleted=@isDeleted", new { id, isDeleted }).FirstAsync();

        return Success<object>(data, ResponseText.QUERY_SUCCESS);
    }

    /// <summary>
    /// 新增数据
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="entity">数据</param>
    /// <returns></returns>
    public async Task<ServiceResult<Guid>> Add(string moduleCode, object entity)
    {
        #region 判断模块是否存在
        var module = ModuleInfo.GetModuleInfo(moduleCode);
        if (module.IsNull())
            return Success<Guid>(ResponseText.INVALID_MODULE_CODE);
        #endregion

        var moduleSql = new ModuleSql(moduleCode);
        string tableName = moduleSql.GetTableName();
        string json = entity.ToString();
        var moduleColumnInfo = new ModuleSqlColumn(moduleCode);
        var moduleColumns = moduleColumnInfo.GetModuleSqlFormColumn();

        var formColumns = moduleColumns.Select(x => x.DataIndex).ToList();
        var dict = ConvertToDic(json);
        dict = dict.Where(pair => formColumns.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);

        #region 检查数据格式
        CheckForm(moduleCode, dict);
        #endregion

        var id = Utility.GuidId;
        dict.Add("ID", id);
        dict.Add("CreatedTime", Utility.GetSysDate());
        dict.Add("CreatedBy", App.User.ID);
        dict.Add("ModificationNum", 0);
        dict.Add("GroupId", Utility.GetGroupId());
        dict.Add("CompanyId", Utility.GetCompanyId());
        await Db.Insertable(dict).AS(tableName).ExecuteCommandAsync();

        return Success(id, ResponseText.INSERT_SUCCESS);
    }

    /// <summary>
    /// 更新数据
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="id">主键ID</param>
    /// <param name="entity">数据</param>
    /// <returns></returns>
    public async Task<ServiceResult<Guid>> Update(string moduleCode, Guid id, object entity)
    {
        #region 判断模块是否存在
        var module = ModuleInfo.GetModuleInfo(moduleCode);
        if (module.IsNull())
            return ServiceResult<Guid>.OprateFailed(ResponseText.INVALID_MODULE_CODE);
        #endregion

        var moduleSql = new ModuleSql(moduleCode);
        string tableName = moduleSql.GetTableName();
        string json = entity.ToString();
        var moduleColumnInfo = new ModuleSqlColumn(moduleCode);
        var moduleColumns = moduleColumnInfo.GetModuleSqlFormColumn();

        var formColumns = moduleColumns.Select(x => x.DataIndex).ToList();
        var dict = ConvertToDic(json);
        dict = dict.Where(pair => formColumns.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);

        #region 检查是否存在相同值
        CheckForm(moduleCode, dict, OperateType.Update, id);
        #endregion

        dict.Add("UpdateTime", Utility.GetSysDate());
        dict.Add("UpdateBy", App.User.ID);

        await Db.Updateable(dict).AS(tableName).Where($"IsDeleted='false' AND ID='{id}'").ExecuteCommandAsync();

        #region 回写修改次数
        string sql = $"UPDATE {tableName} SET ModificationNum = isnull (ModificationNum, 0) + 1, Tag = 1 where ID='{id}'";
        await Db.Ado.ExecuteCommandAsync(sql);
        #endregion

        return Success(id, ResponseText.UPDATE_SUCCESS);
    }

    /// <summary>
    /// 删除数据
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="id">主键ID</param>
    /// <returns></returns>
    public async Task<ServiceResult> Delete(string moduleCode, Guid id) => await Delete(moduleCode, [id]);

    /// <summary>
    /// 批量删除数据
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="ids">主键ID集</param>
    /// <returns></returns>
    public async Task<ServiceResult> Delete(string moduleCode, List<Guid> ids)
    {
        #region 判断模块是否存在
        var module = ModuleInfo.GetModuleInfo(moduleCode);
        if (module.IsNull())
            return Failed(ResponseText.INVALID_MODULE_CODE);
        #endregion

        var moduleSql = new ModuleSql(moduleCode);
        string tableName = moduleSql.GetTableName();

        var dictList = new List<Dictionary<string, object>>();
        if (ids != null && ids.Any())
            for (int i = 0; i < ids.Count; i++)
            {
                dictList.Add(new Dictionary<string, object>
                {
                    { "UpdateTime", Utility.GetSysDate() },
                    { "UpdateBy", App.User.ID },
                    { "IsDeleted", true },
                    { "ID", ids[i] }
                });
            }

        await Db.Updateable(dictList).AS(tableName).WhereColumns("ID").ExecuteCommandAsync();

        return Success(ResponseText.DELETE_SUCCESS);
    }

    //public async Task<ServiceResult<List<ComboGridData>>> Test(string moduleCode, object entity)
    //{
    //    var module = ModuleInfo.GetModuleInfo(moduleCode);
    //    ModuleSql moduleSql = new ModuleSql(moduleCode);
    //    string tableName = moduleSql.GetTableName();
    //    string json = entity.ToString();
    //    var moduleColumnInfo = new ModuleSqlColumn(module.ModuleCode);
    //    var moduleColumns = moduleColumnInfo.GetModuleSqlColumn();

    //    //var entityType = Assembly.Load("EU.Core.Model").GetTypes().Where(a => a.Name == tableName).FirstOrDefault();
    //    //object instance1 = Activator.CreateInstance(entityType);

    //    //PropertyInfo propertyInfo = entityType.GetProperty(tableName);

    //    //if (propertyInfo != null)
    //    //{
    //    //    propertyInfo.SetValue(instance1, 123); 
    //    //} 
    //    //Dictionary<string, object> dict = JsonHelper.JsonToObj<Dictionary<string, object>>(json);
    //    //dict.Add("ID", Guid.NewGuid());
    //    //dict.Add("CreatedTime", Utility.GetSysDate());
    //    //Db.Insertable(dict).AS(tableName).ExecuteCommand();

    //    //var dic = JsonHelper.JsonToObj<Dictionary<string, object>>(json);
    //    //var lstColumns = dic.Keys.Where(x => x != "ID" && x != "Id").ToList();
    //    //lstColumns.Add("AuditStatus");

    //    //#region 检查是否存在相同值
    //    //CheckOnly(model);
    //    //#endregion

    //    List<Dictionary<string, object>> dict = JsonHelper.JsonToObj<List<Dictionary<string, object>>>(json);


    //    return ServiceResult<List<ComboGridData>>.OprateSuccess(null, ResponseText.INSERT_SUCCESS, 0);

    //}
    #endregion
}
