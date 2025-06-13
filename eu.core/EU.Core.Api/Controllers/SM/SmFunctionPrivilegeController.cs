/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmFunctionPrivilege.cs
*
*功 能： N / A
* 类 名： SmFunctionPrivilege
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/23 22:41:48  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/ 
namespace EU.Core.Api.Controllers;

/// <summary>
/// 功能权限(Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SM)]
public class SmFunctionPrivilegeController : BaseController<ISmFunctionPrivilegeServices, SmFunctionPrivilege, SmFunctionPrivilegeDto, InsertSmFunctionPrivilegeInput, EditSmFunctionPrivilegeInput>
{
    public SmFunctionPrivilegeController(ISmFunctionPrivilegeServices service) : base(service)
    {
    }
}