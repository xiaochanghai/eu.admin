/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IvCheckDetail.cs
*
* 功 能： N / A
* 类 名： IvCheckDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2024/12/18 15:50:12  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 库存盘点单明细 (服务)
/// </summary>
public class IvCheckDetailServices : BaseServices<IvCheckDetail, IvCheckDetailDto, InsertIvCheckDetailInput, EditIvCheckDetailInput>, IIvCheckDetailServices
{
    private readonly ICommonServices _commonServices;
    public IvCheckDetailServices(IBaseRepository<IvCheckDetail> dal, IBdMaterialServices materialServices, ICommonServices commonServices)
    {
        BaseDal = dal;
        _commonServices = commonServices;
    }

    #region 更新
    public override async Task<IvCheckDetailDto> UpdateReturn(Guid Id, object entity1)
    {
        var dict = JsonHelper.JsonToObj<Dictionary<string, object>>(entity1.ToString());
        var orderId = dict["masterId"].ObjToGuid();
        var entity = await Query(Id);
        var model = ConvertToEntity(entity1);

        if (entity is null)
        {
            if (await Db.Queryable<IvCheckDetail>()
                .WhereIF(model.BatchNo.IsNotEmptyOrNull(), x => x.BatchNo == model.BatchNo)
                .WhereIF(model.BatchNo.IsNullOrEmpty(), x => string.IsNullOrWhiteSpace(x.BatchNo))
                .Where(x =>
                x.OrderId == orderId &&
                x.MaterialId == model.MaterialId &&
                x.StockId == model.StockId &&
                x.GoodsLocationId == model.GoodsLocationId).AnyAsync())
                throw new Exception("该单子已存在相同的物料，请在原数据上修改！");
            await base.Add(new InsertIvCheckDetailInput() { OrderId = orderId }, Id);
        }

        var lstColumns = new ModuleSqlColumn("IV_CHECK_DETAIL_MNG").GetModuleTableEditableColumns();
        await Update(model, lstColumns, ["OrderId"]);

        var model1 = Mapper.Map(model).ToANew<IvCheckDetailDto>();

        await IVChangeHelper.UpdataOrderDetailSerialNumber(Db, "IvCheckDetail", orderId);

        var filter = new QueryFilter()
        {
            Conditions = $"A.ID='{Id}'",
            PageIndex = 1,
            PageSize = 100
        };

        var result = await _commonServices.QueryByFilter(filter, "IV_CHECK_DETAIL_MNG");
        if (result.data.Rows.Count > 0)
        {
            model1.MaterialName = result.data.Rows[0]["MaterialName"].ToString();
            model1.Specifications = result.data.Rows[0]["Specifications"].ToString();
            model1.UnitName = result.data.Rows[0]["UnitName"].ToString();
            model1.StockName = result.data.Rows[0]["StockName"].ToString();
            model1.GoodsLocationName = result.data.Rows[0]["GoodsLocationName"].ToString();
            model1.SerialNumber = result.data.Rows[0]["SerialNumber"].ObjToInt();
            model1.InitQTY = result.data.Rows[0]["InitQTY"].ObjToDecimal();
            model1.SurplusQTY = result.data.Rows[0]["SurplusQTY"].ObjToDecimal();
            model1.ShortageQTY = result.data.Rows[0]["ShortageQTY"].ObjToDecimal();
        }
        return model1;
    }
    #endregion
}