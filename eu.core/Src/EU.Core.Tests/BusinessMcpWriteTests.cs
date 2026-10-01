using System.Reflection;
using System.Text.Json;
using EU.Core.Api.MCP.Interfaces;
using EU.Core.Api.MCP.Models.Mcp;
using EU.Core.Api.MCP.Services;
using EU.Core.IServices;
using EU.Core.IRepository.Base;
using Microsoft.Extensions.Logging.Abstractions;
using EU.Core.Model.Entity;
using EU.Core.Model.Edit;
using EU.Core.Model.Insert;
using EU.Core.Model.ViewModels.Extend;
using Xunit;

namespace EU.Core.Tests;

/// <summary>直接写工具适配测试；业务接口使用替身，不连接数据库。</summary>
public sealed class BusinessMcpWriteTests
{
    private static JsonElement Call(string name, object arguments) => JsonSerializer.SerializeToElement(new { name, arguments });

    private static BusinessMcpService CreateService(IBdSupplierServices business) => new(
        NullLogger<BusinessMcpService>.Instance, DispatchProxy.Create<IBaseRepository<BdSupplier>, UnusedRepository>(), business);

    public class UnusedRepository : DispatchProxy
    {
        protected override object Invoke(MethodInfo method, object[] args) => throw new InvalidOperationException("MCP adapter must use the business service, not its repository.");
    }

    [Fact]
    public async Task Create_calls_existing_business_add_and_returns_identifier()
    {
        var business = DispatchProxy.Create<IBdSupplierServices, BusinessProxy>();
        var service = CreateService(business);
        var result = Assert.IsType<McpToolResult>(await service.HandleToolCallAsync(Call("create_supplier", new { values = new { FullName = "Test supplier" } }), default));
        var proxy = (BusinessProxy)(object)business;
        Assert.Equal("Test supplier", proxy.Insert.FullName);
        Assert.Contains(proxy.Id.ToString(), result.Content[0].Text);
        Assert.DoesNotContain("module_edit", result.Content[0].Text);
        Assert.Equal(1, proxy.Writes);
    }

    [Fact]
    public async Task Update_preserves_missing_fields_and_sends_only_selected_columns()
    {
        var business = DispatchProxy.Create<IBdSupplierServices, BusinessProxy>();
        var proxy = (BusinessProxy)(object)business;
        await CreateService(business).HandleToolCallAsync(Call("update_supplier",
            new { supplierId = proxy.Id, values = new { ShortName = "Changed" } }), default);
        Assert.Equal("Existing", proxy.Edit.FullName);
        Assert.Equal("Changed", proxy.Edit.ShortName);
        Assert.Equal(new[] { "ShortName" }, proxy.Columns);
        Assert.Equal(1, proxy.Writes);
    }

    [Theory]
    [InlineData("CurrencyId")]
    [InlineData("TaxType")]
    [InlineData("SupplierClassId")]
    [InlineData("SupplierLevelId")]
    [InlineData("DistrictId")]
    [InlineData("SettlementWayId")]
    [InlineData("DeliveryWayId")]
    public async Task Create_rejects_fields_outside_published_seven_fields(string field)
    {
        var business = DispatchProxy.Create<IBdSupplierServices, BusinessProxy>();
        var values = new Dictionary<string, object> { ["FullName"] = "Test", [field] = null };
        await Assert.ThrowsAsync<ArgumentException>(() => CreateService(business).HandleToolCallAsync(Call("create_supplier", new { values }), default));
        Assert.Equal(0, ((BusinessProxy)(object)business).Writes);
    }

    [Fact]
    public async Task Create_passes_all_seven_fields_to_business_service()
    {
        var business = DispatchProxy.Create<IBdSupplierServices, BusinessProxy>();
        await CreateService(business).HandleToolCallAsync(Call("create_supplier", new { values = new
        {
            SupplierNo = "TEST", FullName = "Test supplier", ShortName = "Test", TaxRate = 0.13m,
            Contact = "Test contact", Phone = "000", Remark = "Test remark"
        } }), default);
        var saved = ((BusinessProxy)(object)business).Insert;
        Assert.Equal("TEST", saved.SupplierNo);
        Assert.Equal("Test supplier", saved.FullName);
        Assert.Equal("Test", saved.ShortName);
        Assert.Equal(0.13m, saved.TaxRate);
        Assert.Equal("Test contact", saved.Contact);
        Assert.Equal("000", saved.Phone);
        Assert.Equal("Test remark", saved.Remark);
        Assert.Null(saved.CurrencyId);
    }

