/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoArrivalOrderDetail.cs
*
*功 能： N / A
* 类 名： PoArrivalOrderDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/9/13 16:11:40  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 采购到货通知单明细 (服务)
/// </summary>
public class PoArrivalOrderDetailServices : BaseServices<PoArrivalOrderDetail, PoArrivalOrderDetailDto, InsertPoArrivalOrderDetailInput, EditPoArrivalOrderDetailInput>, IPoArrivalOrderDetailServices
{
    private readonly IBaseRepository<PoArrivalOrderDetail> _dal;
    public PoArrivalOrderDetailServices(IBaseRepository<PoArrivalOrderDetail> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    #region 更新
    public override async Task<PoArrivalOrderDetailDto> UpdateReturn(Guid Id, object entity1)
    {
        try
        {
            await Db.Ado.BeginTranAsync();
            var model = ConvertToEntity(entity1);

            #region 检查是否存在相同值
            CheckOnly(model, Id);
            #endregion

            var entity = await Query(Id);
            var orderDetail = await Db.Queryable<PoOrderDetail>().FirstAsync(x => x.ID == entity.SourceOrderDetailId);

            var qty = orderDetail.QTY - (orderDetail.NoticeQTY ?? 0);

            if (model.NoticeQTY > (qty + entity.NoticeQTY))
                throw new Exception($"采购数量最大为：{Utility.RemoveZero(qty + entity.NoticeQTY)}");

            orderDetail.NoticeQTY = orderDetail.NoticeQTY - entity.NoticeQTY + model.NoticeQTY;

            var dic = ConvertToDic(entity1);
            var lstColumns = new ModuleSqlColumn("PO_ARRIVAL_ORDER_DETAIL_MNG").GetModuleTableEditableColumns();

            await Update(model, lstColumns);

            var model1 = Mapper.Map(model).ToANew<PoArrivalOrderDetailDto>();
            await Db.Updateable<PoOrderDetail>()
                .SetColumns(it => new PoOrderDetail()
                {
                    NoticeQTY = orderDetail.NoticeQTY
                }, true)
                .Where(it => it.ID == entity.SourceOrderDetailId)
                .ExecuteCommandAsync();

            await UpdatePurchaseOrderStatus(entity.SourceOrderId);

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

            var sourceOrderIds = new List<Guid?>();
            var orderIds = new List<Guid?>();

            var entities = new List<PoArrivalOrderDetail>();
            foreach (var id in ids)
            {
                if (!await AnyAsync(id))
                    continue;

                var entity = await Query(id);
                if (entity.IsDeleted)
                    continue;

                if (await Db.Queryable<PoArrivalOrder>().AnyAsync(x => x.AuditStatus != DIC_SYSTEM_AUDIT_STATUS.Add && x.ID == entity.OrderId))
                    throw new Exception($"该订单已审核通过，不可删除！");

                #region 采购通知数量回退
                sourceOrderIds.Add(entity.SourceOrderId);
                orderIds.Add(entity.OrderId);

                await Db.Updateable<PoOrderDetail>()
                    .SetColumns(it => new PoOrderDetail()
                    {
                        NoticeQTY = it.NoticeQTY - entity.NoticeQTY
                    }, true)
                    .Where(it => it.ID == entity.SourceOrderDetailId)
                    .ExecuteCommandAsync();
                #endregion

                entity.IsDeleted = true;
                entities.Add(entity);
            }
            await BaseDal.Update(entities, ["IsDeleted"]);

            #region 变更采购单状态
            if (sourceOrderIds.Any())
            {
                sourceOrderIds = sourceOrderIds.Distinct().ToList();

                for (int i = 0; i < sourceOrderIds.Count; i++)
                {
                    await UpdatePurchaseOrderStatus(sourceOrderIds[i]);
                }
            }
            #endregion

            #region 批量更新排序号

            if (orderIds.Any())
            {
                orderIds = orderIds.Distinct().ToList();

                for (int i = 0; i < orderIds.Count; i++)
                {
                    await IVChangeHelper.UpdataOrderDetailSerialNumber(Db, "PoArrivalOrderDetail", orderIds[i]);
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

    #region 更新采购单订单状态
    /// <summary>
    /// 更新采购单订单状态
    /// </summary>
    /// <param name="orderId">采购单ID</param>
    /// <returns></returns>
    public async Task UpdatePurchaseOrderStatus(Guid? orderId)
    {
        var dt = Utility.GetSysDate();
        var userId = App.User.ID;

        var orderStatus = DIC_PURCHASE_ORDER_STATUS.InNotice;
        var isExist = await Db.Queryable<PoOrderDetail>()
             .Where(x => x.OrderId == orderId && ((x.NoticeQTY != null && x.QTY - x.NoticeQTY > 0) || x.NoticeQTY == null))
             .AnyAsync();
        if (!isExist)
            orderStatus = DIC_PURCHASE_ORDER_STATUS.NoticeComplete;

        await Db.Updateable<PoOrder>()
            .SetColumns(it => new PoOrder()
            {
                OrderStatus = orderStatus
            }, true)
            .Where(x => x.ID == orderId && x.OrderStatus != orderStatus)
            .ExecuteCommandAsync();
    }
    #endregion 
}