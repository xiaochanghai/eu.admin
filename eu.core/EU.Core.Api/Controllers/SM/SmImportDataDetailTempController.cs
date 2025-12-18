/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmImportDataDetailTemp.cs
*
* 功 能： N / A
* 类 名： SmImportDataDetailTemp
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/10/28 16:24:44  SahHsiao   初版
*
* Copyright(c) 2025 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 系统导入数据明细临时表(Controller)
/// </summary>
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SM)]
public class SmImportDataDetailTempController : BaseController<ISmImportDataDetailTempServices, SmImportDataDetailTemp, SmImportDataDetailTempDto, InsertSmImportDataDetailTempInput, EditSmImportDataDetailTempInput>
{
    public SmImportDataDetailTempController(ISmImportDataDetailTempServices service) : base(service)
    {
    }
}