/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* UserDataScopeModel.cs
*
* 功 能： 用户数据权限模型
* 类 名： UserDataScopeModel
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
/// 用户数据权限模型
/// 表示用户可以访问的公司ID列表
/// </summary>
public class UserDataScopeModel
{
    /// <summary>
    /// 可访问的公司ID列表
    /// 如果为空，表示无权限访问任何数据
    /// </summary>
    public List<Guid> CompanyIds { get; set; } = new List<Guid>();
}
