using EU.Core.Common.UserManager;
using EU.Core.IServices.BASE;
using System.Linq.Expressions;
using System.Reflection;

namespace EU.Core.Services.BASE;

/// <summary>
/// 增删改查基础服务类
/// 提供完整的CRUD操作及审核、撤销等业务功能
/// </summary>
/// <typeparam name="TEntity">数据库实体类型</typeparam>
/// <typeparam name="TEntityDto">查询返回的DTO类型</typeparam>
/// <typeparam name="TInsertDto">新增时使用的DTO类型</typeparam>
/// <typeparam name="TEditDto">编辑时使用的DTO类型</typeparam>
public class BaseServices<TEntity, TEntityDto, TInsertDto, TEditDto> : IBaseServices<TEntity, TEntityDto, TInsertDto, TEditDto> where TEntity : class, new()
{
    #region 构造函数及属性

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="BaseDal">数据访问层仓储对象，通过依赖注入传入</param>
    public BaseServices(IBaseRepository<TEntity> BaseDal = null)
    {
        this.BaseDal = BaseDal;
    }

    /// <summary>
    /// 数据访问层仓储对象
    /// 通过子类构造函数注入，基类不直接使用构造函数注入
    /// </summary>
    public IBaseRepository<TEntity> BaseDal { get; set; }

    /// <summary>
    /// SqlSugar数据库客户端对象
    /// 用于执行原生SQL或复杂查询
    /// </summary>
    public ISqlSugarClient Db => BaseDal.Db;

    /// <summary>
    /// 数据上下文对象
    /// </summary>
    //public DataContext _context;

    /// <summary>
    /// 当前登录用户完整信息
    /// 从用户上下文中获取
    /// </summary>
    public SmUsers UserInfo => UserContext.Current.UserInfo;

    /// <summary>
    /// 当前登录用户ID
    /// 类型为Guid?，可能为空
    /// </summary>
    public Guid? UserId => UserContext.Current.User_Id;

    /// <summary>
    /// 当前登录用户ID的字符串形式
    /// 用于需要字符串类型ID的场景
    /// </summary>
    public string UserId1 => UserId?.ToString();

    /// <summary>
    /// 当前登录用户所属公司ID
    /// 用于多公司场景的数据隔离
    /// </summary>
    public Guid? CompanyId => UserContext.Current.CompanyId;

    /// <summary>
    /// 当前登录用户所属集团ID
    /// 用于多集团场景的数据隔离
    /// </summary>
    public Guid? GroupId => UserContext.Current.GroupId;

    #endregion

    #region 写入数据 - 提供单条和批量新增功能

    /// <summary>
    /// 新增数据（使用DTO）
    /// </summary>
    /// <param name="entity">新增DTO对象</param>
    /// <param name="id">可选的主键ID，不传则自动生成</param>
    /// <returns>返回新增数据的主键ID</returns>
    /// <remarks>
    /// 1. 自动进行DTO到实体的映射转换
    /// 2. 执行表单验证和唯一性检查
    /// 3. 处理自动编号字段
    /// </remarks>
    public virtual async Task<Guid> Add(TInsertDto entity, Guid? id = null)
    {
        var entity1 = Mapper.Map(entity).ToANew<TEntity>();
        return await Add(entity1, id);
    }

    /// <summary>
    /// 新增数据（使用实体）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="id">可选的主键ID，不传则自动生成</param>
    /// <returns>返回新增数据的主键ID</returns>
    /// <remarks>
    /// 执行新增前的表单验证，包括：
    /// - 自动编号字段的生成
    /// - 唯一性字段的校验
    /// - 必填字段的检查
    /// </remarks>
    public virtual async Task<Guid> Add(TEntity entity, Guid? id = null)
    {
        CheckForm(entity, OperateType.Add);
        return await BaseDal.Add(entity, id);
    }

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
    public virtual async Task<Guid> Add(object entity)
    {
        var model = ConvertToEntity(entity);
        var dic = ConvertToDic(entity);

        // 获取需要插入的列（排除ID字段）
        var lstColumns = dic.Keys.Where(x => x != nameof(BasePoco.ID) && x != "Id").ToList();

        CheckForm(model, OperateType.Add);
        return await BaseDal.Add(model, lstColumns);
    }

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
    public virtual async Task<List<Guid>> Add(List<TInsertDto> listEntity)
    {
        var list = Mapper.Map(listEntity).ToANew<List<TEntity>>();

        // 对每个实体执行新增前验证
        list.ForEach(entity =>
        {
            CheckForm(entity, OperateType.Add);
        });

        return await BaseDal.Add(list);
    }

    /// <summary>
    /// 批量新增数据（使用实体集合）
    /// </summary>
    /// <param name="list">实体对象集合</param>
    /// <returns>返回新增数据的主键ID集合</returns>
    /// <remarks>
    /// 批量插入，性能优于逐条插入
    /// 每条数据都会执行表单验证
    /// </remarks>
    public virtual async Task<List<Guid>> Add(List<TEntity> list)
    {
        // 对每个实体执行新增前验证
        list.ForEach(entity =>
        {
            CheckForm(entity, OperateType.Add);
        });

        return await BaseDal.Add(list);
    }

    #endregion

    #region 更新数据 - 提供多种更新方式

