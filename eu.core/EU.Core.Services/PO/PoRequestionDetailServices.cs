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
*│　版权所有：苏州一优信息技术有限公司                                │
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
        this._dal = dal;
        base.BaseDal = dal;
    }

    #region 新增 

    /// <summary>
    /// 批量插入实体(速度快)
    /// </summary>
    /// <param name="listEntity">实体集合</param>
    /// <returns>影响行数</returns>
    public override async Task<List<Guid>> Add(List<InsertPoRequestionDetailInput> listEntity)
    {
        var orderId = listEntity[0].OrderId;

        var inserts = new List<InsertPoRequestionDetailInput>();
        var updates = new List<PoRequestionDetail>();

        var order = await Db.Queryable<PoRequestion>().FirstAsync(x => x.ID == orderId);
        for (int i = 0; i < listEntity.Count; i++)
        {
            var detail = await base.QuerySingle(x => x.OrderId == orderId && x.MaterialId == listEntity[i].MaterialId);
            if (detail.IsNullOrEmpty())
            {
                listEntity[i].RequestionDate = order.RequestionDate;
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
                .UpdateColumns(it => new { it.QTY },true)
                .ExecuteCommandAsync();
            result.AddRange(updates.Select(x => x.ID));
        }

        await IVChangeHelper.UpdataOrderDetailSerialNumber(Db, "PoRequestionDetail", orderId);
        return result;
    }
    #endregion 
}