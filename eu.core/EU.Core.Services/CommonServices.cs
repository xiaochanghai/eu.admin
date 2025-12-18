/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* CommonServices.cs
*
* 功 能： 通用服务类，提供模块数据查询、导入导出、增删改查等通用功能
* 类 名： CommonServices
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2024/4/24 22:43:02  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Services;

/// <summary>
/// 通用服务类
/// 提供基于模块配置的动态数据查询、导入导出、增删改查等通用功能
/// </summary>
public partial class CommonServices : BaseServices<SmModules, SmModulesDto, InsertSmModulesInput, EditSmModulesInput>, ICommonServices
{
    private readonly IBaseRepository<SmModules> _dal;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dal">数据访问层仓储</param>
    public CommonServices(IBaseRepository<SmModules> dal)
    {
        _dal = dal;
        BaseDal = dal;
    }

    #region 自定义列模块数据返回
    /// <summary>
    /// 获取模块列表数据（支持分页、排序、搜索、父子关系过滤）
    /// </summary>
    /// <param name="paramData">查询参数JSON字符串（包含分页参数current、pageSize和查询条件）</param>
    /// <param name="moduleCode">模块代码，用于确定查询的模块</param>
    /// <param name="sorter">排序参数JSON字符串，格式：{"字段名":"ascend/descend"}</param>
    /// <param name="parentColumn">父级列名，用于树形结构查询</param>
    /// <param name="parentId">父级ID，用于树形结构查询</param>
    /// <returns>分页列表数据</returns>
    public async Task<GridListReturn> GetGridList(string paramData, string moduleCode, string sorter = "{}", string parentColumn = null, string parentId = null)
    {
        // 初始化分页参数
        int current = 1;
        int pageSize = 10;
        int total = 0;

        // 解析查询参数和排序参数
        var searchParam = ConvertToDic(paramData);
        var sorterParam = JsonHelper.JsonToObj<Dictionary<string, string>>(sorter);

        // 初始化查询条件（默认查询所有）
        string queryCondition = "1=1";
        string keyWord = string.Empty;

        #region 处理查询条件
        // 获取模块列配置信息
        var moduleColumnInfo = new ModuleSqlColumn(moduleCode);
        var moduleColumns = moduleColumnInfo.GetModuleSqlColumn();

        // 遍历查询参数，构建查询条件
        foreach (var item in searchParam)
        {
            // 提取分页参数
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
            // 跳过时间戳参数
            if (item.Key == "_timestamp")
            {
                continue;
            }
            // 提取关键字搜索参数
            if (item.Key == "keyWord")
            {
                keyWord = item.Value.ToString();
                continue;
            }
            // 处理其他查询条件（模糊匹配）
            if (!string.IsNullOrEmpty(item.Value.ToString()))
            {
                if (moduleColumns.Any())
                {
                    var column = moduleColumns.FirstOrDefault(a => a.DataIndex == item.Key);
                    if (column != null)
                        // 注意：此处存在SQL注入风险，建议使用参数化查询
                        queryCondition += " AND " + column.TableAlias + "." + item.Key + " like '%" + item.Value.ToString() + "%'";
                }
                else
                    queryCondition += " AND A." + item.Key + " like '%" + item.Value.ToString() + "%'";
            }
        }

        // 添加父子关系过滤条件
        if (!string.IsNullOrEmpty(parentId) && !string.IsNullOrEmpty(parentColumn))
            queryCondition += " AND A." + parentColumn + " = '" + parentId + "'";
        #endregion

        #region 处理关键字搜索
        string keyWordCondition = string.Empty;
        // 如果有关键字，则在所有允许搜索的字段中进行模糊匹配
        if (!string.IsNullOrEmpty(keyWord) && moduleColumns.Any())
            moduleColumns.ForEach(item =>
            {
                // 只对文本类型且未隐藏搜索的字段进行关键字搜索
                if (item.ValueType == null && item.HideInSearch == false)
                {
                    string tableAlias = item.TableAlias;
                    string dataIndex = item.DataIndex;
                    if (string.IsNullOrEmpty(keyWordCondition))
                        keyWordCondition = tableAlias + "." + dataIndex + " LIKE '%" + keyWord + "%'";
                    else
                        keyWordCondition += " OR " + tableAlias + "." + dataIndex + " LIKE '%" + keyWord + "%'";
                }
            });
        #endregion

        // 获取模块SQL配置
        string userId = string.Empty;
        var moduleSql = new ModuleSql(moduleCode);
        var grid = new GridList();
        string tableName = moduleSql.GetTableName();
        string fullSql = moduleSql.GetFullSql();
        string sqlSelectBrwAndTable = moduleSql.GetSqlSelectBrwAndTable();
        string sqlSelectAndTable = moduleSql.GetSqlSelectAndTable();

        // 格式化SQL语句中的表名占位符
        if (!string.IsNullOrEmpty(tableName))
        {
            sqlSelectBrwAndTable = string.Format(sqlSelectBrwAndTable, tableName);
            sqlSelectAndTable = string.Format(sqlSelectAndTable, tableName);
        }

        // 获取默认查询条件
        string sqlDefaultCondition = moduleSql.GetSqlDefaultCondition();

        #region 添加关键字搜索条件
        if (!string.IsNullOrEmpty(keyWordCondition))
            sqlDefaultCondition += $" AND ({keyWordCondition})";
        #endregion

        // 获取默认排序配置
        string defaultSortField = moduleSql.GetDefaultSortField();
        string defaultSortDirection = moduleSql.GetDefaultSortDirection();
        if (string.IsNullOrEmpty(defaultSortDirection))
        {
            defaultSortDirection = "ASC";
        }

        // 设置网格查询参数
        grid.FullSql = fullSql;
        grid.SqlSelect = sqlSelectBrwAndTable;
        grid.SqlDefaultCondition = sqlDefaultCondition;
        grid.SqlQueryCondition = queryCondition;
        grid.SortField = defaultSortField;
        grid.SortDirection = defaultSortDirection;

        #region 处理自定义排序
        // 如果传入了排序参数，则覆盖默认排序
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

        // 设置分页参数并执行查询
        grid.PageSize = pageSize;
        grid.CurrentPage = current;
        grid.ModuleCode = moduleCode;

        // 获取总记录数
        total = grid.GetTotalCount();

        // 生成查询SQL并执行
        string sql = grid.GetQueryString();
        var dataTableTemp = await Db.Ado.GetDataTableAsync(sql);

        // 格式化树形结构数据
        var dataTable = Utility.FormatDataTableForTree(moduleCode, userId, dataTableTemp);

        return new GridListReturn(pageSize, current, total, dataTable, ResponseText.QUERY_SUCCESS);
    }

