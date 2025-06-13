/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmCity.cs
*
*功 能： N / A
* 类 名： SmCity
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/24 17:31:44  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/ 
namespace EU.Core.Api.Controllers;

/// <summary>
/// 城市(Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SM)]
public class SmCityController : BaseController<ISmCityServices, SmCity, SmCityDto, InsertSmCityInput, EditSmCityInput>
{
    public SmCityController(ISmCityServices service) : base(service)
    {
    }
}