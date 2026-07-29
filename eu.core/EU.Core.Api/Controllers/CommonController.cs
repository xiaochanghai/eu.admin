using EU.Core.Common.Const;
using EU.Core.Common.Seed;
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
    private readonly MyContext _myContext;

    private readonly string _webRootPath;

    public CommonController(ICommonServices service, IWebHostEnvironment webHostEnvironment, MyContext myContext)
    {
        _myContext = myContext;
        _service = service;
        _webRootPath = webHostEnvironment.WebRootPath;
    }
    #region 自定义列模块数据返回
    /// <summary>
    /// 自定义列模块数据返回
    /// </summary>
    /// <param name="filter">filter</param>
    /// <param name="moduleCode">模块代码</param>
    /// <returns></returns>
    [HttpGet("QueryByFilter/{moduleCode}")]
    public async Task<GridListReturn> QueryByFilter([FromFilter] QueryFilter filter, string moduleCode) => await _service.QueryByFilter(filter, moduleCode);

    #endregion

    #region 清空缓存
    /// <summary>
    /// 清空缓存
    /// </summary>
    /// <returns></returns>
    [HttpGet("ClearCache")]
    public async Task<ServiceResult> ClearCache() => await _service.ClearCache();
    #endregion

    #region Excel导出
    /// <summary>
    /// Excel导出
    /// </summary>
    /// <param name="filter">filter</param>
    /// <param name="moduleCode">模块代码</param>
    /// <returns></returns>
    [HttpGet("ExportExcel/{moduleCode}")]
    public async Task<ServiceResult<Guid>> ExportExcelAsync([FromFilter] QueryFilter filter, string moduleCode) => await _service.ExportExcelAsync(filter, moduleCode);
    #endregion

    #region Excel导入
    /// <summary>
    /// Excel导入
    /// </summary>
    /// <param name="import">导入数据</param>
    /// <param name="moduleCode">模块代码</param>
    /// <returns></returns>
    [HttpPost("ImportExcel/{moduleCode}")]
    public async Task<ServiceResult<ImportExcelResult>> ImportExcelAsync([FromForm] ImportExcelForm import, string moduleCode) => await _service.ImportExcelAsync(import, moduleCode);
    #endregion

    #region 获取Excel导入结果
    /// <summary>
    /// 获取Excel导入结果
    /// </summary>
    /// <param name="importDataId">导入数据ID</param>
    /// <param name="templateId">模板ID</param>
    /// <returns>ImportExcelResult</returns>
    [HttpGet("QueryImportExcelResult/{importDataId}/{templateId}")]
    public async Task<ServiceResult<ImportExcelResult>> QueryImportExcelResultAsync(Guid importDataId, Guid templateId) => await _service.QueryImportExcelResultAsync(importDataId, templateId);
    #endregion

    #region Excel导入数据转换
    /// <summary>
    /// Excel导入数据转换
    /// </summary>
    /// <param name="request">请求数据</param> 
    /// <returns></returns>
    [HttpPost("TransferExcelData")]
    public Task<ServiceResult> TransferExcelData([FromBody] TransferExcelRequest request) => _service.TransferExcelData(request);
    #endregion

    #region 获取通用下拉数据
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
    #endregion

    #region 增删查改
    /// <summary>
    /// 查询数据
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="id">主键ID</param>
    /// <returns></returns>
    [HttpGet("{moduleCode}/{id}")]
    public async Task<ServiceResult<object>> Query(string moduleCode, Guid id) => await _service.Query(moduleCode, id);

    /// <summary>
    /// 新增数据
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="entity">数据</param>
    /// <returns></returns>
    [HttpPost("{moduleCode}")]
    public async Task<ServiceResult<Guid>> Add(string moduleCode, [FromBody] object entity) => await _service.Add(moduleCode, entity);

    /// <summary>
    /// 更新数据
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="id">主键ID</param>
    /// <param name="entity">数据</param>
    /// <returns></returns>
    [HttpPut("{moduleCode}/{id}")]
    public async Task<ServiceResult<Guid>> Update(string moduleCode, Guid id, [FromBody] object entity) => await _service.Update(moduleCode, id, entity);

    /// <summary>
    /// 删除数据
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="id">主键ID</param>
    /// <returns></returns>
    [HttpDelete("{moduleCode}/{id}")]
    public async Task<ServiceResult> Delete(string moduleCode, Guid id) => await _service.Delete(moduleCode, id);

    /// <summary>
    /// 批量删除数据
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="ids">主键ID集</param>
    /// <returns></returns>
    [HttpDelete("{moduleCode}")]
    public async Task<ServiceResult> Delete(string moduleCode, [FromBody] List<Guid> ids) => await _service.Delete(moduleCode, ids);
    #endregion

    #region 生成所有表实体
    /// <summary>
    /// 生成所有表实体
    /// </summary>
    /// <returns></returns>
    [HttpGet("GenerateAllEntity"), AllowAnonymous]
    public ServiceResult GenerateAllEntity()
    {

        DBSeed.GenerateAllEntity(_myContext);

        return ServiceResult.OprateSuccess(ResponseText.DELETE_SUCCESS);

    }
    #endregion

    #region 生成所有表实体
    /// <summary>
    /// 生成所有表实体
    /// </summary>
    /// <returns></returns>
    [HttpGet("SyncData"), AllowAnonymous]
    public ServiceResult SyncData()
    {

        DBSeed.SyncData(_myContext);

        return ServiceResult.OprateSuccess(ResponseText.DELETE_SUCCESS);

    }
    #endregion

    #region 测试
    [HttpGet("InitLanguage"), AllowAnonymous]
    public async Task<ServiceResult> InitLanguage()
    {
        var moduleId = await _service.Db.Queryable<SmModules>().Select(x => x.ID).ToListAsync();
        var colums = await _service.Db.Queryable<SmModuleColumn>()
            .Where(x => x.SmModuleId != null && moduleId.Contains(x.SmModuleId.Value)).ToListAsync();

        var fields = new List<string>
        {
            "Title",
            "FormTitle"
        };
        for (int i = 0; i < colums.Count; i++)
        {
            var refId = colums[i].ID;
            var refType = "ModuleColumn";

            var configs = new List<SmLanguageConfig>();

            for (int j = 0; j < fields.Count; j++)
            {
                var refField = fields[j];

                var config = await _service.Db.Queryable<SmLanguageConfig>()
                    .Where(x => x.RefId == refId && x.RefType == refType && x.RefField == refField)
                    .AnyAsync();

                if (!config)
                {
                    string valueZH = colums[i].Title;
                    if (colums[i].ColumnMode.IsNullOrEmpty())
                    {
                        var config1 = new SmLanguageConfig()
                        {
                            RefId = refId,
                            RefType = refType,
                            RefField = refField,
                            Value_ZH = refField == "Title" ? colums[i].Title : colums[i].FormTitle,
                            Value_EN = colums[i].DataIndex
                        };
                        configs.Add(config1);

                        //config1 = new SmLanguageConfig()
                        //{
                        //    RefId = refId,
                        //    RefType = refType,
                        //    RefField = refField,
                        //    Value_ZH = colums[i].FormTitle,
                        //    Value_EN = colums[i].DataIndex
                        //};
                        //configs.Add(config1);
                    }
                    else if (colums[i].ColumnMode == "list")
                    {
                        valueZH = colums[i].FormTitle;

                        var config1 = new SmLanguageConfig()
                        {
                            RefId = refId,
                            RefType = refType,
                            RefField = refField,
                            Value_ZH = valueZH,
                            Value_EN = colums[i].DataIndex
                        };
                        configs.Add(config1);

                    }
                    else if (colums[i].ColumnMode == "form")
                    {
                        valueZH = colums[i].FormTitle;

                        var config1 = new SmLanguageConfig()
                        {
                            RefId = refId,
                            RefType = refType,
                            RefField = refField,
                            Value_ZH = valueZH,
                            Value_EN = colums[i].DataIndex
                        };
                        configs.Add(config1);

                    }

                }
            }

            await _service.Db.Insertable(configs).ExecuteCommandAsync();

        }
        return ServiceResult.OprateSuccess(ResponseText.DELETE_SUCCESS);

    }
    #endregion

    #region 测试
    [HttpGet("Test"), AllowAnonymous]
    public async Task<ServiceResult> Test()
    {
        for (int i = 0; i < 10; i++)
        {
            TaskMsg msg = new TaskMsg();

            msg.MsgId = Guid.NewGuid();
            msg.Time = DateTime.Now;
            RabbitMQHelper.SendMsg(RabbitMQConsts.CLIENT_ID_TASK_JOB, msg);
            Thread.Sleep(2000);
        }
        //DBHelper.ExecuteDML("UPDATE  SmModules set UpdateTime=getdate() where ID='402d1606-286a-47ec-8e45-346a12450e9a'");

        //var aa = Guid.NewGuid().ToString("N");
        //var aa1 = Guid.NewGuid();
        //DBSeed.MigrationLogs1(_myContext);


        return ServiceResult.OprateSuccess(ResponseText.DELETE_SUCCESS);

    }
    #endregion




    #region 测试
    [HttpGet("SyncData/{tableName}"), AllowAnonymous]
    public async Task<ServiceResult> SyncDataTable(string tableName)
    {
        //for (int i = 0; i < 100; i++)
        //{
        //    TaskMsg msg = new TaskMsg();

        //    msg.MsgId = Guid.NewGuid();
        //    msg.Time = DateTime.Now;
        //    RabbitMQHelper.SendMsg(RabbitMQConsts.CLIENT_ID_TASK_JOB, msg);
        //    Thread.Sleep(2000);
        //}
        //DBHelper.ExecuteDML("UPDATE  SmModules set UpdateTime=getdate() where ID='402d1606-286a-47ec-8e45-346a12450e9a'");

        //var aa = Guid.NewGuid().ToString("N");
        //var aa1 = Guid.NewGuid();
        DBSeed.SyncData(_myContext, tableName);


        return ServiceResult.OprateSuccess(ResponseText.DELETE_SUCCESS);

    }
    #endregion
}