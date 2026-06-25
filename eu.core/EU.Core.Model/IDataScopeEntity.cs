/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IDataScopeEntity.cs
*
* 功 能： 数据权限实体接口
* 类 名： IDataScopeEntity
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/6/23  EU Team   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│ 此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露． │
*│ 版权所有：EU Team                              │
*└──────────────────────────────────┘
*/

namespace EU.Core.Model;

/// <summary>
/// 数据权限实体接口
/// 所有需要进行数据权限控制的实体都必须实现此接口
/// </summary>
public interface IDataScopeEntity
{
    /// <summary>
    /// 公司ID
    /// 用于数据权限过滤
    /// </summary>
    Guid? CompanyId { get; set; }
}
