/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IvOutDetail.cs
*
* 功 能： N / A
* 类 名： IvOutDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2024/12/18 15:49:00  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 库存出库单明细 (服务)
/// </summary>
public class IvOutDetailServices : BaseServices<IvOutDetail, IvOutDetailDto, InsertIvOutDetailInput, EditIvOutDetailInput>, IIvOutDetailServices
{
    private readonly IBaseRepository<IvOutDetail> _dal;
    private readonly IBdMaterialServices _materialServices;
    public IvOutDetailServices(IBaseRepository<IvOutDetail> dal, IBdMaterialServices materialServices)
    {
        this._dal = dal;
        base.BaseDal = dal;
        _materialServices = materialServices;
    }

    #region 更新
    public override async Task<IvOutDetailDto> UpdateReturn(Guid Id, object entity1)
    {
        try
        {
            var dict = JsonHelper.JsonToObj<Dictionary<string, object>>(entity1.ToString());
            var orderId = dict["masterId"].ObjToGuid();
            var entity = await Query(Id);
            var model = ConvertToEntity(entity1);

            if (entity is null)
                await base.Add(new InsertIvOutDetailInput() { OrderId = orderId }, Id);

            var inOrder = await Db.Queryable<IvOut>().Where(x => x.ID == orderId).FirstAsync();
            if (inOrder != null)
            {
                model.StockId = inOrder.StockId;
                model.GoodsLocationId = inOrder.GoodsLocationId;
            }

            var lstColumns = new ModuleSqlColumn("IV_OUT_DETAIL_MNG").GetModuleTableEditableColumns();

            await Update(model, lstColumns, ["OrderId"], $"ID='{Id}'");

            var model1 = Mapper.Map(model).ToANew<IvOutDetailDto>();

            var material = await _materialServices.QueryDto(model.MaterialId);
            model1.MaterialName = material.MaterialName + "（" + material.MaterialNo + "）";
            model1.Specifications = material.Specifications;
            model1.UnitName = material.UnitName;
            if (model.StockId != null)
                model1.StockName = await Db.Ado.GetStringAsync($"SELECT StockNames + '（' + StockNo + '）' FROM BdStock WHERE ID='{model.StockId}'");
            if (model.GoodsLocationId != null)
                model1.GoodsLocationName = await Db.Ado.GetStringAsync($"SELECT GoodsLocationName1 FROM BdGoodsLocation_V WHERE ID='{model.GoodsLocationId}'");

            if (model.QTY > 0 && model.Price > 0)
                model1.Amount = model.Price * model.QTY;

            await IVChangeHelper.UpdataOrderDetailSerialNumber(Db, "IvOutDetail", orderId);

            return model1;
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region 删除
    public override async Task<bool> Delete(Guid[] ids)
    {
        var result = await base.Delete(ids);
        var orderDetail = await Db.Queryable<IvOutDetail>().FirstAsync(x => x.ID == ids[0]);
        if (orderDetail != null)
            await IVChangeHelper.UpdataOrderDetailSerialNumber(Db, "IvOutDetail", orderDetail.OrderId);
        return result;
    }
    #endregion 
}