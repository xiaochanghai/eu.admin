/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdSettlementWay.cs
*
*功 能： N / A
* 类 名： BdSettlementWay
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/25 19:29:36  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.IServices;

/// <summary>
/// 结算方式(自定义服务接口)
/// </summary>	
public interface IBdSettlementWayServices :IBaseServices<BdSettlementWay, BdSettlementWayDto, InsertBdSettlementWayInput, EditBdSettlementWayInput>
	{

    /// <summary>查询结算方式最小业务字段，受控环境内按编号和名称分页。</summary>
    /// <param name="input">编号、关键字和分页条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>白名单字段分页数据。</returns>
    Task<PageModel<Dictionary<string, object>>> QueryForMcpAsync(EU.Core.Model.ViewModels.Extend.BasicDataMcpQuery input, CancellationToken cancellationToken = default);

    /// <summary>校验并新增结算方式，调用标准业务持久化。</summary>
    /// <param name="input">白名单业务字段。</param>
    /// <param name="cancellationToken">写入开始前的取消令牌。</param>
    /// <returns>新增记录 ID。</returns>
    Task<Guid> CreateForMcpAsync(EU.Core.Model.ViewModels.Extend.SettlementWayMcpValues input, CancellationToken cancellationToken = default);

    /// <summary>按原名称定位唯一结算方式，只修改传入字段。</summary>
    /// <param name="target">原名称或客户原简称。</param>
    /// <param name="values">待修改字段 JSON。</param>
    /// <param name="cancellationToken">写入开始前的取消令牌。</param>
    /// <returns>实际修改的记录 ID。</returns>
    Task<Guid> UpdateForMcpAsync(EU.Core.Model.ViewModels.Extend.BasicDataMcpTarget target, System.Text.Json.JsonElement values, CancellationToken cancellationToken = default);

    /// <summary>按名称定位并检查已知引用后逻辑删除结算方式。</summary>
    /// <param name="target">原名称或客户原简称。</param>
    /// <param name="cancellationToken">删除开始前的取消令牌。</param>
    /// <returns>实际删除的记录 ID。</returns>
    Task<Guid> DeleteForMcpAsync(EU.Core.Model.ViewModels.Extend.BasicDataMcpTarget target, CancellationToken cancellationToken = default);
}
