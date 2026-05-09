using System.Data;
using System.Linq.Expressions;
using SqlSugar;
using EU.Core.Common;

namespace EU.Core.IServices.BASE;

/// <summary>
/// 增删改查基础服务接口
/// 提供完整的CRUD操作及审核、撤销等业务功能
/// </summary>
/// <typeparam name="TEntity">数据库实体类型</typeparam>
/// <typeparam name="TEntityDto">查询返回的DTO类型</typeparam>
/// <typeparam name="TInsertDto">新增时使用的DTO类型</typeparam>
/// <typeparam name="TEditDto">编辑时使用的DTO类型</typeparam>
public interface IBaseServices<TEntity, TEntityDto, TInsertDto, TEditDto> where TEntity : class
{
    /// <summary>
    /// SqlSugar数据库客户端对象
    /// 用于执行原生SQL或复杂查询
    /// </summary>
    ISqlSugarClient Db { get; }

    /// <summary>
    /// 根据主键ID检查数据是否存在
    /// </summary>
    /// <param name="objId">主键ID（必须在实体上标记[SugarColumn(IsPrimaryKey=true)]特性）</param>
    /// <returns>存在返回true，否则返回false</returns>
    Task<bool> AnyAsync(object objId);

    /// <summary>
    /// 根据主键ID查询单条数据（返回DTO）
    /// </summary>
    /// <param name="objId">主键ID</param>
    /// <returns>实体DTO对象</returns>
    /// <remarks>不使用缓存</remarks>
    Task<TEntityDto> QueryById(object objId);
    /// <summary>
    /// 根据主键ID查询单条数据（返回DTO，支持缓存）
    /// </summary>
    /// <param name="objId">主键ID（必须指定主键特性 [SugarColumn(IsPrimaryKey=true)]），如果是联合主键，请使用Where条件</param>
    /// <param name="blnUseCache">是否使用缓存，默认false</param>
    /// <returns>实体DTO对象</returns>
    /// <remarks>
    /// 1. 先查询实体数据
    /// 2. 映射为DTO对象
    /// 3. 设置主键ID
    /// </remarks>
    Task<TEntityDto> QueryDto(object objId, bool blnUseCache = false);

    /// <summary>
    /// 根据主键ID查询单条数据（返回实体）
    /// </summary>
    /// <param name="objId">主键ID（必须指定主键特性 [SugarColumn(IsPrimaryKey=true)]），如果是联合主键，请使用Where条件</param>
    /// <param name="blnUseCache">是否使用缓存，默认false</param>
    /// <returns>实体对象</returns>
    Task<TEntity> Query(object objId, bool blnUseCache = false);

    /// <summary>
    /// 根据主键ID数组查询多条数据（返回DTO集合）
    /// </summary>
    /// <param name="lstIds">主键ID数组</param>
    /// <returns>实体DTO集合</returns>
    Task<List<TEntityDto>> QueryByIDs(object[] lstIds);

    /// <summary>
    /// 新增数据（使用DTO）
    /// </summary>
    /// <param name="model">新增DTO对象</param>
    /// <param name="id">可选的主键ID，不传则自动生成</param>
    /// <returns>返回新增数据的主键ID</returns>
    /// <remarks>
    /// 1. 自动进行DTO到实体的映射转换
    /// 2. 执行表单验证和唯一性检查
    /// 3. 处理自动编号字段
    /// </remarks>
    Task<Guid> Add(TInsertDto model, Guid? id = null);

    /// <summary>
    /// 新增数据（使用实体）
    /// </summary>
    /// <param name="model">实体对象</param>
    /// <param name="id">可选的主键ID，不传则自动生成</param>
    /// <returns>返回新增数据的主键ID</returns>
    /// <remarks>
    /// 执行新增前的表单验证，包括：
    /// - 自动编号字段的生成
    /// - 唯一性字段的校验
    /// - 必填字段的检查
    /// </remarks>
    Task<Guid> Add(TEntity model, Guid? id = null);

