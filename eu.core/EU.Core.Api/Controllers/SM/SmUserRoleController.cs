/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmUserRole.cs
*
*功 能： N / A
* 类 名： SmUserRole
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/20 23:21:41  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 用户角色(Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SM)]
public class SmUserRoleController : BaseController<ISmUserRoleServices, SmUserRole, SmUserRoleDto, InsertSmUserRoleInput, EditSmUserRoleInput>
{
    public SmUserRoleController(ISmUserRoleServices service) : base(service)
    {
    }

    [HttpPost, Route("BatchInsertUserRole")]
    public async Task<ServiceResult> BatchInsertUserRole([FromBody] UserRoleVM userRoleVm) => await _service.BatchInsertUserRole(userRoleVm);

    [HttpGet, Route("QueryRole")]
    public async Task<ServiceResult<RoleTree>> QueryRole() => await _service.QueryRole();

    #region 获取用户角色数据
    /// <summary>
    /// 获取用户角色数据
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    [HttpGet, Route("QueryUserRole/{userId}")]
    public async Task<ServiceResult<List<SmUserRole>>> QueryUserRole(Guid userId) => await _service.QueryUserRole(userId);
    #endregion
}