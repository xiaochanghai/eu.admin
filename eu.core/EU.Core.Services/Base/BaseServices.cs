using System.Linq.Expressions;
using System.Reflection;
using EU.Core.Common.Extensions;
using EU.Core.Common.UserManager;
using EU.Core.IServices.BASE;
using SqlSugar;

namespace EU.Core.Services.BASE;

/// <summary>
/// 增删改查基础服务
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TEntityDto"></typeparam>
/// <typeparam name="TInsertDto"></typeparam>
/// <typeparam name="TEditDto"></typeparam>
public class BaseServices<TEntity, TEntityDto, TInsertDto, TEditDto> : IBaseServices<TEntity, TEntityDto, TInsertDto, TEditDto> where TEntity : class, new()
{
    public BaseServices(IBaseRepository<TEntity> BaseDal = null)
    {
        this.BaseDal = BaseDal;
    }

    public IBaseRepository<TEntity> BaseDal { get; set; } //通过在子类的构造函数中注入，这里是基类，不用构造函数

    public ISqlSugarClient Db => BaseDal.Db;

    public DataContext _context;

    /// <summary>
    /// 用户信息
    /// </summary>
    public SmUsers UserInfo = UserContext.Current.UserInfo;

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid? UserId = UserContext.Current.User_Id;

    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId1 => UserId?.ToString();

    /// <summary>
    /// 公司ID
    /// </summary>
    public Guid? CompanyId = UserContext.Current.CompanyId;

    /// <summary>
    /// 集团ID
    /// </summary>
    public Guid? GroupId = UserContext.Current.GroupId;

    #region 写入数据

    /// <summary>
    /// 写入实体数据
    /// </summary>
    /// <param name="entity">实体类</param>
    /// <returns>主键ID</returns>
    public virtual async Task<Guid> Add(TInsertDto entity, Guid? id = null)
    {
        var entity1 = Mapper.Map(entity).ToANew<TEntity>();
        return await Add(entity1, id);
    }
    /// <summary>
    /// 写入实体数据
    /// </summary>
    /// <param name="entity">实体类</param>
    /// <returns>主键ID</returns>
    public virtual async Task<Guid> Add(TEntity entity, Guid? id = null)
    {
        CheckForm(entity, OperateType.Add);

        return await BaseDal.Add(entity, id);
    }
    /// <summary>
    /// 写入实体数据
    /// </summary>
    /// <param name="entity">实体类</param>
    /// <returns>主键ID</returns>
    public virtual async Task<Guid> Add(object entity)
    {
        var model = ConvertToEntity(entity);

        var dic = ConvertToDic(entity);
        var lstColumns = dic.Keys.Where(x => x != nameof(BasePoco.ID) && x != "Id").ToList();
        lstColumns.Add(nameof(BasePoco.AuditStatus));

        CheckForm(model, OperateType.Add);

        return await BaseDal.Add(model, lstColumns);
    }

    /// <summary>
    /// 批量插入实体(速度快)
    /// </summary>
    /// <param name="listEntity">实体集合</param>
    /// <returns>影响行数</returns>
    public virtual async Task<List<Guid>> Add(List<TInsertDto> listEntity)
    {
        var list = Mapper.Map(listEntity).ToANew<List<TEntity>>();

        list.ForEach(entity =>
        {
            CheckForm(entity, OperateType.Add);
        });
        return await BaseDal.Add(list);
    }

    /// <summary>
    /// 批量插入实体(速度快)
    /// </summary>
    /// <param name="listEntity">实体集合</param>
    /// <returns>影响行数</returns>
    public virtual async Task<List<Guid>> Add(List<TEntity> list)
    {
        list.ForEach(entity =>
        {
            CheckForm(entity, OperateType.Add);
        });
        return await BaseDal.Add(list);
    }
    #endregion

    #region 更新数据

    /// <summary>
    /// 更新实体数据
    /// </summary>
    /// <param name="Id">数据ID</param>
    /// <param name="editModel">实体类</param>
    /// <returns>true or false</returns>
    public async Task<bool> Update(Guid Id, TEditDto editModel)
    {
        if (editModel == null || !await AnyAsync(Id))
            return false;

        var entity = await Query(Id);
        ConvertTEditDto2TEntity(editModel, entity);

        CheckOnly(entity, Id);
        return await BaseDal.Update(entity);
    }

    public virtual async Task<bool> Update(Guid Id, object entity)
    {
        return await Update(Id, entity, null);
    }

