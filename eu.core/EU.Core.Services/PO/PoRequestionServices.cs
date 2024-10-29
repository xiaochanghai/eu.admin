/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoRequestion.cs
*
*功 能： N / A
* 类 名： PoRequestion
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/9/4 16:16:33  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Services;

/// <summary>
/// 请购单 (服务)
/// </summary>
public class PoRequestionServices : BaseServices<PoRequestion, PoRequestionDto, InsertPoRequestionInput, EditPoRequestionInput>, IPoRequestionServices
{
    private readonly IBaseRepository<PoRequestion> _dal;
    public PoRequestionServices(IBaseRepository<PoRequestion> dal)
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
        var entities = new List<PoRequestion>();
        foreach (var id in ids)
        {
            if (!await AnyAsync(id))
                continue;

            var entity = await Query(id);

            if (entity.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit)
                throw new Exception($"订单【{entity.OrderNo}】已审核通过，不可删除！");

            var ent = entity as BasePoco;
            ent.IsDeleted = true;
            entities.Add(entity);
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
    public override async Task<bool> BulkAudit(Guid[] ids)
    {
        var entities = new List<PoRequestion>();
        foreach (var id in ids)
        {
            if (!await AnyAsync(id))
                continue;

            var entity = await Query(id);

            if (entity.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.Add)
            {
                entity.AuditStatus = DIC_SYSTEM_AUDIT_STATUS.CompleteAudit;
                entities.Add(entity);
            }
        }
        await BaseDal.Update(entities, ["AuditStatus"], null, $"OrderStatus = '{DIC_PURCHASE_REQUEST_STATUS.Wait}'");
        return true;
    }
    #endregion

    #region 撤销数据 
    /// <summary>
    /// 撤销指定ID集合的数据(批量撤销)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public override async Task<bool> BulkRevocation(Guid[] ids)
    {
        var entities = new List<PoRequestion>();
        foreach (var id in ids)
        {
            if (!await AnyAsync(id))
                continue;

            var entity = await Query(id);

            if (entity.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit && entity.OrderStatus == DIC_PURCHASE_REQUEST_STATUS.Wait)
            {
                entity.AuditStatus = DIC_SYSTEM_AUDIT_STATUS.Add;
                entities.Add(entity);
            }
        }
        await BaseDal.Update(entities, ["AuditStatus"], null, $"OrderStatus = '{DIC_PURCHASE_REQUEST_STATUS.Wait}'");
        return true;
    }
    #endregion

    #region 订单完结
    /// <summary>
    /// 订单完结指定ID集合的数据(订单完结)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public async Task<ServiceResult> BulkOrderComplete(Guid[] ids)
    {
        await Db.Updateable<PoRequestion>()
            .SetColumns(it => new PoRequestion()
            {
                OrderStatus = DIC_PURCHASE_REQUEST_STATUS.OrderComplete,
                UpdateBy = App.User.ID,
                UpdateTime = Utility.GetSysDate()
            })
            .Where(it =>
            ids.Contains(it.ID) &&
            it.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit &&
            it.OrderStatus != DIC_PURCHASE_REQUEST_STATUS.PurchaseComplete &&
            it.OrderStatus != DIC_PURCHASE_REQUEST_STATUS.OrderComplete)
            .ExecuteCommandAsync();
        return ServiceResult.OprateSuccess(ResponseText.EXECUTE_SUCCESS);

    }
    #endregion
}