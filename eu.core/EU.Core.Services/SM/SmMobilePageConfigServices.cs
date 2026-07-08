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

namespace EU.Core.Services;

/// <summary>
/// SmMobilePageConfig (服务)
/// </summary>
public class SmMobilePageConfigServices : BaseServices<SmMobilePageConfig, SmMobilePageConfigDto, InsertSmMobilePageConfigInput, EditSmMobilePageConfigInput>, ISmMobilePageConfigServices
{
    private readonly IBaseRepository<SmMobilePageConfig> _dal;
    public SmMobilePageConfigServices(IBaseRepository<SmMobilePageConfig> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    /// <summary>
    /// 发布页面配置
    /// </summary>
    /// <param name="id">配置ID</param>
    /// <returns></returns>
    public async Task<ServiceResult> PublishAsync(Guid id)
    {
        var entity = await base.QuerySingle(x => x.ID == id && x.IsDeleted == false);
        if (entity == null)
            return Failed("配置不存在");

        entity.IsPublished = true;
        entity.Version = (entity.Version ?? 0) + 1;

        var result = await base.Update(entity);
        if (result)
            return Success("发布成功");
        return Failed("发布失败");
    }
}
