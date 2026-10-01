using System.Text.Json;

namespace EU.Core.Services;

/// <summary>计量单位 MCP 业务规则，复用现有服务，不另建存储层。</summary>
public partial class BdUnitServices
{

    #region 查询计量单位
    /// <summary>查询有效且未删除的数据，输出白名单字段。</summary>
    /// <param name="input">分页和筛选条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>分页业务数据。</returns>
    public Task<PageModel<Dictionary<string, object>>> QueryForMcpAsync(BasicDataMcpQuery input, CancellationToken cancellationToken = default)
        => BasicDataMcpRules.Query<BdUnit, UnitMcpValues>(Db, input, "UnitNo", "UnitNames", null, cancellationToken);
    #endregion

    #region 新增计量单位
    /// <summary>应用层查重后调用标准 Add，不保证数据库级并发唯一性。</summary>
    /// <param name="input">业务字段。</param>
    /// <param name="cancellationToken">写入开始前取消令牌。</param>
    /// <returns>新增记录 ID。</returns>
    public async Task<Guid> CreateForMcpAsync(UnitMcpValues input, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BasicDataMcpRules.Validate(input, "UnitNames");
        var insert = BasicDataMcpRules.Convert<InsertBdUnitInput>(input);

        await BasicDataMcpRules.Unique<BdUnit>(Db, insert, null, cancellationToken, "UnitNo", "UnitNames");
        cancellationToken.ThrowIfCancellationRequested();
        var id = await Add(insert);
        if (id == Guid.Empty) throw new InvalidOperationException("BASIC_DATA_CREATE_FAILED");
        return id;
    }
    #endregion

    #region 修改计量单位
    /// <summary>唯一名称定位并合并补丁，修改前查重；未传字段保持原值。</summary>
    /// <param name="target">原名称目标。</param>
    /// <param name="values">业务字段补丁。</param>
    /// <param name="cancellationToken">写入开始前取消令牌。</param>
    /// <returns>修改记录 ID。</returns>
    public async Task<Guid> UpdateForMcpAsync(BasicDataMcpTarget target, JsonElement values, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BasicDataMcpRules.Parse<UnitMcpValues>(values);
        var existing = await BasicDataMcpRules.Resolve<BdUnit>(Db, target, "UnitNames", null, 64, cancellationToken);
        var edit = BasicDataMcpRules.Merge<EditBdUnitInput, UnitMcpValues>(existing, values);
        BasicDataMcpRules.Validate(edit, "UnitNames");
        var columns = values.EnumerateObject().Select(field => field.Name).ToList();

        await BasicDataMcpRules.Unique<BdUnit>(Db, edit, existing.ID, cancellationToken, "UnitNo", "UnitNames");
        cancellationToken.ThrowIfCancellationRequested();
        if (!await Update(existing.ID, edit, columns, null, null)) throw new ArgumentException("BASIC_DATA_UPDATE_FAILED");
        return existing.ID;
    }
    #endregion

    #region 删除计量单位
    /// <summary>拒绝已知未删除业务引用，沿用基类逻辑删除。</summary>
    /// <param name="target">原名称目标。</param>
    /// <param name="cancellationToken">删除开始前取消令牌。</param>
    /// <returns>逻辑删除记录 ID。</returns>
    public async Task<Guid> DeleteForMcpAsync(BasicDataMcpTarget target, CancellationToken cancellationToken = default)
    {
        var existing = await BasicDataMcpRules.Resolve<BdUnit>(Db, target, "UnitNames", null, 64, cancellationToken);
        var id = existing.ID;
        await BasicDataMcpRules.NoReference<BdMaterial>(Db, row => row.UnitId == id || row.WeightUnitId == id, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (!await Delete((object)id)) throw new ArgumentException("BASIC_DATA_DELETE_FAILED");
        return id;
    }
    #endregion
}
