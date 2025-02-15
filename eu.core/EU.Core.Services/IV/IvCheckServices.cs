/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IvCheck.cs
*
* 功 能： N / A
* 类 名： IvCheck
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2024/12/18 15:50:20  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

using MySqlX.XDevAPI.Common;
using static EU.Core.Common.Helper.IVChangeHelper;

namespace EU.Core.Services;

/// <summary>
/// 库存盘点单 (服务)
/// </summary>
public class IvCheckServices : BaseServices<IvCheck, IvCheckDto, InsertIvCheckInput, EditIvCheckInput>, IIvCheckServices
{
    public IvCheckServices(IBaseRepository<IvCheck> dal)
    {
        BaseDal = dal;
    }

    #region 新增
    public override async Task<Guid> Add(object entity)
    {
        var model = ConvertToEntity(entity);
        var result = await base.Add(entity);

        var inventorys = await Db.Queryable<BdMaterialInventory>().Where(x => x.StockId == model.StockId && x.GoodsLocationId == model.GoodsLocationId).ToListAsync();
        if (inventorys.Any())
        {
            var details = inventorys.Select((x, i) => new IvCheckDetail()
            {
                MaterialId = x.MaterialId,
                StockId = x.StockId,
                GoodsLocationId = x.GoodsLocationId,
                BatchNo = x.BatchNo,
                QTY = x.QTY,
                SerialNumber = i++,
                OrderId = result
            }).ToList();
            await Db.Insertable(details).ExecuteCommandAsync();
        }
        return result;
    }
    #endregion
    #region 更新
    public override async Task<bool> Update(Guid Id, object entity)
    {
        var data = await base.QueryById(Id);
        var model = ConvertToEntity(entity);

        if (data.StockId != model.StockId || data.GoodsLocationId != model.GoodsLocationId)
        {
            await Db.Updateable<IvCheckDetail>()
                .SetColumns(it => new IvCheckDetail() { IsDeleted = true }, true)
                .Where(it => it.OrderId == Id)
                .ExecuteCommandAsync();

            var inventorys = await Db.Queryable<BdMaterialInventory>().Where(x => x.StockId == model.StockId && x.GoodsLocationId == model.GoodsLocationId).ToListAsync();
            if (inventorys.Any())
            {
                var details = inventorys
                    .OrderBy(x => x.MaterialId)
                    .Select((x, i) => new IvCheckDetail()
                    {
                        MaterialId = x.MaterialId,
                        StockId = x.StockId,
                        GoodsLocationId = x.GoodsLocationId,
                        BatchNo = x.BatchNo,
                        QTY = x.QTY,
                        SerialNumber = i + 1,
                        OrderId = Id
                    }).ToList();
                await Db.Insertable(details).ExecuteCommandAsync();
            }
        }

        return await base.Update(Id, entity);
    }
    #endregion



    #region 删除数据 
    /// <summary>
    /// 删除指定ID集合的数据(批量删除)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public override async Task<bool> Delete(Guid[] ids)
    {
        var entities = new List<IvCheck>();
        foreach (var id in ids)
        {
            if (!await AnyAsync(id))
                continue;

            var entity = await Query(id);

            if (entity.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit)
                throw new Exception($"订单【{entity.OrderNo}】已审核通过，不可删除！");

            entity.IsDeleted = true;
            entities.Add(entity);

            await Db.Updateable<IvCheckDetail>()
                .SetColumns(x => new IvCheckDetail()
                {
                    IsDeleted = true
                }, true)
                .Where(x => x.OrderId == id && x.IsDeleted == false)
                .ExecuteCommandAsync();
        }
        return await BaseDal.Update(entities, ["IsDeleted"]);
    }
    #endregion

    #region 审核数据
    /// <summary>
    /// 审核指定ID集合的数据(批量审核)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public override async Task<bool> BulkAudit(Guid[] ids) => await base.BulkAudit(ids, $"OrderStatus = '{DIC_IV_CHECK_STATUS.WaitCheck}'");
    #endregion

    #region 撤销数据 
    /// <summary>
    /// 撤销指定ID集合的数据(批量撤销)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public override async Task<bool> BulkRevocation(Guid[] ids)
    {
        List<IvCheck> entities = new();
        foreach (var id in ids)
        {
            if (!await AnyAsync(id))
                continue;

            var entity = await Query(id);

            if (entity.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit && entity.OrderStatus == DIC_IV_CHECK_STATUS.WaitCheck)
            {
                entity.AuditStatus = DIC_SYSTEM_AUDIT_STATUS.Add;
                entities.Add(entity);
            }
        }
        await BaseDal.Update(entities, ["AuditStatus"], null, $"OrderStatus = '{DIC_IV_CHECK_STATUS.WaitCheck}'");
        return true;
    }
    #endregion

    #region 订单过账
    /// <summary>
    /// 订单过账指定ID集合的数据(订单过账)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    [UseTran]
    public async Task<ServiceResult> BulkOrderPostingAsync(Guid[] ids)
    {
        try
        {
            for (int i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                if (await Db.Queryable<IvCheck>().AnyAsync(x => x.ID == id && (x.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.Add || x.OrderStatus == DIC_IV_CHECK_STATUS.CheckComplete)))
                    continue;
                var details = await Db.Queryable<IvCheckDetail>().Where(x => x.OrderId == id).ToListAsync();

                for (int j = 0; j < details.Count; j++)
                {
                    var detail = details[j];

                    await IVChangeHelper.Add(Db,
                         detail.MaterialId,
                         detail.StockId,
                         detail.GoodsLocationId,
                         detail.QTY,
                         ChangeType.InventoryOut, id, detail.ID, detail.BatchNo
                         );
                }
            }
            await Db.Updateable<IvCheck>()
                  .SetColumns(it => new IvCheck()
                  {
                      OrderStatus = DIC_IV_CHECK_STATUS.CheckComplete
                  }, true)
                  .Where(it => ids.Contains(it.ID) &&
                  it.OrderStatus == DIC_IV_CHECK_STATUS.WaitCheck &&
                  it.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit)
                  .ExecuteCommandAsync();

            return Success(ResponseText.EXECUTE_SUCCESS);
        }
        catch (Exception E)
        {
            return Failed(E.Message);
        }
    }
    #endregion
}