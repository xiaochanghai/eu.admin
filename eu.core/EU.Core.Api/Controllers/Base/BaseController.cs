using System.Reflection;
using EU.Core.Common.Const;

namespace EU.Core.Controllers;

/// <summary>
/// 增删改查基础服务
/// </summary>
/// <typeparam name="IServiceBase"></typeparam>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TEntityDto"></typeparam>
/// <typeparam name="TInsertDto"></typeparam>
/// <typeparam name="TEditDto"></typeparam>
[Route("api/[controller]")]
public class BaseController<IServiceBase, TEntity, TEntityDto, TInsertDto, TEditDto> : Controller
{
    #region 初始化
    protected IServiceBase _service;
    /// <summary>
    /// 初始化 (注入)
    /// </summary>
    public BaseController(IServiceBase service)
    {
        _service = service;
    }
    #endregion


    #region 基础接口

    #region 查询
    /// <summary>
    /// 根据条件查询数据
    /// </summary>
    /// <param name="filter">条件</param>
    /// <returns></returns>
    [HttpGet]
    public virtual async Task<ServicePageResult<TEntityDto>> QueryByFilter([FromFilter] QueryFilter filter)
    {
        var data = (await InvokeServiceAsync("QueryFilterPage", [filter])) as ServicePageResult<TEntityDto>;
        return data;
    }

    /// <summary>
    ///  根据Id查询数据
    /// </summary>
    /// <param name="Id">主键ID</param>
    /// <returns></returns>
    [HttpGet("{Id}")]
    public virtual async Task<ServiceResult<object>> QueryById(Guid Id)
    {
        var entity = await InvokeServiceAsync("QueryById", [Id]);
        if (entity == null)
            return Failed<object>(ResponseText.QUERY_FAIL);
        else
            return Success(entity, ResponseText.QUERY_SUCCESS);
    }
    /// <summary>
    /// 查询数据
    /// </summary>
    /// <param name="paramData"></param>
    /// <param name="sorter"></param>
    /// <param name="filter"></param>
    /// <param name="parentColumn"></param>
    /// <param name="parentId"></param>
    /// <param name="moduleCode"></param>
    /// <returns></returns>
    [HttpGet, Route("GetPageList")]
    public async Task<IActionResult> GetPageList(string paramData = "{}", string sorter = "{}", string filter = "{}", string parentColumn = null, string parentId = null, string moduleCode = null)
    {
        var data = (await InvokeServiceAsync("GetPageList", [paramData, sorter, filter, parentColumn, parentId, moduleCode])) as dynamic;
        return Ok(data);
    }
    #endregion

    #region 新增
    /// <summary>
    /// 新增数据
    /// </summary>
    /// <param name="insertModel"></param>
    /// <returns></returns>
    [HttpPost]
    public virtual async Task<ServiceResult<string>> Insert([FromBody] object insertModel)
    {
        var data = Success<string>(null, ResponseText.INSERT_SUCCESS);
        var id = Convert.ToString(await InvokeServiceAsync("Add", [insertModel]));
        data.Success = id != Guid.Empty.ToString();
        if (data.Success)
            data.Data = id;
        else
            return Failed<string>(ResponseText.INSERT_FAIL);

        return data;
    }

    /// <summary>
    /// 批量新增数据
    /// </summary>
    /// <param name="insertModels"></param>
    [HttpPost, Route("BulkInsert")]
    public virtual async Task<ServiceResult<List<Guid>>> BulkInsert([FromBody] List<TInsertDto> insertModels)
    {
        var data = Success<List<Guid>>(null, ResponseText.INSERT_SUCCESS);
        var ids = await InvokeServiceAsync("Add", [insertModels]) as List<Guid>;
        data.Success = ids.Any();
        if (data.Success)
            data.Data = ids;
        else
            return Failed<List<Guid>>(ResponseText.INSERT_FAIL);

        return data;
    }
    #endregion

    #region 更新
    /// <summary>
    /// 更新数据
    /// </summary>
    /// <param name="Id">主键ID</param>
    /// <param name="editModel"></param>
    /// <returns></returns>
    [HttpPut("{Id}")]
    public virtual async Task<ServiceResult> Put(Guid Id, [FromBody] dynamic editModel)
    {
        var data = Success(ResponseText.UPDATE_SUCCESS);
        var flag = Convert.ToBoolean(await InvokeServiceAsync("Update", [Id, editModel]));
        if (!flag)
            return Failed(ResponseText.UPDATE_FAIL);
        return data;
    }

    /// <summary>
    /// 更新数据
    /// </summary>
    /// <param name="Id">主键ID</param>
    /// <param name="editModel"></param>
    /// <returns></returns>
    [HttpPut("UpdateReturn/{Id}")]
    public virtual async Task<ServiceResult<object>> UpdateReturn(Guid Id, [FromBody] dynamic editModel)
    {
        var result = await InvokeServiceAsync("UpdateReturn", [Id, editModel]) as object;
        if (result is null)
            return Failed<object>(ResponseText.UPDATE_FAIL);
        var data = Success<object>(result, ResponseText.UPDATE_SUCCESS);
        return data;
    }

    #endregion

    #region 删除
    /// <summary>
    /// 删除数据
    /// </summary>
    /// <param name="Id">主键ID</param>
    /// <returns></returns>
    [HttpDelete("{Id}")]
    public virtual async Task<ServiceResult> Delete(Guid Id)
    {
        var data = Success(ResponseText.DELETE_SUCCESS);
        var isExist = Convert.ToBoolean(await InvokeServiceAsync("AnyAsync", [Id]));
        if (!isExist)
            return Failed(ResponseText.DELETE_FAIL);
        data.Success = Convert.ToBoolean(await InvokeServiceAsync("Delete", [Id]));
        if (!data.Success)
            return Failed(ResponseText.DELETE_FAIL);
        return data;
    }

