/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmUserRole.cs
*
*功 能： N / A
* 类 名： SmUserRole
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/20 23:21:42  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.IServices;

/// <summary>
/// 用户角色(自定义服务接口)
/// </summary>	
public interface ISmUserRoleServices : IBaseServices<SmUserRole, SmUserRoleDto, InsertSmUserRoleInput, EditSmUserRoleInput>
{
    Task<ServiceResult<List<SmUserRole>>> QueryUserRole(Guid userId);

    Task<ServiceResult<RoleTree>> QueryRole();
    Task<ServiceResult> BatchInsertUserRole(UserRoleVM userRoleVm);
}