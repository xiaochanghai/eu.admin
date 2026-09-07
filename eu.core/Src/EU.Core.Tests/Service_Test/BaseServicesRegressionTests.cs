using EU.Core.Services.BASE;
using EU.Core.Common.Enums;
using SqlSugar;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public class BaseServicesRegressionTests
{
    [Theory]
    [InlineData("Rows;DROP TABLE Rows", "Code")]
    [InlineData("Rows", "Code' OR 1=1")]
    public void InvalidIdentifiersAreRejectedBeforeDatabaseAccess(string table, string field)
    {
        Assert.Throws<ArgumentException>(() => BaseServices<Row, Row, Row, Row>.CheckCodeExist(
            table, field, "value", ModifyType.Add, null, "Code", null));
    }

    [Fact]
    public void EditWithoutIdIsRejectedBeforeDatabaseAccess()
    {
        Assert.Throws<ArgumentException>(() => BaseServices<Row, Row, Row, Row>.CheckCodeExist(
            "Rows", "Code", "value", ModifyType.Edit, null, "Code", null));
    }

    public class Row
    {
        [SugarColumn(IsPrimaryKey = true)]
        public Guid ID { get; set; }
        public string AuditStatus { get; set; }
        public string Untouched { get; set; }
    }

    private sealed class UpdateService : BaseServices<Row, Row, Row, Row>
    {
        public bool UpdateResult { get; set; }
        public Row Stored { get; } = new() { ID = Guid.NewGuid(), AuditStatus = "Stored" };
        public bool ReadCalled { get; private set; }
        public override Task<bool> Update(Guid id, object entity) => Task.FromResult(UpdateResult);
        public override Task<Row> QueryDto(object id, bool blnUseCache = false)
        {
            ReadCalled = true;
            return Task.FromResult(Stored);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpdateReturnOnlyReadsAfterSuccessfulUpdate(bool succeeds)
    {
        var service = new UpdateService { UpdateResult = succeeds };
        var result = await service.UpdateReturn(service.Stored.ID, (object)new { AuditStatus = "Input" });
        Assert.Equal(succeeds, service.ReadCalled);
        if (succeeds) Assert.Same(service.Stored, result);
        else Assert.Null(result);
    }

    [Theory]
    [InlineData(DbType.SqlServer)]
    [InlineData(DbType.MySql)]
    [InlineData(DbType.PostgreSQL)]
    public void AuditSqlRetainsPrimaryKeysAndOriginalState(DbType dbType)
    {
        // SQL generation only: no connection is opened and no command is executed.
        using var db = new SqlSugarClient(new ConnectionConfig
        {
            DbType = dbType,
            ConnectionString = dbType == DbType.PostgreSQL
                ? "Host=127.0.0.1;Database=offline_sql_generation;Username=unused;Password=unused"
                : "Server=127.0.0.1;Database=offline_sql_generation;User ID=unused;Password=unused",
            InitKeyType = InitKeyType.Attribute,
            IsAutoCloseConnection = true
        });
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var rows = new List<Row>
            {
                new() { ID = first, AuditStatus = "CompleteAudit" },
                new() { ID = second, AuditStatus = "CompleteAudit" }
            };
        var statements = rows.Select(row => db.Updateable(row)
            .Where(value => value.ID == row.ID)
            .UpdateColumns(new[] { "AuditStatus" })
            .Where("AuditStatus = 'Add'")
            .ToSqlString());
        var sql = string.Join("\n", statements);
        Assert.Contains("AuditStatus = 'Add'", sql);
        Assert.Contains(first.ToString(), sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(second.ToString(), sql, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(DbType.SqlServer)]
    [InlineData(DbType.MySql)]
    [InlineData(DbType.PostgreSQL)]
    public void ExpressionUpdateOnlySetsSelectedColumns(DbType dbType)
    {
        using var db = new SqlSugarClient(new ConnectionConfig
        {
            DbType = dbType,
            ConnectionString = dbType == DbType.PostgreSQL
                ? "Host=127.0.0.1;Database=offline_sql_generation;Username=unused;Password=unused"
                : "Server=127.0.0.1;Database=offline_sql_generation;User ID=unused;Password=unused",
            InitKeyType = InitKeyType.Attribute,
            IsAutoCloseConnection = true
        });
        var id = Guid.NewGuid();
        var row = new Row { ID = id, AuditStatus = "CompleteAudit", Untouched = "must-not-write" };
        var sql = db.Updateable(row)
            .UpdateColumns(value => new { value.AuditStatus })
            .Where(value => value.ID == id && value.AuditStatus == "Add")
            .ToSqlString();
        Assert.Contains(id.ToString(), sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CompleteAudit", sql);
        Assert.Contains("Add", sql);
        Assert.DoesNotContain("Untouched", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("must-not-write", sql);
    }
}