    /// <summary>
    /// 批量删除数据
    /// </summary>
    /// <param name="Ids">主键IDs</param>
    /// <returns></returns>
    [HttpDelete]
    public virtual async Task<ServiceResult> BulkDelete([FromBody] Guid[] Ids)
    {
        var data = Success(ResponseText.DELETE_SUCCESS);
        data.Success = Convert.ToBoolean(await InvokeServiceAsync("Delete", [Ids]));
        if (!data.Success)
            return Failed(ResponseText.DELETE_FAIL);
        return data;
    }
    #endregion

    #region 审核
    /// <summary>
    ///审核数据
    /// </summary>
    /// <param name="Id">主键ID</param>
    /// <returns></returns>
    [HttpPut("Audit/{Id}")]
    public virtual async Task<ServiceResult> Audit(Guid Id)
    {
        var data = Success(ResponseText.AUDIT_SUCCESS);
        var isExist = Convert.ToBoolean(await InvokeServiceAsync("AnyAsync", [Id]));
        if (!isExist)
            return Failed(ResponseText.AUDIT_FAIL);
        data.Success = Convert.ToBoolean(await InvokeServiceAsync("Audit", [Id]));
        if (!data.Success)
            return Failed(ResponseText.AUDIT_FAIL);
        return data;
    }

    /// <summary>
    /// 审核删除数据
    /// </summary>
    /// <param name="Ids">主键IDs</param>
    /// <returns></returns>
    [HttpPut("BulkAudit")]
    public virtual async Task<ServiceResult> BulkAudit([FromBody] Guid[] Ids)
    {
        var data = Success(ResponseText.AUDIT_SUCCESS);
        data.Success = Convert.ToBoolean(await InvokeServiceAsync("BulkAudit", [Ids]));
        if (!data.Success)
            return Failed(ResponseText.AUDIT_FAIL);
        return data;
    }
    #endregion

    #region 撤销
    /// <summary>
    /// 撤销数据
    /// </summary>
    /// <param name="Id">主键ID</param>
    /// <returns></returns>
    [HttpPut("Revocation/{Id}")]
    public virtual async Task<ServiceResult> Revocation(Guid Id)
    {
        var data = Success(ResponseText.REVOCATION_SUCCESS);
        var isExist = Convert.ToBoolean(await InvokeServiceAsync("AnyAsync", [Id]));
        if (!isExist)
            return Failed(ResponseText.REVOCATION_FAIL);
        data.Success = Convert.ToBoolean(await InvokeServiceAsync("Revocation", [Id]));
        if (!data.Success)
            return Failed(ResponseText.REVOCATION_FAIL);
        return data;
    }

    /// <summary>
    /// 批量撤销数据
    /// </summary>
    /// <param name="Ids">主键IDs</param>
    /// <returns></returns>
    [HttpPut("BulkRevocation")]
    public virtual async Task<ServiceResult> BulkRevocation([FromBody] Guid[] Ids)
    {
        var data = Success(ResponseText.REVOCATION_SUCCESS);
        data.Success = Convert.ToBoolean(await InvokeServiceAsync("BulkRevocation", [Ids]));
        if (!data.Success)
            return Failed(ResponseText.REVOCATION_FAIL);
        return data;
    }
    #endregion
    #endregion

    /// <summary>
    /// 反射调用service方法
    /// </summary>
    /// <param name="methodName"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    [NonAction]
    private object InvokeService(string methodName, object[] parameters)
    {
        return _service.GetType().GetMethod(methodName).Invoke(_service, parameters);
    }
    /// <summary>
    /// 反射调用service方法
    /// </summary>
    /// <param name="methodName"></param>
    /// <param name="types">为要调用重载的方法参数类型：new Type[] { typeof(SaveDataModel)</param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    [NonAction]
    private object InvokeService(string methodName, Type[] types, object[] parameters) => _service.GetType().GetMethod(methodName, types).Invoke(_service, parameters);


    [NonAction]
    private async Task<object> InvokeServiceAsync(string methodName, object[] parameters)
    {
        var task = _service.GetType().InvokeMember(methodName, BindingFlags.InvokeMethod, null, _service, parameters) as Task;
        if (task != null) await task;
        var result = task?.GetType().GetProperty("Result")?.GetValue(task);
        return result;
    }

    [NonAction]
    public ServiceResult<T> Success<T>(T data, string message = "成功")
    {
        return new ServiceResult<T>() { Success = true, Message = message, Data = data, };
    }

    // [NonAction]
    //public ServiceResult<T> Success<T>(T data, string msg = "成功",bool success = true)
    //{
    //    return new ServiceResult<T>()
    //    {
    //        success = success,
    //        msg = msg,
    //        response = data,
    //    };
    //}
    [NonAction]
    public ServiceResult Success(string message = "成功")
    {
        return new ServiceResult() { Success = true, Message = message, Data = null, };
    }

    [NonAction]
    public ServiceResult Failed(string message = "失败", int status = 500)
    {
        return new ServiceResult() { Success = false, Status = status, Message = message, Data = null, };
    }

    [NonAction]
    public ServiceResult<T> Failed<T>(string message = "失败", int status = 500)
    {
        return new ServiceResult<T>() { Success = false, Status = status, Message = message, Data = default, };
    }
}