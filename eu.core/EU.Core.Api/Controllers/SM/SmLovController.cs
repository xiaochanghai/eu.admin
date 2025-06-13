/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmLov.cs
*
*功 能： N / A
* 类 名： SmLov
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/21 1:10:41  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
using static EU.Core.Model.KeyValue;

namespace EU.Core.Api.Controllers;

/// <summary>
/// SmLov(Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SM)]
public class SmLovController : BaseController<ISmLovServices, SmLov, SmLovDto, InsertSmLovInput, EditSmLovInput>
{
    public SmLovController(ISmLovServices service) : base(service)
    {
    }

    #region 根据code 获取ComBoBox下拉值
    /// <summary>
    /// 根据code获取ComBoBox下拉值
    /// </summary>
    /// <param name="code">代码</param>
    /// <returns></returns>
    [HttpGet, Route("GetByCode/{code}")]
    public async Task<ServiceResult<IEnumerable<KeyValue>>> GetByCode(string code)
    {
        return await _service.GetByCode(code);
    }

    [HttpGet, Route("QueryByCode/{code}")]
    public async Task<ServiceResult<IEnumerable<LovData>>> QueryByCode(string code)
    {
        return await _service.QueryByCode(code);
    }
    #endregion

}