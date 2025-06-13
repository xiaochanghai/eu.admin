/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdGoodsLocation.cs
*
*功 能： N / A
* 类 名： BdGoodsLocation
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/25 17:52:09  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 货位 (服务)
/// </summary>
public class BdGoodsLocationServices : BaseServices<BdGoodsLocation, BdGoodsLocationDto, InsertBdGoodsLocationInput, EditBdGoodsLocationInput>, IBdGoodsLocationServices
{
    private readonly IBaseRepository<BdGoodsLocation> _dal;
    public BdGoodsLocationServices(IBaseRepository<BdGoodsLocation> dal, DataContext context)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    #region 新增
    public override async Task<Guid> Add(object entity)
    {
        var model = ConvertToEntity(entity);
        if (await Db.Queryable<BdStock>().AnyAsync(x => x.ID == model.StockId && x.IsVirtual == true))
        {
            if (await Db.Queryable<BdGoodsLocation>().AnyAsync(x => x.StockId == model.StockId && x.IsActive == true && x.IsDeleted == false))
                throw new("虚拟仓只能新建一个货位!");
        }
        return await base.Add(entity);
    }
    #endregion
}