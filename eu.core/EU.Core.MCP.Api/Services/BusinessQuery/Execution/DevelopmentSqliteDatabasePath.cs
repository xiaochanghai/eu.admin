using EU.Core.Api.MCP.Services.BusinessQuery.Configuration;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Execution;

public sealed class DevelopmentSqliteDatabasePath
{
    public DevelopmentSqliteDatabasePath(
        IOptions<BusinessQueryOptions> options,
        IHostEnvironment environment)
    {
        BusinessQueryOptions configuration = options.Value;
        if (!environment.IsDevelopment()
            || !configuration.AllowDevelopmentSqlite
            || !string.Equals(configuration.Dialect, "Sqlite", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The development SQLite business data source is unavailable.");
        }

        string root = Path.GetFullPath(environment.ContentRootPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        DatabasePath = Path.GetFullPath(
            configuration.DevelopmentSqliteDatabasePath,
            root);
        string auditPath = Path.GetFullPath(configuration.AuditDatabasePath, root);
        if (!DatabasePath.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            || string.Equals(DatabasePath, auditPath, StringComparison.OrdinalIgnoreCase)
            || !File.Exists(DatabasePath)
            || ContainsReparsePoint(DatabasePath, root))
        {
            throw new InvalidOperationException(
                "The development SQLite business data source is unavailable.");
        }
    }

    public string DatabasePath { get; }

    private static bool ContainsReparsePoint(string path, string root)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            return true;
        }

        string rootDirectory = root.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);
        DirectoryInfo? current = Directory.GetParent(path);
        while (current is not null
            && current.FullName.StartsWith(rootDirectory, StringComparison.OrdinalIgnoreCase))
        {
            if ((current.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                return true;
            }

            if (string.Equals(
                current.FullName,
                rootDirectory,
                StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            current = current.Parent;
        }

        return false;
    }
}
