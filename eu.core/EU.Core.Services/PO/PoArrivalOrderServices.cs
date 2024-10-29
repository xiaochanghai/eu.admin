/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoArrivalOrder.cs
*
*功 能： N / A
* 类 名： PoArrivalOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/9/13 16:11:50  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 采购到货通知单 (服务)
/// </summary>
public class PoArrivalOrderServices : BaseServices<PoArrivalOrder, PoArrivalOrderDto, InsertPoArrivalOrderInput, EditPoArrivalOrderInput>, IPoArrivalOrderServices
{
    private readonly IBaseRepository<PoArrivalOrder> _dal;
    private readonly IPoArrivalOrderDetailServices _poArrivalOrderDetailServices;
    public PoArrivalOrderServices(IBaseRepository<PoArrivalOrder> dal, IPoArrivalOrderDetailServices poArrivalOrderDetailServices)
    {
        this._dal = dal;
        base.BaseDal = dal;
        _poArrivalOrderDetailServices = poArrivalOrderDetailServices;
    }

    #region 删除数据 
    /// <summary>
    /// 删除指定ID集合的数据(批量删除)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public override async Task<bool> Delete(Guid[] ids)
    {
        var entities = new List<PoArrivalOrder>();
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

            var detailIds = await Db.Queryable<PoArrivalOrderDetail>().Where(x => x.OrderId == id).Select(x => x.ID).ToArrayAsync();
            await _poArrivalOrderDetailServices.Delete(detailIds);
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
    public override async Task<bool> BulkAudit(Guid[] ids)
    {
        var entities = new List<PoArrivalOrder>();
        foreach (var id in ids)
        {
            if (!await AnyAsync(id))
                continue;

            var entity = await Query(id);

            if (entity.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.Add)
            {
                entity.AuditStatus = DIC_SYSTEM_AUDIT_STATUS.CompleteAudit;
                entities.Add(entity);
            }
        }
        await BaseDal.Update(entities, ["AuditStatus"], null, $"OrderStatus = '{DIC_PURCHASE_NOTICE_ORDER_STATUS.Wait}'");
        return true;
    }
    #endregion

    #region 撤销数据 
    /// <summary>
    /// 撤销指定ID集合的数据(批量撤销)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public override async Task<bool> BulkRevocation(Guid[] ids)
    {
        var entities = new List<PoArrivalOrder>();
        foreach (var id in ids)
        {
            if (!await AnyAsync(id))
                continue;

            var entity = await Query(id);

            if (entity.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit && entity.OrderStatus == DIC_PURCHASE_NOTICE_ORDER_STATUS.Wait)
            {
                entity.AuditStatus = DIC_SYSTEM_AUDIT_STATUS.Add;
                entities.Add(entity);
            }
        }
        await BaseDal.Update(entities, ["AuditStatus"], null, $"OrderStatus = '{DIC_PURCHASE_NOTICE_ORDER_STATUS.Wait}'");
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
            var dicts = JsonHelper.JsonToObj<List<Dictionary<string, object>>>(json);

            dicts.ForEach(x =>
            {
                if (x.ContainsKey("PoOrderId"))
                    poOrderIds.Add(Guid.Parse(x["PoOrderId"].ToString()));
            });

            var dt = Utility.GetSysDate();
            var userId = App.User.ID;
            poOrderIds = poOrderIds.Distinct().ToList();

            var addDetails = new List<PoArrivalOrderDetail>();
            var updateDetails = new List<PoArrivalOrderDetail>();

            for (int j = 0; j < dicts.Count; j++)
            {
                var x = dicts[j];

                var poOrderId = Guid.Parse(x["PoOrderId"].ToString());
                var poOrderDetailId = Guid.Parse(x["ID"].ToString());
                decimal? qty = Convert.ToDecimal(x["NoticeQTY"].ToString());

                var poOrderDetail = await Db.Queryable<PoOrderDetail>().FirstAsync(x => x.ID == poOrderDetailId);
                if (!poOrderDetail.IsNullOrEmpty())
                {
                    if (poOrderDetail.QTY - (poOrderDetail.NoticeQTY ?? 0) >= qty)
                        poOrderDetail.NoticeQTY = (poOrderDetail.NoticeQTY ?? 0) + qty;
                    else
                    {
                        qty = poOrderDetail.QTY - (poOrderDetail.NoticeQTY ?? 0);
                        poOrderDetail.NoticeQTY = poOrderDetail.QTY;
                    }

                    await Db.Updateable<PoOrderDetail>()
                        .SetColumns(it => new PoOrderDetail()
                        {
                            NoticeQTY = poOrderDetail.NoticeQTY,
                            UpdateBy = userId,
                            UpdateTime = dt
                        })
                        .Where(it => it.ID == poOrderDetailId)
                        .ExecuteCommandAsync();

                    var shipOrderDetail = await Db.Queryable<PoArrivalOrderDetail>().FirstAsync(x => x.OrderId == id && x.SourceOrderDetailId == poOrderDetailId);
                    if (shipOrderDetail.IsNullOrEmpty())
                        addDetails.Add(new PoArrivalOrderDetail()
                        {
                            SerialNumber = 1,
                            OrderId = id,
                            SourceOrderId = poOrderId,
                            SourceOrderDetailId = poOrderDetailId,
                            MaterialId = Guid.Parse(x["MaterialId"].ToString()),
                            NoticeQTY = qty,
                            InQTY = 0,
                            GroupId = Utility.GetGroupGuidId(),
                            CompanyId = Utility.GetCompanyGuidId()
                        });
                    else
                    {
                        shipOrderDetail.NoticeQTY += qty;
                        updateDetails.Add(shipOrderDetail);
                    }
                }
            }

            if (addDetails.Any())
                await Db.Insertable(addDetails).ExecuteCommandAsync();

            if (updateDetails.Any())
                await Db.Updateable(updateDetails).UpdateColumns(it => new { it.NoticeQTY }).ExecuteCommandAsync();

            for (int i = 0; i < poOrderIds.Count; i++)
                await _poArrivalOrderDetailServices.UpdatePurchaseOrderStatus(poOrderIds[i]);

            await IVChangeHelper.UpdataOrderDetailSerialNumber(Db, "PoArrivalOrderDetail", id);
            await Db.Ado.CommitTranAsync();

            return ServiceResult.OprateSuccess(ResponseText.EXECUTE_SUCCESS);

        }
        catch (Exception E)
        {
            await Db.Ado.RollbackTranAsync();

            return ServiceResult.OprateFailed(E.Message);
        }
    }
    #endregion

