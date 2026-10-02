#nullable enable
using System.Reflection;
using System.Text.Json;
using EU.Core.Api.MCP.Attributes;
using EU.Core.Api.MCP.Interfaces;
using EU.Core.Api.MCP.Models.Mcp;
using EU.Core.Api.MCP.Services;
using EU.Core.IRepository.Base;
using EU.Core.IServices;
using EU.Core.Model.Entity;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EU.Core.Tests;

/// <summary>验证原始 JSON 数字经过 MCP 动态分发后保留业务精度；仅使用替身，不访问数据库。</summary>
public sealed class McpNumericPrecisionTests
{
    private const string PreciseRate = "0.1234564999999999999999999999";
    private static IBaseRepository<BdSupplier> Repository() => DispatchProxy.Create<IBaseRepository<BdSupplier>, BusinessMcpWriteTests.UnusedRepository>();
    private static JsonElement Call(string tool, string arguments) => JsonSerializer.Deserialize<JsonElement>("{\"name\":\"" + tool + "\",\"arguments\":" + arguments + "}");

    [Theory]
    [InlineData(PreciseRate)]
    [InlineData("-0.1234564999999999999999999999")]
    [InlineData("1e-28")]
    [InlineData("0.13")]
    [InlineData("79228162514264337593543950335")]
    [InlineData("-79228162514264337593543950335")]
    public void Decimal_numbers_preserve_their_value(string number)
    {
        var json = JsonSerializer.Deserialize<JsonElement>(number);
        object converted = BaseService<EchoService, BdSupplier>.ConvertToDynamic(json)!;
        Assert.Equal(json.GetDecimal(), Assert.IsType<decimal>(converted));
    }

    [Theory]
    [InlineData("42", typeof(int))]
    [InlineData("2147483648", typeof(long))]
    [InlineData("9223372036854775807", typeof(long))]
    public void Integer_conversion_remains_compatible(string number, Type expectedType)
    {
        var json = JsonSerializer.Deserialize<JsonElement>(number);
        object converted = BaseService<EchoService, BdSupplier>.ConvertToDynamic(json)!;
        Assert.Equal(expectedType, converted.GetType());
        Assert.Equal(json.GetInt64(), Convert.ToInt64(converted));
    }

    [Fact]
    public void Numbers_outside_decimal_range_keep_double_fallback()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("1e100");
        object converted = BaseService<EchoService, BdSupplier>.ConvertToDynamic(json)!;
        Assert.Equal(json.GetDouble(), Assert.IsType<double>(converted));
    }

    [Fact]
    public async Task Nested_objects_and_arrays_preserve_precision_through_tool_dispatch()
    {
        var arguments = "{\"values\":{\"rate\":" + PreciseRate + "},\"items\":[" + PreciseRate + ",{\"rate\":1e-28}]}";
        var result = Assert.IsType<McpToolResult>(await new EchoService().HandleToolCallAsync(Call("echo_numbers", arguments), default));
        var echoed = JsonSerializer.Deserialize<JsonElement>(result.Content[0].Text);
        var expected = JsonSerializer.Deserialize<JsonElement>(PreciseRate).GetDecimal();
        Assert.Equal(expected, echoed.GetProperty("values").GetProperty("rate").GetDecimal());
        Assert.Equal(expected, echoed.GetProperty("items")[0].GetDecimal());
        Assert.Equal(0.0000000000000000000000000001m, echoed.GetProperty("items")[1].GetProperty("rate").GetDecimal());
    }

    [Theory]
    [InlineData("create_supplier")]
    [InlineData("update_supplier")]
    public async Task Supplier_writes_pass_the_exact_tax_rate_to_business_service(string tool)
    {
        var business = DispatchProxy.Create<IBdSupplierServices, BusinessMcpWriteTests.BusinessProxy>();
        var service = new BusinessMcpService(NullLogger<BusinessMcpService>.Instance, Repository(), business, null!, null!, null!, null!);
        var target = tool == "update_supplier" ? "\"fullName\":\"Existing\"," : "";
        var arguments = "{" + target + "\"values\":{\"FullName\":\"Precision supplier\",\"TaxRate\":" + PreciseRate + "}}";
        Assert.IsType<McpToolResult>(await service.HandleToolCallAsync(Call(tool, arguments), default));
        var proxy = (BusinessMcpWriteTests.BusinessProxy)(object)business;
        var actual = tool == "create_supplier" ? proxy.Insert.TaxRate : proxy.Edit.TaxRate;
        Assert.Equal(JsonSerializer.Deserialize<JsonElement>(PreciseRate).GetDecimal(), actual);
        Assert.Equal(1, proxy.Writes);
    }

    [Theory]
    [InlineData("create_customer")]
    [InlineData("update_customer")]
    public async Task Customer_writes_pass_the_exact_tax_rate_to_business_service(string tool)
    {
        var business = DispatchProxy.Create<IBdCustomerServices, CustomerProxy>();
        var service = new BusinessMcpService(NullLogger<BusinessMcpService>.Instance, Repository(), null!, business, null!, null!, null!);
        var target = tool == "update_customer" ? "\"name\":\"Existing\"," : "";
        var arguments = "{" + target + "\"values\":{\"CustomerName\":\"Precision customer\",\"TaxRate\":" + PreciseRate + "}}";
        using var cancellation = new CancellationTokenSource();
        Assert.IsType<McpToolResult>(await service.HandleToolCallAsync(Call(tool, arguments), cancellation.Token));
        var proxy = (CustomerProxy)(object)business;
        Assert.Equal(JsonSerializer.Deserialize<JsonElement>(PreciseRate).GetDecimal(), proxy.TaxRate);
        Assert.Equal(cancellation.Token, proxy.Token);
        Assert.Equal(1, proxy.Writes);
    }

    public class CustomerProxy : DispatchProxy
    {
        public decimal? TaxRate { get; private set; }
        public CancellationToken Token { get; private set; }
        public int Writes { get; private set; }

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            switch (method?.Name)
            {
                case "CreateForMcpAsync":
                    TaxRate = Assert.IsType<CustomerMcpValues>(args![0]).TaxRate;
                    Token = Assert.IsType<CancellationToken>(args[1]);
                    break;
                case "UpdateForMcpAsync":
                    TaxRate = Assert.IsType<JsonElement>(args![1]).GetProperty("TaxRate").GetDecimal();
                    Token = Assert.IsType<CancellationToken>(args[2]);
                    break;
                default:
                    throw new InvalidOperationException("Unexpected business call: " + method?.Name);
            }
            Writes++;
            return Task.FromResult(Guid.NewGuid());
        }
    }

    public sealed class EchoService() : BaseService<EchoService, BdSupplier>(NullLogger<EchoService>.Instance, Repository())
    {
        [McpTool("echo_numbers", "回显数字参数，仅用于离线测试")]
        public McpToolResult Echo(object arguments) => new()
        {
            Content = [new McpContent { Type = "text", Text = JsonSerializer.Serialize(arguments) }]
        };
    }
}
