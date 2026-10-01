using System.Reflection;
using System.Text.Json;
using EU.Core.Api.MCP.Controllers;
using EU.Core.Api.MCP.Interfaces;
using EU.Core.Api.MCP.Models.Mcp;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EU.Core.Tests;

/// <summary>统一入口协议与委派测试；使用替身，不连接数据库或执行真实删除。</summary>
public sealed class BusinessMcpControllerTests
{
    [Theory]
    [InlineData("initialize")]
    [InlineData("tools/list")]
    [InlineData("tools/call")]
    public async Task Protocol_requests_delegate_to_existing_supplier_service(string method)
    {
        var service = new SupplierStub();
        var controller = new BusinessController(service, NullLogger<BusinessController>.Instance);
        using var cancellation = new CancellationTokenSource();
        var response = Assert.IsType<OkObjectResult>(await controller.HandleAsync(new JsonRpcRequest
        {
            Id = 7, Method = method, Params = JsonSerializer.SerializeToElement(new { name = "delete_supplier", arguments = new { supplierNo = "TEST" } })
        }, cancellation.Token));
        var rpc = Assert.IsType<JsonRpcResponse>(response.Value);
        Assert.Null(rpc.Error);
        Assert.Equal(7, rpc.Id);
        Assert.Equal(method, service.Method);
        if (method == "tools/call") Assert.Equal(cancellation.Token, service.Token);
    }

    [Fact]
    public async Task Notification_is_accepted_and_unknown_method_is_protocol_error()
    {
        var controller = new BusinessController(new SupplierStub(), NullLogger<BusinessController>.Instance);
        Assert.IsType<AcceptedResult>(await controller.HandleAsync(new() { Method = "notifications/initialized" }, default));
        var response = Assert.IsType<OkObjectResult>(await controller.HandleAsync(new() { Method = "unknown", Id = 1 }, default));
        Assert.Equal(-32601, Assert.IsType<JsonRpcResponse>(response.Value).Error.Code);
    }

    [Fact]
    public void Business_endpoint_requires_authorization_while_health_remains_anonymous()
    {
        var type = typeof(BusinessController);
        Assert.Equal("/[controller]", type.GetCustomAttribute<RouteAttribute>().Template);
        Assert.NotEmpty(type.GetCustomAttributes<AuthorizeAttribute>(true));
        Assert.Empty(type.GetCustomAttributes<AllowAnonymousAttribute>(true));
        Assert.Empty(type.GetMethod(nameof(BusinessController.HandleAsync)).GetCustomAttributes<AllowAnonymousAttribute>(true));
        Assert.Equal("mcp", type.GetMethod(nameof(BusinessController.HandleAsync)).GetCustomAttribute<HttpPostAttribute>().Template);
        Assert.Single(type.GetMethod(nameof(BusinessController.HealthCheck)).GetCustomAttributes<AllowAnonymousAttribute>(true));
        var controller = new BusinessController(new SupplierStub(), NullLogger<BusinessController>.Instance);
        Assert.Equal("MCP API is running!", Assert.IsType<OkObjectResult>(controller.HealthCheck()).Value);
        Assert.Equal("/[controller]", typeof(SupplierController).GetCustomAttribute<RouteAttribute>().Template);
    }

    [Fact]
    public async Task Cancellation_propagates_without_success_response()
    {
        var controller = new BusinessController(new SupplierStub(), NullLogger<BusinessController>.Instance);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => controller.HandleAsync(new() { Method = "tools/call" }, cancellation.Token));
    }

    private sealed class SupplierStub : IBusinessMcpService
    {
        public string Method { get; private set; }
        public CancellationToken Token { get; private set; }
        public object HandleInitialize(JsonElement? parameters) { Method = "initialize"; return new { }; }
        public object GetAvailableTools() { Method = "tools/list"; return new { tools = Array.Empty<object>() }; }
        public Task<object> HandleToolCallAsync(JsonElement? parameters, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Method = "tools/call";
            Token = cancellationToken;
            return Task.FromResult<object>(new McpToolResult());
        }
        public Task<McpToolResult> QuerySuppliersAsync(SupplierQueryInput input, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
