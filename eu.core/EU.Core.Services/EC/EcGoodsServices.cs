/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* EcGoods.cs
*
* 功 能： N / A
* 类 名： EcGoods
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/12/21 21:24:01  SahHsiao   初版
*
* Copyright(c) 2025 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

using MathNet.Numerics.Distributions;

namespace EU.Core.Services;

/// <summary>
/// 商品管理 (服务)
/// </summary>
public class EcGoodsServices : BaseServices<EcGoods, EcGoodsDto, InsertEcGoodsInput, EditEcGoodsInput>, IEcGoodsServices
{
    public EcGoodsServices(IBaseRepository<EcGoods> dal)
    {
        BaseDal = dal;
    }


    /// <summary>商品详情：返回配件并原子累计浏览量。</summary>
    public override async Task<EcGoodsDto> QueryById(object objId)
    {
        if (!Guid.TryParse(objId?.ToString(), out var id) || id == Guid.Empty)
            return null;

        var goods = await Query(id);
        if (goods == null || goods.IsDeleted)
            return null;

        var entity = await base.QueryById(objId);


        entity.Accessories = await Db.Queryable<FileAttachment>()
            .Where(x => x.MasterId == id && x.ImageType == "Plugin" && !x.IsDeleted)
            .OrderBy(x => x.CreatedTime)
            .OrderBy(x => x.ID)
            .Select(x => new EcGoodsAccessoryDto
            {
                ID = x.ID,
                FileName = x.FileName,
                OriginalFileName = x.OriginalFileName
            })
            .ToListAsync();


        await Db.Updateable<EcGoods>()
            .SetColumns(it => it.ViewCount == SqlFunc.IsNull(it.ViewCount, 0) + 1)
            .Where(it => it.ID == id && !it.IsDeleted)
            .ExecuteCommandAsync();

        entity.ViewCount = await Db.Queryable<EcGoods>()
            .Where(x => x.ID == id)
            .Select(x => x.ViewCount)
            .FirstAsync();
        return entity;
    }
}
