/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SdOutOrder.cs
*
*功 能： N / A
* 类 名： SdOutOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/8/14 15:23:59  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Services;

/// <summary>
/// 出库单 (服务)
/// </summary>
public class SdOutOrderServices : BaseServices<SdOutOrder, SdOutOrderDto, InsertSdOutOrderInput, EditSdOutOrderInput>, ISdOutOrderServices
{
    private readonly IBaseRepository<SdOutOrder> _dal;
    private readonly ISdOutOrderDetailServices _sdOutOrderDetailServices;
    public SdOutOrderServices(IBaseRepository<SdOutOrder> dal, ISdOutOrderDetailServices sdOutOrderDetailServices)
    {
        this._dal = dal;
        base.BaseDal = dal;
        _sdOutOrderDetailServices = sdOutOrderDetailServices;
    }

    #region 审核数据

    /// <summary>
    /// 审核指定ID集合的数据(批量审核)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public override async Task<bool> BulkAudit(Guid[] ids)
    {
        var entities = new List<SdOutOrder>();
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
        await BaseDal.Update(entities, ["AuditStatus"], null, $"OrderStatus = '{DIC_SALES_OUT_ORDER_STATUS.WaitOut}'");
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
        var entities = new List<SdOutOrder>();
        foreach (var id in ids)
        {
            if (!await AnyAsync(id))
                continue;

            var entity = await Query(id);

            if (entity.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit && entity.OrderStatus == DIC_SALES_OUT_ORDER_STATUS.WaitOut)
            {
                entity.AuditStatus = DIC_SYSTEM_AUDIT_STATUS.Add;
                entities.Add(entity);
            }
        }
        await BaseDal.Update(entities, ["AuditStatus"], null, $"OrderStatus = '{DIC_SALES_OUT_ORDER_STATUS.WaitOut}'");
        return true;
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
        var entities = new List<SdOutOrder>();
        for (int i = 0; i < ids.Length; i++)
        {
            var id = ids[i];
            if (!await AnyAsync(id))
                continue;

            var entity = await Query(id);
            if (entity.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit)
                throw new Exception($"订单【{entity.OrderNo}】已审核通过，不可删除！");

            entity.IsDeleted = true;
            entities.Add(entity);

            var details = await Db.Queryable<SdOutOrderDetail>().Where(x => x.OrderId == id).Select(x => x.ID).ToArrayAsync();
            if (details.Any())
                await _sdOutOrderDetailServices.Delete(details);
        }
        List<string> lstColumns = ["IsDeleted"];
        var result = await BaseDal.Update(entities, lstColumns);

        return result;
    }
    #endregion

    #region 订单过账
    /// <summary>
    /// 订单过账指定ID集合的数据(订单过账)
    /// </summary>
    /// <param name="ids">主键ID集合</param>
    /// <returns></returns>
    public async Task<ServiceResult> BulkOrderCarryTo(Guid[] ids)
    {
        await Db.Updateable<SdOutOrder>()
            .SetColumns(it => new SdOutOrder()
            {
                OrderStatus = DIC_SALES_OUT_ORDER_STATUS.OutComplete,
                UpdateBy = App.User.ID,
                UpdateTime = Utility.GetSysDate()
            })
            .Where(it =>
            ids.Contains(it.ID) &&
            it.OrderStatus != DIC_SALES_OUT_ORDER_STATUS.OutComplete &&
            it.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit)
            .ExecuteCommandAsync();
        return ServiceResult.OprateSuccess(ResponseText.EXECUTE_SUCCESS);

    }
    #endregion
}