    /// <summary>
    /// 根据筛选条件查询模块数据（支持分页、排序、关键字搜索、自定义条件）
    /// </summary>
    /// <param name="filter">筛选条件对象（包含分页、排序、查询参数等）</param>
    /// <param name="moduleCode">模块代码</param>
    /// <returns>分页列表数据</returns>
    public async Task<GridListReturn> QueryByFilter(QueryFilter filter, string moduleCode)
    {
        int total = 0;

        // 初始化查询条件
        string queryCondition = "1=1";
        string keyWord = string.Empty;

        #region 处理查询参数
        var moduleColumnInfo = new ModuleSqlColumn(moduleCode);
        var moduleColumns = moduleColumnInfo.GetModuleSqlColumn();

        // 遍历过滤器中的参数
        if (filter.@params != null)
            foreach (var item in filter.@params)
            {
                // 提取关键字参数
                if (item.Key == "keyWord")
                {
                    keyWord = item.Value.ObjToString();
                    continue;
                }
                // 处理其他查询条件（模糊匹配）
                if (!string.IsNullOrEmpty(item.Value.ObjToString()))
                {
                    if (moduleColumns.Any())
                    {
                        var column = moduleColumns.FirstOrDefault(a => a.DataIndex == item.Key);
                        if (column != null)
                            // 注意：此处存在SQL注入风险，建议使用参数化查询
                            queryCondition += " AND " + column.TableAlias + "." + item.Key + " like '%" + item.Value.ObjToString() + "%'";
                    }
                    else
                        queryCondition += " AND A." + item.Key + " like '%" + item.Value.ObjToString() + "%'";
                }
            }

        // 添加自定义查询条件
        if (filter.Conditions.IsNotEmptyOrNull())
            queryCondition += " AND " + filter.Conditions;
        #endregion

        #region 处理关键字搜索
        var keyWordCondition = string.Empty;
        // 如果有关键字，则在所有允许搜索的字段中进行模糊匹配
        if (keyWord.IsNotEmptyOrNull() && moduleColumns.Any())
            moduleColumns.ForEach(item =>
            {
                // 只对文本类型且未隐藏搜索的字段进行关键字搜索
                if (item.ValueType == null && item.HideInSearch == false)
                {
                    var tableAlias = item.TableAlias;
                    var dataIndex = item.DataIndex;
                    if (string.IsNullOrEmpty(keyWordCondition))
                        keyWordCondition = tableAlias + "." + dataIndex + " LIKE '%" + keyWord + "%'";
                    else
                        keyWordCondition += " OR " + tableAlias + "." + dataIndex + " LIKE '%" + keyWord + "%'";
                }
            });
        #endregion

        // 获取模块SQL配置
        var userId = string.Empty;
        ModuleSql moduleSql = new(moduleCode);
        GridList grid = new();
        var tableName = moduleSql.GetTableName();
        var fullSql = moduleSql.GetFullSql();
        var sqlSelectBrwAndTable = moduleSql.GetSqlSelectBrwAndTable();
        var sqlSelectAndTable = moduleSql.GetSqlSelectAndTable();

        // 格式化SQL语句中的表名占位符
        if (tableName.IsNotEmptyOrNull())
        {
            sqlSelectBrwAndTable = string.Format(sqlSelectBrwAndTable, tableName);
            sqlSelectAndTable = string.Format(sqlSelectAndTable, tableName);
        }

        var sqlDefaultCondition = moduleSql.GetSqlDefaultCondition();

        #region 添加关键字搜索条件
        if (keyWordCondition.IsNotEmptyOrNull())
            sqlDefaultCondition += " AND (" + keyWordCondition + ")";
        #endregion

        // 获取默认排序配置
        var defaultSortField = moduleSql.GetDefaultSortField();
        var defaultSortDirection = moduleSql.GetDefaultSortDirection();
        if (defaultSortDirection.IsNullOrEmpty())
            defaultSortDirection = "ASC";

        // 设置网格查询参数
        grid.FullSql = fullSql;
        grid.SqlSelect = sqlSelectBrwAndTable;
        grid.SqlDefaultCondition = sqlDefaultCondition;
        grid.SqlQueryCondition = queryCondition;
        grid.SortField = defaultSortField;
        grid.SortDirection = defaultSortDirection;

        #region 处理自定义排序
        // 如果传入了排序参数，则覆盖默认排序
        if (filter.sorter != null && filter.sorter.Count > 0)
            foreach (var item in filter.sorter)
            {
                grid.SortField = item.Key;
                grid.SortDirection = item.Value switch
                {
                    "ascend" => "ASC",
                    _ => "DESC"
                };
            }
        #endregion

        // 设置分页参数并执行查询
        grid.PageSize = filter.PageSize;
        grid.CurrentPage = filter.PageIndex;
        grid.ModuleCode = moduleCode;

        // 获取总记录数
        total = grid.GetTotalCount();

        // 生成查询SQL并执行
        string sql = grid.GetQueryString();
        var dataTableTemp = await Db.Ado.GetDataTableAsync(sql);

        // 格式化树形结构数据
        var dataTable = Utility.FormatDataTableForTree(moduleCode, userId, dataTableTemp);

        return new GridListReturn(filter.PageSize, filter.PageIndex, total, dataTable, ResponseText.QUERY_SUCCESS);
    }

