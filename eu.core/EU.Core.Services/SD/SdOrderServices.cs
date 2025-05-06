/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdOrder.cs
*
*功 能： N / A
* 类 名： SdOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 14:50:05  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Services;

/// <summary>
/// 销售单 (服务)
/// </summary>
public class SdOrderServices : BaseServices<SdOrder, SdOrderDto, InsertSdOrderInput, EditSdOrderInput>, ISdOrderServices
{
    private readonly IBaseRepository<SdOrder> _dal;
    private readonly ISdOrderDetailServices _sdOrderDetailServices;
    public SdOrderServices(IBaseRepository<SdOrder> dal, ISdOrderDetailServices sdOrderDetailServices)
    {
        this._dal = dal;
        base.BaseDal = dal;
        _sdOrderDetailServices = sdOrderDetailServices;
    }

    #region 新增
    public override async Task<Guid> Add(object entity)
    {
        var model = ConvertToEntity(entity);

        var dic = ConvertToDic(entity);
        var lstColumns = dic.Keys.Where(x => x != "ID" && x != "Id").ToList();
        lstColumns.Add("AuditStatus");
        lstColumns.Add("OrderNo");
        lstColumns.Add("SalesOrderStatus");

        #region 检查是否存在相同值
        CheckOnly(model);
        #endregion

        model.OrderNo = Utility.GenerateContinuousSequence("SdOrderNo");
        model.SalesOrderStatus = DIC_SALES_ORDER_STATUS.WaitShip;
        return await BaseDal.Add(model, lstColumns);
    }
    #endregion