    /// <summary>
    /// 更新数据（使用DTO）
    /// </summary>
    /// <param name="Id">要更新的数据主键ID</param>
    /// <param name="editModel">编辑DTO对象</param>
    /// <returns>更新成功返回true，否则返回false</returns>
    /// <remarks>
    /// 1. 先查询原有数据
    /// 2. 将DTO的属性值复制到实体
    /// 3. 执行唯一性校验
    /// 4. 更新到数据库
    /// </remarks>
    public async Task<bool> Update(Guid Id, TEditDto editModel)
    {
        // 验证参数有效性和数据存在性
        if (editModel == null || !await AnyAsync(Id))
            return false;

        // 查询原有实体数据
        var entity = await Query(Id);

        // 将DTO属性值复制到实体
        ConvertTEditDto2TEntity(editModel, entity);

        // 设置主键ID
        if (entity is RootEntityTkey<Guid> rootEntity1)
            rootEntity1.ID = Id;

        // 校验唯一性字段
        CheckOnly(entity, Id);

        return await BaseDal.Update(entity);
    }

    /// <summary>
    /// 更新数据（使用动态对象，不指定列）
    /// </summary>
    /// <param name="Id">主键ID</param>
    /// <param name="entity">动态对象</param>
    /// <returns>更新成功返回true</returns>
    public virtual async Task<bool> Update(Guid Id, object entity) => await Update(Id, entity, null);

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
    public virtual async Task<bool> Update(Guid Id, object entity, List<string> lstColumns = null)
    {
        var model = ConvertToEntity(entity);

        // 校验唯一性
        CheckOnly(model, Id);

        // 设置主键ID
        if (model is RootEntityTkey<Guid> rootEntity1)
            rootEntity1.ID = Id;

        // 提取要更新的列名（排除ID字段）
        var dic = ConvertToDic(entity);
        var columns = dic.Keys.Where(x => x != "ID" && x != "Id").ToList();
        columns = lstColumns?.Any() == true ? lstColumns : columns;

        var result = await Update(model, columns, null);


        //#region 回写修改次数
        //string sql = $"UPDATE {entityType.GetEntityTableName()} SET ModificationNum = isnull (ModificationNum, 0) + 1, Tag = 1 where ID='{Id}'";
        //await Db.Ado.ExecuteCommandAsync(sql);
        //#endregion

        return result;
    }

    /// <summary>
    /// 更新数据并返回DTO
    /// </summary>
    /// <param name="Id">主键ID</param>
    /// <param name="entity">动态对象</param>
    /// <returns>返回更新后的实体DTO</returns>
    /// <remarks>
    /// 更新完成后将结果转换为DTO返回，适用于需要立即获取更新结果的场景
    /// </remarks>
    public virtual async Task<TEntityDto> UpdateReturn(Guid Id, object entity)
    {
        var model = ConvertToEntity(entity);
        await Update(Id, entity);
        return Mapper.Map(model).ToANew<TEntityDto>();
    }

    /// <summary>
    /// 批量更新数据（字典形式）
    /// </summary>
    /// <param name="editModels">字典，键为主键ID，值为编辑DTO</param>
    /// <returns>更新成功返回true</returns>
    /// <remarks>
    /// 1. 遍历字典中的每个键值对
    /// 2. 查询对应的实体数据
    /// 3. 将DTO属性复制到实体
    /// 4. 批量更新到数据库
    /// </remarks>
    public async Task<bool> Update(Dictionary<Guid, TEditDto> editModels)
    {
        List<TEntity> entities = new();

        foreach (var keyValuePairs in editModels)
        {
            // 验证数据有效性和存在性
            if (keyValuePairs.Value == null || !BaseDal.Any(keyValuePairs.Key))
                continue;

            // 查询原有实体
            var entity = await Query(keyValuePairs.Key);

            // 将DTO属性值复制到实体
            ConvertTEditDto2TEntity(keyValuePairs.Value, entity);

            // 校验唯一性
            CheckOnly(entity, keyValuePairs.Key);

            entities.Add(entity);
        }

        return await BaseDal.Update(entities);
    }

    /// <summary>
    /// 批量更新实体集合
    /// </summary>
    /// <param name="listEntity">实体集合</param>
    /// <returns>更新成功返回true</returns>
    public async Task<bool> Update(List<TEntity> listEntity) => await BaseDal.Update(listEntity);

    /// <summary>
    /// 根据WHERE条件更新实体
    /// </summary>
    /// <param name="entity">实体对象（包含要更新的值）</param>
    /// <param name="where">WHERE条件字符串</param>
    /// <returns>更新成功返回true</returns>
    /// <example>
    /// await Update(entity, "Status='Active' AND CreateTime > '2024-01-01'");
    /// </example>
    public async Task<bool> Update(TEntity entity, string where) => await BaseDal.Update(entity, where);

    /// <summary>
    /// 使用匿名对象更新
    /// </summary>
    /// <param name="operateAnonymousObjects">匿名对象</param>
    /// <returns>更新成功返回true</returns>
    /// <example>
    /// await Update(new { ID = guid, Name = "NewName", Status = "Active" });
    /// </example>
    public async Task<bool> Update(object operateAnonymousObjects) => await BaseDal.Update(operateAnonymousObjects);

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
    public async Task<bool> Update(TEntity entity, List<string> lstColumns = null, List<string> lstIgnoreColumns = null, string where = "")
        => await BaseDal.Update(entity, lstColumns, lstIgnoreColumns, where);