    #endregion

    #region Excel导出
    /// <summary>
    /// 导出Excel文件
    /// </summary>
    /// <param name="filter">查询筛选条件（包含查询参数、排序、自定义条件等）</param>
    /// <param name="moduleCode">模块代码</param>
    /// <returns>返回文件ID，用于下载Excel文件</returns>
    public async Task<ServiceResult<string>> ExportExcelAsync(QueryFilter filter, string moduleCode)
    {
        // 初始化分页参数（导出时不分页，设置一个大数值）
        int current = 1;
        string queryCondition = "1=1 ";
        string defaultCondition = string.Empty;
        int pageCount = 999999999; // 不分页，导出所有数据
        int totalCount;
        int outPageSize;
        string fileId = Utility.GuidId1;
        string keyWord = string.Empty;

        #region 处理查询条件
        var moduleColumnInfo = new ModuleSqlColumn(moduleCode);
        var moduleColumns = moduleColumnInfo.GetModuleSqlColumn();

        // 遍历查询参数，构建查询条件
        foreach (var item in filter.@params)
        {
            // 提取关键字参数
            if (item.Key == "keyWord")
            {
                keyWord = item.Value.ObjToString();
                continue;
            }
            // 处理其他查询条件（模糊匹配）
            if (!string.IsNullOrEmpty(item.Value.ObjToString()))
            {
                if (moduleColumns.Any())
                {
                    var column = moduleColumns.FirstOrDefault(a => a.DataIndex == item.Key);
                    if (column != null)
                        // 注意：此处存在SQL注入风险，建议使用参数化查询
                        queryCondition += " AND " + column.TableAlias + "." + item.Key + " like '%" + item.Value.ObjToString() + "%'";
                }
                else
                    queryCondition += " AND A." + item.Key + " like '%" + item.Value.ObjToString() + "%'";
            }
        }

        // 添加自定义查询条件
        if (filter.Conditions.IsNotEmptyOrNull())
            queryCondition += " AND " + filter.Conditions;
        #endregion

        #region 处理模块信息
        SmModules module = ModuleInfo.GetModuleInfo(moduleCode);
        ModuleSql moduleSql = new ModuleSql(moduleCode);
        ModuleSqlColumn moduleSqlColumn = new ModuleSqlColumn(moduleCode);
        #endregion

        #region 处理默认排序
        string defaultSortField = moduleSql.GetDefaultSortField();
        string defaultSortDirection = moduleSql.GetDefaultSortDirection();
        #endregion

        // 生成查询SQL
        string sql = moduleSql.GetCurrentSql(moduleCode, current, pageCount, defaultSortField, defaultSortDirection, defaultCondition, queryCondition, out totalCount, out outPageSize);

        // 获取导出列配置
        string moduleColumnsForExport = moduleSqlColumn.GetExportExcelColumns();

        // 构建最终的导出SQL
        string excelSql = "SELECT " + moduleColumnsForExport + " FROM (" + sql + ") A";

        // 生成文件信息
        string tableName = module.ModuleName;
        string fileName = $"{tableName}_{Utility.GetSysDate().ToSecondString1()}.xlsx";
        string folder = Utility.GetSysDate().ToString("yyyyMMdd");
        string filePath = $"/Download/ExcelExport/{folder}/";
        string savePath = "wwwroot" + filePath;

        // 创建保存目录
        FileHelper.CreateDirectory(savePath);

        // 执行SQL查询并生成Excel
        var dataTable = DBHelper.GetDataTable(excelSql);

        // 设置列标题（中文列名）
        foreach (DataColumn column in dataTable.Columns)
            column.Caption = moduleSqlColumn.GetExportExcelColumnRenderer(column.ColumnName);

        // 生成Excel文件
        NPOIHelper.ExportExcel(dataTable, tableName, savePath + fileName);

        #region 保存文件附件信息到数据库
        var insertFileAttachment = new DbInsert("FileAttachment");
        insertFileAttachment.IsInitRowId = false;
        insertFileAttachment.Values("ID", fileId);
        insertFileAttachment.Values("OriginalFileName", fileName);
        insertFileAttachment.Values("FileName", fileName);
        insertFileAttachment.Values("FileExt", "xlsx");
        insertFileAttachment.Values("Path", filePath);
        insertFileAttachment.Values("ImageType", "ExcelExport");
        await Db.Ado.ExecuteCommandAsync(insertFileAttachment.GetSql());
        #endregion

        #region 记录模块操作日志
        try
        {
            // TODO: 记录导出操作日志
            // DBHelper.RecordOperateLog(User.Identity.Name, moduleCode, moduleSql.GetTableName(), "", OperateType.Excel, savePath + fileName);
        }
        catch
        {
            // 日志记录失败不影响导出功能
        }
        #endregion

        return Success(fileId, "导出成功！");
    }

