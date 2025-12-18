/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmApplicationVersion.cs
*
* 功 能： N / A
* 类 名： SmApplicationVersion
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/12/3 16:44:02  SahHsiao   初版
*
* Copyright(c) 2025 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// APP版本 (服务)
/// </summary>
public class SmApplicationVersionServices : BaseServices<SmApplicationVersion, SmApplicationVersionDto, InsertSmApplicationVersionInput, EditSmApplicationVersionInput>, ISmApplicationVersionServices
{
    public SmApplicationVersionServices(IBaseRepository<SmApplicationVersion> dal)
    {
        BaseDal = dal;
    }


    #region 记录设备信息
    /// <summary>
    /// 记录设备信息
    /// </summary>
    /// <param name="device">设备信息</param>
    /// <returns></returns>
    public async Task<ServiceResult<SmApplicationVersion>> Latest()
    {
        var platform = App.User.GetPlatform() ?? "ios";

        var version = await Db.Queryable<SmApplicationVersion>()
            .OrderByDescending(x => x.BuildNum)
            .Where(x => x.Platform == platform).FirstAsync();

        return Success(version, ResponseText.QUERY_SUCCESS);
    }
    #endregion
}