    #region 更新
    public override async Task<bool> Update(Guid Id, object entity)
    {
        var model = ConvertToEntity(entity);

        var dic = ConvertToDic(entity);
        var lstColumns = dic.Keys.Where(x => x != "ID" && x != "Id").ToList();

        var order = await QueryDto(Id);
        var result = await Update(model, lstColumns, new List<string> { "OrderNo" } );

        #region 批量更新銷售單明細稅額、含稅交割、未稅金額

        if (order.TaxRate != model.TaxRate || order.TaxType != model.TaxType)
        {
            string sql = string.Empty;

            var details = await Db.Queryable<SdOrderDetail>().Where(x => x.OrderId == Id).ToArrayAsync();
            if (details.Any())
            {
                for (int i = 0; i < details.Length; i++)
                {
                    (decimal? NoTaxAmount, decimal? TaxAmount, decimal? TaxIncludedAmount) = IVChangeHelper.UpdataTaxAmount(model.TaxType, model.TaxRate, details[i].Price, details[i].QTY);
                    details[i].NoTaxAmount = NoTaxAmount;
                    details[i].TaxAmount = TaxAmount;
                    details[i].TaxIncludedAmount = TaxIncludedAmount;
                }
                await Db.Updateable(details).UpdateColumns(it => new { it.NoTaxAmount, it.TaxAmount, it.TaxIncludedAmount }).ExecuteCommandAsync();
            }
        }
        #endregion

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
        var entities = new List<SdOrder>();
        foreach (var id in ids)
        {
            if (!await AnyAsync(id))
                continue;

            var entity = await Query(id);

            if (entity.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit)
                throw new Exception($"订单【{entity.OrderNo}】已审核通过，不可删除！");

            var ent = entity as BasePoco;
            ent.IsDeleted = true;
            entities.Add(entity);

            var details = await Db.Queryable<SdOrderDetail>().Where(x => x.OrderId == id).Select(x => x.ID).ToArrayAsync();
            if (details.Any())
                await _sdOrderDetailServices.Delete(details);
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
    public override async Task<bool> BulkAudit(Guid[] ids) => await BulkAudit(ids, $"SalesOrderStatus = '{DIC_SALES_ORDER_STATUS.WaitShip}'");
    #endregion

    #region 撤销数据 
    /// <summary>
    /// 撤销指定ID集合的数据(批量撤销)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public override async Task<bool> BulkRevocation(Guid[] ids)
    {
        var entities = new List<SdOrder>();
        foreach (var id in ids)
        {
            if (!await AnyAsync(id))
                continue;

            var entity = await Query(id);

            if (entity.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit && entity.SalesOrderStatus == DIC_SALES_ORDER_STATUS.WaitShip)
            {
                entity.AuditStatus = DIC_SYSTEM_AUDIT_STATUS.Add;
                entities.Add(entity);
            }
        }
        await BaseDal.Update(entities, ["AuditStatus"], null, $"SalesOrderStatus = '{DIC_SALES_ORDER_STATUS.WaitShip}'");
        return true;
    }
    #endregion

    #region 批量导入出货通知单
    public async Task<ServiceResult> BulkInsertShipAsync(object entity, string type)
    {
        try
        {
            await Db.Ado.BeginTranAsync();
            string json = entity.ToString();
            var customerIds = new List<Guid>();
            var salesOrderIds = new List<Guid>();
            var dicts = JsonHelper.JsonToObj<List<Dictionary<string, object>>>(json);

            dicts.ForEach(x =>
            {
                if (x.ContainsKey("CustomerId"))
                    customerIds.Add(Guid.Parse(x["CustomerId"].ToString()));

                if (x.ContainsKey("SalesOrderId"))
                    salesOrderIds.Add(Guid.Parse(x["SalesOrderId"].ToString()));
            });
            var dt = Utility.GetSysDate();
            var userId = App.User.ID;
            customerIds = customerIds.Distinct().ToList();
            salesOrderIds = salesOrderIds.Distinct().ToList();

            if (type == "Ship")
                for (int i = 0; i < customerIds.Count; i++)
                {
                    var customerId = customerIds[i];
                    var details = new List<SdShipOrderDetail>();
                    var orderNo = await Utility.GenerateContinuousSequence(Db, "SdShipOrderNo");
                    var order = new SdShipOrder()
                    {
                        ID = Utility.GuidId,
                        CreatedBy = App.User.ID,
                        CreatedTime = dt,
                        OrderNo = orderNo,
                        ShipDate = dt.AddDays(1).Date,
                        OrderDate = dt.Date,
                        CustomerId = customerId,
                        GroupId = Utility.GetGroupGuidId(),
                        CompanyId = Utility.GetCompanyGuidId()
                    };
                    int serialNumber = 1;
                    for (int j = 0; j < dicts.Count; j++)
                    {
                        var x = dicts[j];
                        if (x.ContainsKey("CustomerId") && Guid.Parse(x["CustomerId"].ToString()) == customerId)
                        {
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

                                var result = await Db.Updateable<SdOrderDetail>()
                                  .SetColumns(it => new SdOrderDetail()
                                  {
                                      ShipQTY = sdOrderDetail.ShipQTY
                                  })
                                  .Where(it => it.ID == salesOrderDetailId)
                                  .ExecuteCommandAsync();
                                details.Add(new SdShipOrderDetail()
                                {
                                    SerialNumber = serialNumber,
                                    OrderId = order.ID,
                                    SalesOrderId = salesOrderId,
                                    SalesOrderDetailId = salesOrderDetailId,
                                    MaterialId = Guid.Parse(x["MaterialId"].ToString()),
                                    ShipQTY = shipQTY,
                                    OutQTY = 0,
                                    GroupId = Utility.GetGroupGuidId(),
                                    CompanyId = Utility.GetCompanyGuidId()
                                });
                                serialNumber++;
                            }
                        }
                    }

                    if (details.Any())
                    {
                        await Db.Insertable(order).ExecuteCommandAsync();
                        await Db.Insertable(details).ExecuteCommandAsync();
                    }
                }
            else
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
                        OrderSource = "Sale",
                        OrderNo = orderNo,
                        OutDate = dt.AddDays(1).Date,
                        OrderDate = dt.Date,
                        CustomerId = customerId,
                        DeliveryWayId = customer?.DeliveryWayId,
                        GroupId = Utility.GetGroupGuidId(),
                        CompanyId = Utility.GetCompanyGuidId()
                    });
                    int serialNumber = 1;
                    for (int j = 0; j < dicts.Count; j++)
                    {
                        var x = dicts[j];
                        if (x.ContainsKey("CustomerId") && Guid.Parse(x["CustomerId"].ToString()) == customerId)
                        {
                            var salesOrderId = Guid.Parse(x["SalesOrderId"].ToString());
                            var salesOrderDetailId = Guid.Parse(x["ID"].ToString());
                            decimal? outQTY = Convert.ToDecimal(x["OutQTY"].ToString());

                            var sdOrderDetail = await Db.Queryable<SdOrderDetail>().FirstAsync(x => x.ID == salesOrderDetailId);
                            if (!sdOrderDetail.IsNullOrEmpty())
                            {
                                if (sdOrderDetail.QTY - (sdOrderDetail.OutQTY ?? 0) >= outQTY)
                                    sdOrderDetail.OutQTY = (sdOrderDetail.OutQTY ?? 0) + outQTY;
                                else
                                {
                                    outQTY = sdOrderDetail.QTY - (sdOrderDetail.OutQTY ?? 0);
                                    sdOrderDetail.OutQTY = sdOrderDetail.QTY;
                                }
                                var result = await Db.Updateable<SdOrderDetail>()
                                  .SetColumns(it => new SdOrderDetail()
                                  {
                                      OutQTY = sdOrderDetail.OutQTY,
                                      UpdateBy = userId,
                                      UpdateTime = dt
                                  })
                                  .Where(it => it.ID == salesOrderDetailId)
                                  .ExecuteCommandAsync();
                                details.Add(new SdOutOrderDetail()
                                {
                                    SerialNumber = serialNumber,
                                    OrderSource = "SalesOrder",
                                    OrderId = orderId,
                                    SalesOrderId = salesOrderId,
                                    SalesOrderDetailId = salesOrderDetailId,
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

            for (int i = 0; i < salesOrderIds.Count; i++)
            {
                var salesOrderId = salesOrderIds[i];
                var orderStatus = type == "Ship" ? DIC_SALES_ORDER_STATUS.InShip : DIC_SALES_ORDER_STATUS.InOut;

                var isExist = await Db.Queryable<SdOrderDetail>()
                     .Where(x => x.OrderId == salesOrderId)
                     .WhereIF(type == "Ship", x => ((x.ShipQTY != null && x.QTY - x.ShipQTY > 0) || x.ShipQTY == null))
                     .WhereIF(type != "Ship", x => ((x.OutQTY != null && x.QTY - x.OutQTY > 0) || x.OutQTY == null))
                     .AnyAsync();
                if (!isExist)
                    orderStatus = type == "Ship" ? DIC_SALES_ORDER_STATUS.ShipComplete : DIC_SALES_ORDER_STATUS.OutComplete;

                await Db.Updateable<SdOrder>()
                    .SetColumns(it => new SdOrder()
                    {
                        SalesOrderStatus = orderStatus,
                        UpdateBy = userId,
                        UpdateTime = dt
                    })
                    .Where(it => it.ID == salesOrderId && it.SalesOrderStatus != orderStatus)
                    .ExecuteCommandAsync();
            }
            await Db.Ado.CommitTranAsync();

            return Success(ResponseText.EXECUTE_SUCCESS);

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
        await Db.Updateable<SdOrder>()
            .SetColumns(it => new SdOrder()
            {
                SalesOrderStatus = DIC_SALES_ORDER_STATUS.OrderComplete
            }, true)
            .Where(it =>
            ids.Contains(it.ID) &&
            it.SalesOrderStatus != DIC_SALES_ORDER_STATUS.WaitShip &&
            it.SalesOrderStatus != DIC_SALES_ORDER_STATUS.OrderComplete &&
            it.SalesOrderStatus != DIC_SALES_ORDER_STATUS.OutComplete &&
            it.SalesOrderStatus != DIC_SALES_ORDER_STATUS.ShipComplete &&
            it.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit)
            .ExecuteCommandAsync();
        return Success(ResponseText.EXECUTE_SUCCESS);
    }
    #endregion

    #region 订单变更
    /// <summary>
    /// 订订单变更指定ID集合的数据(订单完结)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public async Task<ServiceResult> BulkOrderChange(List<Guid> ids)
    {
        #region 校验是否已产生变更单
        var changeOrderIds = await Db.Queryable<SdChangeOrder>()
            .Where(x =>
            x.OrderId != null &&
            ids.Contains(x.OrderId.Value) &&
            x.OrderStatus == DIC_SALES_CHANGE_ORDER_STATUS.Invalid)
            .Select(x => x.OrderId.Value).ToListAsync();
        if (changeOrderIds.Any())
            ids = ids.Where(x => !changeOrderIds.Contains(x)).ToList();
        #endregion

        if (!ids.Any())
            return Success(ResponseText.EXECUTE_SUCCESS);

        var orders = await Db.Queryable<SdOrder>().Where(x =>
                              ids.Contains(x.ID) &&
                              (x.SalesOrderStatus == DIC_SALES_ORDER_STATUS.InOut || x.SalesOrderStatus == DIC_SALES_ORDER_STATUS.InShip) &&
                              x.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit).ToListAsync();

        ids = orders.Select(x => x.ID).ToList();
        if (!ids.Any())
            return Success(ResponseText.EXECUTE_SUCCESS);

        try
        {
            await Db.Ado.BeginTranAsync();

            var orderDetails = await Db.Queryable<SdOrderDetail>().Where(x => x.OrderId != null && ids.Contains(x.OrderId.Value)).ToListAsync();

            var changeOrders = new List<SdChangeOrder>();
            var changeOrderDetails = new List<SdChangeOrderDetail>();

            orders.ForEach(order =>
            {
                var changeOrder = Mapper.Map(order).ToANew<SdChangeOrder>();
                var changeOrderDetails1 = orderDetails
                .Where(o => o.OrderId == order.ID)
                .Select(o => new SdChangeOrderDetail
                {
                    CreatedBy = o.CreatedBy,
                    CreatedTime = o.CreatedTime,
                    OrderDetailId = o.ID,
                    OrderId = changeOrder.ID,
                    SerialNumber = o.SerialNumber,
                    MaterialId = o.MaterialId,
                    CustomerMaterialCode = o.CustomerMaterialCode,
                    QTY = o.QTY,
                    Price = o.Price,
                    NoTaxAmount = o.NoTaxAmount,
                    TaxAmount = o.TaxAmount,
                    TaxIncludedAmount = o.TaxIncludedAmount,
                    DeliveryDate = o.DeliveryDate,
                    ShipQTY = o.ShipQTY,
                    OutQTY = o.OutQTY,
                    ReturnQTY = o.ReturnQTY,
                    ProductionQTY = o.ProductionQTY,
                    IsDeleted = o.IsDeleted,
                    IsActive = o.IsActive,
                    Remark = o.Remark
                })
                .ToList();
                changeOrderDetails.AddRange(changeOrderDetails1);
                changeOrder.OrderId = order.ID;
                changeOrder.AuditStatus = DIC_SYSTEM_AUDIT_STATUS.Add;
                changeOrder.OrderStatus = DIC_SALES_CHANGE_ORDER_STATUS.Invalid;
                changeOrder.CreatedBy = UserId;
                changeOrders.Add(changeOrder);
            });

            await Db.Insertable(changeOrders).ExecuteCommandAsync();
            await Db.Insertable(changeOrderDetails).ExecuteCommandAsync();

            await Db.Ado.CommitTranAsync();
            return Success(ResponseText.EXECUTE_SUCCESS);
        }
        catch (Exception E)
        {
            await Db.Ado.RollbackTranAsync();
            return Failed(E.Message);
        }
    }
    #endregion
}