    #endregion

    #region Excel导入
    /// <summary>
    /// 导入Excel文件
    /// </summary>
    /// <param name="import">导入表单对象（包含上传的文件）</param>
    /// <param name="moduleCode">模块代码</param>
    /// <returns>返回导入结果，包含导入数据、错误列表等</returns>
    public async Task<ServiceResult<ImportExcelResult>> ImportExcelAsync(ImportExcelForm import, string moduleCode)
    {
        string message = string.Empty;
        var importDataId = Utility.GuidId;
        var result = new ImportExcelResult();

        try
        {
            result.ImportDataId = importDataId;

            // 验证模块是否存在
            var module = ModuleInfo.GetModuleInfo(moduleCode);
            if (module == null)
                return ServiceResult<ImportExcelResult>.OprateFailed($"模块代码【{moduleCode}】不存在！");

            // 查询导入模板配置
            var template = await Db.Queryable<SmImpTemplate>().Where(x => x.ModuleId == module.ID).FirstAsync();

            // 获取文件扩展名
            var ext = string.Empty;
            if (import.file.FileName.IsNotEmptyOrNull())
            {
                var dotPos = import.file.FileName.LastIndexOf('.');
                ext = import.file.FileName.Substring(dotPos + 1);
            }

            // 构建文件保存路径
            string filePath = $"/ImportExcel/{DateTime.Now:yyyyMMdd}/{Utility.SnowID()}";
            FileHelper.CreateRootDirectory(filePath);

            // 保存上传的文件
            var filepath = Path.Combine(filePath, import.fileName);
            using (var stream = File.Create(FileHelper.GetPhysicsPath() + filepath))
            {
                await import.file.CopyToAsync(stream);
            }

            // 保存文件附件信息到数据库
            var fileAttachment = new FileAttachment
            {
                OriginalFileName = import.file.FileName,
                FileName = import.file.FileName,
                FileExt = ext,
                Length = import.file.Length,
                Path = filePath,
                ImageType = "ImportExcel"
            };
            await Db.Insertable(fileAttachment).ExecuteCommandAsync();

            // 验证导入模板配置
            var impTemplate = await Db.Queryable<SmImpTemplate>().Where(x => x.ModuleId == module.ID).FirstAsync();
            if (impTemplate == null)
                return ServiceResult<ImportExcelResult>.OprateFailed($"请配置模块【{module.ModuleName}】的导入模板，详情请联系客服！");

            string sheetName = impTemplate.SheetName;

            // 读取Excel文件数据
            var dataTable = NPOIHelper.ImportExcel(filepath, sheetName, template?.StartRow ?? 0);

            // 如果有数据，则执行导入处理
            if (dataTable.Rows.Count > 0)
                await ImportHelper.ImportData(Db, impTemplate, importDataId, filePath, import.fileName, dataTable);

            // 构建返回结果
            var importColumns = new List<string>();
            var importColumnNames = new List<string> { "行号" };

            var dtImportData = await ImportHelper.GetImportDataDetailList(Db, importDataId, impTemplate.ID);

            for (int i = 0; i < dtImportData.Columns.Count; i++)
                importColumns.Add(dtImportData.Columns[i].ColumnName);

            for (int i = 0; i < dataTable.Columns.Count; i++)
                importColumnNames.Add(dataTable.Columns[i].ColumnName);

            result.ImportColumns = importColumns;
            result.ImportColumnNames = importColumnNames;
            result.ImportList = dtImportData;
            result.Template = new
            {
                impTemplate.TemplateCode,
                moduleCode,
                impTemplate.TemplateName,
                impTemplate.IsAllowOverride
            };
        }
        catch (Exception ex)
        {
            message = ex.Message;
            result.ErrorList = await ImportHelper.GetImportErrorList(Db, importDataId);
        }

        return Success(result, message);
    }

