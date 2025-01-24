/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdReturnOrder.cs
*
*功 能： N / A
* 类 名： SdReturnOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/8/28 11:57:12  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 退库单 (服务)
/// </summary>
public class SdReturnOrderServices : BaseServices<SdReturnOrder, SdReturnOrderDto, InsertSdReturnOrderInput, EditSdReturnOrderInput>, ISdReturnOrderServices
{
    private readonly IBaseRepository<SdReturnOrder> _dal;
    private readonly ISdReturnOrderDetailServices _sdReturnOrderDetailServices;
    public SdReturnOrderServices(IBaseRepository<SdReturnOrder> dal, ISdReturnOrderDetailServices sdReturnOrderDetailServices)
    {
        this._dal = dal;
        base.BaseDal = dal;
        _sdReturnOrderDetailServices = sdReturnOrderDetailServices;
    }

    #region 删除数据 
    /// <summary>
    /// 删除指定ID集合的数据(批量删除)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public override async Task<bool> Delete(Guid[] ids)
    {
        var entities = new List<SdReturnOrder>();
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

            var details = await Db.Queryable<SdReturnOrderDetail>().Where(x => x.OrderId == ids[i]).Select(x => x.ID).ToArrayAsync();
            if (details.Any())
                await _sdReturnOrderDetailServices.Delete(details);
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
    public override async Task<bool> BulkAudit(Guid[] ids) => await BulkAudit(ids, $"OrderStatus = '{DIC_SALES_RETURN_ORDER_STATUS.WaitReturn}'");
    #endregion

    #region 撤销数据
    /// <summary>
    /// 撤销指定ID集合的数据(批量撤销)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public override async Task<bool> BulkRevocation(Guid[] ids)
    {
        var entities = new List<SdReturnOrder>();
        foreach (var id in ids)
        {
            if (!await AnyAsync(id))
                continue;

            var entity = await Query(id);

            if (entity.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit && entity.OrderStatus == DIC_SALES_RETURN_ORDER_STATUS.WaitReturn)
            {
                entity.AuditStatus = DIC_SYSTEM_AUDIT_STATUS.Add;
                entities.Add(entity);
            }
        }
        await BaseDal.Update(entities, ["AuditStatus"], null, $"OrderStatus = '{DIC_SALES_RETURN_ORDER_STATUS.WaitReturn}'");
        return true;
    }
    #endregion

    #region 批量导入退库明细
    public async Task<ServiceResult> BulkInsertDetailAsync(List<SdReturnOrderDetail> entitys, Guid orderId)
    {
        try
        {
            await Db.Ado.BeginTranAsync();

            int serialNumber = 1;
            var inserts = new List<SdReturnOrderDetail>();
            var updates = new List<SdReturnOrderDetail>();
            for (int j = 0; j < entitys.Count; j++)
            {
                var entity = entitys[j];
                entity.SerialNumber = serialNumber;
                var sdOutDetail = await Db.Queryable<SdOutOrderDetail>().FirstAsync(x => x.ID == entity.OutOrderDetailId);
                if (!sdOutDetail.IsNullOrEmpty())
                {
                    if (sdOutDetail.OutQTY <= sdOutDetail.ReturnQTY)
                        continue;
                    if (sdOutDetail.OutQTY - (sdOutDetail.ReturnQTY ?? 0) >= entity.ReturnQTY)
                        sdOutDetail.ReturnQTY = (sdOutDetail.ReturnQTY ?? 0) + entity.ReturnQTY;
                    else
                    {
                        entity.ReturnQTY = sdOutDetail.OutQTY - (sdOutDetail.ReturnQTY ?? 0);
                        sdOutDetail.ReturnQTY = sdOutDetail.OutQTY;
                    }

                    var result = await Db.Updateable<SdOutOrderDetail>()
                      .SetColumns(it => new SdOutOrderDetail() { ReturnQTY = sdOutDetail.ReturnQTY }, true)
                      .Where(it => it.ID == entity.OutOrderDetailId)
                      .ExecuteCommandAsync();
                    entity.OrderId = orderId;

                    var update = await Db.Queryable<SdReturnOrderDetail>()
                        .Where(x =>
                        x.OrderId == orderId &&
                        x.MaterialId == entity.MaterialId &&
                        x.SalesOrderDetailId == entity.SalesOrderDetailId &&
                        x.OutOrderDetailId == entity.OutOrderDetailId)
                        .FirstAsync();
                    if (update.IsNotEmptyOrNull())
                    {
                        update.ReturnQTY += entity.ReturnQTY;
                        updates.Add(update);
                    }
                    else
                    {
                        serialNumber++;
                        entity.ID = Utility.GuidId;
                        inserts.Add(entity);
                    }
                }
            }

            if (inserts.Any())
                await Db.Insertable(inserts).ExecuteCommandAsync();

            if (updates.Any())
                await Db.Updateable(updates)
                    .UpdateColumns(it => new { it.ReturnQTY }, true)
                    .ExecuteCommandAsync();

            var outOrderIds = entitys.Select(x => x.OutOrderId).Distinct().ToList();
            for (int i = 0; i < outOrderIds.Count; i++)
                await _sdReturnOrderDetailServices.UpdateOutOrderStatus(outOrderIds[i]);

            await Db.Ado.CommitTranAsync();

            return Success(ResponseText.INSERT_SUCCESS);
        }
        catch (Exception E)
        {
            await Db.Ado.RollbackTranAsync();

            return Failed(E.Message);
        }
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
        await Db.Updateable<SdReturnOrder>()
            .SetColumns(it => new SdReturnOrder() { OrderStatus = DIC_SALES_RETURN_ORDER_STATUS.HasReturn }, true)
            .Where(it =>
            ids.Contains(it.ID) &&
            it.OrderStatus != DIC_SALES_RETURN_ORDER_STATUS.HasReturn &&
            it.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit)
            .ExecuteCommandAsync();
        return Success(ResponseText.EXECUTE_SUCCESS);
    }
    #endregion
}