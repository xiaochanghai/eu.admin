using EU.Core.Api.MCP.Services.BusinessQuery.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Persistence;

public sealed class BusinessQueryStorePath
{
    public BusinessQueryStorePath(
        IOptions<BusinessQueryOptions> options,
        IHostEnvironment environment)
    {
        string root = Path.GetFullPath(environment.ContentRootPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        DatabasePath = Path.GetFullPath(options.Value.AuditDatabasePath, root);
        if (!DatabasePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The Business Query store path is invalid.");
        }

        string? directory = Path.GetDirectoryName(DatabasePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("The Business Query store directory is invalid.");
        }

        Directory.CreateDirectory(directory);
        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = true,
            DefaultTimeout = 5
        }.ToString();
    }

    public string DatabasePath { get; }

    public string ConnectionString { get; }
}
