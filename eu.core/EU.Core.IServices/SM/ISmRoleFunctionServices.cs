/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmRoleFunction.cs
*
*功 能： N / A
* 类 名： SmRoleFunction
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/21 1:05:42  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.IServices;

/// <summary>
/// SmRoleFunction(自定义服务接口)
/// </summary>	
public interface ISmRoleFunctionServices : IBaseServices<SmRoleFunction, SmRoleFunctionDto, InsertSmRoleFunctionInput, EditSmRoleFunctionInput>
{
    Task<ServiceResult<dynamic>> GetModuleFunction(Guid roleId, Guid moduleId);

    Task<ServiceResult> SaveModuleFunction(RoleFuncVM roleFuncVm);

    Task<ServiceResult<List<Guid?>>> GetRoleFuncPriv(Guid RoleId);
    Task<ServiceResult<DataTree>> GetAllFuncPriv();

    Task<ServiceResult> SaveRoleFuncPriv(RoleFuncPric roleFuncPric);
}