/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmModules.cs
*
*功 能： N / A
* 类 名： SmModules
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/20 23:12:40  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
using Newtonsoft.Json.Linq;

namespace EU.Core.Api.Controllers;

/// <summary>
/// 系统模板(Controller)
/// </summary>
[Route("api/SmModule")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SM)]
public class SmModulesController : BaseController<ISmModulesServices, SmModules, SmModulesDto, InsertSmModulesInput, EditSmModulesInput>
{
    public SmModulesController(ISmModulesServices service) : base(service)
    {
    }

    #region 获取左侧菜单

    /// <summary>
    /// 获取左侧菜单
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("GetMenuData")]
    public async Task<ServiceResult<List<TreeMenuData>>> GetMenuData() => await _service.GetMenuData();

    /// <summary>
    /// 获取左侧菜单
    /// </summary>
    /// <returns></returns>
    [HttpGet("GetAuthMenu")]
    public async Task<ServiceResult<List<TreeAuthMenu>>> GetAuthMenu() => await _service.GetAuthMenu();
    #endregion

    #region 获取模块信息
    /// <summary>
    /// 获取模块信息
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <returns></returns>
    [HttpGet, Route("GetModuleInfo")]
    public async Task<dynamic> GetModuleInfo(string moduleCode) => await _service.GetModuleInfo(moduleCode);

    /// <summary>
    /// 获取模块信息
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <returns></returns>
    [HttpGet, Route("GetModuleInfo/{moduleCode}")]

    public async Task<dynamic> GetModuleInfo1(string moduleCode) => await _service.GetModuleInfo(moduleCode);
    #endregion

    #region 获取模块表单列信息
    /// <summary>
    /// 获取模块表单列信息
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <returns></returns>
    [HttpGet, Route("FormColumn/{moduleCode}")]
    public ServicePageResult<SmModuleFormOption> GetModuleFormColumn(string moduleCode) => _service.GetModuleFormColumn(moduleCode);
    #endregion 

    #region 获取模块日志信息
    /// <summary>
    /// 获取模块日志信息
    /// </summary>
    /// <param name="moduleCode"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("GetModuleLogInfo")]
    public async Task<ServiceResult<dynamic>> GetModuleLogInfo(string moduleCode, string id) => await _service.GetModuleLogInfo(moduleCode, id);
    #endregion

    #region 导出模块SQL
    /// <summary>
    /// 导出模块SQL
    /// </summary>
    /// <param name="list">ids</param>
    /// <returns></returns>
    [HttpPost]
    [Route("ExportModuleSqlScript")]
    public async Task<ServiceResult<Guid>> ExportModuleSqlScript(List<SmModules> list) => await _service.ExportModuleSqlScript(list);
    #endregion

    #region App.js动态加载路由
    /// <summary>
    /// App.js动态加载路由
    /// </summary>
    /// <returns></returns>
    [HttpGet, Route("GetPatchRoutes")]
    public async Task<ServiceResult<List<TreeMenuData>>> GetPatchRoutes() => await _service.GetPatchRoutes();
    #endregion

    #region 更新模块表单列排序号
    /// <summary>
    /// 更新模块表单列排序号
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="columns">columns</param>
    /// <returns></returns>
    [HttpPut, Route("UpdateFormColumnTaxisNo/{moduleCode}")]
    public async Task<ServiceResult> UpdateFormColumnTaxisNo(string moduleCode, [FromBody] List<SmModuleColumn> columns) => await _service.UpdateFormColumnTaxisNoAsync(moduleCode, columns);
    #endregion

    #region 更新模块表单列
    /// <summary>
    /// 更新模块表单列
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="column">column</param>
    /// <returns></returns>
    [HttpPut, Route("UpdateFormColumn/{moduleCode}")]
    public async Task<ServiceResult> UpdateFormColumnAsync(string moduleCode, [FromBody] SmModuleFormOption column) => await _service.UpdateFormColumnAsync(moduleCode, column);
    #endregion

    #region 更新模块表格列排序号
    /// <summary>
    /// 更新模块表格列排序号
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="columns">columns</param>
    /// <returns></returns>
    [HttpPut, Route("UpdateTableColumnTaxisNo/{moduleCode}")]
    public async Task<ServiceResult> UpdateTableColumnTaxisNo(string moduleCode, [FromBody] List<SmModuleColumn> columns) => await _service.UpdateTableColumnTaxisNoAsync(moduleCode, columns);
    #endregion

    #region 更新模块表格列
    /// <summary>
    /// 更新模块表格列
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="column">column</param>
    /// <returns></returns>
    [HttpPut, Route("UpdateTableColumn/{moduleCode}")]
    public async Task<ServiceResult> UpdateTableColumnAsync(string moduleCode, [FromBody] SmModuleColumn column) => await _service.UpdateTableColumnAsync(moduleCode, column);
    #endregion

    #region 记录用户模块列
    /// <summary>
    /// 记录用户模块列
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <param name="param">columns</param>
    /// <returns></returns>
    [HttpPost("RecordUserModuleColumn/{moduleId}")]
    public async Task<ServiceResult> RecordUserMoudleColumn(Guid moduleId, [FromBody] JObject param) => await _service.RecordUserModuleColumn(moduleId, param);
    #endregion

    #region 复制模块
    /// <summary>
    /// 复制模块
    /// </summary>
    /// <param name="moduleId">模块ID</param> 
    /// <param name="module">模块</param> 
    /// <returns></returns>
    [HttpPost("Copy/{moduleId}")]
    public async Task<ServiceResult> CopyAsync(Guid moduleId, [FromBody] SmModules module) => await _service.CopyAsync(moduleId, module);
    #endregion 

}