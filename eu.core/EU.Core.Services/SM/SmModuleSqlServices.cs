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
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

using System.Dynamic;

namespace EU.Core.Services;

/// <summary>
/// 系统模块SQL (服务)
/// </summary>
public class SmModuleSqlServices : BaseServices<SmModuleSql, SmModuleSqlDto, InsertSmModuleSqlInput, EditSmModuleSqlInput>, ISmModuleSqlServices
{
    private readonly IBaseRepository<SmModuleSql> _dal;
    private readonly IBaseRepository<SmModules> _dalSmModules;
    public SmModuleSqlServices(IBaseRepository<SmModuleSql> dal, IBaseRepository<SmModules> dalSmModules)
    {
        this._dal = dal;
        base.BaseDal = dal;
        _dalSmModules = dalSmModules;
    }

    #region 获取模块信息
    /// <summary>
    /// 获取模块信息
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <returns></returns>
    public async Task<dynamic> GetByModuleId(Guid moduleId)
    {
        dynamic result = new ExpandoObject();
        dynamic data = new ExpandoObject();

        //获取模块信息
        var moduleSql = await _dal.QuerySingle(x => x.ModuleId == moduleId);

        var module = await _dalSmModules.QueryById(moduleId);


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
    /// 获取模块SQL信息
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <returns></returns> 
    public async Task<ServiceResult<string>> GetModuleFullSql(Guid moduleId)
    {
        var module = await _dalSmModules.QueryById(moduleId);
        ModuleSql moduleSql = new(module.ModuleCode);
        string tableName = moduleSql.GetTableName();
        string SqlSelectBrwAndTable = moduleSql.GetSqlSelectBrwAndTable();
        string SqlSelectAndTable = moduleSql.GetSqlSelectAndTable();
        if (!string.IsNullOrEmpty(tableName))
        {
            SqlSelectBrwAndTable = string.Format(SqlSelectBrwAndTable, tableName);
            SqlSelectAndTable = string.Format(SqlSelectAndTable, tableName);
        }
        string queryCodition = "1=1";
        string SqlDefaultCondition = moduleSql.GetSqlDefaultCondition();

        GridList grid = new();
        string DefaultSortField = moduleSql.GetDefaultSortField();
        string DefaultSortDirection = moduleSql.GetDefaultSortDirection();
        if (string.IsNullOrEmpty(DefaultSortDirection))
        {
            DefaultSortDirection = "ASC";
        }
        grid.SqlSelect = SqlSelectBrwAndTable;
        grid.SqlDefaultCondition = SqlDefaultCondition;
        grid.SqlQueryCondition = queryCodition;
        grid.SortField = DefaultSortField;
        grid.SortDirection = DefaultSortDirection;
        grid.ModuleCode = module.ModuleCode;
        string sql = grid.GetQueryString();
        sql = ModuleInfo.FormatSqlVariable(sql);
        return Success(sql, ResponseText.QUERY_SUCCESS);
    }
    #endregion

    #region 新增
    public override async Task<Guid> Add(object entity)
    {
        var result = await base.Add(entity);

        ModuleSql.Init();
        return result;
    }
    #endregion

    #region 更新
    public override async Task<bool> Update(Guid Id, object entity)
    {
        var result = await base.Update(Id, entity);

        ModuleSql.Init();
        return result;
    }
    #endregion
}