    [Theory]
    [InlineData("{\"values\":{\"FullName\":\"\"}}", "create_supplier")]
    [InlineData("{\"values\":{\"FullName\":\"First\",\"FullName\":\"Second\"}}", "create_supplier")]
    [InlineData("{\"values\":{\"FullName\":\"Test\",\"CompanyId\":\"x\"}}", "create_supplier")]
    [InlineData("{\"values\":{\"FullName\":\"Test\",\"ID\":\"x\"}}", "create_supplier")]
    [InlineData("{\"values\":{\"TaxRate\":\"invalid\"}}", "create_supplier")]
    [InlineData("{\"values\":{\"FullName\":\"Test\"}}", "update_supplier")]
    [InlineData("{\"supplierId\":\"00000000-0000-0000-0000-000000000000\",\"values\":{}}", "update_supplier")]
    public async Task Invalid_input_never_reaches_business_write(string json, string tool)
    {
        var business = DispatchProxy.Create<IBdSupplierServices, BusinessProxy>();
        await Assert.ThrowsAsync<ArgumentException>(() => CreateService(business)
            .HandleToolCallAsync(Call(tool, JsonSerializer.Deserialize<JsonElement>(json)), default));
        Assert.Equal(0, ((BusinessProxy)(object)business).Writes);
    }

