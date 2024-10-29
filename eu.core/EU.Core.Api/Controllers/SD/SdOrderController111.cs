/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdOrder.cs
*
*功 能： N / A
* 类 名： SdOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 14:50:04  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 销售单
/// </summary>
[GlobalActionFilter, ApiExplorerSettings(GroupName = Grouping.GroupName_SD)]
public class OrderController : BaseController1<SdOrder>
{
    /// <summary>
    /// 销售单
    /// </summary>
    /// <param name="_context"></param>
    /// <param name="BaseCrud"></param>
    public OrderController(DataContext _context, IBaseCRUDVM<SdOrder> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    [HttpPost]
    public override IActionResult Add(SdOrder Model)
    {


        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            //#region 检查是否存在相同的编码
            //Utility.CheckCodeExist("", "BdColor", "ColorNo", Model.ColorNo, ModifyType.Add, null, "材质编号");
            //#endregion
            Model.OrderNo = Utility.GenerateContinuousSequence("SdOrderNo");
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
        string isReloadDetail = "N";

        try
        {

            //#region 检查是否存在相同的编码
            //Utility.CheckCodeExist("", "BdColor", "ColorNo", modelModify.ColorNo.Value, ModifyType.Edit, modelModify.ID.Value, "材质编号");
            //#endregion

            string ID = modelModify.ID;
            string sql1 = "SELECT * FROM SdOrder A WHERE A.ID = '{0}'";
            sql1 = string.Format(sql1, ID);
            var order = DBHelper.Instance.QueryFirst<SdOrder>(sql1, null);

            Update<SdOrder>(modelModify);
            _context.SaveChanges();

            #region 批量更新銷售單明細稅額、含稅交割、未稅金額

            if (order.TaxRate != Convert.ToDecimal(modelModify.TaxRate) || order.TaxType != Convert.ToString(modelModify.TaxType))
            {
                isReloadDetail = "Y";
                string sql = string.Empty;

                #region 税额计算
                //比如税率13%情况下，客户按未税价计算，含税金额=单价x数量x1.13，未税金额=单价x数量
                //比如税率13 % 情况下，客户按含税价计算，含税金额 = 单价x数量，未税金额 = 单价x数量 / 1.13
                //零税
                if (modelModify.TaxType == "ZeroTax" || modelModify.TaxRate == 0)
                {
                    //Model.NoTaxAmount = Model.Price * Model.Amount;
                    //Model.TaxAmount = 0;
                    //Model.TaxIncludedAmount = Model.NoTaxAmount;
                    sql = @"UPDATE A
                                    SET A.TaxIncludedAmount = Price * QTY,
                                        A.NoTaxAmount = Price * QTY,
                                        A.TaxAmount = 0
                                    FROM SdOrderDetail A
                                    WHERE A.OrderId = '{0}';";
                    sql = string.Format(sql, ID);
                    DBHelper.Instance.ExecuteScalar(sql);

                }//未税
                else if (order.TaxType == "ExcludingTax")
                {
                    decimal rate = (100 + Convert.ToDecimal(modelModify.TaxRate)) / 100;

                    sql = @"UPDATE A
                                    SET A.TaxIncludedAmount = ((Price * QTY) * {1}),
                                        A.NoTaxAmount = Price * QTY
                                    FROM SdOrderDetail A
                                    WHERE A.OrderId = '{0}';";
                    sql = string.Format(sql, ID, rate);
                    DBHelper.Instance.ExecuteScalar(sql);

                    sql = @"UPDATE A
                                    SET A.TaxAmount = A.TaxIncludedAmount - A.NoTaxAmount
                                    FROM SdOrderDetail A
                                    WHERE A.OrderId =  '{0}'; ";
                    sql = string.Format(sql, ID, rate);
                    DBHelper.Instance.ExecuteScalar(sql);

                    //Model.NoTaxAmount = Model.Price * Model.Amount;
                    //Model.TaxIncludedAmount = Model.NoTaxAmount / ((100 + order.TaxRate) / 100);
                    //Model.TaxAmount = Model.TaxIncludedAmount - Model.NoTaxAmount;
                }//含税
                else if (order.TaxType == "IncludingTax")
                {
                    decimal rate = (100 + Convert.ToDecimal(modelModify.TaxRate)) / 100;
                    sql = @"UPDATE A
                                    SET A.NoTaxAmount = ((Price * QTY) / {1}),
                                        A.TaxIncludedAmount = Price * QTY
                                    FROM SdOrderDetail A
                                    WHERE A.OrderId = '{0}';";
                    sql = string.Format(sql, ID, rate);
                    DBHelper.Instance.ExecuteScalar(sql);
                    sql = @"UPDATE A
                                    SET A.TaxAmount = A.TaxIncludedAmount - A.NoTaxAmount
                                    FROM SdOrderDetail A
                                    WHERE A.OrderId =  '{0}'; ";
                    sql = string.Format(sql, ID, rate);
                    DBHelper.Instance.ExecuteScalar(sql);
                    //Model.TaxIncludedAmount = Model.Price * Model.Amount;
                    //Model.NoTaxAmount = Model.TaxIncludedAmount / ((100 + order.TaxRate) / 100);
                    //Model.TaxAmount = Model.TaxIncludedAmount - Model.NoTaxAmount;
                }
                #endregion
            }
            #endregion

            status = "ok";
            message = "修改成功！";
        }
        catch (Exception E)
        {
            message = E.Message;
        }

        obj.status = status;
        obj.message = message;
        obj.isReloadDetail = isReloadDetail;
        return Ok(obj);
    }
    #endregion

    #region 获取待出库订单
    /// <summary>
    /// 获取待出库订单
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public virtual IActionResult GetWaitShipList(string paramData, string masterId)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        int current = 1;
        int pageSize = 10000;
        int total = 0;

        try
        {
            var searchParam = JsonConvert.DeserializeObject<Dictionary<string, object>>(paramData);

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
            }
            #endregion

            var ShipOrder = _context.SdShipOrder.Where(x => x.ID == Guid.Parse(masterId)).SingleOrDefault();

            int _pageSize = pageSize;
            //计算分页起始索引
            int startIndex = current > 1 ? (current - 1) * _pageSize : 0;

            //计算分页结束索引
            int endIndex = current * _pageSize;

            string defaultSql = @"SELECT *
                                        FROM (SELECT A.ID,
                                                   B.OrderNo,
                                                   B.ID SalesOrderId,
                                                   A.CreatedTime,
                                                   A.SerialNumber,
                                                   C.ID MaterialId,
                                                   C.MaterialNo,
                                                   C.UnitId,
                                                   A.MaterialName,
                                                   C.Specifications MaterialSpecifications,
                                                   C.UnitName UnitNames,
                                                   ISNULL (C.DecimalPlaces, 0) DecimalPlaces,
                                                   A.QTY OrderQTY,
                                                   A.QTY - ISNULL (E.ShipQTY, 0) QTY,
                                                   A.CustomerMaterialCode,
                                                   CONVERT (VARCHAR (100), B.DeliveryrDate, 23) DeliveryrDate
                                            FROM SdOrderDetail A
                                                 JOIN SdOrder B
                                                    ON     A.OrderId = B.ID
                                                       AND A.IsDeleted = B.IsDeleted
                                                       AND B.CustomerId = '{0}'
                                                 LEFT JOIN BdMaterial_V C ON A.MaterialId = C.ID
                                                 LEFT JOIN
                                                 (SELECT SUM (A.ShipQTY) ShipQTY, A.SalesOrderDetailId
                                                  FROM SdShipOrderDetail A
                                                       JOIN SdShipOrder B
                                                          ON     A.OrderId = B.ID
                                                             AND A.IsActive = B.IsActive
                                                             AND A.IsDeleted = B.IsDeleted
                                                  WHERE     A.IsDeleted = 'false'
                                                        AND A.IsActive = 'true'
                                                        AND B.CustomerId = '{0}'
                                                  GROUP BY A.SalesOrderDetailId) E
                                                    ON A.ID = E.SalesOrderDetailId
                                            WHERE     A.IsDeleted = 'false'
                                                  AND A.IsActive = 'true'
                                                  AND B.AuditStatus = 'CompleteAudit') A
                                        WHERE QTY > 0";
            defaultSql = string.Format(defaultSql, ShipOrder.CustomerId);

            string sql = @"SELECT * FROM(SELECT *, ROW_NUMBER() OVER(ORDER BY CreatedTime ASC) ROWNUM FROM(" + defaultSql + ") B) C WHERE ROWNUM <= {1} AND ROWNUM > {0}";
            sql = string.Format(sql, startIndex, endIndex);
            List<OrderExtend> list = DBHelper.Instance.QueryList<OrderExtend>(sql);

            for (int i = 0; i < list.Count; i++)
            {
                decimal Step = 1;
                for (int j = 0; j < list[i].DecimalPlaces; j++)
                    Step = Step / 10;

                list[i].Step = Step;
                list[i].Min = Step;
            }

            obj.data = list;
            string countString = @"SELECT COUNT (1)
                                FROM (SELECT A.QTY - ISNULL (E.ShipQTY, 0) QTY
                                      FROM SdOrderDetail A
                                           JOIN SdOrder B
                                              ON     A.OrderId = B.ID
                                                 AND A.IsDeleted = B.IsDeleted
                                                 AND B.CustomerId = '{0}'
                                           LEFT JOIN (SELECT SUM (A.ShipQTY) ShipQTY, A.SalesOrderDetailId
                                                    FROM SdShipOrderDetail A
                                                         JOIN SdShipOrder B
                                                            ON     A.OrderId = B.ID
                                                               AND A.IsActive = B.IsActive
                                                               AND A.IsDeleted = B.IsDeleted
                                                    WHERE A.IsDeleted = 'false' AND A.IsActive = 'true'
                                                    GROUP BY A.SalesOrderDetailId) E
                                              ON A.ID = E.SalesOrderDetailId
                                      WHERE     A.IsDeleted = 'false'
                                            AND A.IsActive = 'true'
                                            AND B.AuditStatus = 'CompleteAudit') A
                                WHERE QTY > 0";
            countString = string.Format(countString, ShipOrder.CustomerId);
            total = Convert.ToInt32(DBHelper.Instance.ExecuteScalar(countString));
            status = "ok";
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

    #region 审核
    /// <summary>
    /// 审核
    /// </summary> 
    /// <param name="modelModify"></param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult AuditOrder(dynamic modelModify)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        string orderId = modelModify.orderId;
        string auditStatus = modelModify.auditStatus;
        string sql = string.Empty;
        try
        {


            #region 修改订单审核状态
            if (auditStatus == "Add")
                auditStatus = "CompleteAudit";
            else if (auditStatus == "CompleteAudit")
            {

                #region 检查单据是否被引用
                sql = @"SELECT A.ID
                                FROM SdOutOrderDetail A
                                WHERE     A.SalesOrderId = '{0}'
                                      AND A.IsDeleted = 'false'
                                      AND A.IsActive = 'true'
                                UNION
                                SELECT A.ID
                                FROM ArPrepaidDetail A
                                WHERE     A.SourceOrderId = '{0}'
                                      AND A.IsDeleted = 'false'
                                      AND A.IsActive = 'true'";
                sql = string.Format(sql, orderId);
                DataTable dt = DBHelper.Instance.GetDataTable(sql);
                #endregion

                if (dt.Rows.Count == 0)
                    auditStatus = "Add";
                else throw new Exception("该单据已被引用，不可撤销！");
            }

            #endregion

            DbUpdate du = new DbUpdate("SdOrder");
            du.Set("AuditStatus", auditStatus);
            du.Where("ID", "=", orderId);
            DBHelper.Instance.ExecuteScalar(du.GetSql());

            #region 导入订单操作历史
            Utility.RecordOperateLog(User.Identity.Name, "SD_SALES_ORDER_MNG", "SdOrder", orderId, OperateType.Update, "Audit", "修改订单审核状态为：" + auditStatus);
            #endregion


            status = "ok";
            message = "提交成功！";
        }
        catch (Exception E)
        {
            message = E.Message;
        }

        obj.status = status;
        obj.message = message;
        obj.auditStatus = auditStatus;
        return Ok(obj);
    }
    #endregion

