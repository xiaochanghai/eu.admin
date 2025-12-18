/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmModuleSql.cs
*
*功 能： N / A
* 类 名： SmModuleSql
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/23 17:07:02  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 系统模块SQL服务
/// 提供模块SQL配置管理、SQL语句生成等功能
/// </summary>
public class SmModuleSqlServices : BaseServices<SmModuleSql, SmModuleSqlDto, InsertSmModuleSqlInput, EditSmModuleSqlInput>, ISmModuleSqlServices
{
    #region 常量定义

    /// <summary>
    /// 默认排序方向
    /// </summary>
    private const string DEFAULT_SORT_DIRECTION = "ASC";

    /// <summary>
    /// 默认查询条件
    /// </summary>
    private const string DEFAULT_QUERY_CONDITION = "1=1";

    #endregion

    #region 字段

    private readonly IBaseRepository<SmModuleSql> _dal;
    private readonly IBaseRepository<SmModules> _modulesDal;

    #endregion

    #region 构造函数

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dal">模块SQL数据访问层</param>
    /// <param name="dalSmModules">模块数据访问层</param>
    public SmModuleSqlServices(IBaseRepository<SmModuleSql> dal, IBaseRepository<SmModules> dalSmModules)
    {
        this._dal = dal;
        base.BaseDal = dal;
        this._modulesDal = dalSmModules;
    }

    #endregion

    #region 获取模块信息

    /// <summary>
    /// 根据模块ID获取模块及其SQL配置信息
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <returns>包含模块信息和SQL配置的动态对象</returns>
    /// <remarks>
    /// 返回的数据包括：
    /// - module: 模块基本信息
    /// - moduleSql: 模块SQL配置信息
    /// </remarks>
    public async Task<dynamic> GetByModuleId(Guid moduleId)
    {
        dynamic result = new ExpandoObject();
        dynamic data = new ExpandoObject();

        // 获取模块SQL配置
        var moduleSql = await _dal.QuerySingle(x => x.ModuleId == moduleId);

        // 获取模块基本信息
        var module = await _modulesDal.QueryById(moduleId);

        // 构建返回数据
        data.module = module;
        data.moduleSql = moduleSql;
        result.Success = true;
        result.Data = data;
        result.Message = ResponseText.QUERY_SUCCESS;

        return result;
    }

    #endregion

    #region 获取模块SQL信息

    /// <summary>
    /// 获取模块的完整查询SQL语句
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <returns>格式化后的完整SQL查询语句</returns>
    /// <remarks>
    /// 生成的SQL包括：
    /// - SELECT子句（从模块SQL配置获取）
    /// - 默认查询条件
    /// - 排序字段和排序方向
    /// - 替换SQL中的系统变量
    /// </remarks>
    public async Task<ServiceResult<string>> GetModuleFullSql(Guid moduleId)
    {
        // 获取模块信息
        var module = await _modulesDal.QueryById(moduleId);
        ModuleSql moduleSql = new(module.ModuleCode);

        // 获取表名和SQL片段
        string tableName = moduleSql.GetTableName();
        string SqlSelectBrwAndTable = moduleSql.GetSqlSelectBrwAndTable();
        string SqlSelectAndTable = moduleSql.GetSqlSelectAndTable();

        // 替换表名占位符
        if (!string.IsNullOrEmpty(tableName))
        {
            SqlSelectBrwAndTable = string.Format(SqlSelectBrwAndTable, tableName);
            SqlSelectAndTable = string.Format(SqlSelectAndTable, tableName);
        }

        // 获取默认查询条件
        string queryCodition = DEFAULT_QUERY_CONDITION;
        string SqlDefaultCondition = moduleSql.GetSqlDefaultCondition();

        // 构建查询网格配置
        GridList grid = new();
        string DefaultSortField = moduleSql.GetDefaultSortField();
        string DefaultSortDirection = moduleSql.GetDefaultSortDirection();

        // 设置默认排序方向
        if (string.IsNullOrEmpty(DefaultSortDirection))
        {
            DefaultSortDirection = DEFAULT_SORT_DIRECTION;
        }

        // 配置查询参数
        grid.SqlSelect = SqlSelectBrwAndTable;
        grid.SqlDefaultCondition = SqlDefaultCondition;
        grid.SqlQueryCondition = queryCodition;
        grid.SortField = DefaultSortField;
        grid.SortDirection = DefaultSortDirection;
        grid.ModuleCode = module.ModuleCode;

        // 生成完整SQL语句
        string sql = grid.GetQueryString();

        // 格式化SQL变量（替换系统变量如 @UserId 等）
        sql = ModuleInfo.FormatSqlVariable(sql);

        return Success(sql, ResponseText.QUERY_SUCCESS);
    }

    #endregion

    #region 新增

    /// <summary>
    /// 新增模块SQL配置
    /// </summary>
    /// <param name="entity">模块SQL实体</param>
    /// <returns>新增的记录ID</returns>
    /// <remarks>
    /// 新增后会重新初始化模块SQL缓存
    /// </remarks>
    public override async Task<Guid> Add(object entity)
    {
        var result = await base.Add(entity);

        // 重新初始化模块SQL缓存
        ModuleSql.Init();

        return result;
    }

    #endregion

    #region 更新

    /// <summary>
    /// 更新模块SQL配置
    /// </summary>
    /// <param name="Id">模块SQL ID</param>
    /// <param name="entity">模块SQL实体</param>
    /// <returns>更新结果</returns>
    /// <remarks>
    /// 更新后会重新初始化模块SQL缓存
    /// </remarks>
    public override async Task<bool> Update(Guid Id, object entity)
    {
        var result = await base.Update(Id, entity);

        // 重新初始化模块SQL缓存
        ModuleSql.Init();

        return result;
    }

    #endregion
}