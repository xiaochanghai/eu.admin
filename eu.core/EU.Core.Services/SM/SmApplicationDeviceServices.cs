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

using Microsoft.Extensions.Logging;

namespace EU.Core.Services;

/// <summary>
/// APP客户端记录 (服务)
/// </summary>
public class SmApplicationDeviceServices : BaseServices<SmApplicationDevice, SmApplicationDeviceDto, InsertSmApplicationDeviceInput, EditSmApplicationDeviceInput>, ISmApplicationDeviceServices
{
    private readonly ILogger<SmApplicationDeviceServices> _logger;

    public SmApplicationDeviceServices(
        IBaseRepository<SmApplicationDevice> dal,
        ILogger<SmApplicationDeviceServices> logger)
    {
        BaseDal = dal;
        _logger = logger;
    }

    #region 记录设备信息
    /// <summary>
    /// 记录设备信息
    /// </summary>
    /// <param name="device">设备信息</param>
    /// <returns></returns>
    public Task<ServiceResult> Record(SmApplicationDevice device)
    {
        if (device == null || device.UUID.IsNullOrEmpty())
            return Task.FromResult(Failed("设备信息不能为空"));

        var ipAddress = HttpContextExtension.GetUserIp(App.HttpContext);

        // 在后台线程异步执行，不阻塞主线程
        _ = Task.Run(async () =>
        {
            try
            {
                await RecordDeviceAsync(device, ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录设备信息失败: UUID={UUID}", device?.UUID);
            }
        });

        return Task.FromResult(Success(ResponseText.EXECUTE_SUCCESS));
    }

    /// <summary>
    /// 异步处理设备数据并记录访问日志
    /// </summary>
    /// <param name="input">设备信息</param>
    /// <param name="ipAddress">IP地址</param>
    private async Task RecordDeviceAsync(SmApplicationDevice input, string ipAddress)
    {
        if (input == null || input.UUID.IsNullOrEmpty())
            return;

        try
        {
            await Db.Ado.BeginTranAsync();

            var existingDevice = await Db.Queryable<SmApplicationDevice>()
                .Where(x => x.UUID == input.UUID)
                .FirstAsync();

            if (existingDevice != null)
            {
                input.ID = existingDevice.ID;
                await Db.Updateable(input)
                    .UpdateColumns(x => new
                    {
                        x.UUID,
                        x.Platform,
                        x.Version,
                        x.Brand,
                        x.Model,
                        x.BundleId,
                        x.BundleVersion,
                        x.PushRegistrationId,
                    })
                    .ExecuteCommandAsync();
            }
            else
            {
                await Db.Insertable(input).ExecuteCommandAsync();
            }

            var record = new SmApplicationRecord
            {
                UUID = input.UUID,
                LaunchTime = DateTime.Now,
                IP = ipAddress
            };
            await Db.Insertable(record).ExecuteCommandAsync();

            await Db.Ado.CommitTranAsync();
        }
        catch (Exception)
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion
}
