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
*│　版权所有：SahHsiao                                │
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
    [HttpGet("GetMenuData")]
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
    [HttpGet("GetModuleInfo")]
    public async Task<dynamic> GetModuleInfo(string moduleCode) => await _service.GetModuleInfo(moduleCode);

    /// <summary>
    /// 获取模块信息
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <returns></returns>
    [HttpGet("GetModuleInfo/{moduleCode}")]

    public async Task<dynamic> GetModuleInfo1(string moduleCode) => await _service.GetModuleInfo(moduleCode);
    #endregion

    #region 获取模块表单列信息
    /// <summary>
    /// 获取模块表单列信息
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <returns></returns>
    [HttpGet("FormColumn/{moduleCode}")]
    public ServicePageResult<SmModuleFormOption> GetModuleFormColumn(string moduleCode) => _service.GetModuleFormColumn(moduleCode);
    #endregion 

    #region 获取模块日志信息
    /// <summary>
    /// 获取模块日志信息
    /// </summary>
    /// <param name="moduleCode"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("GetModuleLogInfo")]
    public async Task<ServiceResult<dynamic>> GetModuleLogInfo(string moduleCode, string id) => await _service.GetModuleLogInfo(moduleCode, id);
    #endregion

    #region 导出模块SQL
    /// <summary>
    /// 导出模块SQL
    /// </summary>
    /// <param name="ids">模块ID列表</param>
    /// <returns></returns>
    [HttpPost("ExportSqlScript")]
    public async Task<ServiceResult<Guid>> ExportModuleSqlScript(List<Guid> ids) => await _service.ExportModuleSqlScript(ids);
    #endregion

    #region 更新模块表单列排序号
    /// <summary>
    /// 更新模块表单列排序号
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="columns">栏位列表</param>
    /// <param name="type">类型（list、form）</param>
    /// <returns></returns>
    [HttpPut("UpdateTaxisNo/{moduleCode}/{type}")]
    public async Task<ServiceResult> UpdateTaxisNoAsync(string moduleCode, [FromBody] List<SmModuleColumn> columns, string type) => await _service.UpdateTaxisNoAsync(moduleCode, columns, type);
    #endregion

    #region 更新模块表单列
    /// <summary>
    /// 更新模块表单列
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="column">栏位</param>
    /// <param name="type">类型（list、form）</param>
    /// <returns></returns>
    [HttpPut("UpdateColumn/{moduleCode}/{type}")]
    public async Task<ServiceResult> UpdateColumnAsync(string moduleCode, [FromBody] SmModuleFormOption column, string type) => await _service.UpdateColumnAsync(moduleCode, column, type);
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