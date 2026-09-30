/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdSupplier.cs
*
*功 能： N / A
* 类 名： BdSupplier
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/25 19:20:50  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.IServices;

/// <summary>
/// 供应商(自定义服务接口)
/// </summary>	
public interface IBdSupplierServices :IBaseServices<BdSupplier, BdSupplierDto, InsertBdSupplierInput, EditBdSupplierInput>
{
    /// <summary>不校验身份和权限，物理删除唯一匹配供应商，仅用于受控环境。</summary>
    /// <param name="supplierId">供应商 ID，与编号至少提供一个。</param>
    /// <param name="supplierNo">供应商编号，同时提供 ID 时必须同时匹配。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>是否成功删除；目标不明确时拒绝。</returns>
    Task<bool> DeleteSupplierForMcpAsync(string supplierId, string supplierNo, CancellationToken cancellationToken = default);
    /// <summary>匿名查询全部公司的有效供应商，仅返回最小业务字段。</summary>
    /// <param name="input">编号、名称关键字与分页条件。</param>
    /// <param name="cancellationToken">查询取消令牌。</param>
    /// <returns>供应商分页结果；保留参数校验和取消支持。</returns>
    Task<PageModel<EU.Core.Model.ViewModels.Extend.SupplierQueryItem>> QuerySuppliersAsync(EU.Core.Model.ViewModels.Extend.SupplierQueryInput input, CancellationToken cancellationToken = default);
}