    #endregion

    #region 删除数据 - 支持逻辑删除和物理删除

    /// <summary>
    /// 删除单条数据（根据实体对象）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <returns>删除成功返回true</returns>
    /// <remarks>物理删除，直接从数据库中删除记录</remarks>
    public async Task<bool> Delete(TEntity entity) => await BaseDal.Delete(entity);

    /// <summary>
    /// 根据条件表达式删除数据
    /// </summary>
    /// <param name="whereExpression">Lambda条件表达式</param>
    /// <returns>删除成功返回true</returns>
    /// <example>
    /// await Delete(x => x.Status == "Deleted" && x.CreateTime < DateTime.Now.AddDays(-30));
    /// </example>
    /// <remarks>物理删除，直接从数据库中删除符合条件的记录</remarks>
    public async Task<bool> Delete(Expression<Func<TEntity, bool>> whereExpression) => await BaseDal.Delete(whereExpression);

    /// <summary>
    /// 根据主键ID删除数据
    /// </summary>
    /// <param name="id">主键ID</param>
    /// <returns>删除成功返回true</returns>
    /// <remarks>物理删除</remarks>
    public async Task<bool> DeleteById(object id) => await BaseDal.DeleteById(id);

    /// <summary>
    /// 删除单条数据（逻辑删除）
    /// </summary>
    /// <param name="id">主键ID</param>
    /// <returns>删除成功返回true</returns>
    /// <remarks>
    /// 逻辑删除，将IsDeleted字段设置为true，数据仍保留在数据库中
    /// </remarks>
    public virtual async Task<bool> Delete(object id) => await Delete([Guid.Parse(id.ObjToString())]);

    /// <summary>
    /// 批量删除数据（逻辑删除）
    /// </summary>
    /// <param name="ids">主键ID数组</param>
    /// <returns>删除成功返回true</returns>
    /// <remarks>
    /// 逻辑删除实现：
    /// 1. 一次性查询所有数据（优化：避免N+1查询）
    /// 2. 在内存中批量设置IsDeleted字段为true
    /// 3. 批量更新到数据库
    /// 优点：数据可恢复，保留历史记录
    /// </remarks>
    public virtual async Task<bool> Delete(Guid[] ids)
    {
        if (ids == null || !ids.Any())
            return false;

        // 1. 一次性查询所有数据（只需1次数据库访问）
        var entities = await BaseDal.Query(x =>
            ids.Contains(((BasePoco)(object)x).ID));

        if (!entities.Any())
            return false;

        // 2. 批量设置删除标记
        entities.ForEach(entity =>
        {
            if (entity is BasePoco basePoco)
                basePoco.IsDeleted = true;
        });

        // 3. 批量更新（只需1次数据库访问）
        return await BaseDal.Update(entities, ["IsDeleted"]);
    }

    /// <summary>
    /// 批量删除数据（物理删除）
    /// </summary>
    /// <param name="ids">主键ID数组</param>
    /// <returns>删除成功返回true</returns>
    /// <remarks>物理删除，直接从数据库中删除记录，不可恢复</remarks>
    public async Task<bool> DeleteByIds(object[] ids) => await BaseDal.DeleteByIds(ids);

    #endregion

    #region 查询数据 - 提供丰富的查询方法

    #region 辅助扩展方法

    /// <summary>
    /// DTO扩展方法：字典映射、全称转换、单位转换等
    /// </summary>
    /// <param name="view">DTO对象</param>
    /// <remarks>
    /// 子类可重写此方法，对查询结果进行二次处理
    /// 例如：将字典值ID转换为显示文本、单位换算等
    /// </remarks>
    public virtual void SetLabel(TEntityDto view)
    {
    }

    #endregion

    #region 数据存在性检查

    /// <summary>
    /// 根据主键ID检查数据是否存在
    /// </summary>
    /// <param name="objId">主键ID（必须在实体上标记[SugarColumn(IsPrimaryKey=true)]特性）</param>
    /// <returns>存在返回true，否则返回false</returns>
    public async Task<bool> AnyAsync(object objId) => await BaseDal.AnyAsync(objId);

