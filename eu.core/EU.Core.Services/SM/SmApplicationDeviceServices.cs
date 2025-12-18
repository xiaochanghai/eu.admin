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

using SqlSugar;

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
    public async Task<ServiceResult> Record(SmApplicationDevice device)
    {
        // 在当前上下文中获取IP地址，避免在异步任务中HttpContext不可用
        var ipAddress = HttpContextExtension.GetUserIp(HttpUseContext.Current);

        // 使用异步方法处理数据
        await Task.Run(() => DealData(device, ipAddress));

        return Success(ResponseText.EXECUTE_SUCCESS);
    }

    /// <summary>
    /// 处理设备数据并记录访问日志
    /// </summary>
    /// <param name="input">设备信息</param>
    /// <param name="ipAddress">IP地址</param>
    private void DealData(SmApplicationDevice input, string ipAddress)
    {
        try
        {
            // 查询现有设备信息
            var existingDevice = Db.Queryable<SmApplicationDevice>()
                .Where(x => x.UUID == input.UUID)
                .First();

            if (existingDevice != null)
            {
                // 更新现有设备信息
                input.ID = existingDevice.ID;
                Db.Updateable(input)
                    .UpdateColumns(x => new
                    {
                        x.UUID,
                        x.Platform,
                        x.Version,
                        x.Brand,
                        x.Model,
                        x.BundleId,
                        x.BundleVersion,
                        x.UpdateBy,
                        x.UpdateTime
                    })
                    .ExecuteCommand();
            }
            else
            {
                // 插入新设备信息
                Db.Insertable(input).ExecuteCommand();
            }

            // 记录设备访问日志
            var record = new SmApplicationRecord
            {
                UUID = input.UUID,
                LaunchTime = DateTime.Now,
                IP = ipAddress
            };
            Db.Insertable(record).ExecuteCommand();
        }
        catch (Exception ex)
        {
            // 记录异常信息（建议注入ILogger进行日志记录）
            Console.WriteLine($"记录设备信息失败: UUID={input?.UUID}, Error={ex.Message}");
            throw;
        }
    }
    #endregion
}