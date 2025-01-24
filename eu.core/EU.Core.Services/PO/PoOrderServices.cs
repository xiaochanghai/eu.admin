/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoOrder.cs
*
*功 能： N / A
* 类 名： PoOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/9/9 15:44:53  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 采购单 (服务)
/// </summary>
public class PoOrderServices : BaseServices<PoOrder, PoOrderDto, InsertPoOrderInput, EditPoOrderInput>, IPoOrderServices
{
    private readonly IBaseRepository<PoOrder> _dal;
    private IPoOrderDetailServices _poOrderDetailServices;
    public PoOrderServices(IBaseRepository<PoOrder> dal, IPoOrderDetailServices poOrderDetailServices)
    {
        this._dal = dal;
        base.BaseDal = dal;
        _poOrderDetailServices = poOrderDetailServices;
    }

    #region 更新
    public override async Task<bool> Update(Guid Id, object entity)
    {
        var model = JsonHelper.JsonToObj<PoOrder>(entity.ToString());
        var entity1 = await base.QueryDto(Id);
        var result = await base.Update(Id, entity);

        if (entity1.TaxRate != model.TaxRate || entity1.TaxType != model.TaxType)
        {
            var details = await Db.Queryable<PoOrderDetail>().Where(x => x.OrderId == Id).ToArrayAsync();
            if (details.Any())
            {
                for (int i = 0; i < details.Length; i++)
                {
                    (decimal? NoTaxAmount, decimal? TaxAmount, decimal? TaxIncludedAmount) = IVChangeHelper.UpdataTaxAmount(model.TaxType, model.TaxRate, details[i].Price, details[i].QTY);
                    details[i].NoTaxAmount = NoTaxAmount;
                    details[i].TaxAmount = TaxAmount;
                    details[i].TaxIncludedAmount = TaxIncludedAmount;
                }
                await Db.Updateable(details).UpdateColumns(it => new { it.NoTaxAmount, it.TaxAmount, it.TaxIncludedAmount }, true).ExecuteCommandAsync();
            }
        }

        return result;
    }
    #endregion 

    #region 删除数据 
    /// <summary>
    /// 删除指定ID集合的数据(批量删除)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public override async Task<bool> Delete(Guid[] ids)
    {
        var entities = new List<PoOrder>();
        for (int i = 0; i < ids.Length; i++)

        {
            var id = ids[i];
            if (!await AnyAsync(id))
                continue;

            var entity = await Query(id);

            if (entity.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit)
                throw new Exception($"订单【{entity.OrderNo}】已审核通过，不可删除！");

            entity.IsDeleted = true;
            entities.Add(entity);

            var detailIds = await Db.Queryable<PoOrderDetail>().Where(x => x.OrderId == id).Select(x => x.ID).ToArrayAsync();
            await _poOrderDetailServices.Delete(detailIds);
        }
        var result = await BaseDal.Update(entities, ["IsDeleted"]);

        return result;
    }
    #endregion

    #region 审核数据
    /// <summary>
    /// 审核指定ID集合的数据(批量审核)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public override async Task<bool> BulkAudit(Guid[] ids) => await BulkAudit(ids, $"OrderStatus = '{DIC_PURCHASE_ORDER_STATUS.Wait}'");
    #endregion

    #region 撤销数据 
    /// <summary>
    /// 撤销指定ID集合的数据(批量撤销)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public override async Task<bool> BulkRevocation(Guid[] ids)
    {
        var entities = new List<PoOrder>();
        foreach (var id in ids)
        {
            if (!BaseDal.Any(id))
                continue;

            var entity = await BaseDal.QueryById(id);

            if (entity.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit && entity.OrderStatus == DIC_PURCHASE_ORDER_STATUS.Wait)
            {
                entity.AuditStatus = DIC_SYSTEM_AUDIT_STATUS.Add;
                entities.Add(entity);
            }
        }
        await BaseDal.Update(entities, ["AuditStatus"], null, $"OrderStatus = '{DIC_PURCHASE_ORDER_STATUS.Wait}'");
        return true;
    }
    #endregion