    /// <summary>
    /// 新增数据（使用动态对象）
    /// </summary>
    /// <param name="entity">动态对象（通常是匿名对象或字典）</param>
    /// <returns>返回新增数据的主键ID</returns>
    /// <remarks>
    /// 1. 将动态对象转换为实体类型
    /// 2. 提取对象中的属性列表（排除ID字段）
    /// 3. 自动添加审核状态字段
    /// 4. 执行表单验证后插入数据
    /// </remarks>
    Task<Guid> Add(object entity);

    /// <summary>
    /// 批量新增数据（使用DTO集合）
    /// </summary>
    /// <param name="listEntity">新增DTO集合</param>
    /// <returns>返回新增数据的主键ID集合</returns>
    /// <remarks>
    /// 1. 批量操作比逐条插入性能更高
    /// 2. 对每条数据执行表单验证
    /// 3. 自动进行DTO到实体的批量映射
    /// </remarks>
    Task<List<Guid>> Add(List<TInsertDto> listEntity);

    /// <summary>
    /// 批量新增数据（使用实体集合）
    /// </summary>
    /// <param name="listEntity">实体对象集合</param>
    /// <returns>返回新增数据的主键ID集合</returns>
    /// <remarks>
    /// 批量插入，性能优于逐条插入
    /// 每条数据都会执行表单验证
    /// </remarks>
    Task<List<Guid>> Add(List<TEntity> listEntity);

    /// <summary>
    /// 根据主键ID删除数据
    /// </summary>
    /// <param name="id">主键ID</param>
    /// <returns>删除成功返回true</returns>
    /// <remarks>物理删除</remarks>
    Task<bool> DeleteById(object id);

    /// <summary>
    /// 删除单条数据（逻辑删除）
    /// </summary>
    /// <param name="id">主键ID</param>
    /// <returns>删除成功返回true</returns>
    /// <remarks>
    /// 逻辑删除，将IsDeleted字段设置为true，数据仍保留在数据库中
    /// </remarks>
    Task<bool> Delete(object id);

    /// <summary>
    /// 删除单条数据（根据实体对象）
    /// </summary>
    /// <param name="model">实体对象</param>
    /// <returns>删除成功返回true</returns>
    /// <remarks>物理删除，直接从数据库中删除记录</remarks>
    Task<bool> Delete(TEntity model);

    /// <summary>
    /// 根据条件表达式删除数据
    /// </summary>
    /// <param name="whereExpression">Lambda条件表达式</param>
    /// <returns>删除成功返回true</returns>
    /// <example>
    /// await Delete(x => x.Status == "Deleted" && x.CreateTime < DateTime.Now.AddDays(-30));
    /// </example>
    /// <remarks>物理删除，直接从数据库中删除符合条件的记录</remarks>
    Task<bool> Delete(Expression<Func<TEntity, bool>> whereExpression);

    /// <summary>
    /// 批量删除数据（物理删除）
    /// </summary>
    /// <param name="ids">主键ID数组</param>
    /// <returns>删除成功返回true</returns>
    /// <remarks>物理删除，直接从数据库中删除记录，不可恢复</remarks>
    Task<bool> DeleteByIds(object[] ids);

    /// <summary>
    /// 批量删除数据（逻辑删除）
    /// </summary>
    /// <param name="ids">主键ID数组</param>
    /// <returns>删除成功返回true</returns>
    /// <remarks>
    /// 逻辑删除实现：
    /// 1. 遍历ID数组，查询对应的实体
    /// 2. 将实体的IsDeleted字段设置为true
    /// 3. 批量更新到数据库
    /// 优点：数据可恢复，保留历史记录
    /// </remarks>
    Task<bool> Delete(Guid[] ids);

    /// <summary>
    /// 审核单条数据
    /// </summary>
    /// <param name="id">主键ID</param>
    /// <returns>审核成功返回true</returns>
    /// <remarks>
    /// 将审核状态从"Add"（新增待审核）改为"CompleteAudit"（审核完成）
    /// </remarks>
    Task<bool> Audit(object id);

