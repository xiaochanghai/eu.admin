﻿/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmEmployee.cs
*
*功 能： N / A
* 类 名： SmEmployee
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/24 15:52:50  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.IServices;

/// <summary>
/// 员工管理(自定义服务接口)
/// </summary>	
public interface ISmEmployeeServices : IBaseServices<SmEmployee, SmEmployeeDto, InsertSmEmployeeInput, EditSmEmployeeInput>
{
}