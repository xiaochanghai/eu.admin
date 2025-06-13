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
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 简历(Controller)
/// </summary>
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_RM)]
public class RmReumeController : BaseController<IRmReumeServices, RmReume, RmReumeDto, InsertRmReumeInput, EditRmReumeInput>
{
    public RmReumeController(IRmReumeServices service) : base(service)
    {
    }


    [HttpGet("ReadPdfAttachments"), AllowAnonymous]
    public async Task ReadPdfAttachmentsAsync() => await _service.ReadPdfAttachmentsAsync();

}