    /// <summary>
    /// 批量审核数据
    /// </summary>
    /// <param name="ids">主键ID数组</param>
    /// <returns>审核成功返回true</returns>
    /// <remarks>
    /// 审核流程：
    /// 1. 遍历ID数组，查询对应实体
    /// 2. 检查审核状态是否为"Add"（待审核）
    /// 3. 将审核状态改为"CompleteAudit"（已审核）
    /// 4. 批量更新到数据库
    /// 只有状态为"Add"的数据才会被审核
    /// </remarks>
    Task<bool> BulkAudit(Guid[] ids);

    /// <summary>
    /// 撤销单条数据的审核
    /// </summary>
    /// <param name="id">主键ID</param>
    /// <returns>撤销成功返回true</returns>
    /// <remarks>
    /// 将审核状态从"CompleteAudit"（已审核）改回"Add"（待审核）
    /// </remarks>
    Task<bool> Revocation(object id);

    /// <summary>
    /// 批量撤销审核
    /// </summary>
    /// <param name="ids">主键ID数组</param>
    /// <returns>撤销成功返回true</returns>
    /// <remarks>
    /// 撤销流程：
    /// 1. 遍历ID数组，查询对应实体
    /// 2. 检查审核状态是否为"CompleteAudit"（已审核）
    /// 3. 将审核状态改回"Add"（待审核）
    /// 4. 批量更新到数据库
    /// 只有状态为"CompleteAudit"的数据才能被撤销
    /// </remarks>
    Task<bool> BulkRevocation(Guid[] ids);

    /// <summary>
    /// 更新数据（使用DTO）
    /// </summary>
    /// <param name="Id">要更新的数据主键ID</param>
    /// <param name="model">编辑DTO对象</param>
    /// <returns>更新成功返回true，否则返回false</returns>
    /// <remarks>
    /// 1. 先查询原有数据
    /// 2. 将DTO的属性值复制到实体
    /// 3. 执行唯一性校验
    /// 4. 更新到数据库
    /// </remarks>
    Task<bool> Update(Guid Id, TEditDto model);

    /// <summary>
    /// 更新数据（使用动态对象，不指定列）
    /// </summary>
    /// <param name="Id">主键ID</param>
    /// <param name="entity">动态对象</param>
    /// <returns>更新成功返回true</returns>
    Task<bool> Update(Guid Id, object entity);

    /// <summary>
    /// 更新数据（使用动态对象，可指定更新列）
    /// </summary>
    /// <param name="Id">主键ID</param>
    /// <param name="entity">动态对象</param>
    /// <param name="lstColumns">指定要更新的列名集合，null则更新所有列（除ID外）</param>
    /// <returns>更新成功返回true</returns>
    /// <remarks>
    /// 1. 将动态对象转换为实体
    /// 2. 校验唯一性字段
    /// 3. 只更新指定的列（如果lstColumns为空则更新除ID外的所有列）
    /// </remarks>
    Task<bool> Update(Guid Id, object entity, List<string> lstColumns);

    /// <summary>
    /// 更新数据并返回DTO
    /// </summary>
    /// <param name="Id">主键ID</param>
    /// <param name="entity">动态对象</param>
    /// <returns>返回更新后的实体DTO</returns>
    /// <remarks>
    /// 更新完成后将结果转换为DTO返回，适用于需要立即获取更新结果的场景
    /// </remarks>
    Task<TEntityDto> UpdateReturn(Guid Id, object entity);

    /// <summary>
    /// 批量更新实体集合
    /// </summary>
    /// <param name="model">实体集合</param>
    /// <returns>更新成功返回true</returns>
    Task<bool> Update(List<TEntity> model);

    /// <summary>
    /// 根据WHERE条件更新实体
    /// </summary>
    /// <param name="entity">实体对象（包含要更新的值）</param>
    /// <param name="where">WHERE条件字符串</param>
    /// <returns>更新成功返回true</returns>
    /// <example>
    /// await Update(entity, "Status='Active' AND CreateTime > '2024-01-01'");
    /// </example>
    Task<bool> Update(TEntity entity, string where);

