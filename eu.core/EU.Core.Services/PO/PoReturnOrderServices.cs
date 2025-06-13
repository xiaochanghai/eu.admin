/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoReturnOrder.cs
*
* 功 能： N / A
* 类 名： PoReturnOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2024/10/8 14:03:46  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 采购退货单 (服务)
/// </summary>
public class PoReturnOrderServices : BaseServices<PoReturnOrder, PoReturnOrderDto, InsertPoReturnOrderInput, EditPoReturnOrderInput>, IPoReturnOrderServices
{
    private readonly IBaseRepository<PoReturnOrder> _dal;
    private readonly IPoReturnOrderDetailServices _poReturnOrderDetailServices;
    public PoReturnOrderServices(IBaseRepository<PoReturnOrder> dal,
        IPoReturnOrderDetailServices poReturnOrderDetailService)
    {
        this._dal = dal;
        base.BaseDal = dal;
        _poReturnOrderDetailServices = poReturnOrderDetailService;
    }

    #region 更新
    public override async Task<bool> Update(Guid Id, object entity)
    {
        var model = ConvertToEntity(entity);
        var entity1 = await base.QueryDto(Id);
        var result = await base.Update(Id, entity);

        if (entity1.StockId != model.StockId || entity1.GoodsLocationId != model.GoodsLocationId)
            await Db.Updateable<PoReturnOrderDetail>()
                .SetColumns(it => new PoReturnOrderDetail()
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
        var entities = new List<PoReturnOrder>();
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

            var detailIds = await Db.Queryable<PoReturnOrderDetail>().Where(x => x.OrderId == id).Select(x => x.ID).ToArrayAsync();
            await _poReturnOrderDetailServices.Delete(detailIds);
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
        var entities = new List<PoReturnOrder>();
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

    #region 订单完结
    /// <summary>
    /// 订单完结指定ID集合的数据(订单完结)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public async Task<ServiceResult> BulkOrderComplete(Guid[] ids)
    {
        await Db.Updateable<PoReturnOrder>()
            .SetColumns(it => new PoReturnOrder()
            {
                OrderStatus = DIC_PURCHASE_RETURN_ORDER_STATUS.OrderComplete
            }, true)
            .Where(it =>
            ids.Contains(it.ID) &&
            it.OrderStatus != DIC_PURCHASE_RETURN_ORDER_STATUS.CompleteReturn &&
            it.OrderStatus != DIC_PURCHASE_RETURN_ORDER_STATUS.OrderComplete &&
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
        await Db.Updateable<PoReturnOrder>()
            .SetColumns(it => new PoReturnOrder()
            {
                OrderStatus = DIC_PURCHASE_RETURN_ORDER_STATUS.CompleteReturn
            }, true)
            .Where(it =>
            ids.Contains(it.ID) &&
            it.OrderStatus != DIC_PURCHASE_RETURN_ORDER_STATUS.CompleteReturn &&
            it.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit)
            .ExecuteCommandAsync();
        return Success(ResponseText.EXECUTE_SUCCESS);
    }
    #endregion
}