    #endregion

    #region 获取Excel导入结果
    /// <summary>
    /// 查询Excel导入结果（用于查看已导入但未转换的数据）
    /// </summary>
    /// <param name="importDataId">导入数据ID</param>
    /// <param name="templateId">模板ID</param>
    /// <returns>返回导入结果，包含导入数据、错误列表等</returns>
    public async Task<ServiceResult<ImportExcelResult>> QueryImportExcelResultAsync(Guid importDataId, Guid templateId)
    {
        string message = string.Empty;
        var result = new ImportExcelResult();

        try
        {
            result.ImportDataId = importDataId;
            var importColumns = new List<string>();
            var importColumnNames = new List<string> { "行号" };

            // 查询导入模板配置
            var template = await Db.Queryable<SmImpTemplate>().Where(x => x.ID == templateId).FirstAsync();

            // 查询导入文件名
            string sql = $"SELECT ImportFileName FROM SmImportData WHERE ID='{importDataId}'";
            var importFileName = DBHelper.ExecuteScalar(sql);

            // 获取导入数据明细列表
            var dtImportData = await ImportHelper.GetImportDataDetailList(Db, importDataId, templateId);

            // 重新读取Excel文件（用于获取原始列名）
            var dataTable = NPOIHelper.ImportExcel(importFileName.ObjToString(), template.SheetName, template.StartRow.Value);

            // 构建列信息
            for (int i = 0; i < dtImportData.Columns.Count; i++)
                importColumns.Add(dtImportData.Columns[i].ColumnName);

            for (int i = 0; i < dataTable.Columns.Count; i++)
                importColumnNames.Add(dataTable.Columns[i].ColumnName);

            result.ImportColumns = importColumns;
            result.ImportColumnNames = importColumnNames;
            result.ImportList = dtImportData;
            result.ImportMasterList = await ImportHelper.GetImportDataMasterList(Db, importDataId, templateId);

            var moduleCode = ModuleInfo.GetModuleCodeById(template.ModuleId);

            result.Template = new
            {
                template.TemplateCode,
                moduleCode,
                template.TemplateName,
                template.IsAllowOverride
            };
            result.ErrorList = await ImportHelper.GetImportErrorList(Db, importDataId);
        }
        catch (Exception ex)
        {
            message = ex.Message;
        }

        return Success(result, message);
    }

