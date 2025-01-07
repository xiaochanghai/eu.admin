using System.Data;
using System.Linq.Expressions;
using SqlSugar;
using EU.Core.Common;

namespace EU.Core.IServices.BASE;

/// <summary>
/// 增删改查接口
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TEntityDto"></typeparam>
/// <typeparam name="TInsertDto"></typeparam>
/// <typeparam name="TEditDto"></typeparam>
public interface IBaseServices<TEntity, TEntityDto, TInsertDto, TEditDto> where TEntity : class
{
    ISqlSugarClient Db { get; }

    /// <summary>
    /// 根据ID查询实体数据是否存在
    /// </summary>
    /// <param name="objId">id（必须指定主键特性 [SugarColumn(IsPrimaryKey=true)]）</param>
    /// <returns>true or false</returns>
    Task<bool> AnyAsync(object objId);
    Task<TEntityDto> QueryById(object objId);
    /// <summary>
    /// 根据ID查询一条数据
    /// </summary>
    /// <param name="objId">id（必须指定主键特性 [SugarColumn(IsPrimaryKey=true)]），如果是联合主键，请使用Where条件</param>
    /// <param name="blnUseCache">是否使用缓存</param>
    /// <returns>数据实体</returns>
    Task<TEntityDto> QueryDto(object objId, bool blnUseCache = false);

    /// <summary>
    /// 根据ID查询一条数据
    /// </summary>
    /// <param name="objId">id（必须指定主键特性 [SugarColumn(IsPrimaryKey=true)]），如果是联合主键，请使用Where条件</param>
    /// <param name="blnUseCache">是否使用缓存</param>
    /// <returns>数据实体</returns>
    Task<TEntity> Query(object objId, bool blnUseCache = false);
    Task<List<TEntityDto>> QueryByIDs(object[] lstIds);

    /// <summary>
    /// 写入实体数据
    /// </summary>
    /// <param name="entity">实体类</param>
    /// <returns>主键ID</returns>
    Task<Guid> Add(TInsertDto model, Guid? id = null);

    /// <summary>
    /// 写入实体数据
    /// </summary>
    /// <param name="entity">实体类</param>
    /// <returns>主键ID</returns>
    Task<Guid> Add(TEntity model, Guid? id = null);

    /// <summary>
    /// 写入实体数据
    /// </summary>
    /// <param name="entity">实体类</param>
    /// <returns>主键ID</returns>
    Task<Guid> Add(object entity);

    /// <summary>
    /// 批量插入实体(速度快)
    /// </summary>
    /// <param name="listEntity">实体集合</param>
    /// <returns>影响行数</returns>
    Task<List<Guid>> Add(List<TInsertDto> listEntity);

    /// <summary>
    /// 批量插入实体(速度快)
    /// </summary>
    /// <param name="listEntity">实体集合</param>
    /// <returns>影响行数</returns>
    Task<List<Guid>> Add(List<TEntity> listEntity);

    Task<bool> DeleteById(object id);
    Task<bool> Delete(object id);

    Task<bool> Delete(TEntity model);

    Task<bool> Delete(Expression<Func<TEntity, bool>> whereExpression);

    Task<bool> DeleteByIds(object[] ids);

    /// <summary>
    /// 批量删除
    /// </summary>
    /// <param name="ids">id合集</param>
    /// <returns></returns>
    Task<bool> Delete(Guid[] ids);
    Task<bool> Audit(object id);
    Task<bool> BulkAudit(Guid[] ids);

    Task<bool> Revocation(object id);
    Task<bool> BulkRevocation(Guid[] ids);

    Task<bool> Update(Guid Id, TEditDto model);

    Task<bool> Update(Guid Id, object entity);

    /// <summary>
    /// 更新数据
    /// </summary>
    /// <param name="Id">主键ID</param>
    /// <param name="entity">实体数据</param>
    /// <param name="lstColumns">只更新某列</param>
    /// <returns></returns>
    Task<bool> Update(Guid Id, object entity, List<string> lstColumns);

    Task<TEntityDto> UpdateReturn(Guid Id, object entity);
    Task<bool> Update(List<TEntity> model);
    Task<bool> Update(TEntity entity, string where);

    Task<bool> Update(object operateAnonymousObjects);

    Task<bool> Update(TEntity entity, List<string> lstColumns = null, List<string> lstIgnoreColumns = null, string where = "");

    Task<List<TEntity>> Query();
    Task<List<TEntity>> Query(string where);
    Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression);
    Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression, string orderByFields);
    Task<List<TResult>> Query<TResult>(Expression<Func<TEntity, TResult>> expression);
    Task<List<TResult>> Query<TResult>(Expression<Func<TEntity, TResult>> expression, Expression<Func<TEntity, bool>> whereExpression, string orderByFields);
    Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> orderByExpression, bool isAsc = true);
    Task<List<TEntity>> Query(string where, string orderByFields);
    Task<List<TEntity>> QuerySql(string sql, SugarParameter[] parameters = null);
    Task<DataTable> QueryTable(string sql, SugarParameter[] parameters = null);

    Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression, int top, string orderByFields);
    Task<List<TEntity>> Query(string where, int top, string orderByFields);

    Task<List<TEntity>> Query(
        Expression<Func<TEntity, bool>> whereExpression, int pageIndex, int pageSize, string orderByFields);
    Task<List<TEntity>> Query(string where, int pageIndex, int pageSize, string orderByFields);


    Task<PageModel<TEntity>> QueryPage(Expression<Func<TEntity, bool>> whereExpression, int pageIndex = 1, int pageSize = 20, string orderByFields = null);
    Task<ServicePageResult<TEntityDto>> QueryFilterPage([FromFilter] QueryFilter filter);

    /// <summary>
    /// 查询数据
    /// </summary>
    /// <param name="whereExpression">whereExpression</param>
    /// <returns>查询数据</returns>
    Task<TEntity> QuerySingle(Expression<Func<TEntity, bool>> whereExpression);
    Task<List<TResult>> QueryMuch<T, T2, T3, TResult>(
        Expression<Func<T, T2, T3, object[]>> joinExpression,
        Expression<Func<T, T2, T3, TResult>> selectExpression,
        Expression<Func<T, T2, T3, bool>> whereLambda = null) where T : class, new();
    Task<PageModel<TEntity>> QueryPage(PaginationModel pagination);

    #region 分表
    Task<TEntity> QueryByIdSplit(object objId);
    Task<List<long>> AddSplit(TEntity entity);
    Task<bool> DeleteSplit(TEntity entity, DateTime dateTime);
    Task<bool> UpdateSplit(TEntity entity, DateTime dateTime);
    Task<PageModel<TEntity>> QueryPageSplit(Expression<Func<TEntity, bool>> whereExpression, DateTime beginTime, DateTime endTime, int pageIndex = 1, int pageSize = 20, string orderByFields = null);
    #endregion 
}