    #region 批量导入出货通知/入库单
    public async Task<ServiceResult> BulkInsertNoticeOrInAsync(object entity, string type)
    {
        try
        {
            await Db.Ado.BeginTranAsync();
            string json = entity.ToString();
            var supplierIds = new List<Guid>();
            var poOrderIds = new List<Guid>();
            var dicts = JsonHelper.JsonToObj<List<Dictionary<string, object>>>(json);

            dicts.ForEach(x =>
            {
                if (x.ContainsKey("SupplierId"))
                    supplierIds.Add(Guid.Parse(x["SupplierId"].ToString()));

                if (x.ContainsKey("PoOrderId"))
                    poOrderIds.Add(Guid.Parse(x["PoOrderId"].ToString()));
            });
            var dt = Utility.GetSysDate();
            var userId = App.User.ID;
            supplierIds = supplierIds.Distinct().ToList();
            poOrderIds = poOrderIds.Distinct().ToList();

            if (type == "Notice")
                for (int i = 0; i < supplierIds.Count; i++)
                {
                    var supplierId = supplierIds[i];
                    var details = new List<PoArrivalOrderDetail>();
                    var orderNo = await Utility.GenerateContinuousSequence(Db, "PdOrderNo");
                    var order = await Db.Queryable<PoArrivalOrder>().FirstAsync(x => x.SupplierId == supplierId && x.OrderStatus == DIC_PURCHASE_ORDER_STATUS.Wait);
                    if (order.IsNullOrEmpty())
                        order = new PoArrivalOrder()
                        {
                            ID =  Utility.GuidId,
                            CreatedBy = App.User.ID,
                            CreatedTime = dt,
                            OrderNo = orderNo,
                            ArrivalTime = dt.AddDays(1).Date,
                            OrderDate = dt.Date,
                            SupplierId = supplierId,
                            GroupId = Utility.GetGroupGuidId(),
                            CompanyId = Utility.GetCompanyGuidId()
                        };
                    for (int j = 0; j < dicts.Count; j++)
                    {
                        var x = dicts[j];
                        if (x.ContainsKey("SupplierId") && Guid.Parse(x["SupplierId"].ToString()) == supplierId)
                        {
                            var poOrderId = Guid.Parse(x["PoOrderId"].ToString());
                            var sourceOrderDetailId = Guid.Parse(x["ID"].ToString());
                            decimal? noticeQTY = Convert.ToDecimal(x["NoticeQTY"].ToString());

                            var poOrderDetail = await Db.Queryable<PoOrderDetail>().FirstAsync(x => x.ID == sourceOrderDetailId);
                            if (!poOrderDetail.IsNullOrEmpty())
                            {
                                if (poOrderDetail.QTY - (poOrderDetail.NoticeQTY ?? 0) >= noticeQTY)
                                    poOrderDetail.NoticeQTY = (poOrderDetail.NoticeQTY ?? 0) + noticeQTY;
                                else
                                {
                                    noticeQTY = poOrderDetail.QTY - (poOrderDetail.NoticeQTY ?? 0);
                                    poOrderDetail.NoticeQTY = poOrderDetail.QTY;
                                }

                                var result = await Db.Updateable<PoOrderDetail>()
                                  .SetColumns(it => new PoOrderDetail()
                                  {
                                      NoticeQTY = poOrderDetail.NoticeQTY
                                  }, true)
                                  .Where(it => it.ID == sourceOrderDetailId)
                                  .ExecuteCommandAsync();

                                //var detail = await Db.Queryable<PoArrivalOrderDetail>().FirstAsync(x => x.OrderId == order.ID && x.SourceOrderDetailId == sourceOrderDetailId);
                                //if (detail.IsNullOrEmpty())
                                details.Add(new PoArrivalOrderDetail()
                                {
                                    OrderSource = "PurchaseOrder",
                                    OrderId = order.ID,
                                    SourceOrderId = poOrderId,
                                    SourceOrderDetailId = sourceOrderDetailId,
                                    MaterialId = Guid.Parse(x["MaterialId"].ToString()),
                                    NoticeQTY = noticeQTY,
                                    DeliveryDate = dt.AddDays(1).Date,
                                    CheckQTY = 0,
                                    InQTY = 0,
                                    GroupId = Utility.GetGroupGuidId(),
                                    CompanyId = Utility.GetCompanyGuidId()
                                });
                                //else
                                //{
                                //    detail.NoticeQTY += noticeQTY;
                                //    updates.Add(detail);
                                //}
                            }
                        }
                    }

                    if (order.OrderStatus.IsNullOrEmpty())
                        await Db.Insertable(order).ExecuteCommandAsync();

                    if (details.Any())
                        await Db.Insertable(details).ExecuteCommandAsync();

                    //if (updates.Any())
                    //    await Db.Updateable(updates).UpdateColumns(it => new
                    //    {
                    //        it.NoticeQTY,
                    //        it.UpdateBy,
                    //        it.UpdateTime,
                    //    }).ExecuteCommandAsync();
                }
            else
                for (int i = 0; i < supplierIds.Count; i++)
                {
                    var supplierId = supplierIds[i];
                    var orders = new List<PoInOrder>();
                    var details = new List<PoInOrderDetail>();
                    var orderId = Utility.GuidId;
                    var orderNo = await Utility.GenerateContinuousSequence(Db, "PoInOrderNo");
                    orders.Add(new PoInOrder()
                    {
                        ID = orderId,
                        CreatedBy = App.User.ID,
                        CreatedTime = dt,
                        UserId = userId,
                        SupplierId = supplierId,
                        OrderNo = orderNo,
                        OrderDate = dt.Date,
                        OrderSource = "PurchaseOrder",
                        GroupId = Utility.GetGroupGuidId(),
                        CompanyId = Utility.GetCompanyGuidId()
                    });
                    int serialNumber = 1;
                    for (int j = 0; j < dicts.Count; j++)
                    {
                        var x = dicts[j];
                        if (x.ContainsKey("SupplierId") && Guid.Parse(x["SupplierId"].ToString()) == supplierId)
                        {
                            var poOrderId = Guid.Parse(x["PoOrderId"].ToString());
                            var sourceOrderDetailId = Guid.Parse(x["ID"].ToString());
                            decimal? inQTY = Convert.ToDecimal(x["InQTY"].ToString());

                            var poOrderDetail = await Db.Queryable<PoOrderDetail>().FirstAsync(x => x.ID == sourceOrderDetailId);
                            if (!poOrderDetail.IsNullOrEmpty())
                            {
                                if (poOrderDetail.QTY - (poOrderDetail.InQTY ?? 0) >= inQTY)
                                    poOrderDetail.InQTY = (poOrderDetail.InQTY ?? 0) + inQTY;
                                else
                                {
                                    inQTY = poOrderDetail.QTY - (poOrderDetail.InQTY ?? 0);
                                    poOrderDetail.InQTY = poOrderDetail.QTY;
                                }
                                var result = await Db.Updateable<PoOrderDetail>()
                                  .SetColumns(it => new PoOrderDetail()
                                  {
                                      InQTY = poOrderDetail.InQTY
                                  }, true)
                                  .Where(it => it.ID == sourceOrderDetailId)
                                  .ExecuteCommandAsync();
                                details.Add(new PoInOrderDetail()
                                {
                                    SerialNumber = serialNumber,
                                    OrderSource = "PurchaseOrder",
                                    OrderId = orderId,
                                    SourceOrderId = poOrderId,
                                    SourceOrderDetailId = sourceOrderDetailId,
                                    MaterialId = Guid.Parse(x["MaterialId"].ToString()),
                                    InQTY = inQTY,
                                    ReturnQTY = 0,
                                    GroupId = Utility.GetGroupGuidId(),
                                    CompanyId = Utility.GetCompanyGuidId()
                                });
                                serialNumber++;
                            }
                        }
                    }

                    if (details.Any() && orders.Any())
                    {
                        await Db.Insertable(orders).ExecuteCommandAsync();
                        await Db.Insertable(details).ExecuteCommandAsync();
                    }
                }

            for (int i = 0; i < poOrderIds.Count; i++)
                await UpdatePoOrderStatus(poOrderIds[i], type);

            await Db.Ado.CommitTranAsync();

            return Success(ResponseText.SAVE_SUCCESS);

        }
        catch (Exception E)
        {
            await Db.Ado.RollbackTranAsync();

            return Failed(E.Message);
        }
    }
    #endregion

