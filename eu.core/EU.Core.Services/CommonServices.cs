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
    #region 常量定义
    private const int EXPORT_MAX_ROWS = 1000000; // 导出最大行数
    private const string DEFAULT_SORT_DIRECTION = "ASC";
    private const string DEFAULT_TABLE_ALIAS = "A";
    private const string ROW_NUMBER_COLUMN = "行号";
    #endregion

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
    /// 根据筛选条件查询模块数据（支持分页、排序、关键字搜索、自定义条件）
    /// </summary>
    /// <param name="filter">筛选条件对象（包含分页、排序、查询参数等）</param>
    /// <param name="moduleCode">模块代码</param>
    /// <returns>分页列表数据</returns>
    public async Task<GridListReturn> QueryByFilter(QueryFilter filter, string moduleCode)
    {
        var module = ModuleInfo.GetModuleInfo(moduleCode);
        if (module is null)
            return new GridListReturn(filter.PageSize, filter.PageIndex, 0, null, ResponseText.QUERY_SUCCESS);

        // 获取模块列配置信息
        var moduleColumnInfo = new ModuleSqlColumn(moduleCode);
        var moduleColumns = moduleColumnInfo.GetModuleSqlColumn();

        // 提取关键字参数
        var keyWord = filter.@params?.FirstOrDefault(x => x.Key == "keyWord").Value?.ObjToString() ?? string.Empty;

        // 构建查询条件
        var queryCondition = BuildQueryCondition(filter.@params ?? new Dictionary<string, object>(), moduleColumns);

        // 添加自定义查询条件
        if (filter.Conditions.IsNotEmptyOrNull())
            queryCondition += " AND " + filter.Conditions;

        // 构建关键字搜索条件
        var keyWordCondition = BuildKeyWordCondition(keyWord, moduleColumns);

        // 获取模块SQL配置
        var userId = Utility.GetUserIdString();
        var moduleSql = new ModuleSql(moduleCode, Db);
        var grid = new GridList(Db);
        var tableName = moduleSql.GetTableName();
        var sqlSelectBrwAndTable = moduleSql.GetSqlSelectBrwAndTable();

        // 格式化SQL语句中的表名占位符
        if (tableName.IsNotEmptyOrNull())
            sqlSelectBrwAndTable = string.Format(sqlSelectBrwAndTable, tableName);

        if (sqlSelectBrwAndTable.IsNotEmptyOrNull() && sqlSelectBrwAndTable.Contains("[USER_ID]"))
            sqlSelectBrwAndTable = sqlSelectBrwAndTable.Replace("[USER_ID]", userId);

        // 获取默认查询条件并添加关键字搜索
        var sqlDefaultCondition = moduleSql.GetSqlDefaultCondition();
        if (keyWordCondition.IsNotEmptyOrNull())
            sqlDefaultCondition += " AND (" + keyWordCondition + ")";

        if (sqlDefaultCondition.IsNotEmptyOrNull() && sqlDefaultCondition.Contains("[USER_ID]"))
            sqlDefaultCondition = sqlDefaultCondition.Replace("[USER_ID]", userId);

        var userType = App.User?.UserInfo?.UserType;
        if (userType != "Admin")
            sqlDefaultCondition = await AppendCompanyScopeConditionAsync(sqlDefaultCondition, module.IsRoleDataScope == true, "A.CompanyId");

        // 设置网格查询参数
        grid.FullSql = moduleSql.GetFullSql();
        grid.SqlSelect = sqlSelectBrwAndTable;
        grid.SqlDefaultCondition = sqlDefaultCondition;
        grid.SqlQueryCondition = queryCondition;
        grid.ModuleCode = moduleCode;
        grid.PageSize = filter.PageSize;
        grid.CurrentPage = filter.PageIndex;

        // 应用排序
        ApplySorting(grid, filter.sorter, moduleSql.GetDefaultSortField(), moduleSql.GetDefaultSortDirection());

        // 执行查询
        var total = grid.GetTotalCount();
        var sql = grid.GetQueryString();
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
        var fileId = Utility.GuidId1;

        try
        {
            // 验证模块是否存在
            var module = ValidateModule(moduleCode);

            // 获取模块列配置信息
            var moduleColumnInfo = new ModuleSqlColumn(moduleCode);
            var moduleColumns = moduleColumnInfo.GetModuleSqlColumn();

            // 提取关键字参数
            var keyWord = filter.@params?.FirstOrDefault(x => x.Key == "keyWord").Value?.ObjToString() ?? string.Empty;

            // 构建查询条件
            var queryCondition = BuildQueryCondition(filter.@params ?? new Dictionary<string, object>(), moduleColumns);

            // 添加自定义查询条件
            if (filter.Conditions.IsNotEmptyOrNull())
                queryCondition += " AND " + filter.Conditions;

            // 获取模块SQL配置
            var moduleSql = new ModuleSql(moduleCode, Db);

            // 获取默认排序配置
            var defaultSortField = moduleSql.GetDefaultSortField();
            var defaultSortDirection = moduleSql.GetDefaultSortDirection();

            // 生成查询SQL（不分页，导出所有数据，但限制最大行数）
            var sql = moduleSql.GetCurrentSql(moduleCode, 1, EXPORT_MAX_ROWS, defaultSortField, defaultSortDirection,
                string.Empty, queryCondition, out var totalCount, out _);

            // 获取导出列配置
            var moduleColumnsForExport = moduleColumnInfo.GetExportExcelColumns();

            // 构建最终的导出SQL
            var excelSql = $"SELECT {moduleColumnsForExport} FROM ({sql}) A";

            // 执行SQL查询并生成Excel
            var dataTable = DBHelper.GetDataTable(excelSql);

            // 设置列标题（中文列名）
            foreach (DataColumn column in dataTable.Columns)
                column.Caption = moduleColumnInfo.GetExportExcelColumnRenderer(column.ColumnName);

            // 生成文件信息
            var tableName = module.ModuleName;
            var fileName = $"{tableName}_{Utility.GetSysDate().ToSecondString1()}.xlsx";
            var folder = Utility.GetSysDate().ToString("yyyyMMdd");
            var filePath = $"/Download/ExcelExport/{folder}/";
            var savePath = "wwwroot" + filePath;

            // 创建保存目录
            FileHelper.CreateDirectory(savePath);

            // 生成Excel文件
            NPOIHelper.ExportExcel(dataTable, tableName, savePath + fileName);

            // 保存文件附件信息到数据库
            var insertFileAttachment = new DbInsert("FileAttachment");
            insertFileAttachment.IsInitRowId = false;
            insertFileAttachment.Values("ID", fileId);
            insertFileAttachment.Values("OriginalFileName", fileName);
            insertFileAttachment.Values("FileName", fileName);
            insertFileAttachment.Values("FileExt", "xlsx");
            insertFileAttachment.Values("Path", filePath);
            insertFileAttachment.Values("ImageType", "ExcelExport");
            await Db.Ado.ExecuteCommandAsync(insertFileAttachment.GetSql());

            return Success(fileId, "导出成功！");
        }
        catch (Exception ex)
        {
            // 记录错误日志
            return Failed<string>($"导出失败：{ex.Message}");
        }
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
        var importDataId = Utility.GuidId;
        var result = new ImportExcelResult { ImportDataId = importDataId };

        try
        {
            // 验证模块是否存在
            var module = ValidateModule(moduleCode);

            // 查询导入模板配置
            var impTemplate = await Db.Queryable<SmImpTemplate>().Where(x => x.ModuleId == module.ID).FirstAsync();
            if (impTemplate == null)
                return ServiceResult<ImportExcelResult>.OprateFailed($"请配置模块【{module.ModuleName}】的导入模板，详情请联系客服！");

            // 获取文件扩展名
            var ext = string.Empty;
            if (import.file.FileName.IsNotEmptyOrNull())
            {
                var dotPos = import.file.FileName.LastIndexOf('.');
                ext = import.file.FileName.Substring(dotPos + 1);
            }

            // 构建文件保存路径
            var filePath = $"/ImportExcel/{DateTime.Now:yyyyMMdd}/{Utility.SnowID()}";
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

            // 读取Excel文件数据
            var dataTable = NPOIHelper.ImportExcel(filepath, impTemplate.SheetName, impTemplate.StartRow ?? 0);

            // 如果有数据，则执行导入处理
            if (dataTable.Rows.Count > 0)
                await ImportHelper.ImportData(Db, impTemplate, importDataId, filePath, import.fileName, dataTable);

            // 构建返回结果
            var importColumns = new List<string>();
            var importColumnNames = new List<string> { ROW_NUMBER_COLUMN };

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

            // 获取错误列表
            result.ErrorList = await ImportHelper.GetImportErrorList(Db, importDataId);

            // 如果有错误，返回带有错误信息的成功结果（允许部分成功）
            var message = result.ErrorList?.Count > 0
                ? $"导入完成，但有 {result.ErrorList.Count} 条数据存在错误"
                : "导入成功！";

            return Success(result, message);
        }
        catch (Exception ex)
        {
            // 尝试获取错误列表
            try
            {
                result.ErrorList = await ImportHelper.GetImportErrorList(Db, importDataId);
            }
            catch
            {
                // 忽略获取错误列表时的异常
            }

            return ServiceResult<ImportExcelResult>.OprateFailed($"导入失败：{ex.Message}", result);
        }
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
    public async Task<ServiceResult> ClearCache()
    {
        await Utility.ReInitCache(Db);
        return Success(ResponseText.EXECUTE_SUCCESS);
    }
    #endregion

    #region 获取通用下拉数据
    /// <summary>
    /// 获取通用下拉列表数据（旧版本，建议使用GetComboGridData）
    /// </summary>
    /// <param name="parentColumn">父级列名</param>
    /// <param name="parentId">父级ID</param>
    /// <param name="current">当前页码（已废弃）</param>
    /// <param name="pageSize">每页大小（已废弃）</param>
    /// <param name="code">数据字典代码</param>
    /// <param name="items">选中项（已废弃）</param>
    /// <param name="key">搜索关键字</param>
    /// <returns>下拉列表数据</returns>
    [Obsolete("建议使用 GetComboGridData 方法")]
    public async Task<ServiceResult<List<ComboGridData>>> ComboGridData(string parentColumn, string parentId, int? current, int? pageSize, string code, string[] items, string key)
    {
        return await GetComboGridDataCore(code, parentColumn, parentId, key);
    }

    /// <summary>
    /// 获取通用下拉列表数据（新版本，推荐使用）
    /// </summary>
    /// <param name="body">查询参数对象</param>
    /// <returns>下拉列表数据</returns>
    public async Task<ServiceResult<List<ComboGridData>>> GetComboGridData(ComboGridDataBody body)
    {
        return await GetComboGridDataCore(body.code, body.parentColumn, body.parentId, body.key);
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
        var module = ModuleInfo.GetModuleInfo(moduleCode);
        if (module.IsNull())
            return ServiceResult<object>.OprateFailed(ResponseText.INVALID_MODULE_CODE);

        // 获取表名并查询数据
        var moduleSql = new ModuleSql(moduleCode, Db);
        var tableName = moduleSql.GetTableName();
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
        var module = ModuleInfo.GetModuleInfo(moduleCode);
        if (module.IsNull())
            return Success<Guid>(ResponseText.INVALID_MODULE_CODE);

        // 获取模块表名和表单列配置
        var (tableName, dict) = PrepareEntityData(moduleCode, entity);

        // 数据格式校验
        await CheckForm(moduleCode, dict);

        // 添加系统字段
        var id = Utility.GuidId;
        dict.Add("ID", id);
        dict.Add("CreatedTime", Utility.GetSysDate());
        dict.Add("CreatedBy", App.User.ID);
        dict.Add("ModificationNum", 0);
        dict.Add("GroupId", App.User?.GroupId);
        dict.Add("CompanyId", App.User?.CompanyId);
        if (!dict.ContainsKey("IsActive"))
            dict.Add("IsActive", true);
        dict.Add("IsDeleted", false);

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
        var module = ModuleInfo.GetModuleInfo(moduleCode);
        if (module.IsNull())
            return ServiceResult<Guid>.OprateFailed(ResponseText.INVALID_MODULE_CODE);

        // 获取模块表名和表单列配置
        var (tableName, dict) = PrepareEntityData(moduleCode, entity);

        // 数据唯一性校验
        await CheckForm(moduleCode, dict, OperateType.Update, id);

        // 添加更新时间和更新人
        dict.Add("UpdateTime", Utility.GetSysDate());
        dict.Add("UpdateBy", App.User.ID);
        dict.Add("ID", id);
        dict.Add("IsDeleted", false);

        // 执行更新操作
        await Db.Updateable(dict).AS(tableName).WhereColumns(["ID", "IsDeleted"]).ExecuteCommandAsync();

        // 回写修改次数（使用参数化查询避免SQL注入）
        await UpdateModificationNumber(tableName, id);

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
        var module = ModuleInfo.GetModuleInfo(moduleCode);
        if (module.IsNull())
            return Failed(ResponseText.INVALID_MODULE_CODE);

        // 获取表名
        var moduleSql = new ModuleSql(moduleCode, Db);
        var tableName = moduleSql.GetTableName();

        // 构建批量更新数据（逻辑删除）
        var dictList = new List<Dictionary<string, object>>();
        if (ids != null && ids.Any())
        {
            foreach (var deleteId in ids)
            {
                dictList.Add(new Dictionary<string, object>
                {
                    { "UpdateTime", Utility.GetSysDate() },
                    { "UpdateBy", App.User.ID },
                    { "IsDeleted", true },
                    { "ID", deleteId }
                });
            }
        }

        // 执行批量更新
        await Db.Updateable(dictList).AS(tableName).WhereColumns("ID").ExecuteCommandAsync();

        return Success(ResponseText.DELETE_SUCCESS);
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 构建查询条件
    /// 注意：此方法仍存在SQL注入风险，建议使用参数化查询重构
    /// </summary>
    private string BuildQueryCondition(Dictionary<string, object> searchParam, List<SmModuleColumnExtend> moduleColumns,
        string parentColumn = null, string parentId = null)
    {
        var queryCondition = "1=1";

        foreach (var item in searchParam)
        {
            // 跳过特殊参数
            if (item.Key is "current" or "pageSize" or "_timestamp" or "keyWord")
                continue;

            // 处理查询条件（模糊匹配）
            var value = item.Value?.ToString();
            if (string.IsNullOrEmpty(value))
                continue;

            // SQL注入风险警告：建议使用参数化查询
            if (moduleColumns.Any())
            {
                var column = moduleColumns.FirstOrDefault(a => a.DataIndex == item.Key);
                if (column != null)
                {
                    // 基本的SQL注入防护：转义单引号
                    var escapedValue = value.Replace("'", "''");
                    queryCondition += $" AND {column.TableAlias}.{item.Key} LIKE '%{escapedValue}%'";
                }
            }
            else
            {
                var escapedValue = value.Replace("'", "''");
                queryCondition += $" AND {DEFAULT_TABLE_ALIAS}.{item.Key} LIKE '%{escapedValue}%'";
            }
        }

        // 添加父子关系过滤条件
        if (!string.IsNullOrEmpty(parentId) && !string.IsNullOrEmpty(parentColumn))
        {
            var escapedParentId = parentId.Replace("'", "''");
            queryCondition += $" AND {DEFAULT_TABLE_ALIAS}.{parentColumn} = '{escapedParentId}'";
        }

        return queryCondition;
    }

    /// <summary>
    /// 构建关键字搜索条件
    /// </summary>
    private string BuildKeyWordCondition(string keyWord, List<SmModuleColumnExtend> moduleColumns)
    {
        if (string.IsNullOrEmpty(keyWord) || !moduleColumns.Any())
            return string.Empty;

        var keyWordCondition = new List<string>();
        var escapedKeyWord = keyWord.Replace("'", "''");

        foreach (var item in moduleColumns)
        {
            // 只对文本类型且未隐藏搜索的字段进行关键字搜索
            if (item.ValueType == null && item.HideInSearch == false)
            {
                keyWordCondition.Add($"{item.TableAlias}.{item.DataIndex} LIKE '%{escapedKeyWord}%'");
            }
        }

        return keyWordCondition.Any() ? string.Join(" OR ", keyWordCondition) : string.Empty;
    }

    /// <summary>
    /// 应用排序参数到GridList
    /// </summary>
    private void ApplySorting(GridList grid, Dictionary<string, string> sorterParam, string defaultSortField, string defaultSortDirection)
    {
        grid.SortField = defaultSortField;
        grid.SortDirection = string.IsNullOrEmpty(defaultSortDirection) ? DEFAULT_SORT_DIRECTION : defaultSortDirection;

        // 如果传入了排序参数，则覆盖默认排序
        if (sorterParam?.Count > 0)
        {
            foreach (var item in sorterParam)
            {
                grid.SortField = item.Key;
                grid.SortDirection = item.Value == "ascend" ? "ASC" : "DESC";
                break; // 只取第一个排序字段
            }
        }
    }

    /// <summary>
    /// 验证模块是否存在
    /// </summary>
    private SmModules ValidateModule(string moduleCode)
    {
        var module = ModuleInfo.GetModuleInfo(moduleCode);
        if (module.IsNull())
            throw new ArgumentException($"模块代码【{moduleCode}】不存在或无效", nameof(moduleCode));
        return module;
    }

    /// <summary>
    /// 准备实体数据（提取表名和过滤字段）
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="entity">实体对象</param>
    /// <returns>表名和字段字典</returns>
    private (string tableName, Dictionary<string, object> dict) PrepareEntityData(string moduleCode, object entity)
    {
        var moduleSql = new ModuleSql(moduleCode, Db);
        var tableName = moduleSql.GetTableName();
        var json = entity.ToString();
        var moduleColumnInfo = new ModuleSqlColumn(moduleCode);
        var moduleColumns = moduleColumnInfo.GetModuleSqlFormColumn();

        // 过滤出表单中配置的字段
        var formColumns = moduleColumns.Select(x => x.DataIndex).ToList();
        var dict = ConvertToDic(json);
        dict = dict.Where(pair => formColumns.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);

        return (tableName, dict);
    }

    /// <summary>
    /// 更新修改次数（使用参数化查询避免SQL注入）
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="id">记录ID</param>
    private async Task UpdateModificationNumber(string tableName, Guid id)
    {
        // 根据数据库类型选择合适的SQL
        var sql = Db.Ado.Context.CurrentConnectionConfig.DbType == SqlSugar.DbType.MySql
            ? $"UPDATE `{tableName}` SET ModificationNum = IFNULL(ModificationNum, 0) + 1, Tag = 1 WHERE ID = @id"
            : $"UPDATE {tableName} SET ModificationNum = ISNULL(ModificationNum, 0) + 1, Tag = 1 WHERE ID = @id";

        await Db.Ado.ExecuteCommandAsync(sql, new { id });
    }

    /// <summary>
    /// 获取下拉列表数据的核心逻辑
    /// </summary>
    private async Task<ServiceResult<List<ComboGridData>>> GetComboGridDataCore(string code, string parentColumn = null,
        string parentId = null, string key = null)
    {
        var data = new List<ComboGridData>();

        // 获取数据字典对应的SQL
        var entity = await LovHelper.GetCommonListSqlEntity(Db, code);
        if (entity == null)
            return ServiceResult<List<ComboGridData>>.OprateSuccess(data, ResponseText.QUERY_SUCCESS, 0);
        var sql = entity.SelectSql;
        if (string.IsNullOrWhiteSpace(sql))
            return ServiceResult<List<ComboGridData>>.OprateSuccess(data, ResponseText.QUERY_SUCCESS, 0);

        // 添加父子关系过滤
        if (!string.IsNullOrWhiteSpace(parentColumn) && !string.IsNullOrWhiteSpace(parentId))
        {
            var escapedParentId = parentId.Replace("'", "''");
            sql += $" AND {parentColumn} = '{escapedParentId}'";
        }
        var userType = App.User?.UserInfo?.UserType;
        if (userType != "Admin")
            sql = await AppendCompanyScopeConditionAsync(sql, entity.IsRoleDataScope == true, "CompanyId");

        sql = $"SELECT * FROM ({sql}) A";

        // 添加关键字搜索
        if (!string.IsNullOrWhiteSpace(key))
        {
            var escapedKey = key.Replace("'", "''");
            sql += $" WHERE label LIKE '%{escapedKey}%'";
        }

        data = await Db.Ado.SqlQueryAsync<ComboGridData>(sql);

        return ServiceResult<List<ComboGridData>>.OprateSuccess(data, ResponseText.QUERY_SUCCESS, data.Count);
    }

    /// <summary>
    /// 为 SQL 片段追加当前用户的数据权限公司范围条件
    /// </summary>
    private async Task<string> AppendCompanyScopeConditionAsync(string sql, bool enableScope, string companyColumn)
    {
        if (!enableScope || UserId == null || string.IsNullOrWhiteSpace(sql))
            return sql;

        var userDataScope = await DataScopeHelper.GetUserDataScope(Db, UserId.Value);
        var companyIds = userDataScope?.CompanyIds;
        if (companyIds == null || !companyIds.Any())
            return $"{sql} AND 1!=1";

        var joinKeys = string.Join("','", companyIds);
        return $"{sql} AND {companyColumn} IN ('{joinKeys}')";
    }

    #endregion
}
