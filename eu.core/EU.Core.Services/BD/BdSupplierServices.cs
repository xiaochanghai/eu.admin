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

namespace EU.Core.Services;

/// <summary>
/// 供应商 (服务)
/// </summary>
public class BdSupplierServices : BaseServices<BdSupplier, BdSupplierDto, InsertBdSupplierInput, EditBdSupplierInput>, IBdSupplierServices
{
    private readonly IBaseRepository<BdSupplier> _dal;
    public BdSupplierServices(IBaseRepository<BdSupplier> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    #region 查询供应商数据
    /// <summary>按编号或名称匿名查询全部公司的有效供应商，仅返回最小业务字段。</summary>
    /// <param name="input">查询条件；页大小最多 100，返回最小业务字段。</param>
    /// <param name="cancellationToken">查询取消令牌。</param>
    /// <returns>按编号、ID 排序的供应商分页数据。</returns>
    public async Task<PageModel<SupplierQueryItem>> QuerySuppliersAsync(SupplierQueryInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        if (input.PageIndex < 1 || input.PageSize is < 1 or > 100
            || (long)input.PageIndex * input.PageSize > int.MaxValue
            || input.SupplierNo?.Length > 32 || input.Keyword?.Length > 100)
            throw new ArgumentException("Invalid supplier query parameters.", nameof(input));
        // 项目所有者确认：此查询暂不校验身份、租户、模块或公司授权。
        var database = Db;
        string number = input.SupplierNo?.Trim();
        string keyword = input.Keyword?.Trim();
        var query = database.Queryable<BdSupplier>()
            .Where(x => x.IsActive == true && !x.IsDeleted)
            .WhereIF(!string.IsNullOrEmpty(number), x => x.SupplierNo == number)
            .WhereIF(!string.IsNullOrEmpty(keyword), x => x.FullName.Contains(keyword) || x.ShortName.Contains(keyword));
        int count = await query.Clone().CountAsync(cancellationToken);
        var rows = await query.OrderBy(x => x.SupplierNo).OrderBy(x => x.ID)
            .Skip((input.PageIndex - 1) * input.PageSize).Take(input.PageSize)
            .Select(x => new SupplierQueryItem
            {
                Id = x.ID, SupplierNo = x.SupplierNo, FullName = x.FullName,
                ShortName = x.ShortName, TaxType = x.TaxType, TaxRate = x.TaxRate
            }).ToListAsync(cancellationToken);
        return new PageModel<SupplierQueryItem>(input.PageIndex, count, input.PageSize, rows);
    }
    #endregion

    #region MCP 删除供应商
    /// <summary>不校验身份或权限，精确定位唯一供应商后物理删除；仅用于受控环境。</summary>
    /// <param name="supplierId">供应商 ID；提供时必须为非空 GUID。</param>
    /// <param name="supplierNo">供应商编号；和 ID 至少提供一个，同时提供时必须同时匹配。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>是否删除目标；不存在返回 false，歧义目标拒绝。</returns>
    public async Task<bool> DeleteSupplierForMcpAsync(string supplierId, string supplierNo, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string number = supplierNo?.Trim();
        bool hasId = !string.IsNullOrWhiteSpace(supplierId);
        Guid id = Guid.Empty;
        if ((!hasId && string.IsNullOrEmpty(number)) || number?.Length > 32
            || (hasId && (!Guid.TryParse(supplierId, out id) || id == Guid.Empty)))
            throw new ArgumentException("SUPPLIER_TARGET_INVALID");
        var targets = await Db.Queryable<BdSupplier>()
            .Where(x => !x.IsDeleted)
            .WhereIF(hasId, x => x.ID == id)
            .WhereIF(!string.IsNullOrEmpty(number), x => x.SupplierNo == number)
            .Take(2).ToListAsync(cancellationToken);
        if (targets.Count == 0) return false;
        if (targets.Count != 1) throw new ArgumentException("SUPPLIER_TARGET_AMBIGUOUS");
        Guid targetId = targets[0].ID;
        // 写入时保留唯一 ID 及编号约束，避免空条件删除。
        return await Db.Deleteable<BdSupplier>()
            .Where(x => x.ID == targetId && !x.IsDeleted)
            .WhereIF(!string.IsNullOrEmpty(number), x => x.SupplierNo == number)
            .ExecuteCommandAsync(cancellationToken) > 0;
    }
    #endregion
}
