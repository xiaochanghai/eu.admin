/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoOrderDetail.cs
*
*功 能： N / A
* 类 名： PoOrderDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/9/9 17:43:23  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 采购单明细 (服务)
/// </summary>
public class PoOrderDetailServices : BaseServices<PoOrderDetail, PoOrderDetailDto, InsertPoOrderDetailInput, EditPoOrderDetailInput>, IPoOrderDetailServices
{
    private readonly IBaseRepository<PoOrderDetail> _dal;
    public PoOrderDetailServices(IBaseRepository<PoOrderDetail> dal)
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
    public override async Task<List<Guid>> Add(List<InsertPoOrderDetailInput> listEntity)
    {
        var orderId = listEntity[0].OrderId;
        var result = new List<Guid>();

        var inserts = new List<InsertPoOrderDetailInput>();
        var updates = new List<PoOrderDetail>();

        var order = await Db.Queryable<PoOrder>().FirstAsync(x => x.ID == orderId);
        for (int i = 0; i < listEntity.Count; i++)
        {
            var entity = listEntity[i];
            entity.OrderSource = entity.OrderSource ?? "Material";

            if (entity.OrderSource == "Requestion")
            {
                var poRequestionDetail = await Db.Queryable<PoRequestionDetail>().FirstAsync(x => x.ID == entity.SourceOrderDetailId);

                if (poRequestionDetail.IsNullOrEmpty())
                    continue;

                if (poRequestionDetail.QTY <= poRequestionDetail.PurchaseQTY)
                    continue;

                if (poRequestionDetail.QTY - (poRequestionDetail.PurchaseQTY ?? 0) >= entity.QTY)
                    poRequestionDetail.PurchaseQTY = (poRequestionDetail.PurchaseQTY ?? 0) + entity.QTY;
                else
                    poRequestionDetail.PurchaseQTY = poRequestionDetail.QTY;

                await Db.Updateable<PoRequestionDetail>()
                    .SetColumns(it => new PoRequestionDetail()
                    {
                        PurchaseQTY = poRequestionDetail.PurchaseQTY
                    })
                    .Where(it => it.ID == entity.SourceOrderDetailId)
                    .ExecuteCommandAsync();

                var orderStatus = DIC_PURCHASE_REQUEST_STATUS.InPurchase;

                var isExist = await Db.Queryable<PoRequestionDetail>()
                     .Where(x => x.OrderId == poRequestionDetail.OrderId)
                     .WhereIF(1 == 1, x => ((x.PurchaseQTY != null && x.QTY - x.PurchaseQTY > 0) || x.PurchaseQTY == null))
                     .AnyAsync();
                if (!isExist)
                    orderStatus = DIC_PURCHASE_REQUEST_STATUS.PurchaseComplete;

                await Db.Updateable<PoRequestion>()
                    .SetColumns(it => new PoRequestion()
                    {
                        OrderStatus = orderStatus
                    })
                    .Where(it => it.ID == poRequestionDetail.OrderId)
                    .ExecuteCommandAsync();
            }

            var detail = await base.QuerySingle(x => x.OrderId == orderId && x.MaterialId == entity.MaterialId && x.OrderSource == entity.OrderSource);
            if (detail.IsNullOrEmpty())
            {
                (decimal? NoTaxAmount, decimal? TaxAmount, decimal? TaxIncludedAmount) = IVChangeHelper.UpdataTaxAmount(order.TaxType, order.TaxRate, entity.Price, entity.QTY);
                entity.DeliveryDate = order.DeliveryDate;
                entity.NoTaxAmount = NoTaxAmount;
                entity.TaxAmount = TaxIncludedAmount;
                entity.TaxIncludedAmount = TaxIncludedAmount;

                inserts.Add(entity);
            }
            else
            {
                detail.QTY += entity.QTY;
                (decimal? NoTaxAmount, decimal? TaxAmount, decimal? TaxIncludedAmount) = IVChangeHelper.UpdataTaxAmount(order.TaxType, order.TaxRate, detail.Price, detail.QTY);
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
        await IVChangeHelper.UpdataOrderDetailSerialNumber(Db, "PoOrderDetail", orderId);
        return result;
    }
    #endregion

    #region 更新
    public override async Task<PoOrderDetailDto> UpdateReturn(Guid Id, object entity1)
    {
        try
        {
            await Db.Ado.BeginTranAsync(); 
            var model = ConvertToEntity(entity1);

            #region 检查是否存在相同值
            CheckOnly(model, Id);
            #endregion

            var entity = await QueryDto(Id);

            if (entity.OrderSource == "Requestion")
            {
                var orderDetail = await Db.Queryable<PoRequestionDetail>().FirstAsync(x => x.ID == entity.SourceOrderDetailId);

                var qty = orderDetail.QTY - (orderDetail.PurchaseQTY ?? 0);

                if (model.QTY > (qty + entity.QTY))
                    throw new Exception($"采购数量最大为：{Utility.RemoveZero(qty + entity.QTY)}");

                orderDetail.PurchaseQTY = orderDetail.PurchaseQTY - entity.QTY + model.QTY;

                await Db.Updateable<PoRequestionDetail>()
                    .SetColumns(it => new PoRequestionDetail()
                    {
                        PurchaseQTY = orderDetail.PurchaseQTY,
                        UpdateBy = UserId,
                        UpdateTime = Utility.GetSysDate()
                    })
                    .Where(it => it.ID == entity.SourceOrderDetailId)
                    .ExecuteCommandAsync();

                var orderStatus = DIC_PURCHASE_REQUEST_STATUS.InPurchase;
                var isExist = await Db.Queryable<PoRequestionDetail>()
                    .Where(x => x.OrderId == entity.SourceOrderId)
                    .WhereIF(1 == 1, x => ((x.PurchaseQTY != null && x.QTY - x.PurchaseQTY > 0) || x.PurchaseQTY == null))
                    .AnyAsync();
                if (!isExist)
                    orderStatus = DIC_PURCHASE_REQUEST_STATUS.PurchaseComplete;

                await Db.Updateable<PoRequestion>()
                    .SetColumns(it => new PoRequestion()
                    {
                        OrderStatus = orderStatus
                    })
                    .Where(it => it.ID == entity.SourceOrderId)
                    .ExecuteCommandAsync();
            }

            var order = await Db.Queryable<PoOrder>().FirstAsync(x => x.ID == entity.OrderId);
            (decimal? NoTaxAmount, decimal? TaxAmount, decimal? TaxIncludedAmount) = IVChangeHelper.UpdataTaxAmount(order.TaxType, order.TaxRate, model.Price, model.QTY);
            model.NoTaxAmount = NoTaxAmount;
            model.TaxAmount = TaxAmount;
            model.TaxIncludedAmount = TaxIncludedAmount;

            var lstColumns = new ModuleSqlColumn("PO_ORDER_DETAIL_MNG").GetModuleTableEditableColumns();

            lstColumns.Add("UpdateBy");
            lstColumns.Add("UpdateTime");
            lstColumns.Add("NoTaxAmount");
            lstColumns.Add("TaxAmount");
            lstColumns.Add("TaxIncludedAmount");
            await Update(model, lstColumns, null, $"ID='{Id}'");

            var model1 = Mapper.Map(model).ToANew<PoOrderDetailDto>();
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

            var requestionOrderIds = new List<Guid?>();
            Guid? orderId = null;

            var entities = new List<PoOrderDetail>();
            foreach (var id in ids)
            {
                if (!await AnyAsync(id))
                    continue;

                var entity = await Query(id);
                if (entity.IsDeleted)
                    continue;

                if (await Db.Queryable<PoOrder>().AnyAsync(x => x.AuditStatus != DIC_SYSTEM_AUDIT_STATUS.Add && x.ID == entity.OrderId))
                    throw new Exception($"该订单已审核通过，不可删除！");

                orderId = entity.OrderId;
                if (entity.OrderSource == "Requestion")
                {
                    #region 请购数量回退
                    requestionOrderIds.Add(entity.SourceOrderId);
                    await Db.Updateable<PoRequestionDetail>()
                        .SetColumns(it => new PoRequestionDetail()
                        {
                            PurchaseQTY = it.PurchaseQTY - entity.QTY,
                            UpdateBy = UserId,
                            UpdateTime = Utility.GetSysDate()
                        })
                        .Where(it => it.ID == entity.SourceOrderDetailId)
                        .ExecuteCommandAsync();
                    #endregion
                }

                entity.IsDeleted = true;
                entities.Add(entity);
            }
            await BaseDal.Update(entities, ["IsDeleted"]);

            #region 变更请购状态 
            if (requestionOrderIds.Any())
            {
                requestionOrderIds = requestionOrderIds.Distinct().ToList();
                for (int i = 0; i < requestionOrderIds.Count; i++)
                {
                    var orderStatus = DIC_PURCHASE_REQUEST_STATUS.Wait;
                    if (await Db.Queryable<PoRequestionDetail>()
                        .Where(x => x.OrderId == requestionOrderIds[i] && x.PurchaseQTY != null && x.PurchaseQTY > 0)
                        .AnyAsync())
                        orderStatus = DIC_PURCHASE_REQUEST_STATUS.InPurchase;
                    var isExist = await Db.Queryable<PoRequestionDetail>()
                        .Where(x => x.OrderId == requestionOrderIds[i])
                        .WhereIF(1 == 1, x => ((x.PurchaseQTY != null && x.QTY - x.PurchaseQTY > 0) || x.PurchaseQTY == null))
                        .AnyAsync();
                    if (!isExist)
                        orderStatus = DIC_PURCHASE_REQUEST_STATUS.PurchaseComplete;

                    await Db.Updateable<PoRequestion>()
                        .SetColumns(it => new PoRequestion()
                        {
                            OrderStatus = orderStatus
                        })
                        .Where(it => it.ID == requestionOrderIds[i] && it.OrderStatus != orderStatus)
                        .ExecuteCommandAsync();
                }
            }
            #endregion

            #region 批量更新排序号
            if (orderId.IsNotEmptyOrNull())
                await IVChangeHelper.UpdataOrderDetailSerialNumber(Db, "PoOrderDetail", orderId);
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
}