    #endregion

    #region Excel导入数据转换
    /// <summary>
    /// 将导入的Excel数据转换为业务数据（写入实际业务表）
    /// </summary>
    /// <param name="request">转换请求对象</param>
    /// <returns>转换结果</returns>
    public async Task<ServiceResult> TransferExcelData(TransferExcelRequest request)
    {
        var importDataId = request.ImportDataId;
        string importTemplateCode = request.ImportTemplateCode;
        string type = request.Type;
        string masterId = request.MasterId;

        // 执行数据转换
        await ImportHelper.TransferData(Db, importDataId, importTemplateCode, UserId1, false);

        // 执行导入后处理（如触发业务逻辑、更新关联数据等）
        await ImportHelper.AfterImport(Db, importTemplateCode, importDataId, masterId);

        return Success("导入成功！");
    }

    #endregion

    #region 清空缓存
    /// <summary>
    /// 清空系统缓存
    /// </summary>
    /// <returns>操作结果</returns>
    public ServiceResult ClearCache()
    {
        Utility.ReInitCache();
        return Success(ResponseText.EXECUTE_SUCCESS);
    }
    #endregion

    #region 获取通用下拉数据
    /// <summary>
    /// 获取通用下拉列表数据（旧版本，建议使用GetComboGridData）
    /// </summary>
    /// <param name="parentColumn">父级列名</param>
    /// <param name="parentId">父级ID</param>
    /// <param name="current">当前页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="code">数据字典代码</param>
    /// <param name="items">选中项</param>
    /// <param name="key">搜索关键字</param>
    /// <returns>下拉列表数据</returns>
    public async Task<ServiceResult<List<ComboGridData>>> ComboGridData(string parentColumn, string parentId, int? current, int? pageSize, string code, string[] items, string key)
    {
        var data = new List<ComboGridData>();

        // 获取数据字典对应的SQL
        var sql = LovHelper.GetCommonListSql(code);
        if (!string.IsNullOrWhiteSpace(sql))
        {
            // 添加父子关系过滤
            if (!string.IsNullOrWhiteSpace(parentColumn) && !string.IsNullOrWhiteSpace(parentId))
                sql += $" AND {parentColumn} = '{parentId}'";

            sql = @$"SELECT * FROM ({sql}) A";

            // 添加关键字搜索
            if (!string.IsNullOrWhiteSpace(key))
                sql += $" WHERE label LIKE '%{key}%'";

            data = await Db.Ado.SqlQueryAsync<ComboGridData>(sql);
        }

        return ServiceResult<List<ComboGridData>>.OprateSuccess(data, ResponseText.QUERY_SUCCESS, data.Count);
    }

