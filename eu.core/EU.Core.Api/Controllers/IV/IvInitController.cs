/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IvInit.cs
*
*功 能： N / A
* 类 名： IvInit
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 11:36:35  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 库存初始化(Controller)
/// </summary>
[ApiController, ApiExplorerSettings(GroupName = Grouping.GroupName_IV)]
public class IvInitController : BaseController1<IvInit>
{

    public IvInitController(DataContext _context, IBaseCRUDVM<IvInit> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    [HttpPost]
    public override IActionResult Add(IvInit Model)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            //#region 检查是否存在相同的编码
            //Utility.CheckCodeExist("", "BdColor", "ColorNo", Model.ColorNo, ModifyType.Add, null, "材质编号");
            //#endregion 

            Model.OrderNo = Utility.GenerateContinuousSequence("IvInitNo");

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

            //string SupplierId = modelModify.SupplierId;
            //var supplier = _context.BdSupplier.Where(x => x.ID == Guid.Parse(SupplierId)).SingleOrDefault();
            //if (supplier != null)
            //{
            //    modelModify.SupplierName = supplier.FullName;
            //    modelModify.TaxType = supplier.TaxType;
            //    modelModify.TaxRate = supplier.TaxRate;
            //    modelModify.DeliveryWayId = supplier.DeliveryWayId;
            //    modelModify.SettlementWayId = supplier.SettlementWayId;
            //}
            decimal QTY = Convert.ToDecimal(modelModify.QTY);
            decimal Price = Convert.ToDecimal(modelModify.Price);
            modelModify.Amount = QTY * Price;

            Update<IvInit>(modelModify);
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

            DbUpdate du = new DbUpdate("IvInit");
            du.Set("AuditStatus", auditStatus);
            du.Where("ID", "=", orderId);
            DBHelper.Instance.ExecuteScalar(du.GetSql());

            #region 导入订单操作历史
            Utility.RecordOperateLog(User.Identity.Name, "IV_STOCK_INIT_MNG", "IvInit", orderId, OperateType.Update, "Audit", "修改订单审核状态为：" + auditStatus);
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

    #region 初始化数据
    [HttpPost]
    public IActionResult Init(Guid Id)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        string sql = string.Empty;
        IDbTransaction trans = DBHelper.Instance.GetNewTransaction();

        try
        {
            IvInit Model = _context.IvInit.Where(x => x.ID == Id).SingleOrDefault();

            if (Model == null)
                throw new Exception("无效的单据ID！");

            if (Model.AuditStatus == "CompleteInit")
                throw new Exception("该单据已完成初始化！");

            sql = @"SELECT A.*
                            FROM IvInitDetail A
                            WHERE A.OrderId = '{0}' AND A.IsDeleted = 'false' AND A.IsActive = 'true'
                            ORDER BY A.SerialNumber ASC";
            sql = string.Format(sql, Id);

            List<IvInitDetail> list = DBHelper.Instance.QueryList<IvInitDetail>(sql);
            foreach (IvInitDetail item in list)
            {
                item.InitTime = Utility.GetSysDate();
                decimal? qty = IVChangeHelper.GetMaterialInventory(item.MaterialId, item.StockId, item.GoodsLocationId, trans);

                BdMaterialIVChange change = new BdMaterialIVChange();
                if (qty > 0)
                {
                    change.CreatedBy = Guid.Parse(User.Identity.Name);
                    change.MaterialId = item.MaterialId;
                    change.StockId = item.StockId;
                    change.GoodsLocationId = item.GoodsLocationId;
                    change.BeforeQTY = qty;
                    change.QTY = qty;
                    change.AfterQTY = 0;
                    change.ChangeType = IVChangeHelper.ChangeType.InventoryInit.ToString();
                    change.OrderId = item.OrderId;
                    change.OrderDetailId = item.ID;
                    IVChangeHelper.Add(change, trans);
                }

                change = new BdMaterialIVChange();
                change.CreatedBy = Guid.Parse(User.Identity.Name);
                change.MaterialId = item.MaterialId;
                change.StockId = item.StockId;
                change.GoodsLocationId = item.GoodsLocationId;
                change.BeforeQTY = 0;
                change.QTY = item.QTY;
                change.AfterQTY = item.QTY;
                change.ChangeType = IVChangeHelper.ChangeType.InventoryInit.ToString();
                change.OrderId = item.OrderId;
                change.OrderDetailId = item.ID;
                IVChangeHelper.Add(change, trans);

                sql = @"UPDATE BdMaterialInventory
                            SET QTY = '{3}'
                            WHERE MaterialId = '{0}'
                                  AND StockId = '{1}'
                                  AND GoodsLocationId = '{2}'";
                sql = string.Format(sql, item.MaterialId, item.StockId, item.GoodsLocationId, item.QTY);
                DBHelper.Instance.ExecuteDML(sql, null, null, trans);

                sql = @"UPDATE IvInitDetail
                            SET InitTime = GETDATE()
                            WHERE ID = '{0}'";
                sql = string.Format(sql, item.ID);
                DBHelper.Instance.ExecuteDML(sql, null, null, trans);
            }

            sql = @"UPDATE IvInit
                            SET AuditStatus = 'CompleteInit'
                            WHERE ID = '{0}'";
            sql = string.Format(sql, Id);
            DBHelper.Instance.ExecuteDML(sql, null, null, trans);

            DBHelper.Instance.CommitTransaction(trans);

            #region 导入订单操作历史
            Utility.RecordOperateLog(User.Identity.Name, "IV_STOCK_INIT_MNG", "IvInit", Id.ToString(), OperateType.Update, "Update", "用户进行确认初始化");
            #endregion


            status = "ok";
            message = "初始化成功！";
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

            if (Order.AuditStatus == "CompleteInit")
                throw new Exception("该单据已完成初始化，暂不可进行删除操作！");

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