    #region 删除
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
            var SdOrder = _context.SdOrder.Where(x => x.ID == Id).SingleOrDefault();

            if (SdOrder == null)
                throw new Exception("无效的数据ID！");

            if (SdOrder.AuditStatus == "CompleteAudit")
                throw new Exception("该销售单已审核通过，暂不可进行删除操作！");

            _BaseCrud.DoDelete(Id);

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

    #region 销售订单分析报表
    /// <summary>
    /// 销售订单分析报表
    /// </summary>
    /// <param name="paramData"></param>
    /// <param name="moduleCode"></param>
    /// <param name="sorter"></param>
    /// <param name="filter"></param>
    /// <param name="parentColumn"></param>
    /// <param name="parentId"></param>
    /// <returns></returns>

    [HttpGet]
    public IActionResult GetReportList(string paramData, string moduleCode, string sorter = "{}", string filter = "{}", string parentColumn = null, string parentId = null)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        int current = 1;
        int pageSize = 20;
        int total = 0;
        try
        {

            var searchParam = JsonConvert.DeserializeObject<Dictionary<string, object>>(paramData);
            var sorterParam = JsonConvert.DeserializeObject<Dictionary<string, string>>(sorter);
            var filterParam = JsonConvert.DeserializeObject<Dictionary<string, object>>(filter);

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

                if (!string.IsNullOrEmpty(item.Value.ToString()))
                    queryCodition += " AND A." + item.Key + " like '%" + item.Value.ToString() + "%'";

            }
            if (!string.IsNullOrEmpty(parentId) && !string.IsNullOrEmpty(parentColumn))
                queryCodition += " AND A." + parentColumn + " = '" + parentId + "'";
            #endregion

            #region 处理过滤条件
            foreach (var item in filterParam)
            {
                if (!string.IsNullOrEmpty(item.Value.ToString()))
                    queryCodition += " AND A." + item.Key + " = '" + item.Value.ToString() + "'";
            }
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