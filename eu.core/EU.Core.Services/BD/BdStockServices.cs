/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdStock.cs
*
*功 能： N / A
* 类 名： BdStock
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/25 18:13:04  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 仓库 (服务)
/// </summary>
public class BdStockServices : BaseServices<BdStock, BdStockDto, InsertBdStockInput, EditBdStockInput>, IBdStockServices
{
    private readonly IBaseRepository<BdStock> _dal;
    public BdStockServices(IBaseRepository<BdStock> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    #region 新增
    public override async Task<Guid> Add(object entity)
    {
        var model = ConvertToEntity(entity);

        var reult = await base.Add(entity);

        var di = new DbInsert("BdGoodsLocation");
        di.Values("LocationNo", model.StockNo + "001");
        di.Values("StockId", reult);
        di.Values("LocationNames", "默认仓");
        await Db.Ado.ExecuteCommandAsync(di.GetSql());

        return reult;
    }
    #endregion

    #region 更新
    public override async Task<bool> Update(Guid Id, object entity)
    {
        var model = ConvertToEntity(entity);
        if (model.IsVirtual == true)
            if (await Db.Queryable<BdGoodsLocation>().AnyAsync(x => x.StockId == model.ID))
                throw new("该仓库下存在多个货位，不可变更为虚拟仓!");
        return await base.Update(Id, entity);
    }
    #endregion
}