    /// <summary>
    /// 根据条件表达式检查数据是否存在
    /// </summary>
    /// <param name="whereExpression">Lambda条件表达式</param>
    /// <returns>存在返回true，否则返回false</returns>
    /// <example>
    /// bool exists = await AnyAsync(x => x.Code == "USER001" && x.IsDeleted == false);
    /// </example>
    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> whereExpression) => await BaseDal.AnyAsync(whereExpression);

    #endregion

    #region 单条数据查询

    /// <summary>
    /// 根据主键ID查询单条数据（返回DTO）
    /// </summary>
    /// <param name="objId">主键ID</param>
    /// <returns>实体DTO对象</returns>
    /// <remarks>不使用缓存</remarks>
    public virtual async Task<TEntityDto> QueryById(object objId) => await QueryDto(objId, false);

    /// <summary>
    /// 根据主键ID查询单条数据（返回DTO，支持缓存）
    /// </summary>
    /// <param name="objId">主键ID</param>
    /// <param name="blnUseCache">是否使用缓存，默认false</param>
    /// <returns>实体DTO对象</returns>
    /// <remarks>
    /// 1. 先查询实体数据
    /// 2. 映射为DTO对象
    /// 3. 设置主键ID
    /// </remarks>
    public virtual async Task<TEntityDto> QueryDto(object objId, bool blnUseCache = false)
    {
        var data = await Query(objId, blnUseCache);
        var data1 = Mapper.Map(data).ToANew<TEntityDto>();

        // 设置主键ID
        if (data1 is RootEntityTkey<Guid> rootEntity)
        {
            rootEntity.ID = Guid.Parse(objId.ObjToString());
        }

        return data1;
    }

    /// <summary>
    /// 根据主键ID查询单条数据（返回实体）
    /// </summary>
    /// <param name="objId">主键ID</param>
    /// <param name="blnUseCache">是否使用缓存，默认false</param>
    /// <returns>实体对象</returns>
    public async Task<TEntity> Query(object objId, bool blnUseCache = false) => await BaseDal.QueryById(objId, blnUseCache);

    /// <summary>
    /// 根据条件查询单条数据
    /// </summary>
    /// <param name="whereExpression">Lambda条件表达式</param>
    /// <returns>实体对象，不存在返回null</returns>
    /// <example>
    /// var user = await QuerySingle(x => x.Code == "ADMIN");
    /// </example>
    public async Task<TEntity> QuerySingle(Expression<Func<TEntity, bool>> whereExpression)
    {
        var list = await BaseDal.Query(whereExpression);
        return list.Any() ? list.FirstOrDefault() : default;
    }

    #endregion

    #region 批量查询

    /// <summary>
    /// 根据主键ID数组查询多条数据（返回DTO集合）
    /// </summary>
    /// <param name="lstIds">主键ID数组</param>
    /// <returns>实体DTO集合</returns>
    public async Task<List<TEntityDto>> QueryByIDs(object[] lstIds)
    {
        var data = await BaseDal.QueryByIDs(lstIds);
        return Mapper.Map(data).ToANew<List<TEntityDto>>();
    }

    /// <summary>
    /// 查询所有数据
    /// </summary>
    /// <returns>实体集合</returns>
    /// <remarks>注意：数据量大时慎用，建议使用分页查询</remarks>
    public virtual async Task<List<TEntity>> Query() => await BaseDal.Query();

    /// <summary>
    /// 根据SQL WHERE条件查询数据
    /// </summary>
    /// <param name="where">WHERE条件字符串（不含WHERE关键字）</param>
    /// <returns>实体集合</returns>
    /// <example>
    /// var list = await Query("Status='Active' AND CreateTime > '2024-01-01'");
    /// </example>
    public async Task<List<TEntity>> Query(string where) => await BaseDal.Query(where);

    /// <summary>
    /// 根据Lambda表达式查询数据
    /// </summary>
    /// <param name="whereExpression">Lambda条件表达式</param>
    /// <returns>实体集合</returns>
    /// <example>
    /// var list = await Query(x => x.Status == "Active" && x.IsDeleted == false);
    /// </example>
    public async Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression) => await BaseDal.Query(whereExpression);

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
    public async Task<List<TResult>> Query<TResult>(Expression<Func<TEntity, TResult>> expression) => await BaseDal.Query(expression);

    /// <summary>
    /// 查询指定列（带条件和排序）
    /// </summary>
    /// <typeparam name="TResult">返回结果类型</typeparam>
    /// <param name="expression">列选择表达式</param>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="orderByFileds">排序字段，如 "Name asc,CreateTime desc"</param>
    /// <returns>指定列数据集合</returns>
    public async Task<List<TResult>> Query<TResult>(Expression<Func<TEntity, TResult>> expression, Expression<Func<TEntity, bool>> whereExpression, string orderByFileds)
        => await BaseDal.Query(expression, whereExpression, orderByFileds);

    #endregion

    #region 排序查询

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
    public async Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> orderByExpression, bool isAsc = true)
        => await BaseDal.Query(whereExpression, orderByExpression, isAsc);

    /// <summary>
    /// 根据条件查询并排序（字符串方式）
    /// </summary>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="orderByFileds">排序字段，如 "Name asc,CreateTime desc"</param>
    /// <returns>实体集合</returns>
    public async Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression, string orderByFileds)
        => await BaseDal.Query(whereExpression, orderByFileds);

    /// <summary>
    /// 根据SQL条件查询并排序
    /// </summary>
    /// <param name="where">WHERE条件字符串</param>
    /// <param name="orderByFileds">排序字段，如 "Name asc,CreateTime desc"</param>
    /// <returns>实体集合</returns>
    public async Task<List<TEntity>> Query(string where, string orderByFileds) => await BaseDal.Query(where, orderByFileds);

    #endregion

    #region TOP N查询

    /// <summary>
    /// 查询前N条数据（使用Lambda条件）
    /// </summary>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="top">取前N条</param>
    /// <param name="orderByFileds">排序字段，如 "Name asc,CreateTime desc"</param>
    /// <returns>实体集合</returns>
    /// <example>
    /// var top10 = await Query(x => x.Status == "Active", 10, "CreateTime desc");
    /// </example>
    public async Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression, int top, string orderByFileds)
        => await BaseDal.Query(whereExpression, top, orderByFileds);

    /// <summary>
    /// 查询前N条数据（使用SQL条件）
    /// </summary>
    /// <param name="where">WHERE条件字符串</param>
    /// <param name="top">取前N条</param>
    /// <param name="orderByFileds">排序字段，如 "Name asc,CreateTime desc"</param>
    /// <returns>实体集合</returns>
    public async Task<List<TEntity>> Query(string where, int top, string orderByFileds) => await BaseDal.Query(where, top, orderByFileds);

    #endregion

    #region 原生SQL查询

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
    public async Task<List<TEntity>> QuerySql(string sql, SugarParameter[] parameters = null) => await BaseDal.QuerySql(sql, parameters);

    /// <summary>
    /// 执行原生SQL查询（返回DataTable）
    /// </summary>
    /// <param name="sql">完整的SQL查询语句</param>
    /// <param name="parameters">SQL参数</param>
    /// <returns>DataTable数据表</returns>
    /// <remarks>适用于动态列查询或需要DataTable格式的场景</remarks>
    public async Task<DataTable> QueryTable(string sql, SugarParameter[] parameters = null) => await BaseDal.QueryTable(sql, parameters);

    #endregion

    #region 分页查询

    /// <summary>
    /// 分页查询（使用Lambda条件）
    /// </summary>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="pageIndex">页码，从1开始</param>
    /// <param name="pageSize">每页记录数</param>
    /// <param name="orderByFileds">排序字段，如 "Name asc,CreateTime desc"</param>
    /// <returns>实体集合</returns>
    /// <example>
    /// var list = await Query(x => x.Status == "Active", 1, 20, "CreateTime desc");
    /// </example>
    public async Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression, int pageIndex, int pageSize, string orderByFileds)
        => await BaseDal.Query(whereExpression, pageIndex, pageSize, orderByFileds);

    /// <summary>
    /// 分页查询（使用SQL条件）
    /// </summary>
    /// <param name="where">WHERE条件字符串</param>
    /// <param name="pageIndex">页码，从1开始</param>
    /// <param name="pageSize">每页记录数</param>
    /// <param name="orderByFileds">排序字段，如 "Name asc,CreateTime desc"</param>
    /// <returns>实体集合</returns>
    public async Task<List<TEntity>> Query(string where, int pageIndex, int pageSize, string orderByFileds)
        => await BaseDal.Query(where, pageIndex, pageSize, orderByFileds);

    /// <summary>
    /// 分页查询（返回分页模型）
    /// </summary>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="pageIndex">页码，从1开始</param>
    /// <param name="pageSize">每页记录数</param>
    /// <param name="orderByFileds">排序字段，可为空</param>
    /// <returns>分页模型，包含数据列表和总记录数</returns>
    public async Task<PageModel<TEntity>> QueryPage(Expression<Func<TEntity, bool>> whereExpression, int pageIndex = 1, int pageSize = 20, string orderByFileds = null)
        => await BaseDal.QueryPage(whereExpression, pageIndex, pageSize, orderByFileds);

    /// <summary>
    /// 分页查询（使用PaginationModel）
    /// </summary>
    /// <param name="pagination">分页模型，包含条件、页码、页大小、排序等</param>
    /// <returns>分页模型，包含数据列表和总记录数</returns>
    /// <remarks>
    /// PaginationModel支持动态条件构建
    /// </remarks>
    public async Task<PageModel<TEntity>> QueryPage(PaginationModel pagination)
        => await QueryPage(DynamicLinqFactory.CreateLambda<TEntity>(pagination.Conditions), pagination.PageIndex, pagination.PageSize, pagination.OrderByFileds);

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
    public async Task<ServicePageResult<TEntityDto>> QueryFilterPage([FromFilter] QueryFilter filter)
    {
        var data = await BaseDal.QueryFilterPage(filter);
        var data1 = Mapper.Map(data.Data).ToANew<List<TEntityDto>>();

        // 为每个DTO设置主键ID
        int i = 0;
        foreach (var entityInfo in data1)
        {
            if (entityInfo is RootEntityTkey<Guid> rootEntity)
            {
                var entityInfo1 = data.Data[i];
                var getType = entityInfo1.GetType();
                var id = getType.GetProperty("ID");
                rootEntity.ID = Guid.Parse(id.GetValue(entityInfo1).ToString());
            }
            i++;
        }

        return new ServicePageResult<TEntityDto>(filter.PageIndex, data.TotalCount, filter.PageSize, data1);
    }

    #endregion

    #region 多表联查

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
    public async Task<List<TResult>> QueryMuch<T, T2, T3, TResult>(
        Expression<Func<T, T2, T3, object[]>> joinExpression,
        Expression<Func<T, T2, T3, TResult>> selectExpression,
        Expression<Func<T, T2, T3, bool>> whereLambda = null) where T : class, new()
    {
        return await BaseDal.QueryMuch(joinExpression, selectExpression, whereLambda);
    }

    #endregion

    #region 分表查询（按时间分表）

    /// <summary>
    /// 新增数据到分表
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <returns>返回插入的行数集合</returns>
    /// <remarks>根据配置的分表规则自动路由到对应的分表</remarks>
    public async Task<List<long>> AddSplit(TEntity entity) => await BaseDal.AddSplit(entity);

    /// <summary>
    /// 更新分表数据
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="dateTime">时间值，用于确定分表</param>
    /// <returns>更新成功返回true</returns>
    public async Task<bool> UpdateSplit(TEntity entity, DateTime dateTime) => await BaseDal.UpdateSplit(entity, dateTime);

    /// <summary>
    /// 删除分表数据
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="dateTime">时间值，用于确定分表</param>
    /// <returns>删除成功返回true</returns>
    public async Task<bool> DeleteSplit(TEntity entity, DateTime dateTime) => await BaseDal.DeleteSplit(entity, dateTime);

    /// <summary>
    /// 根据ID查询分表数据
    /// </summary>
    /// <param name="objId">主键ID</param>
    /// <returns>实体对象</returns>
    /// <remarks>自动在所有分表中查找</remarks>
    public async Task<TEntity> QueryByIdSplit(object objId) => await BaseDal.QueryByIdSplit(objId);

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
    public async Task<PageModel<TEntity>> QueryPageSplit(
        Expression<Func<TEntity, bool>> whereExpression,
        DateTime beginTime,
        DateTime endTime,
        int pageIndex = 1,
        int pageSize = 20,
        string orderByFields = null)
        => await BaseDal.QueryPageSplit(whereExpression, beginTime, endTime, pageIndex, pageSize, orderByFields);

    #endregion

    #endregion

    #region 审核数据 - 业务流程审核功能

    /// <summary>
    /// 审核单条数据
    /// </summary>
    /// <param name="id">主键ID</param>
    /// <returns>审核成功返回true</returns>
    /// <remarks>
    /// 将审核状态从"Add"（新增待审核）改为"CompleteAudit"（审核完成）
    /// </remarks>
    public virtual async Task<bool> Audit(object id) => await BulkAudit([Guid.Parse(id.ToString())]);

    /// <summary>
    /// 批量审核数据
    /// </summary>
    /// <param name="ids">主键ID数组</param>
    /// <returns>审核成功返回true</returns>
    public virtual async Task<bool> BulkAudit(Guid[] ids) => await BulkAudit(ids, null);

    /// <summary>
    /// 批量审核数据（带WHERE条件）
    /// </summary>
    /// <param name="ids">主键ID数组</param>
    /// <param name="where">附加WHERE条件，可为空</param>
    /// <returns>审核成功返回true</returns>
    /// <remarks>
    /// 审核流程：
    /// 1. 一次性查询所有数据（优化：避免N+1查询）
    /// 2. 在内存中过滤状态为"Add"（待审核）的数据
    /// 3. 将审核状态改为"CompleteAudit"（已审核）
    /// 4. 批量更新到数据库
    /// 只有状态为"Add"的数据才会被审核
    /// </remarks>
    public virtual async Task<bool> BulkAudit(Guid[] ids, string where = null)
    {
        if (ids == null || !ids.Any())
            return false;

        // 1. 一次性查询所有数据（只需1次数据库访问）
        var entities = await BaseDal.Query(x =>
            ids.Contains(((BasePoco)(object)x).ID));

        if (!entities.Any())
            return false;

        // 2. 在内存中过滤和修改
        var entitiesToUpdate = entities
            .Where(x => (x as BasePoco)?.AuditStatus == "Add")
            .Select(x =>
            {
                (x as BasePoco).AuditStatus = "CompleteAudit";
                return x;
            })
            .ToList();

        if (!entitiesToUpdate.Any())
            return false;

        // 3. 批量更新（只需1次数据库访问）
        return await BaseDal.Update(entitiesToUpdate, ["AuditStatus"], null, where);
    }

    #endregion

    #region 撤销审核 - 反向业务流程

    /// <summary>
    /// 撤销单条数据的审核
    /// </summary>
    /// <param name="id">主键ID</param>
    /// <returns>撤销成功返回true</returns>
    /// <remarks>
    /// 将审核状态从"CompleteAudit"（已审核）改回"Add"（待审核）
    /// </remarks>
    public virtual async Task<bool> Revocation(object id) => await BulkRevocation([Guid.Parse(id.ToString())]);

    /// <summary>
    /// 批量撤销审核
    /// </summary>
    /// <param name="ids">主键ID数组</param>
    /// <returns>撤销成功返回true</returns>
    /// <remarks>
    /// 撤销流程：
    /// 1. 一次性查询所有数据（优化：避免N+1查询）
    /// 2. 在内存中过滤状态为"CompleteAudit"（已审核）的数据
    /// 3. 将审核状态改回"Add"（待审核）
    /// 4. 批量更新到数据库
    /// 只有状态为"CompleteAudit"的数据才能被撤销
    /// </remarks>
    public virtual async Task<bool> BulkRevocation(Guid[] ids)
    {
        if (ids == null || !ids.Any())
            return false;

        // 1. 一次性查询所有数据（只需1次数据库访问）
        var entities = await BaseDal.Query(x =>
            ids.Contains(((BasePoco)(object)x).ID));

        if (!entities.Any())
            return false;

        // 2. 在内存中过滤和修改
        var entitiesToUpdate = entities
            .Where(x => (x as BasePoco)?.AuditStatus == "CompleteAudit")
            .Select(x =>
            {
                (x as BasePoco).AuditStatus = "Add";
                return x;
            })
            .ToList();

        if (!entitiesToUpdate.Any())
            return false;

        // 3. 批量更新（只需1次数据库访问）
        return await BaseDal.Update(entitiesToUpdate, ["AuditStatus"]);
    }

    #endregion

    #region 辅助方法
    /// <summary>
    /// 转换TEditDto2TEntity
    /// </summary>
    /// <param name="pTargetObjSrc"></param>
    /// <param name="pTargetObjDest"></param>
    /// <returns></returns>
    public static void ConvertTEditDto2TEntity(TEditDto source, TEntity dest)
    {
        foreach (PropertyInfo mItem in typeof(TEditDto).GetProperties())
        {
            if (dest.HasField(mItem.Name))
                dest.SetValueForField(mItem.Name, mItem.GetValue(source, null));
        }
        //dest.SetValueForField(DbConsts.ColunmName_LastModificationTime, DateTimeHelper.Now());
        //if (_currentUserId != default)
        //{
        //    //dest.SetValueForField(DbConsts.ColunmName_LastModifierId, _currentUserId);
        //    dest.SetValueForField(DbConsts.ColunmName_LastModifier, _currentUserName);
        //}

        //if (_currentTenantId != null)
        //{
        //    dest.SetValueForField(DbConsts.ColunmName_TenantId, _currentTenantId);
        //}
    }

    /// <summary>
    /// 转换TEditDto2TEntity
    /// </summary>
    /// <param name="pTargetObjSrc"></param>
    /// <param name="pTargetObjDest"></param>
    /// <returns></returns>
    public static string ConvertToString(TEntity json) => JsonHelper.ObjToJson(json);
    public static TEntity ConvertToEntity(string json) => JsonHelper.JsonToObj<TEntity>(json);
    public static TEntity ConvertToEntity(object json) => ConvertToEntity(json.ToString());
    public static Dictionary<string, object> ConvertToDic(string json) => JsonHelper.JsonToObj<Dictionary<string, object>>(json);
    public static Dictionary<string, object> ConvertToDic(object json) => ConvertToDic(json.ToString());

    /// <summary>
    /// 判断唯一性
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="id">主键ID</param>
    public static void CheckOnly(TEntity entity, Guid? id = null) => CheckForm(entity, id == null ? OperateType.Add : OperateType.Update, id);

    /// <summary>
    /// 验证表单
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="id">主键ID</param>
    public static void CheckForm(TEntity entity, OperateType operateType = OperateType.Add, Guid? id = null)
    {
        var entityType = typeof(TEntity);
        var tableName = entityType.GetEntityTableName();

        var moduleCode = entity.GetModuleCode();
        if (tableName == "SmModules")
            moduleCode = "SM_MODULE_MNG";
        if (tableName == "SmModuleSql")
            moduleCode = null;
        if (moduleCode.IsNotEmptyOrNull())
        {
            var module = ModuleInfo.GetModuleInfo(moduleCode);
            //var moduleSql = new ModuleSql(moduleCode);
            if (module.IsNotEmptyOrNull())
            {
                var moduleColumnInfo = new ModuleSqlColumn(module.ModuleCode);

                var moduleColumns = moduleColumnInfo.GetModuleSqlColumn();
                if (moduleColumns.Any())
                {
                    #region 判断必填

                    #endregion

                    if (operateType == OperateType.Add)
                    {
                        #region 自动编号
                        var autoCodes = moduleColumns.Where(x => x.HideInForm == false && x.IsAutoCode == true).ToList();
                        if (autoCodes.Any())
                            for (int i = 0; i < autoCodes.Count; i++)
                            {
                                if (autoCodes[i].DataSource.IsNotEmptyOrNull())
                                {
                                    var no = Utility.GenerateContinuousSequence(autoCodes[i].DataSource);
                                    entity.SetPropertyValue(autoCodes[i].DataIndex, no);
                                }
                            }
                        #endregion

                        #region 判断唯一性
                        var uniques = moduleColumns.Where(x => x.HideInForm == false && x.IsUnique == true).ToList();
                        if (uniques.Any())
                            for (int i = 0; i < uniques.Count; i++)
                            {
                                var value = entity.GetPropertyValue(uniques[i].DataIndex);
                                CheckCodeExist(tableName, uniques[i].DataIndex, value, id != null ? ModifyType.Edit : ModifyType.Add, uniques[i].Title, id);
                            }
                        #endregion
                    }
                }
            }
        }
    }

    /// <summary>
    /// 验证表单
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="id">主键ID</param>
    public static void CheckForm(ISqlSugarClient _Db, string moduleCode, Dictionary<string, object> dict, OperateType operateType = OperateType.Add, Guid? id = null)
    {
        var module = ModuleInfo.GetModuleInfo(moduleCode);
        var moduleSql = new ModuleSql(moduleCode, _Db);
        string tableName = moduleSql.GetTableName();

        if (tableName == "SmModules")
            moduleCode = "SM_MODULE_MNG";
        if (!moduleCode.IsNull())
            if (!module.IsNull())
            {
                var moduleColumnInfo = new ModuleSqlColumn(module.ModuleCode);

                var moduleColumns = moduleColumnInfo.GetModuleSqlColumn();
                if (moduleColumns.Any())
                {

                    #region 判断必填

                    #endregion

                    #region 自动编号
                    if (operateType == OperateType.Add)
                    {
                        var autoCodes = moduleColumns.Where(x => x.HideInForm == false && x.IsAutoCode == true).ToList();
                        if (autoCodes.Any())
                            for (int i = 0; i < autoCodes.Count; i++)
                            {
                                if (autoCodes[i].DataSource.IsNotEmptyOrNull())
                                {
                                    var no = Utility.GenerateContinuousSequence(autoCodes[i].DataSource);
                                    SetFormDicValue(dict, autoCodes[i].DataIndex, no);
                                }
                            }
                    }
                    #endregion

                    #region 判断唯一性
                    var uniques = moduleColumns.Where(x => x.HideInForm == false && x.IsUnique == true).ToList();
                    if (uniques.Any())
                        for (int i = 0; i < uniques.Count; i++)
                        {
                            var value = GetFormDicValue(dict, uniques[i].DataIndex);
                            CheckCodeExist(tableName, uniques[i].DataIndex, value, id != null ? ModifyType.Edit : ModifyType.Add, uniques[i].Title, id);
                        }
                    #endregion
                }
            }
    }

    public static object GetFormDicValue(Dictionary<string, object> dict, string name)
    {
        object value = null;
        if (dict.ContainsKey(name)) // 检查键是否存在
            value = dict[name];
        return value;
    }

    public static void SetFormDicValue(Dictionary<string, object> dict, string name, object value)
    {
        if (dict.ContainsKey(name)) // 检查键是否存在
            dict[name] = value;
    }

    /// <summary>
    /// 检查表中是否已经存在相同代码的数据
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="fieldName">字段名</param>
    /// <param name="fieldValue">字段值</param>
    /// <param name="modifyType">ModifyType.Add,ModifyType.Edit</param>
    /// <param name="rowid">ModifyType.Edit时修改记录的ROW_ID值</param>
    /// <param name="promptName">判断栏位的提示名称</param>
    public static void CheckCodeExist(string tableName, string fieldName, object fieldValue, ModifyType modifyType, string promptName, Guid? rowid = null) => CheckCodeExist(tableName, fieldName, fieldValue, modifyType, rowid, promptName, null);

    /// <summary>
    /// 检查表中是否已经存在相同代码的数据
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="fieldName">字段名</param>
    /// <param name="fieldValue">字段值</param>
    /// <param name="whereCondition">条件</param>
    /// <param name="modifyType">ModifyType.Add,ModifyType.Edit</param>
    /// <param name="rowid">ModifyType.Edit时修改记录的ROW_ID值</param>
    /// <param name="promptName">判断栏位的提示名称</param>
    /// <param name="whereCondition">Where后的条件，如：IS_ALCON='Y'</param>
    public static bool CheckCodeExist(string tableName, string fieldName, object fieldValue, ModifyType modifyType, Guid? rowid, string promptName, string whereCondition)
    {
        try
        {
            bool result = false;
            if (modifyType == ModifyType.Add)
            {
                string sql = string.Empty;
                sql = "SELECT COUNT(*) FROM " + tableName + " WHERE " + fieldName + "='" + fieldValue + "' AND IsDeleted='false'";

                if (!string.IsNullOrEmpty(whereCondition))
                    sql += " AND " + whereCondition;

                int count = Convert.ToInt32(DBHelper.ExecuteScalar(sql));
                if (count > 0)
                {
                    result = true;
                    throw new Exception(string.Format("{0}【{1}】已经存在！", promptName, fieldValue));
                }
                else
                    result = false;

            }
            else if (modifyType == ModifyType.Edit)
            {
                string sql = string.Empty;
                sql = "SELECT COUNT(*) FROM " + tableName + " WHERE " + fieldName + "='" + fieldValue + "' AND IsDeleted='false' AND ID!='" + rowid.Value + "'";

                if (!string.IsNullOrEmpty(whereCondition))
                    sql += " AND " + whereCondition;

                int count = Convert.ToInt32(DBHelper.ExecuteScalar(sql));
                if (count > 0)
                {
                    result = true;
                    throw new Exception(string.Format("{0}【{1}】已经存在！", promptName, fieldValue));
                }
                else
                    result = false;
            }
            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }


    public ServiceResult<T> Success<T>(string message = ResponseText.QUERY_SUCCESS)
    {
        return new ServiceResult<T>() { Success = true, Message = message, Data = default };
    }
    public ServiceResult<T> Success<T>(T data, string message = ResponseText.QUERY_SUCCESS)
    {
        return new ServiceResult<T>() { Success = true, Message = message, Data = data };
    }
    public ServiceResult Success(string message = "成功")
    {
        return new ServiceResult() { Success = true, Message = message, Data = null };
    }

    public ServiceResult<T> Failed<T>(T data, string message = ResponseText.QUERY_SUCCESS)
    {
        return new ServiceResult<T>() { Success = false, Message = message, Data = data };
    }
    public ServiceResult Failed(string message = "失败", int status = 500)
    {
        return new ServiceResult() { Success = false, Status = status, Message = message, Data = null };
    }

    public ServiceResult<T> Failed<T>(string message = "失败", int status = 500)
    {
        return new ServiceResult<T>() { Success = false, Status = status, Message = message, Data = default };
    }
    #endregion
}
