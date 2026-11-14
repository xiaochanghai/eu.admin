/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmConfig.cs
*
*功 能： N / A
* 类 名： SmConfig
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/22 9:09:55  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
using EU.Core.Common.Const;

namespace EU.Core.Api.Controllers;

/// <summary>
/// 系统配置(Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SM)]
public class SmConfigController : BaseController<ISmConfigServices, SmConfig, SmConfigDto, InsertSmConfigInput, EditSmConfigInput>
{
    public SmConfigController(ISmConfigServices service) : base(service)
    {
    }

    #region 获取前端可用配置

    /// <summary>
    /// 获取后端明确允许提供给前端的配置
    /// </summary>
    /// <remarks>无业务入参。当前仅返回有效、未删除的系统主题分组内，处于白名单且有效、未删除的主题配置。</remarks>
    /// <param name="cancellationToken">请求取消令牌，由 ASP.NET Core 注入，前端无需传入。</param>
    /// <returns>包含前端配置的服务响应；Theme 为主题配置键值集合，无匹配配置时为空字典。</returns>
    [HttpGet("GetClientConfig")]
    public async Task<ServiceResult<ClientConfigDto>> GetClientConfig(CancellationToken cancellationToken) =>
        Success(await _service.GetClientConfig(cancellationToken), ResponseText.QUERY_SUCCESS);

    #endregion


    #region 获取配置分组及明细

    /// <summary>
    /// 获取配置分组列表及各分组对应的配置明细。
    /// </summary>
    /// <remarks>无业务入参，不按指定分组查询。分组和配置明细分别按 Sequence 排序，通过 ConfigGroupId 将明细归入对应分组。</remarks>
    /// <returns>包含配置分组列表的服务响应；每个分组的 detail 为其配置明细，无匹配明细时为空列表。</returns>
    [HttpGet, Route("GetListByGroup")]
    public async Task<ServiceResult<List<SmConfigView>>> GetListByGroup()
    {
        using var _context = ContextFactory.CreateContext();
        dynamic obj = new ExpandoObject();
        var groups = await _context.SmConfigGroup.OrderBy(o => o.Sequence).Select(o => new SmConfigView()
        {
            ID = o.ID,
            ParentId = o.ParentId,
            Name = o.Name,
            Type = o.Type
        }).ToListAsync();
        var configs = await _context.SmConfig.OrderBy(o => o.Sequence).ToListAsync();
        //var views = customerDto.Map().OnTo(customer);
        groups?.ForEach(o =>
        {
            o.detail = configs.Where(x => x.ConfigGroupId == o.ID).ToList();
        });
        return Success(groups, ResponseText.QUERY_SUCCESS);
        //return Ok(obj);
        //return await _systemSettingItemService.GetListByGroupAsync(groupId);
    }

    #endregion

    /// <summary>
    /// 配置分组及其配置明细。
    /// </summary>
    public class SmConfigView : SmConfigGroup
    {
        /// <summary>
        /// 当前分组对应的配置明细列表。
        /// </summary>
        public List<SmConfig> detail { get; set; }
    }
}
