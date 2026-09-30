#nullable enable
using System.Reflection;
using System.Text.Json;
using EU.Core.Api.MCP.Attributes;
using EU.Core.Api.MCP.Interfaces;
using EU.Core.Api.MCP.Models.Mcp;
using EU.Core.Api.MCP.Services.BD;
using EU.Core.IRepository.Base;
using EU.Core.Model.Entity;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EU.Core.Tests;

/// <summary>纯离线反射分发回归，不连接数据库或外部 HTTP。</summary>
public sealed class McpToolDispatchTests
{
    private static JsonElement Request(string name, object? arguments = null) =>
        JsonSerializer.SerializeToElement(new { name, arguments = arguments ?? new { } });

    [Theory]
    [InlineData("legacy")]
    [InlineData("legacy_async")]
    public async Task Single_argument_tools_remain_callable(string name)
    {
        var result = await new Tools().HandleToolCallAsync(Request(name), CancellationToken.None);
        Assert.IsType<McpToolResult>(result);
    }

    [Fact]
    public async Task Request_token_is_passed_to_two_argument_tool()
    {
        var service = new Tools();
        using var cancellation = new CancellationTokenSource();
        await service.HandleToolCallAsync(Request("cancellable"), cancellation.Token);
        Assert.Equal(cancellation.Token, service.Received);
    }

    [Fact]
    public async Task Pre_cancelled_request_does_not_invoke_tool()
    {
        var service = new Tools();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.HandleToolCallAsync(Request("cancellable"), cancellation.Token));
        Assert.False(service.Invoked);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid_async")]
    public async Task Parameter_errors_preserve_type_for_protocol_boundary(string name)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => new Tools().HandleToolCallAsync(Request(name), CancellationToken.None));
    }

    [Fact]
    public async Task Tool_cancellation_is_not_wrapped_as_internal_failure()
    {
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new Tools().HandleToolCallAsync(Request("cancel"), CancellationToken.None));
    }

    [Fact]
    public async Task Direct_execution_discovers_tools_and_preserves_old_call_shape()
    {
        Assert.IsType<McpToolResult>(await new Tools().ExecuteToolAsync("legacy", default, new { }));
    }

    [Fact]
    public async Task Supplier_uses_base_dispatch_for_navigation_and_tool_owned_parsing()
    {
        var service = new SupplierService(NullLogger<SupplierService>.Instance, Repository(), null!, null!, null!);
        Assert.Equal(7, service.GetTools().Count());
        Assert.IsType<McpToolResult>(await service.HandleToolCallAsync(Request("get_supplier"), CancellationToken.None));
        // 未知字段必须在具体工具解析处拒绝，不能进入 null 业务依赖。
        await Assert.ThrowsAsync<ArgumentException>(() => service.HandleToolCallAsync(Request("query_suppliers", new { CompanyId = "forbidden" }), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.HandleToolCallAsync(Request("delete_supplier", new { CompanyId = "forbidden" }), CancellationToken.None));
        Assert.Equal(typeof(BaseService<SupplierService, BdSupplier>), typeof(SupplierService).GetMethod("HandleToolCallAsync")!.DeclaringType);
    }

    private static IBaseRepository<BdSupplier> Repository() => DispatchProxy.Create<IBaseRepository<BdSupplier>, NoDatabase>();

    public class NoDatabase : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => throw new InvalidOperationException("Database access forbidden");
    }

    public sealed class Tools() : BaseService<Tools, BdSupplier>(NullLogger<Tools>.Instance, Repository())
    {
        public CancellationToken Received { get; private set; }
        public bool Invoked { get; private set; }
        [McpTool("legacy", "legacy")]
        public McpToolResult Legacy(object arguments) => new();
        [McpTool("legacy_async", "legacy async")]
        public Task<McpToolResult> LegacyAsync(object arguments) => Task.FromResult(new McpToolResult());
        [McpTool("cancellable", "token")]
        public Task<McpToolResult> Cancellable(object arguments, CancellationToken cancellationToken)
        {
            Invoked = true;
            Received = cancellationToken;
            return Task.FromResult(new McpToolResult());
        }
        [McpTool("invalid", "invalid")]
        public McpToolResult Invalid(object arguments) => throw new ArgumentException("invalid");
        [McpTool("invalid_async", "invalid async")]
        public Task<McpToolResult> InvalidAsync(object arguments) => Task.FromException<McpToolResult>(new ArgumentException("invalid"));
        [McpTool("cancel", "cancel")]
        public Task<McpToolResult> Cancel(object arguments, CancellationToken cancellationToken) => Task.FromCanceled<McpToolResult>(new CancellationToken(true));
    }
}
