using EU.Core.Common.Const;
using EU.Core.Tasks;

namespace EU.Core.Controllers;

/// <summary>
/// 公共服务
/// </summary>
[Produces("application/json")]
[Route("api/Common")]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_Other), GlobalActionFilter]
public class CommonController : Controller
{
    ICommonServices _service;
    public CommonController(ICommonServices service)
    {
        _service = service;
    }
    #region 自定义列模块数据返回
    /// <summary>
    /// 自定义列模块数据返回
    /// </summary>
    /// <param name="paramData">查询条件</param>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="sorter">排序</param>
    /// <param name="filter">过滤条件</param>
    /// <param name="parentColumn"></param>
    /// <param name="parentId"></param>
    /// <returns></returns>
    [HttpGet, Route("GetGridList")]
    public GridListReturn GetGridList(string paramData, string moduleCode, string sorter = "{}", string filter = "{}", string parentColumn = null, string parentId = null) => _service.GetGridList(paramData, moduleCode, sorter, filter, parentColumn, parentId);
    #endregion

    #region 清空缓存
    /// <summary>
    /// 清空缓存
    /// </summary>
    /// <returns></returns>
    [HttpGet, Route("ClearCache")]
    public ServiceResult ClearCache() => _service.ClearCache();
    #endregion

    #region Excel导出
    /// <summary>
    /// Excel导出
    /// </summary>
    /// <returns></returns>
    [HttpGet, Route("ExportExcel")]
    public ServiceResult<string> ExportExcel(string moduleCode, string paramData = "{}", string sorter = "{}", string exportExcelColumns = "") => _service.ExportExcel(moduleCode, paramData, sorter, exportExcelColumns);
    #endregion

    #region Excel导入
    /// <summary>
    /// Excel导入
    /// </summary>
    /// <param name="import"></param>
    /// <returns></returns>
    [HttpPost("ImportExcel")]
    public async Task<ServiceResult<ImportExcelResult>> ImportExcelAsync([FromForm] ImportExcelForm import) => await _service.ImportExcelAsync(import);
    #endregion

    #region Excel导入数据转换
    /// <summary>
    /// Excel导入数据转换
    /// </summary>
    /// <returns></returns>
    [HttpPost, Route("TransferExcelData")]
    public ServiceResult TransferExcelData([FromBody] TransferExcelRequest request) => _service.TransferExcelData(request);
    #endregion

    /// <summary>
    /// 获取通用下拉数据
    /// </summary>
    /// <param name="parentColumn"></param>
    /// <param name="parentId"></param>
    /// <param name="current"></param>
    /// <param name="pageSize"></param>
    /// <param name="code"></param>
    /// <param name="items"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    [HttpPost("ComboGridData")]
    public async Task<ServiceResult<List<ComboGridData>>> ComboGridData(string parentColumn, string parentId, int? current, int? pageSize, string code, [FromBody] string[] items, string key) => await _service.ComboGridData(parentColumn, parentId, current, pageSize, code, items, key);
    [HttpPost("GetComboGridData")]
    public async Task<ServiceResult<List<ComboGridData>>> GetComboGridData([FromBody] ComboGridDataBody body) => await _service.GetComboGridData(body);

    #region 增删查改

    [HttpGet("{moduleCode}/{id}")]
    public async Task<ServiceResult<object>> Query(string moduleCode, Guid id) => await _service.Query(moduleCode, id);

    [HttpPost("{moduleCode}")]
    public async Task<ServiceResult<Guid>> Add(string moduleCode, [FromBody] object entity) => await _service.Add(moduleCode, entity);

    [HttpPut("{moduleCode}/{id}")]
    public async Task<ServiceResult<Guid>> Update(string moduleCode, Guid id, [FromBody] object entity) => await _service.Update(moduleCode, id, entity);

    [HttpDelete("{moduleCode}/{id}")]
    public async Task<ServiceResult> Delete(string moduleCode, Guid id) => await _service.Delete(moduleCode, id);


    [HttpDelete("{moduleCode}")]
    public async Task<ServiceResult> Delete(string moduleCode, [FromBody] List<Guid> ids) => await _service.Delete(moduleCode, ids);
    #endregion

    [HttpGet("Test")]
    public ServiceResult Test()
    {
        for (int i = 0; i < 100; i++)
        {
            TaskMsg msg = new TaskMsg();

            msg.MsgId = Guid.NewGuid();
            msg.Time = DateTime.Now;
            RabbitMQHelper.SendMsg(RabbitMQConsts.CLIENT_ID_TASK_JOB, msg);
            Thread.Sleep(2000);
        }
        return ServiceResult.OprateSuccess(ResponseText.DELETE_SUCCESS);

    }
}