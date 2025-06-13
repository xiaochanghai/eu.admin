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
using System.Dynamic;
using EU.Core.Common.Const;
using EU.Core.DataAccess;
using Microsoft.EntityFrameworkCore;

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


    /// <summary>
    /// 系统参数明细 -- 根据参数分组查询
    /// </summary>
    /// <returns></returns>
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
        return ServiceResult<List<SmConfigView>>.OprateSuccess(groups, ResponseText.QUERY_SUCCESS);
        //return Ok(obj);
        //return await _systemSettingItemService.GetListByGroupAsync(groupId);
    }

    /// <summary>
    /// SmConfigView
    /// </summary>
    public class SmConfigView : SmConfigGroup
    {
        /// <summary>
        /// 
        /// </summary>
        public List<SmConfig> detail { get; set; }
    }
}