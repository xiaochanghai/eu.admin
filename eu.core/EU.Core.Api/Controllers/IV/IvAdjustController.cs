/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IvAdjust.cs
*
*功 能： N / A
* 类 名： IvAdjust
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 11:56:40  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 库存调整单(Controller)
/// </summary>
[ApiController, ApiExplorerSettings(GroupName = Grouping.GroupName_IV)]
public class IvAdjustController : BaseController1<IvAdjust>
{

    public IvAdjustController(DataContext _context, IBaseCRUDVM<IvAdjust> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    [HttpPost]
    public override IActionResult Add(IvAdjust Model)
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
            Model.OrderNo = Utility.GenerateContinuousSequence("IvAdjustNo");
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

            DbUpdate du = new DbUpdate("IvAdjust");
            du.Set("AuditStatus", auditStatus);
            du.Where("ID", "=", orderId);
            DBHelper.Instance.ExecuteScalar(du.GetSql());

            #region 导入订单操作历史
            Utility.RecordOperateLog(User.Identity.Name, "IV_STOCK_ADJUST_MNG", "IvAdjust", orderId, OperateType.Update, "Audit", "修改订单审核状态为：" + auditStatus);
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

    #region 确认调整
    [HttpPost]
    public IActionResult ConfirmAdjust(Guid Id)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        string sql = string.Empty;
        IDbTransaction trans = DBHelper.Instance.GetNewTransaction();

        try
        {

            IvAdjust Model = _context.IvAdjust.Where(x => x.ID == Id).SingleOrDefault();

            if (Model.AuditStatus == "CompleteAdjust")
                throw new Exception("该单据已完成调整！");

            #region 判断单据库存
            //sql = @"SELECT *
            //        FROM (SELECT A.QTY Adjust_QTY,
            //                     D.MaterialNames MaterialName,
            //                     ISNULL (B.QTY, 0) QTY,
            //                     C.GoodsLocationName,
            //                     C.StockName
            //              FROM (SELECT SUM (A.QTY) QTY,
            //                           A.MaterialId,
            //                           A.StockId,
            //                           A.GoodsLocationId
            //                    FROM IvAdjustDetail A
            //                    WHERE     A.IsDeleted = 'false'
            //                          AND A.IsActive = 'true'
            //                          AND A.OrderId = '{0}'
            //                          AND A.GoodsLocationId IS NOT NULL
            //                          AND A.AdjustType = 'Reduce'
            //                    GROUP BY A.MaterialId, A.StockId, A.GoodsLocationId) A
            //                   LEFT JOIN BdMaterialInventory B
            //                      ON     A.MaterialId = B.MaterialId
            //                         AND A.StockId = B.StockId
            //                         AND A.GoodsLocationId = B.GoodsLocationId
            //                         AND B.IsDeleted = 'false'
            //                   LEFT JOIN BdGoodsLocation_V C ON A.GoodsLocationId = C.ID
            //                   LEFT JOIN BdMaterial D ON A.MaterialId = D.ID) A
            //        WHERE A.Adjust_QTY > QTY";
            //sql = string.Format(sql, Id);
            //DataTable dt = DBHelper.Instance.GetDataTable(sql);
            //if (dt.Rows.Count > 0)
            //{
            //    decimal QTY = Convert.ToDecimal(dt.Rows[0]["QTY"]);
            //    throw new Exception("物料【" + dt.Rows[0]["MaterialName"] + "】在【" + dt.Rows[0]["StockName"] + "】【" + dt.Rows[0]["GoodsLocationName"] + "】库存不足，当前仓库存为：【" + QTY + "】！");
            //}
            #endregion

            sql = @"SELECT A.*,
                               B.StockName,
                               B.GoodsLocationName,
                               C.MaterialNo,
                               C.MaterialName,
                               C.UnitId,
                               C.Specifications
                        FROM IvAdjustDetail A
                             LEFT JOIN BdGoodsLocation_V B ON A.GoodsLocationId = B.ID
                             LEFT JOIN BdMaterial_V C ON A.MaterialId = C.ID
                        WHERE A.OrderId = '{0}' AND A.IsDeleted = 'false' AND A.IsActive = 'true'
                        ORDER BY A.SerialNumber ASC";
            sql = string.Format(sql, Id);
            List<IvAdjustDetailExtend> list = DBHelper.Instance.QueryList<IvAdjustDetailExtend>(sql);

            foreach (IvAdjustDetailExtend item in list)
            {

                var qty = IVChangeHelper.GetMaterialInventory(item.MaterialId, item.StockId, item.GoodsLocationId, trans);

                #region 判断库存
                if (item.AdjustType != "Add")
                {
                    if (qty < item.QTY)
                        throw new Exception("物料【" + item.MaterialName + "】在【" + item.StockName + "】-【" + item.GoodsLocationName + "】库存不足，当前仓库存为：【" + qty + "】");
                }
                #endregion

                BdMaterialIVChange change = new BdMaterialIVChange();
                change.CreatedBy = Guid.Parse(User.Identity.Name);
                change.MaterialId = item.MaterialId;
                change.StockId = item.StockId;
                change.GoodsLocationId = item.GoodsLocationId;
                change.BeforeQTY = qty;
                change.QTY = item.QTY;
                if (item.AdjustType == "Add")
                    change.AfterQTY = qty + item.QTY;
                else
                    change.AfterQTY = qty - item.QTY;
                change.ChangeType = IVChangeHelper.ChangeType.InventoryAdjust.ToString();
                change.OrderId = item.OrderId;
                change.OrderDetailId = item.ID;
                IVChangeHelper.Add(change, trans);

                if (item.AdjustType == "Add")
                    sql = @"UPDATE BdMaterialInventory
                            SET QTY = QTY+'{3}'
                            WHERE MaterialId = '{0}'
                                  AND StockId = '{1}'
                                  AND GoodsLocationId = '{2}'";
                else
                    sql = @"UPDATE BdMaterialInventory
                            SET QTY = QTY-'{3}'
                            WHERE MaterialId = '{0}'
                                  AND StockId = '{1}'
                                  AND GoodsLocationId = '{2}'";

                sql = string.Format(sql, item.MaterialId, item.StockId, item.GoodsLocationId, item.QTY);
                DBHelper.Instance.ExecuteDML(sql, null, null, trans);
            }

            DbUpdate du = new DbUpdate("IvAdjust");
            du.Set("AuditStatus", "CompleteAdjust");
            du.Where("ID", "=", Id);
            DBHelper.Instance.ExecuteDML(du.GetSql(), null, null, trans);

            DBHelper.Instance.CommitTransaction(trans);

            #region 导入订单操作历史
            Utility.RecordOperateLog(User.Identity.Name, "IV_STOCK_ADJUST_MNG", "IvAdjust", Id.ToString(), OperateType.Update, "Audit", "用户进行确认调整");
            #endregion



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
            var Order = _context.IvAdjust.Where(x => x.ID == Id).SingleOrDefault();

            if (Order == null)
                throw new Exception("无效的数据ID！");

            if (Order.AuditStatus == "CompleteAudit")
                throw new Exception("该单据已审核通过，暂不可进行删除操作！");

            if (Order.AuditStatus == "CompleteAdjust")
                throw new Exception("该单据已完成调整，暂不可进行删除操作！");

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
}