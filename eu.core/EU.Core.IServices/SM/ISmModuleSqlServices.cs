/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmModuleSql.cs
*
*功 能： N / A
* 类 名： SmModuleSql
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/23 17:07:01  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.IServices;

/// <summary>
/// 系统模块SQL(自定义服务接口)
/// </summary>	
public interface ISmModuleSqlServices : IBaseServices<SmModuleSql, SmModuleSqlDto, InsertSmModuleSqlInput, EditSmModuleSqlInput>
{
    Task<dynamic> GetByModuleId(Guid moduleId);
    Task<ServiceResult<string>> GetModuleFullSql(Guid moduleId);
}