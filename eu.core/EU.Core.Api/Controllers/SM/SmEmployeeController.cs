/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmEmployee.cs
*
*功 能： N / A
* 类 名： SmEmployee
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/24 15:52:49  SimonHsiao   初版
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
using ExpressionType = EU.Core.DataAccess.ExpressionType;

namespace EU.Core.Api.Controllers;

/// <summary>
/// 员工管理(Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SM)]
public class SmEmployeeController : BaseController<ISmEmployeeServices, SmEmployee, SmEmployeeDto, InsertSmEmployeeInput, EditSmEmployeeInput>
{
    public SmEmployeeController(ISmEmployeeServices service) : base(service)
    {
    }



    [HttpGet, Route("GetParentList")]
    public virtual IActionResult GetParentList(string paramData = null)
    {
        dynamic obj = new ExpandoObject();
        bool success = false;
        string message = string.Empty;
        int current = 1;
        int pageSize = 20;
        int total = 0;

        string defaultSorter = "EmployeeCode";
        string sortType = string.Empty;

        try
        {
            using var _context = ContextFactory.CreateContext();
            IQueryable<SmEmployee> query = null;
            query = _context.Set<SmEmployee>();

            var lamadaExtention = new LamadaExtention<SmEmployee>();

            lamadaExtention.GetExpression("IsDeleted", "false", DataAccess.ExpressionType.Equal);

            var searchParam = JsonHelper.JsonToObj<Dictionary<string, object>>(paramData);
            foreach (var item in searchParam)
            {
                if (item.Key == "ID")
                {
                    lamadaExtention.GetExpression("ID", item.Value.ToString(), ExpressionType.NotEqual);
                    continue;
                }
            }

            if (lamadaExtention.GetLambda() != null)
                query = query.Where(lamadaExtention.GetLambda());

            query = query.OrderBy(LamadaExtention<SmEmployee>.SortLambda<SmEmployee, string>(defaultSorter, "EmployeeName"));

            obj.data = query.ToList();


            total = query.Count();

            success = true;
            message = ResponseText.QUERY_SUCCESS;
        }
        catch (Exception E)
        {
            message = E.Message;
        }

        obj.current = current;
        obj.pageSize = pageSize;
        obj.total = total;
        obj.success = success;
        obj.message = message;
        return Ok(obj);
    }
}