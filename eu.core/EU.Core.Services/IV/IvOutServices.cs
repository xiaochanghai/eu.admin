/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IvOut.cs
*
* 功 能： N / A
* 类 名： IvOut
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2024/12/18 15:49:09  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 库存出库单 (服务)
/// </summary>
public class IvOutServices : BaseServices<IvOut, IvOutDto, InsertIvOutInput, EditIvOutInput>, IIvOutServices
{
    private readonly IBaseRepository<IvOut> _dal;
    public IvOutServices(IBaseRepository<IvOut> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    #region 删除数据 
    /// <summary>
    /// 删除指定ID集合的数据(批量删除)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public override async Task<bool> Delete(Guid[] ids)
    {
        var entities = new List<IvOut>();
        foreach (var id in ids)
        {
            if (!await AnyAsync(id))
                continue;

            var entity = await Query(id);

            if (entity.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit)
                throw new Exception($"订单【{entity.OrderNo}】已审核通过，不可删除！");

            entity.IsDeleted = true;
            entities.Add(entity);

            await Db.Updateable<IvOutDetail>()
                .SetColumns(x => new IvOutDetail()
                {
                    IsDeleted = true,
                    UpdateTime = DateTime.Now,
                    UpdateBy = UserId
                })
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
    public override async Task<bool> BulkAudit(Guid[] ids) => await base.BulkAudit(ids, $"OrderStatus = '{DIC_IV_OUT_STATUS.WaitOut}'");
    #endregion

    #region 撤销数据 
    /// <summary>
    /// 撤销指定ID集合的数据(批量撤销)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public override async Task<bool> BulkRevocation(Guid[] ids)
    {
        List<IvOut> entities = new();
        foreach (var id in ids)
        {
            if (!await AnyAsync(id))
                continue;

            var entity = await Query(id);

            if (entity.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit && entity.OrderStatus == DIC_IV_OUT_STATUS.WaitOut)
            {
                entity.AuditStatus = DIC_SYSTEM_AUDIT_STATUS.Add;
                entities.Add(entity);
            }
        }
        await BaseDal.Update(entities, ["AuditStatus"], null, $"OrderStatus = '{DIC_IV_OUT_STATUS.WaitOut}'");
        return true;
    }
    #endregion

    #region 订单过账
    /// <summary>
    /// 订单过账指定ID集合的数据(订单过账)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public async Task<ServiceResult> BulkOrderPostingAsync(Guid[] ids)
    {
        try
        {
            await Db.Ado.BeginTranAsync();
            for (int i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                if (await Db.Queryable<IvOut>().AnyAsync(x => x.ID == id && (x.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.Add || x.OrderStatus == DIC_IV_OUT_STATUS.OutComplete)))
                    continue;
                var details = await Db.Queryable<IvOutDetail>().Where(x => x.OrderId == id).ToListAsync();

                for (int j = 0; j < details.Count; j++)
                {
                    var detail = details[j];

                    await IVChangeHelper.Add(Db,
                         detail.MaterialId,
                         detail.StockId,
                         detail.GoodsLocationId,
                         detail.QTY,
                         DIC_IV_CHANGE_TYPE.IvOut, id, detail.ID, detail.BatchNo
                         );
                }
            }
            await Db.Updateable<IvOut>()
                  .SetColumns(it => new IvOut()
                  {
                      OrderStatus = DIC_IV_OUT_STATUS.OutComplete
                  }, true)
                  .Where(it => ids.Contains(it.ID) &&
                  it.OrderStatus == DIC_IV_OUT_STATUS.WaitOut &&
                  it.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit)
                  .ExecuteCommandAsync();


            await Db.Ado.CommitTranAsync();
            return Success(ResponseText.EXECUTE_SUCCESS);
        }
        catch (Exception E)
        {
            await Db.Ado.RollbackTranAsync();
            return Failed(E.Message);
        }
    }
    #endregion
}