    /// <summary>
    /// 获取通用下拉列表数据（新版本，推荐使用）
    /// </summary>
    /// <param name="body">查询参数对象</param>
    /// <returns>下拉列表数据</returns>
    public async Task<ServiceResult<List<ComboGridData>>> GetComboGridData(ComboGridDataBody body)
    {
        var data = new List<ComboGridData>();

        // 获取数据字典对应的SQL
        var sql = LovHelper.GetCommonListSql(body.code);
        if (!string.IsNullOrWhiteSpace(sql))
        {
            // 添加父子关系过滤
            if (!string.IsNullOrWhiteSpace(body.parentColumn) && !string.IsNullOrWhiteSpace(body.parentId))
                sql += $" AND {body.parentColumn} = '{body.parentId}'";

            sql = @$"SELECT * FROM ({sql}) A";

            // 添加关键字搜索
            if (!string.IsNullOrWhiteSpace(body.key))
                sql += $" WHERE label LIKE '%{body.key}%'";

            data = await Db.Ado.SqlQueryAsync<ComboGridData>(sql);
        }

        return ServiceResult<List<ComboGridData>>.OprateSuccess(data, ResponseText.QUERY_SUCCESS, data.Count);
    }
    #endregion

    #region 增删查改

    /// <summary>
    /// 根据主键ID查询模块数据
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="id">主键ID</param>
    /// <returns>数据对象</returns>
    public async Task<ServiceResult<object>> Query(string moduleCode, Guid id)
    {
        #region 验证模块是否存在
        var module = ModuleInfo.GetModuleInfo(moduleCode);
        if (module.IsNull())
            return ServiceResult<object>.OprateFailed(ResponseText.INVALID_MODULE_CODE);
        #endregion

        // 获取表名并查询数据
        var moduleSql = new ModuleSql(moduleCode);
        string tableName = moduleSql.GetTableName();
        var isDeleted = false;

        var data = await Db.Queryable<dynamic>().AS(tableName).Where("ID=@id AND IsDeleted=@isDeleted", new { id, isDeleted }).FirstAsync();

        return Success<object>(data, ResponseText.QUERY_SUCCESS);
    }

