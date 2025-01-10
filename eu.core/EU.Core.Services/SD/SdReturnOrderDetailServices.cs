/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdReturnOrderDetail.cs
*
*功 能： N / A
* 类 名： SdReturnOrderDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/8/29 13:06:20  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Services;

/// <summary>
/// 退货单明细 (服务)
/// </summary>
public class SdReturnOrderDetailServices : BaseServices<SdReturnOrderDetail, SdReturnOrderDetailDto, InsertSdReturnOrderDetailInput, EditSdReturnOrderDetailInput>, ISdReturnOrderDetailServices
{
    private readonly IBaseRepository<SdReturnOrderDetail> _dal;
    public SdReturnOrderDetailServices(IBaseRepository<SdReturnOrderDetail> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    #region 更新
    public override async Task<SdReturnOrderDetailDto> UpdateReturn(Guid Id, object entity1)
    {
        try
        {
            await Db.Ado.BeginTranAsync();
            var model = ConvertToEntity(entity1);

            #region 检查是否存在相同值
            CheckOnly(model, Id);
            #endregion

            var entity = await QueryDto(Id);
            var orderDetail = await Db.Queryable<SdOutOrderDetail>().Where(x => x.ID == entity.OutOrderDetailId).FirstAsync();

            var qty = orderDetail.OutQTY - (orderDetail.ReturnQTY ?? 0);

            if (model.ReturnQTY > (qty + entity.ReturnQTY))
                throw new Exception($"退库数量最大为：{Utility.RemoveZero(qty + entity.ReturnQTY)}");

            orderDetail.ReturnQTY = orderDetail.ReturnQTY - entity.ReturnQTY + model.ReturnQTY;

            var lstColumns = new ModuleSqlColumn("SD_RETURN_ORDER_DETAIL_MNG").GetModuleTableEditableColumns();

            await Update(model, lstColumns, null, $"ID='{Id}'");

            var model1 = Mapper.Map(model).ToANew<SdReturnOrderDetailDto>();

            await Db.Updateable<SdOutOrderDetail>()
                .SetColumns(it => new SdOutOrderDetail()
                {
                    ReturnQTY = orderDetail.ReturnQTY
                }, true)
                .Where(it => it.ID == entity.OutOrderDetailId)
                .ExecuteCommandAsync();

            #region 变更出库单状态  
            await UpdateOutOrderStatus(entity.OutOrderId);
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

            Guid? outOrderId = null;
            Guid? retrunOrderId = null;

            var entities = new List<SdReturnOrderDetail>();
            foreach (var id in ids)
            {
                if (!await AnyAsync(id))
                    continue;

                var entity = await Query(id);
                if (entity.IsDeleted)
                    continue;

                if (await Db.Queryable<SdReturnOrder>().AnyAsync(x => x.AuditStatus != DIC_SYSTEM_AUDIT_STATUS.Add && x.ID == entity.OrderId))
                    throw new Exception($"该订单已审核通过，不可删除！");

                #region 出货数量回退
                outOrderId = entity.OutOrderId;
                retrunOrderId = entity.OrderId;

                await Db.Updateable<SdOutOrderDetail>()
                    .SetColumns(it => new SdOutOrderDetail()
                    {
                        ReturnQTY = it.ReturnQTY - entity.ReturnQTY
                    }, true)
                    .Where(it => it.ID == entity.OutOrderDetailId)
                    .ExecuteCommandAsync();
                #endregion

                entity.IsDeleted = true;
                entities.Add(entity);
            }
            await BaseDal.Update(entities, ["IsDeleted"]);

            #region 变更出库单状态 
            if (outOrderId.IsNotEmptyOrNull())
                await UpdateOutOrderStatus(outOrderId);
            #endregion

            #region 批量更新排序号
            if (retrunOrderId.IsNotEmptyOrNull())
                await IVChangeHelper.UpdataOrderDetailSerialNumber(Db, "SdReturnOrderDetail", retrunOrderId);
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

    #region 变更出库单状态
    /// <summary>
    /// 变更出库单状态
    /// </summary>
    /// <param name="orderId">订单ID</param> 
    /// <returns></returns>
    public async Task UpdateOutOrderStatus(Guid? orderId)
    {
        var orderStatus = DIC_SALES_OUT_ORDER_STATUS.OutComplete;

        var isExist = await Db.Queryable<SdOutOrderDetail>()
            .Where(x => x.OrderId == orderId && x.ReturnQTY != null && x.ReturnQTY > 0)
            .AnyAsync();

        if (isExist)
            orderStatus = DIC_SALES_OUT_ORDER_STATUS.InReturn;

        isExist = await Db.Queryable<SdOutOrderDetail>()
               .Where(x => x.OrderId == orderId)
               .Where(x => ((x.ReturnQTY != null && x.OutQTY - x.ReturnQTY > 0) || x.ReturnQTY == null))
               .AnyAsync();
        if (!isExist)
            orderStatus = DIC_SALES_OUT_ORDER_STATUS.ReturnComplete;

        await Db.Updateable<SdOutOrder>()
            .SetColumns(it => new SdOutOrder()
            {
                OrderStatus = orderStatus
            }, true)
            .Where(it => it.ID == orderId && it.OrderStatus != orderStatus)
            .ExecuteCommandAsync();
    }
    #endregion
}