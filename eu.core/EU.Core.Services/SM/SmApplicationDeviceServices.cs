/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmApplicationDevice.cs
*
* 功 能： N / A
* 类 名： SmApplicationDevice
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/4/27 16:04:06  SahHsiao   初版
*
* Copyright(c) 2025 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

using Google.Protobuf.WellKnownTypes;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Net.Mail;

namespace EU.Core.Services;

/// <summary>
/// APP客户端记录 (服务)
/// </summary>
public class SmApplicationDeviceServices : BaseServices<SmApplicationDevice, SmApplicationDeviceDto, InsertSmApplicationDeviceInput, EditSmApplicationDeviceInput>, ISmApplicationDeviceServices
{
    public SmApplicationDeviceServices(IBaseRepository<SmApplicationDevice> dal)
    {
        BaseDal = dal;
    }



    #region 记录设备信息
    /// <summary>
    /// 记录设备信息
    /// </summary>
    /// <param name="device">设备信息</param>
    /// <returns></returns>
    public ServiceResult Record(SmApplicationDevice device)
    {
        Task.Factory.StartNew(() => DealData(Db, device));
        return Success(ResponseText.EXECUTE_SUCCESS);
    }

    public void DealData(ISqlSugarClient _Db, SmApplicationDevice input)
    {

        var device = _Db.Queryable<SmApplicationDevice>().Where(x => x.UUID == input.UUID).First();

        if (device != null)
        {

            input.ID = device.ID;
            _Db.Updateable(input).UpdateColumns(x => new { x.UUID, x.Platform, x.Version, x.Brand, x.Model, x.BundleId, x.BundleVersion, x.UpdateBy, x.UpdateTime }).ExecuteCommand();

        }
        else
            _Db.Insertable(input).ExecuteCommand();

        var ipAddress = HttpContextExtension.GetUserIp(HttpUseContext.Current);

        var record = new SmApplicationRecord()
        {
            UUID = device?.UUID ?? input?.UUID,
            LaunchTime = DateTime.Now,
            IP = ipAddress,
        };
        Db.Insertable(record).ExecuteCommand();
    }
    #endregion
}