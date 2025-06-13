/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdOrderDetail.cs
*
*功 能： N / A
* 类 名： SdOrderDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 14:50:06  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 销售订单明细 (服务)
/// </summary>
public class SdOrderDetailServices : BaseServices<SdOrderDetail, SdOrderDetailDto, InsertSdOrderDetailInput, EditSdOrderDetailInput>, ISdOrderDetailServices
{
    private readonly IBaseRepository<SdOrderDetail> _dal;
    public SdOrderDetailServices(IBaseRepository<SdOrderDetail> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    #region 新增
    public override async Task<Guid> Add(object entity)
    {
        var model = ConvertToEntity(entity);

        var dic = ConvertToDic(entity);
        var lstColumns = dic.Keys.Where(x => x != "ID" && x != "Id").ToList();

        #region 检查是否存在相同值
        CheckOnly(model);
        #endregion 

        var order = await Db.Queryable<SdOrder>().FirstAsync(x => x.ID == model.OrderId);
        model.SerialNumber = Utility.GenerateContinuousSequence("SdOrderDetail", "SerialNumber", "OrderId", model.OrderId.ToString());
        model.DeliveryDate = order.DeliveryDate;

        #region 税额计算 
        (decimal? NoTaxAmount, decimal? TaxAmount, decimal? TaxIncludedAmount) = IVChangeHelper.UpdataTaxAmount(order.TaxType, order.TaxRate, model.Price, model.QTY);
        model.NoTaxAmount = NoTaxAmount;
        model.TaxAmount = TaxAmount;
        model.TaxIncludedAmount = TaxIncludedAmount;
        #endregion

        return await BaseDal.Add(model, lstColumns);
    }

    /// <summary>
    /// 批量插入实体(速度快)
    /// </summary>
    /// <param name="listEntity">实体集合</param>
    /// <returns>影响行数</returns>
    public override async Task<List<Guid>> Add(List<InsertSdOrderDetailInput> listEntity)
    {
        Guid? orderId = listEntity[0].OrderId;
        var order = await Db.Queryable<SdOrder>().FirstAsync(x => x.ID == orderId);

        var inserts = new List<InsertSdOrderDetailInput>();
        var updates = new List<SdOrderDetail>();
        for (int i = 0; i < listEntity.Count; i++)
        {
            var detail = await base.QuerySingle(x => x.OrderId == orderId && x.MaterialId == listEntity[i].MaterialId);
            if (detail.IsNullOrEmpty())
            {
                (decimal? NoTaxAmount, decimal? TaxAmount, decimal? TaxIncludedAmount) = IVChangeHelper.UpdataTaxAmount(order.TaxType, order.TaxRate, listEntity[i].Price, listEntity[i].QTY);
                listEntity[i].NoTaxAmount = NoTaxAmount;
                listEntity[i].TaxAmount = TaxAmount;
                listEntity[i].TaxIncludedAmount = TaxIncludedAmount;

                listEntity[i].DeliveryDate = order.DeliveryDate;
                inserts.Add(listEntity[i]);
            }
            else
            {
                detail.QTY += listEntity[i].QTY;
                updates.Add(detail);
            }
        }
        var result = await base.Add(inserts);
        if (updates.Any())
        {
            await Db.Updateable(updates)
                .UpdateColumns(it => new { it.QTY }, true)
                .ExecuteCommandAsync();
            result.AddRange(updates.Select(x => x.ID));
        }
        await IVChangeHelper.UpdataOrderDetailSerialNumber(Db, "SdOrderDetail", orderId);

        return result;
    }
    #endregion

    #region 更新
    public override async Task<bool> Update(Guid Id, object entity)
    {
        var model = ConvertToEntity(entity);

        #region 检查是否存在相同值
        CheckOnly(model, Id);
        #endregion

        if (model.QTY <= 0)
            throw new Exception("数量必须大于0！");

        var order = await Db.Queryable<SdOrder>().FirstAsync(x => x.ID == model.OrderId);
        (decimal? NoTaxAmount, decimal? TaxAmount, decimal? TaxIncludedAmount) = IVChangeHelper.UpdataTaxAmount(order.TaxType, order.TaxRate, model.Price, model.QTY);
        model.NoTaxAmount = NoTaxAmount;
        model.TaxAmount = TaxAmount;
        model.TaxIncludedAmount = TaxIncludedAmount;

        var lstColumns = new ModuleSqlColumn("SD_SALES_ORDER_DETAIL_MNG").GetModuleTableEditableColumns();

        lstColumns.Add("NoTaxAmount");
        lstColumns.Add("TaxAmount");
        lstColumns.Add("TaxIncludedAmount");
        return await Update(model, lstColumns);
    }

    public override async Task<SdOrderDetailDto> UpdateReturn(Guid Id, object entity)
    {
        var model = ConvertToEntity(entity);

        #region 检查是否存在相同值
        CheckOnly(model, Id);
        #endregion

        if (model.QTY <= 0)
            throw new Exception("数量必须大于0！");

        var order = await Db.Queryable<SdOrder>().FirstAsync(x => x.ID == model.OrderId);
        (decimal? NoTaxAmount, decimal? TaxAmount, decimal? TaxIncludedAmount) = IVChangeHelper.UpdataTaxAmount(order.TaxType, order.TaxRate, model.Price, model.QTY);
        model.NoTaxAmount = NoTaxAmount;
        model.TaxAmount = TaxAmount;
        model.TaxIncludedAmount = TaxIncludedAmount;

        var lstColumns = new ModuleSqlColumn("SD_SALES_ORDER_DETAIL_MNG").GetModuleTableEditableColumns();

        lstColumns.Add("NoTaxAmount");
        lstColumns.Add("TaxAmount");
        lstColumns.Add("TaxIncludedAmount");
        await Update(model, lstColumns);
        return Mapper.Map(model).ToANew<SdOrderDetailDto>();
    }
    #endregion

    #region 删除
    public override async Task<bool> Delete(Guid[] ids)
    {
        var result = await base.Delete(ids);
        var orderDetail = await Db.Queryable<SdOrderDetail>().FirstAsync(x => x.ID == ids[0]);
        if (orderDetail != null)
            await IVChangeHelper.UpdataOrderDetailSerialNumber(Db, "SdOrderDetail", orderDetail.OrderId);
        return result;
    }
    #endregion 
}