/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoInOrderDetail.cs
*
*功 能： N / A
* 类 名： PoInOrderDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/9/13 20:11:24  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 采购入库单明细 (服务)
/// </summary>
public class PoInOrderDetailServices : BaseServices<PoInOrderDetail, PoInOrderDetailDto, InsertPoInOrderDetailInput, EditPoInOrderDetailInput>, IPoInOrderDetailServices
{
    private readonly IBaseRepository<PoInOrderDetail> _dal;
    public PoInOrderDetailServices(IBaseRepository<PoInOrderDetail> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    #region 更新
    public override async Task<PoInOrderDetailDto> UpdateReturn(Guid Id, object entity1)
    {
        try
        {
            await Db.Ado.BeginTranAsync();
            var model = ConvertToEntity(entity1);

            #region 检查是否存在相同值
            CheckForm(model, OperateType.Update, Id);
            #endregion

            var entity = await QueryDto(Id);
            var order = await Db.Queryable<PoInOrder>().SingleAsync(x => x.ID == entity.OrderId);

            if (await Db.Queryable<PoInOrder>().AnyAsync(x => x.ID == entity.OrderId && x.OrderSource == "NoticeOrder"))
            {
                var orderDetail = await Db.Queryable<PoArrivalOrderDetail>().SingleAsync(x => x.ID == entity.SourceOrderDetailId);

                var qty = orderDetail.NoticeQTY - (orderDetail.InQTY ?? 0);

                if (model.InQTY > (qty + entity.InQTY))
                    throw new Exception($"入库数量最大为：{Utility.RemoveZero(qty + entity.InQTY)}");

                orderDetail.InQTY = orderDetail.InQTY - entity.InQTY + model.InQTY;

                await Db.Updateable<PoArrivalOrderDetail>()
                    .SetColumns(it => new PoArrivalOrderDetail()
                    {
                        InQTY = orderDetail.InQTY,
                        UpdateBy = UserId,
                        UpdateTime = Utility.GetSysDate()
                    })
                    .Where(x => x.ID == entity.SourceOrderDetailId && x.InQTY != orderDetail.InQTY)
                    .ExecuteCommandAsync();

                await UpdateSourceOrderStatus(entity.SourceOrderId, "NoticeOrder");
            }
            else
            {
                var orderDetail = await Db.Queryable<PoOrderDetail>().SingleAsync(x => x.ID == entity.SourceOrderDetailId);

                var qty = orderDetail.NoticeQTY - (orderDetail.InQTY ?? 0);

                if (model.InQTY > (qty + entity.InQTY))
                    throw new Exception($"入库数量最大为：{Utility.RemoveZero(qty + entity.InQTY)}");

                orderDetail.InQTY = orderDetail.InQTY - entity.InQTY + model.InQTY;


                await Db.Updateable<PoOrderDetail>()
                    .SetColumns(it => new PoOrderDetail()
                    {
                        InQTY = orderDetail.InQTY,
                        UpdateBy = UserId,
                        UpdateTime = Utility.GetSysDate()
                    })
                    .Where(x => x.ID == entity.SourceOrderDetailId && x.InQTY != orderDetail.InQTY)
                    .ExecuteCommandAsync();
                await UpdateSourceOrderStatus(entity.SourceOrderId, "PurchaseOrder");
            }

            var lstColumns = new ModuleSqlColumn("PO_IN_ORDER_DETAIL_MNG").GetModuleTableEditableColumns();

            lstColumns.Add("UpdateBy");
            lstColumns.Add("UpdateTime");
            await Update(model, lstColumns, null, $"ID='{Id}'");

            var model1 = Mapper.Map(model).ToANew<PoInOrderDetailDto>();

            var sql = @$"SELECT StockName1 StockName,GoodsLocationName1 GoodsLocationName FROM BdGoodsLocation_V where ID='{model.GoodsLocationId}'";
            var goodsLocation = await Db.Ado.SqlQuerySingleAsync<PoInOrderDetailDto>(sql);
            if (goodsLocation != null)
            {
                model1.StockName = goodsLocation.StockName;
                model1.GoodsLocationName = goodsLocation.GoodsLocationName;
            }
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
            var orderIds = new List<Guid?>();

            var entities = new List<PoInOrderDetail>();
            foreach (var id in ids)
            {
                if (!await AnyAsync(id))
                    continue;

                var entity = await Query(id);
                if (entity.IsDeleted)
                    continue;

                if (await Db.Queryable<PoInOrder>().AnyAsync(x => x.AuditStatus != DIC_SYSTEM_AUDIT_STATUS.Add && x.ID == entity.OrderId))
                    throw new Exception($"该订单已审核通过，不可删除！");

                if (await Db.Queryable<PoInOrder>().AnyAsync(x => x.ID == entity.OrderId && x.OrderSource == "NoticeOrder"))
                {
                    #region 采购通知单数量回退
                    orderIds.Add(entity.OrderId);

                    await Db.Updateable<PoArrivalOrderDetail>()
                        .SetColumns(it => new PoArrivalOrderDetail()
                        {
                            InQTY = it.InQTY - entity.InQTY,
                            UpdateBy = UserId,
                            UpdateTime = Utility.GetSysDate()
                        })
                        .Where(it => it.ID == entity.SourceOrderDetailId)
                        .ExecuteCommandAsync();
                    #endregion

                    await UpdateSourceOrderStatus(entity.SourceOrderId, "NoticeOrder");
                }
                else
                {
                    #region 采购通知单数量回退
                    orderIds.Add(entity.OrderId);
                    await Db.Updateable<PoOrderDetail>()
                        .SetColumns(it => new PoOrderDetail()
                        {
                            InQTY = it.InQTY - entity.InQTY,
                            UpdateBy = UserId,
                            UpdateTime = Utility.GetSysDate()
                        })
                        .Where(it => it.ID == entity.SourceOrderDetailId)
                        .ExecuteCommandAsync();
                    #endregion

                    await UpdateSourceOrderStatus(entity.SourceOrderId, "PurchaseOrder");
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