/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoReturnOrderDetail.cs
*
* 功 能： N / A
* 类 名： PoReturnOrderDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2024/10/8 14:03:36  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 采购退货单明细 (服务)
/// </summary>
public class PoReturnOrderDetailServices : BaseServices<PoReturnOrderDetail, PoReturnOrderDetailDto, InsertPoReturnOrderDetailInput, EditPoReturnOrderDetailInput>, IPoReturnOrderDetailServices
{
    private readonly IBaseRepository<PoReturnOrderDetail> _dal;
    public PoReturnOrderDetailServices(IBaseRepository<PoReturnOrderDetail> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    #region 新增 
    /// <summary>
    /// 批量插入实体(速度快)
    /// </summary>
    /// <param name="listEntity">实体集合</param>
    /// <returns>影响行数</returns>
    public override async Task<List<Guid>> Add(List<InsertPoReturnOrderDetailInput> listEntity)
    {
        var orderId = listEntity[0].OrderId;
        var result = new List<Guid>();

        var inserts = new List<InsertPoReturnOrderDetailInput>();
        var updates = new List<PoReturnOrderDetail>();

        var order = await Db.Queryable<PoOrder>().FirstAsync(x => x.ID == orderId);
        for (int i = 0; i < listEntity.Count; i++)
        {
            var entity = listEntity[i];
            entity.OrderSource = entity.OrderSource ?? "Material";

            if (entity.OrderSource == "PurchaseInOrder")
            {
                var poInDetail = await Db.Queryable<PoInOrderDetail>().FirstAsync(x => x.ID == entity.SourceOrderDetailId);

                if (poInDetail.IsNullOrEmpty())
                    continue;

                if (poInDetail.InQTY <= poInDetail.ReturnQTY)
                    continue;

                if (poInDetail.InQTY - (poInDetail.ReturnQTY ?? 0) >= entity.QTY)
                    poInDetail.ReturnQTY = (poInDetail.ReturnQTY ?? 0) + entity.QTY;
                else
                    poInDetail.ReturnQTY = poInDetail.InQTY;

                await Db.Updateable<PoInOrderDetail>()
                    .SetColumns(it => new PoInOrderDetail()
                    {
                        ReturnQTY = poInDetail.ReturnQTY
                    })
                    .Where(it => it.ID == entity.SourceOrderDetailId)
                    .ExecuteCommandAsync();

                var orderStatus = DIC_PURCHASE_IN_ORDER_STATUS.InReturn;

                var isExist = await Db.Queryable<PoInOrderDetail>()
                     .Where(x => x.OrderId == poInDetail.OrderId)
                     .WhereIF(1 == 1, x => ((x.ReturnQTY != null && x.InQTY - x.ReturnQTY > 0) || x.ReturnQTY == null))
                     .AnyAsync();
                if (!isExist)
                    orderStatus = DIC_PURCHASE_IN_ORDER_STATUS.CompleteReturn;

                await Db.Updateable<PoInOrder>()
                    .SetColumns(it => new PoInOrder()
                    {
                        OrderStatus = orderStatus
                    })
                    .Where(it => it.ID == poInDetail.OrderId)
                    .ExecuteCommandAsync();
            }

            var detail = await base.QuerySingle(x => x.OrderId == orderId && x.MaterialId == entity.MaterialId && x.OrderSource == entity.OrderSource);
            if (detail.IsNullOrEmpty())
                inserts.Add(entity);
            else
            {
                detail.QTY += entity.QTY;
                updates.Add(detail);
            }
        }
        if (inserts.Any())
            result = await base.Add(inserts);

        if (updates.Any())
        {
            await Db.Updateable(updates)
                .UpdateColumns(it => new { it.QTY })
                .ExecuteCommandAsync();
            result = updates.Select(x => x.ID).ToList();
        }
        await IVChangeHelper.UpdataOrderDetailSerialNumber(Db, "PoReturnOrderDetail", orderId);
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
        try
        {
            await Db.Ado.BeginTranAsync();
            var orderIds = new List<Guid?>();

            var entities = new List<PoReturnOrderDetail>();
            foreach (var id in ids)
            {
                if (!await AnyAsync(id))
                    continue;

                var entity = await Query(id);
                if (entity.IsDeleted)
                    continue;

                if (await Db.Queryable<PoReturnOrder>().AnyAsync(x => x.AuditStatus != DIC_SYSTEM_AUDIT_STATUS.Add && x.ID == entity.OrderId))
                    throw new Exception($"该订单已审核通过，不可删除！");

                if (await Db.Queryable<PoInOrder>().AnyAsync(x => x.ID == entity.OrderId && x.OrderSource == "NoticeOrder"))
                {
                    #region 采购入库单数量回退
                    orderIds.Add(entity.OrderId);
                    #endregion

                    await UpdateSourceOrderStatus(entity.SourceOrderId, "NoticeOrder");
                }

                entity.IsDeleted = true;
                entities.Add(entity);
            }
            await BaseDal.Update(entities, ["IsDeleted"]);


            #region 批量更新排序号

            if (orderIds.Any())
            {
                orderIds = orderIds.Distinct().ToList();

                for (int i = 0; i < orderIds.Count; i++)
                {
                    await IVChangeHelper.UpdataOrderDetailSerialNumber(Db, "PoReturnOrderDetail", orderIds[i]);
                }
            }
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

    #region 更新 采购到货通知/采购单 订单状态
    /// <summary>
    /// 更新 采购到货通知/采购单 订单状态
    /// </summary>
    /// <param name="orderId">订单ID</param> 
    /// <returns></returns>
    public async Task UpdateSourceOrderStatus(Guid? orderId, string orderSource)
    {
        var dt = Utility.GetSysDate();
        var userId = App.User.ID;
        if (orderSource == "NoticeOrder")
        {
            var orderStatus = DIC_PURCHASE_NOTICE_ORDER_STATUS.In;
            var isExist = await Db.Queryable<PoArrivalOrderDetail>()
                 .Where(x => x.OrderId == orderId && ((x.InQTY != null && x.NoticeQTY - x.InQTY > 0) || x.InQTY == null))
                 .AnyAsync();
            if (!isExist)
                orderStatus = DIC_PURCHASE_NOTICE_ORDER_STATUS.InComplete;

            await Db.Updateable<PoArrivalOrder>()
                .SetColumns(it => new PoArrivalOrder()
                {
                    OrderStatus = orderStatus,
                    UpdateBy = userId,
                    UpdateTime = dt
                })
                .Where(x => x.ID == orderId && x.OrderStatus != orderStatus)
                .ExecuteCommandAsync();
        }
        else
        {
            var orderStatus = DIC_PURCHASE_ORDER_STATUS.In;
            var isExist = await Db.Queryable<PoOrderDetail>()
                 .Where(x => x.OrderId == orderId && ((x.InQTY != null && x.QTY - x.InQTY > 0) || x.InQTY == null))
                 .AnyAsync();
            if (!isExist)
                orderStatus = DIC_PURCHASE_ORDER_STATUS.InComplete;

            await Db.Updateable<PoOrder>()
                .SetColumns(it => new PoOrder()
                {
                    OrderStatus = orderStatus,
                    UpdateBy = userId,
                    UpdateTime = dt
                })
                .Where(x => x.ID == orderId && x.OrderStatus != orderStatus)
                .ExecuteCommandAsync();
        }
    }
    #endregion
}