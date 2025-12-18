/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* EmEquipment.cs
*
* 功 能： N / A
* 类 名： EmEquipment
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/12/3 23:26:02  SahHsiao   初版
*
* Copyright(c) 2025 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

using EU.Core.Model.ViewModels;

namespace EU.Core.Services;

/// <summary>
/// 设备基础资料 (服务)
/// </summary>
public class EmEquipmentServices : BaseServices<EmEquipment, EmEquipmentDto, InsertEmEquipmentInput, EditEmEquipmentInput>, IEmEquipmentServices
{
    public EmEquipmentServices(IBaseRepository<EmEquipment> dal)
    {
        BaseDal = dal;
    }

    public override async Task<EmEquipmentDto> QueryById(object objId)
    {
        var entity = await base.QueryById(objId);

        var attachment = await Db.Queryable<FileAttachment>().Where(x => x.MasterId == entity.ID).ToListAsync();
        entity.ImageIds = attachment.Where(x => x.ImageType == "equipment").Select(x => x.ID).ToList();
        entity.Attachments = attachment.Where(x => x.ImageType == "equipment_attachment").ToList();

        var repairStats = new List<EquipmetRepairStats>
        {
            new EquipmetRepairStats()
            {
                value = "12次",
                label = "累计维修",
                bgColor = "#eff6ff",
                textColor = "#1890ff"
            },
            new EquipmetRepairStats()
            {
                value = "8次",
                label = "累计保养",
                bgColor = "#f0fdf4",
                textColor = "#52c41a"
            },
            new EquipmetRepairStats()
            {
                value = "2次",
                label = "本月维修",
                bgColor = "#fff7ed",
                textColor = "#faad14"
            },
            new EquipmetRepairStats()
            {
                value = "'¥8,500",
                label = "维修成本",
                bgColor = "#faf5ff",
                textColor = "#a855f7"
            }
        };

        entity.RepairStats = repairStats;
        entity.StartDate1 = entity.StartDate1.ConvertToDayString();
        if (entity.UseDeptId.IsNotEmptyOrNull())
            entity.DeptName = await Db.Queryable<SmDepartment>().Where(x => x.ID == entity.UseDeptId).Select(x => x.DepartmentName).FirstAsync();

        if (entity.MachineTypeId.IsNotEmptyOrNull())
            entity.MachineType = await Db.Queryable<EmEquipmentType>().Where(x => x.ID == entity.MachineTypeId).Select(x => x.TypeName).FirstAsync();

        if (entity.UseManagerId.IsNotEmptyOrNull())
            entity.UseManagerName = await Db.Queryable<SmEmployee>().Where(x => x.ID == entity.UseManagerId)
                .Select(x => $"{x.EmployeeName}（{x.Phone}）").FirstAsync();



        return entity;
    }
}