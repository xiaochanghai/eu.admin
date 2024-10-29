/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdOutOrderDetail.cs
*
*功 能： N / A
* 类 名： SdOutOrderDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/8/14 15:23:48  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Services;

/// <summary>
/// 出库单明细 (服务)
/// </summary>
public class SdOutOrderDetailServices : BaseServices<SdOutOrderDetail, SdOutOrderDetailDto, InsertSdOutOrderDetailInput, EditSdOutOrderDetailInput>, ISdOutOrderDetailServices
{
    private readonly IBaseRepository<SdOutOrderDetail> _dal;
    public SdOutOrderDetailServices(IBaseRepository<SdOutOrderDetail> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }
    #region 更新
    public override async Task<SdOutOrderDetailDto> UpdateReturn(Guid Id, object entity1)
    {
        try
        {
            await Db.Ado.BeginTranAsync(); 
            var model = ConvertToEntity(entity1);

            #region 检查是否存在相同值
            CheckOnly(model, Id);
            #endregion

            var entity = await QueryDto(Id);
            var model1 = new SdOutOrderDetailDto();
            if (entity.ShipOrderDetailId.IsNullOrEmpty())
            {
                var orderDetail = await Db.Queryable<SdOrderDetail>().FirstAsync(x => x.ID == entity.SalesOrderDetailId);

                var qty = orderDetail.QTY - (orderDetail.ShipQTY ?? 0);

                if (model.OutQTY > (qty + entity.OutQTY))
                    throw new Exception($"出库数量最大为：{Utility.RemoveZero(qty + entity.OutQTY)}");

                orderDetail.OutQTY = orderDetail.OutQTY - entity.OutQTY + model.OutQTY;

                await Db.Updateable<SdOrderDetail>()
                    .SetColumns(it => new SdOrderDetail()
                    {
                        OutQTY = orderDetail.OutQTY,
                        UpdateBy = UserId,
                        UpdateTime = Utility.GetSysDate()
                    })
                    .Where(it => it.ID == entity.SalesOrderDetailId)
                    .ExecuteCommandAsync();

                await UpdateSalesOrderStatus(entity.SalesOrderId);
            }
            else
            {
                var orderDetail = await Db.Queryable<SdShipOrderDetail>().FirstAsync(x => x.ID == entity.ShipOrderDetailId.Value);

                var qty = orderDetail.ShipQTY - (orderDetail.OutQTY ?? 0);

                if (model.OutQTY > (qty + entity.OutQTY))
                    throw new Exception($"出货通知数量最大为：{Utility.RemoveZero(qty + entity.OutQTY)}");

                orderDetail.OutQTY = orderDetail.OutQTY - entity.OutQTY + model.OutQTY;
                await Db.Updateable<SdShipOrderDetail>()
                    .SetColumns(it => new SdShipOrderDetail()
                    {
                        OutQTY = orderDetail.OutQTY,
                        UpdateBy = UserId,
                        UpdateTime = Utility.GetSysDate()
                    })
                    .Where(it => it.ID == entity.ShipOrderDetailId)
                    .ExecuteCommandAsync();
                await UpdateShipOrderStatus(entity.ShipOrderId);
            }

            var lstColumns = new ModuleSqlColumn("SD_OUT_ORDER_DETAIL_MNG").GetModuleTableEditableColumns();

            lstColumns.Add("UpdateBy");
            lstColumns.Add("UpdateTime");
            await Update(model, lstColumns, null, $"ID='{Id}'");

            model1 = Mapper.Map(model).ToANew<SdOutOrderDetailDto>();

            await Db.Ado.CommitTranAsync();

            return model1;
        }
        catch (Exception)
        {
            await Db.Ado.RollbackTranAsync();

            throw;
        }
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
        try
        {
            await Db.Ado.BeginTranAsync();

            Guid? outOrderId = null;
            var shipOrderIds = new List<Guid?>();
            var salesOrderIds = new List<Guid?>();
            var entities = new List<SdOutOrderDetail>();
            foreach (var id in ids)
            {
                if (!await AnyAsync(id))
                    continue;

                var entity = await Query(id);
                if (entity.IsDeleted)
                    continue;
                outOrderId = entity.OrderId;

                if (await Db.Queryable<SdOutOrder>().AnyAsync(x => x.AuditStatus != DIC_SYSTEM_AUDIT_STATUS.Add && x.ID == entity.OrderId))
                    throw new Exception($"该订单已审核通过，不可删除！");

                if (entity.ShipOrderId.IsNullOrEmpty())
                {
                    salesOrderIds.Add(entity.SalesOrderId);

                    #region 销售单出货数量回退  
                    await Db.Updateable<SdOrderDetail>()
                        .SetColumns(x => new SdOrderDetail()
                        {
                            OutQTY = x.OutQTY - entity.OutQTY,
                            UpdateBy = UserId,
                            UpdateTime = Utility.GetSysDate()
                        })
                        .Where(it => it.ID == entity.SalesOrderDetailId)
                        .ExecuteCommandAsync();
                    #endregion
                }
                else
                {
                    shipOrderIds.Add(entity.ShipOrderId);

                    #region 出货通知出货数量回退
                    await Db.Updateable<SdShipOrderDetail>()
                        .SetColumns(it => new SdShipOrderDetail()
                        {
                            OutQTY = it.OutQTY - entity.OutQTY,
                            UpdateBy = UserId,
                            UpdateTime = Utility.GetSysDate()
                        })
                        .Where(it => it.ID == entity.ShipOrderDetailId)
                        .ExecuteCommandAsync();
                    #endregion 
                }
                entity.IsDeleted = true;
                entities.Add(entity);
            }
            await BaseDal.Update(entities, ["IsDeleted"]);

            #region 变更单据状态
            salesOrderIds = salesOrderIds.Distinct().ToList();
            if (salesOrderIds.Any())
            {
                for (int i = 0; i < salesOrderIds.Count; i++)
                    await UpdateSalesOrderStatus(salesOrderIds[i]);
            }

            shipOrderIds = shipOrderIds.Distinct().ToList();
            if (shipOrderIds.Any())
            {
                for (int i = 0; i < shipOrderIds.Count; i++)
                    await UpdateShipOrderStatus(shipOrderIds[i]);
            }
            #endregion

            #region 批量更新排序号
            if (!outOrderId.IsNullOrEmpty())
                await IVChangeHelper.UpdataOrderDetailSerialNumber(Db, "SdOutOrderDetail", outOrderId);
            #endregion

            await Db.Ado.CommitTranAsync();

            return true;
        }
        catch (Exception)
        {
            await Db.Ado.RollbackTranAsync();

            throw;
        }
    }
    #endregion

