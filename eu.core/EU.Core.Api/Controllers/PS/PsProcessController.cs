/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PsProcess.cs
*
*功 能： N / A
* 类 名： PsProcess
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 14:58:23  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/ 
namespace EU.Core.Api.Controllers;

/// <summary>
/// PsProcess(Controller)
/// </summary>
[GlobalActionFilter, ApiExplorerSettings(GroupName = Grouping.GroupName_PS)]
public class ProcessController : BaseController1<PsProcess>
{

    public ProcessController(DataContext _context, IBaseCRUDVM<PsProcess> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    [HttpPost]
    public override IActionResult Add(PsProcess Model)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            //#region 检查是否存在相同的编码
            Utility.CheckCodeExist("", "PsProcess", "ProcessNo", Model.ProcessNo, ModifyType.Add, null, "工序编号");
            //#endregion 

            return base.Add(Model);
        }
        catch (Exception E)
        {
            message = E.Message;
        }

        obj.status = status;
        obj.message = message;
        return Ok(obj);
    }
    #endregion

    #region 更新重写
    /// <summary>
    /// 更新重写
    /// </summary>
    /// <param name="modelModify"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult Update(dynamic modelModify)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {

            #region 检查是否存在相同的编码
            Utility.CheckCodeExist("", "PsProcess", "ProcessNo", modelModify.ProcessNo.Value, ModifyType.Edit, modelModify.ID.Value, "工序编号");
            #endregion

            Update<PsProcess>(modelModify);
            _context.SaveChanges();

            status = "ok";
            message = "修改成功！";
        }
        catch (Exception E)
        {
            message = E.Message;
        }

        obj.status = status;
        obj.message = message;
        return Ok(obj);
    }
    #endregion

    #region 获取工序设备
    /// <summary>
    /// 获取工序设备
    /// </summary>
    /// <param name="paramData"></param>
    /// <param name="moduleCode"></param>
    /// <param name="sorter"></param>
    /// <param name="filter"></param>
    /// <param name="parentColumn"></param>
    /// <param name="parentId"></param>
    /// <returns></returns>

    [HttpGet]
    public IActionResult GetMachineList(string paramData, string moduleCode = "PS_PROCESS_MACHINE_MNG", string sorter = "{}", string filter = "{}", string parentColumn = null, string parentId = null)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        int current = 1;
        int pageSize = 10000;
        int total = 0;
        try
        {

            var searchParam = JsonHelper.JsonToObj<Dictionary<string, object>>(paramData);
            var sorterParam = JsonHelper.JsonToObj<Dictionary<string, string>>(sorter);

            string queryCodition = "1=1";

            #region 处理查询条件
            if (!string.IsNullOrEmpty(parentId) && !string.IsNullOrEmpty(parentColumn))
                queryCodition += " AND A." + parentColumn + " = '" + parentId + "'";
            #endregion

            string userId = string.Empty;
            ModuleSql moduleSql = new ModuleSql(moduleCode);
            GridList grid = new GridList();
            string tableName = moduleSql.GetTableName();
            string SqlSelectBrwAndTable = moduleSql.GetSqlSelectBrwAndTable();
            string SqlSelectAndTable = moduleSql.GetSqlSelectAndTable();
            if (!string.IsNullOrEmpty(tableName))
            {
                SqlSelectBrwAndTable = string.Format(SqlSelectBrwAndTable, tableName);
                SqlSelectAndTable = string.Format(SqlSelectAndTable, tableName);
            }
            string SqlDefaultCondition = moduleSql.GetSqlDefaultCondition();
            //SqlDefaultCondition = SqlDefaultCondition.Replace("[UserId]", userId);
            string DefaultSortField = moduleSql.GetDefaultSortField();
            string DefaultSortDirection = moduleSql.GetDefaultSortDirection();
            if (string.IsNullOrEmpty(DefaultSortDirection))
            {
                DefaultSortDirection = "ASC";
            }
            grid.SqlSelect = SqlSelectBrwAndTable;
            grid.SqlDefaultCondition = SqlDefaultCondition;
            grid.SqlQueryCondition = queryCodition;
            grid.SortField = DefaultSortField;
            grid.SortDirection = DefaultSortDirection;

            #region 处理排序
            if (sorterParam.Count > 0)
                foreach (var item in sorterParam)
                {
                    grid.SortField = item.Key;
                    if (item.Value == "ascend")
                        grid.SortDirection = "ASC";
                    else if (item.Value == "descend")
                        grid.SortDirection = "DESC";

                }
            #endregion

            grid.PageSize = pageSize;
            grid.CurrentPage = current;
            grid.ModuleCode = moduleCode;
            total = grid.GetTotalCount();
            string sql = grid.GetQueryString();
            DataTable dtTemp = DBHelper.Instance.GetDataTable(sql);
            DataTable dt = Utility.FormatDataTableForTree(moduleCode, userId, dtTemp);
            obj.data = dt;

            status = "ok";
            message = "查询成功！";
        }
        catch (Exception E)
        {
            message = E.Message;
        }

        obj.current = current;
        obj.pageSize = pageSize;
        obj.total = total;
        obj.status = status;
        obj.message = message;
        return Ok(obj);
    }

    #endregion
}