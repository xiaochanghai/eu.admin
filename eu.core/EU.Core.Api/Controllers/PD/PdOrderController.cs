/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PdOrder.cs
*
*功 能： N / A
* 类 名： PdOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 14:39:48  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Api.Controllers;

/// <summary>
/// 生产工单
/// </summary>
[GlobalActionFilter, ApiExplorerSettings(GroupName = Grouping.GroupName_PD)]
public class PdOrderController : BaseController1<PdOrder>
{
    /// <summary>
    /// 生产工单
    /// </summary>
    /// <param name="_context"></param>
    /// <param name="BaseCrud"></param>
    public PdOrderController(DataContext _context, IBaseCRUDVM<PdOrder> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    /// <summary>
    /// 新增
    /// </summary>
    /// <param name="Model"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult Add(PdOrder Model)
    {


        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            //#region 检查是否存在相同的编码
            //Utility.CheckCodeExist("", "BdColor", "ColorNo", Model.ColorNo, ModifyType.Add, null, "材质编号");
            //#endregion 

            Model.OrderNo = Utility.GenerateContinuousSequence("PdOrderNo");

            #region 导入对应订单
            var order = _context.SdOrder.Where(o => o.ID == Model.SourceOrderId).SingleOrDefault();
            var orderDetail = _context.SdOrderDetail.Where(o => o.ID == Model.SourceOrderDetailId).SingleOrDefault();
            if (order != null && orderDetail != null)
            {
                DbInsert di = new DbInsert("PdOrderDetail");
                di.Values("SerialNumber", 1);
                di.Values("OrderId", Model.ID);
                di.Values("SourceOrderId", Model.SourceOrderId);
                di.Values("SourceOrderDetailId", Model.SourceOrderDetailId);
                di.Values("SourceSerialNumber", orderDetail.SerialNumber);
                di.Values("CustomerMaterialCode", orderDetail.CustomerMaterialCode);
                di.Values("QTY", orderDetail.QTY);
                di.Values("DeliveryDate", order.DeliveryDate);
                DBHelper.Instance.ExecuteDML(di.GetSql());
            }
            #endregion

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

            //#region 检查是否存在相同的编码
            //Utility.CheckCodeExist("", "BdColor", "ColorNo", modelModify.ColorNo.Value, ModifyType.Edit, modelModify.ID.Value, "材质编号");
            //#endregion

            Update<PdOrder>(modelModify);
            _context.SaveChanges();

            #region 导入对应订单
            Guid id = Guid.Parse(modelModify.ID.Value);
            string sql = "SELECT A.* FROM PdOrderDetail A WHERE A.OrderId='{0}' AND A.IsDeleted='false'";
            sql = string.Format(sql, id);
            PdOrderDetail pdOrderDetail = DBHelper.Instance.QueryList<PdOrderDetail>(sql).SingleOrDefault();
            if (pdOrderDetail != null && modelModify.SourceOrderId != null)
            {
                Guid sourceOrderDetailId = Guid.Parse(modelModify.SourceOrderDetailId.Value);
                if (pdOrderDetail.SourceOrderDetailId != sourceOrderDetailId)
                {
                    Guid SourceOrderId = Guid.Parse(modelModify.SourceOrderId.Value);
                    Guid SourceOrderDetailId = Guid.Parse(modelModify.SourceOrderDetailId.Value);

                    var order = _context.SdOrder.Where(o => o.ID == SourceOrderId).SingleOrDefault();
                    var orderDetail = _context.SdOrderDetail.Where(o => o.ID == SourceOrderDetailId).SingleOrDefault();
                    if (order != null && orderDetail != null)
                    {
                        DbUpdate du = new DbUpdate("PdOrderDetail", "ID", pdOrderDetail.ID.ToString());
                        du.Set("SerialNumber", 1);
                        du.Set("SourceOrderId", SourceOrderId);
                        du.Set("SourceOrderDetailId", SourceOrderDetailId);
                        du.Set("SourceSerialNumber", orderDetail.SerialNumber);
                        du.Set("CustomerMaterialCode", orderDetail.CustomerMaterialCode);
                        du.Set("QTY", orderDetail.QTY);
                        du.Set("DeliveryDate", order.DeliveryDate);
                        DBHelper.Instance.ExecuteDML(du.GetSql());
                    }
                }
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
        return Ok(obj);
    }
    #endregion

    #region 审核
    /// <summary>
    /// 审核
    /// </summary>
    /// <param name="modelModify">数据</param>
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
                                FROM PdReissueDetail A
                                WHERE     A.SourceOrderId = '{0}'
                                      AND A.IsDeleted = 'false'
                                      AND A.IsActive = 'true'
                                UNION
                                SELECT A.ID
                                FROM PdOutDetail A
                                WHERE     A.SourceOrderId = '{0}'
                                      AND A.IsDeleted = 'false'
                                      AND A.IsActive = 'true'
                                UNION
                                SELECT A.ID
                                FROM PdCompleteDetail A
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

            DbUpdate du = new DbUpdate("PdOrder");
            du.Set("AuditStatus", auditStatus);
            du.Where("ID", "=", orderId);
            DBHelper.Instance.ExecuteScalar(du.GetSql());

            #region 导入订单操作历史
            Utility.RecordOperateLog(UserId.ToString(), "PD_ORDER_MNG", "PdOrder", orderId, OperateType.Update, "Audit", "修改订单审核状态为：" + auditStatus);
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

    #region 获取详情
    /// <summary>
    /// 获取详情
    /// </summary>
    /// <param name="Id">数据ID</param>
    /// <returns></returns>
    [HttpGet]
    public override async Task<IActionResult> GetById(Guid Id)
    {
        dynamic obj = new ExpandoObject();
        dynamic extend = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        int count = 0;

        try
        {
            //string sql = @"SELECT COUNT (0)
            //            FROM PdOrderDetail A
            //            WHERE     A.IsDeleted = 'false'
            //                  AND A.IsActive = 'true'
            //                  AND A.OrderId = '{0}'";
            //sql = string.Format(sql, Id);
            //count = Convert.ToInt32(DBHelper.Instance.ExecuteScalar(sql));

            PdOrder order = _context.PdOrder.Where(O => O.ID == Id).SingleOrDefault();
            obj.data = order;

            string sql = @"SELECT A.MaterialNo,
                                   A.MaterialName,
                                   A.Specifications,
                                   A.UnitName,
                                   B.Formula
                            FROM BdMaterial_V A LEFT JOIN BdBOMMaterial_V B ON A.ID = B.MaterialId
                            WHERE A.ID = '{0}'";
            sql = string.Format(sql, order.MaterialId);
            OrderSourceList list = DBHelper.Instance.QueryList<OrderSourceList>(sql).SingleOrDefault();
            if (list != null)
            {
                extend.Specifications = list.Specifications;
                extend.MaterialName = list.MaterialNo + " - " + list.MaterialName;
                extend.UnitName = list.UnitName;
                extend.Formula = list.Formula;
            }

        }
        catch (Exception E)
        {
            message = E.Message;
        }
        obj.count = count;
        obj.extend = extend;
        obj.status = status;
        obj.message = message;
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
            var Order = _context.PdOrder.Where(x => x.ID == Id).SingleOrDefault();

            if (Order == null)
                throw new Exception("无效的数据ID！");

            if (Order.AuditStatus != "Add")
                throw new Exception("该单据已审核通过，暂不可进行删除操作！");

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

    #region 获取来源列表
    /// <summary>
    /// 获取来源列表
    /// </summary>
    /// <param name="paramData">参数</param>
    /// <param name="source">来源</param>
    /// <returns></returns>
    [HttpGet]
    public virtual IActionResult GetSourceList(string paramData, string source)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        int current = 1;
        int pageSize = 10000;
        int total = 0;

        var searchParam = JsonHelper.JsonToObj<Dictionary<string, object>>(paramData);

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

        int _pageSize = pageSize;
        //计算分页起始索引
        int startIndex = current > 1 ? (current - 1) * _pageSize : 0;

        //计算分页结束索引
        int endIndex = current * _pageSize;

        string defaultSql = string.Empty;
        if (source == "Material")
        {
            defaultSql = @"SELECT A.ID,
                                       A.CreatedTime,
                                       'Material' Source,
                                       NULL SourceOrderDetailId,
                                       NULL SourceOrderId,
                                       A.ID MaterialId,
                                       A.MaterialNo,
                                       A.MaterialName,
                                       A.Specifications,
                                       A.UnitName,
                                       D.Formula
                                FROM BdMaterial_V A LEFT JOIN BdBOMMaterial_V D ON A.ID = D.MaterialId
                                WHERE A.IsDeleted = 'false' AND A.IsActive = 'true'";
        }
        else if (source == "Sales")
        {
            defaultSql = @"SELECT *
                                    FROM (SELECT A.ID, A.SerialNumber,
                                                 A.CreatedTime,
                                                 'Sales' Source,
                                                 A.ID SourceOrderDetailId,
                                                 A.OrderId SourceOrderId,
                                                 A.QTY OrderQTY,
                                                 A.QTY - ISNULL (F.QTY, 0) QTY,
                                                 B.OrderNo SourceOrderNo,
                                                 A.MaterialId,
                                                 C.MaterialNo,
                                                 C.MaterialName,
                                                 C.Specifications,
                                                 C.UnitName,
                                                 D.Formula
                                          FROM SdOrderDetail A
                                               JOIN SdOrder B ON A.OrderId = B.ID AND B.IsDeleted = 'false' AND B.IsActive = 'true'
                                               LEFT JOIN BdMaterial_V C ON A.MaterialId = C.ID
                                               LEFT JOIN BdBOMMaterial_V D ON A.MaterialId = D.MaterialId
                                               LEFT JOIN (SELECT SUM (A.QTY) QTY, A.SourceOrderDetailId
                                                          FROM PdOrder A
                                                          WHERE A.IsDeleted = 'false' AND A.IsActive = 'true' AND A.Source='Sales'
                                                          GROUP BY A.SourceOrderDetailId) F
                                                  ON A.ID = F.SourceOrderDetailId
                                          WHERE     A.IsDeleted = 'false'
                                                AND A.IsActive = 'true'
                                                AND B.AuditStatus = 'CompleteAudit') A
                                    WHERE QTY > 0";
        }
        else
        {
            defaultSql = @"SELECT *
                                    FROM(SELECT A.ID,
                                                 A.SerialNumber,
                                                 A.CreatedTime,
                                                 'PdPlan' Source,
                                                 A.ID SourceOrderDetailId,
                                                 A.OrderId SourceOrderId,
                                                 A.QTY OrderQTY,
                                                 A.QTY - ISNULL(F.QTY, 0) QTY,
                                                 B.OrderNo SourceOrderNo,
                                                 A.MaterialId,
                                                 C.MaterialNo,
                                                 C.MaterialName,
                                                 C.Specifications,
                                                 C.UnitName,
                                                 D.Formula
                                          FROM PdPlanDetail A
                                               JOIN PdPlanOrder B ON A.OrderId = B.ID AND B.IsDeleted = 'false' AND B.IsActive = 'true'
                                               LEFT JOIN BdMaterial_V C ON A.MaterialId = C.ID
                                               LEFT JOIN BdBOMMaterial_V D ON A.MaterialId = D.MaterialId
                                               LEFT JOIN
                                               (SELECT SUM(A.QTY) QTY, A.SourceOrderDetailId
                                                FROM PdOrder A
                                                WHERE     A.IsDeleted = 'false'
                                                      AND A.IsActive = 'true'
                                                      AND A.Source = 'PdPlan'
                                                GROUP BY A.SourceOrderDetailId) F
                                                  ON A.ID = F.SourceOrderDetailId
                                          WHERE     A.IsDeleted = 'false'
                                                AND A.IsActive = 'true'
                                                AND B.AuditStatus = 'CompleteAudit') A
                                    WHERE QTY > 0";
        }



        string sql = @"SELECT *
                                FROM(SELECT *, ROW_NUMBER() OVER(ORDER BY CreatedTime DESC) ROWNUM
                                      FROM(SELECT *
                                            FROM (" + defaultSql + ") A) B) C WHERE ROWNUM <= {1} AND ROWNUM > {0}";
        sql = string.Format(sql, startIndex, endIndex);
        obj.data = DBHelper.Instance.QueryList<OrderSourceList>(sql);

        string countSql = @"SELECT COUNT (0) FROM (" + defaultSql + ") A";
        total = Convert.ToInt32(DBHelper.Instance.ExecuteScalar(countSql));
        status = "ok";

        obj.current = current;
        obj.pageSize = pageSize;
        obj.total = total;
        obj.status = status;
        obj.message = message;
        return Ok(obj);
    }

    #endregion

    #region 生成明细
    /// <summary>
    /// 生成明细
    /// </summary>
    /// <param name="Id">生产单ID</param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult GenerateDetail(Guid Id)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        string sql = string.Empty;

        //IDbTransaction trans = DBHelper.Instance.GetNewTransaction();

        try
        {

            Utility.IsNullOrEmpty(Id, "生产单ID不能为空！");

            #region 判断是否有关联单据
            sql = @"SELECT A.ID
                            FROM PdOutDetail A
                                 JOIN PdOrderMaterial B
                                    ON A.SourceOrderDetailId = B.ID AND B.OrderId = '{0}'
                            WHERE A.IsActive = B.IsActive
                                  AND A.IsDeleted = B.IsDeleted
                                  AND A.IsActive = 'true'
                                  AND A.IsDeleted = 'false'";
            sql = string.Format(sql, Id);
            DataTable dt = DBHelper.Instance.GetDataTable(sql);
            if (dt.Rows.Count > 0)
                throw new Exception("该工单已被其他单据关联，不可重新生成数据");
            #endregion

            PdOrder order = _context.PdOrder.Where(o => o.ID == Id).SingleOrDefault();

            var bom = _context.PsBOM.Where(o => o.MaterialId == order.MaterialId).SingleOrDefault();
            var BulkQty = bom.BulkQty;
            #region 删除数据
            sql = "UPDATE PdOrderMaterial SET IsDeleted='true', UpdateBy = '{1}', UpdateTime = GETDATE () WHERE OrderId='{0}' AND IsDeleted='false';" +
                  "UPDATE PdOrderProcess SET IsDeleted='true', UpdateBy = '{1}', UpdateTime = GETDATE () WHERE OrderId='{0}' AND IsDeleted='false';" +
                  "UPDATE PdOrderMould SET IsDeleted='true', UpdateBy = '{1}', UpdateTime = GETDATE () WHERE OrderId='{0}' AND IsDeleted='false'";
            sql = string.Format(sql, Id, UserId);
            DBHelper.Instance.ExecuteDML(sql);
            #endregion

            if (bom != null)
            {
                DateTime? dateTime = Utility.GetSysDate();
                #region 材料明细
                List<PsBOMMaterial> bOMMaterialList = _context.PsBOMMaterial.Where(o => o.BOMId == bom.ID && o.IsActive == true && o.IsDeleted == false).ToList();
                List<PdOrderMaterial> detailList = new List<PdOrderMaterial>();
                int num = 0;
                bOMMaterialList?.ForEach(item =>
                {
                    num++;
                    decimal? WastageRate = 1 + item.WastageRate / 100;
                    decimal? ShouldQTY = (item.Dosage / BulkQty) * item.DosageBase;
                    ShouldQTY = ShouldQTY * order.QTY;
                    ShouldQTY = ShouldQTY * WastageRate;
                    ShouldQTY = StringHelper.TrimDecimal(ShouldQTY, 2);
                    detailList.Add(new PdOrderMaterial()
                    {
                        ID = Guid.NewGuid(),
                        CreatedBy = UserId,
                        CreatedTime = dateTime,
                        GroupId = GroupId,
                        CompanyId = CompanyId,
                        OrderId = Id,
                        SerialNumber = num,
                        MaterialId = item.MaterialId,
                        Dosage = item.Dosage,
                        WastageRate = item.WastageRate,
                        ShouldQTY = ShouldQTY,
                        ActualQTY = 0,
                        PdOrderMaterialStatus = "NoIssue",
                        IsActive = true,
                        AuditStatus = "Add"
                    }); ;
                });
                _context.PdOrderMaterial.AddRange(detailList);
                #endregion

                #region 工艺路线
                List<PdOrderProcess> processList = _context.PsBOMProcess
                    .Where(o => o.BOMId == bom.ID && o.IsActive == true && o.IsDeleted == false)
                    .Select(item => new PdOrderProcess()
                    {
                        ID = Guid.NewGuid(),
                        CreatedBy = UserId,
                        CreatedTime = dateTime,
                        GroupId = GroupId,
                        CompanyId = CompanyId,
                        OrderId = Id,
                        ProcessId = item.ProcessId,
                        MachineId = item.MachineId,
                        WeightUnit = item.WeightUnit,
                        SingleWeight = item.SingleWeight,
                        ProcessingDays = item.ProcessingDays,
                        SetupTime = item.SetupTime,
                        StandardHours = item.StandardHours,
                        TimeUnit = item.TimeUnit,
                        StandardWages = item.StandardWages,
                        IsTransfer = item.IsTransfer,
                        RejectRate = item.RejectRate,
                        IsActive = true
                    }).ToList();
                num = 0;
                processList?.ForEach(item =>
                {
                    num++;
                    item.SerialNumber = num;
                });
                _context.PdOrderProcess.AddRange(processList);

                #endregion

                #region 工模治具
                List<PdOrderMould> mouldList = _context.PsBOMMould
                    .Where(o => o.BOMId == bom.ID && o.IsActive == true && o.IsDeleted == false)
                    .Select(item => new PdOrderMould()
                    {
                        ID = Guid.NewGuid(),
                        CreatedBy = UserId,
                        CreatedTime = dateTime,
                        GroupId = GroupId,
                        CompanyId = CompanyId,
                        OrderId = Id,
                        SerialNumber = num,
                        BOMId = item.BOMId,
                        MouldId = item.MouldId,
                        ProcessId = item.ProcessId,
                        IsActive = true
                    }).ToList();

                num = 0;
                mouldList?.ForEach(item =>
                {
                    num++;
                    item.SerialNumber = num;
                });
                _context.PdOrderMould.AddRange(mouldList);
                #endregion
            }

            #region 更新订单状态
            //DbUpdate du = new DbUpdate("SdOutOrder");
            //du.Set("AuditStatus", "CompleteOut");
            //du.Set("OutTime", Utility.GetSysDate());
            //du.Where("ID", "=", Id);
            //DBHelper.Instance.ExecuteDML(du.GetSql(), null, null, trans);
            #endregion

            _context.SaveChanges();

            #region 导入订单操作历史
            Utility.RecordOperateLog(UserId.ToString(), "SD_OUT_ORDER_MNG", "SdOutOrder", Id.ToString(), OperateType.Update, "CompleteOut", "修改订单审核状态为：CompleteOut");
            #endregion

            //DBHelper.Instance.CommitTransaction(trans);

            status = "ok";
            message = "生成成功！";
        }
        catch (Exception E)
        {
            //DBHelper.Instance.RollbackTransaction(trans);
            message = E.Message;
        }

        obj.status = status;
        obj.message = message;
        return Ok(obj);
    }
    #endregion

    #region 获取工单批量结清列表
    /// <summary>
    /// 获取工单批量结清列表
    /// </summary>
    /// <param name="paramData"></param>
    /// <param name="moduleCode"></param>
    /// <param name="sorter"></param>
    /// <param name="filter"></param>
    /// <param name="parentColumn"></param>
    /// <param name="parentId"></param>
    /// <returns></returns>

    [HttpGet]
    public IActionResult GetCloseList(string paramData, string moduleCode, string sorter = "{}", string filter = "{}", string parentColumn = null, string parentId = null)
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