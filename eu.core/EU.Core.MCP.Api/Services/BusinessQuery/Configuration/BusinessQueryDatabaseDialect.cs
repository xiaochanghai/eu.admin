using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Configuration;

/// <summary>仅根据项目 SqlSugar 连接类型选择执行方言；Auto 不隐式启用开发 SQLite。</summary>
public static class BusinessQueryDatabaseDialect
{
    public static BusinessCatalogDialect Resolve(SqlSugar.DbType databaseType, string configuredDialect, bool development, bool allowSqlite)
    {
        var actual = databaseType switch
        {
            SqlSugar.DbType.SqlServer => BusinessCatalogDialect.SqlServer,
            SqlSugar.DbType.MySql => BusinessCatalogDialect.MySql,
            SqlSugar.DbType.Sqlite when configuredDialect == "Sqlite" && development && allowSqlite => BusinessCatalogDialect.Sqlite,
            _ => throw new InvalidOperationException("The project database provider is unsupported for BusinessQuery.")
        };
        if (configuredDialect != "Auto" && !string.Equals(configuredDialect, actual.ToString(), StringComparison.Ordinal))
            throw new InvalidOperationException("The project database does not match BusinessQuery configuration.");
        return actual;
    }
}
