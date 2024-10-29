/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IvActualCheck.cs
*
*功 能： N / A
* 类 名： IvActualCheck
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 11:51:25  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Api.Controllers;

/// <summary>
/// 实际盘点录入(Controller)
/// </summary>
[ApiController, ApiExplorerSettings(GroupName = Grouping.GroupName_IV)]
public class IvActualCheckController : BaseController1<IvActualCheck>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="_context"></param>
    /// <param name="BaseCrud"></param>
    public IvActualCheckController(DataContext _context, IBaseCRUDVM<IvActualCheck> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    /// <summary>
    /// 新增
    /// </summary>
    /// <param name="Model"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult Add(IvActualCheck Model)
    {


        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            //#region 检查是否存在相同的编码
            //Utility.CheckCodeExist("", "BdColor", "ColorNo", Model.ColorNo, ModifyType.Add, null, "材质编号");
            //#endregion 

            //var supplier = _context.BdSupplier.Where(x => x.ID == Model.SupplierId).SingleOrDefault();
            //if (supplier != null)
            //{
            //    Model.SupplierName = supplier.FullName;
            //}
            Model.OrderNo = Utility.GenerateContinuousSequence("IvActualCheckNo");
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

                //#region 检查单据是否被引用
                //sql = @"SELECT A.ID
                //              FROM SdOutOrderDetail A
                //              WHERE     A.SalesOrderId = '{0}'
                //                    AND A.IsDeleted = 'false'
                //                    AND A.IsActive = 'true'";
                //sql = string.Format(sql, orderId);
                //DataTable dt = DBHelper.Instance.GetDataTable(sql);
                //#endregion

                //if (dt.Rows.Count == 0)
                auditStatus = "Add";
                //else throw new Exception("该单据已被引用，不可撤销！");
            }

            #endregion

            DbUpdate du = new DbUpdate("IvActualCheck");
            du.Set("AuditStatus", auditStatus);
            du.Where("ID", "=", orderId);
            DBHelper.Instance.ExecuteScalar(du.GetSql());

            #region 导入订单操作历史
            Utility.RecordOperateLog(User.Identity.Name, "IV_STOCK_CHECK_MNG", "IvActualCheck", orderId, OperateType.Update, "Audit", "修改订单审核状态为：" + auditStatus);
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

    #region 盘点确认
    /// <summary>
    /// 盘点确认
    /// </summary>
    /// <param name="Id"></param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult ConfirmCheck(Guid Id)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        IDbTransaction trans = DBHelper.Instance.GetNewTransaction();

        try
        {

            IvActualCheck Model = _context.IvActualCheck.Where(x => x.ID == Id).SingleOrDefault();

            if (Model.AuditStatus == "CompleteCheck")
                throw new Exception("该单据已完成调整！");

            //List<IvActualCheckDetailExtend> list = DBHelper.Instance.QueryList<IvActualCheckDetailExtend>(sql);

            //foreach (IvActualCheckDetailExtend item in list)
            //{

            //    decimal qty = IVChangeHelper.GetMaterialInventory(item.MaterialId, item.StockId, item.GoodsLocationId, trans);

            //    #region 判断库存
            //    if (item.AdjustType != "Add")
            //    {
            //        if (qty < item.QTY)
            //            throw new Exception("物料【" + item.MaterialName + "】在【" + item.StockName + "】-【" + item.GoodsLocationName + "】库存不足，当前仓库存为：【" + qty + "】");
            //    }
            //    #endregion

            //    MaterialIVChange change = new MaterialIVChange();
            //    change.CreatedBy = Guid.Parse(User.Identity.Name);
            //    change.MaterialId = item.MaterialId;
            //    change.StockId = item.StockId;
            //    change.GoodsLocationId = item.GoodsLocationId;
            //    change.BeforeQTY = qty;
            //    change.QTY = item.QTY;
            //    if (item.AdjustType == "Add")
            //        change.AfterQTY = qty + item.QTY;
            //    else
            //        change.AfterQTY = qty - item.QTY;
            //    change.ChangeType = IVChangeHelper.ChangeType.InventoryAdjust.ToString();
            //    IVChangeHelper.Add(change);

            //    if (item.AdjustType == "Add")
            //        sql = @"UPDATE BdMaterialInventory
            //            SET QTY = QTY+'{3}'
            //            WHERE MaterialId = '{0}'
            //                  AND StockId = '{1}'
            //                  AND GoodsLocationId = '{2}'";
            //    else
            //        sql = @"UPDATE BdMaterialInventory
            //            SET QTY = QTY-'{3}'
            //            WHERE MaterialId = '{0}'
            //                  AND StockId = '{1}'
            //                  AND GoodsLocationId = '{2}'";

            //    sql = string.Format(sql, item.MaterialId, item.StockId, item.GoodsLocationId, item.QTY);
            //    DBHelper.Instance.ExecuteDML(sql, null, null, trans);
            //}

            DbUpdate du = new DbUpdate("IvActualCheck");
            du.Set("AuditStatus", "CompleteCheck");
            du.Where("ID", "=", Id);
            DBHelper.Instance.ExecuteDML(du.GetSql(), null, null, trans);

            #region 导入订单操作历史
            Utility.RecordOperateLog(User.Identity.Name, "IV_STOCK_CHECK_MNG", "IvActualCheck", Id.ToString(), OperateType.Update, "Audit", "用户进行确认调整");
            #endregion


            DBHelper.Instance.CommitTransaction(trans);

            status = "ok";
            message = "确认成功！";
        }
        catch (Exception E)
        {
            DBHelper.Instance.RollbackTransaction(trans);
            message = E.Message;
        }

        obj.status = status;
        obj.message = message;
        return Ok(obj);
    }
    #endregion

    #region 生成盘点单
    /// <summary>
    /// 生成盘点单
    /// </summary>
    /// <param name="Id"></param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult GenerateCheckDetail(Guid Id)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        //IDbTransaction trans = DBHelper.Instance.GetNewTransaction();

        try
        {

            IvActualCheck Model = _context.IvActualCheck.Where(x => x.ID == Id).SingleOrDefault();

            if (Model.AuditStatus == "CompleteCheck")
                throw new Exception("该单据已完成调整！");

            GenerateCheckDetailData(Id);

            #region 导入订单操作历史
            //DBHelper.RecordOperateLog(User.Identity.Name, "IV_STOCK_CHECK_MNG", "IvActualCheck", Id.ToString(), OperateType.Update, "Audit", "用户进行确认调整");
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
            var Order = _context.IvActualCheck.Where(x => x.ID == Id).SingleOrDefault();

            if (Order == null)
                throw new Exception("无效的数据ID！");

            if (Order.AuditStatus == "CompleteAudit")
                throw new Exception("该单据已审核通过，暂不可进行删除操作！");

            if (Order.AuditStatus == "CompleteCheck")
                throw new Exception("该单据已完成盘点确认，暂不可进行删除操作！");

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
            Update<IvActualCheck>(modelModify);
            _context.SaveChanges();

            Guid Id = Guid.Parse(modelModify.ID.Value);
            GenerateCheckDetailData(Id);

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

    #region 自动生成明细数据
    /// <summary>
    /// 自动生成明细数据
    /// </summary>
    /// <param name="Id">订单ID</param>
    private void GenerateCheckDetailData(Guid Id)
    {
        IDbTransaction trans = DBHelper.Instance.GetNewTransaction();

        try
        {

            IvActualCheck Model = _context.IvActualCheck.Where(x => x.ID == Id).SingleOrDefault();

            if (Model != null)
            {
                string sql = string.Empty;
                string condition = string.Empty;

                #region 删除明细数据
                DbUpdate du = new DbUpdate("IvActualCheckDetail");
                du.Set("IsDeleted", "true");
                du.Where("OrderId", "=", Id);
                DBHelper.Instance.ExecuteDML(du.GetSql(), null, null, trans);
                #endregion

                Guid? StockId = Model.StockId;
                if (StockId != null)
                    condition = condition + " AND B.StockId='" + StockId + "'";

                Guid? GoodsLocationId = Model.GoodsLocationId;
                if (GoodsLocationId != null)
                    condition = condition + " AND B.GoodsLocationId='" + GoodsLocationId + "'";

                Guid? MaterialId = Model.MaterialId;
                if (MaterialId != null)
                    condition = condition + " AND B.MaterialId='" + MaterialId + "'";

                sql = @"INSERT INTO IvActualCheckDetail (ID,
                           CreatedBy,
                           CreatedTime,
                           GroupId,
                           CompanyId,
                           OrderId,
                           MaterialId,
                           UnitId,
                           StockId,
                           GoodsLocationId,
                           QTY,
                           SerialNumber)
                           SELECT *
                           FROM (SELECT *,
                                        ROW_NUMBER () OVER (ORDER BY CreatedTime DESC) SerialNumber
                                 FROM (SELECT NewID () ID,
                                              '{0}' CreatedBy,
                                              GETDATE () CreatedTime,
                                              '{1}' GroupId,
                                              '{2}' CompanyId,
                                              '{3}' OrderId,
                                              A.ID MaterialId,
                                              A.UnitId,
                                              B.StockId,
                                              B.GoodsLocationId,
                                              ISNULL (B.QTY, 0) QTY
                                       FROM BdMaterial_V A
                                            LEFT JOIN BdMaterialInventory B ON A.ID = B.MaterialId
                                       WHERE     1 = 1
                                             AND A.IsDeleted = 'false'
                                             AND A.IsActive = 'true'
                                             {4})
                                      B) A";
                sql = string.Format(sql, User.Identity.Name, GetGroupId(), GetCompanyId(), Id, condition);
                DBHelper.Instance.ExecuteDML(sql, null, null, trans);
                DBHelper.Instance.CommitTransaction(trans);

            }
        }
        catch (Exception)
        {
            DBHelper.Instance.RollbackTransaction(trans);
        }


    }
    #endregion

    #region 获取存货盘点
    /// <summary>
    /// 获取存货盘点
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public virtual IActionResult GetCheckList(string paramData, string masterId)
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

            string defaultSql = @"SELECT A.ID,
                                           A.GroupId,
                                           A.CompanyId,
                                           A.CreatedTime,
                                           A.OrderId,
                                           A.OrderId SourceOrderId,
                                           B.OrderNo SourceOrderNo,
                                           A.ID SourceOrderDetailId,
                                           B.CheckName,
                                           A.MaterialId,
                                           A.StockId,
                                           A.GoodsLocationId,
                                           A.QTY,
                                           A.Remark,
                                           D.GoodsLocationName,
                                           E.MaterialNo,
                                           E.MaterialNames MaterialName,
                                           D.StockName
                                    FROM IvCheckDetail A
                                         JOIN IvCheck B
                                            ON A.OrderId = B.ID AND B.IsDeleted = 'false' AND B.IsActive = 'true'
                                         LEFT JOIN IvActualCheckDetail C
                                            ON     A.ID = C.SourceOrderDetailId
                                               AND C.IsDeleted = 'false'
                                               AND C.IsActive = 'true'
                                         LEFT JOIN BdGoodsLocation_V D ON A.GoodsLocationId = D.ID
                                         LEFT JOIN BdMaterial E ON A.MaterialId = E.ID
                                    WHERE     A.IsDeleted = 'false'
                                          AND A.IsActive = 'true'
                                          AND C.ID IS NULL
                                          AND A.QTY > 0";
            defaultSql = string.Format(defaultSql);

            string sql = @"SELECT * FROM(SELECT *, ROW_NUMBER() OVER(ORDER BY CreatedTime ASC) ROWNUM FROM(" + defaultSql + ") B) C WHERE ROWNUM <= {1} AND ROWNUM > {0}";
            sql = string.Format(sql, startIndex, endIndex);
            List<IvActualCheckDetailExtend> list = DBHelper.Instance.QueryList<IvActualCheckDetailExtend>(sql);

            obj.data = list;
            string countString = @"SELECT COUNT (1)
                                 FROM IvCheckDetail A
                                         JOIN IvCheck B
                                            ON A.OrderId = B.ID AND B.IsDeleted = 'false' AND B.IsActive = 'true'
                                         LEFT JOIN IvActualCheckDetail C ON A.ID = C.SourceOrderDetailId
                                    WHERE     A.IsDeleted = 'false'
                                          AND A.IsActive = 'true'
                                          AND C.ID IS NULL
                                          AND A.QTY > 0";
            //countString = string.Format(countString);
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

}