    /// <summary>
    /// 使用匿名对象更新
    /// </summary>
    /// <param name="operateAnonymousObjects">匿名对象</param>
    /// <returns>更新成功返回true</returns>
    /// <example>
    /// await Update(new { ID = guid, Name = "NewName", Status = "Active" });
    /// </example>
    Task<bool> Update(object operateAnonymousObjects);

    /// <summary>
    /// 更新实体（指定列和忽略列）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="lstColumns">要更新的列名集合（为空则更新所有列）</param>
    /// <param name="lstIgnoreColumns">要忽略的列名集合</param>
    /// <param name="where">WHERE条件字符串</param>
    /// <returns>更新成功返回true</returns>
    /// <remarks>
    /// lstColumns和lstIgnoreColumns不能同时使用
    /// - 指定lstColumns：只更新这些列
    /// - 指定lstIgnoreColumns：更新除这些列外的所有列
    /// </remarks>
    Task<bool> Update(TEntity entity, List<string> lstColumns = null, List<string> lstIgnoreColumns = null, string where = "");

    /// <summary>
    /// 查询所有数据
    /// </summary>
    /// <returns>实体集合</returns>
    /// <remarks>注意：数据量大时慎用，建议使用分页查询</remarks>
    Task<List<TEntity>> Query();

    /// <summary>
    /// 根据SQL WHERE条件查询数据
    /// </summary>
    /// <param name="where">WHERE条件字符串（不含WHERE关键字）</param>
    /// <returns>实体集合</returns>
    /// <example>
    /// var list = await Query("Status='Active' AND CreateTime > '2024-01-01'");
    /// </example>
    Task<List<TEntity>> Query(string where);