    #region 更新订单状态
    /// <summary>
    /// 更新订单状态
    /// </summary>
    /// <param name="orderId">采购单ID</param>
    /// <param name="type">type</param>
    /// <returns></returns>
    public async Task UpdatePoOrderStatus(Guid? orderId, string type)
    {
        var dt = Utility.GetSysDate();
        var userId = App.User.ID;
        var orderStatus = type == "Notice" ? DIC_PURCHASE_ORDER_STATUS.InNotice : DIC_PURCHASE_ORDER_STATUS.In;

        var isExist = await Db.Queryable<PoOrderDetail>()
             .Where(x => x.OrderId == orderId)
             .WhereIF(type == "Notice", x => ((x.NoticeQTY != null && x.QTY - x.NoticeQTY > 0) || x.NoticeQTY == null))
             .WhereIF(type != "Notice", x => ((x.InQTY != null && x.QTY - x.InQTY > 0) || x.InQTY == null))
             .AnyAsync();
        if (!isExist)
            orderStatus = type == "Notice" ? DIC_PURCHASE_ORDER_STATUS.NoticeComplete : DIC_PURCHASE_ORDER_STATUS.InComplete;

        await Db.Updateable<PoOrder>()
            .SetColumns(it => new PoOrder()
            {
                OrderStatus = orderStatus
            }, true)
            .Where(it => it.ID == orderId && it.OrderStatus != orderStatus)
            .ExecuteCommandAsync();
    }
    #endregion
}