    #region 批量导入入库单
    public async Task<ServiceResult> BulkInsertInAsync(object entity)
    {
        try
        {
            await Db.Ado.BeginTranAsync();
            string json = entity.ToString();
            var supplierIds = new List<Guid>();
            var poNoticeOrderIds = new List<Guid>();
            var dicts = JsonHelper.JsonToObj<List<Dictionary<string, object>>>(json);

            dicts.ForEach(x =>
            {
                if (x.ContainsKey("SupplierId"))
                    supplierIds.Add(Guid.Parse(x["SupplierId"].ToString()));

                if (x.ContainsKey("PoNoticeOrderId"))
                    poNoticeOrderIds.Add(Guid.Parse(x["PoNoticeOrderId"].ToString()));
            });
            var dt = Utility.GetSysDate();
            var userId = App.User.ID;
            supplierIds = supplierIds.Distinct().ToList();
            poNoticeOrderIds = poNoticeOrderIds.Distinct().ToList();

            for (int i = 0; i < supplierIds.Count; i++)
            {
                var supplierId = supplierIds[i];
                var orders = new List<PoInOrder>();
                var details = new List<PoInOrderDetail>();
                var orderId = StringHelper.Id1;
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
                    OrderSource = "NoticeOrder",
                    GroupId = Utility.GetGroupGuidId(),
                    CompanyId = Utility.GetCompanyGuidId()
                });
                int serialNumber = 1;
                for (int j = 0; j < dicts.Count; j++)
                {
                    var x = dicts[j];
                    if (x.ContainsKey("SupplierId") && Guid.Parse(x["SupplierId"].ToString()) == supplierId)
                    {
                        var poNoticeOrderId = Guid.Parse(x["PoNoticeOrderId"].ToString());
                        var sourceOrderDetailId = Guid.Parse(x["ID"].ToString());
                        decimal? inQTY = Convert.ToDecimal(x["InQTY"].ToString());

                        var poNoticeOrderDetail = await Db.Queryable<PoArrivalOrderDetail>().FirstAsync(x => x.ID == sourceOrderDetailId);
                        if (!poNoticeOrderDetail.IsNullOrEmpty())
                        {
                            if (poNoticeOrderDetail.NoticeQTY - (poNoticeOrderDetail.InQTY ?? 0) >= inQTY)
                                poNoticeOrderDetail.InQTY = (poNoticeOrderDetail.InQTY ?? 0) + inQTY;
                            else
                            {
                                inQTY = poNoticeOrderDetail.NoticeQTY - (poNoticeOrderDetail.InQTY ?? 0);
                                poNoticeOrderDetail.InQTY = poNoticeOrderDetail.NoticeQTY;
                            }
                            var result = await Db.Updateable<PoArrivalOrderDetail>()
                              .SetColumns(it => new PoArrivalOrderDetail()
                              {
                                  InQTY = poNoticeOrderDetail.InQTY,
                                  UpdateBy = userId,
                                  UpdateTime = dt
                              })
                              .Where(it => it.ID == sourceOrderDetailId)
                              .ExecuteCommandAsync();
                            details.Add(new PoInOrderDetail()
                            {
                                SerialNumber = serialNumber,
                                OrderSource = "NoticeOrder",
                                OrderId = orderId,
                                SourceOrderId = poNoticeOrderId,
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

            for (int i = 0; i < poNoticeOrderIds.Count; i++)
            {
                var id = poNoticeOrderIds[i];
                await UpdateOrderStatus(id);
                await IVChangeHelper.UpdataOrderDetailSerialNumber(Db, "PoInOrderDetail", id);
            }
            await Db.Ado.CommitTranAsync();

            return ServiceResult.OprateSuccess(ResponseText.EXECUTE_SUCCESS);
        }
        catch (Exception E)
        {
            await Db.Ado.RollbackTranAsync();

            return ServiceResult.OprateFailed(E.Message);
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
        await Db.Updateable<PoArrivalOrder>()
            .SetColumns(it => new PoArrivalOrder()
            {
                OrderStatus = DIC_PURCHASE_NOTICE_ORDER_STATUS.OrderComplete,
                UpdateBy = App.User.ID,
                UpdateTime = Utility.GetSysDate()
            })
            .Where(it =>
            ids.Contains(it.ID) &&
            it.OrderStatus != DIC_PURCHASE_NOTICE_ORDER_STATUS.Wait &&
            it.OrderStatus != DIC_PURCHASE_NOTICE_ORDER_STATUS.OrderComplete &&
            it.OrderStatus != DIC_PURCHASE_NOTICE_ORDER_STATUS.InComplete &&
            it.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit)
            .ExecuteCommandAsync();
        return ServiceResult.OprateSuccess(ResponseText.EXECUTE_SUCCESS);
    }
    #endregion

    #region 更新订单状态
    /// <summary>
    /// 更新订单状态
    /// </summary>
    /// <param name="orderId">采购单ID</param>
    /// <returns></returns>
    public async Task UpdateOrderStatus(Guid? orderId)
    {
        var orderStatus = DIC_PURCHASE_NOTICE_ORDER_STATUS.In;
        var dt = Utility.GetSysDate();
        var userId = App.User.ID;
        var isExist = await Db.Queryable<PoArrivalOrderDetail>()
             .Where(x => x.OrderId == orderId)
             .WhereIF(1 != 2, x => ((x.InQTY != null && x.NoticeQTY - x.InQTY > 0) || x.InQTY == null))
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
            .Where(it => it.ID == orderId)
            .ExecuteCommandAsync();
    }
    #endregion

}