    /// <summary>
    /// 根据Lambda表达式查询数据
    /// </summary>
    /// <param name="whereExpression">Lambda条件表达式</param>
    /// <returns>实体集合</returns>
    /// <example>
    /// var list = await Query(x => x.Status == "Active" && x.IsDeleted == false);
    /// </example>
    Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression);

    /// <summary>
    /// 根据条件查询并排序（字符串方式）
    /// </summary>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="orderByFields">排序字段，如 "Name asc,CreateTime desc"</param>
    /// <returns>实体集合</returns>
    Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression, string orderByFields);

    /// <summary>
    /// 查询指定列
    /// </summary>
    /// <typeparam name="TResult">返回结果类型</typeparam>
    /// <param name="expression">列选择表达式</param>
    /// <returns>指定列数据集合</returns>
    /// <example>
    /// var names = await Query(x => x.Name);
    /// var anonymousObjs = await Query(x => new { x.ID, x.Name, x.Status });
    /// </example>
    Task<List<TResult>> Query<TResult>(Expression<Func<TEntity, TResult>> expression);

    /// <summary>
    /// 查询指定列（带条件和排序）
    /// </summary>
    /// <typeparam name="TResult">返回结果类型</typeparam>
    /// <param name="expression">列选择表达式</param>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="orderByFields">排序字段，如 "Name asc,CreateTime desc"</param>
    /// <returns>指定列数据集合</returns>
    Task<List<TResult>> Query<TResult>(Expression<Func<TEntity, TResult>> expression, Expression<Func<TEntity, bool>> whereExpression, string orderByFields);

    /// <summary>
    /// 根据条件查询并排序
    /// </summary>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="orderByExpression">排序表达式</param>
    /// <param name="isAsc">是否升序，默认true</param>
    /// <returns>实体集合</returns>
    /// <example>
    /// var list = await Query(x => x.Status == "Active", x => x.CreateTime, false); // 按创建时间降序
    /// </example>
    Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> orderByExpression, bool isAsc = true);

    /// <summary>
    /// 根据SQL条件查询并排序
    /// </summary>
    /// <param name="where">WHERE条件字符串</param>
    /// <param name="orderByFields">排序字段，如 "Name asc,CreateTime desc"</param>
    /// <returns>实体集合</returns>
    Task<List<TEntity>> Query(string where, string orderByFields);

    /// <summary>
    /// 执行原生SQL查询
    /// </summary>
    /// <param name="sql">完整的SQL查询语句</param>
    /// <param name="parameters">SQL参数</param>
    /// <returns>实体集合</returns>
    /// <example>
    /// var list = await QuerySql("SELECT * FROM Users WHERE Name LIKE @Name",
    ///     new SugarParameter("@Name", "%admin%"));
    /// </example>
    Task<List<TEntity>> QuerySql(string sql, SugarParameter[] parameters = null);

    /// <summary>
    /// 执行原生SQL查询（返回DataTable）
    /// </summary>
    /// <param name="sql">完整的SQL查询语句</param>
    /// <param name="parameters">SQL参数</param>
    /// <returns>DataTable数据表</returns>
    /// <remarks>适用于动态列查询或需要DataTable格式的场景</remarks>
    Task<DataTable> QueryTable(string sql, SugarParameter[] parameters = null);

    /// <summary>
    /// 查询前N条数据（使用Lambda条件）
    /// </summary>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="top">取前N条</param>
    /// <param name="orderByFields">排序字段，如 "Name asc,CreateTime desc"</param>
    /// <returns>实体集合</returns>
    /// <example>
    /// var top10 = await Query(x => x.Status == "Active", 10, "CreateTime desc");
    /// </example>
    Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression, int top, string orderByFields);

    /// <summary>
    /// 查询前N条数据（使用SQL条件）
    /// </summary>
    /// <param name="where">WHERE条件字符串</param>
    /// <param name="top">取前N条</param>
    /// <param name="orderByFields">排序字段，如 "Name asc,CreateTime desc"</param>
    /// <returns>实体集合</returns>
    Task<List<TEntity>> Query(string where, int top, string orderByFields);

    /// <summary>
    /// 分页查询（使用Lambda条件）
    /// </summary>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="pageIndex">页码，从1开始</param>
    /// <param name="pageSize">每页记录数</param>
    /// <param name="orderByFields">排序字段，如 "Name asc,CreateTime desc"</param>
    /// <returns>实体集合</returns>
    /// <example>
    /// var list = await Query(x => x.Status == "Active", 1, 20, "CreateTime desc");
    /// </example>
    Task<List<TEntity>> Query(
        Expression<Func<TEntity, bool>> whereExpression, int pageIndex, int pageSize, string orderByFields);

    /// <summary>
    /// 分页查询（使用SQL条件）
    /// </summary>
    /// <param name="where">WHERE条件字符串</param>
    /// <param name="pageIndex">页码，从1开始</param>
    /// <param name="pageSize">每页记录数</param>
    /// <param name="orderByFields">排序字段，如 "Name asc,CreateTime desc"</param>
    /// <returns>实体集合</returns>
    Task<List<TEntity>> Query(string where, int pageIndex, int pageSize, string orderByFields);


    /// <summary>
    /// 分页查询（返回分页模型）
    /// </summary>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="pageIndex">页码，从1开始</param>
    /// <param name="pageSize">每页记录数</param>
    /// <param name="orderByFields">排序字段，可为空</param>
    /// <returns>分页模型，包含数据列表和总记录数</returns>
    Task<PageModel<TEntity>> QueryPage(Expression<Func<TEntity, bool>> whereExpression, int pageIndex = 1, int pageSize = 20, string orderByFields = null);

    /// <summary>
    /// 高级分页查询（使用QueryFilter，返回DTO）
    /// </summary>
    /// <param name="filter">查询过滤器，支持复杂条件、排序、分页</param>
    /// <returns>服务层分页结果，包含DTO列表和总记录数</returns>
    /// <remarks>
    /// 1. 执行分页查询
    /// 2. 将实体映射为DTO
    /// 3. 设置每个DTO的主键ID
    /// 4. 返回分页结果
    /// </remarks>
    Task<ServicePageResult<TEntityDto>> QueryFilterPage([FromFilter] QueryFilter filter);

    /// <summary>
    /// 根据条件查询单条数据
    /// </summary>
    /// <param name="whereExpression">Lambda条件表达式</param>
    /// <returns>实体对象，不存在返回null</returns>
    /// <example>
    /// var user = await QuerySingle(x => x.Code == "ADMIN");
    /// </example>
    Task<TEntity> QuerySingle(Expression<Func<TEntity, bool>> whereExpression);

    /// <summary>
    /// 根据条件查询多条数据（返回DTO集合）
    /// </summary>
    /// <param name="whereExpression">Lambda条件表达式</param>
    /// <returns>实体DTO集合</returns>
    /// <example>
    /// var user = await QueryDto(x => x.Code == "ADMIN");
    /// </example>
    Task<List<TEntityDto>> QueryDto(Expression<Func<TEntity, bool>> whereExpression);

    /// <summary>
    /// 三表联查
    /// </summary>
    /// <typeparam name="T">第一个表的实体类型</typeparam>
    /// <typeparam name="T2">第二个表的实体类型</typeparam>
    /// <typeparam name="T3">第三个表的实体类型</typeparam>
    /// <typeparam name="TResult">返回结果类型</typeparam>
    /// <param name="joinExpression">联接表达式</param>
    /// <param name="selectExpression">查询列表达式</param>
    /// <param name="whereLambda">条件表达式，可为空</param>
    /// <returns>查询结果集合</returns>
    /// <example>
    /// var result = await QueryMuch&lt;User, Role, Department, UserRoleDto&gt;(
    ///     (u, r, d) => new object[] {
    ///         JoinType.Left, u.RoleId == r.ID,
    ///         JoinType.Left, u.DeptId == d.ID
    ///     },
    ///     (u, r, d) => new UserRoleDto { UserId = u.ID, RoleName = r.Name, DeptName = d.Name },
    ///     (u, r, d) => u.IsDeleted == false
    /// );
    /// </example>
    Task<List<TResult>> QueryMuch<T, T2, T3, TResult>(
        Expression<Func<T, T2, T3, object[]>> joinExpression,
        Expression<Func<T, T2, T3, TResult>> selectExpression,
        Expression<Func<T, T2, T3, bool>> whereLambda = null) where T : class, new();

    /// <summary>
    /// 分页查询（使用PaginationModel）
    /// </summary>
    /// <param name="pagination">分页模型，包含条件、页码、页大小、排序等</param>
    /// <returns>分页模型，包含数据列表和总记录数</returns>
    /// <remarks>
    /// PaginationModel支持动态条件构建
    /// </remarks>
    Task<PageModel<TEntity>> QueryPage(PaginationModel pagination);

    #region 分表（按时间分表）

    /// <summary>
    /// 根据ID查询分表数据
    /// </summary>
    /// <param name="objId">主键ID</param>
    /// <returns>实体对象</returns>
    /// <remarks>自动在所有分表中查找</remarks>
    Task<TEntity> QueryByIdSplit(object objId);

    /// <summary>
    /// 新增数据到分表
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <returns>返回插入的行数集合</returns>
    /// <remarks>根据配置的分表规则自动路由到对应的分表</remarks>
    Task<List<long>> AddSplit(TEntity entity);

    /// <summary>
    /// 删除分表数据
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="dateTime">时间值，用于确定分表</param>
    /// <returns>删除成功返回true</returns>
    Task<bool> DeleteSplit(TEntity entity, DateTime dateTime);

    /// <summary>
    /// 更新分表数据
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="dateTime">时间值，用于确定分表</param>
    /// <returns>更新成功返回true</returns>
    Task<bool> UpdateSplit(TEntity entity, DateTime dateTime);

    /// <summary>
    /// 分表分页查询
    /// </summary>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="beginTime">开始时间（确定起始分表）</param>
    /// <param name="endTime">结束时间（确定结束分表）</param>
    /// <param name="pageIndex">页码，从1开始</param>
    /// <param name="pageSize">每页记录数</param>
    /// <param name="orderByFields">排序字段</param>
    /// <returns>分页模型，包含数据列表和总记录数</returns>
    /// <remarks>会在时间范围内的所有分表中查询并合并结果</remarks>
    Task<PageModel<TEntity>> QueryPageSplit(Expression<Func<TEntity, bool>> whereExpression, DateTime beginTime, DateTime endTime, int pageIndex = 1, int pageSize = 20, string orderByFields = null);

    #endregion 
}
