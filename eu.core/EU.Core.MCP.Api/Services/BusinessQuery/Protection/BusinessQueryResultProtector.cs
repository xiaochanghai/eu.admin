using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;
using EU.Core.Api.MCP.Services.BusinessQuery.Compilation;
using EU.Core.Api.MCP.Services.BusinessQuery.Contracts;
using EU.Core.Api.MCP.Services.BusinessQuery.Execution;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Protection;

public sealed class BusinessQueryResultProtector
{
    public BusinessQueryResult Protect(
        CompiledBusinessQuery query,
        IReadOnlyList<object?[]> sourceRows,
        BusinessQueryExecutionLimits limits)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(sourceRows);
        ArgumentNullException.ThrowIfNull(limits);
        limits.Validate();
        if (sourceRows.Count > limits.MaximumRows
            || sourceRows.Count > query.MaximumResultRows)
        {
            throw Error(query.IncludeBoundaryTies
                ? BusinessQueryExecutionErrorCodes.TieResultLimitExceeded
                : BusinessQueryExecutionErrorCodes.ResultLimitExceeded);
        }

        int[] visibleIndexes = query.Columns
            .Select((column, index) => (column, index))
            .Where(value => value.column.Sensitivity != BusinessCatalogSensitivity.Restricted)
            .Select(value => value.index)
            .ToArray();
        var columns = visibleIndexes.Select(index =>
        {
            CompiledBusinessQueryColumn column = query.Columns[index];
            return new BusinessQueryColumn(
                column.ResultKey,
                ToValueKind(column.DataType),
                column.Unit,
                column.Currency);
        }).Append(new BusinessQueryColumn(
            "rank",
            BusinessQueryValueKind.Integer,
            "count",
            string.Empty)).ToArray();
        if (columns.Length > limits.MaximumColumns)
        {
            throw Error(BusinessQueryExecutionErrorCodes.ResultLimitExceeded);
        }

        var rows = new List<BusinessQueryRow>(sourceRows.Count);
        int payloadBytes = 0;
        foreach (object?[] source in sourceRows)
        {
            if (source.Length != query.Columns.Count + 1)
            {
                throw Error(BusinessQueryExecutionErrorCodes.ResultInvalid);
            }

            var values = new Dictionary<string, BusinessQueryValue>(StringComparer.Ordinal);
            foreach (int index in visibleIndexes)
            {
                CompiledBusinessQueryColumn column = query.Columns[index];
                BusinessQueryValue protectedValue = ProtectValue(column, source[index]);
                AddValue(values, column.ResultKey, protectedValue, limits, ref payloadBytes);
            }

            BusinessQueryValue rank = ProtectInteger(source[^1]);
            if (!long.TryParse(
                    rank.CanonicalValue,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out long rankValue)
                || rankValue < 1)
            {
                throw Error(BusinessQueryExecutionErrorCodes.ResultInvalid);
            }

            AddValue(values, "rank", rank, limits, ref payloadBytes);
            rows.Add(new BusinessQueryRow(values));
        }

        string hash = ComputeHash(columns, rows, out int exactPayloadBytes);
        if (exactPayloadBytes > limits.MaximumPayloadUtf8Bytes)
        {
            throw Error(BusinessQueryExecutionErrorCodes.ResultLimitExceeded);
        }

