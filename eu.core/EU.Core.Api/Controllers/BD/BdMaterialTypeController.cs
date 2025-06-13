/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdMaterialType.cs
*
*功 能： N / A
* 类 名： BdMaterialType
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/23 20:13:32  SimonHsiao   初版
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
/// 物料类型(Controller)
/// </summary>
[Route("api/[controller]")]
[Authorize(Permissions.Name), GlobalActionFilter, ApiExplorerSettings(GroupName = Grouping.GroupName_BD)]
public class MaterialTypeController : BaseController<IBdMaterialTypeServices, BdMaterialType, BdMaterialTypeDto, InsertBdMaterialTypeInput, EditBdMaterialTypeInput>
{
    public MaterialTypeController(IBdMaterialTypeServices service) : base(service)
    {
    }

    #region 获取物料类型树结构数据
    /// <summary>
    /// 获取物料类型树结构数据
    /// </summary>
    /// <returns></returns>
    [HttpGet("GetAllMaterialType")]
    public async Task<ServiceResult<MaterialTypeTree>> GetAllMaterialType() => await _service.GetAllMaterialType();

    /// <summary>
    /// 获取物料类型树结构数据
    /// </summary>
    /// <returns></returns>
    [HttpGet("QueryClass/{classId}")]
    public async Task<ServiceResult<MaterialTypeTree>> QueryClass(Guid classId) => await _service.QueryClass(classId);
    #endregion

    #region 获取物料类型树结构数据
    /// <summary>
    /// 获取物料类型树结构数据
    /// </summary>
    /// <returns></returns>
    [HttpGet("QueryClass")]
    public async Task<ServiceResult<List<BdMaterialType>>> QueryClass()
    {
        var result = await _service.QueryPage(x => x.ParentTypeId == null, 1, 100, "TaxisNo ASC");
        return ServiceResult<List<BdMaterialType>>.OprateSuccess(result.data, ResponseText.QUERY_SUCCESS);
    }
    #endregion
}
