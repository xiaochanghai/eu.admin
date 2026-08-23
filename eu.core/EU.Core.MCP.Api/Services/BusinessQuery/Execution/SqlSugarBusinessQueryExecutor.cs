using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text.RegularExpressions;
using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;
using EU.Core.Api.MCP.Services.BusinessQuery.Compilation;
using EU.Core.Api.MCP.Services.BusinessQuery.Protection;
using SqlSugar;
using DbType = System.Data.DbType;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Execution;

public sealed partial class SqlSugarBusinessQueryExecutor : IBusinessQueryExecutor
{
    private readonly BusinessQueryResultProtector _protector;

    public SqlSugarBusinessQueryExecutor(BusinessQueryResultProtector? protector = null)
    {
        _protector = protector ?? new BusinessQueryResultProtector();
    }

    public async Task<BusinessQueryExecutionResult> ExecuteAsync(
        CompiledBusinessQuery query,
        BusinessDataSourceDescriptor descriptor,
        ISqlSugarClient database,
        BusinessQueryExecutionLimits limits,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(limits);
        limits.Validate();
        if (cancellationToken.IsCancellationRequested)
        {
            throw Error(BusinessQueryExecutionErrorCodes.Cancelled);
        }
        ValidateDescriptor(query, descriptor);
        ValidateReadOnlyCommand(query.CommandText);

        int maximumRows = Math.Min(limits.MaximumRows, query.MaximumResultRows);
        var stopwatch = Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(limits.CommandTimeoutSeconds));
        try
        {
            SugarParameter[] parameters = query.Parameters
                .Select(parameter => new SugarParameter(
                    parameter.Name,
                    ToProviderValue(
                    parameter.Value,
                    parameter.DataType,
                    descriptor.Dialect),
                    ToDbType(parameter.DataType, descriptor.Dialect)))
                .ToArray();
            var rows = new List<object?[]>();
            database.Ado.CommandTimeOut = limits.CommandTimeoutSeconds;
            database.Ado.CancellationToken = timeout.Token;
            try
            {
                IDataReader dataReader = await database.Ado
                    .GetDataReaderAsync(query.CommandText, parameters)
                    .ConfigureAwait(false);
                if (dataReader is not DbDataReader reader)
                {
                    dataReader.Dispose();
                    throw Error(BusinessQueryExecutionErrorCodes.ResultInvalid);
                }

                await using (reader)
                {
                    int expectedColumns = query.Columns.Count + 1;
                    if (reader.FieldCount != expectedColumns
                        || reader.FieldCount > limits.MaximumColumns)
                    {
                        throw Error(BusinessQueryExecutionErrorCodes.ResultInvalid);
                    }

                    while (await reader.ReadAsync(timeout.Token).ConfigureAwait(false))
                    {
                        if (rows.Count >= maximumRows)
                        {
                            throw Error(query.IncludeBoundaryTies
                                ? BusinessQueryExecutionErrorCodes.TieResultLimitExceeded
                                : BusinessQueryExecutionErrorCodes.ResultLimitExceeded);
                        }

                        var values = new object?[reader.FieldCount];
                        for (int index = 0; index < reader.FieldCount; index++)
                        {
                            values[index] = await reader.IsDBNullAsync(index, timeout.Token)
                                .ConfigureAwait(false)
                                ? null
                                : reader.GetValue(index);
                        }

                        rows.Add(values);
                    }
                }
            }
            finally
            {
                database.Ado.RemoveCancellationToken();
            }

            stopwatch.Stop();
            return new BusinessQueryExecutionResult(
                _protector.Protect(query, rows, limits),
                stopwatch.Elapsed,
                "succeeded");
        }
        catch (BusinessQueryExecutionException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw Error(BusinessQueryExecutionErrorCodes.Timeout);
        }
        catch (Exception exception) when (
            exception is DbException
                or InvalidOperationException
                or InvalidCastException
                or FormatException
                or OverflowException)
        {
            throw Error(BusinessQueryExecutionErrorCodes.ExecutionFailed);
        }
    }

    public static void ValidateReadOnlyCommand(string commandText)
    {
        string value = commandText?.TrimStart() ?? string.Empty;
        if (!(value.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("WITH ", StringComparison.OrdinalIgnoreCase))
            || value.Contains(';', StringComparison.Ordinal)
            || value.Contains("--", StringComparison.Ordinal)
            || value.Contains("/*", StringComparison.Ordinal)
            || value.Contains("*/", StringComparison.Ordinal)
            || MutatingKeywordPattern().IsMatch(value))
        {
            throw Error(BusinessQueryExecutionErrorCodes.CommandRejected);
        }
    }

    private static void ValidateDescriptor(
        CompiledBusinessQuery query,
        BusinessDataSourceDescriptor descriptor)
    {
        if (!descriptor.ReadOnly)
        {
            throw Error(BusinessQueryExecutionErrorCodes.ReadOnlyRequired);
        }

        if (!string.Equals(
                descriptor.DataSourceCode,
                query.DataSourceCode,
                StringComparison.Ordinal)
            || descriptor.Dialect != query.Dialect
            || !ProviderMatchesDialect(
                descriptor.ProviderInvariantName,
                descriptor.Dialect)
            || !ProviderPattern().IsMatch(descriptor.ProviderInvariantName ?? string.Empty)
            || !CredentialAliasPattern().IsMatch(descriptor.CredentialAlias ?? string.Empty))
        {
            throw Error(BusinessQueryExecutionErrorCodes.DescriptorMismatch);
        }
    }

    private static bool ProviderMatchesDialect(
        string provider,
        BusinessCatalogDialect dialect) =>
        dialect switch
        {
            BusinessCatalogDialect.SqlServer => string.Equals(
                provider, "Microsoft.Data.SqlClient", StringComparison.Ordinal),
            BusinessCatalogDialect.Sqlite => string.Equals(
                provider, "Microsoft.Data.Sqlite", StringComparison.Ordinal),
            BusinessCatalogDialect.MySql => string.Equals(
                provider, "SqlSugar.MySql", StringComparison.Ordinal),
            _ => false
        };

    internal static object ToProviderValue(
        object value,
        BusinessCatalogDataType dataType,
        BusinessCatalogDialect dialect) =>
        dialect == BusinessCatalogDialect.MySql
            && dataType == BusinessCatalogDataType.DateTime
            && value is DateTimeOffset offset
                ? offset.UtcDateTime
                : value;

    internal static DbType ToDbType(
        BusinessCatalogDataType value,
        BusinessCatalogDialect dialect) =>
        value switch
        {
            BusinessCatalogDataType.String => DbType.String,
            BusinessCatalogDataType.Boolean => DbType.Boolean,
            BusinessCatalogDataType.Integer => DbType.Int64,
            BusinessCatalogDataType.Decimal => DbType.Decimal,
            BusinessCatalogDataType.Date => DbType.Date,
            BusinessCatalogDataType.DateTime when dialect == BusinessCatalogDialect.MySql =>
                DbType.DateTime,
            BusinessCatalogDataType.DateTime => DbType.DateTimeOffset,
            _ => throw Error(BusinessQueryExecutionErrorCodes.ResultInvalid)
        };

    private static BusinessQueryExecutionException Error(string code) => new(code);

    [GeneratedRegex(
        "\\b(?:INSERT|UPDATE|DELETE|DROP|ALTER|CREATE|TRUNCATE|MERGE|EXEC(?:UTE)?|PRAGMA|ATTACH|DETACH|VACUUM|REINDEX)\\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MutatingKeywordPattern();

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9.]{1,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex ProviderPattern();

    [GeneratedRegex("^alias:[a-z][a-z0-9.-]{1,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex CredentialAliasPattern();
}
