/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* RmReume.cs
*
* 功 能： N / A
* 类 名： RmReume
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/6/12 17:43:58  SahHsiao   初版
*
* Copyright(c) 2025 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
using EU.Core.Tasks;

namespace EU.Core.Api.Controllers;

/// <summary>
/// 简历(Controller)
/// </summary>
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_RM)]
public class RmReumeController : BaseController<IRmReumeServices, RmReume, RmReumeDto, InsertRmReumeInput, EditRmReumeInput>
{
    ISchedulerCenter _schedulerCenter;
    public RmReumeController(IRmReumeServices service, ISchedulerCenter schedulerCenter) : base(service)
    {
        _schedulerCenter = schedulerCenter;
    }


    [HttpGet("ReadPdfAttachments"), AllowAnonymous]
    public async Task ReadPdfAttachmentsAsync() => await _service.ReadPdfAttachmentsAsync();



    [HttpPost("Refresh"), AllowAnonymous]
    public async Task<ServiceResult> Refresh()
    {
        var qz = new TasksQz()
        {
            Id = Guid.Parse("cbdb1c20-e6ce-4f26-813e-0457f4d93659"),
            Name = "抓取简历邮件",
            JobGroup = "JOB",
            AssemblyName = "EU.Core.Tasks",
            ClassName = "Job_ReumeEmail_Quartz",
            Cron = "0 0/30 * * * ?",
            TriggerType = 1
        };
        await _schedulerCenter.ExecuteJobAsync(qz);
        return ServiceResult.OprateSuccess();
    }

}