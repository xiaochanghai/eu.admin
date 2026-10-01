using System.Reflection;
using EU.Core.IRepository.Base;
using EU.Core.Model.Entity;
using EU.Core.Services;
using SqlSugar;
using Xunit;

namespace EU.Core.Tests;

/// <summary>供应商名称定位与查重；仅使用隔离 SQLite 内存库，不访问项目数据库。</summary>
public sealed class SupplierNameMatchingTests
{
    private static SqlSugarClient Database()
    {
        var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = "Data Source=:memory:", DbType = DbType.Sqlite, IsAutoCloseConnection = false
        });
        db.CodeFirst.InitTables<BdSupplier>();
        return db;
    }

    private static BdSupplierServices Service(SqlSugarClient db)
    {
        var repository = DispatchProxy.Create<IBaseRepository<BdSupplier>, SupplierMcpDeletionTests.MemoryRepository>();
        ((SupplierMcpDeletionTests.MemoryRepository)(object)repository).Database = db;
        return new BdSupplierServices(repository);
    }

    private static BdSupplier Row(string full, string shortName, bool active = true, bool deleted = false) => new()
    {
        ID = Guid.NewGuid(), FullName = full, ShortName = shortName, IsActive = active, IsDeleted = deleted
    };

    [Fact]
    public async Task Names_match_exactly_and_both_must_match_same_active_row()
    {
        using var db = Database();
        var alpha = Row("Alpha", "A");
        db.Insertable(new[] { alpha, Row("Beta", "B"), Row("Inactive", "I", false), Row("Deleted", "D", true, true) }).ExecuteCommand();
        var service = Service(db);
        Assert.Equal(alpha.ID, (await service.ResolveSupplierByNamesAsync(" Alpha ", null)).ID);
        Assert.Equal(alpha.ID, (await service.ResolveSupplierByNamesAsync(null, "A")).ID);
        Assert.Equal(alpha.ID, (await service.ResolveSupplierByNamesAsync("Alpha", "A")).ID);
        foreach (var names in new[] { ("Alpha", "B"), ("Al", (string)null), ("Inactive", (string)null), ("Deleted", (string)null) })
            Assert.Equal("SUPPLIER_NOT_FOUND", (await Assert.ThrowsAsync<ArgumentException>(() => service.ResolveSupplierByNamesAsync(names.Item1, names.Item2))).Message);
    }

    [Theory]
    [InlineData("Same", null)]
    [InlineData(null, "S")]
    public async Task Ambiguous_name_is_rejected(string full, string shortName)
    {
        using var db = Database();
        db.Insertable(new[] { Row("Same", "S"), Row("Same", "S") }).ExecuteCommand();
        Assert.Equal("SUPPLIER_TARGET_AMBIGUOUS", (await Assert.ThrowsAsync<ArgumentException>(() => Service(db).ResolveSupplierByNamesAsync(full, shortName))).Message);
        Assert.Equal(2, await db.Queryable<BdSupplier>().CountAsync());
    }

    [Fact]
    public async Task Name_checks_reject_either_duplicate_include_inactive_and_exclude_self_or_deleted()
    {
        using var db = Database();
        var alpha = Row("Alpha", "A");
        db.Insertable(new[] { alpha, Row("Inactive", "I", false), Row("Deleted", "D", true, true), Row("Blank", "") }).ExecuteCommand();
        var service = Service(db);
        Assert.Equal("SUPPLIER_FULLNAME_DUPLICATE", (await Assert.ThrowsAsync<ArgumentException>(() => service.EnsureSupplierNamesAvailableAsync(" Alpha ", "New"))).Message);
        Assert.Equal("SUPPLIER_SHORTNAME_DUPLICATE", (await Assert.ThrowsAsync<ArgumentException>(() => service.EnsureSupplierNamesAvailableAsync("New", " A "))).Message);
        Assert.Equal("SUPPLIER_FULLNAME_DUPLICATE", (await Assert.ThrowsAsync<ArgumentException>(() => service.EnsureSupplierNamesAvailableAsync("Inactive", null))).Message);
        await service.EnsureSupplierNamesAvailableAsync("Alpha", "A", alpha.ID);
        await service.EnsureSupplierNamesAvailableAsync("Deleted", "D");
        await service.EnsureSupplierNamesAvailableAsync("New", " ");
        Assert.Equal(4, await db.Queryable<BdSupplier>().CountAsync());
    }

    [Fact]
    public async Task Empty_target_and_cancellation_fail_before_database_access()
    {
        var service = new BdSupplierServices(DispatchProxy.Create<IBaseRepository<BdSupplier>, SupplierMcpDeletionTests.NoDatabaseProxy>());
        await Assert.ThrowsAsync<ArgumentException>(() => service.ResolveSupplierByNamesAsync(" ", null));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ResolveSupplierByNamesAsync("Name", null, new CancellationToken(true)));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.EnsureSupplierNamesAvailableAsync("Name", null, null, new CancellationToken(true)));
    }
}
