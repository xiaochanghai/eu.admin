/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoRequestionDetail.cs
*
*功 能： N / A
* 类 名： PoRequestionDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/9/4 16:16:22  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 请购单明细 (服务)
/// </summary>
public class PoRequestionDetailServices : BaseServices<PoRequestionDetail, PoRequestionDetailDto, InsertPoRequestionDetailInput, EditPoRequestionDetailInput>, IPoRequestionDetailServices
{
    private readonly IBaseRepository<PoRequestionDetail> _dal;
    public PoRequestionDetailServices(IBaseRepository<PoRequestionDetail> dal)
    {
        _dal = dal;
        BaseDal = dal;
    }

    #region 新增 

    /// <summary>
    /// 批量插入实体(速度快)
    /// </summary>
    /// <param name="listEntity">实体集合</param>
    /// <returns>影响行数</returns>
    public override async Task<List<Guid>> Add(List<InsertPoRequestionDetailInput> listEntity)
    {
        if (listEntity == null || listEntity.Count == 0)
            return new List<Guid>();

        var orderId = listEntity[0].OrderId;
        if (orderId.IsNullOrEmpty())
            return new List<Guid>();

        var inserts = new List<InsertPoRequestionDetailInput>();
        var updates = new List<PoRequestionDetail>();
        var updateIds = new HashSet<Guid>();

        var order = await Db.Queryable<PoRequestion>().FirstAsync(x => x.ID == orderId);
        var materialIds = listEntity.Select(x => x.MaterialId).Distinct().ToList();
        var existingDetails = await Db.Queryable<PoRequestionDetail>()
            .Where(x => x.OrderId == orderId && materialIds.Contains(x.MaterialId))
            .ToListAsync();
        var detailMap = existingDetails.ToDictionary(x => x.MaterialId, x => x);

        for (int i = 0; i < listEntity.Count; i++)
        {
            if (!detailMap.TryGetValue(listEntity[i].MaterialId, out var detail) || detail.IsNullOrEmpty())
            {
                listEntity[i].RequestionDate = order.RequestionDate;
                inserts.Add(listEntity[i]);
            }
            else
            {
                detail.QTY += listEntity[i].QTY;
                if (updateIds.Add(detail.ID))
                    updates.Add(detail);
            }
        }
        var result = await base.Add(inserts);

        if (updates.Any())
        {
            await Db.Updateable(updates)
                .UpdateColumns(it => new { it.QTY },true)
                .ExecuteCommandAsync();
            result.AddRange(updates.Select(x => x.ID));
        }

        await IVChangeHelper.UpdataOrderDetailSerialNumber(Db, "PoRequestionDetail", orderId);
        return result;
    }
    #endregion 
}
