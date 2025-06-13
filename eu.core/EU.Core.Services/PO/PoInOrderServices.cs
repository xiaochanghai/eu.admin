/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoInOrder.cs
*
*功 能： N / A
* 类 名： PoInOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/9/13 20:11:32  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 采购入库单 (服务)
/// </summary>
public class PoInOrderServices : BaseServices<PoInOrder, PoInOrderDto, InsertPoInOrderInput, EditPoInOrderInput>, IPoInOrderServices
{
    private readonly IBaseRepository<PoInOrder> _dal;
    private readonly IPoInOrderDetailServices _poInOrderDetailServices;
    public PoInOrderServices(IBaseRepository<PoInOrder> dal,
        IPoInOrderDetailServices poInOrderDetailServices)
    {
        this._dal = dal;
        base.BaseDal = dal;
        _poInOrderDetailServices = poInOrderDetailServices;
    }

    #region 更新
    public override async Task<bool> Update(Guid Id, object entity)
    {
        var model = ConvertToEntity(entity);
        var entity1 = await base.QueryDto(Id);
        var result = await base.Update(Id, entity);

        if (entity1.StockId != model.StockId || entity1.GoodsLocationId != model.GoodsLocationId)
            await Db.Updateable<PoInOrderDetail>()
                .SetColumns(it => new PoInOrderDetail()
                {
                    StockId = model.StockId,
                    GoodsLocationId = model.GoodsLocationId
                }, true)
                .Where(x => x.OrderId == Id && (x.StockId == null || x.GoodsLocationId == null))
                .ExecuteCommandAsync();
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
        var entities = new List<PoInOrder>();
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

            var detailIds = await Db.Queryable<PoInOrderDetail>().Where(x => x.OrderId == id).Select(x => x.ID).ToArrayAsync();
            await _poInOrderDetailServices.Delete(detailIds);
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
    public override async Task<bool> BulkAudit(Guid[] ids) => await BulkAudit(ids, "OrderStatus = 'Wait'");
    #endregion

    #region 撤销数据 
    /// <summary>
    /// 撤销指定ID集合的数据(批量撤销)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public override async Task<bool> BulkRevocation(Guid[] ids)
    {
        var entities = new List<PoInOrder>();
        foreach (var id in ids)
        {
            if (!await AnyAsync(id))
                continue;

            var entity = await Query(id);

            if (entity.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit && entity.OrderStatus == DIC_PURCHASE_ORDER_STATUS.Wait)
            {
                entity.AuditStatus = DIC_SYSTEM_AUDIT_STATUS.Add;
                entities.Add(entity);
            }
        }
        await BaseDal.Update(entities, ["AuditStatus"], null, "OrderStatus = 'Wait'");
        return true;
    }
    #endregion

    #region 批量导入明细
    public async Task<ServiceResult> BulkInsertDetailAsync(object entity, Guid id)
    {
        try
        {
            await Db.Ado.BeginTranAsync();
            string json = entity.ToString();
            var poOrderIds = new List<Guid>();
            var poNoticeOrderIds = new List<Guid>();
            var dicts = JsonHelper.JsonToObj<List<Dictionary<string, object>>>(json);

            dicts.ForEach(x =>
            {
                if (x.ContainsKey("PoOrderId"))
                    poOrderIds.Add(Guid.Parse(x["PoOrderId"].ToString()));

                if (x.ContainsKey("PoNoticeOrderId"))
                    poNoticeOrderIds.Add(Guid.Parse(x["PoNoticeOrderId"].ToString()));
            });

            var dt = Utility.GetSysDate();
            var userId = App.User.ID;
            poOrderIds = poOrderIds.Distinct().ToList();
            poNoticeOrderIds = poNoticeOrderIds.Distinct().ToList();

            var addDetails = new List<PoInOrderDetail>();
            var updateDetails = new List<PoInOrderDetail>();

            var inOrder = await base.QueryDto(id);
            if (inOrder.OrderSource == "NoticeOrder")
                for (int j = 0; j < dicts.Count; j++)
                {
                    var x = dicts[j];

                    var poNoticeOrderId = Guid.Parse(x["PoNoticeOrderId"].ToString());
                    var poNoticeOrderDetailId = Guid.Parse(x["ID"].ToString());
                    decimal? qty = Convert.ToDecimal(x["InQTY"].ToString());

                    var poNoticeOrderDetail = await Db.Queryable<PoArrivalOrderDetail>().FirstAsync(x => x.ID == poNoticeOrderDetailId);
                    if (!poNoticeOrderDetail.IsNullOrEmpty())
                    {
                        if (poNoticeOrderDetail.NoticeQTY - (poNoticeOrderDetail.InQTY ?? 0) >= qty)
                            poNoticeOrderDetail.InQTY = (poNoticeOrderDetail.InQTY ?? 0) + qty;
                        else
                        {
                            qty = poNoticeOrderDetail.NoticeQTY - (poNoticeOrderDetail.InQTY ?? 0);
                            poNoticeOrderDetail.InQTY = poNoticeOrderDetail.NoticeQTY;
                        }

                        await Db.Updateable<PoArrivalOrderDetail>()
                            .SetColumns(it => new PoArrivalOrderDetail()
                            {
                                InQTY = poNoticeOrderDetail.InQTY
                            }, true)
                            .Where(it => it.ID == poNoticeOrderDetailId)
                            .ExecuteCommandAsync();

                        var inOrderDetail = await Db.Queryable<PoInOrderDetail>().FirstAsync(x => x.OrderId == id && x.SourceOrderDetailId == poNoticeOrderDetailId);
                        if (inOrderDetail.IsNullOrEmpty())
                            addDetails.Add(new PoInOrderDetail()
                            {
                                SerialNumber = 1,
                                OrderId = id,
                                SourceOrderId = poNoticeOrderId,
                                SourceOrderDetailId = poNoticeOrderDetailId,
                                MaterialId = Guid.Parse(x["MaterialId"].ToString()),
                                InQTY = qty,
                                ReturnQTY = 0,
                                GroupId = Utility.GetGroupGuidId(),
                                CompanyId = Utility.GetCompanyGuidId()
                            });
                        else
                        {
                            inOrderDetail.InQTY += qty;
                            updateDetails.Add(inOrderDetail);
                        }
                    }
                }
            else
                for (int j = 0; j < dicts.Count; j++)
                {
                    var x = dicts[j];

                    var poOrderId = Guid.Parse(x["PoOrderId"].ToString());
                    var poOrderDetailId = Guid.Parse(x["ID"].ToString());
                    decimal? qty = Convert.ToDecimal(x["InQTY"].ToString());

                    var poOrderDetail = await Db.Queryable<PoOrderDetail>().FirstAsync(x => x.ID == poOrderDetailId);
                    if (!poOrderDetail.IsNullOrEmpty())
                    {
                        if (poOrderDetail.InQTY - (poOrderDetail.InQTY ?? 0) >= qty)
                            poOrderDetail.InQTY = (poOrderDetail.InQTY ?? 0) + qty;
                        else
                        {
                            qty = poOrderDetail.InQTY - (poOrderDetail.InQTY ?? 0);
                            poOrderDetail.InQTY = poOrderDetail.QTY;
                        }

                        await Db.Updateable<PoOrderDetail>()
                            .SetColumns(it => new PoOrderDetail()
                            {
                                InQTY = poOrderDetail.InQTY
                            }, true)
                            .Where(it => it.ID == poOrderDetailId)
                            .ExecuteCommandAsync();

                        var inOrderDetail = await Db.Queryable<PoInOrderDetail>().FirstAsync(x => x.OrderId == id && x.SourceOrderDetailId == poOrderDetailId);
                        if (inOrderDetail.IsNullOrEmpty())
                            addDetails.Add(new PoInOrderDetail()
                            {
                                SerialNumber = 1,
                                OrderId = id,
                                SourceOrderId = poOrderId,
                                SourceOrderDetailId = poOrderDetailId,
                                MaterialId = Guid.Parse(x["MaterialId"].ToString()),
                                InQTY = qty,
                                ReturnQTY = 0,
                                GroupId = Utility.GetGroupGuidId(),
                                CompanyId = Utility.GetCompanyGuidId()
                            });
                        else
                        {
                            inOrderDetail.InQTY += qty;
                            updateDetails.Add(inOrderDetail);
                        }
                    }
                }

            if (addDetails.Any())
                await Db.Insertable(addDetails).ExecuteCommandAsync();

            if (updateDetails.Any())
                await Db.Updateable(updateDetails).UpdateColumns(it => new { it.InQTY }).ExecuteCommandAsync();

            for (int i = 0; i < poOrderIds.Count; i++)
                await _poInOrderDetailServices.UpdateSourceOrderStatus(poOrderIds[i], "PurchaseOrder");

            for (int i = 0; i < poNoticeOrderIds.Count; i++)
                await _poInOrderDetailServices.UpdateSourceOrderStatus(poNoticeOrderIds[i], "NoticeOrder");

            await IVChangeHelper.UpdataOrderDetailSerialNumber(Db, "PoInOrderDetail", id);

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
        await Db.Updateable<PoInOrder>()
            .SetColumns(it => new PoInOrder()
            {
                OrderStatus = DIC_PURCHASE_IN_ORDER_STATUS.OrderComplete
            }, true)
            .Where(it =>
            ids.Contains(it.ID) &&
            it.OrderStatus != DIC_PURCHASE_IN_ORDER_STATUS.Wait &&
            it.OrderStatus != DIC_PURCHASE_IN_ORDER_STATUS.OrderComplete &&
            it.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit)
            .ExecuteCommandAsync();
        return Success(ResponseText.EXECUTE_SUCCESS);

    }
    #endregion

    #region 订单过账
    /// <summary>
    /// 订单过账指定ID集合的数据(订单过账)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public async Task<ServiceResult> BulkOrderCarryTo(Guid[] ids)
    {
        await Db.Updateable<PoInOrder>()
            .SetColumns(it => new PoInOrder()
            {
                OrderStatus = DIC_PURCHASE_IN_ORDER_STATUS.CompleteIn
            }, true)
            .Where(it =>
            ids.Contains(it.ID) &&
            it.OrderStatus != DIC_PURCHASE_IN_ORDER_STATUS.CompleteIn &&
            it.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit)
            .ExecuteCommandAsync();
        return Success(ResponseText.EXECUTE_SUCCESS);
    }
    #endregion
}