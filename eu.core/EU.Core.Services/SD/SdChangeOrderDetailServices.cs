/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdChangeOrderDetail.cs
*
*功 能： N / A
* 类 名： SdChangeOrderDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/8/16 15:17:02  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 销售变更单明细 (服务)
/// </summary>
public class SdChangeOrderDetailServices : BaseServices<SdChangeOrderDetail, SdChangeOrderDetailDto, InsertSdChangeOrderDetailInput, EditSdChangeOrderDetailInput>, ISdChangeOrderDetailServices
{
    private readonly IBaseRepository<SdChangeOrderDetail> _dal;
    public SdChangeOrderDetailServices(IBaseRepository<SdChangeOrderDetail> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    #region 更新
    public override async Task<SdChangeOrderDetailDto> UpdateReturn(Guid Id, object entity)
    {
        var model = ConvertToEntity(entity);

        #region 检查是否存在相同值
        CheckOnly(model, Id);
        #endregion

        var order = await Db.Queryable<SdChangeOrder>().FirstAsync(x => x.ID == model.OrderId);
        (decimal? NoTaxAmount, decimal? TaxAmount, decimal? TaxIncludedAmount) = IVChangeHelper.UpdataTaxAmount(order.TaxType, order.TaxRate, model.Price, model.QTY);
        model.NoTaxAmount = NoTaxAmount;
        model.TaxAmount = TaxAmount;
        model.TaxIncludedAmount = TaxIncludedAmount;
        var dic = ConvertToDic(entity);
        var lstColumns = new ModuleSqlColumn("SD_SALES_CHANGE_ORDER_DETAIL_MNG").GetModuleTableEditableColumns();

        await Update(model, lstColumns, null, $"ID='{Id}'");
        return Mapper.Map(model).ToANew<SdChangeOrderDetailDto>();
    }
    #endregion 
}