#nullable enable
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using EU.Core.Model.Entity;
using Xunit;

namespace EU.Core.Tests;

/// <summary>额度初始迁移与实体归属契约的离线检查，不连接数据库。</summary>
public sealed class AgentUserTokenQuotaMigrationTests
{
    [Theory]
    [InlineData("SqlServer", "075_add_user_token_quota.sql", "AgUserTokenQuotaPeriod", "GroupId,CompanyId,PeriodKind,PeriodKey")]
    [InlineData("SqlServer", "075_add_user_token_quota.sql", "AgUserTokenQuotaReservation", "GroupId,CompanyId,State")]
    [InlineData("SqlServer", "076_add_user_token_quota_management.sql", "AgUserTokenQuotaPolicy", "GroupId,CompanyId")]
    [InlineData("SqlServer", "076_add_user_token_quota_management.sql", "AgUserTokenQuotaAdjustment", "GroupId,CompanyId")]
    [InlineData("MySql", "013_add_user_token_quota.sql", "AgUserTokenQuotaPeriod", "GroupId,CompanyId,PeriodKind,PeriodKey")]
    [InlineData("MySql", "013_add_user_token_quota.sql", "AgUserTokenQuotaReservation", "GroupId,CompanyId,State")]
    [InlineData("MySql", "014_add_user_token_quota_management.sql", "AgUserTokenQuotaPolicy", "GroupId,CompanyId")]
    [InlineData("MySql", "014_add_user_token_quota_management.sql", "AgUserTokenQuotaAdjustment", "GroupId,CompanyId")]
    public void Initial_schema_has_no_TenantId_and_indexes_the_full_owner(string dialect, string file, string table, string expectedOwnerColumns)
    {
        var sql = ReadMigration(dialect, file);
        var definition = Regex.Match(sql, @"CREATE TABLE (?:IF NOT EXISTS )?(?:dbo\.)?" + table + @"\s*\((.*?)\n\s*\)(?: ENGINE=[^\r\n;]*)?;", RegexOptions.Singleline);
        Assert.True(definition.Success, $"Missing CREATE TABLE for {table}.");
        Assert.DoesNotContain("TenantId", definition.Groups[1].Value);
        foreach (var column in new[] { "GroupId", "CompanyId" })
            Assert.Matches(@"\b" + column + @"\s+(?:uniqueidentifier|char\(36\))\s", definition.Groups[1].Value);
        Assert.DoesNotMatch(@"\bUserId\b", definition.Groups[1].Value);
        if (table == nameof(AgUserTokenQuotaReservation)) Assert.Matches(@"\bConsumerUserId\s+(?:uniqueidentifier|char\(36\))\s", definition.Groups[1].Value);

        var indexName = table switch
        {
            nameof(AgUserTokenQuotaPeriod) => "UX_AgUserTokenQuotaPeriod_OwnerPeriod",
            nameof(AgUserTokenQuotaReservation) => "IX_AgUserTokenQuotaReservation_OwnerState",
            nameof(AgUserTokenQuotaPolicy) => "UX_AgUserTokenQuotaPolicy_Owner",
            nameof(AgUserTokenQuotaAdjustment) => "IX_AgUserTokenQuotaAdjustment_Owner",
            _ => throw new ArgumentOutOfRangeException(nameof(table))
        };
        var index = Regex.Match(sql, @"(?:CREATE (?:UNIQUE )?INDEX|(?:UNIQUE )?KEY) " + indexName + @"(?: ON (?:dbo\.)?" + table + @")?\s*\(([^)]+)\)");
        Assert.True(index.Success, $"Missing owner index for {table}.");
        Assert.Equal(expectedOwnerColumns, Regex.Replace(index.Groups[1].Value, @"\s", string.Empty));
    }

    [Fact]
    public void Legacy_conversion_checks_all_tables_under_locks_before_altering_schema()
    {
        var sql = ReadMigration("SqlServer", "077_convert_user_quota_to_group_company.sql");
        var lockPosition = sql.IndexOf("WITH (TABLOCKX, HOLDLOCK)", StringComparison.Ordinal);
        var emptyGuard = sql.IndexOf("IF @Rows<>0 THROW 51995", StringComparison.Ordinal);
        var firstDrop = sql.IndexOf("N'DROP INDEX ", StringComparison.Ordinal);
        Assert.True(lockPosition > 0 && emptyGuard > lockPosition && firstDrop > emptyGuard);
        Assert.Contains("COUNT_BIG(*)", sql);
        Assert.DoesNotContain("WHERE IsDeleted", sql);
        Assert.DoesNotContain("DELETE FROM", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP TABLE", sql, StringComparison.OrdinalIgnoreCase);
        foreach (var table in new[] { "AgUserTokenQuotaPeriod", "AgUserTokenQuotaReservation", "AgUserTokenQuotaPolicy", "AgUserTokenQuotaAdjustment" })
            Assert.Contains($"(N'{table}', N'", sql[..lockPosition]);
        Assert.Contains("Unrecognized legacy quota index", sql);
        Assert.Contains("Custom quota dependencies require manual review", sql);
        Assert.Contains("ROLLBACK TRANSACTION", sql);
    }

    [Fact]
    public void MySql_legacy_conversion_refuses_nonempty_or_custom_dependent_tables_before_altering_columns()
    {
        var sql = ReadMigration("MySql", "015_convert_user_quota_to_group_company.sql");
        var emptyGuard = sql.IndexOf("Legacy per-user quota schema contains data", StringComparison.Ordinal);
        var dependencyGuard = sql.IndexOf("Custom quota dependencies require manual review", StringComparison.Ordinal);
        var collisionGuard = sql.IndexOf("Reservation contains both UserId and ConsumerUserId", StringComparison.Ordinal);
        var compatibilityGuard = sql.IndexOf("Legacy quota owner columns are incomplete", StringComparison.Ordinal);
        var firstDrop = sql.IndexOf("DROP INDEX UX_AgUserTokenQuotaPeriod_OwnerPeriod", StringComparison.Ordinal);
        var firstAlter = sql.IndexOf("ALTER TABLE", StringComparison.Ordinal);
        Assert.True(emptyGuard > 0 && dependencyGuard > emptyGuard && collisionGuard > dependencyGuard &&
            compatibilityGuard > collisionGuard && firstDrop > compatibilityGuard && firstAlter > compatibilityGuard);
        Assert.DoesNotContain("DELETE FROM", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP TABLE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RENAME COLUMN UserId TO ConsumerUserId", sql);
    }

    private static string ReadMigration(string dialect, string file, [CallerFilePath] string sourcePath = "")
    {
        var directory = FindSolution(new FileInfo(sourcePath).Directory) ?? FindSolution(new DirectoryInfo(AppContext.BaseDirectory))
            ?? FindSolution(new DirectoryInfo(Environment.CurrentDirectory));
        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory.FullName, "EU.Core.Api.Agent", "Database", "Migrations", dialect, file));
    }

    private static DirectoryInfo? FindSolution(DirectoryInfo? directory)
    {
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EU.Core.sln")))
            directory = directory.Parent;
        return directory;
    }
}
