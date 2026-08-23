using System.Text.RegularExpressions;
using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Configuration;

public sealed record BusinessQueryOptions
{
    public const string SectionName = "BusinessQuery";

    public string DataSourceCode { get; init; } = string.Empty;
    public string Dialect { get; init; } = string.Empty;
    public string CatalogPath { get; init; } = string.Empty;
    public string ExpectedCatalogHash { get; init; } = string.Empty;
    public string CredentialAlias { get; init; } = string.Empty;
    public bool AllowDevelopmentSqlite { get; init; }
    public string DevelopmentSqliteDatabasePath { get; init; } = string.Empty;
    public string AuditDatabasePath { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string CredentialEnvironmentVariable { get; init; } = string.Empty;
    public string ServiceTokenEnvironmentVariable { get; init; } = string.Empty;
    public string DevelopmentServiceToken { get; init; } = string.Empty;
    public string ServerCode { get; init; } = string.Empty;
    public string ExecutionContextIssuer { get; init; } = string.Empty;
    public string ExecutionContextAudience { get; init; } = string.Empty;
    public string ExecutionContextSigningKeyAlias { get; init; } = string.Empty;
    public string DevelopmentExecutionContextSigningKey { get; init; } = string.Empty;
    public string PreviousExecutionContextSigningKeyAlias { get; init; } = string.Empty;
    public int ExecutionContextClockSkewSeconds { get; init; } = 5;
    public int CommandTimeoutSeconds { get; init; } = 30;
    public int MaximumResultRows { get; init; } = 100;
    public int MinimumGroupSize { get; init; } = 5;
    public int MaximumComplexity { get; init; } = 250;
    public int MaximumConcurrentQueriesPerUser { get; init; } = 2;
    public int MaximumConcurrentQueriesPerTenant { get; init; } = 20;
    public int QuotaReservationTtlSeconds { get; init; } = 360;
    public int MaximumReplayEntries { get; init; } = 10000;
}

public sealed partial class BusinessQueryOptionsValidator(
    IHostEnvironment? environment = null)
    : IValidateOptions<BusinessQueryOptions>
{
    public ValidateOptionsResult Validate(string? name, BusinessQueryOptions options)
    {
        var failures = new List<string>();
        if (!SafeCode().IsMatch(options.DataSourceCode ?? string.Empty))
        {
            failures.Add("BusinessQuery:DataSourceCode is invalid.");
        }

        bool sqlServer = string.Equals(
            options.Dialect, "SqlServer", StringComparison.Ordinal);
        bool sqlite = string.Equals(
            options.Dialect, "Sqlite", StringComparison.Ordinal);
        bool mySql = string.Equals(
            options.Dialect, "MySql", StringComparison.Ordinal);
        if (!sqlServer && !sqlite && !mySql)
        {
            failures.Add("BusinessQuery:Dialect must be SqlServer, MySql, or the development-only Sqlite mode.");
        }

        if (sqlite
            && (!options.AllowDevelopmentSqlite
                || !SafeRelativePath(options.DevelopmentSqliteDatabasePath)
                || !options.DevelopmentSqliteDatabasePath.EndsWith(
                    ".db", StringComparison.OrdinalIgnoreCase)))
        {
            failures.Add("Development SQLite requires explicit opt-in and a safe relative .db path.");
        }

        if (!sqlite
            && (options.AllowDevelopmentSqlite
                || !string.IsNullOrEmpty(options.DevelopmentSqliteDatabasePath)))
        {
            failures.Add("Development SQLite settings cannot be enabled for this provider.");
        }

        if (!string.IsNullOrEmpty(options.DevelopmentExecutionContextSigningKey)
            && (environment?.IsDevelopment() != true
                || !sqlite
                || !options.AllowDevelopmentSqlite
                || !IsValidSigningKey(options.DevelopmentExecutionContextSigningKey)))
        {
            failures.Add("The development execution-context signing key is invalid.");
        }

        if (!string.IsNullOrEmpty(options.DevelopmentServiceToken)
            && (environment?.IsDevelopment() != true
                || !sqlite
                || !options.AllowDevelopmentSqlite
                || options.DevelopmentServiceToken.Length is < 32 or > 256
                || options.DevelopmentServiceToken.Contains('\r')
                || options.DevelopmentServiceToken.Contains('\n')))
        {
            failures.Add("The development service token is invalid.");
        }

        if (!SafeRelativePath(options.CatalogPath))
        {
            failures.Add("BusinessQuery:CatalogPath must be a safe relative JSON path.");
        }

        if (!Sha256().IsMatch(options.ExpectedCatalogHash ?? string.Empty))
        {
            failures.Add("BusinessQuery:ExpectedCatalogHash must be a trusted SHA-256 value.");
        }

        if (!CredentialAlias().IsMatch(options.CredentialAlias ?? string.Empty))
        {
            failures.Add("BusinessQuery:CredentialAlias must use alias: syntax.");
        }

        if (!EnvironmentVariable().IsMatch(options.CredentialEnvironmentVariable ?? string.Empty)
            || !EnvironmentVariable().IsMatch(options.ServiceTokenEnvironmentVariable ?? string.Empty)
            || string.Equals(
                options.CredentialEnvironmentVariable,
                options.ServiceTokenEnvironmentVariable,
                StringComparison.Ordinal))
        {
            failures.Add("BusinessQuery secret environment variable names are invalid.");
        }

        if (!SafeCode().IsMatch(options.ServerCode ?? string.Empty)
            || !SafeCode().IsMatch(options.ExecutionContextIssuer ?? string.Empty)
            || !SafeCode().IsMatch(options.ExecutionContextAudience ?? string.Empty)
            || !CredentialAlias().IsMatch(
                options.ExecutionContextSigningKeyAlias ?? string.Empty)
            || (!string.IsNullOrEmpty(options.PreviousExecutionContextSigningKeyAlias)
                && (!CredentialAlias().IsMatch(
                        options.PreviousExecutionContextSigningKeyAlias)
                    || string.Equals(
                        options.ExecutionContextSigningKeyAlias,
                        options.PreviousExecutionContextSigningKeyAlias,
                        StringComparison.Ordinal)))
            || options.ExecutionContextClockSkewSeconds is < 0 or > 5)
        {
            failures.Add("Business Query execution-context configuration is invalid.");
        }

        if (!SafeRelativePath(options.AuditDatabasePath)
            || !options.AuditDatabasePath.EndsWith(".db", StringComparison.OrdinalIgnoreCase)
            || string.Equals(options.AuditDatabasePath, options.CatalogPath, StringComparison.OrdinalIgnoreCase)
            || (sqlite && string.Equals(
                options.AuditDatabasePath,
                options.DevelopmentSqliteDatabasePath,
                StringComparison.OrdinalIgnoreCase)))
        {
            failures.Add("BusinessQuery:AuditDatabasePath must be a separate safe relative .db path.");
        }

        if (!SafeCode().IsMatch(options.TenantId ?? string.Empty)
            || options.CommandTimeoutSeconds is < 1 or > 300
            || options.MaximumResultRows is < 1 or > 100
            || options.MinimumGroupSize is < 2 or > 1000
            || options.MaximumComplexity < 1
            || options.MaximumConcurrentQueriesPerUser is < 1 or > 100
            || options.MaximumConcurrentQueriesPerTenant
                < options.MaximumConcurrentQueriesPerUser
            || options.MaximumConcurrentQueriesPerTenant > 1000
            || options.QuotaReservationTtlSeconds is < 60 or > 900
            || options.MaximumReplayEntries is < 100 or > 1000000)
        {
            failures.Add("BusinessQuery Policy and execution limits are invalid.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool SafeRelativePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || Path.IsPathRooted(value)
            || value.Contains("..", StringComparison.Ordinal)
            || value.Any(char.IsControl))
        {
            return false;
        }

        return value.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
            .All(segment => segment.Length <= 128 && SafePathSegment().IsMatch(segment));
    }

    private static bool IsValidSigningKey(string encoded)
    {
        try
        {
            byte[] key = Convert.FromBase64String(encoded);
            bool valid = key.Length is >= 32 and <= 64;
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(key);
            return valid;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    [GeneratedRegex("^[a-z][a-z0-9-]{1,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeCode();

    [GeneratedRegex("^[a-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256();

    [GeneratedRegex("^alias:[a-z][a-z0-9.-]{1,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex CredentialAlias();

    [GeneratedRegex("^[A-Za-z0-9_.-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SafePathSegment();

    [GeneratedRegex("^[A-Z][A-Z0-9_]{2,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex EnvironmentVariable();
}
