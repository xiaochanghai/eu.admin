/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdShipOrder.cs
*
*功 能： N / A
* 类 名： SdShipOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 14:50:13  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Services;

/// <summary>
/// 发货单 (服务)
/// </summary>
public class SdShipOrderServices : BaseServices<SdShipOrder, SdShipOrderDto, InsertSdShipOrderInput, EditSdShipOrderInput>, ISdShipOrderServices
{
    private readonly IBaseRepository<SdShipOrder> _dal;
    private readonly ISdShipOrderDetailServices _sdShipOrderDetailServices;
    public SdShipOrderServices(IBaseRepository<SdShipOrder> dal,
        ISdShipOrderDetailServices sdShipOrderDetailServices)
    {
        this._dal = dal;
        base.BaseDal = dal;
        _sdShipOrderDetailServices = sdShipOrderDetailServices;
    }

    #region 删除数据 
    /// <summary>
    /// 删除指定ID集合的数据(批量删除)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public override async Task<bool> Delete(Guid[] ids)
    {
        var entities = new List<SdShipOrder>();
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

            var details = await Db.Queryable<SdShipOrderDetail>().Where(x => x.OrderId == id).Select(x => x.ID).ToArrayAsync();
            if (details.Any())
                await _sdShipOrderDetailServices.Delete(details);
        }
        return await BaseDal.Update(entities, ["IsDeleted"]);
    }
    #endregion

    #region 审核数据

    /// <summary>
    /// 审核指定ID集合的数据(批量审核)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public override async Task<bool> BulkAudit(Guid[] ids) => await BulkAudit(ids, $"OrderStatus = '{DIC_SALES_SHIP_ORDER_STATUS.WaitShip}'");
    #endregion

    #region 撤销数据
    /// <summary>
    /// 撤销指定ID集合的数据(批量撤销)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public override async Task<bool> BulkRevocation(Guid[] ids)
    {
        var entities = new List<SdShipOrder>();
        foreach (var id in ids)
        {
            if (!await AnyAsync(id))
                continue;

            var entity = await Query(id);

            if (entity.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit && entity.OrderStatus == DIC_SALES_SHIP_ORDER_STATUS.WaitShip)
            {
                entity.AuditStatus = DIC_SYSTEM_AUDIT_STATUS.Add;
                entities.Add(entity);
            }
        }
        await BaseDal.Update(entities, ["AuditStatus"], null, $"OrderStatus = '{DIC_SALES_SHIP_ORDER_STATUS.WaitShip}'");
        return true;
    }
    #endregion

    #region 批量导入出货明细
    public async Task<ServiceResult> BulkInsertShipAsync(object entity, Guid id)
    {
        try
        {
            await Db.Ado.BeginTranAsync();
            string json = entity.ToString();
            var salesOrderIds = new List<Guid>();
            var dicts = JsonHelper.JsonToObj<List<Dictionary<string, object>>>(json);

            dicts.ForEach(x =>
            {
                if (x.ContainsKey("SalesOrderId"))
                    salesOrderIds.Add(Guid.Parse(x["SalesOrderId"].ToString()));
            });

            var dt = Utility.GetSysDate();
            var userId = App.User.ID;
            salesOrderIds = salesOrderIds.Distinct().ToList();

            var addDetails = new List<SdShipOrderDetail>();
            var updateDetails = new List<SdShipOrderDetail>();

            for (int j = 0; j < dicts.Count; j++)
            {
                var x = dicts[j];

                var salesOrderId = Guid.Parse(x["SalesOrderId"].ToString());
                var salesOrderDetailId = Guid.Parse(x["ID"].ToString());
                decimal? shipQTY = Convert.ToDecimal(x["ShipQTY"].ToString());

                var sdOrderDetail = await Db.Queryable<SdOrderDetail>().FirstAsync(x => x.ID == salesOrderDetailId);
                if (!sdOrderDetail.IsNullOrEmpty())
                {
                    if (sdOrderDetail.QTY - (sdOrderDetail.ShipQTY ?? 0) >= shipQTY)
                        sdOrderDetail.ShipQTY = (sdOrderDetail.ShipQTY ?? 0) + shipQTY;
                    else
                    {
                        shipQTY = sdOrderDetail.QTY - (sdOrderDetail.ShipQTY ?? 0);
                        sdOrderDetail.ShipQTY = sdOrderDetail.QTY;
                    }

                    await Db.Updateable<SdOrderDetail>()
                        .SetColumns(it => new SdOrderDetail() { ShipQTY = sdOrderDetail.ShipQTY }, true)
                        .Where(it => it.ID == salesOrderDetailId)
                        .ExecuteCommandAsync();

                    var shipOrderDetail = await Db.Queryable<SdShipOrderDetail>().FirstAsync(x => x.OrderId == id && x.SalesOrderDetailId == salesOrderDetailId);
                    if (shipOrderDetail.IsNullOrEmpty())
                        addDetails.Add(new SdShipOrderDetail()
                        {
                            SerialNumber = 1,
                            OrderId = id,
                            SalesOrderId = salesOrderId,
                            SalesOrderDetailId = salesOrderDetailId,
                            MaterialId = Guid.Parse(x["MaterialId"].ToString()),
                            ShipQTY = shipQTY,
                            OutQTY = 0,
                            GroupId = Utility.GetGroupGuidId(),
                            CompanyId = Utility.GetCompanyGuidId()
                        });
                    else
                    {
                        shipOrderDetail.ShipQTY += shipQTY;
                        updateDetails.Add(shipOrderDetail);
                    }
                }
            }

            if (addDetails.Any())
                await Db.Insertable(addDetails).ExecuteCommandAsync();

            if (updateDetails.Any())
                await Db.Updateable(updateDetails).UpdateColumns(it => new { it.ShipQTY }, true).ExecuteCommandAsync();

            for (int i = 0; i < salesOrderIds.Count; i++)
                await _sdShipOrderDetailServices.UpdateSalesOrderStatus(salesOrderIds[i]);

            await IVChangeHelper.UpdataOrderDetailSerialNumber(Db, "SdShipOrderDetail", id);
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

    #region 订单完结
    /// <summary>
    /// 订单完结指定ID集合的数据(订单完结)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public async Task<ServiceResult> BulkOrderComplete(Guid[] ids)
    {
        await Db.Updateable<SdShipOrder>()
            .SetColumns(it => new SdShipOrder() { OrderStatus = DIC_SALES_ORDER_STATUS.OrderComplete })
            .Where(it =>
            ids.Contains(it.ID) &&
            it.OrderStatus != DIC_SALES_ORDER_STATUS.WaitShip &&
            it.OrderStatus != DIC_SALES_ORDER_STATUS.OrderComplete &&
            it.OrderStatus != DIC_SALES_ORDER_STATUS.OutComplete &&
            it.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit)
            .ExecuteCommandAsync();
        return Success(ResponseText.EXECUTE_SUCCESS);
    }
    #endregion

    #region 批量导入出库单
    public async Task<ServiceResult> BulkInsertOutAsync(object entity)
    {
        try
        {
            await Db.Ado.BeginTranAsync();
            string json = entity.ToString();
            var customerIds = new List<Guid>();
            var shipOrderIds = new List<Guid>();
            var dicts = JsonHelper.JsonToObj<List<Dictionary<string, object>>>(json);

            dicts.ForEach(x =>
            {
                if (x.ContainsKey("CustomerId"))
                    customerIds.Add(Guid.Parse(x["CustomerId"].ToString()));

                if (x.ContainsKey("SalesOrderId"))
                    shipOrderIds.Add(Guid.Parse(x["SalesOrderId"].ToString()));
            });
            var dt = Utility.GetSysDate();
            var userId = App.User.ID;
            customerIds = customerIds.Distinct().ToList();
            shipOrderIds = shipOrderIds.Distinct().ToList();

            for (int i = 0; i < customerIds.Count; i++)
            {
                var customerId = customerIds[i];
                var orders = new List<SdOutOrder>();
                var details = new List<SdOutOrderDetail>();
                var orderId = Utility.GuidId;
                var orderNo = await Utility.GenerateContinuousSequence(Db, "SdOutOrderNo");
                var customer = await Db.Queryable<BdCustomer>().FirstAsync(x => x.ID == customerId);
                orders.Add(new SdOutOrder()
                {
                    ID = orderId,
                    CreatedBy = App.User.ID,
                    CreatedTime = dt,
                    OrderNo = orderNo,
                    OrderSource = "Ship",
                    OutDate = dt.AddDays(1).Date,
                    OrderDate = dt.Date,
                    CustomerId = customerId,
                    DeliveryWayId = customer?.DeliveryWayId,
                    GroupId = Utility.GetGroupGuidId(),
                    CompanyId = Utility.GetCompanyGuidId(),
                    OrderStatus = DIC_SALES_OUT_ORDER_STATUS.WaitOut
                });
                int serialNumber = 1;
                for (int j = 0; j < dicts.Count; j++)
                {
                    var x = dicts[j];
                    if (x.ContainsKey("CustomerId") && Guid.Parse(x["CustomerId"].ToString()) == customerId)
                    {
                        var shipOrderId = Guid.Parse(x["SalesOrderId"].ToString());
                        var shipOrderDetailId = Guid.Parse(x["ID"].ToString());
                        decimal? outQTY = Convert.ToDecimal(x["OutQTY"].ToString());

                        var sdShipDetail = await Db.Queryable<SdShipOrderDetail>().FirstAsync(x => x.ID == shipOrderDetailId);
                        if (!sdShipDetail.IsNullOrEmpty())
                        {
                            if (sdShipDetail.ShipQTY - (sdShipDetail.OutQTY ?? 0) >= outQTY)
                                sdShipDetail.OutQTY = (sdShipDetail.OutQTY ?? 0) + outQTY;
                            else
                            {
                                outQTY = sdShipDetail.ShipQTY - (sdShipDetail.OutQTY ?? 0);
                                sdShipDetail.OutQTY = sdShipDetail.ShipQTY;
                            }

                            var result = await Db.Updateable<SdShipOrderDetail>()
                              .SetColumns(it => new SdShipOrderDetail()
                              {
                                  OutQTY = sdShipDetail.OutQTY
                              }, true)
                              .Where(it => it.ID == shipOrderDetailId)
                              .ExecuteCommandAsync();
                            details.Add(new SdOutOrderDetail()
                            {
                                SerialNumber = serialNumber,
                                OrderSource = "ShipOrder",
                                OrderId = orderId,
                                ShipOrderId = shipOrderId,
                                ShipOrderDetailId = shipOrderDetailId,
                                SalesOrderDetailId = sdShipDetail.SalesOrderDetailId,
                                SalesOrderId = sdShipDetail.SalesOrderId,
                                MaterialId = Guid.Parse(x["MaterialId"].ToString()),
                                OutQTY = outQTY,
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

            for (int i = 0; i < shipOrderIds.Count; i++)
            {
                var shipOrderId = shipOrderIds[i];
                var shipOrderStatus = DIC_SALES_ORDER_STATUS.InOut;

                var isExist = await Db.Queryable<SdShipOrderDetail>()
                     .Where(x => x.OrderId == shipOrderId)
                     .WhereIF(1 == 1, x => ((x.OutQTY != null && x.ShipQTY - x.OutQTY > 0) || x.OutQTY == null))
                     .AnyAsync();
                if (!isExist)
                    shipOrderStatus = DIC_SALES_ORDER_STATUS.OutComplete;

                await Db.Updateable<SdShipOrder>()
                    .SetColumns(it => new SdShipOrder()
                    {
                        OrderStatus = shipOrderStatus
                    }, true)
                    .Where(it => it.ID == shipOrderId && it.OrderStatus != shipOrderStatus)
                    .ExecuteCommandAsync();
            }
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
}