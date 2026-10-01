using System.Reflection;
using System.Text.Json;
using EU.Core.Api.MCP.Models.Mcp;
using EU.Core.Api.MCP.Services;
using EU.Core.Common.Helper;
using EU.Core.IRepository.Base;
using EU.Core.Model;
using EU.Core.Model.Entity;
using EU.Core.Model.Insert;
using EU.Core.Model.Edit;
using EU.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using SqlSugar;
using Xunit;

namespace EU.Core.Tests;

/// <summary>四类工具的端到端离线规则测试。只使用 SQLite 内存库；替换标准持久化和字典，不访问业务库、元数据缓存或 Redis。</summary>
public sealed class BusinessMcpBasicDataTests
{
    private static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value);
    private static readonly string[] Entities = ["customer", "currency", "unit", "settlement_way"];

    private static object Values(string entity) => entity switch
    {
        "customer" => new { CustomerNo = "TEST", CustomerName = "测试客户", CustomerShortName = "客户", Remark = "original" },
        "currency" => new { CurrencyNo = "TEST", CurrencyName = "测试币别", Remark = "original" },
        "unit" => new { UnitNo = "TEST", UnitNames = "测试单位", Remark = "original" },
        _ => new { SettlementNo = "TEST", SettlementAccountType = "ImmediatePay", SettlementBillType = "Out", Days = 0, Remark = "original" }
    };
    private static string Name(string entity) => entity switch { "customer" => "测试客户", "currency" => "测试币别", "unit" => "测试单位", _ => "现付" };
    private static string Plural(string entity) => entity switch { "currency" => "currencies", "settlement_way" => "settlement_ways", _ => entity + "s" };

    [Theory]
    [InlineData("customer")]
    [InlineData("currency")]
    [InlineData("unit")]
    [InlineData("settlement_way")]
    public async Task All_four_tools_create_query_patch_and_soft_delete(string entity)
    {
        using var fixture = new Fixture();
        var created = await fixture.Call("create_" + entity, new { values = Values(entity) });
        Assert.True(created.GetProperty("succeeded").GetBoolean());
        var id = created.GetProperty("id").GetGuid();
        var page = await fixture.Call("query_" + Plural(entity), new { Code = "TEST", Keyword = Name(entity), PageSize = 1 });
        Assert.True(page.GetProperty("untrustedData").GetBoolean());
        var row = Assert.Single(page.GetProperty("page").GetProperty("data").EnumerateArray());
        Assert.Equal(id, row.GetProperty("ID").GetGuid());
        Assert.False(row.TryGetProperty("CompanyId", out _));
        await fixture.Call("update_" + entity, new { name = " " + Name(entity) + " ", values = new { Remark = "changed" } });
        page = await fixture.Call("query_" + Plural(entity), new { Code = "TEST" });
        row = Assert.Single(page.GetProperty("page").GetProperty("data").EnumerateArray());
        Assert.Equal("changed", row.GetProperty("Remark").GetString());
        if (entity == "settlement_way") Assert.Equal("现付", row.GetProperty("SettlementName").GetString());
        var deleted = await fixture.Call("delete_" + entity, new { name = Name(entity) });
        Assert.True(deleted.GetProperty("softDeleted").GetBoolean());
        page = await fixture.Call("query_" + Plural(entity), new { });
        Assert.Empty(page.GetProperty("page").GetProperty("data").EnumerateArray());
        Assert.Equal(1, fixture.Db.QueryableByObject(fixture.EntityType(entity)).Count());
    }

    [Theory]
    [InlineData("customer")]
    [InlineData("currency")]
    [InlineData("unit")]
    [InlineData("settlement_way")]
    public async Task Duplicate_create_unknown_field_missing_target_and_cancellation_do_not_write(string entity)
    {
        using var fixture = new Fixture();
        await fixture.Call("create_" + entity, new { values = Values(entity) });
        var error = await Assert.ThrowsAsync<ArgumentException>(() => fixture.Call("create_" + entity, new { values = Values(entity) }));
        Assert.StartsWith("BASIC_DATA_DUPLICATE", error.Message);
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Call("update_" + entity, new { name = Name(entity), values = new { CompanyId = Guid.NewGuid() } }));
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Call("update_" + entity, new { values = new { Remark = "bad" } }));
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Call("delete_" + entity, new { name = "not found" }));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Call("create_" + entity, new { values = Values(entity) }, new CancellationToken(true)));
        var row = Assert.Single((await fixture.Call("query_" + Plural(entity), new { })).GetProperty("page").GetProperty("data").EnumerateArray());
        Assert.Equal("original", row.GetProperty("Remark").GetString());
    }

    [Theory]
    [InlineData("customer")]
    [InlineData("currency")]
    [InlineData("unit")]
    [InlineData("settlement_way")]
    public async Task Active_or_inactive_business_reference_blocks_delete(string entity)
    {
        using var fixture = new Fixture();
        var id = (await fixture.Call("create_" + entity, new { values = Values(entity) })).GetProperty("id").GetGuid();
        switch (entity)
        {
            case "customer": fixture.Db.Insertable(new SdOrder { ID = Guid.NewGuid(), CustomerId = id, IsActive = false }).ExecuteCommand(); break;
            case "currency": fixture.Db.Insertable(new BdSupplier { ID = Guid.NewGuid(), CurrencyId = id, IsActive = false }).ExecuteCommand(); break;
            case "unit": fixture.Db.Insertable(new BdMaterial { ID = Guid.NewGuid(), WeightUnitId = id, IsActive = false }).ExecuteCommand(); break;
            default: fixture.Db.Insertable(new BdSupplier { ID = Guid.NewGuid(), SettlementWayId = id, IsActive = false }).ExecuteCommand(); break;
        }
        var error = await Assert.ThrowsAsync<ArgumentException>(() => fixture.Call("delete_" + entity, new { name = Name(entity) }));
        Assert.Equal("BASIC_DATA_IN_USE", error.Message);
    }

    [Fact]
    public async Task Customer_short_name_and_intersection_match_only_unique_target()
    {
        using var fixture = new Fixture();
        await fixture.Call("create_customer", new { values = Values("customer") });
        await fixture.Call("update_customer", new { shortName = " 客户 ", values = new { Consignee = "测试" } });
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Call("update_customer", new { name = "wrong", shortName = "客户", values = new { Remark = "bad" } }));
        fixture.Db.Insertable(new BdCustomer { ID = Guid.NewGuid(), CustomerName = "测试客户", CustomerShortName = "客户", IsActive = true }).ExecuteCommand();
        var error = await Assert.ThrowsAsync<ArgumentException>(() => fixture.Call("delete_customer", new { shortName = "客户" }));
        Assert.Equal("BASIC_DATA_TARGET_AMBIGUOUS", error.Message);
    }

    [Fact]
    public async Task Settlement_name_is_derived_and_partial_update_preserves_other_fields()
    {
        using var fixture = new Fixture();
        await fixture.Call("create_settlement_way", new { values = Values("settlement_way") });
        await fixture.Call("update_settlement_way", new { name = "现付", values = new { Days = 30 } });
        await fixture.Call("update_settlement_way", new { name = "现付,付款天数为30天", values = new { Remark = "note" } });
        var row = Assert.Single((await fixture.Call("query_settlement_ways", new { })).GetProperty("page").GetProperty("data").EnumerateArray());
        Assert.Equal(30, row.GetProperty("Days").GetInt32());
        Assert.Equal("现付,付款天数为30天", row.GetProperty("SettlementName").GetString());
        foreach (var patch in new object[] { new { Days = -1 }, new { SettlementBillType = "bad" }, new { SettlementAccountType = "bad" }, new { SettlementName = "manual" } })
            await Assert.ThrowsAsync<ArgumentException>(() => fixture.Call("update_settlement_way", new { name = "现付,付款天数为30天", values = patch }));
    }

    [Fact]
    public async Task Currency_query_filters_state_and_pages_and_duplicate_name_includes_inactive()
    {
        using var fixture = new Fixture();
        fixture.Db.Insertable(new[] {
            new BdCurrency { ID = Guid.NewGuid(), CurrencyNo = "A", CurrencyName = "Alpha", IsActive = true },
            new BdCurrency { ID = Guid.NewGuid(), CurrencyNo = "B", CurrencyName = "Beta", IsActive = true },
            new BdCurrency { ID = Guid.NewGuid(), CurrencyNo = "C", CurrencyName = "Disabled", IsActive = false },
            new BdCurrency { ID = Guid.NewGuid(), CurrencyNo = "D", CurrencyName = "Deleted", IsActive = true, IsDeleted = true }
        }).ExecuteCommand();
        var page = (await fixture.Call("query_currencies", new { PageSize = 1 })).GetProperty("page");
        Assert.Equal(2, page.GetProperty("dataCount").GetInt32());
        Assert.Single(page.GetProperty("data").EnumerateArray());
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Call("query_currencies", new { PageSize = 101 }));
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Call("create_currency", new { values = new { CurrencyName = " Disabled " } }));
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Call("update_currency", new { name = "Alpha", values = new { CurrencyName = "Beta" } }));
        await fixture.Call("create_currency", new { values = new { CurrencyName = "Deleted" } });
    }

    [Fact]
    public void All_new_schemas_publish_only_business_fields_and_correct_annotations()
    {
        using var fixture = new Fixture();
        var tools = Json(fixture.Service.GetAvailableTools()).GetProperty("tools").EnumerateArray().ToDictionary(t => t.GetProperty("name").GetString()!);
        Assert.Equal(20, tools.Count);
        foreach (var entity in Entities)
        {
            Assert.True(tools["query_" + Plural(entity)].GetProperty("annotations").GetProperty("readOnlyHint").GetBoolean());
            Assert.True(tools["delete_" + entity].GetProperty("annotations").GetProperty("destructiveHint").GetBoolean());
            var schema = tools["create_" + entity].GetProperty("inputSchema").GetProperty("properties").GetProperty("values");
            Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
            Assert.DoesNotContain(schema.GetProperty("properties").EnumerateObject(), p => p.Name is "ID" or "CompanyId" or "IsDeleted");
            Assert.NotEmpty(schema.GetProperty("required").EnumerateArray());
        }
    }

    private sealed class Fixture : IDisposable
    {
        public SqlSugarClient Db { get; } = new(new ConnectionConfig { ConnectionString = "Data Source=:memory:", DbType = DbType.Sqlite, IsAutoCloseConnection = false });
        public BusinessMcpService Service { get; }
        public Fixture()
        {
            Db.CodeFirst.InitTables(typeof(BdCustomer), typeof(BdCurrency), typeof(BdUnit), typeof(BdSettlementWay), typeof(BdSupplier),
                typeof(SdOrder), typeof(SdChangeOrder), typeof(SdShipOrder), typeof(SdOutOrder), typeof(SdReturnOrder),
                typeof(IvOut), typeof(BdCustomerDeliveryAddress), typeof(BdMaterial), typeof(PoOrder));
            Service = new BusinessMcpService(NullLogger<BusinessMcpService>.Instance, Repo<BdSupplier>(Db), null!,
                new CustomerService(Db), new CurrencyService(Db), new UnitService(Db), new SettlementWayService(Db));
        }
        public async Task<JsonElement> Call(string tool, object arguments, CancellationToken token = default)
        {
            var result = Assert.IsType<McpToolResult>(await Service.HandleToolCallAsync(Json(new { name = tool, arguments }), token));
            return JsonSerializer.Deserialize<JsonElement>(result.Content[0].Text);
        }
        public Type EntityType(string entity) => entity switch { "customer" => typeof(BdCustomer), "currency" => typeof(BdCurrency), "unit" => typeof(BdUnit), _ => typeof(BdSettlementWay) };
        public void Dispose() => Db.Dispose();
    }

    private static IBaseRepository<T> Repo<T>(ISqlSugarClient db) where T : BasePoco, new()
    {
        var repo = DispatchProxy.Create<IBaseRepository<T>, SupplierMcpDeletionTests.MemoryRepository>();
        ((SupplierMcpDeletionTests.MemoryRepository)(object)repo).Database = db;
        return repo;
    }
    private static async Task<Guid> Insert<T>(ISqlSugarClient db, object dto) where T : BasePoco, new()
    {
        var row = Json(dto).Deserialize<T>()!;
        row.ID = Guid.NewGuid(); row.IsActive = true;
        await db.Insertable(row).ExecuteCommandAsync();
        return row.ID;
    }
    private static async Task<bool> Patch<T>(ISqlSugarClient db, Guid id, object dto, List<string> columns) where T : BasePoco, new()
    {
        var row = Json(dto).Deserialize<T>()!; row.ID = id;
        return await db.Updateable(row).UpdateColumns(columns.ToArray()).ExecuteCommandAsync() == 1;
    }
    private static async Task<bool> Remove<T>(ISqlSugarClient db, object id) where T : BasePoco, new()
        => await db.Updateable<T>().SetColumns(row => row.IsDeleted == true).Where(row => row.ID == (Guid)id).ExecuteCommandAsync() == 1;

    private sealed class CustomerService(ISqlSugarClient db) : BdCustomerServices(Repo<BdCustomer>(db))
    {
        public override Task<Guid> Add(InsertBdCustomerInput entity, Guid? id = null) => Insert<BdCustomer>(db, entity);
        public override Task<bool> Update(Guid id, EditBdCustomerInput edit, List<string> columns = null, List<string> ignore = null, string where = null) => Patch<BdCustomer>(db, id, edit, columns);
        public override Task<bool> Delete(object id) => Remove<BdCustomer>(db, id);
    }

    private sealed class CurrencyService(ISqlSugarClient db) : BdCurrencyServices(Repo<BdCurrency>(db))
    {
        public override Task<Guid> Add(InsertBdCurrencyInput entity, Guid? id = null) => Insert<BdCurrency>(db, entity);
        public override Task<bool> Update(Guid id, EditBdCurrencyInput edit, List<string> columns = null, List<string> ignore = null, string where = null) => Patch<BdCurrency>(db, id, edit, columns);
        public override Task<bool> Delete(object id) => Remove<BdCurrency>(db, id);
    }

    private sealed class UnitService(ISqlSugarClient db) : BdUnitServices(Repo<BdUnit>(db))
    {
        public override Task<Guid> Add(InsertBdUnitInput entity, Guid? id = null) => Insert<BdUnit>(db, entity);
        public override Task<bool> Update(Guid id, EditBdUnitInput edit, List<string> columns = null, List<string> ignore = null, string where = null) => Patch<BdUnit>(db, id, edit, columns);
        public override Task<bool> Delete(object id) => Remove<BdUnit>(db, id);
    }

    private sealed class SettlementWayService(ISqlSugarClient db) : BdSettlementWayServices(Repo<BdSettlementWay>(db))
    {
        public override Task<Guid> Add(InsertBdSettlementWayInput entity, Guid? id = null) => Insert<BdSettlementWay>(db, entity);
        public override Task<bool> Update(Guid id, EditBdSettlementWayInput edit, List<string> columns = null, List<string> ignore = null, string where = null) => Patch<BdSettlementWay>(db, id, edit, columns);
        public override Task<bool> Delete(object id) => Remove<BdSettlementWay>(db, id);
        protected override Task<List<LovInfo>> GetMcpSettlementTypesAsync() => Task.FromResult(new List<LovInfo> { new() { Value = "ImmediatePay", Text = "现付" } });
    }
}
