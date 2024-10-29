/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdMaterialIVChange.cs
*
*功 能： N / A
* 类 名： BdMaterialIVChange
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 11:49:15  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/ 
namespace EU.Core.Api.Controllers;

/// <summary>
/// BdMaterialIVChange(Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_BD)]
public class BdMaterialIVChangeController : BaseController<IBdMaterialIVChangeServices, BdMaterialIVChange, BdMaterialIVChangeDto, InsertBdMaterialIVChangeInput, EditBdMaterialIVChangeInput>
{
    public BdMaterialIVChangeController(IBdMaterialIVChangeServices service) : base(service)
    {
    }
}