    /// <summary>
    /// 更新数据
    /// </summary>
    /// <param name="Id">主键ID</param>
    /// <param name="entity">实体数据</param>
    /// <param name="lstColumns">只更新某列</param>
    /// <returns></returns>
    public virtual async Task<bool> Update(Guid Id, object entity, List<string> lstColumns)
    {
        var model = ConvertToEntity(entity);
        CheckOnly(model, Id);
        var dic = ConvertToDic(entity);
        var columns = dic.Keys.Where(x => x != "ID" && x != "Id").ToList();
        columns = lstColumns?.Any() == true ? lstColumns : columns;
        var result = await Update(model, columns, null).ConfigureAwait(false);


        //#region 回写修改次数
        //string sql = $"UPDATE {entityType.GetEntityTableName()} SET ModificationNum = isnull (ModificationNum, 0) + 1, Tag = 1 where ID='{Id}'";
        //await Db.Ado.ExecuteCommandAsync(sql);
        //#endregion

        return result;
    }

    public virtual async Task<TEntityDto> UpdateReturn(Guid Id, object entity)
    {
        var model = ConvertToEntity(entity);
        await Update(Id, entity);
        return Mapper.Map(model).ToANew<TEntityDto>();
    }

    /// <summary>
    /// 更新实体数据
    /// </summary>
    /// <param name="editModels">实体类</param>
    /// <returns>true or false</returns>
    public async Task<bool> Update(Dictionary<Guid, TEditDto> editModels)
    {
        List<TEntity> entities = new();
        foreach (var keyValuePairs in editModels)
        {
            if (keyValuePairs.Value == null || !BaseDal.Any(keyValuePairs.Key))
                continue;

            var entity = await Query(keyValuePairs.Key);

            ConvertTEditDto2TEntity(keyValuePairs.Value, entity);
            CheckOnly(entity, keyValuePairs.Key);
            entities.Add(entity);
        }

        return await BaseDal.Update(entities);
    }
    /// <summary>
    /// 更新实体数据
    /// </summary>
    /// <param name="entity">实体类</param>
    /// <returns></returns>
    public async Task<bool> Update(List<TEntity> listEntity) => await BaseDal.Update(listEntity);

    /// <summary>
    /// 根据model，更新，带where条件
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="where">条件</param>
    /// <returns></returns>
    public async Task<bool> Update(TEntity entity, string where) => await BaseDal.Update(entity, where);

    public async Task<bool> Update(object operateAnonymousObjects) => await BaseDal.Update(operateAnonymousObjects);

    /// <summary>
    /// 更新实体数据，指定列
    /// </summary>
    /// <param name="entitys">实体类</param>
    /// <param name="lstColumns">只更新某列</param>
    /// <param name="lstIgnoreColumns">不更新某列</param>
    /// <param name="where">where条件</param>
    /// <returns></returns>
    public async Task<bool> Update(TEntity entity, List<string> lstColumns = null, List<string> lstIgnoreColumns = null, string where = "") => await BaseDal.Update(entity, lstColumns, lstIgnoreColumns, where);
    #endregion

    #region 删除数据

    /// <summary>
    /// 根据实体删除一条数据
    /// </summary>
    /// <param name="entity">博文实体类</param>
    /// <returns></returns>
    public async Task<bool> Delete(TEntity entity) => await BaseDal.Delete(entity);
    /// <summary>
    /// 根据表达式，删除实体
    /// </summary>
    /// <param name="whereExpression">表达式</param>
    /// <returns></returns>
    public async Task<bool> Delete(Expression<Func<TEntity, bool>> whereExpression) => await BaseDal.Delete(whereExpression);

    /// <summary>
    /// 删除指定ID的数据
    /// </summary>
    /// <param name="id">主键ID</param>
    /// <returns></returns>
    public async Task<bool> DeleteById(object id) => await BaseDal.DeleteById(id);

    /// <summary>
    /// 删除指定ID的数据
    /// </summary>
    /// <param name="id">主键ID</param>
    /// <returns></returns>
    public virtual async Task<bool> Delete(object id)
    {
        return await Delete([Guid.Parse(id.ToString())]);
    }