    [Fact]
    public async Task Missing_target_and_cancellation_do_not_write()
    {
        var business = DispatchProxy.Create<IBdSupplierServices, BusinessProxy>();
        var proxy = (BusinessProxy)(object)business;
        proxy.Missing = true;
        var service = CreateService(business);
        await Assert.ThrowsAsync<ArgumentException>(() => service.HandleToolCallAsync(Call("update_supplier", new { supplierId = proxy.Id, values = new { FullName = "Test" } }), default));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.HandleToolCallAsync(Call("create_supplier", new { values = new { FullName = "Test" } }), new CancellationToken(true)));
        Assert.Equal(0, proxy.Writes);
    }

    [Fact]
    public void Independent_service_discovers_four_tools_and_marks_writes()
    {
        var service = CreateService(DispatchProxy.Create<IBdSupplierServices, BusinessProxy>());
        var tools = JsonSerializer.SerializeToElement(service.GetAvailableTools()).GetProperty("tools");
        Assert.Equal(4, tools.GetArrayLength());
        var byName = tools.EnumerateArray().ToDictionary(tool => tool.GetProperty("name").GetString()!);
        Assert.False(byName["create_supplier"].GetProperty("annotations").GetProperty("readOnlyHint").GetBoolean());
        Assert.True(byName["update_supplier"].GetProperty("inputSchema").GetProperty("properties").TryGetProperty("supplierId", out _));
        Assert.True(byName["query_suppliers"].GetProperty("annotations").GetProperty("readOnlyHint").GetBoolean());
        Assert.True(byName["delete_supplier"].GetProperty("annotations").GetProperty("destructiveHint").GetBoolean());
        Assert.DoesNotContain(typeof(BusinessMcpService).GetConstructors().Single().GetParameters(), parameter => parameter.ParameterType == typeof(ISupplierService));
        Assert.Equal(typeof(BaseService<BusinessMcpService, BdSupplier>), typeof(BusinessMcpService).BaseType);
        Assert.True(service.CanHandle("create_supplier"));
        Assert.True(service.CanHandle("update_supplier"));
        Assert.Equal(typeof(BaseService<BusinessMcpService, BdSupplier>), typeof(BusinessMcpService).GetMethod("ExecuteToolAsync").DeclaringType);
        Assert.Equal(typeof(BaseService<BusinessMcpService, BdSupplier>), typeof(BusinessMcpService).GetMethod("GetAvailableTools").DeclaringType);
        var createSchema = byName["create_supplier"].GetProperty("inputSchema");
        Assert.False(createSchema.GetProperty("additionalProperties").GetBoolean());
        var values = createSchema.GetProperty("properties").GetProperty("values");
        Assert.Equal("object", values.GetProperty("type").GetString());
        Assert.False(values.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal(1, values.GetProperty("minProperties").GetInt32());
        Assert.Equal("FullName", values.GetProperty("required")[0].GetString());
        var fields = values.GetProperty("properties");
        Assert.False(fields.TryGetProperty("CompanyId", out _));
        Assert.False(fields.TryGetProperty("ID", out _));
        Assert.Equal(32, fields.GetProperty("FullName").GetProperty("maxLength").GetInt32());
        Assert.Equal(new[] { "Contact", "FullName", "Phone", "Remark", "ShortName", "SupplierNo", "TaxRate" }, fields.EnumerateObject().Select(field => field.Name).OrderBy(name => name, StringComparer.Ordinal));
        Assert.Equal("联系人", fields.GetProperty("Contact").GetProperty("description").GetString());
        Assert.Equal("uuid", byName["update_supplier"].GetProperty("inputSchema").GetProperty("properties").GetProperty("values").GetProperty("properties").GetProperty("CurrencyId").GetProperty("format").GetString());
        Assert.Equal("number", fields.GetProperty("TaxRate").GetProperty("type")[0].GetString());
        Assert.Equal("null", fields.GetProperty("TaxRate").GetProperty("type")[1].GetString());
        Assert.Equal(0, byName["update_supplier"].GetProperty("inputSchema").GetProperty("properties").GetProperty("values").GetProperty("required").GetArrayLength());
    }

    public class BusinessProxy : DispatchProxy
    {
        public Guid Id = Guid.NewGuid();
        public int Writes;
        public bool Missing;
        public InsertBdSupplierInput Insert;
        public EditBdSupplierInput Edit;
        public List<string> Columns;
        public SupplierQueryInput Query;
        public string DeleteId;
        public CancellationToken Token;
        protected override object Invoke(MethodInfo method, object[] args)
        {
            if (method.Name == "QuerySuppliersAsync") { Query = Assert.IsType<SupplierQueryInput>(args[0]); Token = (CancellationToken)args[1]; return Task.FromResult(new EU.Core.Model.PageModel<SupplierQueryItem>()); }
            if (method.Name == "DeleteSupplierForMcpAsync") { DeleteId = (string)args[0]; Token = (CancellationToken)args[2]; Writes++; return Task.FromResult(true); }
            if (method.Name == "QuerySingle") return Task.FromResult(Missing ? null : new BdSupplier { ID = Id, FullName = "Existing", ShortName = "Old", IsActive = true });
            if (method.Name == "Add") { Insert = Assert.IsType<InsertBdSupplierInput>(args[0]); Writes++; return Task.FromResult(Id); }
            if (method.Name == "Update") { Assert.Equal(Id, args[0]); Edit = Assert.IsType<EditBdSupplierInput>(args[1]); Columns = Assert.IsType<List<string>>(args[2]); Writes++; return Task.FromResult(true); }
            throw new NotSupportedException(method.Name);
        }
    }

    [Fact]
    public async Task Query_and_delete_dispatch_directly_to_business_service()
    {
        var business = DispatchProxy.Create<IBdSupplierServices, BusinessProxy>();
        var proxy = (BusinessProxy)(object)business;
        var service = CreateService(business);
        using var cancellation = new CancellationTokenSource();
        var query = Assert.IsType<McpToolResult>(await service.HandleToolCallAsync(Call("query_suppliers", new { Keyword = "test" }), cancellation.Token));
        Assert.Equal("test", proxy.Query.Keyword);
        Assert.Contains("supplier_query", query.Content[0].Text);
        Assert.Equal(cancellation.Token, proxy.Token);
        Assert.Equal(0, proxy.Writes);
        await service.HandleToolCallAsync(Call("delete_supplier", new { supplierId = proxy.Id.ToString() }), cancellation.Token);
        Assert.Equal(proxy.Id.ToString(), proxy.DeleteId);
        Assert.Equal(cancellation.Token, proxy.Token);
        Assert.Equal(1, proxy.Writes);
    }

    [Theory]
    [InlineData("{\"name\":\"query_suppliers\"}")]
    [InlineData("{\"name\":\"query_suppliers\",\"arguments\":null}")]
    [InlineData("{\"name\":\"query_suppliers\",\"arguments\":{}}")]
    public async Task Query_without_conditions_uses_default_pagination(string json)
    {
        var business = DispatchProxy.Create<IBdSupplierServices, BusinessProxy>();
        using var cancellation = new CancellationTokenSource();
        var result = await CreateService(business).HandleToolCallAsync(JsonSerializer.Deserialize<JsonElement>(json), cancellation.Token);
        Assert.IsType<McpToolResult>(result);
        var proxy = (BusinessProxy)(object)business;
        Assert.Equal(1, proxy.Query.PageIndex);
        Assert.Equal(20, proxy.Query.PageSize);
        Assert.Null(proxy.Query.SupplierNo);
        Assert.Null(proxy.Query.Keyword);
        Assert.Equal(cancellation.Token, proxy.Token);
        Assert.Equal(0, proxy.Writes);
    }

    [Theory]
    [InlineData("create_supplier")]
    [InlineData("update_supplier")]
    [InlineData("delete_supplier")]
    public async Task Writes_without_arguments_remain_rejected(string tool)
    {
        var business = DispatchProxy.Create<IBdSupplierServices, BusinessProxy>();
        await Assert.ThrowsAsync<ArgumentException>(() => CreateService(business).HandleToolCallAsync(JsonSerializer.SerializeToElement(new { name = tool }), default));
        Assert.Equal(0, ((BusinessProxy)(object)business).Writes);
    }

    [Theory]
    [InlineData("query_suppliers")]
    [InlineData("delete_supplier")]
    public async Task Read_and_delete_reject_unknown_fields_before_business_calls(string tool)
    {
        var business = DispatchProxy.Create<IBdSupplierServices, BusinessProxy>();
        await Assert.ThrowsAsync<ArgumentException>(() => CreateService(business).HandleToolCallAsync(Call(tool, new { CompanyId = "invalid" }), default));
        var proxy = (BusinessProxy)(object)business;
        Assert.Null(proxy.Query);
        Assert.Equal(0, proxy.Writes);
    }

}
