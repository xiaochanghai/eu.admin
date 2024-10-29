/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* MfInOrderDetail.cs
*
*功 能： N / A
* 类 名： MfInOrderDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 14:30:04  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// MfInOrderDetail(Controller)
/// </summary>
[ApiExplorerSettings(GroupName = Grouping.GroupName_MF)]
public class MfInOrderDetailController : BaseController1<MfInOrderDetail>
{

    public MfInOrderDetailController(DataContext _context, IBaseCRUDVM<MfInOrderDetail> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    [HttpPost]
    public override IActionResult Add(MfInOrderDetail Model)
    {


        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            #region 检查是否存在相同的编码
            //Utility.CheckCodeExist("", "BdColor", "ColorNo", Model.ColorNo, ModifyType.Add, null, "材质编号");
            #endregion

            Model.SerialNumber = Utility.GenerateContinuousSequence("MfInOrderDetail", "SerialNumber", "OrderId", Model.OrderId.ToString());
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

    [HttpPost]
    public override IActionResult BatchAdd(List<MfInOrderDetail> data)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            Guid? OrderId = data[0].OrderId;
            var order = _context.SdOrder.Where(x => x.ID == OrderId).SingleOrDefault();

            //var ShipOrder = _context.SdShipOrder.Where(x => x.ID == OrderId).SingleOrDefault();
            //List<MfInOrderDetail> list = new List<MfInOrderDetail>();
            //int i = 1;
            //foreach (MfInOrderDetail item in data)
            //{
            //    MfInOrderDetail Model = new MfInOrderDetail();
            //    Model.ID = Guid.NewGuid();
            //    DoAddPrepare(Model);
            //    Model.OrderId = item.ShipOrderId;
            //    Model.SalesOrderId = item.SalesOrderId;
            //    Model.MaterialId = item.MaterialId;
            //    Model.MaterialName = item.MaterialName;
            //    Model.MaterialSpecifications = item.MaterialSpecifications;
            //    Model.UnitId = item.UnitId;
            //    Model.SerialNumber = i;
            //    Model.SalesMfInOrderDetailId = item.ID;
            //    Model.NoShipQTY = item.UnfilledOrderyQTY;
            //    Model.ShipQTY = item.ShipQTY;
            //    Model.CustomerMaterialCode = item.CustomerMaterialCode;
            //    Model.StockId = item.StockId;
            //    Model.GoodsLocationId = item.GoodsLocationId;
            //    Model.DeliveryrDate = item.DeliveryrDate;
            //    Model.ShipDate = ShipOrder.ShipDate;
            //    if (item.ShipQTY > 0)
            //    {
            //        list.Add(Model);
            //        i++;
            //    }
            //}
            for (int i = 0; i < data.Count; i++)
            {
                data[i].ID = Guid.NewGuid();
                DoAddPrepare(data[i]);
                data[i].CreatedBy = new Guid(User.Identity.Name);
            }

            if (data.Count > 0)
                DBHelper.Instance.AddRange(data);

            BatchUpdateSerialNumber(OrderId.ToString());

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

    #region 更新重写
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

            Update<MfInOrderDetail>(modelModify);
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

    #region 批量更新排序号
    /// <summary>
    /// 批量更新排序号
    /// </summary>
    /// <param name="orderId">订单ID</param>
    private void BatchUpdateSerialNumber(string orderId)
    {
        string sql = @"UPDATE A
                        SET A.SerialNumber = C.NUM
                        FROM MfInOrderDetail A
                             JOIN
                             (SELECT *, ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) NUM
                              FROM (SELECT *
                                    FROM (SELECT A.*
                                          FROM MfInOrderDetail A
                                          WHERE     1 = 1
                                                AND A.OrderId =
                                                    '{0}'
                                                AND A.IsDeleted = 'false'
                                                AND A.IsActive = 'true') A) B) C
                                ON A.ID = C.ID";
        sql = string.Format(sql, orderId);
        DBHelper.Instance.ExecuteScalar(sql);

    }
    #endregion

    #region 删除

    [HttpGet]
    public override IActionResult Delete(Guid Id)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            _BaseCrud.DoDelete(Id);

            MfInOrderDetail Model = _context.MfInOrderDetail.Where(x => x.ID == Id).SingleOrDefault();
            if (Model != null)
                BatchUpdateSerialNumber(Model.OrderId.ToString());

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

    [HttpPost]
    public override IActionResult BatchDelete(List<MfInOrderDetail> entryList)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {

            for (int i = 0; i < entryList.Count; i++)
            {
                DbUpdate du = new DbUpdate("MfInOrderDetail");
                du.Set("IsDeleted", "true");
                du.Where("ID", "=", entryList[i].ID);
                DBHelper.Instance.ExecuteScalar(du.GetSql());
            }

            MfInOrderDetail Model = _context.MfInOrderDetail.Where(x => x.ID == entryList[0].ID).SingleOrDefault();
            if (Model != null)
                BatchUpdateSerialNumber(Model.OrderId.ToString());

            status = "ok";
            message = "批量删除成功！";
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


    //[HttpPost]
    //public override IActionResult BatchDelete(List<MfInOrderDetail> entryList)
    //{
    //    dynamic obj = new ExpandoObject();
    //    string status = "error";
    //    string message = string.Empty;

    //    try
    //    {
    //        for (int i = 0; i < entryList.Count; i++)
    //        {
    //            var navigations = _context.Entry(entryList[i]).Navigations.ToList();
    //            for (int n = 0; n < navigations.Count; n++)
    //            {
    //                navigations[n].CurrentValue = null;
    //            }

    //            entryList[i].IsDeleted = true;
    //            _context.Update(entryList[i]);
    //        }
    //        _context.SaveChanges();

    //        if (entryList.Count > 0)
    //        {
    //            MfInOrderDetail Model = _context.MfInOrderDetail.Where(x => x.ID == entryList[0].ID).SingleOrDefault();
    //            if (Model != null)
    //                BatchUpdateSerialNumber(Model.OrderId.ToString());
    //        }

    //        status = "ok";
    //        message = "批量删除成功！";
    //    }
    //    catch (Exception E)
    //    {
    //        message = E.Message;
    //    }

    //    obj.status = status;
    //    obj.message = message;
    //    return Ok(obj);
    //}
}