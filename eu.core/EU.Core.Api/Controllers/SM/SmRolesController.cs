/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmRoles.cs
*
*功 能： N / A
* 类 名： SmRoles
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/20 23:20:38  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/ 
namespace EU.Core.Api.Controllers;

/// <summary>
/// 系统角色(Controller)
/// </summary>
[Route("api/SmRole")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SM)]
public class SmRolesController : BaseController<ISmRolesServices, SmRoles, SmRolesDto, InsertSmRolesInput, EditSmRolesInput>
{
    public SmRolesController(ISmRolesServices service) : base(service)
    {
    }
}