#nullable enable
using System.Reflection;
using EU.Core.IRepository.Base;
using EU.Core.Model.Entity;
using EU.Core.Services;
using SqlSugar;
using Xunit;

namespace EU.Core.Tests;

/// <summary>供应商匿名查询/删除及目标保护测试；仅使用隔离内存库或禁止访问的仓储替身。</summary>
public sealed class SupplierMcpDeletionTests
{
    [Fact]
    public async Task Anonymous_query_returns_multiple_companies_but_excludes_inactive_and_deleted()
    {
        using var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = "Data Source=:memory:", DbType = DbType.Sqlite, IsAutoCloseConnection = false
        });
        db.CodeFirst.InitTables<BdSupplier>();
        db.Insertable(new[]
        {
            new BdSupplier { ID = Guid.NewGuid(), CompanyId = Guid.NewGuid(), SupplierNo = "A", FullName = "Alpha", IsActive = true, IsDeleted = false },
            new BdSupplier { ID = Guid.NewGuid(), CompanyId = Guid.NewGuid(), SupplierNo = "B", FullName = "Beta", IsActive = true, IsDeleted = false },
            new BdSupplier { ID = Guid.NewGuid(), SupplierNo = "C", IsActive = false, IsDeleted = false },
            new BdSupplier { ID = Guid.NewGuid(), SupplierNo = "D", IsActive = true, IsDeleted = true }
        }).ExecuteCommand();
        var repository = DispatchProxy.Create<IBaseRepository<BdSupplier>, MemoryRepository>();
        ((MemoryRepository)(object)repository).Database = db;
        var service = new BdSupplierServices(repository);
        var page = await service.QuerySuppliersAsync(new EU.Core.Model.ViewModels.Extend.SupplierQueryInput());
        Assert.Equal(2, page.dataCount);
        Assert.Equal(new[] { "A", "B" }, page.data.Select(x => x.SupplierNo));
        var filtered = await service.QuerySuppliersAsync(new EU.Core.Model.ViewModels.Extend.SupplierQueryInput { SupplierNo = "B" });
        Assert.Equal("B", Assert.Single(filtered.data).SupplierNo);
        // 无身份/权限表的隔离库中可按唯一目标删除；不会删除另一公司的记录。
        Assert.True(await service.DeleteSupplierForMcpAsync(null!, "B"));
        Assert.True(await db.Queryable<BdSupplier>().Where(x => x.SupplierNo == "A").AnyAsync());
        Assert.False(await db.Queryable<BdSupplier>().Where(x => x.SupplierNo == "B").AnyAsync());
    }

    public class MemoryRepository : DispatchProxy
    {
        public ISqlSugarClient Database { get; set; } = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method?.Name == "get_Db"
            ? Database : throw new InvalidOperationException("Unexpected repository call");
    }

    [Fact]
    public async Task Query_does_not_access_identity()
    {
        var service = Create();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service
            .QuerySuppliersAsync(new EU.Core.Model.ViewModels.Extend.SupplierQueryInput()));
        Assert.Equal("Test forbids repository access: get_Db", error.Message);
    }

    [Fact]
    public async Task Query_without_user_id_reaches_database()
    {
        var service = Create();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service
            .QuerySuppliersAsync(new EU.Core.Model.ViewModels.Extend.SupplierQueryInput()));
        Assert.Equal("Test forbids repository access: get_Db", error.Message);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", " ")]
    [InlineData("invalid", "supplier")]
    [InlineData("00000000-0000-0000-0000-000000000000", null)]
    [InlineData(null, "123456789012345678901234567890123")]
    public async Task Invalid_target_is_rejected_before_database_access(string? id, string? number)
    {
        var service = Create();
        var error = await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteSupplierForMcpAsync(id!, number!));
        Assert.Equal("SUPPLIER_TARGET_INVALID", error.Message);
    }

    [Fact]
    public async Task Valid_delete_target_reaches_database_without_identity()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Create()
            .DeleteSupplierForMcpAsync(Guid.NewGuid().ToString(), null!));
        Assert.Equal("Test forbids repository access: get_Db", error.Message);
    }

    [Fact]
    public async Task Ambiguous_number_does_not_delete_any_rows()
    {
        using var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = "Data Source=:memory:", DbType = DbType.Sqlite, IsAutoCloseConnection = false
        });
        db.CodeFirst.InitTables<BdSupplier>();
        var firstId = Guid.NewGuid();
        db.Insertable(new[]
        {
            new BdSupplier { ID = firstId, SupplierNo = "DUP", CompanyId = Guid.NewGuid(), IsDeleted = false },
            new BdSupplier { ID = Guid.NewGuid(), SupplierNo = "DUP", CompanyId = Guid.NewGuid(), IsDeleted = false }
        }).ExecuteCommand();
        var repository = DispatchProxy.Create<IBaseRepository<BdSupplier>, MemoryRepository>();
        ((MemoryRepository)(object)repository).Database = db;
        var service = new BdSupplierServices(repository);
        await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteSupplierForMcpAsync(null!, "DUP"));
        Assert.Equal(2, await db.Queryable<BdSupplier>().CountAsync());
        Assert.False(await service.DeleteSupplierForMcpAsync(firstId.ToString(), "wrong"));
        Assert.True(await service.DeleteSupplierForMcpAsync(firstId.ToString(), "DUP"));
        Assert.Equal(1, await db.Queryable<BdSupplier>().CountAsync());
    }

    [Fact]
    public async Task Cancelled_request_cannot_access_database()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Create()
            .DeleteSupplierForMcpAsync(Guid.NewGuid().ToString(), null!, cancellation.Token));
    }

    private static BdSupplierServices Create() => new(DispatchProxy.Create<IBaseRepository<BdSupplier>, NoDatabaseProxy>());

    public class NoDatabaseProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            throw new InvalidOperationException("Test forbids repository access: " + method?.Name);
    }

}
