/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmQuartzJob.cs
*
*功 能： N / A
* 类 名： SmQuartzJob
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/24 23:35:31  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 任务调度(Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SM)]
public class SmQuartzJobController : BaseController<ISmQuartzJobServices, SmQuartzJob, SmQuartzJobDto, InsertSmQuartzJobInput, EditSmQuartzJobInput>
{
    public SmQuartzJobController(ISmQuartzJobServices service) : base(service)
    {
    }

    #region 远程操作
    /// <summary>
    /// 远程操作
    /// </summary>
    /// <param name="id">任务清单标识</param>
    /// <param name="operate">操作值 字典Code为`DIC.TASK.OPERATE`</param>
    /// <param name="args">操作参数，当操作为修改参数是必填</param>
    /// <returns></returns>
    [HttpPost("Operate/{id}/{operate}")]
    public async Task<ServiceResult> Operate(Guid id, string operate, [FromBody] SmQuartzJob args) => await _service.Operate(id, operate, args);
    #endregion
}