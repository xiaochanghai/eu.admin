/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdChangeOrder.cs
*
*功 能： N / A
* 类 名： SdChangeOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/8/16 15:05:57  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Services;

/// <summary>
/// 销售变更单 (服务)
/// </summary>
public class SdChangeOrderServices : BaseServices<SdChangeOrder, SdChangeOrderDto, InsertSdChangeOrderInput, EditSdChangeOrderInput>, ISdChangeOrderServices
{
    private readonly IBaseRepository<SdChangeOrder> _dal;
    public SdChangeOrderServices(IBaseRepository<SdChangeOrder> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    #region 审核数据
    /// <summary>
    /// 审核指定ID集合的数据(批量审核)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public override async Task<bool> BulkAudit(Guid[] ids)
    {
        var entities = new List<SdChangeOrder>();
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
        await BaseDal.Update(entities, ["AuditStatus"], null, $"OrderStatus = '{DIC_SALES_CHANGE_ORDER_STATUS.Invalid}'");
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
        var entities = new List<SdChangeOrder>();
        foreach (var id in ids)
        {
            if (!await AnyAsync(id))
                continue;

            var entity = await Query(id);

            if (entity.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit && entity.OrderStatus == DIC_SALES_CHANGE_ORDER_STATUS.Invalid)
            {
                entity.AuditStatus = DIC_SYSTEM_AUDIT_STATUS.Add;
                entities.Add(entity);
            }
        }
        await BaseDal.Update(entities, ["AuditStatus"], null, $"OrderStatus = '{DIC_SALES_CHANGE_ORDER_STATUS.Invalid}'");
        return true;
    }
    #endregion

    #region 更新
    public override async Task<bool> Update(Guid Id, object entity)
    { 
        var model = ConvertToEntity(entity);

        var dic = ConvertToDic(entity);
        var lstColumns = dic.Keys.Where(x => x != "ID" && x != "Id").ToList();

        lstColumns.Add("UpdateBy");
        lstColumns.Add("UpdateTime");

        var order = await QueryDto(Id);
        var result = await Update(model, lstColumns, new List<string> { "OrderNo", "CustomerId" }, $"ID='{Id}'");

        #region 批量更新銷售單明細稅額、含稅交割、未稅金額
        if (order.TaxRate != model.TaxRate || order.TaxType != model.TaxType)
        {
            var details = await Db.Queryable<SdChangeOrderDetail>().Where(x => x.OrderId == Id).ToListAsync();

            details.ForEach(x =>
            {
                (decimal? NoTaxAmount, decimal? TaxAmount, decimal? TaxIncludedAmount) = IVChangeHelper.UpdataTaxAmount(order.TaxType, order.TaxRate, x.Price, x.QTY);
                x.NoTaxAmount = NoTaxAmount;
                x.TaxAmount = TaxAmount;
                x.TaxIncludedAmount = TaxIncludedAmount;
            });
            await Db.Updateable(details).UpdateColumns(it => new { it.NoTaxAmount, it.TaxAmount, it.TaxIncludedAmount }).ExecuteCommandAsync();
        }
        #endregion

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
        var entities = new List<SdChangeOrder>();
        foreach (var id in ids)
        {
            if (!await AnyAsync(id))
                continue;

            var entity = await Query(id);

            if (entity.OrderStatus == DIC_SALES_CHANGE_ORDER_STATUS.Valid)
                throw new Exception($"订单【{entity.OrderNo}】已生效，不可删除！");

            var ent = entity as BasePoco;
            ent.IsDeleted = true;
            entities.Add(entity);
        }
        return await BaseDal.Update(entities, ["IsDeleted"]);
    }
    #endregion

    #region 执行变更
    /// <summary>
    /// 执行变更 指定ID集合的数据
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public async Task<ServiceResult> BulkOrderChange(Guid[] ids)
    {
        try
        {
            await Db.Ado.BeginTranAsync();

            for (int i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                var changeOrder = await BaseDal.QueryById(id);

                if (changeOrder.OrderStatus == DIC_SALES_CHANGE_ORDER_STATUS.Valid)
                    continue;
                 
                var order = await Db.Queryable<SdOrder>().FirstAsync(x => x.ID == changeOrder.OrderId);

                if (order.SalesOrderStatus == DIC_SALES_ORDER_STATUS.ShipComplete || order.SalesOrderStatus == DIC_SALES_ORDER_STATUS.OutComplete || order.SalesOrderStatus == DIC_SALES_ORDER_STATUS.OrderComplete)
                {
                    var text = order.SalesOrderStatus switch
                    {
                        DIC_SALES_ORDER_STATUS.ShipComplete => "出货完成",
                        DIC_SALES_ORDER_STATUS.OutComplete => "出库完成",
                        DIC_SALES_ORDER_STATUS.OrderComplete => "订单完结",
                        _ => ""
                    }; ;
                    throw new Exception($"销售单【{order.OrderNo}】已【{text}】，不可执行变更！");
                }
                order = Mapper.Map(changeOrder).ToANew<SdOrder>();

                var changeOrderDetails = await Db.Queryable<SdChangeOrderDetail>().Where(x => x.OrderId == id).ToListAsync();

                await Db.Updateable(order)
                     .IgnoreColumns(x => new { x.CreatedBy, x.CreatedTime, x.ID, x.CustomerId, x.OrderNo, x.AuditStatus, x.SalesOrderStatus, x.IsActive, x.IsDeleted })
                     .Where(x => x.ID == changeOrder.OrderId).ExecuteCommandAsync();

                for (int j = 0; j < changeOrderDetails.Count; j++)
                {
                    var changeOrderDetail = changeOrderDetails[j];

                    var dt = new Dictionary<string, object>
                    {
                        { "ID", changeOrderDetail.OrderDetailId },
                        { "Price", changeOrderDetail.Price },
                        { "DeliveryDate", changeOrderDetail.DeliveryDate },
                        { "Remark", changeOrderDetail.Remark },
                        { "NoTaxAmount", changeOrderDetail.NoTaxAmount },
                        { "TaxAmount", changeOrderDetail.TaxAmount },
                        { "TaxIncludedAmount", changeOrderDetail.TaxIncludedAmount }
                    };
                    await Db.Updateable(dt).AS("SdOrderDetail").WhereColumns("ID").ExecuteCommandAsync();
                }

                await Db.Updateable<SdChangeOrder>()
                .SetColumns(it => new SdChangeOrder()
                {
                    OrderStatus = DIC_SALES_CHANGE_ORDER_STATUS.Valid,
                    UpdateBy = App.User.ID,
                    UpdateTime = Utility.GetSysDate()
                })
                .Where(it => it.ID == id &&
                it.OrderStatus != DIC_SALES_CHANGE_ORDER_STATUS.Valid &&
                it.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit)
                .ExecuteCommandAsync();
            }


            await Db.Ado.CommitTranAsync();
            return ServiceResult.OprateSuccess(ResponseText.EXECUTE_SUCCESS);
        }
        catch (Exception)
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }

    }
    #endregion
}