    /// <summary>
    /// 删除指定ID集合的数据(批量删除)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public virtual async Task<bool> Delete(Guid[] ids)
    {
        var entities = new List<TEntity>();
        foreach (var id in ids)
        {
            if (!await AnyAsync(id))
                continue;

            var entity = await Query(id);

            var ent = entity as BasePoco;
            ent.IsDeleted = true;
            entities.Add(entity);
        }
        return await BaseDal.Update(entities, ["IsDeleted"]);
    }

    /// <summary>
    /// 删除指定ID集合的数据(批量删除)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public async Task<bool> DeleteByIds(object[] ids) => await BaseDal.DeleteByIds(ids);
    #endregion

    #region 查询数据

    #region 字典映射、全称、单位转换等
    /// <summary>
    /// 字典映射、全称、单位转换等
    /// </summary>
    /// <param name="view"></param>
    public virtual void SetLabel(TEntityDto view)
    {
    }
    #endregion

    #region 根据ID查询实体数据是否存在
    /// <summary>
    /// 根据ID查询实体数据是否存在
    /// </summary>
    /// <param name="objId">id（必须指定主键特性 [SugarColumn(IsPrimaryKey=true)]）</param>
    /// <returns>true or false</returns>
    public async Task<bool> AnyAsync(object objId) => await BaseDal.AnyAsync(objId);

