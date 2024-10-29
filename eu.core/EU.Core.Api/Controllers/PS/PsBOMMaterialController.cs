/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PsBOMMaterial.cs
*
*功 能： N / A
* 类 名： PsBOMMaterial
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 14:58:20  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/ 
namespace EU.Core.Api.Controllers;

/// <summary>
/// PsBOMMaterial(Controller)
/// </summary>
[GlobalActionFilter, ApiExplorerSettings(GroupName = Grouping.GroupName_PS)]
public class BOMMaterialController : BaseController1<PsBOMMaterial>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="_context"></param>
    /// <param name="BaseCrud"></param>
    public BOMMaterialController(DataContext _context, IBaseCRUDVM<PsBOMMaterial> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    /// <summary>
    /// 新增
    /// </summary>
    /// <param name="Model"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult Add(PsBOMMaterial Model)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            #region 检查是否存在相同的编码
            Utility.CheckCodeExist("", "PsBOMMaterial", "MaterialId", Model.MaterialId.ToString(), ModifyType.Add, null, "材质编号", "BOMId='" + Model.BOMId + "'");
            #endregion

            Model.SerialNumber = Utility.GenerateContinuousSequence("PsBOMMaterial", "SerialNumber", "BOMId", Model.BOMId.ToString());

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

    /// <summary>
    ///批量新增
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult BatchAdd(List<PsBOMMaterial> data)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            for (int i = 0; i < data.Count; i++)
            {
                data[i].ID = Guid.NewGuid();
                DoAddPrepare(data[i]);
                data[i].CreatedBy = new Guid(User.Identity.Name);
            }

            if (data.Count > 0)
            {
                DBHelper.Instance.AddRange(data);
                PsBOMMaterial Model = _context.PsBOMMaterial.Where(x => x.ID == data[0].ID).SingleOrDefault();
                if (Model != null)
                    BatchUpdateSerialNumber(Model.BOMId.ToString());
            }

            status = "ok";
            message = "添加成功！";
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

    #region 批量更新排序号
    /// <summary>
    /// 批量更新排序号
    /// </summary>
    /// <param name="BOMId">BOMId</param>
    private void BatchUpdateSerialNumber(string BOMId)
    {
        string sql = @"UPDATE A
                        SET A.SerialNumber = C.NUM
                        FROM PsBOMMaterial A
                             JOIN
                             (SELECT *, ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) NUM
                              FROM (SELECT *
                                    FROM (SELECT A.*
                                          FROM PsBOMMaterial A
                                          WHERE     1 = 1
                                                AND A.BOMId =
                                                    '{0}'
                                                AND A.IsDeleted = 'false'
                                                AND A.IsActive = 'true') A) B) C
                                ON A.ID = C.ID";
        sql = string.Format(sql, BOMId);
        DBHelper.Instance.ExecuteScalar(sql);
    }
    #endregion

    #region 删除重写
    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="Id"></param>
    /// <returns></returns>
    [HttpGet]
    public override IActionResult Delete(Guid Id)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            _BaseCrud.DoDelete(Id);

            PsBOMMaterial Model = _context.PsBOMMaterial.Where(x => x.ID == Id).SingleOrDefault();
            if (Model != null)
                BatchUpdateSerialNumber(Model.BOMId.ToString());

            status = "ok";
            message = "删除成功！";
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

    #region 获取物料列表
    /// <summary>
    /// 获取物料列表
    /// </summary>
    /// <param name="paramData"></param>
    /// <param name="moduleCode"></param>
    /// <param name="sorter"></param>
    /// <param name="filter"></param>
    /// <param name="Id"></param>
    /// <returns></returns>

    [HttpGet]
    public IActionResult GetMaterialList(string paramData, string moduleCode, string Id, string sorter = "{}", string filter = "{}")
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        int current = 1;
        int pageSize = 20;
        int total = 0;
        try
        {

            var searchParam = JsonHelper.JsonToObj<Dictionary<string, object>>(paramData);
            var sorterParam = JsonHelper.JsonToObj<Dictionary<string, string>>(sorter);
            var filterParam = JsonHelper.JsonToObj<Dictionary<string, object>>(filter);

            string queryCodition = "1=1";

            #region 处理查询条件
            foreach (var item in searchParam)
            {
                if (item.Key == "current")
                {
                    current = int.Parse(item.Value.ToString());
                    continue;
                }

                if (item.Key == "pageSize")
                {
                    pageSize = int.Parse(item.Value.ToString());
                    continue;
                }

                if (item.Key == "_timestamp")
                {
                    continue;
                }

                if (item.Key == "StartDate")
                {
                    queryCodition += " AND A.OrderDate >='" + item.Value.ToString() + "'";
                    continue;
                }

                if (item.Key == "EndDate")
                {
                    queryCodition += " AND A.OrderDate <='" + item.Value.ToString() + "'";
                    continue;
                }
                if (item.Key == "SalesOrderNo")
                {
                    queryCodition += " AND D.OrderNo like '%" + item.Value.ToString() + "%'";
                    continue;
                }

                if (item.Key == "CustomerName")
                {
                    queryCodition += " AND E.CustomerName like '%" + item.Value.ToString() + "%'";
                    continue;
                }

                //if (string.IsNullOrEmpty(item.Value.ToString()))
                //    queryCodition += " AND A." + item.Key + " =''";
                //else
                //    queryCodition += " AND A." + item.Key + " like '%" + item.Value.ToString() + "%'";

                if (!string.IsNullOrEmpty(item.Value.ToString()))
                    queryCodition += " AND A." + item.Key + " like '%" + item.Value.ToString() + "%'";

            }
            #endregion

            #region 处理过滤条件
            foreach (var item in filterParam)
            {
                if (!string.IsNullOrEmpty(item.Value.ToString()))
                    queryCodition += " AND A." + item.Key + " = '" + item.Value.ToString() + "'";
            }
            var BOM = _context.PsBOM.Where(o => o.ID == Guid.Parse(Id)).SingleOrDefault();
            if (BOM != null)
                queryCodition += " AND A.ID != '" + BOM.MaterialId + "'";
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