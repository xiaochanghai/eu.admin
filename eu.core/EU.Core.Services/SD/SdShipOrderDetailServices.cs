/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdShipOrderDetail.cs
*
*功 能： N / A
* 类 名： SdShipOrderDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/8/12 16:11:12  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Services;

/// <summary>
/// 发货单明细 (服务)
/// </summary>
public class SdShipOrderDetailServices : BaseServices<SdShipOrderDetail, SdShipOrderDetailDto, InsertSdShipOrderDetailInput, EditSdShipOrderDetailInput>, ISdShipOrderDetailServices
{
    private readonly IBaseRepository<SdShipOrderDetail> _dal;
    public SdShipOrderDetailServices(IBaseRepository<SdShipOrderDetail> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    #region 更新
    public override async Task<SdShipOrderDetailDto> UpdateReturn(Guid Id, object entity1)
    {
        try
        {
            await Db.Ado.BeginTranAsync();
            var model = ConvertToEntity(entity1);

            if (model.ShipQTY <= 0)
                throw new Exception($"出货通知数量不能小于0！");

            #region 检查是否存在相同值
            CheckOnly(model, Id);
            #endregion

            var entity = await QueryDto(Id);
            var orderDetail = await Db.Queryable<SdOrderDetail>().FirstAsync(x => x.ID == entity.SalesOrderDetailId);

            var qty = orderDetail.QTY - (orderDetail.ShipQTY ?? 0);

            if (model.ShipQTY > (qty + entity.ShipQTY))
                throw new Exception($"出货通知数量最大为：{Utility.RemoveZero(qty + entity.ShipQTY)}");

            orderDetail.ShipQTY = orderDetail.ShipQTY - entity.ShipQTY + model.ShipQTY;
            var lstColumns = new ModuleSqlColumn("SD_SHIP_ORDER_DETAIL_MNG").GetModuleTableEditableColumns();

            await Update(model, lstColumns);

            var model1 = Mapper.Map(model).ToANew<SdShipOrderDetailDto>();
            model1.SalesOrderQTY = orderDetail.QTY - orderDetail.ShipQTY;
            model1.TaxIncludedAmount = model.ShipQTY * orderDetail.Price;
            model1.NoOutQTY = model.ShipQTY - (model.OutQTY ?? 0);

            await Db.Updateable<SdOrderDetail>()
                .SetColumns(it => new SdOrderDetail() { ShipQTY = orderDetail.ShipQTY }, true)
                .Where(it => it.ID == entity.SalesOrderDetailId)
                .ExecuteCommandAsync();

            #region 变更销售单状态 
            await UpdateSalesOrderStatus(entity.SalesOrderId);
            #endregion

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

            Guid? salesOrderId = null;
            Guid? shipOrderId = null;

            var entities = new List<SdShipOrderDetail>();
            foreach (var id in ids)
            {
                if (!await AnyAsync(id))
                    continue;

                var entity = await Query(id);
                if (entity.IsDeleted)
                    continue;

                if (await Db.Queryable<SdShipOrder>().AnyAsync(x => x.AuditStatus != DIC_SYSTEM_AUDIT_STATUS.Add && x.ID == entity.OrderId))
                    throw new Exception($"该订单已审核通过，不可删除！");

                #region 出货数量回退
                salesOrderId = entity.SalesOrderId;
                shipOrderId = entity.OrderId;

                await Db.Updateable<SdOrderDetail>()
                    .SetColumns(it => new SdOrderDetail() { ShipQTY = it.ShipQTY - entity.ShipQTY }, true)
                    .Where(it => it.ID == entity.SalesOrderDetailId)
                    .ExecuteCommandAsync();
                #endregion

                entity.IsDeleted = true;
                entities.Add(entity);
            }
            await BaseDal.Update(entities, ["IsDeleted"]);

            #region 变更销售单状态
            if (salesOrderId.IsNotEmptyOrNull())
                await UpdateSalesOrderStatus(salesOrderId);
            #endregion

            #region 批量更新排序号 
            if (shipOrderId.IsNotEmptyOrNull())
                await IVChangeHelper.UpdataOrderDetailSerialNumber(Db, "SdShipOrderDetail", shipOrderId);
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
           .Where(x => x.ShipQTY != null && x.ShipQTY > 0)
            .AnyAsync();

        if (isExist)
            orderStatus = DIC_SALES_ORDER_STATUS.InShip;

        isExist = await Db.Queryable<SdOrderDetail>()
           .Where(x => x.OrderId == orderId)
           .Where(x => ((x.ShipQTY != null && x.QTY - x.ShipQTY > 0) || x.ShipQTY == null))
           .AnyAsync();
        if (!isExist)
            orderStatus = DIC_SALES_ORDER_STATUS.ShipComplete;

        await Db.Updateable<SdOrder>()
            .SetColumns(it => new SdOrder() { SalesOrderStatus = orderStatus }, true)
            .Where(it => it.ID == orderId && it.SalesOrderStatus != orderStatus)
            .ExecuteCommandAsync();
    }
    #endregion
}