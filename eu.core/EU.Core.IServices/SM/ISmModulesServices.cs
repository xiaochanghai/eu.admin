/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmModules.cs
*
*功 能： N / A
* 类 名： SmModules
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/20 23:12:41  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
using Newtonsoft.Json.Linq;

namespace EU.Core.IServices;

/// <summary>
/// 系统模板(自定义服务接口)
/// </summary>	
public interface ISmModulesServices : IBaseServices<SmModules, SmModulesDto, InsertSmModulesInput, EditSmModulesInput>
{

    Task<ServiceResult<List<TreeMenuData>>> GetMenuData();
    Task<dynamic> GetModuleInfo(string moduleCode);
    ServicePageResult<SmModuleFormOption> GetModuleFormColumn(string moduleCode);

    Task<ServiceResult<dynamic>> GetModuleLogInfo(string moduleCode, string id);

    Task<ServiceResult<Guid>> ExportModuleSqlScript(List<Guid> ids);

    Task<ServiceResult> RecordUserModuleColumn(Guid smModuleId, JObject param);

    Task<ServiceResult<List<TreeAuthMenu>>> GetAuthMenu();
    Task<ServiceResult> CopyAsync(Guid moduleId, SmModules module);

    Task<ServiceResult> UpdateTaxisNoAsync(string moduleCode, List<SmModuleColumn> columns, string type);

    Task<ServiceResult> UpdateColumnAsync(string moduleCode, SmModuleFormOption column, string type);
}