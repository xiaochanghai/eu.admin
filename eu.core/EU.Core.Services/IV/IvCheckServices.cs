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

using MathNet.Numerics.Distributions;
using static EU.Core.Common.Helper.IVChangeHelper;

namespace EU.Core.Services;

/// <summary>
/// 库存盘点单 (服务)
/// </summary>
public class IvCheckServices : BaseServices<IvCheck, IvCheckDto, InsertIvCheckInput, EditIvCheckInput>, IIvCheckServices
{
    private readonly ICommonServices _commonServices;
    public IvCheckServices(IBaseRepository<IvCheck> dal, ICommonServices commonServices)
    {
        BaseDal = dal;
        _commonServices = commonServices;
    }

    #region 新增
    public override async Task<Guid> Add(object entity)
    {
        var model = ConvertToEntity(entity);
        var result = await base.Add(entity);
        await GenerateCheckDetail(result, model.StockId, model.GoodsLocationId);
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
            await Db.Deleteable<IvCheckDetail>().Where(it => it.OrderId == Id).ExecuteCommandAsync();
            await GenerateCheckDetail(Id, model.StockId, model.GoodsLocationId);
        }

        return await base.Update(Id, entity);
    }
    /// <summary>
    /// 生成盘点明细（根据当前库存数据）
    /// </summary>
    /// <param name="orderId">盘点单ID</param>
    /// <param name="StockId">仓库ID</param>
    /// <param name="GoodsLocationId">货位ID</param>
    /// <returns></returns>
    public async Task GenerateCheckDetail(Guid orderId, Guid? StockId, Guid? GoodsLocationId)
    {
        var inventorys = await Db.Queryable<BdMaterialInventory>()
            .Where(x => x.StockId == StockId && x.GoodsLocationId == GoodsLocationId)
            .ToListAsync();
        if (inventorys.Any())
        {
            var details = inventorys
                .OrderBy(x => x.MaterialId)
                .Select((x, i) => new IvCheckDetail()
                {
                    MaterialId = x.MaterialId,
                    StockId = x.StockId,
                    GoodsLocationId = x.GoodsLocationId,
                    BatchNo = x.BatchNo.IsNotEmptyOrNull() ? x.BatchNo : null,
                    QTY = x.QTY,
                    SerialNumber = i + 1,
                    OrderId = orderId
                }).ToList();
            await Db.Insertable(details).ExecuteCommandAsync();
        }
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
        for (int i = 0; i < ids.Length; i++)
        {
            var id = ids[i];
            var filter = new QueryFilter()
            {
                Conditions = $"A.OrderId='{id}'",
                PageIndex = 1,
                PageSize = 100
            };

            var result = await _commonServices.QueryByFilter(filter, "IV_CHECK_DETAIL_MNG");

            var list = Db.Utilities.DataTableToList<IvCheckDetailDto>(result.data);
            var checkOrder = await base.QueryById(id);
            var remark = "库存盘点" + "," + checkOrder.OrderNo;

            #region 生成关联出库单
            var outOrder = new IvOut()
            {
                ID = Utility.GuidId,
                OrderDate = Utility.GetSysDate().Date,
                OrderNo = Utility.GenerateContinuousSequence("IvOutNo"),
                OrderStatus = DIC_IV_OUT_STATUS.OutComplete,
                StockId = checkOrder.StockId,
                GoodsLocationId = checkOrder.GoodsLocationId,
                Remark = remark
            };
            var outDetail = new List<IvOutDetail>();

            if (list.Where(x => x.ShortageQTY > 0).Any())
                await Db.Insertable(outOrder).ExecuteCommandAsync();
            #endregion

            #region 生成关联入库单
            var inOrder = new IvIn()
            {
                ID = Utility.GuidId,
                OrderDate = Utility.GetSysDate().Date,
                OrderNo = Utility.GenerateContinuousSequence("IvnNo"),
                OrderStatus = DIC_IV_IN_STATUS.InComplete,
                StockId = checkOrder.StockId,
                GoodsLocationId = checkOrder.GoodsLocationId,
                Remark = remark
            };
            var inDetail = new List<IvInDetail>();

            if (list.Where(x => x.SurplusQTY > 0).Any())
                await Db.Insertable(inOrder).ExecuteCommandAsync();
            #endregion


            int SerialNumber1 = 1;
            int SerialNumber2 = 1;
            for (int j = 0; j < list.Count; j++)
            {
                if (list[j].QTY != list[j].InitQTY)
                {
                    var material = await Db.Queryable<BdMaterial>().Where(x => x.ID == list[j].MaterialId).FirstAsync();
                    if (list[j].ShortageQTY > 0)
                    {
                        await IVChangeHelper.Add(Db,
                             list[j].MaterialId,
                             list[j].StockId,
                             list[j].GoodsLocationId,
                             list[j].ShortageQTY,
                             ChangeType.InventoryCheckOut, id, list[j].ID, list[j].BatchNo, remark);
                        outDetail.Add(new IvOutDetail()
                        {
                            OrderId = outOrder.ID,
                            SerialNumber = SerialNumber1,
                            QTY = list[j].ShortageQTY,
                            Price = material?.PurchasePrice,
                            MaterialId = list[j].MaterialId,
                            StockId = list[j].StockId,
                            GoodsLocationId = list[j].GoodsLocationId,
                            BatchNo = list[j].BatchNo,
                            Remark = remark
                        });
                        SerialNumber1++;
                    }
                    if (list[j].SurplusQTY > 0)
                    {
                        await IVChangeHelper.Add(Db,
                             list[j].MaterialId,
                             list[j].StockId,
                             list[j].GoodsLocationId,
                             list[j].SurplusQTY,
                             ChangeType.InventoryCheckIn, id, list[j].ID, list[j].BatchNo, remark);

                        inDetail.Add(new IvInDetail()
                        {
                            OrderId = inOrder.ID,
                            SerialNumber = SerialNumber2,
                            QTY = list[j].SurplusQTY,
                            Price = material?.PurchasePrice,
                            MaterialId = list[j].MaterialId,
                            StockId = list[j].StockId,
                            GoodsLocationId = list[j].GoodsLocationId,
                            BatchNo = list[j].BatchNo,
                            Remark = remark
                        });
                        SerialNumber2++;
                    }
                }

                await Db.Updateable<IvCheckDetail>()
                    .SetColumns(it => new IvCheckDetail()
                    {
                        BeforeQTY = list[j].InitQTY,
                        AfterQTY = list[j].QTY,
                    }, true)
                    .Where(it => it.ID == list[j].ID)
                    .ExecuteCommandAsync();
            }

            if (inDetail.Any())
                await Db.Insertable(inDetail).ExecuteCommandAsync();

            if (outDetail.Any())
                await Db.Insertable(outDetail).ExecuteCommandAsync();
        }
        await Db.Updateable<IvCheck>()
            .SetColumns(it => new IvCheck()
            {
                OrderStatus = DIC_IV_CHECK_STATUS.CheckComplete
            }, true)
            .Where(it => ids.Contains(it.ID) && it.OrderStatus == DIC_IV_CHECK_STATUS.WaitCheck &&
            it.AuditStatus == DIC_SYSTEM_AUDIT_STATUS.CompleteAudit)
            .ExecuteCommandAsync();

        return Success(ResponseText.EXECUTE_SUCCESS);
    }
    #endregion
}