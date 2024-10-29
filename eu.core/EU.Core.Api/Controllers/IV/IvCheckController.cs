/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IvCheck.cs
*
*功 能： N / A
* 类 名： IvCheck
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 13:28:44  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 库存盘点单(Controller)
/// </summary>
[ApiController, ApiExplorerSettings(GroupName = Grouping.GroupName_IV)]
public class IvCheckController : BaseController1<IvCheck>
{

    public IvCheckController(DataContext _context, IBaseCRUDVM<IvCheck> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    [HttpPost]
    public override IActionResult Add(IvCheck Model)
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
            Model.OrderNo = Utility.GenerateContinuousSequence("IvCheckNo");
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

            DbUpdate du = new DbUpdate("IvCheck");
            du.Set("AuditStatus", auditStatus);
            du.Where("ID", "=", orderId);
            DBHelper.Instance.ExecuteScalar(du.GetSql());

            #region 导入订单操作历史
            Utility.RecordOperateLog(User.Identity.Name, "IV_STOCK_CHECK_MNG", "IvCheck", orderId, OperateType.Update, "Audit", "修改订单审核状态为：" + auditStatus);
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
    [HttpPost]
    public IActionResult ConfirmCheck(Guid Id)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        IDbTransaction trans = DBHelper.Instance.GetNewTransaction();

        try
        {

            IvCheck Model = _context.IvCheck.Where(x => x.ID == Id).SingleOrDefault();

            if (Model.AuditStatus == "CompleteCheck")
                throw new Exception("该单据已完成调整！");

            //List<IvCheckDetailExtend> list = DBHelper.Instance.QueryList<IvCheckDetailExtend>(sql);

            //foreach (IvCheckDetailExtend item in list)
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

            DbUpdate du = new DbUpdate("IvCheck");
            du.Set("AuditStatus", "CompleteCheck");
            du.Where("ID", "=", Id);
            DBHelper.Instance.ExecuteDML(du.GetSql(), null, null, trans);

            #region 导入订单操作历史
            Utility.RecordOperateLog(User.Identity.Name, "IV_STOCK_CHECK_MNG", "IvCheck", Id.ToString(), OperateType.Update, "Audit", "用户进行确认调整");
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
    [HttpPost]
    public IActionResult GenerateCheckDetail(Guid Id)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        //IDbTransaction trans = DBHelper.Instance.GetNewTransaction();

        try
        {

            IvCheck Model = _context.IvCheck.Where(x => x.ID == Id).SingleOrDefault();

            if (Model.AuditStatus == "CompleteCheck")
                throw new Exception("该单据已完成调整！");

            GenerateCheckDetailData(Id);

            #region 导入订单操作历史
            //DBHelper.RecordOperateLog(User.Identity.Name, "IV_STOCK_CHECK_MNG", "IvCheck", Id.ToString(), OperateType.Update, "Audit", "用户进行确认调整");
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
            var Order = _context.IvCheck.Where(x => x.ID == Id).SingleOrDefault();

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
    [HttpPost]
    public override IActionResult Update(dynamic modelModify)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            Update<IvCheck>(modelModify);
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

            IvCheck Model = _context.IvCheck.Where(x => x.ID == Id).SingleOrDefault();

            if (Model != null)
            {
                string sql = string.Empty;
                string condition = string.Empty;

                #region 删除明细数据
                DbUpdate du = new DbUpdate("IvCheckDetail");
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

                sql = @"INSERT INTO IvCheckDetail (ID,
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

}