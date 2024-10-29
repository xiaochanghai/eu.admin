/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PdOrderMaterial.cs
*
*功 能： N / A
* 类 名： PdOrderMaterial
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 14:39:50  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 生产工单-材料明细
/// </summary>
[GlobalActionFilter, ApiExplorerSettings(GroupName = Grouping.GroupName_PD)]
public class PdOrderMaterialController : BaseController1<PdOrderMaterial>
{
    /// <summary>
    /// 生产工单-材料明细
    /// </summary>
    /// <param name="_context"></param>
    /// <param name="BaseCrud"></param>
    public PdOrderMaterialController(DataContext _context, IBaseCRUDVM<PdOrderMaterial> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    /// <summary>
    /// 新增
    /// </summary>
    /// <param name="Model"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult Add(PdOrderMaterial Model)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            #region 检查是否存在相同的编码
            //Utility.CheckCodeExist("", "BdColor", "ColorNo", Model.ColorNo, ModifyType.Add, null, "材质编号");
            #endregion
            //Model.OrderDetailNo = Utility.GenerateContinuousSequence("SdOrderDetailNo");

            Model.SerialNumber = Utility.GenerateContinuousSequence("PdOrderMaterial", "SerialNumber", "OrderId", Model.OrderId.ToString());
            Model.PdOrderMaterialStatus = "NoIssue";

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
    public override IActionResult BatchAdd(List<PdOrderMaterial> list)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        string OrderId = string.Empty;

        try
        {
            if (list.Count > 0)
            {
                OrderId = list[0].OrderId.ToString();
                //POOrder order = _context.PoOrder.Where(x => x.ID == Guid.Parse(OrderId)).SingleOrDefault();
                //Supplier supplier = _context.BdSupplier.Where(x => x.ID == Guid.Parse(OrderId)).SingleOrDefault();

                for (int i = 0; i < list.Count; i++)
                {
                    list[i].ID = Guid.NewGuid();
                    list[i].CreatedTime = Utility.GetSysDate();
                    DoAddPrepare(list[i]);
                }
                DBHelper.Instance.AddRange(list);
                BatchUpdateSerialNumber(OrderId);
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

    #region 更新重写
    ///// <summary>
    ///// 
    ///// </summary>
    ///// <param name="modelModify"></param>
    ///// <returns></returns>
    //[HttpPost]
    //public override IActionResult Update(dynamic modelModify)
    //{

    //    dynamic obj = new ExpandoObject();
    //    string status = "error";
    //    string message = string.Empty;

    //    try
    //    {
    //        string id = modelModify.ID.Value;
    //        var PdOrderMaterial = _context.PdOrderMaterial.Where(x => x.ID == Guid.Parse(id)).SingleOrDefault();
    //        if (PdOrderMaterial == null)
    //            throw new Exception("无效的数据ID！");

    //        decimal NewArrivalQTY = modelModify.ArrivalQTY.Value;
    //        decimal ArrivalQTY = PdOrderMaterial.ArrivalQTY;

    //        if (NewArrivalQTY > ArrivalQTY)
    //        {
    //            Guid? SourceOrderDetailId = PdOrderMaterial.SourceOrderDetailId;
    //            var ArrivalOrder = _context.PoArrivalOrder.Where(x => x.ID == PdOrderMaterial.OrderId).SingleOrDefault();
    //            string sql = @"SELECT A.PurchaseQTY - ISNULL (D.ArrivalQTY, 0) ArrivalQTY
    //                                FROM PoOrderDetail A
    //                                     JOIN PoOrder B
    //                                        ON     A.OrderId = B.ID
    //                                           AND B.IsDeleted = 'false'
    //                                           AND B.IsActive = 'true'
    //                                           AND B.SupplierId = '{1}'
    //                                           AND B.AuditStatus = 'CompleteAudit'
    //                                     LEFT JOIN
    //                                     (SELECT SUM (A.ArrivalQTY) ArrivalQTY, A.SourceOrderDetailId
    //                                      FROM PoPdOrderMaterial A
    //                                           JOIN PoArrivalOrder B
    //                                              ON     A.OrderId = B.ID
    //                                                 AND B.IsActive = 'true'
    //                                                 AND B.IsDeleted = 'false'
    //                                      WHERE A.IsActive = 'true' AND B.IsDeleted = 'false'
    //                                      GROUP BY A.SourceOrderDetailId) D
    //                                        ON A.ID = D.SourceOrderDetailId
    //                                WHERE A.IsDeleted = 'false' AND A.IsActive = 'true' AND A.ID = '{0}'";
    //            sql = string.Format(sql, SourceOrderDetailId, ArrivalOrder.SupplierId);
    //            decimal QTY = Convert.ToDecimal(DBHelper.Instance.ExecuteScalar(sql));
    //            if (QTY < (NewArrivalQTY - ArrivalQTY))
    //                throw new Exception("待到货数量不足，当前待到货:" + QTY + "！");
    //        }

    //        DbUpdate du = new DbUpdate("PoPdOrderMaterial", "ID", id);
    //        du.Set("ArrivalQTY", NewArrivalQTY);
    //        du.Set("ReserveDeliveryTime", modelModify.ReserveDeliveryTime.Value);
    //        DBHelper.Instance.ExecuteScalar(du.GetSql());

    //        //Update<PdOrderMaterial>(modelModify);
    //        //_context.SaveChanges();

    //        status = "ok";
    //        message = "修改成功！";
    //    }
    //    catch (Exception E)
    //    {
    //        message = E.Message;
    //    }

    //    obj.status = status;
    //    obj.message = message;
    //    return Ok(obj);
    //}
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
                        FROM PoPdOrderMaterial A
                             JOIN
                             (SELECT *, ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) NUM
                              FROM (SELECT *
                                    FROM (SELECT A.*
                                          FROM PoPdOrderMaterial A
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

    #region 删除重写

    //[HttpGet]
    //public override IActionResult Delete(Guid Id)
    //{
    //    dynamic obj = new ExpandoObject();
    //    string status = "error";
    //    string message = string.Empty;

    //    try
    //    {
    //        _BaseCrud.DoDelete(Id);

    //        status = "ok";
    //        message = "删除成功！";
    //    }
    //    catch (Exception E)
    //    {
    //        message = E.Message;
    //    }

    //    obj.status = status;
    //    obj.message = message;
    //    return Ok(obj);
    //}
    #endregion
}