    #region 变更销售单状态
    /// <summary>
    /// 变更销售单状态
    /// </summary>
    /// <param name="orderId">订单ID</param> 
    /// <returns></returns>
    public async Task UpdateSalesOrderStatus(Guid? orderId)
    {
        var orderStatus = DIC_SALES_ORDER_STATUS.WaitShip;

        var isExist = await Db.Queryable<SdOrderDetail>()
            .Where(x => x.OrderId == orderId)
           .Where(x => x.OutQTY != null && x.OutQTY > 0)
            .AnyAsync();

        if (isExist)
            orderStatus = DIC_SALES_ORDER_STATUS.InOut;

        isExist = await Db.Queryable<SdOrderDetail>()
           .Where(x => x.OrderId == orderId)
           .Where(x => ((x.OutQTY != null && x.QTY - x.OutQTY > 0) || x.OutQTY == null))
           .AnyAsync();
        if (!isExist)
            orderStatus = DIC_SALES_ORDER_STATUS.OutComplete;

        await Db.Updateable<SdOrder>()
            .SetColumns(it => new SdOrder()
            {
                SalesOrderStatus = orderStatus,
                UpdateBy = UserId,
                UpdateTime = Utility.GetSysDate()
            })
            .Where(it => it.ID == orderId && it.SalesOrderStatus != orderStatus)
            .ExecuteCommandAsync();
    }
    #endregion

    #region 变更出货通知单状态
    /// <summary>
    /// 变更出货通知单状态
    /// </summary>
    /// <param name="orderId">订单ID</param> 
    /// <returns></returns>
    public async Task UpdateShipOrderStatus(Guid? orderId)
    {
        var orderStatus = DIC_SALES_SHIP_ORDER_STATUS.WaitShip;

        var isExist = await Db.Queryable<SdShipOrderDetail>()
            .Where(x => x.OrderId == orderId)
           .Where(x => x.OutQTY != null && x.OutQTY > 0)
            .AnyAsync();

        if (isExist)
            orderStatus = DIC_SALES_SHIP_ORDER_STATUS.InShip;

        isExist = await Db.Queryable<SdShipOrderDetail>()
           .Where(x => x.OrderId == orderId)
           .Where(x => ((x.OutQTY != null && x.ShipQTY - x.OutQTY > 0) || x.OutQTY == null))
           .AnyAsync();
        if (!isExist)
            orderStatus = DIC_SALES_SHIP_ORDER_STATUS.ShipComplete;

        await Db.Updateable<SdShipOrder>()
            .SetColumns(it => new SdShipOrder()
            {
                OrderStatus = orderStatus,
                UpdateBy = UserId,
                UpdateTime = Utility.GetSysDate()
            })
            .Where(it => it.ID == orderId && it.OrderStatus != orderStatus)
            .ExecuteCommandAsync();
    }
    #endregion
}