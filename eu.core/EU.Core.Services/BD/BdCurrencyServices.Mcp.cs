using System.Text.Json;

namespace EU.Core.Services;

/// <summary>币别 MCP 业务规则，复用现有服务，不另建存储层。</summary>
public partial class BdCurrencyServices
{

    #region 查询币别
    /// <summary>查询有效且未删除的数据，输出白名单字段。</summary>
    /// <param name="input">分页和筛选条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>分页业务数据。</returns>
    public Task<PageModel<Dictionary<string, object>>> QueryForMcpAsync(BasicDataMcpQuery input, CancellationToken cancellationToken = default)
        => BasicDataMcpRules.Query<BdCurrency, CurrencyMcpValues>(Db, input, "CurrencyNo", "CurrencyName", null, cancellationToken);
    #endregion

    #region 新增币别
    /// <summary>应用层查重后调用标准 Add，不保证数据库级并发唯一性。</summary>
    /// <param name="input">业务字段。</param>
    /// <param name="cancellationToken">写入开始前取消令牌。</param>
    /// <returns>新增记录 ID。</returns>
    public async Task<Guid> CreateForMcpAsync(CurrencyMcpValues input, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BasicDataMcpRules.Validate(input, "CurrencyName");
        var insert = BasicDataMcpRules.Convert<InsertBdCurrencyInput>(input);

        await BasicDataMcpRules.Unique<BdCurrency>(Db, insert, null, cancellationToken, "CurrencyNo", "CurrencyName");
        cancellationToken.ThrowIfCancellationRequested();
        var id = await Add(insert);
        if (id == Guid.Empty) throw new InvalidOperationException("BASIC_DATA_CREATE_FAILED");
        return id;
    }
    #endregion

    #region 修改币别
    /// <summary>唯一名称定位并合并补丁，修改前查重；未传字段保持原值。</summary>
    /// <param name="target">原名称目标。</param>
    /// <param name="values">业务字段补丁。</param>
    /// <param name="cancellationToken">写入开始前取消令牌。</param>
    /// <returns>修改记录 ID。</returns>
    public async Task<Guid> UpdateForMcpAsync(BasicDataMcpTarget target, JsonElement values, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BasicDataMcpRules.Parse<CurrencyMcpValues>(values);
        var existing = await BasicDataMcpRules.Resolve<BdCurrency>(Db, target, "CurrencyName", null, 32, cancellationToken);
        var edit = BasicDataMcpRules.Merge<EditBdCurrencyInput, CurrencyMcpValues>(existing, values);
        BasicDataMcpRules.Validate(edit, "CurrencyName");
        var columns = values.EnumerateObject().Select(field => field.Name).ToList();

        await BasicDataMcpRules.Unique<BdCurrency>(Db, edit, existing.ID, cancellationToken, "CurrencyNo", "CurrencyName");
        cancellationToken.ThrowIfCancellationRequested();
        if (!await Update(existing.ID, edit, columns, null, null)) throw new ArgumentException("BASIC_DATA_UPDATE_FAILED");
        return existing.ID;
    }
    #endregion

    #region 删除币别
    /// <summary>拒绝已知未删除业务引用，沿用基类逻辑删除。</summary>
    /// <param name="target">原名称目标。</param>
    /// <param name="cancellationToken">删除开始前取消令牌。</param>
    /// <returns>逻辑删除记录 ID。</returns>
    public async Task<Guid> DeleteForMcpAsync(BasicDataMcpTarget target, CancellationToken cancellationToken = default)
    {
        var existing = await BasicDataMcpRules.Resolve<BdCurrency>(Db, target, "CurrencyName", null, 32, cancellationToken);
        var id = existing.ID;
        await BasicDataMcpRules.NoReference<BdCustomer>(Db, row => row.CurrencyId == id, cancellationToken);
        await BasicDataMcpRules.NoReference<BdSupplier>(Db, row => row.CurrencyId == id, cancellationToken);
        await BasicDataMcpRules.NoReference<SdOrder>(Db, row => row.CurrencyId == id, cancellationToken);
        await BasicDataMcpRules.NoReference<SdChangeOrder>(Db, row => row.CurrencyId == id, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (!await Delete((object)id)) throw new ArgumentException("BASIC_DATA_DELETE_FAILED");
        return id;
    }
    #endregion
}
