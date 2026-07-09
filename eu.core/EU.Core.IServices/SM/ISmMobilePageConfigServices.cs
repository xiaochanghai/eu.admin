/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmMobilePageConfig.cs
*
* 功 能： N / A
* 类 名： SmMobilePageConfig
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2026/7/8  Claude   初版
*
* Copyright(c) 2026 EU Corporation. All Rights Reserved.
*/
namespace EU.Core.IServices;

/// <summary>
/// SmMobilePageConfig(自定义服务接口)
/// </summary>
public interface ISmMobilePageConfigServices : IBaseServices<SmMobilePageConfig, SmMobilePageConfigDto, InsertSmMobilePageConfigInput, EditSmMobilePageConfigInput>
{
    /// <summary>
    /// 发布页面配置
    /// </summary>
    /// <param name="id">配置ID</param>
    /// <returns></returns>
    Task<ServiceResult> PublishAsync(Guid id);

    /// <summary>
    /// Get published page config by page code.
    /// </summary>
    /// <param name="pageCode">Page code.</param>
    /// <param name="appScope">Optional app scope.</param>
    /// <returns></returns>
    Task<ServiceResult<SmMobilePageConfigDto>> GetPublishedByPageCodeAsync(string pageCode, string appScope = null);
}
