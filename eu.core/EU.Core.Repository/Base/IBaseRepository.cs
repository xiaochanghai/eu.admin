using EU.Core.Model;
using SqlSugar;
using System.Data;
using System.Linq.Expressions;
using EU.Core.Common;

namespace EU.Core.IRepository.Base;

public interface IBaseRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// SqlsugarClient实体
    /// </summary>
    ISqlSugarClient Db { get; }

    /// <summary>
    /// 查询实体数据是否存在
    /// </summary>
    /// <param name="objId"></param>
    /// <returns></returns>
    Task<bool> AnyAsync(object objId);

    /// <summary>
    /// 查询实体数据是否存在
    /// </summary>
    /// <param name="objId"></param>
    /// <returns></returns>
    bool Any(object objId);

    /// <summary>
    /// 查询实体数据是否存在
    /// </summary>
    /// <param name="whereExpression">条件表达式</param>
    /// <returns></returns>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> whereExpression);

    /// <summary>
    /// 根据Id查询实体
    /// </summary>
    /// <param name="objId"></param>
    /// <returns></returns>
    Task<TEntity> QueryById(object objId);
    /// <summary>
    /// 根据ID查询一条数据
    /// </summary>
    /// <param name="objId">id（必须指定主键特性 [SugarColumn(IsPrimaryKey=true)]），如果是联合主键，请使用Where条件</param>
    /// <param name="blnUseCache">是否使用缓存</param>
    /// <returns>数据实体</returns>
    Task<TEntity> QueryById(object objId, bool blnUseCache = false);
    /// <summary>
    /// 根据id数组查询实体list
    /// </summary>
    /// <param name="lstIds"></param>
    /// <returns></returns>
    Task<List<TEntity>> QueryByIDs(object[] lstIds);

    /// <summary>
    /// 添加
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    Task<Guid> Add(TEntity model, Guid? id = null);


    /// <summary>
    /// 写入实体数据
    /// </summary>
    /// <param name="entity">实体类</param>
    /// <param name="insertColumns">指定只插入列</param>
    /// <returns>返回Guid</returns>
    Task<Guid> Add(TEntity entity, List<string> insertColumns);

    /// <summary>
    /// 批量添加
    /// </summary>
    /// <param name="listEntity"></param>
    /// <returns></returns>
    Task<List<Guid>> Add(List<TEntity> listEntity);

    /// <summary>
    /// 根据id 删除某一实体
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<bool> DeleteById(object id);

    /// <summary>
    /// 根据对象，删除某一实体
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    Task<bool> Delete(TEntity model);

    Task<bool> Delete(Expression<Func<TEntity, bool>> whereExpression);

    /// <summary>
    /// 根据id数组，删除实体list
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    Task<bool> DeleteByIds(object[] ids);

    /// <summary>
    /// 更新model
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    Task<bool> Update(TEntity model);

    /// <summary>
    /// 更新实体数据
    /// </summary>
    /// <param name="entities">实体类</param>
    /// <param name="lstColumns">只更新某列</param>
    /// <param name="lstIgnoreColumns">不更新某列</param>
    /// <param name="where">where条件</param>
    /// <returns></returns>
    Task<bool> Update(List<TEntity> entities, List<string> lstColumns = null, List<string> lstIgnoreColumns = null, string where = null);

    /// <summary>
    /// 更新model
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    Task<bool> Update(List<TEntity> model);

    /// <summary>
    /// 根据model，更新，带where条件
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="where">条件</param>
    /// <returns></returns>
    Task<bool> Update(TEntity entity, string where);

    /// <summary>
    /// 根据SQL更新
    /// </summary>
    /// <param name="sql">SQL语句</param>
    /// <param name="parameters">参数</param>
    /// <returns></returns>
    Task<bool> Update(string sql, SugarParameter[] parameters = null);

    /// <summary>
    /// 根据匿名对象更新
    /// </summary>
    /// <param name="operateAnonymousObjects">匿名对象</param>
    /// <returns></returns>
    Task<bool> Update(object operateAnonymousObjects);

    /// <summary>
    /// 更新实体数据，指定列
    /// </summary>
    /// <param name="entity">实体类</param>
    /// <param name="lstColumns">只更新某列</param>
    /// <param name="lstIgnoreColumns">不更新某列</param>
    /// <param name="where">where条件</param>
    /// <returns></returns>
    Task<bool> Update(TEntity entity, List<string> lstColumns = null, List<string> lstIgnoreColumns = null, string where = null);

    /// <summary>
    /// 查询
    /// </summary>
    /// <returns></returns>
    Task<List<TEntity>> Query();

    /// <summary>
    /// 带sql where查询
    /// </summary>
    /// <param name="where"></param>
    /// <returns></returns>
    Task<List<TEntity>> Query(string where);

    /// <summary>
    /// 根据表达式查询
    /// </summary>
    /// <param name="whereExpression"></param>
    /// <returns></returns>
    Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression);

    /// <summary>
    /// 根据表达式，指定返回对象模型，查询
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="expression"></param>
    /// <returns></returns>
    Task<List<TResult>> Query<TResult>(Expression<Func<TEntity, TResult>> expression);

    /// <summary>
    /// 查询单个实体
    /// </summary>
    /// <param name="expression">查询条件</param>
    /// <returns></returns>
    Task<TEntity> QuerySingle(Expression<Func<TEntity, bool>> expression);

    /// <summary>
    /// 根据表达式，指定返回对象模型，排序，查询
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="expression"></param>
    /// <param name="whereExpression"></param>
    /// <param name="orderByFields"></param>
    /// <returns></returns>
    Task<List<TResult>> Query<TResult>(Expression<Func<TEntity, TResult>> expression, Expression<Func<TEntity, bool>> whereExpression, string orderByFields);

    /// <summary>
    /// 根据表达式查询并排序
    /// </summary>
    /// <param name="whereExpression">查询条件</param>
    /// <param name="orderByFields">排序字段</param>
    /// <returns></returns>
    Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression, string orderByFields);

    /// <summary>
    /// 根据表达式查询并排序
    /// </summary>
    /// <param name="whereExpression">查询条件</param>
    /// <param name="orderByExpression">排序表达式</param>
    /// <param name="isAsc">是否升序</param>
    /// <returns></returns>
    Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> orderByExpression, bool isAsc = true);

    /// <summary>
    /// 根据条件查询并排序
    /// </summary>
    /// <param name="where">条件</param>
    /// <param name="orderByFields">排序字段</param>
    /// <returns></returns>
    Task<List<TEntity>> Query(string where, string orderByFields);

    /// <summary>
    /// 查询前N条数据
    /// </summary>
    /// <param name="whereExpression">查询条件</param>
    /// <param name="intTop">前N条</param>
    /// <param name="orderByFields">排序字段</param>
    /// <returns></returns>
    Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression, int intTop, string orderByFields);

    /// <summary>
    /// 查询前N条数据
    /// </summary>
    /// <param name="where">条件</param>
    /// <param name="intTop">前N条</param>
    /// <param name="orderByFields">排序字段</param>
    /// <returns></returns>
    Task<List<TEntity>> Query(string where, int intTop, string orderByFields);

    /// <summary>
    /// 根据SQL语句查询
    /// </summary>
    /// <param name="sql">完整的SQL语句</param>
    /// <param name="parameters">参数</param>
    /// <returns>泛型集合</returns>
    Task<List<TEntity>> QuerySql(string sql, SugarParameter[] parameters = null);

    /// <summary>
    /// 根据SQL语句查询DataTable
    /// </summary>
    /// <param name="sql">完整的SQL语句</param>
    /// <param name="parameters">参数</param>
    /// <returns>DataTable</returns>
    Task<DataTable> QueryTable(string sql, SugarParameter[] parameters = null);

    /// <summary>
    /// 分页查询
    /// </summary>
    /// <param name="whereExpression">查询条件</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="orderByFields">排序字段</param>
    /// <returns></returns>
    Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression, int pageIndex, int pageSize, string orderByFields);

    /// <summary>
    /// 分页查询
    /// </summary>
    /// <param name="where">条件</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="orderByFields">排序字段</param>
    /// <returns></returns>
    Task<List<TEntity>> Query(string where, int pageIndex, int pageSize, string orderByFields);

    /// <summary>
    /// 根据表达式，排序字段，分页查询
    /// </summary>
    /// <param name="whereExpression"></param>
    /// <param name="pageIndex"></param>
    /// <param name="pageSize"></param>
    /// <param name="orderByFields"></param>
    /// <returns></returns>
    Task<PageModel<TEntity>> QueryPage(Expression<Func<TEntity, bool>> whereExpression, int pageIndex = 1, int pageSize = 20, string orderByFields = null);

    /// <summary>
    /// 根据过滤器分页查询
    /// </summary>
    /// <param name="filter">查询过滤器</param>
    /// <returns></returns>
    Task<ServicePageResult<TEntity>> QueryFilterPage([FromFilter] QueryFilter filter);

    /// <summary>
    /// 三表联查
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="joinExpression"></param>
    /// <param name="selectExpression"></param>
    /// <param name="whereLambda"></param>
    /// <returns></returns>
    Task<List<TResult>> QueryMuch<T, T2, T3, TResult>(
        Expression<Func<T, T2, T3, object[]>> joinExpression,
        Expression<Func<T, T2, T3, TResult>> selectExpression,
        Expression<Func<T, T2, T3, bool>> whereLambda = null) where T : class, new();

    /// <summary>
    /// 两表联查-分页
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="joinExpression"></param>
    /// <param name="selectExpression"></param>
    /// <param name="whereExpression"></param>
    /// <param name="pageIndex"></param>
    /// <param name="pageSize"></param>
    /// <param name="orderByFields"></param>
    /// <returns></returns>
    Task<PageModel<TResult>> QueryTabsPage<T, T2, TResult>(
        Expression<Func<T, T2, object[]>> joinExpression,
        Expression<Func<T, T2, TResult>> selectExpression,
        Expression<Func<TResult, bool>> whereExpression,
        int pageIndex = 1,
        int pageSize = 20,
        string orderByFields = null);

    /// <summary>
    /// 两表联合查询-分页-分组
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="joinExpression"></param>
    /// <param name="selectExpression"></param>
    /// <param name="whereExpression"></param>
    /// <param name="groupExpression"></param>
    /// <param name="pageIndex"></param>
    /// <param name="pageSize"></param>
    /// <param name="orderByFields"></param>
    /// <returns></returns>
    Task<PageModel<TResult>> QueryTabsPage<T, T2, TResult>(
        Expression<Func<T, T2, object[]>> joinExpression,
        Expression<Func<T, T2, TResult>> selectExpression,
        Expression<Func<TResult, bool>> whereExpression,
        Expression<Func<T, object>> groupExpression,
        int pageIndex = 1,
        int pageSize = 20,
        string orderByFields = null);

    #region 分表
    /// <summary>
    /// 通过ID查询（分表）
    /// </summary>
    /// <param name="objId">主键ID</param>
    /// <returns></returns>
    Task<TEntity> QueryByIdSplit(object objId);

    /// <summary>
    /// 自动分表插入
    /// </summary>
    /// <param name="entity">实体数据</param>
    /// <returns></returns>
    Task<List<long>> AddSplit(TEntity entity);

    /// <summary>
    /// 删除数据（分表）
    /// </summary>
    /// <param name="entity">数据实体</param>
    /// <param name="dateTime">时间参数用于定位分表</param>
    /// <returns></returns>
    Task<bool> DeleteSplit(TEntity entity, DateTime dateTime);

    /// <summary>
    /// 更新实体数据（分表）
    /// </summary>
    /// <param name="entity">数据实体</param>
    /// <param name="dateTime">时间参数用于定位分表</param>
    /// <returns></returns>
    Task<bool> UpdateSplit(TEntity entity, DateTime dateTime);

    /// <summary>
    /// 分页查询（分表）
    /// </summary>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="beginTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="pageIndex">页码（下标0）</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="orderByFields">排序字段，如name asc,age desc</param>
    /// <returns></returns>
    Task<PageModel<TEntity>> QueryPageSplit(Expression<Func<TEntity, bool>> whereExpression, DateTime beginTime, DateTime endTime, int pageIndex = 1, int pageSize = 20, string orderByFields = null);
    #endregion
}
