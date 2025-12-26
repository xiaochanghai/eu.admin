/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* EmRepairOrder.cs
*
* 功 能： N / A
* 类 名： EmRepairOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/11/23 19:19:28  SahHsiao   初版
*
* Copyright(c) 2025 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

using EU.Core.Model.ViewModels;
using Microsoft.AspNetCore.Http;

namespace EU.Core.Services;

/// <summary>
/// 报修工单 (服务)
/// </summary>
public class EmRepairOrderServices : BaseServices<EmRepairOrder, EmRepairOrderDto, InsertEmRepairOrderInput, EditEmRepairOrderInput>, IEmRepairOrderServices
{
    private readonly IHttpContextAccessor _accessor;
    public EmRepairOrderServices(IBaseRepository<EmRepairOrder> dal, IHttpContextAccessor accessor)
    {
        BaseDal = dal;
        _accessor = accessor;
    }

    /// <summary>
    /// 获取设备
    /// </summary>
    /// <returns></returns>
    public async Task<ServiceResult<List<EmEquipment>>> GetEquipment()
    {
        var equipments = await Db.Queryable<EmEquipment>().OrderBy(x => x.MachineNo)
            .Select(x => new EmEquipment()
            {
                ID = x.ID,
                MachineNo = x.MachineNo,
                MachineName = x.MachineName,
                Location = x.Location
            })
            .ToListAsync();
        return Success(equipments);
    }

    #region 详情
    public override async Task<EmRepairOrderDto> QueryById(object objId)
    {
        var entity = await base.QueryById(objId);
        var platform = _accessor.HttpContext?.Request?.Headers["platform"].ObjToString();

        if (platform.IsNotEmptyOrNull())
        {
            entity.Equipment = await Db.Queryable<EmEquipment>()
                .Where(x => x.ID == entity.EquipmentId)
            .Select(x => new EmEquipment()
            {
                ID = x.ID,
                MachineNo = x.MachineNo,
                MachineName = x.MachineName,
                Location = x.Location
            })
            .FirstAsync();

            entity.FaultType = await LovHelper.GetLovText("EquipmentFaultType", entity.FaultType);
            entity.Status = await LovHelper.GetLovText("RepairOrderStatus", entity.Status);
            entity.Impact = await LovHelper.GetLovText("RepairOrderImpact", entity.Impact);
        }

        return entity;
    }
    #endregion

    #region 新增
    public override async Task<Guid> Add(object entity)
    {
        var model = ConvertToEntity(entity);

        var dic = ConvertToDic(entity);
        var lstColumns = dic.Keys.Where(x => x != "ID" && x != "Id").ToList();
        lstColumns.Add("AuditStatus");
        lstColumns.Add("OrderNo");
        lstColumns.Add("Status");

        #region 检查是否存在相同值
        CheckOnly(model);
        #endregion

        model.OrderNo = Utility.GenerateContinuousSequence("EmRepairOrderNo");
        model.Status = DIC_REPAIR_ORDER_STATUS.Wait;
        return await BaseDal.Add(model, lstColumns);
    }
    #endregion
}