    /// <summary>
    /// 查询实体数据是否存在
    /// </summary>
    /// <param name="whereExpression">条件表达式</param>
    /// <returns></returns>
    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> whereExpression)
    {
        return await BaseDal.AnyAsync(whereExpression);
    }
    #endregion

    #region 根据ID查询查询一条数据 
    /// <summary>
    /// 根据ID查询一条数据
    /// </summary>
    /// <param name="objId">id（必须指定主键特性 [SugarColumn(IsPrimaryKey=true)]），如果是联合主键，请使用Where条件</param>
    /// <returns>数据实体</returns>
    public virtual async Task<TEntityDto> QueryById(object objId)
    {
        return await QueryDto(objId, false);
    }
    /// <summary>
    /// 根据ID查询一条数据
    /// </summary>
    /// <param name="objId">id（必须指定主键特性 [SugarColumn(IsPrimaryKey=true)]），如果是联合主键，请使用Where条件</param>
    /// <param name="blnUseCache">是否使用缓存</param>
    /// <returns>数据实体Dto</returns>
    public virtual async Task<TEntityDto> QueryDto(object objId, bool blnUseCache = false)
    {
        var data = await Query(objId, blnUseCache);
        return Mapper.Map(data).ToANew<TEntityDto>();
    }
    /// <summary>
    /// 根据ID查询一条数据
    /// </summary>
    /// <param name="objId">id（必须指定主键特性 [SugarColumn(IsPrimaryKey=true)]），如果是联合主键，请使用Where条件</param>
    /// <param name="blnUseCache">是否使用缓存</param>
    /// <returns>数据实体</returns>
    public async Task<TEntity> Query(object objId, bool blnUseCache = false) => await BaseDal.QueryById(objId, blnUseCache);

    /// <summary>
    /// 根据ID查询数据
    /// </summary>
    /// <param name="lstIds">id列表（必须指定主键特性 [SugarColumn(IsPrimaryKey=true)]），如果是联合主键，请使用Where条件</param>
    /// <returns>数据实体列表</returns>
    public async Task<List<TEntityDto>> QueryByIDs(object[] lstIds)
    {
        var data = await BaseDal.QueryByIDs(lstIds);
        return Mapper.Map(data).ToANew<List<TEntityDto>>();
    }
    #endregion

    /// <summary>
    /// 查询所有数据
    /// </summary>
    /// <returns>数据列表</returns>
    public virtual async Task<List<TEntity>> Query() => await BaseDal.Query();

    /// <summary>
    /// 查询数据列表
    /// </summary>
    /// <param name="where">条件</param>
    /// <returns>数据列表</returns>
    public async Task<List<TEntity>> Query(string where) => await BaseDal.Query(where);

    /// <summary>
    /// 查询数据列表
    /// </summary>
    /// <param name="whereExpression">whereExpression</param>
    /// <returns>数据列表</returns>
    public async Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression) => await BaseDal.Query(whereExpression);

    /// <summary>
    /// 按照特定列查询数据列表
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="expression"></param>
    /// <returns></returns>
    public async Task<List<TResult>> Query<TResult>(Expression<Func<TEntity, TResult>> expression) => await BaseDal.Query(expression);

    /// <summary>
    /// 按照特定列查询数据列表带条件排序
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="whereExpression">过滤条件</param>
    /// <param name="expression">查询实体条件</param>
    /// <param name="orderByFileds">排序条件</param>
    /// <returns></returns>
    public async Task<List<TResult>> Query<TResult>(Expression<Func<TEntity, TResult>> expression, Expression<Func<TEntity, bool>> whereExpression, string orderByFileds) => await BaseDal.Query(expression, whereExpression, orderByFileds);

    /// <summary>
    /// 查询一个列表
    /// </summary>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="strOrderByFileds">排序字段，如name asc,age desc</param>
    /// <returns>数据列表</returns>
    public async Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> orderByExpression, bool isAsc = true) => await BaseDal.Query(whereExpression, orderByExpression, isAsc);

    public async Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression, string orderByFileds) => await BaseDal.Query(whereExpression, orderByFileds);

    /// <summary>
    /// 查询一个列表
    /// </summary>
    /// <param name="where">条件</param>
    /// <param name="orderByFileds">排序字段，如name asc,age desc</param>
    /// <returns>数据列表</returns>
    public async Task<List<TEntity>> Query(string where, string orderByFileds) => await BaseDal.Query(where, orderByFileds);

    /// <summary>
    /// 根据sql语句查询
    /// </summary>
    /// <param name="sql">完整的sql语句</param>
    /// <param name="parameters">参数</param>
    /// <returns>泛型集合</returns>
    public async Task<List<TEntity>> QuerySql(string sql, SugarParameter[] parameters = null) => await BaseDal.QuerySql(sql, parameters);


    /// <summary>
    /// 根据sql语句查询
    /// </summary>
    /// <param name="sql">完整的sql语句</param>
    /// <param name="parameters">参数</param>
    /// <returns>DataTable</returns>
    public async Task<DataTable> QueryTable(string sql, SugarParameter[] parameters = null) => await BaseDal.QueryTable(sql, parameters);

    /// <summary>
    /// 查询前N条数据
    /// </summary>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="top">前N条</param>
    /// <param name="orderByFileds">排序字段，如name asc,age desc</param>
    /// <returns>数据列表</returns>
    public async Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression, int top, string orderByFileds) => await BaseDal.Query(whereExpression, top, orderByFileds);

    /// <summary>
    /// 查询前N条数据
    /// </summary>
    /// <param name="where">条件</param>
    /// <param name="top">前N条</param>
    /// <param name="orderByFileds">排序字段，如name asc,age desc</param>
    /// <returns>数据列表</returns>
    public async Task<List<TEntity>> Query(string where, int top, string orderByFileds) => await BaseDal.Query(where, top, orderByFileds);

    /// <summary>
    /// 分页查询
    /// </summary>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="pageIndex">页码（下标0）</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="orderByFileds">排序字段，如name asc,age desc</param>
    /// <returns>数据列表</returns>
    public async Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression, int pageIndex, int pageSize, string orderByFileds) => await BaseDal.Query(whereExpression, pageIndex, pageSize, orderByFileds);

    /// <summary>
    /// 分页查询
    /// </summary>
    /// <param name="where">条件</param>
    /// <param name="pageIndex">页码（下标0）</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="orderByFileds">排序字段，如name asc,age desc</param>
    /// <returns>数据列表</returns>
    public async Task<List<TEntity>> Query(string where, int pageIndex, int pageSize, string orderByFileds) => await BaseDal.Query(where, pageIndex, pageSize, orderByFileds);

    public async Task<PageModel<TEntity>> QueryPage(Expression<Func<TEntity, bool>> whereExpression, int pageIndex = 1, int pageSize = 20, string orderByFileds = null) => await BaseDal.QueryPage(whereExpression, pageIndex, pageSize, orderByFileds);


    public async Task<ServicePageResult<TEntityDto>> QueryFilterPage([FromFilter] QueryFilter filter)
    {
        var data = await BaseDal.QueryFilterPage(filter);

        var data1 = Mapper.Map(data.Data).ToANew<List<TEntityDto>>();
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

    public async Task<List<TResult>> QueryMuch<T, T2, T3, TResult>(Expression<Func<T, T2, T3, object[]>> joinExpression, Expression<Func<T, T2, T3, TResult>> selectExpression, Expression<Func<T, T2, T3, bool>> whereLambda = null) where T : class, new()
    {
        return await BaseDal.QueryMuch(joinExpression, selectExpression, whereLambda);
    }

    public async Task<PageModel<TEntity>> QueryPage(PaginationModel pagination) => await QueryPage(DynamicLinqFactory.CreateLambda<TEntity>(pagination.Conditions), pagination.PageIndex, pagination.PageSize, pagination.OrderByFileds);

    /// <summary>
    /// 查询数据
    /// </summary>
    /// <param name="whereExpression">whereExpression</param>
    /// <returns>查询数据</returns>
    public async Task<TEntity> QuerySingle(Expression<Func<TEntity, bool>> whereExpression)
    {
        var list = await BaseDal.Query(whereExpression);
        return list.Any() ? list.FirstOrDefault() : default;
    }

    #region 分表

    public async Task<List<long>> AddSplit(TEntity entity) => await BaseDal.AddSplit(entity);

    public async Task<bool> UpdateSplit(TEntity entity, DateTime dateTime) => await BaseDal.UpdateSplit(entity, dateTime);

    /// <summary>
    /// 根据实体删除一条数据
    /// </summary>
    /// <param name="entity">博文实体类</param>
    /// <returns></returns>
    public async Task<bool> DeleteSplit(TEntity entity, DateTime dateTime) => await BaseDal.DeleteSplit(entity, dateTime);

    public async Task<TEntity> QueryByIdSplit(object objId) => await BaseDal.QueryByIdSplit(objId);

    public async Task<PageModel<TEntity>> QueryPageSplit(Expression<Func<TEntity, bool>> whereExpression, DateTime beginTime, DateTime endTime, int pageIndex = 1, int pageSize = 20, string orderByFields = null) => await BaseDal.QueryPageSplit(whereExpression, beginTime, endTime, pageIndex, pageSize, orderByFields);

    #endregion

    #endregion

    #region 审核数据
    /// <summary>
    /// 审核指定ID的数据
    /// </summary>
    /// <param name="id">主键ID</param>
    /// <returns></returns>
    public virtual async Task<bool> Audit(object id) => await BulkAudit([Guid.Parse(id.ToString())]);

    /// <summary>
    /// 审核指定ID集合的数据(批量审核)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public virtual async Task<bool> BulkAudit(Guid[] ids) => await BulkAudit(ids, null);

    /// <summary>
    /// 审核指定ID集合的数据(批量审核)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public virtual async Task<bool> BulkAudit(Guid[] ids, string where = null)
    {
        List<TEntity> entities = new();
        foreach (var id in ids)
        {
            if (!await AnyAsync(id))
                continue;

            var entity = await Query(id);

            var ent = entity as BasePoco;
            if (ent.AuditStatus == "Add")
            {
                ent.AuditStatus = "CompleteAudit";
                entities.Add(entity);
            }
        }
        await BaseDal.Update(entities, ["AuditStatus"], null, where);
        return true;
    }
    #endregion

    #region 撤销数据
    /// <summary>
    /// 撤销指定ID的数据
    /// </summary>
    /// <param name="id">主键ID</param>
    /// <returns></returns>
    public virtual async Task<bool> Revocation(object id) => await BulkRevocation([Guid.Parse(id.ToString())]);

    /// <summary>
    /// 撤销指定ID集合的数据(批量撤销)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public virtual async Task<bool> BulkRevocation(Guid[] ids)
    {
        List<TEntity> entities = new();
        foreach (var id in ids)
        {
            if (!await AnyAsync(id))
                continue;

            var entity = await Query(id);

            var ent = entity as BasePoco;
            if (ent.AuditStatus == "CompleteAudit")
            {
                ent.AuditStatus = "Add";
                entities.Add(entity);
            }
        }
        await BaseDal.Update(entities, ["AuditStatus"]);
        return true;
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
    public static void CheckForm(string moduleCode, Dictionary<string, object> dict, OperateType operateType = OperateType.Add, Guid? id = null)
    {
        var module = ModuleInfo.GetModuleInfo(moduleCode);
        var moduleSql = new ModuleSql(moduleCode);
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


    public ServiceResult<T> Success<T>(string message = "查询成功")
    {
        return new ServiceResult<T>() { Success = true, Message = message, Data = default };
    }
    public ServiceResult<T> Success<T>(T data, string message = "查询成功")
    {
        return new ServiceResult<T>() { Success = true, Message = message, Data = data };
    }
    public ServiceResult Success(string message = "成功")
    {
        return new ServiceResult() { Success = true, Message = message, Data = null };
    }

    public ServiceResult<T> Failed<T>(T data, string message = "查询成功")
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