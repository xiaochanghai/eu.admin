/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdCurrency.cs
*
* 功 能： N / A
* 类 名： BdCurrency
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/2/28 10:54:29  SahHsiao   初版
*
* Copyright(c) 2025 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 币别(Controller)
/// </summary>
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_BD)]
public class BdCurrencyController : BaseController<IBdCurrencyServices, BdCurrency, BdCurrencyDto, InsertBdCurrencyInput, EditBdCurrencyInput>
{
    public BdCurrencyController(IBdCurrencyServices service) : base(service)
    {
    }
}