/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IvOtherIn.cs
*
*功 能： N / A
* 类 名： IvOtherIn
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 13:34:17  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 其他入库单(Controller)
/// </summary>
[ApiExplorerSettings(GroupName = Grouping.GroupName_IV)]
public class IvOtherInController : BaseController1<IvOtherIn>
{

    public IvOtherInController(DataContext _context, IBaseCRUDVM<IvOtherIn> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    [HttpPost]
    public override IActionResult Add(IvOtherIn Model)
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
            Model.OrderNo = Utility.GenerateContinuousSequence("IvOtherInNo");
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

            DbUpdate du = new DbUpdate("IvOtherIn");
            du.Set("AuditStatus", auditStatus);
            du.Where("ID", "=", orderId);
            DBHelper.Instance.ExecuteScalar(du.GetSql());

            #region 导入订单操作历史
            Utility.RecordOperateLog(User.Identity.Name, "IV_STOCK_OTHER_IN_MNG", "IvOtherIn", orderId, OperateType.Update, "Audit", "修改订单审核状态为：" + auditStatus);
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
            var Order = _context.IvOtherIn.Where(x => x.ID == Id).SingleOrDefault();

            if (Order == null)
                throw new Exception("无效的数据ID！");

            if (Order.AuditStatus == "CompleteAudit")
                throw new Exception("该单据已审核通过，暂不可进行删除操作！");

            if (Order.AuditStatus == "CompleteIn")
                throw new Exception("该单据已完成入库，暂不可进行删除操作！");

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

    #region 确认入库
    [HttpPost]
    public IActionResult ConfirmIn(Guid Id)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        IDbTransaction trans = DBHelper.Instance.GetNewTransaction();

        try
        {

            IvOtherIn Model = _context.IvOtherIn.Where(x => x.ID == Id).SingleOrDefault();

            if (Model.AuditStatus == "CompleteIn")
                throw new Exception("该单据已完成入库！");

            string sql = @"SELECT A.*
                                FROM IvOtherInDetail A
                                WHERE     A.OrderId = '{0}'
                                      AND A.IsDeleted = 'false'
                                      AND A.IsActive = 'true'
                                      AND A.StockId IS NOT NULL
                                      AND A.GoodsLocationId IS NOT NULL
                                ORDER BY A.SerialNumber ASC";
            sql = string.Format(sql, Id);
            List<IvOtherInDetail> list = DBHelper.Instance.QueryList<IvOtherInDetail>(sql);

            foreach (IvOtherInDetail item in list)
            {

                var qty = IVChangeHelper.GetMaterialInventory(item.MaterialId, item.StockId, item.GoodsLocationId, trans);

                BdMaterialIVChange change = new BdMaterialIVChange();
                change.CreatedBy = Guid.Parse(User.Identity.Name);
                change.MaterialId = item.MaterialId;
                change.StockId = item.StockId;
                change.GoodsLocationId = item.GoodsLocationId;
                change.BeforeQTY = qty;
                change.QTY = item.QTY;
                change.AfterQTY = qty + item.QTY;
                change.ChangeType = IVChangeHelper.ChangeType.InventoryOtherIn.ToString();
                change.OrderId = item.OrderId;
                change.OrderDetailId = item.ID;
                IVChangeHelper.Add(change, trans);

                sql = @"UPDATE BdMaterialInventory
                            SET QTY = QTY+'{3}'
                            WHERE MaterialId = '{0}'
                                  AND StockId = '{1}'
                                  AND GoodsLocationId = '{2}'";
                sql = string.Format(sql, item.MaterialId, item.StockId, item.GoodsLocationId, item.QTY);
                DBHelper.Instance.ExecuteDML(sql, null, null, trans);
            }

            DbUpdate du = new DbUpdate("IvOtherIn");
            du.Set("AuditStatus", "CompleteIn");
            du.Where("ID", "=", Id);
            DBHelper.Instance.ExecuteDML(du.GetSql(), null, null, trans);

            #region 导入订单操作历史
            Utility.RecordOperateLog(User.Identity.Name, "IV_STOCK_OTHER_IN_MNG", "IvOtherIn", Id.ToString(), OperateType.Update, "Audit", "用户进行确认其他入库");
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
}