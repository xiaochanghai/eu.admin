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
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 销售变更单明细 (服务)
/// </summary>
public class SdChangeOrderDetailServices : BaseServices<SdChangeOrderDetail, SdChangeOrderDetailDto, InsertSdChangeOrderDetailInput, EditSdChangeOrderDetailInput>, ISdChangeOrderDetailServices
{
    private const string ModuleCode = "SD_SALES_CHANGE_ORDER_DETAIL_MNG";
    private readonly IBaseRepository<SdChangeOrderDetail> _dal;
    public SdChangeOrderDetailServices(IBaseRepository<SdChangeOrderDetail> dal)
    {
        _dal = dal;
        BaseDal = dal;
    }

    #region 更新
    public override async Task<SdChangeOrderDetailDto> UpdateReturn(Guid Id, object entity)
    {
        var model = ConvertToEntity(entity);

        #region 检查是否存在相同值
        await CheckOnly(model, Id);
        #endregion

        var orderTax = await Db.Queryable<SdChangeOrder>()
            .Where(x => x.ID == model.OrderId)
            .Select(x => new { x.TaxType, x.TaxRate })
            .FirstAsync();
        (decimal? NoTaxAmount, decimal? TaxAmount, decimal? TaxIncludedAmount) = IVChangeHelper.UpdataTaxAmount(orderTax.TaxType, orderTax.TaxRate, model.Price, model.QTY);
        model.NoTaxAmount = NoTaxAmount;
        model.TaxAmount = TaxAmount;
        model.TaxIncludedAmount = TaxIncludedAmount;
        var lstColumns = new ModuleSqlColumn(ModuleCode).GetModuleTableEditableColumns();

        await Update(model, lstColumns);
        return Mapper.Map(model).ToANew<SdChangeOrderDetailDto>();
    }
    #endregion 
}
