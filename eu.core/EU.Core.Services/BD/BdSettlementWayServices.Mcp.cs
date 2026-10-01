using System.Text.Json;

namespace EU.Core.Services;

/// <summary>结算方式 MCP 业务规则，复用现有服务，不另建存储层。</summary>
public partial class BdSettlementWayServices
{

    #region 查询结算方式
    /// <summary>查询有效且未删除的数据，输出白名单字段。</summary>
    /// <param name="input">分页和筛选条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>分页业务数据。</returns>
    public Task<PageModel<Dictionary<string, object>>> QueryForMcpAsync(BasicDataMcpQuery input, CancellationToken cancellationToken = default)
        => BasicDataMcpRules.Query<BdSettlementWay, SettlementWayMcpValues>(Db, input, "SettlementNo", "SettlementName", null, cancellationToken, "SettlementName");
    #endregion

    #region 新增结算方式
    /// <summary>应用层查重后调用标准 Add，不保证数据库级并发唯一性。</summary>
    /// <param name="input">业务字段。</param>
    /// <param name="cancellationToken">写入开始前取消令牌。</param>
    /// <returns>新增记录 ID。</returns>
    public async Task<Guid> CreateForMcpAsync(SettlementWayMcpValues input, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BasicDataMcpRules.Validate(input, "SettlementAccountType", "SettlementBillType");
        var insert = BasicDataMcpRules.Convert<InsertBdSettlementWayInput>(input);
        insert.SettlementName = await BuildMcpSettlementNameAsync(insert.SettlementAccountType, insert.Days, insert.SettlementBillType, cancellationToken);
        await BasicDataMcpRules.Unique<BdSettlementWay>(Db, insert, null, cancellationToken, "SettlementNo", "SettlementName");
        cancellationToken.ThrowIfCancellationRequested();
        var id = await Add(insert);
        if (id == Guid.Empty) throw new InvalidOperationException("BASIC_DATA_CREATE_FAILED");
        return id;
    }
    #endregion

    #region 修改结算方式
    /// <summary>唯一名称定位并合并补丁，修改前查重；未传字段保持原值。</summary>
    /// <param name="target">原名称目标。</param>
    /// <param name="values">业务字段补丁。</param>
    /// <param name="cancellationToken">写入开始前取消令牌。</param>
    /// <returns>修改记录 ID。</returns>
    public async Task<Guid> UpdateForMcpAsync(BasicDataMcpTarget target, JsonElement values, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BasicDataMcpRules.Parse<SettlementWayMcpValues>(values);
        var existing = await BasicDataMcpRules.Resolve<BdSettlementWay>(Db, target, "SettlementName", null, 32, cancellationToken);
        var edit = BasicDataMcpRules.Merge<EditBdSettlementWayInput, SettlementWayMcpValues>(existing, values);
        BasicDataMcpRules.Validate(edit, "SettlementAccountType", "SettlementBillType");
        var columns = values.EnumerateObject().Select(field => field.Name).ToList();
        edit.SettlementName = await BuildMcpSettlementNameAsync(edit.SettlementAccountType, edit.Days, edit.SettlementBillType, cancellationToken);
        columns.Add("SettlementName");
        await BasicDataMcpRules.Unique<BdSettlementWay>(Db, edit, existing.ID, cancellationToken, "SettlementNo", "SettlementName");
        cancellationToken.ThrowIfCancellationRequested();
        if (!await Update(existing.ID, edit, columns, null, null)) throw new ArgumentException("BASIC_DATA_UPDATE_FAILED");
        return existing.ID;
    }
    #endregion

    #region 删除结算方式
    /// <summary>拒绝已知未删除业务引用，沿用基类逻辑删除。</summary>
    /// <param name="target">原名称目标。</param>
    /// <param name="cancellationToken">删除开始前取消令牌。</param>
    /// <returns>逻辑删除记录 ID。</returns>
    public async Task<Guid> DeleteForMcpAsync(BasicDataMcpTarget target, CancellationToken cancellationToken = default)
    {
        var existing = await BasicDataMcpRules.Resolve<BdSettlementWay>(Db, target, "SettlementName", null, 32, cancellationToken);
        var id = existing.ID;
        await BasicDataMcpRules.NoReference<BdCustomer>(Db, row => row.SettlementWayId == id, cancellationToken);
        await BasicDataMcpRules.NoReference<BdSupplier>(Db, row => row.SettlementWayId == id, cancellationToken);
        await BasicDataMcpRules.NoReference<SdOrder>(Db, row => row.SettlementWayId == id, cancellationToken);
        await BasicDataMcpRules.NoReference<SdChangeOrder>(Db, row => row.SettlementWayId == id, cancellationToken);
        await BasicDataMcpRules.NoReference<PoOrder>(Db, row => row.SettlementWayId == id, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (!await Delete((object)id)) throw new ArgumentException("BASIC_DATA_DELETE_FAILED");
        return id;
    }
    #endregion

    #region 生成结算名称
    /// <summary>通过现有账款类型字典生成名称，拒绝未知类型、负账期和过长名称。</summary>
    /// <param name="accountType">账款类型参数值。</param>
    /// <param name="days">账期天数。</param>
    /// <param name="billType">Get 收款或 Out 付款。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>与旧服务一致的派生名称。</returns>
    private async Task<string> BuildMcpSettlementNameAsync(string accountType, int? days, string billType, CancellationToken cancellationToken)
    {
        if (days < 0 || billType is not ("Get" or "Out")) throw new ArgumentException("SETTLEMENT_VALUES_INVALID");
        var types = await GetMcpSettlementTypesAsync();
        cancellationToken.ThrowIfCancellationRequested();
        var matches = types.Where(item => item.Value == accountType).Take(2).ToList();
        if (matches.Count != 1 || string.IsNullOrWhiteSpace(matches[0].Text)) throw new ArgumentException("SETTLEMENT_ACCOUNT_TYPE_INVALID");
        var name = ComposeSettlementName(matches[0].Text, days);
        if (name.Length > 32) throw new ArgumentException("SETTLEMENT_NAME_TOO_LONG");
        return name;
    }

    #endregion

    #region 获取结算字典
    /// <summary>使用现有字典服务；测试可替换为隔离字典。</summary>
    /// <returns>账款类型字典。</returns>
    protected virtual Task<List<EU.Core.Common.Helper.LovInfo>> GetMcpSettlementTypesAsync() => LovHelper.GetLovListAsync(Db, "SettlementAccountType");

    #endregion

    #region 组合结算名称
    /// <summary>共享旧入口与 MCP 的结算名称格式。</summary>
    /// <param name="text">账款类型显示文本。</param>
    /// <param name="days">账期天数。</param>
    /// <returns>派生名称。</returns>
    private static string ComposeSettlementName(string text, int? days) => days > 0 ? text + ",付款天数为" + days + "天" : text;
    #endregion
}
