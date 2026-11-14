/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmConfig.cs
*
*功 能： N / A
* 类 名： SmConfig
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/22 9:09:56  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 系统配置 (服务)
/// </summary>
public class SmConfigServices : BaseServices<SmConfig, SmConfigDto, InsertSmConfigInput, EditSmConfigInput>, ISmConfigServices
{
    private readonly IBaseRepository<SmConfig> _dal;
    public SmConfigServices(IBaseRepository<SmConfig> dal)
    {
        _dal = dal;
        BaseDal = dal;
    }



    #region 更新
    public override async Task<bool> Update(Guid Id, object entity)
    {

        var result = await base.Update(Id, entity);
        await ConfigCache.ClearThemeAsync();
        return result;
    }
    #endregion

    #region 获取前端可用配置

    /// <summary>
    /// 获取后端明确允许提供给前端的配置，当前包含主题配置。
    /// </summary>
    /// <remarks>
    /// 从有效、未删除的 system-theme 分组中，按 Sequence、ID 排序取第一个。
    /// 仅返回该分组内有效、未删除且编码位于白名单中的配置，配置值为 null 时转为空字符串。
    /// </remarks>
    /// <param name="cancellationToken">取消令牌，默认不取消；在查询前后检查，不中断已提交的数据库查询。</param>
    /// <returns>前端配置对象；Theme 的键为去掉 theme. 前缀的配置编码，值为字符串，无匹配分组或明细时为空字典。</returns>
    /// <exception cref="OperationCanceledException">检查取消令牌时发现操作已取消。</exception>
    /// <exception cref="ArgumentException">查询结果中存在重复的配置编码，无法构建字典。</exception>
    public async Task<ClientConfigDto> GetClientConfig(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = new ClientConfigDto();
        if (await ConfigCache.ExistsThemeAsync())
        {
            result.Theme = await ConfigCache.GetThemeAsync();
        }
        else
        {
            var group = await Db.Queryable<SmConfigGroup>()
                .Where(x => x.Type == "system-theme" && x.IsActive == true && !x.IsDeleted)
                .OrderBy(x => x.Sequence).OrderBy(x => x.ID).FirstAsync();
            cancellationToken.ThrowIfCancellationRequested();
            if (group == null) return result;

            string[] codes = [
                "theme.allowCustom", "theme.layout", "theme.primary", "theme.borderRadius",
            "theme.compactAlgorithm", "theme.isDark", "theme.isGrey", "theme.isWeak", "theme.isHappy",
            "theme.menuSplit", "theme.siderInverted", "theme.headerInverted", "theme.isCollapse",
            "theme.accordion", "theme.watermark", "theme.breadcrumb", "theme.breadcrumbIcon",
            "theme.tabs", "theme.tabsIcon", "theme.tabsDrag", "theme.footer"
            ];
            var rows = await Db.Queryable<SmConfig>()
                .Where(x => x.ConfigGroupId == group.ID && x.IsActive == true && !x.IsDeleted
                    && codes.Contains(x.ConfigCode))
                .Select(x => new { x.ConfigCode, x.ConfigValue }).ToListAsync();
            cancellationToken.ThrowIfCancellationRequested();
            // 配置编码重复时报告配置错误，不任意选取其中一条。
            result.Theme = rows.ToDictionary(x => x.ConfigCode.Replace("theme.", string.Empty), x => x.ConfigValue ?? string.Empty);

            await ConfigCache.AddThemeAsync(result.Theme);
        }
        return result;
    }

    #endregion

}