        return new BusinessQueryResult(columns, rows, false, hash);
    }

    private static BusinessQueryValue ProtectValue(
        CompiledBusinessQueryColumn column,
        object? value)
    {
        if (value is null or DBNull)
        {
            return new BusinessQueryValue(BusinessQueryValueKind.Null, string.Empty);
        }

        return column.DataType switch
        {
            BusinessCatalogDataType.String => new BusinessQueryValue(
                BusinessQueryValueKind.String,
                Sanitize(value.ToString() ?? string.Empty),
                true),
            BusinessCatalogDataType.Boolean => new BusinessQueryValue(
                BusinessQueryValueKind.Boolean,
                Convert.ToBoolean(value, CultureInfo.InvariantCulture) ? "true" : "false"),
            BusinessCatalogDataType.Integer => ProtectInteger(value),
            BusinessCatalogDataType.Decimal => new BusinessQueryValue(
                BusinessQueryValueKind.Decimal,
                RoundDecimal(value, column.Scale).ToString("G29", CultureInfo.InvariantCulture)),
            BusinessCatalogDataType.Date => new BusinessQueryValue(
                BusinessQueryValueKind.Date,
                ConvertDate(value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            BusinessCatalogDataType.DateTime => new BusinessQueryValue(
                BusinessQueryValueKind.DateTime,
                ConvertDate(value).ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)),
            _ => throw Error(BusinessQueryExecutionErrorCodes.ResultInvalid)
        };
    }

    private static BusinessQueryValue ProtectInteger(object? value)
    {
        try
        {
            return new BusinessQueryValue(
                BusinessQueryValueKind.Integer,
                Convert.ToInt64(value, CultureInfo.InvariantCulture)
                    .ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception exception) when (
            exception is FormatException or InvalidCastException or OverflowException)
        {
            throw Error(BusinessQueryExecutionErrorCodes.ResultInvalid);
        }
    }

    private static decimal RoundDecimal(object value, int? scale)
    {
        try
        {
            decimal number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            return scale.HasValue
                ? decimal.Round(number, scale.Value, MidpointRounding.ToEven)
                : number;
        }
        catch (Exception exception) when (
            exception is FormatException or InvalidCastException or OverflowException)
        {
            throw Error(BusinessQueryExecutionErrorCodes.ResultInvalid);
        }
    }

    private static DateTimeOffset ConvertDate(object value)
    {
        if (value is DateTimeOffset offset)
        {
            return offset;
        }

        if (value is DateTime dateTime)
        {
            return new DateTimeOffset(
                DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
        }

        if (DateTimeOffset.TryParse(
            value.ToString(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out DateTimeOffset parsed))
        {
            return parsed;
        }

        throw Error(BusinessQueryExecutionErrorCodes.ResultInvalid);
    }

    private static void AddValue(
        IDictionary<string, BusinessQueryValue> values,
        string key,
        BusinessQueryValue value,
        BusinessQueryExecutionLimits limits,
        ref int payloadBytes)
    {
        int bytes = Encoding.UTF8.GetByteCount(key)
            + Encoding.UTF8.GetByteCount(value.CanonicalValue);
        if (Encoding.UTF8.GetByteCount(value.CanonicalValue) > limits.MaximumCellUtf8Bytes
            || payloadBytes > limits.MaximumPayloadUtf8Bytes - bytes)
        {
            throw Error(BusinessQueryExecutionErrorCodes.ResultLimitExceeded);
        }

        payloadBytes += bytes;
        values.Add(key, value);
    }

    private static string Sanitize(string value)
    {
        var result = new StringBuilder(value.Length);
        foreach (Rune rune in value.EnumerateRunes())
        {
            if (!Rune.IsControl(rune))
            {
                result.Append(rune.ToString());
            }
        }

        return result.ToString();
    }

    private static string ComputeHash(
        IReadOnlyList<BusinessQueryColumn> columns,
        IReadOnlyList<BusinessQueryRow> rows,
        out int payloadBytes)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("columns");
            writer.WriteStartArray();
            foreach (BusinessQueryColumn column in columns)
            {
                writer.WriteStartObject();
                writer.WriteString("key", column.Key);
                writer.WriteString("kind", column.Kind.ToString());
                writer.WriteString("unit", column.Unit);
                writer.WriteString("currency", column.Currency);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WritePropertyName("rows");
            writer.WriteStartArray();
            foreach (BusinessQueryRow row in rows)
            {
                writer.WriteStartObject();
                foreach (BusinessQueryColumn column in columns)
                {
                    BusinessQueryValue value = row.Values[column.Key];
                    writer.WritePropertyName(column.Key);
                    writer.WriteStartObject();
                    writer.WriteString("kind", value.Kind.ToString());
                    writer.WriteString("value", value.CanonicalValue);
                    writer.WriteBoolean("untrustedData", value.UntrustedData);
                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        byte[] payload = stream.ToArray();
        payloadBytes = payload.Length;
        return Convert.ToHexStringLower(SHA256.HashData(payload));
    }

    private static BusinessQueryValueKind ToValueKind(BusinessCatalogDataType value) =>
        value switch
        {
            BusinessCatalogDataType.String => BusinessQueryValueKind.String,
            BusinessCatalogDataType.Boolean => BusinessQueryValueKind.Boolean,
            BusinessCatalogDataType.Integer => BusinessQueryValueKind.Integer,
            BusinessCatalogDataType.Decimal => BusinessQueryValueKind.Decimal,
            BusinessCatalogDataType.Date => BusinessQueryValueKind.Date,
            BusinessCatalogDataType.DateTime => BusinessQueryValueKind.DateTime,
            _ => throw Error(BusinessQueryExecutionErrorCodes.ResultInvalid)
        };

    private static BusinessQueryExecutionException Error(string code) => new(code);
}
