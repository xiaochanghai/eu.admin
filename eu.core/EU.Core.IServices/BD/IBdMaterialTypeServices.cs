﻿/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdMaterialType.cs
*
*功 能： N / A
* 类 名： BdMaterialType
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/23 20:13:33  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
using EU.Core.Model;

namespace EU.Core.IServices;

/// <summary>
/// 物料类型(自定义服务接口)
/// </summary>	
public interface IBdMaterialTypeServices :IBaseServices<BdMaterialType, BdMaterialTypeDto, InsertBdMaterialTypeInput, EditBdMaterialTypeInput>
{
	Task<ServiceResult<MaterialTypeTree>> GetAllMaterialType();

	Task<ServiceResult<MaterialTypeTree>> QueryClass(Guid classId);
    }