    /// <summary>
    /// 新增模块数据
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="entity">数据对象</param>
    /// <returns>新增记录的ID</returns>
    public async Task<ServiceResult<Guid>> Add(string moduleCode, object entity)
    {
        #region 验证模块是否存在
        var module = ModuleInfo.GetModuleInfo(moduleCode);
        if (module.IsNull())
            return Success<Guid>(ResponseText.INVALID_MODULE_CODE);
        #endregion

        // 获取模块表名和表单列配置
        var moduleSql = new ModuleSql(moduleCode);
        string tableName = moduleSql.GetTableName();
        string json = entity.ToString();
        var moduleColumnInfo = new ModuleSqlColumn(moduleCode);
        var moduleColumns = moduleColumnInfo.GetModuleSqlFormColumn();

        // 过滤出表单中配置的字段
        var formColumns = moduleColumns.Select(x => x.DataIndex).ToList();
        var dict = ConvertToDic(json);
        dict = dict.Where(pair => formColumns.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);

        #region 数据格式校验
        CheckForm(moduleCode, dict);
        #endregion

        // 添加系统字段
        var id = Utility.GuidId;
        dict.Add("ID", id);
        dict.Add("CreatedTime", Utility.GetSysDate());
        dict.Add("CreatedBy", App.User.ID);
        dict.Add("ModificationNum", 0);
        dict.Add("GroupId", Utility.GetGroupId());
        dict.Add("CompanyId", Utility.GetCompanyId());

        // 执行插入操作
        await Db.Insertable(dict).AS(tableName).ExecuteCommandAsync();

        return Success(id, ResponseText.INSERT_SUCCESS);
    }

    /// <summary>
    /// 更新模块数据
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="id">主键ID</param>
    /// <param name="entity">数据对象</param>
    /// <returns>更新记录的ID</returns>
    public async Task<ServiceResult<Guid>> Update(string moduleCode, Guid id, object entity)
    {
        #region 验证模块是否存在
        var module = ModuleInfo.GetModuleInfo(moduleCode);
        if (module.IsNull())
            return ServiceResult<Guid>.OprateFailed(ResponseText.INVALID_MODULE_CODE);
        #endregion

        // 获取模块表名和表单列配置
        var moduleSql = new ModuleSql(moduleCode);
        string tableName = moduleSql.GetTableName();
        string json = entity.ToString();
        var moduleColumnInfo = new ModuleSqlColumn(moduleCode);
        var moduleColumns = moduleColumnInfo.GetModuleSqlFormColumn();

        // 过滤出表单中配置的字段
        var formColumns = moduleColumns.Select(x => x.DataIndex).ToList();
        var dict = ConvertToDic(json);
        dict = dict.Where(pair => formColumns.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);

        #region 数据唯一性校验
        CheckForm(moduleCode, dict, OperateType.Update, id);
        #endregion

        // 添加更新时间和更新人
        dict.Add("UpdateTime", Utility.GetSysDate());
        dict.Add("UpdateBy", App.User.ID);

        // 执行更新操作
        await Db.Updateable(dict).AS(tableName).Where($"IsDeleted='false' AND ID='{id}'").ExecuteCommandAsync();

        #region 回写修改次数
        string sql = $"UPDATE {tableName} SET ModificationNum = isnull(ModificationNum, 0) + 1, Tag = 1 WHERE ID='{id}'";
        await Db.Ado.ExecuteCommandAsync(sql);
        #endregion

        return Success(id, ResponseText.UPDATE_SUCCESS);
    }

    /// <summary>
    /// 删除模块数据（单条）
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="id">主键ID</param>
    /// <returns>操作结果</returns>
    public async Task<ServiceResult> Delete(string moduleCode, Guid id) => await Delete(moduleCode, [id]);

    /// <summary>
    /// 批量删除模块数据（逻辑删除，标记IsDeleted=true）
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="ids">主键ID集合</param>
    /// <returns>操作结果</returns>
    public async Task<ServiceResult> Delete(string moduleCode, List<Guid> ids)
    {
        #region 验证模块是否存在
        var module = ModuleInfo.GetModuleInfo(moduleCode);
        if (module.IsNull())
            return Failed(ResponseText.INVALID_MODULE_CODE);
        #endregion

        // 获取表名
        var moduleSql = new ModuleSql(moduleCode);
        string tableName = moduleSql.GetTableName();

        // 构建批量更新数据（逻辑删除）
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

        // 执行批量更新
        await Db.Updateable(dictList).AS(tableName).WhereColumns("ID").ExecuteCommandAsync();

        return Success(ResponseText.DELETE_SUCCESS);
    }

    #endregion
}
